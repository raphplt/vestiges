using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// Pistes séparées d'une run enregistrée (plan 15, M2) : un effet d'enregistrement en fin de chaque bus écrit
/// musique, ambiance, effets et pas dans des WAV synchrones du mixage complet, pour désigner à l'oreille la famille
/// d'un son sans rejouer la run, qui divergerait. Les pas, quasi continus en marche, passent le temps de la mesure par
/// un bus à eux, au volume du bus des effets : le mixage ne change pas. L'effet passe avant le volume du bus : les
/// pistes sont au niveau d'avant le fader, que tools/audio_stems.py réapplique. stems.json donne ces volumes et le
/// début des pistes sur l'horloge sonore, qui est la position dans l'enregistrement du Movie Maker.
/// </summary>
internal sealed class AudioStemRecorder : IDisposable
{
    private const string StepsBus = "StemSteps";
    private const string StepsPrefix = "sfx_pas_";
    private static readonly (string Bus, string Stem)[] Stems = { ("Music", "music"), ("Ambiance", "ambiance"), ("SFX", "sfx"), (StepsBus, "steps") };
    private readonly string _output;
    private readonly List<(int Bus, int Effect, AudioEffectRecord Record)> _records = new();
    private readonly double _startSeconds = AudioManager.NowMsec / 1000.0;
    private readonly Dictionary<string, string> _soundBuses;
    private readonly List<string> _reroutedSteps = new();

    internal AudioStemRecorder(string output)
    {
        _output = output;
        AudioServer.AddBus();
        int steps = AudioServer.BusCount - 1;
        AudioServer.SetBusName(steps, StepsBus);
        AudioServer.SetBusVolumeDb(steps, AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("SFX")));
        AudioServer.SetBusSend(steps, "Master");
        _soundBuses = (Dictionary<string, string>)typeof(AudioManager)
            .GetField("_soundBuses", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(AudioManager.Instance);
        foreach (string key in _soundBuses.Keys.Where(key => key.StartsWith(StepsPrefix) && _soundBuses[key] == "SFX").ToList())
        {
            _soundBuses[key] = StepsBus;
            _reroutedSteps.Add(key);
        }

        foreach ((string name, string _) in Stems)
        {
            int bus = AudioServer.GetBusIndex(name);
            if (bus < 0)
                throw new InvalidOperationException($"Bus audio absent : {name}");
            AudioEffectRecord record = new() { Format = AudioStreamWav.FormatEnum.Format16Bits };
            AudioServer.AddBusEffect(bus, record);
            int effect = AudioServer.GetBusEffectCount(bus) - 1;
            record.SetRecordingActive(true);
            _records.Add((bus, effect, record));
        }
    }

    public void Dispose()
    {
        foreach (string key in _reroutedSteps)
            _soundBuses[key] = "SFX";
        Godot.Collections.Dictionary manifest = new() { ["start_audio_s"] = _startSeconds };
        Godot.Collections.Dictionary stems = new();
        for (int i = 0; i < _records.Count; i++)
        {
            (int bus, int effect, AudioEffectRecord record) = _records[i];
            record.SetRecordingActive(false);
            AudioStreamWav wav = record.GetRecording();
            string stem = Stems[i].Stem;
            string path = $"{_output}/stem-{stem}.wav";
            Error error = wav?.SaveToWav(path) ?? Error.Failed;
            GD.Print($"[AudioStems] {stem} {(error == Error.Ok ? $"saved {path} seconds={wav.GetLength():F1}" : $"error {error}")}");
            if (error == Error.Ok)
                stems[stem] = new Godot.Collections.Dictionary
                {
                    ["file"] = path.GetFile(), ["bus_volume_db"] = AudioServer.GetBusVolumeDb(bus), ["seconds"] = wav.GetLength(),
                };
            AudioServer.RemoveBusEffect(bus, effect);
        }
        AudioServer.RemoveBus(AudioServer.GetBusIndex(StepsBus));
        manifest["stems"] = stems;
        manifest["steps_keys"] = _reroutedSteps.ToArray();
        using FileAccess file = FileAccess.Open($"{_output}/stems.json", FileAccess.ModeFlags.Write);
        file?.StoreString(Json.Stringify(manifest, "  "));
    }
}
