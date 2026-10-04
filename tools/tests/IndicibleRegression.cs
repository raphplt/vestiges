using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Score;

namespace Vestiges.Tests;

/// <summary>
/// Plan 26 Q6b : réglages de l'Indicible lus depuis les données. La configuration du dépôt redonne les constantes
/// d'avant le lot, une configuration invalide est refusée, et un combat scripté vérifie le rythme, le nombre de
/// tentacules, les dégâts, l'enrage et la récompense.
/// </summary>
public partial class IndicibleRegression : Node2D
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private const double Frame = 1.0 / 60.0;
    private int _failures;

    public override async void _Ready()
    {
        try
        {
            CheckConfigMatchesFormerConstants();
            CheckInvalidConfigsRejected();
            await CheckFight();
        }
        catch (Exception ex)
        {
            Check(false, $"exception {ex.GetType().Name} : {ex.Message}");
        }
        GD.Print($"[IndicibleRegression] RESULT failures={_failures}");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void CheckConfigMatchesFormerConstants()
    {
        Check(IndicibleConfig.TryLoad(out IndicibleConfig c, out string loadError), $"configuration du jeu chargée {loadError}");
        // Valeurs écrites dans Combat/Indicible.cs avant le lot.
        (string Name, float Actual, float Former)[] values =
        {
            ("enrage_hp_ratio", c.EnrageHpRatio, 0.5f), ("interval_sec", c.TentacleInterval, 2.5f),
            ("first_attack_ratio", c.FirstAttackRatio, 0.3f), ("enraged_interval_ratio", c.EnragedIntervalRatio, 0.6f),
            ("count", c.TentacleCount, 2f), ("enraged_count", c.EnragedTentacleCount, 3f), ("warning_sec", c.WarningDuration, 0.8f),
            ("width", c.TentacleWidth, 30f), ("length", c.TentacleLength, 120f), ("target_jitter", c.TargetJitter, 60f),
            ("spread", c.EdgeSpread, 350f), ("half_length", c.EdgeHalfLength, 200f), ("half_thickness", c.EdgeHalfThickness, 40f),
            ("eye_count", c.EyeCount, 5f), ("eye_spread", c.EyeSpread, 330f), ("eye_offset", c.EyeOffset, 150f),
            ("eye_first_shift_sec", c.EyeFirstShift, 1f), ("eye_shift_interval_sec", c.EyeShiftInterval, 3f),
            ("eye_shift_sec", c.EyeShiftDuration, 1.2f), ("strike_visual_sec", c.StrikeVisualDuration, 0.4f),
        };
        foreach ((string name, float actual, float former) in values)
            Check(actual == former, $"configuration : {name} = {actual} (avant : {former})");
        EnemyData data = EnemyDataLoader.Get("indicible");
        Check(data.Stats.Hp == 2000f && data.Stats.Damage == 15f, $"fiche : {data.Stats.Hp} PV, {data.Stats.Damage} de dégâts (avant : 2000 et 15 en dur)");
    }

    private void CheckInvalidConfigsRejected()
    {
        string valid = FileAccess.GetFileAsString("res://data/scaling/indicible.json");
        Check(IndicibleConfig.TryParse(valid, out _, out string validError), $"configuration du dépôt acceptée {validError}");
        (string Label, Action<JsonObject> Mutate, string Expected)[] cases =
        {
            ("section absente", root => root.Remove("edges"), "section edges absente"),
            ("réglage absent", root => root["tentacles"]!.AsObject().Remove("warning_sec"), "warning_sec absent"),
            ("cadence nulle", root => root["tentacles"]!["interval_sec"] = 0, "interval_sec"),
            ("nombre non entier", root => root["tentacles"]!["count"] = 2.5, "count"),
            ("nombre démesuré", root => root["tentacles"]!["enraged_count"] = 1e12, "enraged_count"),
            ("part hors de ]0 ; 1]", root => root["enrage_hp_ratio"] = 1.5, "enrage_hp_ratio"),
            ("texte au lieu d'un nombre", root => root["tentacles"]!["width"] = "trente", "width"),
            ("écart négatif", root => root["decor"]!["eye_offset"] = -1, "eye_offset"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            JsonObject root = JsonNode.Parse(valid)!.AsObject();
            mutate(root);
            bool rejected = !IndicibleConfig.TryParse(root.ToJsonString(), out IndicibleConfig config, out string error);
            Check(rejected && config == null && error.Contains(expected), $"configuration invalide refusée ({label}) : {error}");
        }
    }

    /// <summary>
    /// Joueur immobile et inerte, couloir tiré sans écart autour de lui : chaque tentacule touche, si bien que les
    /// touches comptent les tentacules de chaque attaque.
    /// </summary>
    private async Task CheckFight()
    {
        Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(player);
        player.InitializeCharacter(CharacterDataLoader.Get("vagabond"));
        player.ProcessMode = ProcessModeEnum.Disabled;
        player.GlobalPosition = Vector2.Zero;
        typeof(Player).GetField("_currentHp", Private)!.SetValue(player, 1_000_000f);

        ScoreManager score = new();
        AddChild(score);
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        List<(double Time, float Damage)> hits = new();
        double now = 0;
        EventBus.PlayerHitByEventHandler onHit = (source, damage) => { if (source == "indicible") hits.Add((now, damage)); };
        bus.PlayerHitBy += onHit;

        const float damageScale = 0.5f;
        Indicible boss = new() { Name = "IndicibleBoss" };
        AddChild(boss);
        Check(boss.Initialize(1f, damageScale, Vector2.Zero), "boss initialisé");
        JsonObject root = JsonNode.Parse(FileAccess.GetFileAsString("res://data/scaling/indicible.json"))!.AsObject();
        root["tentacles"]!["target_jitter"] = 0;
        IndicibleConfig.TryParse(root.ToJsonString(), out IndicibleConfig noJitter, out _);
        typeof(Indicible).GetField("_config", Private)!.SetValue(boss, noJitter);
        FieldInfo timer = typeof(Indicible).GetField("_tentacleTimer", Private)!;
        FieldInfo phase = typeof(Indicible).GetField("_phase", Private)!;
        FieldInfo maxHp = typeof(Indicible).GetField("_maxHp", Private)!;
        Check((float)maxHp.GetValue(boss)! == 2000f, $"PV du boss : {(float)maxHp.GetValue(boss)!} (fiche × 1)");

        List<(double Time, int Phase, float Reset)> attacks = new();
        float previous = (float)timer.GetValue(boss)!;
        Check(Mathf.IsEqualApprox(previous, 0.75f), $"première attaque après {previous} s (2,5 × 0,3)");
        bool enraged = false;
        while (now < 14.0)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            now += Frame;
            float current = (float)timer.GetValue(boss)!;
            if (current > previous)
                attacks.Add((now, (int)phase.GetValue(boss)!, current));
            previous = current;
            // Après trois attaques normales, le boss passe sous la moitié de ses PV.
            if (!enraged && attacks.Count == 3)
            {
                boss.TakeDamage(1000f);
                enraged = true;
            }
        }

        List<(double Time, int Phase, float Reset)> normal = attacks.FindAll(a => a.Phase == 1);
        List<(double Time, int Phase, float Reset)> angry = attacks.FindAll(a => a.Phase == 2);
        Check(normal.Count == 3 && Math.Abs(normal[0].Time - 0.75) < 2.5 * Frame, $"première attaque à {(normal.Count > 0 ? normal[0].Time : -1):0.00} s");
        Check(normal.TrueForAll(a => Mathf.IsEqualApprox(a.Reset, 2.5f)), "rythme normal : 2,5 s entre deux attaques");
        Check(angry.Count >= 3 && angry.TrueForAll(a => Mathf.IsEqualApprox(a.Reset, 1.5f)), $"rythme enragé : 1,5 s ({angry.Count} attaques)");
        Check(angry.Count >= 3 && Math.Abs(angry[2].Time - angry[1].Time - 1.5) < 2.5 * Frame, "enragé : écart mesuré de 1,5 s");
        foreach ((double time, int attackPhase, float _) in attacks)
        {
            if (time + 0.9 > now)
                continue;
            int count = hits.FindAll(h => h.Time > time && h.Time <= time + 0.8 + 2 * Frame).Count;
            int expected = attackPhase == 2 ? 3 : 2;
            Check(count == expected, $"attaque à {time:0.00} s (phase {attackPhase}) : {count} tentacules, {expected} attendus");
        }
        Check(hits.Count > 0 && hits.TrueForAll(h => Mathf.IsEqualApprox(h.Damage, 15f * damageScale)), $"dégâts d'un tentacule : 15 × {damageScale}");

        int before = score.CurrentScore;
        boss.TakeDamage(1_000_000f);
        Check(score.CurrentScore - before == 5000, $"récompense : +{score.CurrentScore - before} points");
        bus.PlayerHitBy -= onHit;
    }

    private void Check(bool passed, string message)
    {
        if (!passed)
            _failures++;
        GD.Print($"[IndicibleRegression] {(passed ? "PASS" : "FAIL")} {message}");
    }
}
