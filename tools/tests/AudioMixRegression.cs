using System;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>Régressions du mix : priorités, fondus, biomes et arrêt des sons à la fin d'une run.</summary>
public partial class AudioMixRegression : Node
{
    private int _failures;

    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            AudioStreamWav stream = new()
            {
                Format = AudioStreamWav.FormatEnum.Format16Bits,
                MixRate = 48000,
                Data = new byte[48000 * 2 * 5]
            };
            AudioVoicePool pool = new(this, "Probe", 3, ProcessModeEnum.Always);
            AudioStreamPlayer first = Start(pool, stream, "ordinary-a", 10);
            Start(pool, stream, "ordinary-b", 10);
            Start(pool, stream, "warning", 80);
            Check(pool.Acquire("ordinary-c", 10, 2, out _) == null, "pool plein : une voix ordinaire attend");
            AudioStreamPlayer important = pool.Acquire("injury", 80, 1, out bool stolen);
            Check(important != null && stolen, "blessure prioritaire : remplace une voix ordinaire");
            important.Stream = stream;
            important.Play();
            Check(GetNode<AudioStreamPlayer>("Probe2").Playing, "l'alerte existante reste audible");
            Check(pool.Acquire("warning", 80, 1, out _) == null, "limite simultanée : une seule alerte identique");
            pool.Stop();
            first = Start(pool, stream, "reveal", 60);
            pool.FadeOut(first, "reveal", 0.05f);
            Start(pool, stream, "other-a", 80);
            Start(pool, stream, "other-b", 80);
            AudioStreamPlayer replacement = Start(pool, stream, "confirmation", 80);
            Check(replacement == first, "le test réemploie le lecteur en fondu");
            await Frames(12);
            Check(replacement.Playing && Mathf.IsEqualApprox(replacement.VolumeDb, 0f), "ancien fondu annulé au réemploi");
            pool.Stop();

            AudioManager audio = AudioManager.Instance;
            CheckFullLoopDuration();
            AudioStreamPlayer hover = audio.PlayUiSfx("sfx_menu_survol");
            Check(hover != null && audio.PlayUiSfx("sfx_menu_survol") == null, "survol UI limité même hors pause");
            GetTree().Paused = true;
            AudioStreamPlayer confirm = audio.PlayUiSfx("sfx_menu_confirmer");
            Check(confirm != null && confirm.CanProcess(), "confirmation audible pendant la pause");
            GetTree().Paused = false;
            AudioStreamPlayer loop = audio.GetNode<AudioStreamPlayer>("LoopingSfx");
            audio.PlayLoopingSfx("sfx_level_up_loop", -2f);
            Check(loop.Playing && loop.VolumeDb < -40f, "boucle : entrée en fondu, pas d'un coup");
            await Frames(40);
            Check(Mathf.IsEqualApprox(loop.VolumeDb, -8f), "gain de banque appliqué aux boucles au terme du fondu");
            audio.StopLoopingSfx();
            Check(loop.Playing, "boucle : sortie en fondu, pas d'un coup");
            await Frames(45);
            Check(!loop.Playing, "boucle : arrêtée au terme du fondu de sortie");
            await CheckMixBuses(audio);

            await CheckNoteVariants(audio);

            await CheckSelectedEvents(audio);
            await CheckBiomeAmbiance(audio);

            EventBus bus = GetNode<EventBus>("/root/EventBus");
            bus.EmitSignal(EventBus.SignalName.GameStateChanged, "Hub", "Run");
            bus.EmitSignal(EventBus.SignalName.PlayerBiomeChanged, "forest_reclaimed");
            bus.EmitSignal(EventBus.SignalName.RunPhaseChanged, "Crisis", "Exploration");
            Check(audio.GetNode<AudioStreamPlayer>("Ambiance").Playing, "ambiance démarrée en exploration");
            AudioStreamWav forest = (AudioStreamWav)audio.GetNode<AudioStreamPlayer>("Ambiance").Stream;
            Check(forest.LoopEnd == 384000, "forêt : la boucle du lecteur couvre les huit secondes");
            bus.EmitSignal(EventBus.SignalName.RunPhaseChanged, "Exploration", "Crisis");
            bus.EmitSignal(EventBus.SignalName.CrisisStarted, 1, 1);
            AudioStreamWav thunder = (AudioStreamWav)audio.GetNode<AudioStreamPlayer>("AmbianceOverlay").Stream;
            Check(thunder.LoopEnd == 291840, "tonnerre : la boucle contexte couvre les 6,08 secondes");
            bus.EmitSignal(EventBus.SignalName.RunPhaseChanged, "Crisis", "Exploration");
            await Frames(135);
            Check(audio.GetNode<AudioStreamPlayer>("Ambiance").Playing, "retour exploration : ancien fondu annulé");
            bus.EmitSignal(EventBus.SignalName.GameStateChanged, "Run", "Hub");
            await Frames(2);
            Check(!audio.GetNode<AudioStreamPlayer>("Ambiance").Playing
                && !audio.GetNode<AudioStreamPlayer>("AmbianceOverlay").Playing, "Hub : arrêt des ambiances de run");

            await CheckDeathAudio(audio, stream);

            MusicDirector music = new() { Name = "GainProbe" };
            music.Initialize(MusicConfig.Load(), _ => stream, _ => -9f);
            AddChild(music);
            music.Refresh();
            await Frames(135);
            Check(Mathf.IsEqualApprox(music.GetNode<AudioStreamPlayer>("MusicA").VolumeDb, -9f), "gain de banque appliqué au fondu musical");
        }
        catch (Exception exception)
        {
            _failures++;
            GD.PushError(exception.ToString());
        }
        GetTree().Paused = false;
        GD.Print($"[AudioMixRegression] RESULT failures={_failures}");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    /// <summary>Revue de mix (plan 15 X1, X4) : limiteur, monde séparé de l'interface, ducking des grands moments.</summary>
    private async Task CheckMixBuses(AudioManager audio)
    {
        int master = AudioServer.GetBusIndex("Master");
        int world = AudioServer.GetBusIndex("World");
        int music = AudioServer.GetBusIndex("Music");
        Check(HasEffect<AudioEffectHardLimiter>(master), "Master : limiteur de sortie");
        Check(world >= 0 && AudioServer.GetBusSend(world) == "SFX" && HasEffect<AudioEffectCompressor>(world),
            "monde : sous-bus compressé des effets, réglé par le curseur Effets");
        AudioManager.SetWorldMuffle(1f);
        Check(HasEffect<AudioEffectLowPassFilter>(world) && !HasEffect<AudioEffectLowPassFilter>(AudioServer.GetBusIndex("SFX")),
            "étouffement : le monde seulement, l'interface reste nette");
        AudioManager.SetWorldMuffle(0f);
        Check(!HasEffect<AudioEffectLowPassFilter>(world), "étouffement retiré");
        AudioEffectAmplify duck = null;
        for (int i = 0; i < AudioServer.GetBusEffectCount(music); i++)
            duck ??= AudioServer.GetBusEffect(music, i) as AudioEffectAmplify;
        AudioStreamPlayer reveal = audio.PlayUiSfx("sfx_chest_reveal");
        await Frames(15);
        Check(duck != null && Mathf.IsEqualApprox(duck.VolumeDb, -5f), "coffre révélé : musique baissée de 5 dB");
        AudioManager.FadeOutUI(reveal, "sfx_chest_reveal", 0.01f);
        await Frames(250);
        Check(Mathf.IsEqualApprox(duck.VolumeDb, 0f), "ducking : la musique revient après le moment");

        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        AudioVoicePool sfx = (AudioVoicePool)typeof(AudioManager).GetField("_sfxPool", flags).GetValue(audio);
        System.Collections.Generic.Dictionary<string, ulong> times =
            (System.Collections.Generic.Dictionary<string, ulong>)typeof(AudioManager).GetField("_sfxLastPlayTime", flags).GetValue(audio);
        float bank = ((System.Collections.Generic.Dictionary<string, float>)typeof(AudioManager)
            .GetField("_soundVolumes", flags).GetValue(audio))["sfx_impact_blunt"];
        AudioStreamPlayer voice = audio.GetNode<AudioStreamPlayer>("Sfx0");
        System.Collections.Generic.HashSet<float> levels = new();
        bool bounded = true;
        for (int i = 0; i < 8; i++)
        {
            sfx.Stop();
            times.Remove("sfx_impact_blunt");
            audio.PlaySfx("sfx_impact_blunt", 0f);
            bounded &= Mathf.Abs(voice.VolumeDb - bank) <= 1.5f + 0.001f;
            levels.Add(voice.VolumeDb);
        }
        sfx.Stop();
        Check(bounded && levels.Count > 1, "impact répété : volume varié de ±1,5 dB autour de la banque");
    }

    private static bool HasEffect<T>(int bus) where T : AudioEffect
    {
        for (int i = 0; i < AudioServer.GetBusEffectCount(bus); i++)
        {
            if (AudioServer.GetBusEffect(bus, i) is T)
                return true;
        }
        return false;
    }

    private async Task CheckDeathAudio(AudioManager audio, AudioStreamWav stream)
    {
        GameManager manager = GetNode<GameManager>("/root/GameManager");
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        manager.RunSeed = 221092026;
        manager.ChangeState(GameManager.GameState.Run);
        await Frames(2);
        bus.EmitSignal(EventBus.SignalName.PlayerBiomeChanged, "forest_reclaimed");
        bus.EmitSignal(EventBus.SignalName.RandomEventTriggered, "storm", "Orage");
        audio.PlaySfx("sfx_weapon_heavy_hammer", 0f);
        audio.PlaySfx("sfx_enemy_ranged_shot", 0f);
        audio.PlayLoopingSfx("sfx_level_up_loop");
        AudioStreamPlayer runUi = audio.PlayUiSfx("sfx_chest_reveal");
        Check(runUi != null && runUi.Playing && CountWorldVoices(audio) == 5,
            "avant mort : arme, ennemi, forêt, météo et boucle de choix actifs");
        AudioManager.FadeOutUI(runUi, "sfx_chest_reveal", 5f);
        // Des queues longues et deux alertes en cours rendent le contrôle indépendant de la durée des assets.
        foreach (Node child in audio.GetChildren())
        {
            if (child is AudioStreamPlayer player && (player.Playing || player.Name == "LowHealthLoop"
                || player.Name == "BorderWarningLoop"))
            {
                player.Stream = stream;
                player.Play();
            }
        }
        int before = CountWorldVoices(audio);
        manager.ChangeState(GameManager.GameState.Death);
        int atDeath = CountWorldVoices(audio);
        GD.Print($"[AudioMixRegression] DEATH seed={manager.RunSeed} before={before} at_death={atDeath}");
        Check(before == 7 && atDeath == 0 && !runUi.Playing,
            "mort : arrêt immédiat des sept voix de monde et du son UI engagé");

        // Même image, puis bilan en pause : les attaques et événements retardés ne doivent pas relancer le monde.
        audio.PlaySfx("sfx_weapon_sling", 0f);
        audio.PlaySfx("sfx_rodeur_attaque", 0f);
        audio.PlayLoopingSfx("sfx_level_up_loop");
        bus.EmitSignal(EventBus.SignalName.WeaponEquipped, "heavy_hammer", 0);
        bus.EmitSignal(EventBus.SignalName.WeaponDropped, "heavy_hammer");
        Check(!audio.GetNode<AudioStreamPlayer>("UiSfx0").Playing,
            "mort : événements d'équipement tardifs sans son d'interface");
        bus.EmitSignal(EventBus.SignalName.PlayerBiomeChanged, "urban_ruins");
        bus.EmitSignal(EventBus.SignalName.PlayerBiomeChanged, "forest_reclaimed");
        bus.EmitSignal(EventBus.SignalName.CrisisStarted, 2, 2);
        bus.EmitSignal(EventBus.SignalName.RandomEventTriggered, "storm", "Orage");
        bus.EmitSignal(EventBus.SignalName.RunPhaseChanged, "Death", "Exploration");
        audio._Process(30);
        await Frames(2);
        Check(CountWorldVoices(audio) == 0, "mort : demandes et événements tardifs restent muets");
        GetTree().Paused = true;
        audio.PlaySfx("sfx_weapon_makeshift_bow", 0f);
        audio.PlayLoopingSfx("sfx_level_up_loop");
        AudioStreamPlayer deathUi = audio.PlayUiSfx("sfx_monde_dissolution");
        Check(CountWorldVoices(audio) == 0 && deathUi != null && deathUi.Playing && deathUi.CanProcess(),
            "bilan en pause : monde silencieux, audio dédié disponible");
        Check(audio.CurrentMusicIntent == MusicIntent.Death && audio.CurrentMusicKey == "mus_mort",
            "la musique de mort garde son intention");
        GetTree().Paused = false;
        manager.ChangeState(GameManager.GameState.Hub);
        Check(CountWorldVoices(audio) == 0, "retour au Hub : aucune voix de monde résiduelle");
        manager.ChangeState(GameManager.GameState.Run);
        await Frames(2);
        audio.PlaySfx("sfx_weapon_heavy_hammer", 0f);
        bus.EmitSignal(EventBus.SignalName.PlayerBiomeChanged, "forest_reclaimed");
        audio.PlayLoopingSfx("sfx_level_up_loop");
        Check(CountWorldVoices(audio) == 3 && audio.GetNode<AudioStreamPlayer>("Ambiance").Playing,
            "deuxième run : sons et ambiance réactivés sans ancienne météo");
        await Frames(135);
        Check(audio.GetNode<AudioStreamPlayer>("Ambiance").Playing,
            "deuxième run : aucun ancien fondu ne coupe l'ambiance");
        manager.ChangeState(GameManager.GameState.Hub);
    }

    private static int CountWorldVoices(AudioManager audio)
    {
        int playing = 0;
        foreach (Node child in audio.GetChildren())
        {
            if (child is AudioStreamPlayer player && player.Playing
                && !player.Name.ToString().StartsWith("UiSfx", StringComparison.Ordinal))
                playing++;
        }
        return playing;
    }

    private async Task CheckBiomeAmbiance(AudioManager audio)
    {
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        AudioStreamPlayer forest = audio.GetNode<AudioStreamPlayer>("Ambiance");
        bus.EmitSignal(EventBus.SignalName.GameStateChanged, "Hub", "Run");
        bus.EmitSignal(EventBus.SignalName.RunPhaseChanged, "Crisis", "Exploration");
        Check(!forest.Playing, "biome inconnu au départ : aucun fond de forêt");
        foreach (string biome in new[] { "urban_ruins", "wild_fields", "swamp", "collapsed_quarry" })
        {
            bus.EmitSignal(EventBus.SignalName.PlayerBiomeChanged, biome);
            Check(!forest.Playing, $"{biome} : aucun démarrage du fond de forêt");
        }
        bus.EmitSignal(EventBus.SignalName.PlayerBiomeChanged, "forest_reclaimed");
        Check(forest.Playing && forest.VolumeDb < -40f, "entrée en forêt : le fond entre en fondu");
        await Frames(80);
        Check(forest.Playing && Mathf.IsEqualApprox(forest.VolumeDb, -2f), "entrée en forêt : fond complet à −2 dB");
        AudioStream unchanged = forest.Stream;
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        RandomNumberGenerator rng = (RandomNumberGenerator)typeof(AudioManager).GetField("_rng", flags).GetValue(audio);
        ulong state = rng.State;
        bus.EmitSignal(EventBus.SignalName.PlayerBiomeChanged, "forest_reclaimed");
        Check(forest.Stream == unchanged && rng.State == state, "biome identique : ni redémarrage ni nouveau tirage");

        System.Collections.Generic.List<string> birds = new();
        void OnSound(string key, AudioManager.SoundOutcome outcome)
        {
            if (key.StartsWith("sfx_ambiance_oiseaux_", StringComparison.Ordinal))
                birds.Add(key);
        }
        audio.SoundRequested += OnSound;
        try
        {
            System.Reflection.FieldInfo timer = typeof(AudioManager).GetField("_birdTimer", flags);
            timer.SetValue(audio, 0f);
            audio._Process(0.01);
            Check(birds.Count == 1, "forêt : les oiseaux accompagnent le fond");
            birds.Clear();
            foreach (string biome in new[] { "urban_ruins", "wild_fields", "swamp", "collapsed_quarry", "" })
            {
                bus.EmitSignal(EventBus.SignalName.PlayerBiomeChanged, biome);
                timer.SetValue(audio, 0f);
                audio._Process(0.01);
                await Frames(45);
                Check(!forest.Playing && birds.Count == 0, $"sortie vers {biome} : fond éteint et pas de nouveaux oiseaux");
                bus.EmitSignal(EventBus.SignalName.PlayerBiomeChanged, "forest_reclaimed");
                Check(forest.Playing, "retour en forêt : le fond revient");
            }
        }
        finally
        {
            audio.SoundRequested -= OnSound;
        }
        foreach (string biome in new[] { "urban_ruins", "wild_fields", "swamp", "collapsed_quarry" })
        {
            bus.EmitSignal(EventBus.SignalName.PlayerBiomeChanged, biome);
            await Frames(10);
        }
        Check(!forest.Playing, "biomes non forestiers successifs : le fondu de sortie ne repart pas à zéro");
        bus.EmitSignal(EventBus.SignalName.PlayerBiomeChanged, "forest_reclaimed");
        bus.EmitSignal(EventBus.SignalName.PlayerBiomeChanged, "urban_ruins");
        await Frames(8);
        bus.EmitSignal(EventBus.SignalName.PlayerBiomeChanged, "forest_reclaimed");
        await Frames(80);
        Check(forest.Playing && Mathf.IsEqualApprox(forest.VolumeDb, -2f), "retour rapide : l'ancien fondu ne coupe pas le fond");
        bus.EmitSignal(EventBus.SignalName.RandomEventTriggered, "storm", "Orage");
        bus.EmitSignal(EventBus.SignalName.PlayerBiomeChanged, "wild_fields");
        await Frames(45);
        Check(!forest.Playing && audio.GetNode<AudioStreamPlayer>("AmbianceOverlay").Playing,
            "hors forêt : météo audible, sans réactiver le fond de forêt");
        bus.EmitSignal(EventBus.SignalName.GameStateChanged, "Run", "Hub");
        bus.EmitSignal(EventBus.SignalName.GameStateChanged, "Hub", "Run");
        bus.EmitSignal(EventBus.SignalName.RunPhaseChanged, "Crisis", "Exploration");
        Check(!forest.Playing, "nouvelle run : l'ancien biome ne suffit pas à lancer la forêt");
        bus.EmitSignal(EventBus.SignalName.GameStateChanged, "Run", "Hub");
    }

    private void CheckFullLoopDuration()
    {
        System.Reflection.MethodInfo create = typeof(AudioManager).GetMethod("CreateLoopableStream",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        foreach (bool stereo in new[] { false, true })
        {
            foreach (AudioStreamWav.FormatEnum format in new[] { AudioStreamWav.FormatEnum.Format8Bits, AudioStreamWav.FormatEnum.Format16Bits })
            {
                int channels = stereo ? 2 : 1;
                int bytesPerSample = format == AudioStreamWav.FormatEnum.Format8Bits ? 1 : 2;
                AudioStreamWav source = new()
                {
                    Format = format, Stereo = stereo, MixRate = 22050,
                    Data = new byte[22050 * 3 * channels * bytesPerSample],
                    LoopMode = AudioStreamWav.LoopModeEnum.Disabled, LoopBegin = 13, LoopEnd = 31
                };
                AudioStreamWav loop = (AudioStreamWav)create.Invoke(null, new object[] { source });
                Check(loop.LoopEnd == 66150 && loop.LoopBegin == 0 && loop.LoopMode == AudioStreamWav.LoopModeEnum.Forward,
                    $"{format} {(stereo ? "stéréo" : "mono")} : trois secondes entières en boucle");
                Check(source.LoopMode == AudioStreamWav.LoopModeEnum.Disabled && source.LoopBegin == 13 && source.LoopEnd == 31,
                    "stream partagé : bornes et mode conservés");
            }
        }
        AudioStreamWav compressed = GD.Load<AudioStreamWav>("res://assets/audio/sfx/ambiance/sfx_ambiance_foret.wav");
        Check(compressed.Format == AudioStreamWav.FormatEnum.Qoa, "le témoin de forêt teste le format QOA importé");
        AudioStreamWav full = (AudioStreamWav)create.Invoke(null, new object[] { compressed });
        Check(full.LoopEnd == 384000, "QOA : 384 000 échantillons malgré les 310 208 octets compressés");
    }

    private async Task CheckNoteVariants(AudioManager audio)
    {
        const string key = "sfx_weapon_music_box";
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        AudioVoicePool pool = (AudioVoicePool)typeof(AudioManager).GetField("_sfxPool", flags).GetValue(audio);
        System.Collections.Generic.Dictionary<string, ulong> times =
            (System.Collections.Generic.Dictionary<string, ulong>)typeof(AudioManager).GetField("_sfxLastPlayTime", flags).GetValue(audio);
        RandomNumberGenerator rng = (RandomNumberGenerator)typeof(AudioManager).GetField("_rng", flags).GetValue(audio);
        rng.Seed = 221092026;
        AudioStreamPlayer voice = audio.GetNode<AudioStreamPlayer>("Sfx0");
        using FileAccess file = FileAccess.Open(AudioManager.SoundBankPath, FileAccess.ModeFlags.Read);
        Godot.Collections.Dictionary bank = Json.ParseString(file.GetAsText()).AsGodotDictionary();
        Godot.Collections.Array paths = bank[key].AsGodotDictionary()["variants"].AsGodotArray();
        System.Collections.Generic.HashSet<AudioStream> expected = new();
        foreach (Variant path in paths)
            expected.Add(GD.Load<AudioStream>(path.AsString()));
        System.Collections.Generic.HashSet<AudioStream> heard = new();
        AudioStream previous = null;
        bool distinct = true, stablePitch = true, throttled = true, limited = true;
        for (int i = 0; i < 40; i++)
        {
            pool.Stop();
            times.Remove(key);
            audio.PlaySfx(key, 0.4f, 0f, 1.5f);
            distinct &= voice.Playing && expected.Contains(voice.Stream) && voice.Stream != previous;
            stablePitch &= Mathf.IsEqualApprox(voice.PitchScale, 1f);
            heard.Add(voice.Stream);
            previous = voice.Stream;
            ulong state = rng.State;
            audio.PlaySfx(key);
            throttled &= rng.State == state && voice.Stream == previous;
            times.Remove(key);
            audio.PlaySfx(key);
            limited &= rng.State == state && voice.Stream == previous;
        }
        Check(distinct && heard.SetEquals(expected), "Boîte : cinq notes admises, jamais deux identiques à la suite");
        Check(stablePitch, "Boîte : notes justes, aucun désaccordage aléatoire");
        Check(throttled && limited, "Boîte : cadence et plafond communs, refus sans avancer le tirage");
        pool.Stop();
        AudioStreamPlayer ui = audio.PlayUiSfx(key);
        AudioManager.FadeOutUI(ui, key, 0.01f);
        await Frames(12);
        Check(ui != null && !ui.Playing, "Boîte : fondu UI appliqué aussi à une variante");
    }

    private async Task CheckSelectedEvents(AudioManager audio)
    {
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        System.Collections.Generic.List<string> played = new();
        void OnSound(string key, AudioManager.SoundOutcome outcome)
        {
            if (outcome is AudioManager.SoundOutcome.Played or AudioManager.SoundOutcome.VoiceStolen)
                played.Add(key);
        }
        audio.SoundRequested += OnSound;
        try
        {
            bus.EmitSignal(EventBus.SignalName.GameStateChanged, "Hub", "Run");
            bus.EmitSignal(EventBus.SignalName.PlayerDamaged, 95f, 100f);
            bus.PublishPlayerDamage(new(1, PlayerDamageKind.Combat, 100f, 5f, false, false, true));
            Check(played.Count == 1 && played[0] == "sfx_hit_joueur", "premier coup résolu : blessure audible sans état HP préalable");
            played.Clear();
            bus.PublishPlayerDamage(new(1, PlayerDamageKind.Combat, 95f, 0f, false, true, true));
            bus.PublishPlayerDamage(default);
            Check(played.Count == 0, "bouclier et dommage non appliqué : aucun son de blessure");
            bus.EmitSignal(EventBus.SignalName.PlayerDamaged, 65f, 100f);
            bus.PublishPlayerDamage(new(1, PlayerDamageKind.Erasure, 95f, 30f, false, false, true));
            Check(played.Count == 1 && played[0] == "sfx_void_burn", "Néant : brûlure seule, sans blessure ni accent critique");
            bus.PublishPlayerDamage(new(1, PlayerDamageKind.Erasure, 65f, 1f, false, false, true));
            Check(played.Count == 1, "Néant : cadence bornée même sur deux demandes rapprochées");
            // Hors Movie Maker, la cadence suit l'horloge réelle, pas les images headless accélérées.
            await Task.Delay(550);
            bus.PublishPlayerDamage(new(1, PlayerDamageKind.Erasure, 64f, 1f, false, false, true));
            Check(played.Count == 2 && played[1] == "sfx_void_burn", "Néant : nouvelle tranche audible après l'intervalle");
            played.Clear();
            bus.EmitSignal(EventBus.SignalName.CrisisWarning, 1, 20f);
            Check(!played.Contains("sfx_resurgence_accent"), "annonce : aucun accent de début de crise prématuré");
            bus.EmitSignal(EventBus.SignalName.CrisisStarted, 1, 1);
            Check(played.Contains("sfx_resurgence_accent"), "début réel de Résurgence : accent choisi");
            played.Clear();
            Vestiges.Combat.PlayerDefenseFeedback defense = new();
            defense.ArmorAbsorbed(Vector2.Zero, 20f, 20f, Vector2.Up);
            Check(played.Count == 0, "armure sans réduction : pas de parade sonore");
            defense.Ignored(Vector2.Zero);
            Check(played.Count == 1 && played[0] == "sfx_armor_parry", "coup ignoré : parade choisie audible sans pool de particules");
            audio.SoundRequested -= OnSound;
            bus.EmitSignal(EventBus.SignalName.GameStateChanged, "Run", "Hub");
        }
        finally
        {
            audio.SoundRequested -= OnSound;
        }
    }

    private static AudioStreamPlayer Start(AudioVoicePool pool, AudioStream stream, string key, int priority)
    {
        AudioStreamPlayer player = pool.Acquire(key, priority, 1, out _);
        if (player == null)
            throw new InvalidOperationException($"Voix de test refusée : {key}");
        player.Stream = stream;
        player.VolumeDb = 0f;
        player.Play();
        return player;
    }

    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private void Check(bool condition, string label)
    {
        if (!condition)
            _failures++;
        GD.Print($"[AudioMixRegression] {(condition ? "OK" : "FAIL")} {label}");
    }
}
