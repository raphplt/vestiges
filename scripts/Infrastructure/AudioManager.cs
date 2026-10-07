using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Infrastructure;

/// <summary>
/// Gestionnaire audio central — Autoload singleton.
/// Gère la musique adaptative (exploration/combat/Résurgences/Hub) et tous les SFX du jeu
/// via abonnement à l'EventBus. Crée les buses audio si absentes.
/// Persiste les réglages dans user://audio_settings.cfg.
/// </summary>
public partial class AudioManager : Node
{
	public static AudioManager Instance { get; private set; }

	// --- Buses ---
	private const string BusMusic    = "Music";
	private const string BusSfx      = "SFX";
	private const string BusAmbiance = "Ambiance";

	// --- Monde assourdi (mort du joueur) ---
	private const float MuffleOpenHz = 20000f;
	private const float MuffleClosedHz = 500f;
	private static readonly string[] MuffledBuses = { BusSfx, BusAmbiance };
	private readonly List<AudioEffectLowPassFilter> _muffles = new();

	// --- Musique (intention et fondus, plan 15 A1) ---
	private MusicDirector _music;

	// --- SFX pool ---
	private AudioVoicePool _sfxPool;
	private const int SfxPoolSize = 12;

	// --- Ambiance player ---
	private AudioStreamPlayer _ambiancePlayer;
	private Tween _ambianceFade;
	private string _ambianceKey = "";
	private AudioStreamPlayer _ambianceOverlayPlayer;
	private string _ambianceOverlayKey = "";

	// --- Looping SFX player (for UI loops like level-up screen) ---
	private AudioStreamPlayer _loopingSfxPlayer;
	private string _loopingSfxKey = "";
	private AudioStreamPlayer _lowHealthLoopPlayer;
	private string _lowHealthLoopKey = "";
	private AudioStreamPlayer _borderWarningPlayer;
	private string _borderWarningKey = "";

	// --- UI SFX pool (plays even when paused) ---
	private AudioVoicePool _uiSfxPool;
	private const int UiSfxPoolSize = 3;

	// --- Stream cache ---
	private readonly Dictionary<string, AudioStream> _streams = new();

	// --- SFX throttle (prevents spam of the same sound) ---
	private readonly Dictionary<string, ulong> _sfxLastPlayTime = new();
	private readonly Dictionary<string, ulong> _sfxMinIntervals = new();
	private const ulong DefaultMinInterval = 0;
	private readonly Dictionary<string, ulong> _uiLastPlayTime = new();
	private readonly Dictionary<string, int> _soundPriorities = new();
	private readonly Dictionary<string, int> _soundMaxVoices = new();
	private readonly Dictionary<string, string> _soundBuses = new();

	// --- Horloge sonore sous Movie Maker (enregistrements d'écoute, plan 15 A0) ---
	private static ulong _movieFps;
	private double _movieStepSum;
	private int _movieStepCount;
	private const int MovieFpsSkippedFrames = 3;
	private const int MovieFpsSampledFrames = 60;

	private string _currentPhase = "";

	// Tirages propres à l'audio : ils ne décalent pas ceux du gameplay (seed des bancs, avant/après d'écoute).
	private readonly RandomNumberGenerator _rng = new();

	// --- Ambiance oiseaux ---
	private float _birdTimer;
	private string _currentRandomEventId = "";
	private Core.Player _player;
	private World.ErasureManager _erasureManager;
	private World.WorldSetup _worldSetup;
	private TileMapLayer _ground;
	private const float LowHealthStartThreshold = 0.25f;
	private const float LowHealthStopThreshold = 0.33f;
	private const float CriticalDamageThresholdRatio = 0.18f;
	private const float BorderEffacementThreshold = 0.18f;
	private const float MapBorderWarningCells = 2.5f;

	// --- Persistence ---
	private const string SettingsPath = "user://audio_settings.cfg";

	// --- Chemins de tous les streams ---
	private readonly Dictionary<string, string> _paths = new();
	private readonly Dictionary<string, float> _soundVolumes = new();

	public const string SoundBankPath = "res://data/audio/sounds.json";

	private void LoadSoundBank()
	{
		using FileAccess file = FileAccess.Open(SoundBankPath, FileAccess.ModeFlags.Read);
		if (file == null)
			throw new System.InvalidOperationException("Banque audio introuvable.");
		using Json json = new();
		if (json.Parse(file.GetAsText()) != Error.Ok)
			throw new System.InvalidOperationException($"Banque audio invalide : {json.GetErrorMessage()}");
		foreach (System.Collections.Generic.KeyValuePair<Variant, Variant> entry in json.Data.AsGodotDictionary())
		{
			string key = entry.Key.AsString();
			Godot.Collections.Dictionary settings = entry.Value.AsGodotDictionary();
			_paths[key] = settings["path"].AsString();
			_soundVolumes[key] = (float)settings["volume_db"].AsDouble();
			_sfxMinIntervals[key] = (ulong)settings["min_interval_ms"].AsInt64();
			_soundPriorities[key] = settings.ContainsKey("priority") ? settings["priority"].AsInt32() : 20;
			_soundMaxVoices[key] = settings.ContainsKey("max_voices") ? Mathf.Max(1, settings["max_voices"].AsInt32()) : 2;
			_soundBuses[key] = settings.ContainsKey("bus") ? settings["bus"].AsString() : BusSfx;
		}
	}

	/// <summary>Issue d'une demande de son, pour la trace d'écoute (tools/record_run_audio.sh).</summary>
	public enum SoundOutcome { Played, Throttled, VoiceStolen, Interface, VoiceLimited }

	/// <summary>Chaque demande de son et son issue ; sans abonné, ne coûte rien.</summary>
	public event System.Action<string, SoundOutcome> SoundRequested;

	public string CurrentMusicKey => _music?.CurrentKey ?? "";
	public MusicIntent CurrentMusicIntent => _music?.CurrentIntent ?? MusicIntent.None;

	/// <summary>
	/// Horloge des intervalles sonores, en millisecondes. Le Movie Maker de Godot rend à cadence fixe, plus lentement
	/// que le temps réel : l'horloge suit alors les images rendues, qui sont la durée du son enregistré.
	/// </summary>
	public static ulong NowMsec => _movieFps > 0 ? Engine.GetProcessFrames() * 1000UL / _movieFps : Time.GetTicksMsec();

	/// <summary>
	/// La cadence du Movie Maker se lit au pas d'image moyen : le pas varie d'une image à l'autre pour rester calé sur
	/// la physique, mais vaut 1/cadence en moyenne, ralenti compris.
	/// </summary>
	private void ResolveMovieFps()
	{
		if (Engine.GetProcessFrames() < MovieFpsSkippedFrames)
			return;
		_movieStepSum += GetProcessDeltaTime() / Mathf.Max(Engine.TimeScale, 0.001);
		if (++_movieStepCount < MovieFpsSampledFrames)
			return;
		_movieFps = (ulong)Mathf.RoundToInt(_movieStepCount / _movieStepSum);
		GetTree().ProcessFrame -= ResolveMovieFps;
		// Les instants notés sur l'horloge réelle ne se comparent pas à la nouvelle.
		_sfxLastPlayTime.Clear();
		_uiLastPlayTime.Clear();
		GD.Print($"[AudioManager] Movie Maker : horloge sonore à {_movieFps} images/s");
	}

	public override void _Ready()
	{
		Instance = this;
		_movieFps = 0;
		if (OS.HasFeature("movie"))
			GetTree().ProcessFrame += ResolveMovieFps;

		EnsureAudioBuses();
		LoadSettings();
		LoadSoundBank();
		PreloadStreams();

		_music = new MusicDirector { Name = "Music" };
		_music.Initialize(MusicConfig.Load(), key => _streams.GetValueOrDefault(key), key => _soundVolumes.GetValueOrDefault(key));
		AddChild(_music);

		_ambiancePlayer = new AudioStreamPlayer { Bus = BusAmbiance, Name = "Ambiance" };
		AddChild(_ambiancePlayer);
		_ambianceOverlayPlayer = new AudioStreamPlayer { Bus = BusAmbiance, Name = "AmbianceOverlay", VolumeDb = -10f };
		AddChild(_ambianceOverlayPlayer);

		_loopingSfxPlayer = new AudioStreamPlayer { Bus = BusSfx, Name = "LoopingSfx", ProcessMode = ProcessModeEnum.Always };
		AddChild(_loopingSfxPlayer);
		_lowHealthLoopPlayer = new AudioStreamPlayer { Bus = BusSfx, Name = "LowHealthLoop", ProcessMode = ProcessModeEnum.Always, VolumeDb = -10f };
		AddChild(_lowHealthLoopPlayer);
		_borderWarningPlayer = new AudioStreamPlayer { Bus = BusSfx, Name = "BorderWarningLoop", ProcessMode = ProcessModeEnum.Always, VolumeDb = -12f };
		AddChild(_borderWarningPlayer);

		_uiSfxPool = new AudioVoicePool(this, "UiSfx", UiSfxPoolSize, ProcessModeEnum.Always);
		_sfxPool = new AudioVoicePool(this, "Sfx", SfxPoolSize, ProcessModeEnum.Inherit);

		Core.EventBus eventBus = GetNodeOrNull<Core.EventBus>("/root/EventBus");
		if (eventBus != null)
			ConnectEventBus(eventBus);

		_birdTimer = _rng.RandfRange(15f, 35f);

	}

	public override void _ExitTree()
	{
		RemoveWorldMuffle();
		Instance = null;
		Core.EventBus eventBus = GetNodeOrNull<Core.EventBus>("/root/EventBus");
		if (eventBus != null)
			DisconnectEventBus(eventBus);
	}

	// =========================================================
	// BUS MANAGEMENT
	// =========================================================

	/// <summary>
	/// Assourdit les sons du monde, de 0 (net, filtre retiré) à 1 (étouffé), pendant la séquence de mort (plan 02 M1).
	/// La musique n'est pas filtrée : la musique de mort reste claire.
	/// </summary>
	public static void SetWorldMuffle(float amount) => Instance?.ApplyWorldMuffle(amount);

	private void ApplyWorldMuffle(float amount)
	{
		if (amount <= 0f)
		{
			RemoveWorldMuffle();
			return;
		}
		if (_muffles.Count == 0)
		{
			foreach (string bus in MuffledBuses)
			{
				int index = AudioServer.GetBusIndex(bus);
				if (index < 0)
					continue;
				AudioEffectLowPassFilter filter = new();
				AudioServer.AddBusEffect(index, filter);
				_muffles.Add(filter);
			}
		}
		// Balayage exponentiel : l'oreille entend les octaves, pas les hertz.
		float cutoff = MuffleOpenHz * Mathf.Pow(MuffleClosedHz / MuffleOpenHz, Mathf.Clamp(amount, 0f, 1f));
		foreach (AudioEffectLowPassFilter filter in _muffles)
			filter.CutoffHz = cutoff;
	}

	private void RemoveWorldMuffle()
	{
		if (_muffles.Count == 0)
			return;
		foreach (string bus in MuffledBuses)
		{
			int index = AudioServer.GetBusIndex(bus);
			for (int i = index < 0 ? -1 : AudioServer.GetBusEffectCount(index) - 1; i >= 0; i--)
			{
				if (AudioServer.GetBusEffect(index, i) is AudioEffectLowPassFilter filter && _muffles.Contains(filter))
					AudioServer.RemoveBusEffect(index, i);
			}
		}
		_muffles.Clear();
	}

	private static void EnsureAudioBuses()
	{
		EnsureBus(BusMusic,    -6f);
		EnsureBus(BusSfx,      0f);
		EnsureBus(BusAmbiance, -8f);
	}

	private static void EnsureBus(string name, float defaultDb)
	{
		if (AudioServer.GetBusIndex(name) >= 0)
			return;
		AudioServer.AddBus();
		int idx = AudioServer.BusCount - 1;
		AudioServer.SetBusName(idx, name);
		AudioServer.SetBusVolumeDb(idx, defaultDb);
		AudioServer.SetBusSend(idx, "Master");
	}

	// =========================================================
	// SETTINGS (persistence user://)
	// =========================================================

	public void LoadSettings()
	{
		ConfigFile cfg = new();
		if (cfg.Load(SettingsPath) != Error.Ok)
			return;

		SetBusVolumeLinear("Master",   (float)cfg.GetValue("audio", "master",   1.0).AsDouble());
		SetBusVolumeLinear(BusMusic,   (float)cfg.GetValue("audio", "music",    1.0).AsDouble());
		SetBusVolumeLinear(BusSfx,     (float)cfg.GetValue("audio", "sfx",      1.0).AsDouble());
		SetBusVolumeLinear(BusAmbiance,(float)cfg.GetValue("audio", "ambiance", 0.7).AsDouble());
	}

	public void SaveSettings()
	{
		ConfigFile cfg = new();
		cfg.SetValue("audio", "master",   GetBusVolumeLinear("Master"));
		cfg.SetValue("audio", "music",    GetBusVolumeLinear(BusMusic));
		cfg.SetValue("audio", "sfx",      GetBusVolumeLinear(BusSfx));
		cfg.SetValue("audio", "ambiance", GetBusVolumeLinear(BusAmbiance));
		cfg.Save(SettingsPath);
	}

	/// <summary>Retourne le volume d'un bus en linéaire [0..1].</summary>
	public float GetBusVolumeLinear(string busName)
	{
		int idx = AudioServer.GetBusIndex(busName);
		if (idx < 0)
			return 1f;
		return Mathf.DbToLinear(AudioServer.GetBusVolumeDb(idx));
	}

	/// <summary>Définit le volume d'un bus depuis une valeur linéaire [0..1].</summary>
	public void SetBusVolumeLinear(string busName, float linear)
	{
		int idx = AudioServer.GetBusIndex(busName);
		if (idx < 0)
			return;
		float db = linear <= 0.001f ? -80f : Mathf.LinearToDb(linear);
		AudioServer.SetBusVolumeDb(idx, db);
	}

	// =========================================================
	// PRELOADING
	// =========================================================

	private void PreloadStreams()
	{
		float serverRate = AudioServer.GetMixRate();
		foreach (KeyValuePair<string, string> kv in _paths)
		{
			AudioStream stream = GD.Load<AudioStream>(kv.Value);
			if (stream != null)
			{
				if (stream is AudioStreamWav wav)
				{
					wav.LoopMode = AudioStreamWav.LoopModeEnum.Disabled;
					// Diagnostic : un MixRate incorrect cause un pitch aigu (ex: 22050 Hz importé comme 44100 Hz)
					if (wav.MixRate > 0 && wav.MixRate != serverRate)
						GD.PushWarning($"[AudioManager] MixRate mismatch sur '{kv.Key}': WAV={wav.MixRate} Hz, serveur={serverRate} Hz → pitch incorrect probable. Réimporter dans l'éditeur Godot.");
				}
				_streams[kv.Key] = stream;
			}
			else
				GD.PushWarning($"[AudioManager] Stream introuvable : {kv.Value}");
		}
	}

	// =========================================================
	// API PUBLIQUE
	// =========================================================

	/// <summary>
	/// Joue un SFX depuis le pool.
	/// pitchVariance : variation aléatoire de pitch (+/-).
	/// volumeDb      : offset de volume en dB (0 = nominal, négatif = plus silencieux).
	/// </summary>
	/// <summary>Joue un SFX ; <paramref name="basePitch"/> décale la hauteur (chaîne de ramassages, plan 02 J3).</summary>
	public static void Play(string key, float pitchVariance = 0.05f, float volumeDb = 0f, float basePitch = 1f)
	{
		Instance?.PlaySfx(key, pitchVariance, volumeDb, basePitch);
	}

	public void PlaySfx(string key, float pitchVariance = 0.05f, float volumeDb = 0f, float basePitch = 1f)
	{
		if (!_streams.TryGetValue(key, out AudioStream stream))
			return;

		// Throttle: skip if same SFX was played too recently
		ulong now = NowMsec;
		ulong minInterval = _sfxMinIntervals.TryGetValue(key, out ulong interval) ? interval : DefaultMinInterval;
		if (minInterval > 0 && _sfxLastPlayTime.TryGetValue(key, out ulong lastTime) && now - lastTime < minInterval)
		{
			SoundRequested?.Invoke(key, SoundOutcome.Throttled);
			return;
		}
		AudioStreamPlayer player = _sfxPool.Acquire(key, _soundPriorities[key], _soundMaxVoices[key], out bool stolen);
		if (player == null)
		{
			SoundRequested?.Invoke(key, SoundOutcome.VoiceLimited);
			return;
		}
		_sfxLastPlayTime[key] = now;
		SoundRequested?.Invoke(key, stolen ? SoundOutcome.VoiceStolen : SoundOutcome.Played);

		player.Bus = _soundBuses[key];
		player.Stream = stream;
		player.PitchScale = basePitch + _rng.RandfRange(-pitchVariance, pitchVariance);
		player.VolumeDb = volumeDb + _soundVolumes.GetValueOrDefault(key);
		player.Play();
	}

	/// <summary>Joue un SFX UI qui fonctionne même en pause (level-up, coffre, etc.).</summary>
	/// <summary>Joue un son d'interface ; le lecteur rendu permet de l'éteindre avant sa fin (<see cref="FadeOutUI"/>).</summary>
	public static AudioStreamPlayer PlayUI(string key, float pitchVariance = 0f, float volumeDb = 0f) =>
		Instance?.PlayUiSfx(key, pitchVariance, volumeDb);

	public AudioStreamPlayer PlayUiSfx(string key, float pitchVariance = 0f, float volumeDb = 0f)
	{
		if (!_streams.TryGetValue(key, out AudioStream stream))
			return null;

		ulong now = NowMsec;
		if (_uiLastPlayTime.TryGetValue(key, out ulong lastTime) && now - lastTime < _sfxMinIntervals[key])
		{
			SoundRequested?.Invoke(key, SoundOutcome.Throttled);
			return null;
		}
		AudioStreamPlayer player = _uiSfxPool.Acquire(key, _soundPriorities[key], _soundMaxVoices[key], out _);
		if (player == null)
		{
			SoundRequested?.Invoke(key, SoundOutcome.VoiceLimited);
			return null;
		}
		_uiLastPlayTime[key] = now;
		SoundRequested?.Invoke(key, SoundOutcome.Interface);

		player.Bus = BusSfx;
		player.Stream = stream;
		player.PitchScale = 1f + _rng.RandfRange(-pitchVariance, pitchVariance);
		player.VolumeDb = volumeDb + _soundVolumes.GetValueOrDefault(key);
		player.Play();
		return player;
	}

	/// <summary>
	/// Éteint en fondu un son d'interface lancé par <see cref="PlayUI"/>, s'il joue encore ce même son : le lecteur du
	/// pool a pu être repris entre-temps par un autre.
	/// </summary>
	public static void FadeOutUI(AudioStreamPlayer player, string key, float duration)
	{
		if (Instance == null || player == null || !GodotObject.IsInstanceValid(player) || !player.Playing
			|| !Instance._streams.TryGetValue(key, out AudioStream stream) || player.Stream != stream)
			return;
		Instance._uiSfxPool.FadeOut(player, key, duration);
	}

	/// <summary>Joue un SFX en boucle (ex : ambiance UI). Un seul loop SFX à la fois.</summary>
	public static void PlayLoop(string key, float volumeDb = 0f)
	{
		Instance?.PlayLoopingSfx(key, volumeDb);
	}

	public void PlayLoopingSfx(string key, float volumeDb = 0f)
	{
		if (_loopingSfxKey == key && _loopingSfxPlayer.Playing)
			return;

		if (!_streams.TryGetValue(key, out AudioStream stream))
			return;

		_loopingSfxPlayer.Stream = CreateLoopableStream(stream);
		_loopingSfxPlayer.VolumeDb = volumeDb + _soundVolumes.GetValueOrDefault(key);
		_loopingSfxPlayer.Play();
		_loopingSfxKey = key;
	}

	/// <summary>Arrête le SFX en boucle.</summary>
	public static void StopLoop()
	{
		Instance?.StopLoopingSfx();
	}

	public void StopLoopingSfx()
	{
		_loopingSfxPlayer.Stop();
		_loopingSfxKey = "";
	}

	/// <summary>Relit l'intention musicale : à l'ouverture du jeu, aucun changement d'état n'annonce le Hub.</summary>
	public void RefreshMusic() => _music.Refresh();

	// =========================================================
	// AMBIANCE
	// =========================================================

	private void PlayAmbiance(string key)
	{
		if (!_streams.TryGetValue(key, out AudioStream stream))
			return;

		_ambianceFade?.Kill();
		if (_ambianceKey != key || !_ambiancePlayer.Playing)
		{
			_ambiancePlayer.Stream = CreateLoopableStream(stream);
			_ambiancePlayer.Play();
		}
		_ambianceKey = key;
		_ambiancePlayer.VolumeDb = _soundVolumes.GetValueOrDefault(key);
	}

	private void FadeOutAmbiance(float duration = 2f)
	{
		_ambianceFade?.Kill();
		_ambianceFade = CreateTween();
		_ambianceFade.TweenProperty(_ambiancePlayer, "volume_db", -60f, duration);
		_ambianceFade.TweenCallback(Callable.From(_ambiancePlayer.Stop));
	}

	// =========================================================
	// _PROCESS — musique adaptative + oiseaux
	// =========================================================

	public override void _Process(double delta)
	{
		if (string.IsNullOrEmpty(_currentPhase))
			return;
		float dt = (float)delta;

		if (_currentPhase == "Exploration")
		{
			_birdTimer -= dt;
			if (_birdTimer <= 0f)
			{
				_birdTimer = _rng.RandfRange(20f, 45f);
				string birdKey = _rng.Randi() % 2 == 0 ? "sfx_ambiance_oiseaux_1" : "sfx_ambiance_oiseaux_2";
				PlaySfx(birdKey, 0.05f, -4f);
			}
		}

		UpdateContextAmbiance();
		UpdatePlayerWarnings();
	}

	// =========================================================
	// EVENTBUS
	// =========================================================

	private void ConnectEventBus(Core.EventBus eb)
	{
		eb.RunPhaseChanged    += OnRunPhaseChanged;
		eb.CrisisWarning      += OnCrisisWarning;
		eb.CrisisStarted      += OnCrisisStarted;
		eb.CrisisEnded        += OnCrisisEnded;
		eb.EnemySpawned       += OnEnemySpawned;
		eb.EnemyKilled        += OnEnemyKilled;
		eb.PlayerDamaged      += OnPlayerDamaged;
		eb.PlayerHitBy        += OnPlayerHitBy;
		eb.PlayerShieldChanged += OnPlayerShieldChanged;
		eb.SouvenirDiscovered += OnSouvenirDiscovered;
		eb.ZoneDiscovered     += OnZoneDiscovered;
		eb.FragmentChosen     += OnFragmentChosen;
		eb.GameStateChanged   += OnGameStateChanged;
		eb.ChestOpened        += OnChestOpened;
		eb.PoiExplored        += OnPoiExplored;
		eb.WeaponEquipped += OnWeaponEquipped;
		eb.WeaponDropped += OnWeaponDropped;
		eb.RandomEventTriggered += OnRandomEventTriggered;
		eb.RandomEventEnded   += OnRandomEventEnded;
	}

	private void DisconnectEventBus(Core.EventBus eb)
	{
		eb.RunPhaseChanged    -= OnRunPhaseChanged;
		eb.CrisisWarning      -= OnCrisisWarning;
		eb.CrisisStarted      -= OnCrisisStarted;
		eb.CrisisEnded        -= OnCrisisEnded;
		eb.EnemySpawned       -= OnEnemySpawned;
		eb.EnemyKilled        -= OnEnemyKilled;
		eb.PlayerDamaged      -= OnPlayerDamaged;
		eb.PlayerHitBy        -= OnPlayerHitBy;
		eb.PlayerShieldChanged -= OnPlayerShieldChanged;
		eb.SouvenirDiscovered -= OnSouvenirDiscovered;
		eb.ZoneDiscovered     -= OnZoneDiscovered;
		eb.FragmentChosen     -= OnFragmentChosen;
		eb.GameStateChanged   -= OnGameStateChanged;
		eb.ChestOpened        -= OnChestOpened;
		eb.PoiExplored        -= OnPoiExplored;
		eb.WeaponEquipped -= OnWeaponEquipped;
		eb.WeaponDropped -= OnWeaponDropped;
		eb.RandomEventTriggered -= OnRandomEventTriggered;
		eb.RandomEventEnded   -= OnRandomEventEnded;
	}

	private void OnRunPhaseChanged(string oldPhase, string newPhase)
	{
		_currentPhase = newPhase;

		// La musique suit son propre pilotage (MusicDirector) ; la phase ne règle ici que l'ambiance.
		switch (newPhase)
		{
			case "Exploration":
				PlayAmbiance("sfx_ambiance_foret");
				_birdTimer = _rng.RandfRange(5f, 15f);
				break;
			case "Crisis":
			case "LateGame":
				FadeOutAmbiance(2f);
				break;
		}

		UpdateContextAmbiance();
	}

	private void OnCrisisWarning(int crisisNumber, float countdown)
	{
		PlaySfx("sfx_danger_building", 0f, -5f);
	}

	private void OnCrisisStarted(int crisisNumber, int intensity)
	{
		_currentPhase = "Crisis";
		FadeOutAmbiance(2f);
	}

	/// <summary>La fin de crise précède le retour de phase : celle-ci est relue en fin d'image (l'endgame n'en change pas).</summary>
	private void OnCrisisEnded(int crisisNumber) => Callable.From(SyncRunPhase).CallDeferred();

	private void OnEnemySpawned(string enemyId, float hpScale, float dmgScale)
	{
		if (enemyId == EnemyGrammar.FinalBossId)
			PlaySfx("sfx_danger_building", 0f, -4f);
	}

	private void OnEnemyKilled(string enemyId, Vector2 position)
	{
		// Son de dissolution discret — pas sur chaque kill pour éviter la saturation
		if (_rng.Randf() < 0.5f)
			PlaySfx("sfx_monde_dissolution", 0.08f, -6f);
	}

	private float _lastKnownHp = -1f;
	/// <summary>Image de la dernière tranche du Néant : ses PV perdus ne rejouent pas le son d'un coup (plan 27 V3d).</summary>
	private ulong _voidTickFrame = ulong.MaxValue;
	private float _lastKnownShield = -1f;

	/// <summary>Le bouclier qui baisse a encaissé un coup : parade s'il tient, bris s'il tombe à zéro. Sa recharge reste muette.</summary>
	private void OnPlayerShieldChanged(float shield, float maxShield)
	{
		bool absorbed = _lastKnownShield > 0f && shield < _lastKnownShield - 0.01f;
		_lastKnownShield = shield;
		if (absorbed)
			PlaySfx(shield <= 0f ? "sfx_shield_break" : "sfx_shield_block");
	}


	private void OnPlayerHitBy(string enemyId, float damage)
	{
		if (enemyId == "void")
			_voidTickFrame = Engine.GetProcessFrames();
	}

	private void OnPlayerDamaged(float currentHp, float maxHp)
	{
		float damageTaken = _lastKnownHp >= 0f ? _lastKnownHp - currentHp : 0f;
		bool isDamage = damageTaken > 0.01f;
		_lastKnownHp = currentHp;
		// Le Néant consume deux fois par seconde : son de brûlure à produire (plan 15), pas le son d'un coup en boucle.
		if (isDamage && _voidTickFrame == Engine.GetProcessFrames())
		{
			UpdatePlayerWarnings();
			return;
		}
		if (isDamage)
			PlaySfx("sfx_hit_joueur");
		if (damageTaken >= Mathf.Max(12f, maxHp * CriticalDamageThresholdRatio))
			PlaySfx("sfx_degat_critique_recu", 0f, -1f);

		UpdatePlayerWarnings();
	}

	private void OnSouvenirDiscovered(string souvenirId, string souvenirName, string constellationId)
	{
		PlaySfx("sfx_souvenir_trouve", 0f);
	}

	private void OnZoneDiscovered(int cellX, int cellY, int cellCount)
	{
	}

	private void OnFragmentChosen(string fragmentId, string fragmentType)
	{
		PlaySfx("sfx_perk_choix", 0f);
	}

	private void OnChestOpened(string chestId, string rarity, Vector2 position)
	{
		if (rarity == "lore")
			PlaySfx("sfx_artefact_trouve", 0f, -3f);
	}

	private void OnWeaponEquipped(string weaponId, int slotIndex) => PlayUiSfx("sfx_weapon_equip", 0.03f);

	private void OnWeaponDropped(string weaponId) => PlayUiSfx("sfx_weapon_drop", 0.03f);

	private void OnPoiExplored(string poiId, string poiType)
	{
		PlaySfx("sfx_poi_activate", 0.03f);
		if (poiType == "lore" || poiType == "sanctuary")
			PlaySfx("sfx_artefact_trouve", 0f, -3f);
	}

	private void OnRandomEventTriggered(string eventId, string eventName)
	{
		_currentRandomEventId = eventId;
		if (eventId is "resurgence" or "the_call")
			PlaySfx("sfx_danger_building", 0f, -5f);
		UpdateContextAmbiance();
	}

	private void OnRandomEventEnded(string eventId)
	{
		if (_currentRandomEventId == eventId)
			_currentRandomEventId = "";
		UpdateContextAmbiance();
	}

	private void OnGameStateChanged(string oldState, string newState)
	{
		if (newState == "Hub")
		{
			_currentPhase = "";
			_currentRandomEventId = "";
			_ambianceFade?.Kill();
			_ambiancePlayer.Stop();
			_ambianceKey = "";
			_sfxPool.Stop();
			StopLoopingSfx();
			StopLoopOnPlayer(_ambianceOverlayPlayer, ref _ambianceOverlayKey);
			StopLoopOnPlayer(_lowHealthLoopPlayer, ref _lowHealthLoopKey);
			StopLoopOnPlayer(_borderWarningPlayer, ref _borderWarningKey);
		}
		else if (newState == "Run")
		{
			_lastKnownHp = -1f;
			_lastKnownShield = -1f;
			_voidTickFrame = ulong.MaxValue;
			_currentPhase = "";
			_currentRandomEventId = "";
			// La première run d'une session commence déjà en exploration : aucun changement de phase ne
			// l'annonce. La phase réelle est relue en fin d'image, après celui qu'émet un retour de mort.
			Callable.From(SyncRunPhase).CallDeferred();
		}
	}

	private void SyncRunPhase()
	{
		GameManager manager = GetNode<GameManager>("/root/GameManager");
		if (manager.CurrentState != GameManager.GameState.Run)
			return;
		string phase = manager.CurrentRunPhase.ToString();
		if (phase != _currentPhase)
			OnRunPhaseChanged(_currentPhase, phase);
	}

	private void UpdateContextAmbiance()
	{
		string targetKey = ResolveContextAmbianceKey();
		if (string.IsNullOrEmpty(targetKey))
		{
			StopLoopOnPlayer(_ambianceOverlayPlayer, ref _ambianceOverlayKey);
			return;
		}

		float volumeDb = targetKey switch
		{
			"sfx_orage_proche" => -6f,
			"sfx_pluie_legere" => -9f,
			"sfx_foret_rafales" => -8f,
			"sfx_brouillard" => -12f,
			"sfx_tonnerre_lointain" => -11f,
			_ => -10f
		};

		PlayLoopOnPlayer(_ambianceOverlayPlayer, ref _ambianceOverlayKey, targetKey, volumeDb);
	}

	private string ResolveContextAmbianceKey()
	{
		return _currentRandomEventId switch
		{
			"thick_fog" => "sfx_brouillard",
			"storm" => "sfx_orage_proche",
			"ash_rain" => "sfx_pluie_legere",
			"forgotten_wind" => "sfx_foret_rafales",
			_ when _currentPhase is "Crisis" or "LateGame" => "sfx_tonnerre_lointain",
			_ => ""
		};
	}

	private void UpdatePlayerWarnings()
	{
		CachePlayer();
		CacheErasureManager();
		CacheWorldBounds();

		if (_player == null || !GodotObject.IsInstanceValid(_player) || _player.IsDead)
		{
			StopLoopOnPlayer(_lowHealthLoopPlayer, ref _lowHealthLoopKey);
			StopLoopOnPlayer(_borderWarningPlayer, ref _borderWarningKey);
			return;
		}

		float maxHp = Mathf.Max(1f, _player.EffectiveMaxHp);
		float hpRatio = _player.CurrentHp / maxHp;
		if (hpRatio <= LowHealthStartThreshold)
			PlayLoopOnPlayer(_lowHealthLoopPlayer, ref _lowHealthLoopKey, "sfx_sante_basse", -10f);
		else if (hpRatio >= LowHealthStopThreshold)
			StopLoopOnPlayer(_lowHealthLoopPlayer, ref _lowHealthLoopKey);

		if (_erasureManager == null || !GodotObject.IsInstanceValid(_erasureManager))
		{
			StopLoopOnPlayer(_borderWarningPlayer, ref _borderWarningKey);
			return;
		}

		float memory = _erasureManager.GetMemoryAt(_player.GlobalPosition);
		bool nearErasureBorder = memory <= BorderEffacementThreshold || IsNearMapBorder(_player.GlobalPosition);
		if (nearErasureBorder)
			PlayLoopOnPlayer(_borderWarningPlayer, ref _borderWarningKey, "sfx_bord_effacement_proche", -11f);
		else
			StopLoopOnPlayer(_borderWarningPlayer, ref _borderWarningKey);
	}

	private void CachePlayer()
	{
		if (_player != null && GodotObject.IsInstanceValid(_player))
			return;
		_player = GetTree().GetFirstNodeInGroup("player") as Core.Player;
	}

	private void CacheErasureManager()
	{
		if (_erasureManager != null && GodotObject.IsInstanceValid(_erasureManager))
			return;
		_erasureManager = GetNodeOrNull<World.ErasureManager>("/root/Main/ErasureManager");
	}

	private void CacheWorldBounds()
	{
		if (_worldSetup != null && GodotObject.IsInstanceValid(_worldSetup) && _ground != null && GodotObject.IsInstanceValid(_ground))
			return;

		_worldSetup = GetNodeOrNull<World.WorldSetup>("/root/Main");
		_ground = _worldSetup?.GetNodeOrNull<TileMapLayer>("Ground");
	}

	private bool IsNearMapBorder(Vector2 worldPos)
	{
		if (_worldSetup?.Generator == null || _ground == null)
			return false;

		Vector2I cell = _ground.LocalToMap(_ground.ToLocal(worldPos));
		float dist = _worldSetup.Generator.EllipseDistance(cell.X, cell.Y);
		float mapRadius = _worldSetup.Generator.MapRadiusX;
		return dist >= mapRadius - MapBorderWarningCells;
	}

	private void PlayLoopOnPlayer(AudioStreamPlayer player, ref string currentKey, string key, float volumeDb)
	{
		if (currentKey == key && player.Playing)
			return;

		if (!_streams.TryGetValue(key, out AudioStream stream))
			return;

		player.Stream = CreateLoopableStream(stream);
		player.VolumeDb = volumeDb + _soundVolumes.GetValueOrDefault(key);
		player.Play();
		currentKey = key;
	}

	private void StopLoopOnPlayer(AudioStreamPlayer player, ref string currentKey)
	{
		if (player.Playing)
			player.Stop();
		currentKey = "";
	}

	private static AudioStream CreateLoopableStream(AudioStream stream)
	{
		if (stream is not AudioStreamWav wav)
			return stream;

		AudioStreamWav clone = (AudioStreamWav)wav.Duplicate();
		clone.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
		clone.LoopBegin = 0;
		int bytesPerFrame = clone.Format switch
		{
			AudioStreamWav.FormatEnum.Format8Bits => clone.Stereo ? 2 : 1,
			AudioStreamWav.FormatEnum.Format16Bits => clone.Stereo ? 4 : 2,
			_ => clone.Stereo ? 4 : 2
		};
		if (clone.Data != null && clone.Data.Length > 0)
			clone.LoopEnd = clone.Data.Length / bytesPerFrame;

		return clone;
	}
}
