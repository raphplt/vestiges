using System;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>Régressions du mix : saturation, priorités, répétitions et réemploi d'une voix en fondu.</summary>
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
            AudioStreamPlayer hover = audio.PlayUiSfx("sfx_menu_survol");
            Check(hover != null && audio.PlayUiSfx("sfx_menu_survol") == null, "survol UI limité même hors pause");
            GetTree().Paused = true;
            AudioStreamPlayer confirm = audio.PlayUiSfx("sfx_menu_confirmer");
            Check(confirm != null && confirm.CanProcess(), "confirmation audible pendant la pause");
            GetTree().Paused = false;
            audio.PlayLoopingSfx("sfx_bow_release", -2f);
            Check(Mathf.IsEqualApprox(audio.GetNode<AudioStreamPlayer>("LoopingSfx").VolumeDb, -16f), "gain de banque appliqué aux boucles");
            audio.StopLoopingSfx();

            await CheckNoteVariants(audio);

            EventBus bus = GetNode<EventBus>("/root/EventBus");
            bus.EmitSignal(EventBus.SignalName.RunPhaseChanged, "Crisis", "Exploration");
            Check(audio.GetNode<AudioStreamPlayer>("Ambiance").Playing, "ambiance démarrée en exploration");
            bus.EmitSignal(EventBus.SignalName.CrisisStarted, 1, 1);
            bus.EmitSignal(EventBus.SignalName.RunPhaseChanged, "Crisis", "Exploration");
            await Frames(135);
            Check(audio.GetNode<AudioStreamPlayer>("Ambiance").Playing, "retour exploration : ancien fondu annulé");
            bus.EmitSignal(EventBus.SignalName.GameStateChanged, "Run", "Hub");
            await Frames(2);
            Check(!audio.GetNode<AudioStreamPlayer>("Ambiance").Playing
                && !audio.GetNode<AudioStreamPlayer>("AmbianceOverlay").Playing, "Hub : arrêt des ambiances de run");

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
