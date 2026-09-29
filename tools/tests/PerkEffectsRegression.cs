using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Tests;

/// <summary>
/// Effets des perks B2 (plan 05 §3, §8.3) sur le vrai joueur et les résultats de combat publiés par l'EventBus :
/// chaque cas limite des fiches, sans passer par l'écran de niveau.
/// </summary>
public partial class PerkEffectsRegression : Node2D
{
    private int _failures;
    private Player _player;
    private EventBus _events;
    private readonly List<SpecializationGauge> _gauges = new();

    public override void _Ready()
    {
        try
        {
            GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Run);
            _events = GetNode<EventBus>("/root/EventBus");
            _events.SpecializationGaugeChanged += gauge => _gauges.Add(gauge);
            PerkSpecializationDataLoader.Load();

            CheckOverhealReserve();
            CheckRally();
            CheckSurvivalOrder();
            CheckOverflow();
            GD.Print($"[PerkEffectsRegression] RESULT failures={_failures}");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(2);
        }
    }

    /// <summary>Prévoyance : charge initiale, restitution plafonnée, excédents seuls, Néant et coup fatal exclus.</summary>
    private void CheckOverhealReserve()
    {
        Setup("overheal_reserve");
        OverhealReserve reserve = _player.SpecializationRuntime.Reserve;
        float max = _player.EffectiveMaxHp;
        Check(Near(reserve.Stock, max * 0.2f) && LastGauge("overheal_reserve") is { } initial && Near(initial.Value, reserve.Stock),
            $"Prévoyance : réserve pleine à l'acquisition ({reserve.Stock:0.#} / {reserve.Capacity:0.#}) et publiée");

        _player.TakeDamage(10f);
        Check(Near(_player.CurrentHp, max) && Near(reserve.Stock, max * 0.2f - 10f),
            $"Coup de 10 : PV rendus ({_player.CurrentHp:0.#}), réserve débitée ({reserve.Stock:0.#})");

        float stock = reserve.Stock;
        _player.TakeDamage(30f);
        Check(Near(_player.CurrentHp, max - 30f + stock) && Near(reserve.Stock, 0f),
            $"Coup plus fort que la réserve : seul le stock revient ({_player.CurrentHp:0.#} PV)");

        float missing = max - _player.CurrentHp;
        _player.Heal(missing + 6f);
        Check(Near(reserve.Stock, 6f), $"Soin excédentaire : seul l'excédent entre en réserve ({reserve.Stock:0.#})");
        _player.Heal(4f, HealingKind.Regeneration);
        Check(Near(reserve.Stock, 10f), "Régénération à PV pleins : l'excédent remplit la réserve");
        _player.Heal(50f, HealingKind.PerkRecovery);
        _player.Heal(50f);
        Check(Near(reserve.Stock, reserve.Capacity), "Restitution de perk sans recharge ; la réserve reste plafonnée");

        float before = _player.CurrentHp;
        _player.TakeErasureDamage(10f);
        Check(Near(_player.CurrentHp, before - 10f) && Near(reserve.Stock, reserve.Capacity), "Néant : aucune restitution");

        _player.ApplyPerkModifier("max_hp", -50f, "additive");
        _player.Heal(1f);
        Check(Near(reserve.Stock, _player.EffectiveMaxHp * 0.2f), $"PV max en baisse : réserve tronquée ({reserve.Stock:0.#})");
        _player.ApplyPerkModifier("max_hp", 50f, "additive");
        float truncated = reserve.Stock;
        _player.Heal(0.01f, HealingKind.PerkRecovery);
        Check(Near(reserve.Stock, truncated) && reserve.Capacity > truncated, "PV max en hausse : capacité agrandie, réserve non remplie");

        _player.Heal(_player.EffectiveMaxHp);
        float storedBeforeDeath = reserve.Stock;
        _player.TakeDamage(_player.EffectiveMaxHp * 3f);
        Check(_player.IsDead && Near(reserve.Stock, storedBeforeDeath), "Coup fatal : la réserve ne sauve pas et reste intacte");
    }

    /// <summary>Reprise : budget sur la perte, plafonds, crédit par élimination attribuée, échéance non repoussée.</summary>
    private void CheckRally()
    {
        Setup("rally");
        RallyWindow rally = _player.SpecializationRuntime.Rally;
        float max = _player.EffectiveMaxHp;

        float wound = max * 0.25f;
        float perKill = max * 0.02f;
        _player.TakeDamage(wound);
        Check(rally.IsOpen && Near(rally.Recoverable, wound * 0.4f) && Near(rally.Remaining, 4f),
            $"Coup de {wound:0.#} : fenêtre de 4 s, 40 % récupérables ({rally.Recoverable:0.#})");

        Kill(0);
        Check(Near(_player.CurrentHp, max - wound), "Élimination sans attribution au joueur : aucun crédit");
        Kill(_player.GetInstanceId());
        Check(Near(_player.CurrentHp, max - wound + perKill) && Near(rally.Recoverable, wound * 0.4f - perKill),
            $"Élimination attribuée : +2 % des PV max ({_player.CurrentHp:0.#})");

        float budget = rally.Recoverable;
        _player.Heal(2f);
        Check(Near(rally.Recoverable, budget - 2f), $"Soin extérieur de 2 : budget réduit d'autant ({rally.Recoverable:0.#})");

        Advance(2f);
        _player.TakeDamage(max * 0.5f);
        Check(!_player.IsDead && Near(rally.Remaining, 2f) && Near(rally.Recoverable, max * 0.2f),
            $"Second coup : budget plafonné à 20 % ({rally.Recoverable:0.#}), échéance inchangée ({rally.Remaining:0.#} s)");

        Advance(2.1f);
        float before = _player.CurrentHp;
        Kill(_player.GetInstanceId());
        Check(!rally.IsOpen && Near(rally.Recoverable, 0f) && Near(_player.CurrentHp, before)
            && LastGauge("rally") is { Value: 0f }, "Fenêtre expirée : budget perdu, plus de crédit, jauge éteinte");

        _player.TakeErasureDamage(10f);
        Check(!rally.IsOpen, "Néant : aucune fenêtre");

        Setup("rally");
        rally = _player.SpecializationRuntime.Rally;
        _player.TakeDamage(10f);
        for (int i = 0; i < 4; i++)
            Kill(_player.GetInstanceId());
        Check(Near(_player.CurrentHp, _player.EffectiveMaxHp - 6f) && Near(rally.Recoverable, 0f),
            $"Les crédits s'arrêtent au budget ({_player.CurrentHp:0.#} PV)");
    }

    /// <summary>Les deux défensifs : Prévoyance rend d'abord, Reprise compte la perte restante.</summary>
    private void CheckSurvivalOrder()
    {
        Setup("overheal_reserve", "rally");
        SpecializationRuntime runtime = _player.SpecializationRuntime;
        float stock = runtime.Reserve.Stock;
        _player.TakeDamage(30f);
        Check(Near(runtime.Reserve.Stock, 0f) && Near(runtime.Rally.Recoverable, (30f - stock) * 0.4f),
            $"Prévoyance puis Reprise : {stock:0.#} rendus, {runtime.Rally.Recoverable:0.#} récupérables sur le reste");
        Kill(_player.GetInstanceId());
        Check(Near(runtime.Reserve.Stock, 0f), "Une restitution de Reprise n'alimente pas Prévoyance");
    }

    /// <summary>Débordement : excédent natif, plafond de l'attaque qui charge, lancement ultérieur, expirations.</summary>
    private void CheckOverflow()
    {
        Setup("overflow");
        WeaponInstance bow = _player.WeaponSlots[0];
        AttackContext launchA = _player.BeginAttack(bow, 30f);
        Enemy first = SpawnEnemy();
        first.TakeDamage(Hp(first) + 20f, source: launchA);
        Check(Near(OverflowLedger.Amount(_player.GetInstanceId(), bow), 10f) && LastGauge("overflow") is { Value: 10f } gauge
            && gauge.Key == bow.Id, "Coup fatal direct : la moitié de l'excédent natif en réserve, jauge de l'arme publiée");

        Enemy secondVictim = SpawnEnemy();
        secondVictim.TakeDamage(Hp(secondVictim) + 60f, source: launchA);
        Check(Near(OverflowLedger.Amount(_player.GetInstanceId(), bow), 30f),
            "Deux impacts du même lancement : excédents agrégés sous le plafond de l'attaque (30)");

        Enemy sameLaunch = SpawnEnemy();
        DamageResult own = sameLaunch.TakeDamage(5f, source: launchA);
        Check(Near(own.CarriedDamage, 0f) && Near(OverflowLedger.Amount(_player.GetInstanceId(), bow), 30f),
            "Le lancement qui charge ne consomme pas sa propre réserve");

        WeaponInstance bell = AddWeapon("teachers_bell");
        DamageResult other = SpawnEnemy().TakeDamage(5f, source: _player.BeginAttack(bell, 5f));
        DamageResult dot = SpawnEnemy().TakeDamage(5f, source: _player.BeginAttack(bow, 5f).As(DamageKind.DamageOverTime));
        Check(Near(other.CarriedDamage, 0f) && Near(dot.CarriedDamage, 0f), "Une autre arme ou un DOT ne consomment pas la réserve");

        Enemy target = SpawnEnemy();
        DamageResult carried = target.TakeDamage(5f, source: _player.BeginAttack(bow, 5f));
        Check(Near(carried.CarriedDamage, 30f) && Near(carried.HpLost, 35f) && Near(OverflowLedger.Amount(_player.GetInstanceId(), bow), 0f)
            && LastGauge("overflow") is { Value: 0f }, $"Lancement suivant : réserve entière sur le premier impact ({carried.HpLost:0.#} PV)");
        DamageResult second = target.TakeDamage(5f, source: _player.BeginAttack(bow, 5f));
        Check(Near(second.CarriedDamage, 0f), "Réserve consommée une seule fois");

        OverflowLedger.Charge(_player.GetInstanceId(), bow, 999, 10f, 10f, 3f);
        Enemy finished = SpawnEnemy();
        float finishedHp = Hp(finished);
        DamageResult reportKill = finished.TakeDamage(finishedHp - 2f, source: _player.BeginAttack(bow, 50f));
        Check(reportKill.Fatal && Near(reportKill.NativeOverkill, 0f) && Near(OverflowLedger.Amount(_player.GetInstanceId(), bow), 0f),
            "Mort due au seul report : aucun nouvel excédent natif");

        Enemy dotVictim = SpawnEnemy();
        dotVictim.TakeDamage(Hp(dotVictim) + 50f, source: _player.BeginAttack(bow, 50f).As(DamageKind.SecondaryWeapon));
        Check(Near(OverflowLedger.Amount(_player.GetInstanceId(), bow), 0f), "Un coup secondaire fatal ne charge rien");

        Enemy timed = SpawnEnemy();
        timed.TakeDamage(Hp(timed) + 10f, source: _player.BeginAttack(bow, 50f));
        Advance(2.9f);
        bool alive = OverflowLedger.Amount(_player.GetInstanceId(), bow) > 0f;
        Advance(0.2f);
        Check(alive && Near(OverflowLedger.Amount(_player.GetInstanceId(), bow), 0f) && LastGauge("overflow") is { Value: 0f },
            "Réserve valable 3 s, puis perdue");

        Enemy dropped = SpawnEnemy();
        dropped.TakeDamage(Hp(dropped) + 10f, source: _player.BeginAttack(bow, 50f));
        _player.RemoveWeapon(0);
        Check(Near(OverflowLedger.Amount(_player.GetInstanceId(), bow), 0f), "Arme retirée : sa réserve disparaît");

        ulong owner = _player.GetInstanceId();
        OverflowLedger.Charge(owner, bell, 1, 5f, 5f, 3f);
        Setup();
        Check(Near(OverflowLedger.Amount(owner, bell), 0f), "Joueur retiré : aucune réserve ne lui survit");
    }

    private Enemy SpawnEnemy()
    {
        Enemy enemy = GD.Load<PackedScene>("res://scenes/enemies/Enemy.tscn").Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get("rodeur"), 1f, 1f);
        enemy.SetPhysicsProcess(false);
        enemy.Position = new Vector2(1000f, 1000f);
        return enemy;
    }

    private static float Hp(Enemy enemy) =>
        (float)typeof(Enemy).GetField("_currentHp", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(enemy);

    private WeaponInstance AddWeapon(string id)
    {
        _player.AddWeapon(WeaponDataLoader.Get(id));
        return _player.WeaponSlots[_player.WeaponSlots.Count - 1];
    }

    private void Setup(params string[] perks)
    {
        if (_player != null)
        {
            RemoveChild(_player);
            _player.QueueFree();
        }
        _gauges.Clear();
        _player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(_player);
        _player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
        _player.IsAIControlled = true;
        _player.IsGodMode = false;
        _player.SetPhysicsProcess(false);
        _player.DisableDefenseForTests();
        foreach (string id in perks)
            _player.AcquireSpecialization(PerkSpecializationDataLoader.Get(id));
    }

    private void Kill(ulong ownerId)
    {
        AttackContext source = new(ownerId, null, 1, DamageKind.DirectWeapon, 10f);
        DamageResult damage = new(new EnemyLife(1, 1), source, 5f, 10f, 0f, 5f, true, true);
        _events.PublishEnemyKill(new EnemyKillResult(damage.Target, "test", Vector2.Zero, damage, default, default));
    }

    /// <summary>Temps de jeu écoulé pour les fenêtres, sans attendre de vraies frames.</summary>
    private void Advance(float seconds)
    {
        const float step = 0.1f;
        for (float elapsed = 0f; elapsed < seconds - 0.0001f; elapsed += step)
            _player.SpecializationRuntime._Process(Mathf.Min(step, seconds - elapsed));
    }

    private SpecializationGauge? LastGauge(string effect)
    {
        for (int i = _gauges.Count - 1; i >= 0; i--)
            if (_gauges[i].Effect == effect)
                return _gauges[i];
        return null;
    }

    private static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.01f;

    private void Check(bool ok, string label)
    {
        if (!ok)
            _failures++;
        GD.Print($"[PerkEffectsRegression] {(ok ? "PASS" : "FAIL")} {label}");
    }
}
