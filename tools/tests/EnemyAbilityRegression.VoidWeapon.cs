using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Spawn;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Le vide comme arme (plan 52 E4) : une bande de Néant de x = 128 à 256 dans un gestionnaire synthétique, reliée au
/// joueur. Élan éteint, les créatures l'ignorent ; allumé, elles la longent en marchant, la percussion du dash les
/// projette, et une créature ordinaire poussée dedans y tombe, éliminée.
/// </summary>
public partial class EnemyAbilityRegression
{
    private const float VoidEdge = 128f;

    private async Task RunVoidWeaponChecks()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        ErasureManager erasure = new();
        AddChild(erasure);
        erasure.SetProcess(false);
        Dictionary<Vector2I, ErasureManager.ErasureZonePhase> phases =
            (Dictionary<Vector2I, ErasureManager.ErasureZonePhase>)typeof(ErasureManager).GetField("_zonePhases", flags).GetValue(erasure);
        for (int y = -1; y <= 1; y++)
            phases[new Vector2I(1, y)] = ErasureManager.ErasureZonePhase.Void;
        FieldInfo managerField = typeof(Player).GetField("_erasureManager", flags);
        managerField.SetValue(_player, erasure);
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        int kills = 0, falls = 0;
        EventBus.EnemyKilledEventHandler onKill = (_, _) => kills++;
        Action<Vector2> onFall = _ => falls++;
        bus.EnemyKilled += onKill;
        bus.CreatureFellIntoVoid += onFall;
        // Les Ombres frappent pendant les attentes : sans invincibilité, le joueur blessé ne dashe plus, puis meurt.
        _player.IsGodMode = true;
        try
        {
            _player.Position = new Vector2(0f, 20f);
            _player.AIInputOverride = Vector2.Zero;

            _player.SetElanEnabled(false);
            Enemy walker = await SpawnReady("shade", new Vector2(320f, 0f));
            bool enteredVoid = await WalkToward(walker, erasure, 180);
            Check(enteredVoid, "vide comme arme : Élan éteint, la créature traverse le Néant comme avant");
            Despawn(walker);

            _player.SetElanEnabled(true);
            walker = await SpawnReady("shade", new Vector2(320f, 0f));
            enteredVoid = await WalkToward(walker, erasure, 180);
            Check(!enteredVoid && walker.Position.X >= 2f * VoidEdge - 20f,
                $"vide comme arme : Élan allumé, la créature s'arrête au bord au lieu d'entrer (x={walker.Position.X:0})");
            Despawn(walker);

            await WaitDashReady();
            Enemy target = await SpawnReady("shade", new Vector2(-40f, 0f));
            Vector2 start = target.Position;
            int percussions = _player.Percussions;
            await DashNow(Vector2.Left);
            await Step(8);
            float pushed = start.DistanceTo(target.Position);
            Check(_player.Percussions == percussions + 1 && pushed > 30f,
                $"percussion : le dash projette la créature traversée ({pushed:0} px)");
            Despawn(target);

            await WaitDashReady();
            List<Enemy> line = new();
            for (int i = 0; i < 8; i++)
                line.Add(await SpawnReady("shade", new Vector2(-20f - i * 6f, i % 2 == 0 ? 3f : -3f)));
            float before = _player.Elan.Value;
            await DashNow(Vector2.Left);
            float gained = _player.Elan.Value - before;
            Check(gained <= _player.Elan.Config.PercussionMaxGainPerDash + 0.02f && gained >= _player.Elan.Config.PercussionMaxGainPerDash - 0.02f,
                $"percussion : huit créatures, gain plafonné à {_player.Elan.Config.PercussionMaxGainPerDash:F2} ({gained:F3})");
            foreach (Enemy enemy in line)
                Despawn(enemy);

            int killsBefore = kills;
            _player.Position = new Vector2(40f, 20f);
            await WaitDashReady();
            Enemy faller = await SpawnReady("shade", new Vector2(70f, 0f), 1f);
            await DashNow(Vector2.Right);
            await Step(10);
            Check(faller.IsDying && falls == 1 && kills == killsBefore + 1,
                $"chute : projetée dans le Néant, la créature tombe et compte comme éliminée (chutes {falls}, éliminations {kills - killsBefore})");
            Despawn(faller);

            _player.Position = new Vector2(0f, 20f);
            Enemy knocked = await SpawnReady("shade", new Vector2(110f, 0f), 1f);
            knocked.ApplyKnockback(Vector2.Right, 40f);
            await Step(10);
            Check(knocked.IsDying && falls == 2, "chute : le recul d'une arme fait aussi tomber dans le Néant");
            Despawn(knocked);

            _player.Position = new Vector2(320f, 20f);
            Enemy blocked = await SpawnReady("shade", new Vector2(-210f, 0f), 1f);
            await Step(60);
            blocked.ApplyKnockback(Vector2.Left, 60f);
            await Step(40);
            Check(!blocked.IsDying && falls == 2,
                $"chute : collée au bord, une créature repoussée à l'opposé du vide ne tombe pas (x={blocked.Position.X:0})");
            Despawn(blocked);

            _player.Position = new Vector2(900f, 20f);
            Enemy far = await SpawnReady("shade", new Vector2(-1100f, 0f));
            enteredVoid = await WalkToward(far, erasure, 420);
            Check(!enteredVoid && far.Position.X < VoidEdge,
                $"vide comme arme : loin du joueur (pas simplifié), la créature longe aussi le bord (x={far.Position.X:0})");
            Despawn(far);

            await CheckFallPoolReuse(() => falls);

            _player.Position = new Vector2(0f, 20f);
            int fallsBeforeElite = falls;
            Enemy elite = await SpawnReady("shade", new Vector2(110f, 0f));
            elite.ApplyVariant(EnemyVariantDataLoader.GetVariant("elite"), Array.Empty<EnemyAffixData>());
            elite.ApplyKnockback(Vector2.Right, 60f);
            await Step(20);
            Check(!elite.IsDying && falls == fallsBeforeElite && elite.Position.X < VoidEdge,
                $"chute : une élite poussée s'arrête au bord (x={elite.Position.X:0})");
            Despawn(elite);

            _player.PercussionMinTier = 2;
            await WaitDashReady();
            target = await SpawnReady("shade", new Vector2(-40f, 0f));
            percussions = _player.Percussions;
            await DashNow(Vector2.Left);
            Check(_player.Percussions == percussions, "percussion : sous le palier minimum réglé, le dash ne projette rien");
            Despawn(target);
            _player.PercussionMinTier = _player.Elan.Config.PercussionMinTier;
        }
        finally
        {
            bus.EnemyKilled -= onKill;
            bus.CreatureFellIntoVoid -= onFall;
            _player.SetElanEnabled(false);
            _player.IsGodMode = false;
            _player.AIInputOverride = Vector2.Zero;
            managerField.SetValue(_player, null);
            erasure.QueueFree();
            _player.Position = Vector2.Zero;
            await Step(2);
        }
    }

    /// <summary>
    /// Une créature tombée finit sa chute, revient au pool remise à neuf (échelle, opacité, vivante), puis retombe : le
    /// tween de chute ne laisse rien derrière lui.
    /// </summary>
    private async Task CheckFallPoolReuse(Func<int> falls)
    {
        Node main = new() { Name = "Main" };
        GetTree().Root.AddChild(main);
        EnemyPool pool = new() { Name = "EnemyPool", InitialSize = 0 };
        main.AddChild(pool);
        try
        {
            _player.Position = new Vector2(0f, 20f);
            Enemy enemy = await SpawnReady("shade", new Vector2(110f, 0f), 1f);
            int before = falls();
            enemy.ApplyKnockback(Vector2.Right, 40f);
            await Step(50);
            bool returned = IsInstanceValid(enemy) && !enemy.IsInsideTree() && falls() == before + 1;
            _enemies.Remove(enemy);
            Enemy reused = pool.Get();
            AddChild(reused);
            reused.Initialize(EnemyDataLoader.Get("shade"), 1f, 1f);
            reused.SetTicking(false);
            reused.Position = _player.Position + new Vector2(110f, 0f);
            _enemies.Add(reused);
            bool fresh = ReferenceEquals(reused, enemy) && reused.Scale == Vector2.One && Mathf.IsEqualApprox(reused.Modulate.A, 1f)
                && !reused.IsDying;
            await Step(1);
            reused.ApplyKnockback(Vector2.Right, 40f);
            await Step(10);
            Check(returned && fresh && reused.IsDying && falls() == before + 2,
                $"chute : la créature revient du pool remise à neuf et retombe (rendue {returned}, neuve {fresh}, chutes {falls() - before})");
            Despawn(reused);
        }
        finally
        {
            main.QueueFree();
            await Step(1);
        }
    }

    /// <summary>La créature marche vers le joueur immobile ; vrai si elle a mis le pied dans le Néant.</summary>
    private async Task<bool> WalkToward(Enemy enemy, ErasureManager erasure, int ticks)
    {
        bool entered = false;
        for (int tick = 0; tick < ticks; tick++)
        {
            await Step(1);
            entered |= erasure.GetZonePhaseAt(enemy.GlobalPosition) == ErasureManager.ErasureZonePhase.Void;
        }
        return entered;
    }

    /// <summary>Attend la recharge du dash, joueur immobile, avant de poser les créatures du cas suivant.</summary>
    private async Task WaitDashReady()
    {
        _player.AIInputOverride = Vector2.Zero;
        await Step(160);
    }

    /// <summary>Un dash dans cette direction, aussitôt, puis l'arrêt.</summary>
    private async Task DashNow(Vector2 direction)
    {
        _player.AIInputOverride = direction;
        await Step(1);
        _player.Mobility.Request();
        await Step(12);
        _player.AIInputOverride = Vector2.Zero;
    }
}
