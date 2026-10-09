using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>Contrats B0 sur les vrais ennemis/joueur, sans acquisition ni activation des nouveaux perks.</summary>
public partial class PerkContractsRegression : Node2D
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private int _failures;
    private Player _player;
    private EventBus _events;
    private WeaponInstance _weapon;

    public override async void _Ready()
    {
        try
        {
            GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Run);
            _events = GetNode<EventBus>("/root/EventBus");
            _player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
            AddChild(_player);
            _player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
            _player.IsAIControlled = true;
            _player.IsGodMode = false;
            _player.SetPhysicsProcess(false);
            _weapon = _player.WeaponSlots[0];

            CheckCatalogue();
            CheckEnemyDamage();
            await CheckPersistentSources();
            CheckHealth();
            GD.Print($"[PerkContractsRegression] RESULT failures={_failures}");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(2);
        }
    }

    private void CheckCatalogue()
    {
        Check(PerkSpecializationDataLoader.Load(), "Catalogue chargeable indépendamment des anciens Dons");
        IReadOnlyList<PerkSpecializationData> perks = PerkSpecializationDataLoader.GetAll();
        PerkSpecializationConfig config = PerkSpecializationDataLoader.Config;
        Check(perks.Count == 9 && config.MaxEquipped == 4 && config.OfferSize == 3
            && config.FirstOfferFamilies.Count == 3, "Catalogue V1 : neuf règles, quatre emplacements, première offre composée");
        using Godot.FileAccess file = Godot.FileAccess.Open("res://data/progression/perk_specializations.json", Godot.FileAccess.ModeFlags.Read);
        string source = file.GetAsText();
        JsonNode root = JsonNode.Parse(source);
        JsonArray definitions = root["perks"].AsArray();
        JsonNode extra = definitions[0].DeepClone();
        extra["id"] = "future_specialization";
        extra["effect"] = "future_rule";
        definitions.Add(extra);
        Check(PerkSpecializationDataLoader.TryParse(root.ToJsonString(), out PerkSpecializationCatalogue expanded, out _)
            && expanded.Perks.Count == 10, "Une dixième définition ne demande pas de modifier le lecteur");
        extra["id"] = definitions[0]["id"].GetValue<string>();
        Check(!PerkSpecializationDataLoader.TryParse(root.ToJsonString(), out _, out _), "Identifiant dupliqué refusé");
        foreach (string field in new[] { "max_level", "rarity", "max_stacks" })
        {
            root = JsonNode.Parse(source);
            root["perks"][0][field] = 2;
            Check(!PerkSpecializationDataLoader.TryParse(root.ToJsonString(), out _, out _), $"Contrat sans {field} imposé");
        }
        root = JsonNode.Parse(source);
        root["perks"][0]["parameters"]["overflowing"] = 1e39;
        Check(!PerkSpecializationDataLoader.TryParse(root.ToJsonString(), out _, out _), "Coefficient dépassant float refusé");
        bool immutable = false;
        try { ((IList<PerkSpecializationData>)perks).Clear(); }
        catch (NotSupportedException) { immutable = true; }
        Check(immutable && perks.Count == 9, "Collection exposée effectivement immuable");
    }

    private Enemy SpawnEnemy()
    {
        Enemy enemy = GD.Load<PackedScene>("res://scenes/enemies/Enemy.tscn").Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get("rodeur"), 1f, 1f);
        enemy.SetTicking(false);
        enemy.Position = new Vector2(1000f, 1000f);
        return enemy;
    }

    private void CheckEnemyDamage()
    {
        AttackContext attack = _player.BeginAttack(_weapon, 12f);
        DamageResult calculated = DamageResult.Resolve(new EnemyLife(1, 1), attack, 10f, 8f, 5f);
        Check(calculated.Fatal && calculated.HpLost == 10f && calculated.NativeOverkill == 0f,
            "Un report qui achève ne crée pas de surkill natif");
        calculated = DamageResult.Resolve(new EnemyLife(1, 1), attack, 10f, 30f, 8f, 0.5f);
        Check(calculated.NativeDamage == 15f && calculated.CarriedDamage == 4f && calculated.NativeOverkill == 5f,
            "Défense appliquée une fois aux contributions distinctes");

        Enemy enemy = SpawnEnemy();
        EnemyLife life = enemy.Life;
        enemy.Reset();
        enemy.Initialize(EnemyDataLoader.Get("rodeur"), 1f, 1f);
        enemy.SetTicking(false);
        Check(enemy.Life.InstanceId == life.InstanceId && enemy.Life.Generation != life.Generation,
            "Le pool garde le nœud, pas l'identité de sa vie");
        enemy.ApplyDisorient(3f, attack);
        enemy.ApplyDisorient(0.5f, attack.As(DamageKind.Passive));
        typeof(Enemy).GetMethod("ProcessDisorient", Private).Invoke(enemy, new object[] { 0.6f, false });
        Check(!enemy.DisorientationControl.CanPropagate(attack.OwnerId), "Un contrôle natif raccourci ne survit pas à son expiration réelle");
        enemy.ApplySlow(0.9f, 0.5f, attack.As(DamageKind.Passive));
        Check(!enemy.SlowControl.CanPropagate(attack.OwnerId), "Le contrôle issu d'un ricochet passif n'est pas natif");
        enemy.ApplySlow(0.5f, 2f, attack, ControlOrigin.Propagated);
        enemy.ApplyDisorient(3f, attack, ControlOrigin.Propagated);
        Check(!enemy.SlowControl.CanPropagate(attack.OwnerId) && !enemy.DisorientationControl.CanPropagate(attack.OwnerId),
            "Un contrôle transmis conserve son arme sans devenir transmissible");
        enemy.ApplySlow(0.8f, 1f, attack);
        enemy.ApplyDisorient(1f, attack);
        Check(enemy.SlowControl.Strength == 0.8f && enemy.SlowControl.Remaining == 1f,
            "Un faible contrôle natif ne récupère ni puissance ni durée du contrôle transmis");
        enemy.ApplySlow(0.9f, 0.5f, attack, ControlOrigin.Propagated);
        enemy.ApplyDisorient(0.5f, attack, ControlOrigin.Propagated);
        Check(enemy.SlowControl.CanPropagate(attack.OwnerId) && enemy.SlowControl.Strength == 0.8f
            && enemy.DisorientationControl.Remaining == 1f,
            "Un faible contrôle transmis ne retire ni ne raccourcit la contribution native");
        int kills = 0;
        EnemyKillResult last = default;
        void OnKill(EnemyKillResult result) { kills++; last = result; }
        _events.EnemyKillResolved += OnKill;
        DamageResult hit = enemy.TakeDamage(10000f, showImpact: false, source: attack);
        DamageResult ignored = enemy.TakeDamage(10000f, showImpact: false, source: attack);
        _events.EnemyKillResolved -= OnKill;
        Check(hit.Fatal && hit.Source == attack && hit.Target == enemy.Life && !ignored.Applied && kills == 1,
            "Une seule élimination attribuée par vie, même avec impacts supplémentaires");
        Check(last.Slow.CanPropagate(attack.OwnerId) && last.Disorientation.CanPropagate(attack.OwnerId)
            && !last.Slow.CanPropagate(attack.OwnerId + 1), "Contrôles capturés avant mort avec propriétaire et durée");
        Check(hit.NativeOverkill > 0f && hit.HpLost == hit.HpBefore, "Dommage réel séparé du surkill");
        enemy.QueueFree();
    }

    private async Task CheckPersistentSources()
    {
        AttackContext attack = _player.BeginAttack(_weapon, 10f);
        Enemy groundTarget = SpawnEnemy();
        ulong spawnFrame = Engine.GetProcessFrames();
        do { await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
        while (Engine.GetProcessFrames() <= spawnFrame);
        Check(groundTarget.CrowdSlot >= 0, "La fixture de flaque est recensée par l'index de la foule");
        GroundFire fire = new();
        AttackContext groundSource = attack.As(DamageKind.DamageOverTime);
        DamageResult groundHit = default;
        void OnDamage(DamageResult result) { if (result.Target == groundTarget.Life) groundHit = result; }
        _events.EnemyDamageResolved += OnDamage;
        fire.Add(groundTarget.GlobalPosition, 1f, 1f, 20f, 0.5f, groundSource);
        fire.Process(0.6f);
        // Le feu pose une Brûlure (planche 05, R1) : le dégât arrive au tic de la créature.
        typeof(Enemy).GetMethod("ProcessIgnite", Private).Invoke(groundTarget, new object[] { 0.1f });
        _events.EnemyDamageResolved -= OnDamage;
        Check(groundHit.Applied && groundHit.Source == groundSource, "La flaque garde le propriétaire du projectile qui l'a créée");
        groundTarget.QueueFree();
        Check(attack.LaunchId != _player.BeginAttack(_weapon, 10f).LaunchId, "Lancements successifs distincts");
        foreach (string effect in new[] { "Ignite", "Bleed" })
        {
            Enemy enemy = SpawnEnemy();
            if (effect == "Ignite") enemy.ApplyIgnite(10000f, 2f, attack);
            else enemy.ApplyBleed(10000f, 2f, attack);
            EnemyKillResult last = default;
            void OnKill(EnemyKillResult result) { last = result; }
            _events.EnemyKillResolved += OnKill;
            typeof(Enemy).GetMethod("Process" + effect, Private).Invoke(enemy, new object[] { 1f });
            _events.EnemyKillResolved -= OnKill;
            Check(last.Damage.Source.OwnerId == attack.OwnerId && last.Damage.Source.Weapon == _weapon
                && last.Damage.Source.LaunchId == attack.LaunchId && last.Damage.Source.Kind == DamageKind.DamageOverTime
                && !last.Damage.Source.IsDirectWeapon, $"{effect} conserve sa source, sans devenir un impact direct");
            enemy.QueueFree();
        }
    }

    private void CheckHealth()
    {
        // Plus de bouclier de départ (plan 23 R1) : celui d'un objet (Écusson de pompier) encaisse le premier coup.
        _player.ApplyPerkModifier("shield", 10f, "additive");
        PlayerDamageResult shield = _player.TakeDamage(5f);
        Check(shield.Applied && shield.ShieldAbsorbed && shield.HpLost == 0f && !shield.CanRecover,
            "Un coup sur le bouclier n'est pas une blessure récupérable");
        _player.DisableDefenseForTests();
        PlayerDamageResult injury = _player.TakeDamage(10f);
        Check(injury.CanRecover && injury.HpLost > 0f, "Blessure réelle après défense identifiable");
        HealingResult heal = _player.Heal(10000f);
        Check(heal.HpRestored == injury.HpLost && heal.Excess > 0f && heal.CanStoreExcess,
            "Soin : PV rendus et excédent séparés");
        HealingResult restored = _player.Heal(5f, HealingKind.PerkRecovery);
        Check(!restored.CanStoreExcess && restored.HpRestored == 0f, "Une restitution ne recharge pas Prévoyance");
        HealingResult regen = default;
        void OnHeal(HealingResult result) { regen = result; }
        _events.PlayerHealingResolved += OnHeal;
        typeof(Player).GetMethod("ApplyRegen", Private).Invoke(_player, new object[] { 1f });
        _events.PlayerHealingResolved -= OnHeal;
        Check(regen.Kind == HealingKind.Regeneration && regen.HpRestored == 0f && regen.Excess > 0f,
            "La régénération à pleine vie expose son excédent sans changer les PV");
        PlayerDamageResult erasure = _player.TakeErasureDamage(3f);
        Check(erasure.HpLost == 3f && !erasure.CanRecover, "Le Néant reste une origine non récupérable");
        PlayerDamageResult fatal = _player.TakeDamage(10000f);
        Check(fatal.Fatal && !fatal.CanRecover, "Un coup fatal reste fatal et non récupérable dans le contrat");
    }

    private void Check(bool condition, string message)
    {
        if (!condition) _failures++;
        GD.Print($"[PerkContractsRegression] {(condition ? "PASS" : "FAIL")} {message}");
    }
}
