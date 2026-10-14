using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.Tests;

public partial class PerkEffectsRegression
{
    private async Task CheckPressure()
    {
        CheckPressureConfiguration();
        Setup();
        PerkManager perks = new();
        AddChild(perks);
        perks.ApplyPassivePerks("scaphandriere");
        ErasurePressure pressure = _player.GetNodeOrNull<ErasurePressure>("ErasurePressure");
        Check(pressure != null, "Pression : signature réelle branchée par PerkManager");
        if (pressure == null) return;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo penaltyField = typeof(Player).GetField("_erasurePenalty", flags);
        FieldInfo speedField = typeof(Player).GetField("_speedMultiplier", flags);
        MethodInfo damageMethod = typeof(Player).GetMethod("ComputeBaseAttackDamage", flags, null,
            new[] { typeof(WeaponInstance) }, null);
        ErasureEffects.Effect Penalty() => (ErasureEffects.Effect)penaltyField.GetValue(_player);
        float Damage() => (float)damageMethod.Invoke(_player, new object[] { _player.WeaponSlots[0] });
        void Phase(ErasureManager.ErasureZonePhase phase) =>
            _events.EmitSignal(EventBus.SignalName.PlayerErasurePhaseChanged, (int)phase);
        Phase(ErasureManager.ErasureZonePhase.Anchored);
        _player.ApplyPerkModifier("speed", 1.2f, "multiplicative");
        _player.ApplyPerkModifier("attack_damage", 1.3f, "multiplicative");
        float baseDamage = Damage();
        float speed = (float)speedField.GetValue(_player);
        foreach ((ErasureManager.ErasureZonePhase phase, float multiplier, int intensity) in new[]
        {
            (ErasureManager.ErasureZonePhase.Anchored, 1f, 0), (ErasureManager.ErasureZonePhase.Fragile, 1f, 0),
            (ErasureManager.ErasureZonePhase.Frayed, 1.25f, 1), (ErasureManager.ErasureZonePhase.Erased, 1.5f, 2),
            (ErasureManager.ErasureZonePhase.Void, 1f, 0),
        })
        {
            Phase(phase);
            ErasureEffects.Effect effect = ErasureEffects.For(phase);
            Check(Near(pressure.DamageMultiplier, multiplier) && Near(Damage(), baseDamage * effect.Damage * multiplier)
                && Penalty() == effect && pressure.Intensity == intensity && pressure.Visible == (intensity > 0),
                $"Pression : {phase}, dégâts ×{effect.Damage * multiplier:0.000}, malus inchangés et feedback={intensity}");
        }
        Phase(ErasureManager.ErasureZonePhase.Erased);
        float hp = _player.CurrentHp;
        _player.TakeErasureDamage(3f);
        Check(Near(_player.CurrentHp, hp - 3f), "Pression : aucun effet sur les dégâts reçus de l’Effacement");
        for (int i = 0; i < 30; i++)
        {
            Phase(ErasureManager.ErasureZonePhase.Frayed);
            Phase(ErasureManager.ErasureZonePhase.Erased);
            Phase(ErasureManager.ErasureZonePhase.Anchored);
        }
        Check(Near(Damage(), baseDamage) && Near((float)speedField.GetValue(_player), speed),
            "Pression : trente cycles sans dérive des stats ou de la vitesse");
        GetTree().Paused = true;
        try
        {
            Phase(ErasureManager.ErasureZonePhase.Erased);
            float damage = Damage();
            for (int i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(Near(Damage(), damage) && Near(pressure.DamageMultiplier, 1.5f),
                "Pression : vraie pause, bonus stable sans minuterie ni rattrapage");
        }
        finally { GetTree().Paused = false; }
        GameManager manager = GetNode<GameManager>("/root/GameManager");
        manager.ChangeState(GameManager.GameState.Hub);
        Check(!pressure.Visible && Near(pressure.DamageMultiplier, 1f), "Pression : hors run, bonus retiré et feedback masqué");
        manager.ChangeState(GameManager.GameState.Run);
        Check(Near(pressure.DamageMultiplier, 1.5f), "Pression : retour Run relit la phase sans empiler le bonus");
        perks.ApplyPassivePerks("scaphandriere");
        Check(_player.GetNode<ErasurePressure>("ErasurePressure") == pressure && Near(pressure.DamageMultiplier, 1.5f),
            "Pression : branchement répété ne double ni module ni effet");
        _player.RemoveChild(pressure);
        Check(Near(Damage(), baseDamage * ErasureEffects.For(ErasureManager.ErasureZonePhase.Erased).Damage),
            "Pression : retirer le module retire uniquement le gain offensif");
        pressure.QueueFree();
        ErasureManager erasure = new();
        AddChild(erasure);
        erasure.SetProcess(false);
        Dictionary<Vector2I, ErasureManager.ErasureZonePhase> phases =
            (Dictionary<Vector2I, ErasureManager.ErasureZonePhase>)typeof(ErasureManager).GetField("_zonePhases", flags).GetValue(erasure);
        phases[Vector2I.Zero] = ErasureManager.ErasureZonePhase.Erased;
        Phase(ErasureManager.ErasureZonePhase.Anchored);
        typeof(Player).GetField("_erasureManager", flags).SetValue(_player, erasure);
        perks.ApplyPassivePerks("scaphandriere");
        pressure = _player.GetNode<ErasurePressure>("ErasurePressure");
        Check(pressure.Intensity == 2 && Near(pressure.DamageMultiplier, 1.5f),
            "Pression : ajout tardif en vrai sol effacé relit la phase immédiatement");
        _player.TakeErasureDamage(_player.EffectiveMaxHp * 2f);
        Check(_player.IsDead && !pressure.Visible && Near(pressure.DamageMultiplier, 1f),
            "Pression : mort retire immédiatement le bonus et masque le feedback");
        RemoveChild(erasure);
        erasure.QueueFree();
        Setup();
        perks.ApplyPassivePerks("scaphandriere");
        Check(Near(_player.GetNode<ErasurePressure>("ErasurePressure").DamageMultiplier, 1f),
            "Pression : nouvelle run sans phase ni bonus hérités");
        perks.QueueFree();
    }

    private void CheckPressureConfiguration()
    {
        PerkPressure config = PerkDataLoader.GetAll().Find(perk => perk.CharacterId == "scaphandriere")?.Pressure;
        Check(config != null && Near(config.FrayedBonus, 0.25f) && Near(config.ErasedBonus, 0.5f),
            "Pression : coefficients réellement lus en JSON");
        foreach (string key in new[] { "frayed_bonus", "erased_bonus" })
        {
            foreach (Variant value in new Variant[] { -1, double.NaN, double.PositiveInfinity, 1e100, "0.25", true })
            {
                Godot.Collections.Dictionary dict = new() { ["frayed_bonus"] = 0.25f, ["erased_bonus"] = 0.5f };
                dict[key] = value;
                bool refused = false;
                try { PerkDataLoader.ReadPressure(dict); }
                catch (InvalidOperationException) { refused = true; }
                Check(refused, $"Pression : {key}={value} refusé");
            }
            Godot.Collections.Dictionary missing = new() { ["frayed_bonus"] = 0.25f, ["erased_bonus"] = 0.5f };
            missing.Remove(key);
            bool absentRefused = false;
            try { PerkDataLoader.ReadPressure(missing); }
            catch (InvalidOperationException) { absentRefused = true; }
            Check(absentRefused, $"Pression : coefficient absent {key} refusé");
        }
        foreach (Variant value in new Variant[] { "pressure", new Godot.Collections.Dictionary
        { ["frayed_bonus"] = 0.25f, ["erased_bonus"] = 0.5f, ["unknown"] = 1 },
            new Godot.Collections.Dictionary { ["frayed_bonus"] = 1f, ["erased_bonus"] = 0.5f } })
        {
            bool refused = false;
            try { PerkDataLoader.ReadPressure(value); }
            catch (InvalidOperationException) { refused = true; }
            Check(refused, "Pression : mauvais type, clé inconnue ou intensités inversées refusés");
        }
    }
}
