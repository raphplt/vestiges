using System;
using System.IO;
using System.Text.Json;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Score;

namespace Vestiges.Tests;

/// <summary>
/// Plan 26 Q2a : interruptions, fichiers tronqués, racines et versions invalides, version future, échec d'écriture.
/// Tourne dans le profil de test (user://tests/), isolé par tools/test_saves.sh.
/// </summary>
public partial class SaveFileRegression : Node
{
    private int _checks;
    private int _failures;

    public override void _Ready()
    {
        try
        {
            RunSaveFileChecks();
            RunMetaChecks();
            RunHistoryChecks();
            RunHighScoreChecks();
        }
        catch (Exception ex)
        {
            Fail($"exception {ex.GetType().Name} : {ex.Message}");
        }

        MetaSaveManager.ReloadProfile();
        RunHistoryManager.ForceReload();
        GD.Print($"[SaveFileRegression] RESULT checks={_checks} failures={_failures}");
        if (_failures == 0)
            GD.Print("[SaveFileRegression] PASS");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void RunSaveFileChecks()
    {
        string path = DevelopmentMode.GetSavePath("save_regression/sample.json");
        string target = Abs(path);
        Reset(target);

        Check(SaveFile.Write(path, "{\"v\":1,\"n\":1}").Succeeded, "écriture d'un fichier neuf");
        Check(File.Exists(target) && !File.Exists(target + ".tmp") && !File.Exists(target + ".bak"), "fichier neuf sans temporaire ni copie");
        SaveFile.ReadResult<int> read = SaveFile.Read<int>(path, ParseSample);
        Check(read.Status == SaveFile.ReadStatus.Loaded && read.Value == 1, "relecture du fichier neuf");

        Check(SaveFile.Write(path, "{\"v\":1,\"n\":2}").Succeeded, "seconde écriture");
        Check(File.ReadAllText(target + ".bak") == "{\"v\":1,\"n\":1}", "la version précédente devient la copie de secours");

        // Interruption avant le remplacement : le temporaire est incomplet, la destination fait foi.
        File.WriteAllText(target + ".tmp", "{\"v\":1,\"n\":");
        read = SaveFile.Read<int>(path, ParseSample);
        Check(read.Status == SaveFile.ReadStatus.Loaded && read.Value == 2, "temporaire interrompu ignoré");
        Check(!File.Exists(target + ".tmp"), "temporaire interrompu retiré");

        // Remplacement arrêté entre ses deux renommages : destination absente, temporaire sain promu.
        File.Copy(target, target + ".keep");
        File.Move(target, target + ".tmp");
        read = SaveFile.Read<int>(path, ParseSample);
        Check(read.Status == SaveFile.ReadStatus.Loaded && read.Value == 2 && !File.Exists(target + ".tmp"), "temporaire sain promu sans destination");
        File.Delete(target + ".keep");

        // Destination disparue pendant un remplacement : reprise sur la copie, qui reste en place.
        File.Delete(target);
        read = SaveFile.Read<int>(path, ParseSample);
        Check(read.Status == SaveFile.ReadStatus.Recovered && read.Value == 1, "destination absente reprise sur la copie");
        Check(File.Exists(target) && File.ReadAllText(target) == "{\"v\":1,\"n\":1}", "destination restaurée");
        Check(File.Exists(target + ".bak"), "copie de secours conservée après restauration");

        // Fichier tronqué : mis de côté pour diagnostic, reprise sur la copie.
        SaveFile.Write(path, "{\"v\":1,\"n\":3}");
        File.WriteAllText(target, "{\"v\":1,\"n");
        read = SaveFile.Read<int>(path, ParseSample);
        Check(read.Status == SaveFile.ReadStatus.Recovered && read.Value == 1, "fichier tronqué repris sur la copie");
        Check(CountQuarantined(target) == 1, "fichier tronqué conservé à part");

        // Tronqué sans copie : on repart à vide, le fichier reste à part.
        Reset(target);
        File.WriteAllText(target, "{\"v\":1,");
        read = SaveFile.Read<int>(path, ParseSample);
        Check(read.Status == SaveFile.ReadStatus.Unreadable && !File.Exists(target), "tronqué sans copie : illisible et retiré");
        Check(CountQuarantined(target) == 1, "tronqué sans copie conservé à part");

        // Version future : lue au mieux, jamais touchée.
        Reset(target);
        File.WriteAllText(target, "{\"v\":9,\"n\":4}");
        read = SaveFile.Read<int>(path, ParseSample);
        Check(read.Status == SaveFile.ReadStatus.FutureVersion && read.BlocksWrites && read.Value == 4, "version future détectée");
        Check(File.ReadAllText(target) == "{\"v\":9,\"n\":4}", "version future intacte");

        // Échec d'écriture : le temporaire ne peut pas être créé, la destination ne bouge pas.
        Reset(target);
        SaveFile.Write(path, "{\"v\":1,\"n\":5}");
        Directory.CreateDirectory(target + ".tmp");
        SaveFile.WriteResult failed = SaveFile.Write(path, "{\"v\":1,\"n\":6}");
        Directory.Delete(target + ".tmp");
        Check(!failed.Succeeded && failed.Error.Length > 0, "échec d'écriture signalé");
        Check(File.ReadAllText(target) == "{\"v\":1,\"n\":5}", "destination intacte après un échec d'écriture");
        Reset(target);
    }

    private void RunMetaChecks()
    {
        string path = DevelopmentMode.GetSavePath("meta_save.json");
        string target = Abs(path);

        // Racine [] avec une copie valide.
        Reset(target);
        File.WriteAllText(target + ".bak", "{\"version\":2,\"vestiges\":41,\"unlocked_characters\":[\"vagabond\"]}");
        File.WriteAllText(target, "[]");
        MetaSaveManager.ReloadProfile();
        Check(MetaSaveManager.GetVestiges() == 41 && MetaSaveManager.LoadStatus == SaveFile.ReadStatus.Recovered, "méta : racine [] reprise sur la copie");
        Check(CountQuarantined(target) == 1, "méta : racine [] conservée à part");

        // Version non entière, sans copie : profil vide, puis écriture normale.
        Reset(target);
        File.WriteAllText(target, "{\"version\":\"deux\",\"vestiges\":9}");
        MetaSaveManager.ReloadProfile();
        Check(MetaSaveManager.GetVestiges() == 0 && MetaSaveManager.LoadStatus == SaveFile.ReadStatus.Unreadable, "méta : version invalide refusée");
        Check(CountQuarantined(target) == 1, "méta : version invalide conservée à part");
        MetaSaveManager.AddVestiges(3);
        MetaSaveManager.ReloadProfile();
        Check(MetaSaveManager.GetVestiges() == 3 && MetaSaveManager.LoadStatus == SaveFile.ReadStatus.Loaded, "méta : nouveau profil écrit et relu");

        // Valeur de champ d'un mauvais type : illisible, pas d'exception.
        Reset(target);
        File.WriteAllText(target, "{\"version\":2,\"vestiges\":\"beaucoup\"}");
        MetaSaveManager.ReloadProfile();
        MetaSaveManager.Load();
        Check(MetaSaveManager.LoadStatus == SaveFile.ReadStatus.Unreadable, "méta : champ mal typé refusé");

        // Version future : jamais réécrite.
        Reset(target);
        const string future = "{\"version\":99,\"vestiges\":70,\"nouveau_champ\":true}";
        File.WriteAllText(target, future);
        MetaSaveManager.ReloadProfile();
        Check(MetaSaveManager.GetVestiges() == 70 && !MetaSaveManager.CanWrite, "méta : version future lue sans écriture");
        MetaSaveManager.AddVestiges(5);
        Check(!MetaSaveManager.Save().Succeeded, "méta : sauvegarde refusée sur version future");
        Check(File.ReadAllText(target) == future && !File.Exists(target + ".bak"), "méta : version future intacte");

        // Migration V1 inchangée : archive puis passage en V2.
        Reset(target);
        string archive = Abs(DevelopmentMode.GetSavePath("meta_save_legacy_v1.json"));
        Reset(archive);
        File.WriteAllText(target, "{\"vestiges\":12,\"stats\":{\"max_nights_survived\":3,\"total_runs\":4}}");
        MetaSaveManager.ReloadProfile();
        Check(MetaSaveManager.GetVestiges() == 12 && MetaSaveManager.GetStats().TotalRuns == 4, "méta : migration V1");
        Check(File.Exists(archive) && File.ReadAllText(target).Contains("\"version\": 2"), "méta : V1 archivée et réécrite en V2");

        // Sauvegarde tronquée après coup : la copie garde l'avant-dernière version.
        MetaSaveManager.AddVestiges(8);
        string written = File.ReadAllText(target);
        File.WriteAllText(target, written[..(written.Length / 2)]);
        MetaSaveManager.ReloadProfile();
        Check(MetaSaveManager.GetVestiges() == 12 && MetaSaveManager.LoadStatus == SaveFile.ReadStatus.Recovered, "méta : sauvegarde tronquée reprise sur la précédente");
        Reset(target);
        Reset(archive);
        MetaSaveManager.ReloadProfile();
    }

    private void RunHistoryChecks()
    {
        string target = Abs(DevelopmentMode.GetSavePath("run_history.json"));

        Reset(target);
        File.WriteAllText(target, "{}");
        RunHistoryManager.ForceReload();
        Check(RunHistoryManager.GetHistory().Count == 0 && RunHistoryManager.LoadStatus == SaveFile.ReadStatus.Unreadable, "historique : racine {} refusée");
        Check(CountQuarantined(target) == 1, "historique : racine {} conservée à part");

        Reset(target);
        const string future = "[{\"version\":3,\"score\":10},{\"version\":7,\"score\":20}]";
        File.WriteAllText(target, future);
        RunHistoryManager.ForceReload();
        Check(RunHistoryManager.GetHistory().Count == 2 && !RunHistoryManager.CanWrite, "historique : version future lue sans écriture");
        Check(!RunHistoryManager.SaveRun(new RunRecord { Score = 30 }).Succeeded, "historique : ajout refusé sur version future");
        Check(File.ReadAllText(target) == future, "historique : version future intacte");

        Reset(target);
        File.WriteAllText(target, "[{\"version\":3,\"score\":10}]");
        RunHistoryManager.ForceReload();
        Check(RunHistoryManager.SaveRun(new RunRecord { Score = 30 }).Succeeded, "historique : ajout écrit");
        RunHistoryManager.ForceReload();
        Check(RunHistoryManager.GetHistory().Count == 2 && RunHistoryManager.GetBestScore() == 30, "historique : ajout relu");
        Reset(target);
        RunHistoryManager.ForceReload();
    }

    private void RunHighScoreChecks()
    {
        string target = Abs(DevelopmentMode.GetSavePath("highscore_kills.save"));
        Reset(target);
        File.WriteAllText(target + ".bak", "42");
        File.WriteAllText(target, "quarante");
        ScoreManager score = new();
        AddChild(score);
        Check(score.BestScore == 42, "record illisible repris sur la copie");
        score.QueueFree();
        Reset(target);
    }

    private static SaveFile.Parse<int> ParseSample(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            int version = doc.RootElement.GetProperty("v").GetInt32();
            int value = doc.RootElement.GetProperty("n").GetInt32();
            return version > 1 ? SaveFile.Parse<int>.Future(value, "v>1") : SaveFile.Parse<int>.Valid(value);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or System.Collections.Generic.KeyNotFoundException)
        {
            return SaveFile.Parse<int>.Invalid(ex.Message);
        }
    }

    private static string Abs(string path) => ProjectSettings.GlobalizePath(path);

    private static int CountQuarantined(string target)
    {
        string name = Path.GetFileNameWithoutExtension(target);
        return Directory.GetFiles(Path.GetDirectoryName(target) ?? "", $"{name}.corrupt-*").Length;
    }

    private static void Reset(string target)
    {
        foreach (string file in new[] { target, target + ".tmp", target + ".bak" })
        {
            if (File.Exists(file))
                File.Delete(file);
        }
        string name = Path.GetFileNameWithoutExtension(target);
        foreach (string file in Directory.GetFiles(Path.GetDirectoryName(target) ?? "", $"{name}.corrupt-*"))
            File.Delete(file);
    }

    private void Check(bool condition, string label)
    {
        _checks++;
        if (condition)
            GD.Print($"[SaveFileRegression] ok {label}");
        else
            Fail(label);
    }

    private void Fail(string label)
    {
        _failures++;
        GD.Print($"[SaveFileRegression] FAIL {label}");
    }
}
