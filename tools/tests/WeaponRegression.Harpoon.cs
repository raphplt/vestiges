using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Tests;

public partial class WeaponRegression
{
    private Enemy HarpoonTarget(string id, Vector2 position)
    {
        Enemy enemy = EnemyScene.Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get(id), 1000f, 1f);
        enemy.SetTicking(false);
        enemy.Position = position;
        return enemy;
    }

    private Projectile HarpoonShot(Player player, WeaponInstance weapon, Vector2 origin, AttackContext context, float delay = 0f)
    {
        Projectile shot = GD.Load<PackedScene>("res://scenes/combat/Projectile.tscn").Instantiate<Projectile>();
        AddChild(shot);
        shot.Launch(origin, Vector2.Right, 1f, 260f, 3f, 2, false, player, weapon.Base, weapon, context, launchDelay: delay);
        shot.StopTicking();
        return shot;
    }

    private async Task CheckHarpoon()
    {
        Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(player);
        player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
        player.SetPhysicsProcess(false);
        player.IsAIControlled = true;
        Vector2 origin = new(64000f, 64000f);
        player.Position = origin;
        WeaponInstance weapon = new(WeaponDataLoader.Get("harpoon_rifle"));
        Enemy light = HarpoonTarget("rodeur", origin + new Vector2(160f, 0f));
        Enemy second = HarpoonTarget("rodeur", origin + new Vector2(200f, 0f));
        Enemy heavy = HarpoonTarget("void_brute", origin + new Vector2(180f, 0f));
        Enemy boss = HarpoonTarget("rodeur", origin + new Vector2(150f, 0f));
        typeof(Enemy).GetField("_tier", Private).SetValue(boss, EnemyTier.Boss);
        MethodInfo hit = typeof(Projectile).GetMethod("Hit", Private);
        MethodInfo release = typeof(Projectile).GetMethod("Release", Private);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        AttackContext launch = player.BeginAttack(weapon, 1f);
        Projectile first = HarpoonShot(player, weapon, origin, launch);
        Projectile extra = HarpoonShot(player, weapon, origin, launch);
        float beforeSecond = second.HpRatio;
        hit.Invoke(first, new object[] { light });
        player.Harpoon.Advance(0.1f, true);
        Check(light.Position.X < origin.X + 160f && player.Harpoon.IsActive && !player.Harpoon.PullsPlayer,
            "Harpon : le premier léger survivant est ramené");
        // Le tireur a changé de côté après le départ ; le filin normal suit sa position actuelle.
        player.Position = origin + new Vector2(250f, 0f);
        float movedLight = light.Position.X;
        player.Harpoon.Advance(0.1f, true);
        Check(light.Position.X > movedLight, "Harpon : la destination légère suit le tireur qui se déplace");
        player.Harpoon.Cancel();
        hit.Invoke(first, new object[] { second });
        hit.Invoke(extra, new object[] { heavy });
        Check(second.HpRatio < beforeSecond && !player.Harpoon.IsActive,
            "Harpon : perforation et tir supplémentaire infligent leurs dégâts sans seconde accroche de salve");
        first.QueueFree(); extra.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(player.Harpoon.TrackedLaunches == 0, "Harpon : le dernier projectile libère le registre de sa salve");
        player.Position = origin;

        int token = player.Harpoon.Register(player.BeginAttack(weapon, 1f));
        Check(!player.Harpoon.TryCatch(token, boss, origin) && !player.Harpoon.TryCatch(token, second, origin),
            "Harpon : boss ancré, sans reporter l'accroche sur la cible suivante");
        player.Harpoon.Release(token);
        token = player.Harpoon.Register(player.BeginAttack(weapon, 1f));
        Check(player.Harpoon.TryCatch(token, heavy, origin), "Harpon : la Brute est lourde par ses données");
        Vector2 heavyStep = player.Harpoon.Advance(0.1f, true);
        Check(player.Harpoon.PullsPlayer && heavyStep.X > 0f, "Harpon : le lourd attire le joueur");
        player.Harpoon.Cancel(); player.Harpoon.Release(token);

        AttackContext echo = player.BeginAttack(weapon, 1f) with { IsEcho = true, Origin = origin + new Vector2(300f, 0f) };
        token = player.Harpoon.Register(echo);
        Check(!player.Harpoon.TryCatch(token, heavy, echo.Origin), "Harpon : la rémanence ne rappelle jamais le joueur vers un lourd");
        player.Harpoon.Release(token);
        token = player.Harpoon.Register(player.BeginAttack(weapon, 1f) with { IsEcho = true });
        light.Position = origin + new Vector2(160f, 0f);
        player.Harpoon.TryCatch(token, light, echo.Origin);
        player.Harpoon.Advance(0.1f, true);
        Check(light.Position.X > origin.X + 160f, "Harpon : la rémanence ramène le léger vers son point figé, pas le joueur");
        light.Reset(); light.Initialize(EnemyDataLoader.Get("rodeur"), 1000f, 1f); light.SetTicking(false);
        Vector2 recycled = light.Position;
        player.Harpoon.Advance(0.1f, true);
        Check(!player.Harpoon.IsActive && light.Position == recycled, "Harpon : une cible recyclée n'hérite pas de la traction");
        player.Harpoon.Release(token);

        // Un projectile ordinaire réutilisant le même nœud ne garde ni bail ni effet du harpon.
        Projectile reused = HarpoonShot(player, weapon, origin, player.BeginAttack(weapon, 1f));
        reused.SetRelease(_ => { });
        release.Invoke(reused, null);
        WeaponInstance ordinary = new(WeaponDataLoader.Get("crossbow"));
        reused.Launch(origin, Vector2.Right, 1f, 260f, 3f, 2, false, player, ordinary.Base, ordinary, player.BeginAttack(ordinary, 1f));
        reused.StopTicking();
        hit.Invoke(reused, new object[] { second });
        Check(!player.Harpoon.IsActive && player.Harpoon.TrackedLaunches == 0,
            "Harpon : retour au pool puis projectile ordinaire sans ancien état de traction");
        reused.QueueFree();

        // Le départ retardé choisit sa vraie origine, après déplacement du tireur ; l'écho garde la sienne.
        Projectile delayed = HarpoonShot(player, weapon, origin, player.BeginAttack(weapon, 1f) with { IsEcho = true }, 0.1f);
        delayed.DepartFrom(echo.Origin);
        player.Position = origin + new Vector2(-200f, 0f);
        delayed.PhysicsTick(0.11);
        light.Position = origin + new Vector2(160f, 0f);
        hit.Invoke(delayed, new object[] { light });
        player.Harpoon.Advance(0.05f, true);
        Check(light.Position.X > origin.X + 160f, "Harpon : origine et provenance de l'écho survivent au départ retardé");
        player.Harpoon.Cancel(); delayed.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        player.Position = origin;

        token = player.Harpoon.Register(player.BeginAttack(weapon, 1f));
        player.Harpoon.TryCatch(token, light, origin);
        Vector2 frozen = light.Position;
        player.Harpoon.Advance(0.1f, false);
        Check(!player.Harpoon.IsActive && light.Position == frozen, "Harpon : hors run annule sans déplacer la cible");
        player.Harpoon.Release(token);
        token = player.Harpoon.Register(player.BeginAttack(weapon, 1f));
        player.Harpoon.TryCatch(token, light, origin);
        player.Mobility.Request(); player.Mobility.Step(1f / 60f, Vector2.Right, 190f, 1f, true);
        player.Harpoon.Advance(1f / 60f, true);
        Check(!player.Harpoon.IsActive, "Harpon : la mobilité est prioritaire dès son premier tick");
        player.Harpoon.Release(token); player.Mobility.Suspend();
        player.Mobility.Step(1f / 60f, Vector2.Zero, 190f, 1f, true);

        // Aucun historique infini, même après bien plus de lancements que la capacité simultanée.
        for (int i = 0; i < 1024; i++)
        {
            int lease = player.Harpoon.Register(player.BeginAttack(weapon, 1f));
            if (lease < 0) { Check(false, "Harpon : une salve libérée rend sa place"); break; }
            player.Harpoon.Release(lease);
        }
        Check(player.Harpoon.TrackedLaunches == 0, "Harpon : 1024 salves libérées, registre vide");

        int[] held = new int[256];
        for (int i = 0; i < held.Length; i++) held[i] = player.Harpoon.Register(player.BeginAttack(weapon, 1f));
        Check(player.Harpoon.TrackedLaunches == 256 && player.Harpoon.Register(player.BeginAttack(weapon, 1f)) == -1,
            "Harpon : 256 salves simultanées, saturation bornée sans écraser un bail vivant");
        foreach (int lease in held) player.Harpoon.Release(lease);
        Check(player.Harpoon.TrackedLaunches == 0, "Harpon : la saturation rend toutes ses places après libération");
        token = player.Harpoon.Register(player.BeginAttack(weapon, 1f));
        player.Harpoon.TryCatch(token, light, origin);
        GetTree().Paused = true;
        Check(!player.Harpoon.IsActive && light.HarpoonOwner == 0, "Harpon : une vraie pause libère immédiatement le léger");
        GetTree().Paused = false;
        player.Harpoon.Release(token);
        Enemy doomed = HarpoonTarget("rodeur", origin + new Vector2(150f, 0f));
        token = player.Harpoon.Register(player.BeginAttack(weapon, 1f));
        player.Harpoon.TryCatch(token, doomed, origin);
        doomed.TakeDamage(1000000f);
        player.Harpoon.Advance(0.1f, true);
        Check(!player.Harpoon.IsActive, "Harpon : une cible morte relâche la liaison");
        player.Harpoon.Release(token);
        // La mort conserve son animation différée ; cette cible ne sera pas réutilisée par les scénarios suivants.

        Enemy fragile = HarpoonTarget("rodeur", origin + new Vector2(100f, 0f));
        fragile.Initialize(EnemyDataLoader.Get("rodeur"), 0.001f, 1f); fragile.SetTicking(false);
        Projectile survivorShot = HarpoonShot(player, weapon, origin, player.BeginAttack(weapon, 1f));
        hit.Invoke(survivorShot, new object[] { fragile });
        Check(fragile.IsDying && !player.Harpoon.IsActive, "Harpon : une cible tuée à l'impact ne consomme pas l'accroche");
        hit.Invoke(survivorShot, new object[] { second });
        Check(player.Harpoon.IsActive, "Harpon : la perforation accroche le survivant suivant");
        player.Harpoon.Cancel(); survivorShot.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(player.Harpoon.Register(player.BeginAttack(weapon, 1f) with { OwnerId = _player.GetInstanceId() }) == -1,
            "Harpon : une provenance d'un autre propriétaire ne reçoit pas de bail");

        light.Position = origin + new Vector2(800f, 0f);
        typeof(Enemy).GetField("_player", Private).SetValue(light, player);
        token = player.Harpoon.Register(player.BeginAttack(weapon, 1f));
        player.Harpoon.TryCatch(token, light, player.Position);
        Vector2 distant = light.Position;
        light.PhysicsTick(0.1);
        Check(light.Position == distant, "Harpon : le mouvement simplifié hors 600 px respecte la liaison");
        player.Harpoon.Advance(1f / 120f, true);
        Check(player.Harpoon.IsActive && light.Position.X < distant.X,
            "Harpon : coordonnées lointaines à 120 Hz, sans annulation par arrondi sous-pixel");
        player.Harpoon.Cancel(); player.Harpoon.Release(token);
        player.Mobility.Hurt(0.25f);
        token = player.Harpoon.Register(player.BeginAttack(weapon, 1f));
        Check(!player.Harpoon.TryCatch(token, light, origin), "Harpon : un tir déjà en vol ne relance pas la traction pendant hurt");
        player.Harpoon.Release(token);
        player.Mobility.Step(0.3f, Vector2.Zero, 190f, 1f, true);

        await CheckHarpoonWalls(player, weapon, light, heavy, origin);
        CheckHarpoonVoid(player, weapon, light, heavy);
        CheckHarpoonData();
        player.Position = origin;
        light.Position = origin + new Vector2(150f, 0f);
        token = player.Harpoon.Register(player.BeginAttack(weapon, 1f));
        Check(player.Harpoon.TryCatch(token, light, origin), "Harpon : liaison établie avant mort propriétaire");
        player.IsGodMode = false;
        player.TakeErasureDamage(player.EffectiveMaxHp + 100f);
        Check(player.IsDead && !player.Harpoon.IsActive && light.HarpoonOwner == 0,
            "Harpon : la mort du propriétaire libère le déplacement de sa cible");
        player.Harpoon.Release(token);
        player.Harpoon.Cancel();
        light.QueueFree(); second.QueueFree(); heavy.QueueFree(); boss.QueueFree(); player.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task CheckHarpoonWalls(Player player, WeaponInstance weapon, Enemy light, Enemy heavy, Vector2 origin)
    {
        int previousHz = Engine.PhysicsTicksPerSecond;
        StaticBody2D wall = new() { Position = origin + new Vector2(80f, 0f), CollisionLayer = 4 };
        wall.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(2f, 300f) } });
        AddChild(wall);
        ObstacleField.Add(wall, new[] { new Vector2(-1f, -150f), new Vector2(1f, -150f), new Vector2(1f, 150f), new Vector2(-1f, 150f) });
        foreach (int hz in new[] { 30, 60, 120 })
        {
            Engine.PhysicsTicksPerSecond = hz;
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            player.Position = origin;
            light.Position = origin + new Vector2(160f, 0f);
            int token = player.Harpoon.Register(player.BeginAttack(weapon, 1f));
            player.Harpoon.TryCatch(token, light, origin);
            for (int i = 0; i < hz; i++) player.Harpoon.Advance(1f / hz, true);
            Check(light.Position.X > wall.Position.X + light.BodyRadius && !player.Harpoon.IsActive,
                $"Harpon {hz} Hz : le léger ne traverse pas le mur de 2 px");
            player.Harpoon.Release(token);
            token = player.Harpoon.Register(player.BeginAttack(weapon, 1f));
            heavy.Position = origin + new Vector2(190f, 0f);
            player.Harpoon.TryCatch(token, heavy, origin);
            for (int i = 0; i < hz / 2; i++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                player._PhysicsProcess(1.0 / hz);
            }
            Check(player.Position.X < wall.Position.X - player.BodyRadius && player.Position.X > origin.X
                && !player.Harpoon.IsActive, $"Harpon {hz} Hz : MoveAndSlide arrête le joueur au mur fin ({player.Position.X - origin.X:0.0} px)");
            player.Harpoon.Release(token);
        }
        Engine.PhysicsTicksPerSecond = previousHz;
        wall.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private void CheckHarpoonVoid(Player player, WeaponInstance weapon, Enemy light, Enemy heavy)
    {
        ErasureManager erasure = new();
        AddChild(erasure); erasure.SetProcess(false);
        Dictionary<Vector2I, ErasureManager.ErasureZonePhase> phases = (Dictionary<Vector2I, ErasureManager.ErasureZonePhase>)
            typeof(ErasureManager).GetField("_zonePhases", Private).GetValue(erasure);
        phases[new Vector2I(1, 0)] = ErasureManager.ErasureZonePhase.Void;
        typeof(Player).GetField("_erasureManager", Private).SetValue(player, erasure);
        player.Position = new Vector2(erasure.CellSize - 20f, 20f);
        heavy.Position = player.Position + Vector2.Right * 150f;
        int token = player.Harpoon.Register(player.BeginAttack(weapon, 1f));
        player.Harpoon.TryCatch(token, heavy, player.Position);
        Vector2 step = player.Harpoon.Advance(0.3f, true);
        player.Harpoon.FinishPlayerTravel(step);
        Check(player.Position.X + step.X < erasure.CellSize && !player.Harpoon.IsActive,
            "Harpon : le rappel lourd s'arrête dès la frontière du Néant");
        player.Harpoon.Release(token);
        player.Position = new Vector2(erasure.CellSize + 100f, 20f);
        light.Position = new Vector2(erasure.CellSize - 20f, 20f);
        token = player.Harpoon.Register(player.BeginAttack(weapon, 1f));
        player.Harpoon.TryCatch(token, light, player.Position);
        player.Harpoon.Advance(0.3f, true);
        Check(light.Position.X < erasure.CellSize && !player.Harpoon.IsActive,
            "Harpon : un léger ne traverse pas le Néant vers le tireur");
        player.Harpoon.Release(token);
        typeof(Player).GetField("_erasureManager", Private).SetValue(player, null);
        erasure.QueueFree();
    }

    private void CheckHarpoonData()
    {
        WeaponInstance winch = new(WeaponDataLoader.Get("harpoon_rifle"));
        WeaponInstance spear = new(WeaponDataLoader.Get("harpoon_rifle"));
        while (winch.CanLevelUp) winch.ApplyUpgrade(Array.Empty<StatGain>());
        while (spear.CanLevelUp) spear.ApplyUpgrade(Array.Empty<StatGain>());
        Check(winch.Ascend("salvage_winch") && spear.Ascend("abyssal_spear")
            && winch.SpecialEffect.Get(SpecialEffectParam.PullDistance) > spear.SpecialEffect.Get(SpecialEffectParam.PullDistance)
            && spear.GetStat("projectile_pierce") == 8f, "Harpon : deux ascensions aux réglages distincts et complets");
        Godot.Collections.Array entries = Json.ParseString(FileAccess.GetFileAsString("res://data/weapons/weapons.json")).AsGodotArray();
        Godot.Collections.Dictionary candidate = new();
        foreach (Variant item in entries)
        {
            Godot.Collections.Dictionary entry = item.AsGodotDictionary();
            if (entry["id"].AsString() == "harpoon_rifle") candidate = entry;
        }
        foreach (string key in new[] { "pull_distance", "pull_speed", "pull_duration", "stop_distance" })
        {
            Godot.Collections.Dictionary invalid = candidate.Duplicate(true);
            invalid["special_effect"].AsGodotDictionary()[key] = 0;
            Check(WeaponDataValidator.Validate(invalid)?.Contains(key) == true, $"Harpon : contrat refuse {key}=0");
            invalid = candidate.Duplicate(true);
            invalid["special_effect"].AsGodotDictionary().Remove(key);
            Check(WeaponDataValidator.Validate(invalid)?.Contains(key) == true, $"Harpon : contrat exige {key}");
        }
    }
}
