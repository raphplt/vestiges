using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Gestionnaire audio central — Autoload singleton.
/// Gère la musique adaptative (jour/nuit/hub) et tous les SFX du jeu
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

	// --- Music players (cross-fade A/B) ---
	private AudioStreamPlayer _musicPlayerA;
	private AudioStreamPlayer _musicPlayerB;
	private bool _usingA = true;
	private string _currentMusicKey = "";
	private Tween _musicFadeTween;

	// --- SFX pool ---
	private readonly List<AudioStreamPlayer> _sfxPool = new();
	private const int SfxPoolSize = 12;

	// --- Ambiance player ---
	private AudioStreamPlayer _ambiancePlayer;

	// --- Stream cache ---
	private readonly Dictionary<string, AudioStream> _streams = new();

	// --- Musique adaptative ---
	private int _activeEnemyCount;
	private const int CombatThreshold = 3;
	private string _currentPhase = "";
	private float _nightTimer;
	private const float NightChaosDelay = 130f;

	// --- Ambiance oiseaux ---
	private float _birdTimer;

	// --- Debug ---
	private float _debugTimer;
	private const float DebugInterval = 3f;

	// --- Persistence ---
	private const string SettingsPath = "user://audio_settings.cfg";

	// --- Chemins de tous les streams ---
	private static readonly Dictionary<string, string> Paths = new()
	{
		// Musique
		["mus_jour_exploration"] = "res://assets/audio/musique/mus_jour_exploration.ogg",
		["mus_jour_combat"]      = "res://assets/audio/musique/mus_jour_combat.ogg",
		["mus_crepuscule"]       = "res://assets/audio/musique/mus_crepuscule.ogg",
		["mus_nuit_vagues"]      = "res://assets/audio/musique/mus_nuit_vagues.ogg",
		["mus_nuit_chaos"]       = "res://assets/audio/musique/mus_nuit_chaos.ogg",
		["mus_aube"]             = "res://assets/audio/musique/mus_aube.ogg",
		["mus_hub"]              = "res://assets/audio/musique/mus_hub.ogg",
		["mus_mort"]             = "res://assets/audio/musique/mus_mort..ogg",

		// Pas du joueur
		["sfx_pas_herbe"]   = "res://assets/audio/sfx/joueur/pas/sfx_pas_herbe.wav",
		["sfx_pas_eau"]     = "res://assets/audio/sfx/joueur/pas/sfx_pas_eau.wav",
		["sfx_pas_beton"]   = "res://assets/audio/sfx/joueur/pas/sfx_pas_beton.wav",
		["sfx_pas_bois"]    = "res://assets/audio/sfx/joueur/pas/sfx_pas_bois.wav",
		["sfx_pas_gravier"] = "res://assets/audio/sfx/joueur/pas/sfx_pas_gravier.wav",

		// Récolte
		["sfx_recolte_hache"]    = "res://assets/audio/sfx/gameplay/sfx_recolte_hache.wav",
		["sfx_recolte_pioche"]   = "res://assets/audio/sfx/gameplay/sfx_recolte_pioche.wav",
		["sfx_recolte_obtenu"]   = "res://assets/audio/sfx/gameplay/sfx_recolte_obtenu.wav",

		// Craft & construction
		["sfx_craft_termine"]    = "res://assets/audio/sfx/gameplay/sfx_craft_termine.wav",
		["sfx_structure_pose"]   = "res://assets/audio/sfx/gameplay/sfx_structure_pose.wav",
		["sfx_craft_impossible"] = "res://assets/audio/sfx/gameplay/sfx_craft_impossible.wav",

		// Progression
		["sfx_perk_choix"]      = "res://assets/audio/sfx/gameplay/sfx_perk_choix.wav",
		["sfx_souvenir_trouve"] = "res://assets/audio/sfx/gameplay/sfx_souvenir_trouve.wav",

		// Monde
		["sfx_monde_tuile_apparait"] = "res://assets/audio/sfx/gameplay/sfx_monde_tuile_apparait.wav",
		["sfx_monde_dissolution"]    = "res://assets/audio/sfx/gameplay/sfx_monde_dissolution.wav",
		["sfx_monde_bord_map"]       = "res://assets/audio/sfx/gameplay/sfx_monde_bord_map.wav",
		["sfx_monde_crepuscule"]     = "res://assets/audio/sfx/gameplay/sfx_monde_crepuscule.wav",
		["sfx_monde_aube"]           = "res://assets/audio/sfx/gameplay/sfx_monde_aube.wav",

		// Combat
		["sfx_hit_ennemi"]   = "res://assets/audio/sfx/combat/sfx_hit_ennemi.wav",
		["sfx_hit_critique"] = "res://assets/audio/sfx/combat/sfx_hit_critique.wav",
		["sfx_hit_joueur"]   = "res://assets/audio/sfx/combat/sfx_hit_joueur.wav",

		// Ambiance biomes
		["sfx_ambiance_foret"]     = "res://assets/audio/sfx/ambiance/sfx_ambiance_foret.wav",
		["sfx_ambiance_ruines"]    = "res://assets/audio/sfx/ambiance/sfx_ambiance_ruines.wav",
		["sfx_ambiance_marecages"] = "res://assets/audio/sfx/ambiance/sfx_ambiance_marecages.wav",
		["sfx_ambiance_oiseaux_1"] = "res://assets/audio/sfx/ambiance/sfx_ambiance_oiseaux_1.wav",
		["sfx_ambiance_oiseaux_2"] = "res://assets/audio/sfx/ambiance/sfx_ambiance_oiseaux_2.wav",
		["sfx_ambiance_bulle"]     = "res://assets/audio/sfx/ambiance/sfx_ambiance_bulle.wav",

		// Foyer
		["sfx_foyer_crepitement"] = "res://assets/audio/sfx/foyer/sfx_foyer_crepitement.wav",
		["sfx_foyer_aura"]        = "res://assets/audio/sfx/foyer/sfx_foyer_aura.wav",
		["sfx_foyer_upgrade"]     = "res://assets/audio/sfx/foyer/sfx_foyer_upgrade.wav",

		// Créatures
		["sfx_rodeur_idle"]           = "res://assets/audio/sfx/creatures/sfx_rodeur_idle.wav",
		["sfx_rodeur_attaque"]        = "res://assets/audio/sfx/creatures/sfx_rodeur_attaque.wav",
		["sfx_charognard_idle"]       = "res://assets/audio/sfx/creatures/sfx_charognard_idle.wav",
		["sfx_charognard_meute"]      = "res://assets/audio/sfx/creatures/sfx_charognard_meute.wav",
		["sfx_sentinelle_activation"] = "res://assets/audio/sfx/creatures/sfx_sentinelle_activation.wav",
		["sfx_sentinelle_tir"]        = "res://assets/audio/sfx/creatures/sfx_sentinelle_tir.wav",
		["sfx_ombre_idle"]            = "res://assets/audio/sfx/creatures/sfx_ombre_idle.wav",
		["sfx_ombre_attaque"]         = "res://assets/audio/sfx/creatures/sfx_ombre_attaque.wav",
		["sfx_brute_pas"]             = "res://assets/audio/sfx/creatures/sfx_brute_pas.wav",
		["sfx_brute_charge"]          = "res://assets/audio/sfx/creatures/sfx_brute_charge.wav",
		["sfx_tisseuse_idle"]         = "res://assets/audio/sfx/creatures/sfx_tisseuse_idle.wav",
		["sfx_hurleur_cri"]           = "res://assets/audio/sfx/creatures/sfx_hurleur_cri.wav",
		["sfx_rampant_deplacement"]   = "res://assets/audio/sfx/creatures/sfx_rampant_deplacement.wav",
		["sfx_rampant_surgissement"]  = "res://assets/audio/sfx/creatures/sfx_rampant_surgissement.wav",
		["sfx_indicible_presence"]    = "res://assets/audio/sfx/creatures/sfx_indicible_presence.wav",
	};

	public override void _Ready()
	{
		Instance = this;

		EnsureAudioBuses();
		LoadSettings();
		PreloadStreams();

		_musicPlayerA = new AudioStreamPlayer { Bus = BusMusic, Name = "MusicA", VolumeDb = -80f };
		_musicPlayerB = new AudioStreamPlayer { Bus = BusMusic, Name = "MusicB", VolumeDb = -80f };
		AddChild(_musicPlayerA);
		AddChild(_musicPlayerB);

		_ambiancePlayer = new AudioStreamPlayer { Bus = BusAmbiance, Name = "Ambiance" };
		AddChild(_ambiancePlayer);

		for (int i = 0; i < SfxPoolSize; i++)
		{
			AudioStreamPlayer p = new() { Bus = BusSfx, Name = $"Sfx{i}" };
			_sfxPool.Add(p);
			AddChild(p);
		}

		Core.EventBus eventBus = GetNodeOrNull<Core.EventBus>("/root/EventBus");
		if (eventBus != null)
			ConnectEventBus(eventBus);

		_birdTimer = (float)GD.RandRange(15.0, 35.0);

		// Diagnostic audio
		for (int i = 0; i < AudioServer.BusCount; i++)
		{
			string busName = AudioServer.GetBusName(i);
			string send = AudioServer.GetBusSend(i);
			float vol = AudioServer.GetBusVolumeDb(i);
			bool muted = AudioServer.IsBusMute(i);
			GD.Print($"[AudioManager] Bus[{i}] \"{busName}\" → send=\"{send}\" vol={vol:F1}dB mute={muted}");
		}
		GD.Print($"[AudioManager] Ready — {_streams.Count}/{Paths.Count} streams chargés");
	}

	public override void _ExitTree()
	{
		Instance = null;
		Core.EventBus eventBus = GetNodeOrNull<Core.EventBus>("/root/EventBus");
		if (eventBus != null)
			DisconnectEventBus(eventBus);
	}

	// =========================================================
	// BUS MANAGEMENT
	// =========================================================

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
		foreach (KeyValuePair<string, string> kv in Paths)
		{
			AudioStream stream = GD.Load<AudioStream>(kv.Value);
			if (stream != null)
			{
				// Fixer le LoopMode au chargement pour éviter de muter les streams partagés à chaud
				if (stream is AudioStreamWav wav)
					wav.LoopMode = AudioStreamWav.LoopModeEnum.Disabled;
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
	public static void Play(string key, float pitchVariance = 0.05f, float volumeDb = 0f)
	{
		Instance?.PlaySfx(key, pitchVariance, volumeDb);
	}

	public void PlaySfx(string key, float pitchVariance = 0.05f, float volumeDb = 0f)
	{
		if (!_streams.TryGetValue(key, out AudioStream stream))
			return;

		AudioStreamPlayer player = GetFreePoolPlayer();
		if (player == null)
		{
			GD.PrintErr($"[Audio t={Time.GetTicksMsec()}ms] SFX POOL FULL — cannot play \"{key}\". All {SfxPoolSize} slots busy.");
			return;
		}

		player.Stream = stream;
		player.PitchScale = 1f + (float)GD.RandRange(-pitchVariance, pitchVariance);
		player.VolumeDb = volumeDb;
		player.Play();
		GD.Print($"[Audio t={Time.GetTicksMsec()}ms] SFX play \"{key}\" on {player.Name} (vol={volumeDb:F1}dB)");
	}

	/// <summary>Démarre la musique du Hub.</summary>
	public void PlayHubMusic()
	{
		PlayMusic("mus_hub", loop: true, fadeDuration: 2f);
	}

	/// <summary>Joue le stinger de mort et coupe la musique.</summary>
	public void PlayDeathStinger()
	{
		FadeOutMusic(0.4f);
		FadeOutAmbiance(0.3f);
		PlaySfx("mus_mort", 0f);
	}

	// =========================================================
	// MUSIQUE
	// =========================================================

	private void PlayMusic(string key, float fadeDuration = 2.5f, bool loop = true)
	{
		if (_currentMusicKey == key)
			return;

		if (!_streams.TryGetValue(key, out AudioStream stream))
		{
			GD.PushWarning($"[AudioManager] Musique introuvable : {key}");
			return;
		}

		GD.Print($"[Audio t={Time.GetTicksMsec()}ms] MUSIC switch \"{_currentMusicKey}\" → \"{key}\" (loop={loop}, fade={fadeDuration:F1}s)");
		_currentMusicKey = key;

		AudioStreamPlayer incoming = _usingA ? _musicPlayerA : _musicPlayerB;
		AudioStreamPlayer outgoing = _usingA ? _musicPlayerB : _musicPlayerA;
		_usingA = !_usingA;

		if (stream is AudioStreamOggVorbis ogg)
			ogg.Loop = loop;

		incoming.Stream = stream;
		incoming.VolumeDb = -80f;
		incoming.Play();

		_musicFadeTween?.Kill();
		_musicFadeTween = CreateTween().SetParallel();
		_musicFadeTween.TweenProperty(incoming, "volume_db", 0f, fadeDuration)
			.SetTrans(Tween.TransitionType.Sine);
		_musicFadeTween.TweenProperty(outgoing, "volume_db", -80f, fadeDuration)
			.SetTrans(Tween.TransitionType.Sine);
	}

	private void FadeOutMusic(float duration = 1.5f)
	{
		_currentMusicKey = "";
		_musicFadeTween?.Kill();
		_musicFadeTween = CreateTween().SetParallel();
		_musicFadeTween.TweenProperty(_musicPlayerA, "volume_db", -80f, duration);
		_musicFadeTween.TweenProperty(_musicPlayerB, "volume_db", -80f, duration);
	}

	// =========================================================
	// AMBIANCE
	// =========================================================

	private void PlayAmbiance(string key)
	{
		if (!_streams.TryGetValue(key, out AudioStream stream))
			return;

		// Dupliquer le stream pour ne pas muter l'objet partagé en cache
		if (stream is AudioStreamWav wav)
		{
			AudioStreamWav clone = (AudioStreamWav)wav.Duplicate();
			clone.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
			stream = clone;
		}

		GD.Print($"[Audio t={Time.GetTicksMsec()}ms] AMBIANCE start \"{key}\" (len={stream.GetLength():F1}s)");
		_ambiancePlayer.Stream = stream;
		_ambiancePlayer.VolumeDb = 0f;
		_ambiancePlayer.Play();
	}

	private void FadeOutAmbiance(float duration = 2f)
	{
		Tween tween = CreateTween();
		tween.TweenProperty(_ambiancePlayer, "volume_db", -80f, duration);
		tween.TweenCallback(Callable.From(_ambiancePlayer.Stop));
	}

	// =========================================================
	// POOL SFX
	// =========================================================

	private AudioStreamPlayer GetFreePoolPlayer()
	{
		foreach (AudioStreamPlayer p in _sfxPool)
		{
			if (!p.Playing)
				return p;
		}
		return _sfxPool.Count > 0 ? _sfxPool[0] : null;
	}

	// =========================================================
	// _PROCESS — musique adaptative + oiseaux
	// =========================================================

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		// --- Debug : dump tous les players actifs toutes les N secondes ---
		_debugTimer += dt;
		if (_debugTimer >= DebugInterval)
		{
			_debugTimer = 0f;
			DumpActivePlayers();
		}

		// Bascule nuit vagues → chaos
		if (_currentPhase == "Night")
		{
			_nightTimer += dt;
			if (_nightTimer >= NightChaosDelay && _currentMusicKey == "mus_nuit_vagues")
				PlayMusic("mus_nuit_chaos", fadeDuration: 5f);
		}

		// Musique adaptative jour (check toutes les 2s ~)
		if (_currentPhase == "Day" && Engine.GetProcessFrames() % 120 == 0)
			RefreshDayMusic();

		// Oiseaux ambiants aléatoires le jour
		if (_currentPhase == "Day")
		{
			_birdTimer -= dt;
			if (_birdTimer <= 0f)
			{
				_birdTimer = (float)GD.RandRange(20.0, 45.0);
				string birdKey = GD.Randi() % 2 == 0 ? "sfx_ambiance_oiseaux_1" : "sfx_ambiance_oiseaux_2";
				PlaySfx(birdKey, 0.05f, -4f);
			}
		}
	}

	private void DumpActivePlayers()
	{
		int activeSfx = 0;
		foreach (AudioStreamPlayer p in _sfxPool)
		{
			if (p.Playing)
			{
				string streamName = p.Stream?.ResourcePath ?? p.Stream?.GetType().Name ?? "null";
				float pos = p.GetPlaybackPosition();
				double len = p.Stream?.GetLength() ?? 0.0;
				bool looping = false;
				if (p.Stream is AudioStreamWav wav)
					looping = wav.LoopMode != AudioStreamWav.LoopModeEnum.Disabled;
				else if (p.Stream is AudioStreamOggVorbis ogg)
					looping = ogg.Loop;
				GD.Print($"[Audio t={Time.GetTicksMsec()}ms] ACTIVE SFX {p.Name}: \"{streamName}\" pos={pos:F1}/{len:F1}s loop={looping}");
				activeSfx++;
			}
		}

		if (_musicPlayerA.Playing)
			GD.Print($"[Audio t={Time.GetTicksMsec()}ms] ACTIVE MusicA: \"{_musicPlayerA.Stream?.ResourcePath}\" pos={_musicPlayerA.GetPlaybackPosition():F1}s vol={_musicPlayerA.VolumeDb:F1}dB");
		if (_musicPlayerB.Playing)
			GD.Print($"[Audio t={Time.GetTicksMsec()}ms] ACTIVE MusicB: \"{_musicPlayerB.Stream?.ResourcePath}\" pos={_musicPlayerB.GetPlaybackPosition():F1}s vol={_musicPlayerB.VolumeDb:F1}dB");
		if (_ambiancePlayer.Playing)
		{
			bool ambLoop = false;
			if (_ambiancePlayer.Stream is AudioStreamWav ambWav)
				ambLoop = ambWav.LoopMode != AudioStreamWav.LoopModeEnum.Disabled;
			GD.Print($"[Audio t={Time.GetTicksMsec()}ms] ACTIVE Ambiance: \"{_ambiancePlayer.Stream?.ResourcePath}\" pos={_ambiancePlayer.GetPlaybackPosition():F1}s loop={ambLoop}");
		}

		if (activeSfx >= SfxPoolSize - 2)
			GD.PrintErr($"[Audio t={Time.GetTicksMsec()}ms] WARNING: SFX pool near saturation ({activeSfx}/{SfxPoolSize} active)");
	}

	private void RefreshDayMusic()
	{
		string target = _activeEnemyCount >= CombatThreshold
			? "mus_jour_combat"
			: "mus_jour_exploration";
		PlayMusic(target, fadeDuration: 3f);
	}

	// =========================================================
	// EVENTBUS
	// =========================================================

	private void ConnectEventBus(Core.EventBus eb)
	{
		eb.DayPhaseChanged    += OnDayPhaseChanged;
		eb.EnemySpawned       += OnEnemySpawned;
		eb.EnemyKilled        += OnEnemyKilled;
		eb.PlayerDamaged      += OnPlayerDamaged;
		eb.CraftCompleted     += OnCraftCompleted;
		eb.StructurePlaced    += OnStructurePlaced;
		eb.SouvenirDiscovered += OnSouvenirDiscovered;
		eb.ZoneDiscovered     += OnZoneDiscovered;
		eb.PerkChosen         += OnPerkChosen;
		eb.FragmentChosen     += OnFragmentChosen;
		eb.ResourceCollected  += OnResourceCollected;
		eb.GameStateChanged   += OnGameStateChanged;
	}

	private void DisconnectEventBus(Core.EventBus eb)
	{
		eb.DayPhaseChanged    -= OnDayPhaseChanged;
		eb.EnemySpawned       -= OnEnemySpawned;
		eb.EnemyKilled        -= OnEnemyKilled;
		eb.PlayerDamaged      -= OnPlayerDamaged;
		eb.CraftCompleted     -= OnCraftCompleted;
		eb.StructurePlaced    -= OnStructurePlaced;
		eb.SouvenirDiscovered -= OnSouvenirDiscovered;
		eb.ZoneDiscovered     -= OnZoneDiscovered;
		eb.PerkChosen         -= OnPerkChosen;
		eb.FragmentChosen     -= OnFragmentChosen;
		eb.ResourceCollected  -= OnResourceCollected;
		eb.GameStateChanged   -= OnGameStateChanged;
	}

	private void OnDayPhaseChanged(string phase)
	{
		_currentPhase = phase;
		_nightTimer = 0f;

		switch (phase)
		{
			case "Day":
				// Démarre la musique de jour — DayPhaseChanged est la source de vérité
				// pour la transition Hub → Run (pas OnGameStateChanged).
				RefreshDayMusic();
				PlayAmbiance("sfx_ambiance_foret");
				_birdTimer = (float)GD.RandRange(5.0, 15.0);
				break;
			case "Dusk":
				PlayMusic("mus_crepuscule", fadeDuration: 4f);
				PlaySfx("sfx_monde_crepuscule", 0f);
				FadeOutAmbiance(3f);
				break;
			case "Night":
				PlayMusic("mus_nuit_vagues", fadeDuration: 3f);
				FadeOutAmbiance(2f);
				break;
			case "Dawn":
				PlayMusic("mus_aube", fadeDuration: 3f, loop: false);
				PlaySfx("sfx_monde_aube", 0f);
				break;
		}
	}

	private void OnEnemySpawned(string enemyId, float hpScale, float dmgScale)
	{
		_activeEnemyCount++;
	}

	private void OnEnemyKilled(string enemyId, Vector2 position)
	{
		_activeEnemyCount = Mathf.Max(0, _activeEnemyCount - 1);
		// Son de dissolution discret — pas sur chaque kill pour éviter la saturation
		if (GD.Randf() < 0.5f)
			PlaySfx("sfx_monde_dissolution", 0.08f, -6f);
	}

	private void OnPlayerDamaged(float currentHp, float maxHp)
	{
		PlaySfx("sfx_hit_joueur");
	}

	private void OnCraftCompleted(string recipeId)
	{
		PlaySfx("sfx_craft_termine");
	}

	private void OnStructurePlaced(string structureId, Vector2 position)
	{
		PlaySfx("sfx_structure_pose");
	}

	private void OnSouvenirDiscovered(string souvenirId, string souvenirName, string constellationId)
	{
		PlaySfx("sfx_souvenir_trouve", 0f);
	}

	private void OnZoneDiscovered(int cellX, int cellY, int cellCount)
	{
		PlaySfx("sfx_monde_tuile_apparait", 0.05f, -3f);
	}

	private void OnPerkChosen(string perkId)
	{
		PlaySfx("sfx_perk_choix", 0f);
	}

	private void OnFragmentChosen(string fragmentId, string fragmentType)
	{
		PlaySfx("sfx_perk_choix", 0f);
	}

	private void OnResourceCollected(string resourceId, int amount)
	{
		PlaySfx("sfx_recolte_obtenu", 0.05f, -2f);
	}

	private void OnGameStateChanged(string oldState, string newState)
	{
		if (newState == "Hub")
		{
			// Retour au hub : réinitialise et joue la musique hub
			_currentMusicKey = "";
			_activeEnemyCount = 0;
			_currentPhase = "";
			PlayHubMusic();
		}
		else if (newState == "Run")
		{
			// La musique démarre via OnDayPhaseChanged (DayNightCycle._Ready émet "Day")
			// Ne pas toucher _currentMusicKey ici pour ne pas interrompre la transition
			_activeEnemyCount = 0;
		}
	}
}
