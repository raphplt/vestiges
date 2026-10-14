using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Tests;

public partial class PerkEffectsRegression
{
    private async Task CheckOpeningStrike()
    {
        CheckOpeningConfiguration();
        Setup();
        PerkManager perks = new();
        AddChild(perks);
        perks.ApplyPassivePerks("traqueur");
        OpeningStrike signature = _player.OpeningStrike;
        Check(signature != null && Near(signature.ChanceAt(1), 0.1f)
            && Near(signature.ChanceAt(21), 0.15f) && Near(signature.ChanceAt(1000), 0.5f),
            "Œil d'aigle : signature du Traqueur, 10 % au niveau 1, +0,25 point par niveau, plafond 50 %");
        _events.EmitSignal(EventBus.SignalName.LevelUp, 21);
        Check(signature != null && Near(signature.Chance, 0.15f), "Œil d'aigle : les niveaux mettent à jour le bonus en run");
        if (signature == null)
            return;

        WeaponInstance bow = new(WeaponDataLoader.Get("makeshift_bow"));
        Enemy singleRollTarget = SpawnEnemy();
        FieldInfo nativeChance = typeof(Player).GetField("_critChance", BindingFlags.Instance | BindingFlags.NonPublic);
        float beforeChance = _player.CritChance;
        nativeChance.SetValue(_player, 0.2f);
        Check(Near(signature.CombinedChance(singleRollTarget), 0.35f), "Œil d'aigle : chance native et bonus d'ouverture s'additionnent");
        ulong beforeRoll = RunRandom.Combat.State;
        typeof(Player).GetMethod("RollAttackCrit", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_player, null);
        Check(RunRandom.Combat.State == beforeRoll, "Œil d'aigle : le départ de l'attaque ne tire pas un second critique");
        RunRandom.Combat.Randf();
        ulong afterOneRoll = RunRandom.Combat.State;
        RunRandom.Combat.State = beforeRoll;
        float rollDamage = 1f;
        bool rollCritical = false;
        AttackContext rollContext = _player.BeginAttack(bow, 1f);
        _player.ResolveOpeningStrike(singleRollTarget, ref rollDamage, ref rollCritical, ref rollContext);
        Check(RunRandom.Combat.State == afterOneRoll, "Œil d'aigle : un seul jet consolidé à l'impact");
        nativeChance.SetValue(_player, 0.95f);
        Check(Near(signature.CombinedChance(singleRollTarget), 1f), "Œil d'aigle : chance critique totale plafonnée à 100 %");
        nativeChance.SetValue(_player, beforeChance);
        RemoveChild(singleRollTarget);
        singleRollTarget.QueueFree();
        _player.RemoveChild(signature);
        signature.QueueFree();
        OpeningStrike guaranteed = new();
        guaranteed.Initialize(_player, new PerkOpeningStrike(1f, 0f, 1f));
        _player.AddChild(guaranteed);

        List<Enemy> fixtures = new();
        Enemy Fresh(Vector2? position = null)
        {
            Enemy enemy = SpawnEnemyAt(position ?? new Vector2(9000f, 9000f), fixtures);
            enemy.Initialize(EnemyDataLoader.Get("rodeur"), 100f, 1f);
            enemy.SetTicking(false);
            return enemy;
        }
        AttackContext direct = _player.BeginAttack(bow, 5f);
        Enemy first = Fresh();
        DamageResult opening = first.TakeDamage(5f, source: direct);
        DamageResult following = first.TakeDamage(5f, source: _player.BeginAttack(bow, 5f));
        Check(opening.Critical && Near(opening.NativeDamage, 5f * _player.CritMultiplier)
            && Near(opening.Source.ReferenceDamage, 5f * _player.CritMultiplier)
            && !following.Critical && Near(following.NativeDamage, 5f),
            "Œil d'aigle : seul le premier impact est renforcé, avec référence et provenance critique publiées");

        Enemy ordinary = Fresh();
        DamageResult existingCrit = ordinary.TakeDamage(5f * _player.CritMultiplier, true,
            source: _player.BeginAttack(bow, 5f * _player.CritMultiplier));
        Check(existingCrit.Critical && Near(existingCrit.NativeDamage, 5f * _player.CritMultiplier),
            "Œil d'aigle : un critique ordinaire ne reçoit pas un second multiplicateur");

        foreach (DamageKind kind in new[] { DamageKind.Passive, DamageKind.DamageOverTime, DamageKind.SecondaryWeapon, DamageKind.EnemyExplosion })
        {
            Enemy excluded = Fresh();
            DamageResult result = excluded.TakeDamage(5f, source: _player.BeginAttack(bow, 5f).As(kind));
            DamageResult later = excluded.TakeDamage(5f, source: _player.BeginAttack(bow, 5f));
            Check(!result.Critical && !later.Critical, $"Œil d'aigle : {kind} exclu, la cible endommagée perd son ouverture");
        }
        Enemy echo = Fresh();
        DamageResult echoed = echo.TakeDamage(5f, source: _player.BeginAttack(bow, 5f) with { IsEcho = true });
        Check(!echoed.Critical && Near(echoed.NativeDamage, 5f), "Œil d'aigle : la rémanence ne déclenche pas le bonus");
        Enemy foreign = Fresh();
        DamageResult unattributed = foreign.TakeDamage(5f, source: direct with { OwnerId = 0 });
        Check(!unattributed.Critical, "Œil d'aigle : aucun bonus sans propriétaire joueur");

        Enemy healed = Fresh();
        healed.TakeDamage(1f, source: direct.As(DamageKind.Passive));
        typeof(Enemy).GetField("_currentHp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(healed, healed.MaxHp);
        Check(!healed.TakeDamage(5f, source: direct).Critical, "Œil d'aigle : une régénération complète ne réarme pas l'ouverture");
        EnemyLife previousLife = healed.Life;
        healed.Initialize(EnemyDataLoader.Get("rodeur"), 100f, 1f);
        healed.SetTicking(false);
        Check(healed.Life != previousLife && healed.TakeDamage(5f, source: direct).Critical,
            "Œil d'aigle : un ennemi recyclé a une nouvelle ouverture");

        foreach (string status in new[] { "burn", "bleed", "poison" })
        {
            Enemy statusTarget = Fresh();
            statusTarget.Modifiers.IsEventBound = true;
            statusTarget.ApplyAffix(EnemyVariantDataLoader.GetAffix("regenerant"));
            float before = statusTarget.CurrentHp;
            if (status == "burn") statusTarget.ApplyIgnite(10f, 0.1f, direct);
            else if (status == "bleed") statusTarget.ApplyBleed(10f, 0.1f, direct);
            else statusTarget.ApplyPoison(1f, 0.1f, direct);
            statusTarget.PhysicsTick(0.1f);
            Check(statusTarget.CurrentHp < before && !statusTarget.IsUndamaged,
                $"Œil d'aigle : vrai tick {status} ferme l'ouverture dès la perte de PV");
            statusTarget.PhysicsTick(1f);
            Check(Near(statusTarget.CurrentHp, statusTarget.MaxHp) && !statusTarget.IsUndamaged
                && !statusTarget.TakeDamage(5f, source: direct).Critical,
                $"Œil d'aigle : guérison Tenace après {status} ne réarme jamais l'ouverture");
        }
        Enemy zeroDamage = Fresh();
        zeroDamage.TakeDamage(0f, source: direct.As(DamageKind.Passive));
        Check(zeroDamage.IsUndamaged && zeroDamage.TakeDamage(5f, source: direct).Critical,
            "Œil d'aigle : zéro dégât effectif ne consomme pas l'ouverture");

        ScreenShake shake = ScreenShake.Instance;
        bool ownsShake = shake == null;
        if (ownsShake)
        {
            shake = new ScreenShake();
            AddChild(shake);
            shake.SetProcess(false);
        }
        FieldInfo trauma = typeof(ScreenShake).GetField("_trauma", BindingFlags.Instance | BindingFlags.NonPublic);
        trauma.SetValue(shake, 0f);
        Enemy shownCritical = Fresh();
        shownCritical.TakeDamage(5f, showImpact: true, source: direct);
        float firstTrauma = (float)trauma.GetValue(shake);
        Check(Near(firstTrauma, Mathf.Min(0.5f * CombatFxSettings.ScreenShake, 1f)),
            "Œil d'aigle : l'impact critique montré produit sa secousse normale");
        trauma.SetValue(shake, 0f);
        shownCritical.TakeDamage(5f, true, showImpact: false, source: direct);
        Check(Near((float)trauma.GetValue(shake), 0f),
            "Œil d'aigle : une touche critique continue sans impact ne répète pas la secousse");
        if (ownsShake) { RemoveChild(shake); shake.QueueFree(); }

        Enemy shotTarget = Fresh();
        Projectile shot = GD.Load<PackedScene>("res://scenes/combat/Projectile.tscn").Instantiate<Projectile>();
        AddChild(shot);
        shot.SetPhysicsProcess(false);
        shot.Launch(_player.GlobalPosition, Vector2.Right, 5f, 200f, 2f, 1, false, _player, bow.Base, bow, direct);
        typeof(Projectile).GetMethod("Hit", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(shot, new object[] { shotTarget });
        Check(shotTarget.LastDamageResult.Critical && Near(shotTarget.LastDamageResult.NativeDamage, 5f * _player.CritMultiplier)
            && Near(_player.GetDamageDealt(bow.Id), 5f * _player.CritMultiplier),
            "Œil d'aigle : le projectile résout le critique à l'impact et le registre reçoit les vrais dégâts");
        RemoveChild(shot);
        shot.QueueFree();

        _player.Position = new Vector2(-20000f, -20000f);
        Enemy melee = Fresh(_player.Position + new Vector2(30f, 0f));
        SetEquipped(new WeaponInstance(WeaponDataLoader.Get("chipped_blade")));
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        typeof(Player).GetMethod("PerformMeleeAttack", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(_player, new object[] { AttackPatternKind.Arc });
        Check(melee.LastDamageResult.Critical && melee.LastDamageResult.Source.Weapon?.Id == "chipped_blade",
            "Œil d'aigle : frappe de mêlée renforcée avec l'identité de son arme");
        Free(melee, fixtures);

        Enemy chainFirst = Fresh(_player.Position + new Vector2(35f, 0f));
        Enemy chainNext = Fresh(_player.Position + new Vector2(75f, 0f));
        WeaponInstance chain = new(WeaponDataLoader.Get("chain_of_names"));
        SetEquipped(chain);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        typeof(Player).GetMethod("PerformChainAttack", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_player, null);
        Check(chainFirst.LastDamageResult.Critical && chainNext.LastDamageResult.Critical
            && chainFirst.LastDamageResult.Source.LaunchId == chainNext.LastDamageResult.Source.LaunchId,
            "Œil d'aigle : chaque cible indemne de la chaîne a son propre tirage d'ouverture");
        Free(chainFirst, fixtures);
        Free(chainNext, fixtures);

        Enemy orbital = Fresh();
        WeaponInstance orbit = new(WeaponDataLoader.Get("music_box"));
        typeof(Player).GetField("_orbitalWeapon", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_player, orbit);
        typeof(Player).GetMethod("OrbitalHit", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_player, new object[] { orbital });
        Check(orbital.LastDamageResult.Critical && orbital.LastDamageResult.Source.Weapon == orbit,
            "Œil d'aigle : les contacts orbitaux suivent le même contrat d'impact");

        Enemy fatal = Fresh();
        EnemyKillResult? published = null;
        void CaptureKill(EnemyKillResult kill) { if (kill.Target == fatal.Life) published = kill; }
        _events.EnemyKillResolved += CaptureKill;
        fatal.TakeDamage(fatal.MaxHp, source: _player.BeginAttack(bow, fatal.MaxHp));
        _events.EnemyKillResolved -= CaptureKill;
        Check(published is { Damage.Critical: true } kill && kill.Damage.Source.OwnerId == _player.GetInstanceId()
            && kill.Damage.Source.Weapon == bow, "Œil d'aigle : l'élimination critique conserve les contrats des quêtes");

        foreach (Enemy enemy in fixtures)
        {
            RemoveChild(enemy);
            enemy.QueueFree();
        }
        RemoveChild(perks);
        perks.QueueFree();
    }
    private void CheckOpeningConfiguration()
    {
        foreach (string key in new[] { "base_chance", "gain_per_level", "max_chance" })
        {
            foreach (Variant value in new Variant[] { -1, 2, double.NaN, double.PositiveInfinity, 1e100, "0.1", true })
            {
                Godot.Collections.Dictionary dict = new() { ["base_chance"] = 0.1f, ["gain_per_level"] = 0.0025f, ["max_chance"] = 0.5f };
                dict[key] = value;
                bool refused = false;
                try { PerkDataLoader.ReadOpeningStrike(dict); }
                catch (InvalidOperationException) { refused = true; }
                Check(refused, $"Œil d'aigle : {key}={value} refusé par le contrat");
            }
            Godot.Collections.Dictionary missing = new() { ["base_chance"] = 0.1f, ["gain_per_level"] = 0.0025f, ["max_chance"] = 0.5f };
            missing.Remove(key);
            bool absentRefused = false;
            try { PerkDataLoader.ReadOpeningStrike(missing); }
            catch (InvalidOperationException) { absentRefused = true; }
            Check(absentRefused, $"Œil d'aigle : coefficient absent {key} refusé");
        }
        foreach (Variant value in new Variant[] { "opening_strike", new Godot.Collections.Dictionary
        { ["base_chance"] = 0.1f, ["gain_per_level"] = 0.0025f, ["max_chance"] = 0.5f, ["unknown"] = 1 },
            new Godot.Collections.Dictionary { ["base_chance"] = 0.6f, ["gain_per_level"] = 0.0025f, ["max_chance"] = 0.5f } })
        {
            bool refused = false;
            try { PerkDataLoader.ReadOpeningStrike(value); }
            catch (InvalidOperationException) { refused = true; }
            Check(refused, "Œil d'aigle : mauvais type, clé inconnue ou plafond inversé refusé");
        }
    }

}
