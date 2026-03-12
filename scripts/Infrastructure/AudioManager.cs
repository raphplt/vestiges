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
	private readonly List<AudioStreamPlayer> _uiSfxPool = new();
	private const int UiSfxPoolSize = 3;

	// --- Stream cache ---
	private readonly Dictionary<string, AudioStream> _streams = new();

	// --- SFX throttle (prevents spam of the same sound) ---
	private readonly Dictionary<string, ulong> _sfxLastPlayTime = new();
	private static readonly Dictionary<string, ulong> SfxMinIntervals = new()
	{
		["sfx_hit_joueur"] = 150,
		["sfx_hit_ennemi"] = 80,
		["sfx_hit_critique"] = 80,
		["sfx_monde_dissolution"] = 200,
		["sfx_recolte_obtenu"] = 100,
		["xp_gain"] = 60,
		["sfx_structure_impossible"] = 180,
		["sfx_perk_refuse"] = 120,
		["sfx_degat_critique_recu"] = 180,
		["sfx_charognard_meute"] = 3000,
		["sfx_brute_pas"] = 2000,
		["sfx_rampant_deplacement"] = 2000,
		["sfx_sentinelle_activation"] = 5000,
	};
	private const ulong DefaultMinInterval = 0;

	// --- Musique adaptative ---
	private int _activeEnemyCount;
	private const int CombatThreshold = 3;
	private string _currentPhase = "";
	private float _nightTimer;
	private const float NightChaosDelay = 130f;

	// --- Ambiance oiseaux ---
	private float _birdTimer;
	private string _currentRandomEventId = "";
	private int _activeColosseCount;
	private Core.Player _player;
	private World.ZoneMemoryManager _zoneMemoryManager;
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
		["mus_mort"]             = "res://assets/audio/musique/mus_mort.ogg",

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
		["sfx_level_up"]        = "res://assets/audio/sfx/gameplay/level_up.wav",
		["sfx_level_up_loop"]   = "res://assets/audio/sfx/gameplay/level_up_loop.wav",
		["sfx_level_up_after"]  = "res://assets/audio/sfx/gameplay/level_up_after.wav",
		["sfx_chest_opening"]   = "res://assets/audio/sfx/gameplay/chest_opening.wav",
		["xp_gain"]            = "res://assets/audio/sfx/gameplay/xp_gain.wav",
		["sfx_perk_refuse"]     = "res://assets/audio/sfx/gameplay/sfx_perk_refuse.wav",
		["sfx_malediction_acceptee"] = "res://assets/audio/sfx/gameplay/sfx_malediction_acceptee.wav",
		["sfx_artefact_trouve"] = "res://assets/audio/sfx/gameplay/sfx_artefact_trouve.wav",
		["sfx_danger_building"] = "res://assets/audio/sfx/gameplay/sfx_danger_building.wav",
		["sfx_sante_basse"]     = "res://assets/audio/sfx/gameplay/sfx_sante_basse.wav",
		["sfx_structure_impossible"] = "res://assets/audio/sfx/gameplay/sfx_structure_impossible.wav",

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
		["sfx_degat_critique_recu"] = "res://assets/audio/sfx/combat/sfx_degat_critique_recu.wav",

		// UI
		["sfx_menu_clic"]         = "res://assets/audio/sfx/ui/sfx_menu_clic.wav",
		["sfx_menu_survol"]       = "res://assets/audio/sfx/ui/sfx_menu_survol.wav",
		["sfx_menu_confirmer"]    = "res://assets/audio/sfx/ui/sfx_menu_confirmer.wav",
		["sfx_inventaire_ouvrir"] = "res://assets/audio/sfx/ui/sfx_inventaire_ouvrir.wav",
		["sfx_inventaire_fermer"] = "res://assets/audio/sfx/ui/sfx_inventaire_fermer.wav",

		// Ambiance biomes
		["sfx_ambiance_foret"]     = "res://assets/audio/sfx/ambiance/sfx_ambiance_foret.wav",
		["sfx_ambiance_ruines"]    = "res://assets/audio/sfx/ambiance/sfx_ambiance_ruines.wav",
		["sfx_ambiance_marecages"] = "res://assets/audio/sfx/ambiance/sfx_ambiance_marecages.wav",
		["sfx_ambiance_oiseaux_1"] = "res://assets/audio/sfx/ambiance/sfx_ambiance_oiseaux_1.wav",
		["sfx_ambiance_oiseaux_2"] = "res://assets/audio/sfx/ambiance/sfx_ambiance_oiseaux_2.wav",
		["sfx_ambiance_bulle"]     = "res://assets/audio/sfx/ambiance/sfx_ambiance_bulle.wav",
		["sfx_tonnerre_lointain"] = "res://assets/audio/sfx/ambiance/sfx_tonnerre_lointain.wav",
		["sfx_pluie_legere"]      = "res://assets/audio/sfx/ambiance/sfx_pluie_legere.wav",
		["sfx_orage_proche"]      = "res://assets/audio/sfx/ambiance/sfx_orage_proche.wav",
		["sfx_foret_rafales"]     = "res://assets/audio/sfx/ambiance/sfx_foret_rafales.wav",
		["sfx_brouillard"]        = "res://assets/audio/sfx/ambiance/sfx_brouillard.wav",
		["sfx_bord_effacement_proche"] = "res://assets/audio/sfx/ambiance/sfx_bord_effacement_proche.wav",
		["sfx_colosse_lointain"]  = "res://assets/audio/sfx/ambiance/sfx_colosse_lointain.wav",

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
		_ambianceOverlayPlayer = new AudioStreamPlayer { Bus = BusAmbiance, Name = "AmbianceOverlay", VolumeDb = -10f };
		AddChild(_ambianceOverlayPlayer);

		_loopingSfxPlayer = new AudioStreamPlayer { Bus = BusSfx, Name = "LoopingSfx", ProcessMode = ProcessModeEnum.Always };
		AddChild(_loopingSfxPlayer);
		_lowHealthLoopPlayer = new AudioStreamPlayer { Bus = BusSfx, Name = "LowHealthLoop", ProcessMode = ProcessModeEnum.Always, VolumeDb = -10f };
		AddChild(_lowHealthLoopPlayer);
		_borderWarningPlayer = new AudioStreamPlayer { Bus = BusSfx, Name = "BorderWarningLoop", ProcessMode = ProcessModeEnum.Always, VolumeDb = -12f };
		AddChild(_borderWarningPlayer);

		for (int i = 0; i < UiSfxPoolSize; i++)
		{
			AudioStreamPlayer uiP = new() { Bus = BusSfx, Name = $"UiSfx{i}", ProcessMode = ProcessModeEnum.Always };
			_uiSfxPool.Add(uiP);
			AddChild(uiP);
		}

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
		float serverRate = AudioServer.GetMixRate();
		foreach (KeyValuePair<string, string> kv in Paths)
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
	public static void Play(string key, float pitchVariance = 0.05f, float volumeDb = 0f)
	{
		Instance?.PlaySfx(key, pitchVariance, volumeDb);
	}

	public void PlaySfx(string key, float pitchVariance = 0.05f, float volumeDb = 0f)
	{
		if (!_streams.TryGetValue(key, out AudioStream stream))
			return;

		// Throttle: skip if same SFX was played too recently
		ulong now = Time.GetTicksMsec();
		ulong minInterval = SfxMinIntervals.TryGetValue(key, out ulong interval) ? interval : DefaultMinInterval;
		if (minInterval > 0 && _sfxLastPlayTime.TryGetValue(key, out ulong lastTime) && now - lastTime < minInterval)
			return;
		_sfxLastPlayTime[key] = now;

		AudioStreamPlayer player = GetFreePoolPlayer();
		if (player == null)
			return;


		player.Stream = stream;
		player.PitchScale = 1f + (float)GD.RandRange(-pitchVariance, pitchVariance);
		player.VolumeDb = volumeDb;
		player.Play();
	}

	/// <summary>Joue un SFX UI qui fonctionne même en pause (level-up, coffre, etc.).</summary>
	public static void PlayUI(string key, float pitchVariance = 0f, float volumeDb = 0f)
	{
		Instance?.PlayUiSfx(key, pitchVariance, volumeDb);
	}

	public void PlayUiSfx(string key, float pitchVariance = 0f, float volumeDb = 0f)
	{
		if (!_streams.TryGetValue(key, out AudioStream stream))
			return;

		AudioStreamPlayer player = GetFreeUiPoolPlayer();
		if (player == null)
			return;

		player.Stream = stream;
		player.PitchScale = 1f + (float)GD.RandRange(-pitchVariance, pitchVariance);
		player.VolumeDb = volumeDb;
		player.Play();
	}

	private AudioStreamPlayer GetFreeUiPoolPlayer()
	{
		foreach (AudioStreamPlayer p in _uiSfxPool)
		{
			if (!p.Playing)
				return p;
		}
		return _uiSfxPool.Count > 0 ? _uiSfxPool[0] : null;
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

		AudioStreamWav clone = null;
		if (stream is AudioStreamWav wav)
		{
			clone = (AudioStreamWav)wav.Duplicate();
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
		}

		_loopingSfxPlayer.Stream = clone ?? stream;
		_loopingSfxPlayer.VolumeDb = volumeDb;
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

		UpdateContextAmbiance();
		UpdatePlayerWarnings();
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
		eb.SouvenirDiscovered += OnSouvenirDiscovered;
		eb.ZoneDiscovered     += OnZoneDiscovered;
		eb.PerkChosen         += OnPerkChosen;
		eb.FragmentChosen     += OnFragmentChosen;
		eb.GameStateChanged   += OnGameStateChanged;
		eb.ChestOpened        += OnChestOpened;
		eb.PoiExplored        += OnPoiExplored;
		eb.RandomEventTriggered += OnRandomEventTriggered;
		eb.RandomEventEnded   += OnRandomEventEnded;
	}

	private void DisconnectEventBus(Core.EventBus eb)
	{
		eb.DayPhaseChanged    -= OnDayPhaseChanged;
		eb.EnemySpawned       -= OnEnemySpawned;
		eb.EnemyKilled        -= OnEnemyKilled;
		eb.PlayerDamaged      -= OnPlayerDamaged;
		eb.SouvenirDiscovered -= OnSouvenirDiscovered;
		eb.ZoneDiscovered     -= OnZoneDiscovered;
		eb.PerkChosen         -= OnPerkChosen;
		eb.FragmentChosen     -= OnFragmentChosen;
		eb.GameStateChanged   -= OnGameStateChanged;
		eb.ChestOpened        -= OnChestOpened;
		eb.PoiExplored        -= OnPoiExplored;
		eb.RandomEventTriggered -= OnRandomEventTriggered;
		eb.RandomEventEnded   -= OnRandomEventEnded;
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

		UpdateContextAmbiance();
	}

	private void OnEnemySpawned(string enemyId, float hpScale, float dmgScale)
	{
		_activeEnemyCount++;
		if (enemyId.StartsWith("colosse_"))
			_activeColosseCount++;
		if (enemyId.StartsWith("colosse_") || enemyId == "indicible")
			PlaySfx("sfx_danger_building", 0f, -4f);
	}

	private void OnEnemyKilled(string enemyId, Vector2 position)
	{
		_activeEnemyCount = Mathf.Max(0, _activeEnemyCount - 1);
		if (enemyId.StartsWith("colosse_"))
			_activeColosseCount = Mathf.Max(0, _activeColosseCount - 1);
		// Son de dissolution discret — pas sur chaque kill pour éviter la saturation
		if (GD.Randf() < 0.5f)
			PlaySfx("sfx_monde_dissolution", 0.08f, -6f);
	}

	private float _lastKnownHp = -1f;

	private void OnPlayerDamaged(float currentHp, float maxHp)
	{
		float damageTaken = _lastKnownHp >= 0f ? _lastKnownHp - currentHp : 0f;
		bool isDamage = damageTaken > 0.01f;
		_lastKnownHp = currentHp;
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

	private void OnPerkChosen(string perkId)
	{
		PlaySfx("sfx_perk_choix", 0f);
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

	private void OnPoiExplored(string poiId, string poiType)
	{
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
			// Retour au hub : réinitialise et joue la musique hub
			_currentMusicKey = "";
			_activeEnemyCount = 0;
			_currentPhase = "";
			_currentRandomEventId = "";
			_activeColosseCount = 0;
			StopLoopOnPlayer(_ambianceOverlayPlayer, ref _ambianceOverlayKey);
			StopLoopOnPlayer(_lowHealthLoopPlayer, ref _lowHealthLoopKey);
			StopLoopOnPlayer(_borderWarningPlayer, ref _borderWarningKey);
			PlayHubMusic();
		}
		else if (newState == "Run")
		{
			// La musique démarre via OnDayPhaseChanged (DayNightCycle._Ready émet "Day")
			// Ne pas toucher _currentMusicKey ici pour ne pas interrompre la transition
			_activeEnemyCount = 0;
		}
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
			"sfx_colosse_lointain" => -5f,
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
		if (_activeColosseCount > 0)
			return "sfx_colosse_lointain";

		return _currentRandomEventId switch
		{
			"thick_fog" => "sfx_brouillard",
			"storm" => "sfx_orage_proche",
			"ash_rain" => "sfx_pluie_legere",
			"forgotten_wind" => "sfx_foret_rafales",
			_ when _currentPhase is "Dusk" or "Night" => "sfx_tonnerre_lointain",
			_ => ""
		};
	}

	private void UpdatePlayerWarnings()
	{
		CachePlayer();
		CacheZoneMemoryManager();
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

		if (_zoneMemoryManager == null || !GodotObject.IsInstanceValid(_zoneMemoryManager))
		{
			StopLoopOnPlayer(_borderWarningPlayer, ref _borderWarningKey);
			return;
		}

		float memory = _zoneMemoryManager.GetMemoryAt(_player.GlobalPosition);
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

	private void CacheZoneMemoryManager()
	{
		if (_zoneMemoryManager != null && GodotObject.IsInstanceValid(_zoneMemoryManager))
			return;
		_zoneMemoryManager = GetNodeOrNull<World.ZoneMemoryManager>("/root/Main/ZoneMemoryManager");
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
		float dist = Mathf.Sqrt(cell.X * cell.X + cell.Y * cell.Y);
		float mapRadius = _worldSetup.Generator.MapRadius;
		return dist >= mapRadius - MapBorderWarningCells;
	}

	private void PlayLoopOnPlayer(AudioStreamPlayer player, ref string currentKey, string key, float volumeDb)
	{
		if (currentKey == key && player.Playing)
			return;

		if (!_streams.TryGetValue(key, out AudioStream stream))
			return;

		player.Stream = CreateLoopableStream(stream);
		player.VolumeDb = volumeDb;
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
