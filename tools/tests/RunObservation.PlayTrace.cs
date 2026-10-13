using System.Threading.Tasks;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// --check-play-trace : l'enregistreur des vraies parties (plan 42 M0e), branché à la main puisque les bancs ne l'ont
/// pas, suit 40 s d'un bot mortel qui tourne en rond ; à sa sortie de l'arbre, sa trace doit être écrite et remplie.
/// </summary>
public partial class RunObservation
{
    private async Task CheckPlayTrace()
    {
        // Dossier vidé d'abord : une trace d'un essai précédent ne doit pas faire passer le contrôle.
        string folder = DevelopmentMode.GetSavePath("traces/x").GetBaseDir();
        foreach (string old in DirAccess.GetFilesAt(folder))
            DirAccess.RemoveAbsolute($"{folder}/{old}");
        _player.IsGodMode = false;
        _player.SurviveFatalHitsForTests = true;
        PlayTraceRecorder recorder = new() { Name = "PlayTraceRecorder" };
        recorder.Setup(_player);
        _world.AddChild(recorder);
        ProcessMode = ProcessModeEnum.Always;
        double time = 0;
        while (time < 40.0)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (GetTree().Paused)
            {
                AutoPickLevelUp();
                continue;
            }
            time += GetProcessDeltaTime();
            _player.AIInputOverride = Vector2.FromAngle((float)time * 0.5f);
        }
        _world.RemoveChild(recorder);
        recorder.QueueFree();
        string[] files = DirAccess.GetFilesAt(folder);
        string csv = null, summary = null;
        foreach (string file in files)
        {
            if (file.EndsWith(".csv"))
                csv = FileAccess.GetFileAsString($"{folder}/{file}");
            else if (file.EndsWith(".txt"))
                summary = FileAccess.GetFileAsString($"{folder}/{file}");
        }
        if (csv == null || summary == null || !summary.Contains("seconds=4") || csv.Split('\n').Length < 3)
            throw new System.InvalidOperationException($"Trace absente ou vide dans {folder} : {files.Length} fichiers, résumé « {summary} »");
        GD.Print($"[RunObservation] RESULT play-trace {summary.Trim()} lignes={csv.Split('\n').Length - 2}");
    }
}
