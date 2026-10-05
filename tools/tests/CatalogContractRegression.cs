using System;
using System.Text.Json.Nodes;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// Contrats des catalogues de réglages (plan 26 Q7a) : les fichiers du dépôt sont acceptés ; chaque règle a sa fixture
/// négative (champ absent, mauvais type, borne, clé inconnue, clé en double, créature inconnue), refusée avec un message
/// qui nomme le champ, sans rien publier.
/// </summary>
public partial class CatalogContractRegression : Node
{
    private int _checks;

    public override void _Ready()
    {
        try
        {
            EnemyDataLoader.Load();
            CheckXpCurve();
            CheckScore();
            CheckPeril();
            GD.Print($"[CatalogContractRegression] RESULT failures=0 checks={_checks}");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[CatalogContractRegression] FAIL {exception}");
            GetTree().Quit(1);
        }
    }

    private void CheckXpCurve()
    {
        string valid = FileAccess.GetFileAsString("res://data/scaling/progression.json");
        Check(XpCurveConfig.TryParse(valid, out _, out string error), $"courbe d'XP du dépôt acceptée {error}");
        (string, Action<JsonObject>, string)[] cases =
        {
            ("champ absent", root => root.Remove("base_xp"), "base_xp absent"),
            ("coût nul", root => root["base_xp"] = 0, "base_xp : 0 (strictement positif attendu)"),
            ("plafond nul", root => root["max_xp_per_level"] = 0, "max_xp_per_level : 0"),
            ("texte au lieu d'un nombre", root => root["exponent"] = "vite", "exponent : nombre fini attendu"),
            ("niveaux précoces non entiers", root => root["early_levels"] = 2.5, "early_levels : 2.5 (entier de 0 à 100 attendu)"),
            ("clé inconnue", root => root["base_xpp"] = 20, "clé « base_xpp » inconnue"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            bool rejected = !XpCurveConfig.TryParse(Mutated(valid, mutate), out XpCurveConfig config, out string message);
            Check(rejected && config == null && message.Contains(expected, StringComparison.Ordinal), $"courbe d'XP refusée ({label}) : {message}");
        }
        bool duplicate = !XpCurveConfig.TryParse("{\"base_xp\": 22, \"base_xp\": 0, \"exponent\": 1.42, \"early_levels\": 5, \"early_multiplier_start\": 1.65, \"early_multiplier_end\": 1.2, \"max_xp_per_level\": 20000}",
            out _, out string duplicateError);
        Check(duplicate && duplicateError.Contains("« base_xp » en double", StringComparison.Ordinal), $"courbe d'XP refusée (clé en double) : {duplicateError}");
    }

    private void CheckScore()
    {
        string valid = FileAccess.GetFileAsString("res://data/scaling/score.json");
        Check(ScoreConfig.TryParse(valid, EnemyDataLoader.Exists, out _, out string error), $"barème du dépôt accepté {error}");
        (string, Action<JsonObject>, string)[] cases =
        {
            ("table absente", root => root.Remove("kill_points"), "section kill_points absente"),
            ("défaut absent", root => root["kill_points"]!.AsObject().Remove("default"), "kill_points : default absent"),
            ("créature inconnue", root => root["kill_points"]!["rodeurr"] = 15, "créature « rodeurr » inconnue"),
            ("points négatifs", root => root["kill_points"]!["rodeur"] = -5, "rodeur : -5"),
            ("points fractionnaires", root => root["kill_points"]!["rodeur"] = 15.5, "rodeur : 15.5"),
            ("texte au lieu d'un nombre", root => root["kill_points"]!["rodeur"] = "quinze", "rodeur : nombre fini attendu"),
            ("clé inconnue", root => root["points"] = 1, "clé « points » inconnue"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            bool rejected = !ScoreConfig.TryParse(Mutated(valid, mutate), EnemyDataLoader.Exists, out ScoreConfig config, out string message);
            Check(rejected && config == null && message.Contains(expected, StringComparison.Ordinal), $"barème refusé ({label}) : {message}");
        }
    }

    private void CheckPeril()
    {
        string valid = FileAccess.GetFileAsString("res://data/scaling/peril.json");
        Check(PerilDataLoader.Parse(valid) == null, "Péril du dépôt accepté");
        int max = PerilDataLoader.Max;
        float xp = PerilDataLoader.XpMultiplier(10);
        (string, Action<JsonObject>, string)[] cases =
        {
            ("bloc absent", root => root.Remove("banish"), "section banish absente"),
            ("diviseur nul", root => root["banish"]!["peril_divisor"] = 0, "peril_divisor : 0 (entier de 1 à 1000 attendu)"),
            ("valeur négative", root => root["per_point"]!["xp"] = -0.1, "xp : -0.1 (positif ou nul attendu)"),
            ("valeur absente", root => root["per_point"]!.AsObject().Remove("rarity_steps"), "rarity_steps absent"),
            ("clé inconnue", root => root["per_point"]!["luck"] = 0.1, "clé « luck » inconnue"),
            ("maximum non entier", root => root["max"] = 9.5, "max : 9.5"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            JsonObject root = JsonNode.Parse(valid)!.AsObject();
            mutate(root);
            // Une valeur valide changée en même temps prouve qu'un refus ne publie rien, même partiellement.
            if (root["max"] is JsonValue value && value.TryGetValue(out int _))
                root["max"] = 7;
            string message = PerilDataLoader.Parse(root.ToJsonString());
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"Péril refusé ({label}) : {message}");
        }
        Check(PerilDataLoader.Max == max && Mathf.IsEqualApprox(PerilDataLoader.XpMultiplier(10), xp),
            "Péril : aucun refus n'a publié de valeur");
    }

    private static string Mutated(string json, Action<JsonObject> mutate)
    {
        JsonObject root = JsonNode.Parse(json)!.AsObject();
        mutate(root);
        return root.ToJsonString();
    }

    private void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        _checks++;
        GD.Print($"[CatalogContractRegression] OK {message}");
    }
}
