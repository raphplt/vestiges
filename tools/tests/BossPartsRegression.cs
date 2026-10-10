using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// Plan 07 B1a : parties de boss. La fiche commune et ses réglages sont lus et contrôlés ; une partie à PV propres
/// tombe seule et rend la main au boss une fois, sans élimination ; des parties en réserve commune partagent
/// leurs PV, brûlure comprise ; les seuils de phase tombent une fois, dans l'ordre ; une partie ne bouge ni ne frappe.
/// B1b : la barre de boss suit la réserve et ses rencontres successives. B2 : réglages et disposition de la Barrière.
/// </summary>
public partial class BossPartsRegression : Node2D
{
    private static readonly PackedScene EnemyScene = GD.Load<PackedScene>("res://scenes/enemies/Enemy.tscn");
    private Player _player;
    private EventBus _bus;
    private int _kills;
    private int _failures;

    public override async void _Ready()
    {
        try
        {
            GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Run);
            _bus = GetNode<EventBus>("/root/EventBus");
            _bus.EnemyKilled += OnEnemyKilled;
            _player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
            AddChild(_player);
            _player.InitializeCharacter(CharacterDataLoader.Get("vagabond"));
            _player.DisableDefenseForTests();
            _player.SetPhysicsProcess(false);
            _player.IsAIControlled = true;
            foreach (Node child in _player.GetChildren())
                if (child is Timer weaponTimer)
                    weaponTimer.Stop();
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            CheckConfig();
            CheckPartSheet();
            await CheckOwnHpParts();
            await CheckSharedReserve();
            await CheckPartStaysPut();
            await CheckBar();
            CheckBarrierConfig();
        }
        catch (Exception ex)
        {
            Check(false, $"exception {ex.GetType().Name} : {ex.Message}");
        }
        _bus.EnemyKilled -= OnEnemyKilled;
        GD.Print($"[BossPartsRegression] RESULT failures={_failures}");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void OnEnemyKilled(string enemyId, Vector2 position) => _kills++;

    private void CheckConfig()
    {
        Check(BossPartsConfig.TryLoad(out BossPartsConfig config, out string error), $"réglages du dépôt chargés {error}");
        Check(config?.MaxBodyRadius == 50f, $"rayon de touche au plus : {config?.MaxBodyRadius}");
        string valid = FileAccess.GetFileAsString("res://data/scaling/boss_parts.json");
        (string Label, Action<JsonObject> Mutate, string Expected)[] cases =
        {
            ("section absente", root => root.Remove("parts"), "section parts absente"),
            ("rayon absent", root => root["parts"]!.AsObject().Remove("max_body_radius"), "max_body_radius"),
            ("rayon nul", root => root["parts"]!["max_body_radius"] = 0, "max_body_radius"),
            ("texte au lieu d'un nombre", root => root["parts"]!["max_body_radius"] = "cinquante", "max_body_radius"),
            ("clé inconnue", root => root["parts"]!["max_body_radiu"] = 40, "inconnue"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            JsonObject root = JsonNode.Parse(valid)!.AsObject();
            mutate(root);
            bool rejected = !BossPartsConfig.TryParse(root.ToJsonString(), out BossPartsConfig parsed, out string reason);
            Check(rejected && parsed == null && reason.Contains(expected), $"réglages invalides refusés ({label}) : {reason}");
        }
    }

    private void CheckPartSheet()
    {
        EnemyData data = EnemyDataLoader.Get(EnemyGrammar.BossPartId);
        Check(data is { Behavior: EnemyBehavior.BossPart, Tier: EnemyTier.Boss, CombatType: EnemyCombatType.Boss },
            "fiche boss_part : comportement boss_part, rang boss");
        Check(data is { Stats.Speed: 0f, Stats.Damage: 0f, Stats.XpReward: 0f } && data.GetStat("spawn_weight") == 0f,
            "fiche boss_part : immobile, sans dégâts, sans XP, jamais tirée par le flux");
    }

    /// <summary>Deux battants de 100 PV, un seuil à la moitié : chacun tombe seul, l'excès d'un coup n'est pas compté.</summary>
    private async Task CheckOwnHpParts()
    {
        BossHealth boss = new("Barrière d'essai", 0f, new[] { 0.5f });
        Enemy left = SpawnPart(new Vector2(120f, 0f));
        Enemy right = SpawnPart(new Vector2(220f, 0f));
        boss.AddPart(left, 40f, 100f);
        boss.AddPart(right, 40f, 100f);
        List<string> events = new();
        boss.PartHit += (part, lost) => events.Add($"hit {(part == left ? "g" : "d")} {lost:0}");
        boss.PartBroken += part => events.Add($"broken {(part == left ? "g" : "d")}");
        boss.PhaseReached += phase => events.Add($"phase {phase}");
        boss.Depleted += () => events.Add("depleted");

        Check(boss.Max == 200f && boss.Current == 200f && boss.OwnPartCount == 2, $"réserve : {boss.Current}/{boss.Max}, {boss.OwnPartCount} crans");
        Check(left.BodyRadius == 40f && Enemy.LargestBodyRadius >= 40f, $"rayon de touche : {left.BodyRadius} (marge des tirs {Enemy.LargestBodyRadius})");
        Check(left.Boss == boss && left.IsBoundToOwnHp, "partie rattachée à son boss, PV propres");

        int kills = _kills;
        left.TakeDamage(60f);
        Check(boss.Current == 140f && left.HpRatio == 0.4f && right.HpRatio == 1f, $"coup de 60 : réserve {boss.Current}, gauche {left.HpRatio:0.00}, droite {right.HpRatio:0.00}");
        left.TakeDamage(60f);
        Check(left.IsDying && boss.Current == 100f && boss.BrokenPartCount == 1, $"battant brisé, excès ignoré : réserve {boss.Current}");
        Check(boss.Phase == 1 && !boss.IsDepleted, $"seuil de la moitié franchi une fois (phase {boss.Phase})");
        left.TakeDamage(60f);
        Check(boss.Current == 100f, "un battant brisé ne prend plus de coups");
        right.TakeDamage(500f);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        string sequence = string.Join(", ", events);
        Check(sequence == "hit g 60, hit g 40, phase 1, broken g, hit d 100, broken d, depleted", $"ordre des événements : {sequence}");
        Check(_kills == kills, $"aucune élimination de créature émise ({_kills - kills})");
    }

    /// <summary>Trois mains sur une réserve de 300 : un coup sur l'une entame les trois, la brûlure aussi.</summary>
    private async Task CheckSharedReserve()
    {
        BossHealth boss = new("Indicible d'essai", 300f, new[] { 2f / 3f, 1f / 3f });
        Enemy[] hands = { SpawnPart(new Vector2(-100f, 0f)), SpawnPart(new Vector2(0f, 100f)), SpawnPart(new Vector2(100f, 100f)) };
        foreach (Enemy hand in hands)
            boss.AddPart(hand, 24f);
        List<int> phases = new();
        int depleted = 0;
        boss.PhaseReached += phases.Add;
        boss.Depleted += () => depleted++;
        Check(boss.Max == 300f && boss.OwnPartCount == 0 && !hands[0].IsBoundToOwnHp, "réserve commune de 300, sans cran");

        hands[0].TakeDamage(50f);
        Check(Array.TrueForAll(hands, hand => Mathf.IsEqualApprox(hand.HpRatio, 250f / 300f)), $"coup de 50 : chaque main à {hands[1].HpRatio:0.000}");
        hands[1].ApplyIgnite(60f, 1f);
        for (int i = 0; i < 70; i++)
            hands[1].PhysicsTick(1.0 / 60.0);
        Check(Mathf.Abs(boss.Current - 190f) < 1.5f && Mathf.IsEqualApprox(hands[2].HpRatio, boss.Current / 300f),
            $"brûlure de 60 PV en 1 s reportée : réserve {boss.Current:0.0}");
        Check(phases.Count == 1 && phases[0] == 1, $"premier seuil (2/3) franchi : {string.Join(",", phases)}");
        hands[2].TakeDamage(1000f);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        Check(boss.IsDepleted && depleted == 1 && hands[2].IsDying, "réserve vidée par la main frappée : boss vaincu une fois");
        Check(string.Join(",", phases) == "1,2", $"seuils franchis dans l'ordre, une fois chacun : {string.Join(",", phases)}");
        Check(!hands[0].IsDying && !hands[1].IsDying, "les autres mains restent au boss, qui les retire");
        foreach (Enemy hand in hands)
            hand.Vanish();
    }

    private async Task CheckPartStaysPut()
    {
        BossHealth boss = new("Immobile", 0f);
        Vector2 position = _player.GlobalPosition + new Vector2(20f, 0f);
        Enemy part = SpawnPart(position);
        boss.AddPart(part, 30f, 1000f);
        float hp = _player.CurrentHp;
        part.ApplyKnockback(Vector2.Right, 60f);
        for (int i = 0; i < 120; i++)
        {
            part.PhysicsTick(1.0 / 60.0);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        }
        Check(part.GlobalPosition == position, $"collée au joueur, poussée : la partie ne bouge pas ({part.GlobalPosition - position})");
        Check(_player.CurrentHp == hp, "pas de coup au contact");
        part.Reset();
        Check(part.Boss == null && boss.Parts.Count == 0, "rendue au pool : détachée de son boss");
        part.QueueFree();
    }

    /// <summary>B1b : la barre suit la réserve, un cran par battant, et une fin tardive ne ferme pas la rencontre suivante.</summary>
    private async Task CheckBar()
    {
        Vestiges.UI.BossHealthBar bar = new();
        AddChild(bar);
        BossHealth boss = new("Barrière d'essai", 0f);
        Enemy[] gates = { SpawnPart(new Vector2(300f, 0f)), SpawnPart(new Vector2(400f, 0f)), SpawnPart(new Vector2(500f, 0f)) };
        foreach (Enemy gate in gates)
            boss.AddPart(gate, 40f, 100f);
        int notches = -1;
        EventBus.BossEncounterStartedEventHandler onStarted = (_, _, _, count) => notches = count;
        _bus.BossEncounterStarted += onStarted;
        boss.ShowBar(_bus);
        _bus.BossEncounterStarted -= onStarted;
        int first = bar.EncounterId;
        Check(first != 0 && notches == 3 && bar.ShownRatio == 1f, $"barre ouverte : rencontre {first}, {notches} crans");
        gates[0].TakeDamage(150f);
        Check(Mathf.IsEqualApprox(bar.ShownRatio, 200f / 300f), $"battant brisé : barre à {bar.ShownRatio:0.000}");

        BossBarFeed souverain = new(_bus, "Souverain d'essai", 400f);
        Check(bar.EncounterId == souverain.Id && bar.ShownRatio == 1f, "une nouvelle rencontre prend la barre");
        boss.EndEncounter();
        Check(bar.EncounterId == souverain.Id, "la fin de la rencontre précédente ne la ferme pas");
        souverain.Update(100f, 400f);
        souverain.Update(100f, 400f);
        Check(bar.ShownRatio == 0.25f, $"PV du Souverain : {bar.ShownRatio}");
        souverain.End(true);
        souverain.End(false);
        Check(!souverain.IsOpen && bar.ShownRatio == 0f, "Souverain abattu : barre vidée, fermée une seule fois");
        for (int i = 0; i < 70; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(!bar.Visible && bar.EncounterId == 0, "barre retirée après son fondu");
        foreach (Enemy gate in gates)
            gate.Vanish();
        bar.QueueFree();
    }

    /// <summary>B2 : réglages de la Barrière lus et contrôlés ; battants selon les Mémoriaux ; disposition des sprites.</summary>
    private void CheckBarrierConfig()
    {
        Check(BarrierConfig.TryLoad(out BarrierConfig config, out string error), $"réglages de la Barrière chargés {error}");
        Check(config.AppearAtSec == 600f && config.CrowdDensity == 0.5f, $"Barrière à {config.AppearAtSec} s, foule ×{config.CrowdDensity}");
        Check(config.LeavesFor(0) == 1 && config.LeavesFor(2) == 3 && config.LeavesFor(9) == 5, "battants : 1, puis un par Mémorial, 5 au plus");
        Check(config.LeafHpFor(1) == 45000f && config.LeafHpFor(3) * 3 == 75000f && config.LeafHpFor(5) * 5 == 105000f,
            $"réserve : {config.LeafHpFor(1):F0}, {config.LeafHpFor(3) * 3:F0}, {config.LeafHpFor(5) * 5:F0} PV pour 1, 3 et 5 battants");
        foreach (string suffix in new[] { "h", "v" })
            Check(Barrier.TryReadLayout(suffix, out Vector2 stride, out Vector2 wing, out _) && stride.Length() > 30f && wing.Length() > 10f,
                $"disposition {suffix} : pas de pilier {stride}, pas d'aile {wing}");
        string valid = FileAccess.GetFileAsString("res://data/events/barrier.json");
        (string Label, Action<JsonObject> Mutate, string Expected)[] cases =
        {
            ("section absente", root => root.Remove("attacks"), "section attacks absente"),
            ("clé inconnue", root => root["leaves"]!["hp_max"] = 3, "inconnue"),
            ("PV nuls", root => root["leaves"]!["hp_per_leaf"] = 0, "hp_per_leaf"),
            ("rayon au-delà des parties", root => root["leaves"]!["body_radius"] = 80, "body_radius"),
            ("max sous la base", root => root["leaves"]!["max"] = 0, "max"),
            ("coffre inconnu", root => root["rewards"]!["last_chest"] = "chest_mythique", "chest_mythique"),
            ("son absent", root => root["audio"]!["rise"] = "sfx_inexistant", "sfx_inexistant"),
            ("famille inconnue", root => root["attacks"]!["fx_family"] = "plasma", "plasma"),
            ("arc démesuré", root => root["attacks"]!["chain"]!["arc_deg"] = 270, "arc_deg"),
            ("densité nulle", root => root["fight"]!["crowd_density"] = 0, "crowd_density"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            JsonObject root = JsonNode.Parse(valid)!.AsObject();
            mutate(root);
            bool rejected = !BarrierConfig.TryParse(root.ToJsonString(), 50f, out BarrierConfig parsed, out string reason);
            Check(rejected && parsed == null && reason.Contains(expected), $"Barrière invalide refusée ({label}) : {reason}");
        }
    }

    private Enemy SpawnPart(Vector2 position)
    {
        Enemy part = EnemyScene.Instantiate<Enemy>();
        AddChild(part);
        part.Initialize(EnemyDataLoader.Get(EnemyGrammar.BossPartId), 1f, 1f);
        part.SetTicking(false);
        part.GlobalPosition = position;
        return part;
    }

    private void Check(bool passed, string message)
    {
        if (!passed)
            _failures++;
        GD.Print($"[BossPartsRegression] {(passed ? "PASS" : "FAIL")} {message}");
    }
}
