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
using Vestiges.Spawn;

namespace Vestiges.Tests;

/// <summary>
/// Plan 07 B3 : l'Indicible refait. Réglages lus et contrôlés ; combat scripté dans la vraie scène de run : nuit qui
/// tombe, vent qui fait dériver le joueur, éclairs et mains qui agrippent un joueur immobile, réserve commune entamée
/// par les mains, marée sous le premier seuil (B3b), seuils de phase, mort unique (score, élimination « indicible »).
/// </summary>
public partial class IndicibleRegression : Node2D
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private int _failures;

    public override async void _Ready()
    {
        try
        {
            CheckConfig();
            await CheckFight();
        }
        catch (Exception ex)
        {
            Check(false, $"exception {ex.GetType().Name} : {ex.Message}");
        }
        GD.Print($"[IndicibleRegression] RESULT failures={_failures}");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void CheckConfig()
    {
        Check(IndicibleConfig.TryLoad(out IndicibleConfig c, out string loadError), $"configuration du jeu chargée {loadError}");
        Check(c.PhaseThresholds.Count == 2 && c.HandMaxAlive >= 1 && c.LightningWarning > 0f, $"deux seuils, {c.HandMaxAlive} main(s) à la fois");
        string valid = FileAccess.GetFileAsString("res://data/scaling/indicible.json");
        (string Label, Action<JsonObject> Mutate, string Expected)[] cases =
        {
            ("section absente", root => root.Remove("storm"), "section storm absente"),
            ("réglage absent", root => root["hands"]!.AsObject().Remove("grab_warning_sec"), "grab_warning_sec absent"),
            ("clé inconnue", root => root["hands"]!["grab_delay"] = 1, "inconnue"),
            ("seuils croissants", root => root["phase_thresholds"] = new JsonArray(0.3, 0.6), "phase_thresholds"),
            ("seuil hors de ]0 ; 1[", root => root["phase_thresholds"] = new JsonArray(1.2), "phase_thresholds"),
            ("distances inversées", root => root["hands"]!["distance_min"] = 300, "distance_max"),
            ("rayon au-delà des parties", root => root["hands"]!["body_radius"] = 90, "body_radius"),
            ("nombre démesuré", root => root["hands"]!["max_alive"] = 1e6, "max_alive"),
            ("son absent", root => root["audio"]!["defeated"] = "sfx_inexistant", "sfx_inexistant"),
            ("famille inconnue", root => root["fx_family"] = "plasma", "plasma"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            JsonObject root = JsonNode.Parse(valid)!.AsObject();
            mutate(root);
            bool rejected = !IndicibleConfig.TryParse(root.ToJsonString(), 50f, out IndicibleConfig config, out string error);
            Check(rejected && config == null && error.Contains(expected), $"configuration invalide refusée ({label}) : {error}");
        }
    }

    /// <summary>
    /// Joueur immobile, armes coupées, dans la vraie scène de run sans flux de créatures : chaque éclair et chaque prise
    /// tombent sur lui ; les dégâts aux mains sont portés à la main pour franchir les seuils, puis vider la réserve.
    /// </summary>
    private async Task CheckFight()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GameManager manager = GetNode<GameManager>("/root/GameManager");
        manager.RunSeed = 221092026;
        manager.SelectedCharacterId = "traqueur";
        World.WorldSetup main = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<World.WorldSetup>();
        GetTree().Root.AddChild(main);
        GetTree().CurrentScene = main;
        ulong deadline = Time.GetTicksMsec() + 120000;
        // Sans rendu, céder 1 ms laisse du CPU au thread de génération (même attente que l'intégration des déplacements).
        while ((!main.IsWorldReady || GetTree().Paused || manager.CurrentState != GameManager.GameState.Run) && Time.GetTicksMsec() < deadline)
        {
            OS.DelayMsec(1);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        Check(main.IsWorldReady && !GetTree().Paused, "scène de run prête");
        Player player = main.GetNode<Player>("Player");
        SpawnManager spawner = main.GetNode<SpawnManager>("SpawnManager");
        spawner.ProcessMode = ProcessModeEnum.Disabled;
        player.IsAIControlled = true;
        player.DisableDefenseForTests();
        foreach (Node child in player.GetChildren())
            if (child is Timer weaponTimer)
                weaponTimer.Stop();
        typeof(Player).GetField("_currentHp", Private)!.SetValue(player, 1_000_000f);
        CanvasModulate night = main.GetNode<CanvasModulate>("CanvasModulate");
        Color day = night.Color;

        EventBus bus = GetNode<EventBus>("/root/EventBus");
        int kills = 0;
        EventBus.EnemyKilledEventHandler onKill = (id, _) => { if (id == EnemyGrammar.FinalBossId) kills++; };
        bus.EnemyKilled += onKill;
        ScoreManager score = main.GetNodeOrNull<ScoreManager>("ScoreManager");
        int scoreBefore = score?.CurrentScore ?? 0;

        Indicible boss = new() { Name = "IndicibleBoss" };
        main.AddChild(boss);
        Check(boss.Initialize(1f, 1f, player, spawner), "boss initialisé");
        Check(boss.Health.Max == EnemyDataLoader.Get(EnemyGrammar.FinalBossId).Stats.Hp, $"réserve : {boss.Health.Max} PV (fiche × 1)");
        Vector2 start = player.GlobalPosition;
        for (int frame = 0; frame < 60 * 14; frame++)
        {
            player.AIInputOverride = Vector2.Zero;
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        }
        Check(night.Color.V < day.V * 0.8f, $"la nuit tombe : {night.Color} (jour {day})");
        Check(player.GlobalPosition.DistanceTo(start) > 40f, $"le vent fait dériver le joueur immobile : {player.GlobalPosition.DistanceTo(start):F0} px en 14 s");
        Check(boss.LightningStrikes >= 3 && boss.LightningHits >= boss.LightningStrikes - 1,
            $"éclairs sur le joueur immobile : {boss.LightningHits}/{boss.LightningStrikes}");
        Check(boss.HandsRaised >= 3 && boss.Grabs >= 1 && boss.GrabHits >= 1, $"mains : {boss.HandsRaised} levées, {boss.GrabHits}/{boss.Grabs} prises portées");

        int lightningBefore = boss.LightningStrikes;
        List<int> phases = new();
        boss.Health.PhaseReached += phases.Add;
        Enemy hand = FirstHand(boss);
        Check(hand != null, "une main est une cible");
        hand?.TakeDamage(boss.Health.Max * 0.4f);
        Check(phases.Count == 1 && Mathf.IsEqualApprox(boss.Health.Ratio, 0.6f, 0.01f), $"coup sur une main : réserve à {boss.Health.Ratio:P0}, phase {phases.Count + 1}");
        Check(boss.Tide.IsActive && player.ExternalDrift == Vector2.Zero, "sous le premier seuil : le vent tombe, la marée monte");
        for (int frame = 0; frame < 60 * 4; frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        Check(boss.LightningStrikes == lightningBefore || boss.LightningStrikes == lightningBefore + 1,
            $"plus d'éclairs pendant la marée ({boss.LightningStrikes - lightningBefore} de plus)");
        hand = FirstHand(boss);
        hand?.TakeDamage(boss.Health.Max * 0.3f);
        Check(phases.Count == 2 && boss.Wave.IsActive && !boss.Tide.IsActive, "sous le second seuil : la mer se retire, la seconde vague vient");
        for (int frame = 0; frame < 60 * 12 && boss.Wave.Waves == 0; frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        Check(boss.Wave.Waves >= 1, $"une vague annoncée ({boss.Wave.Waves})");
        for (int frame = 0; frame < 60 * 6 && boss.Wave.WindowPart == null; frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        Enemy window = boss.Wave.WindowPart;
        Check(window != null && window.BodyRadius > 40f, $"après la vague, l'Indicible se découvre (rayon {window?.BodyRadius ?? 0f})");
        if (window != null)
        {
            float before = boss.Health.Current;
            window.TakeDamage(100f);
            Check(Mathf.IsEqualApprox(before - boss.Health.Current, 200f, 1f), $"découvert, il prend double : 100 → {before - boss.Health.Current:F0}");
        }
        hand = FirstHand(boss);
        hand?.TakeDamage(boss.Health.Max * 2f);
        for (int frame = 0; frame < 120; frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        Check(boss.IsDefeated && kills == 1, $"vaincu une fois : {kills} élimination(s) « indicible »");
        Check(string.Join(",", phases) == "1,2", $"seuils dans l'ordre : {string.Join(",", phases)}");
        Check(score == null || score.CurrentScore - scoreBefore >= 5000, $"récompense : +{(score?.CurrentScore ?? 0) - scoreBefore} points");
        Check(player.ExternalDrift == Vector2.Zero, "le vent tombe avec lui");
        bus.EnemyKilled -= onKill;
        main.QueueFree();
    }

    private static Enemy FirstHand(Indicible boss)
    {
        foreach (Enemy part in boss.Health.Parts)
            if (part.IsActive && !part.IsDying)
                return part;
        return null;
    }

    private void Check(bool passed, string message)
    {
        if (!passed)
            _failures++;
        GD.Print($"[IndicibleRegression] {(passed ? "PASS" : "FAIL")} {message}");
    }
}
