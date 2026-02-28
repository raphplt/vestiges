using System;
using System.Collections.Generic;
using System.Linq;
using Vestiges.Infrastructure;

namespace Vestiges.Simulation.MathSim;

/// <summary>
/// État joueur pur C# sans dépendance Node.
/// Miroir des stats combat de Player.cs pour la simulation mathématique.
/// </summary>
public class SimPlayerState
{
    // Base stats (from CharacterData)
    public string CharacterId;
    public string CharacterName;
    public float MaxHp;
    public float CurrentHp;
    public float BaseDamage;
    public float BaseAttackSpeed;
    public float BaseAttackRange;
    public float BaseRegenRate;
    public float ScoreMultiplier = 1f;

    // Weapon
    public string WeaponId;
    public float WeaponDamage;
    public float WeaponAttackSpeed = 1f;
    public int WeaponProjectileCount = 1;
    public int WeaponProjectilePierce;

    // Perk stat modifiers (mirrors Player.cs fields)
    public float DamageMultiplier = 1f;
    public float SpeedMultiplier = 1f;
    public float AttackSpeedMultiplier = 1f;
    public float BonusMaxHp;
    public int ExtraProjectiles;
    public float AoeMultiplier = 1f;
    public float AttackRangeMultiplier = 1f;
    public float BonusRegenRate;
    public float Armor;
    public float CritChance;
    public float CritMultiplier = 2f;
    public int ProjectilePierce;

    // Complex perk effects
    public float VampirismPercent;
    public float BerserkerThreshold;
    public float BerserkerDamageMult = 1f;
    public float ThornsPercent;
    public float ExecutionThreshold;
    public float DodgeChance;
    public bool SecondWindAvailable;
    public float SecondWindHealPercent;
    public float IgniteChance;
    public float IgniteDamage;
    public float IgniteDuration;
    public float RicochetChance;
    public float KillSpeedBonusPerKill;
    public float KillSpeedDuration;
    public int KillSpeedMaxStacks;

    // Kill speed runtime state
    public int KillSpeedActiveStacks;
    public float KillSpeedTimer;

    // Progression
    public int Level = 1;
    public float CurrentXp;
    public float XpToNextLevel;
    public bool HasPendingLevelUp;
    public List<string> PerkIds = new();
    public Dictionary<string, int> PerkStacks = new();

    public float EffectiveMaxHp => MaxHp + BonusMaxHp;
    public float EffectiveRegenRate => BaseRegenRate + BonusRegenRate;
    public float HpRatio => EffectiveMaxHp > 0 ? CurrentHp / EffectiveMaxHp : 0f;

    public static SimPlayerState FromConfig(string characterId)
    {
        CharacterData data = CharacterDataLoader.Get(characterId)
            ?? CharacterDataLoader.Get("traqueur");

        SimPlayerState state = new()
        {
            CharacterId = data.Id,
            CharacterName = data.Name,
            MaxHp = data.BaseStats.MaxHp,
            BaseDamage = data.BaseStats.AttackDamage,
            BaseAttackSpeed = data.BaseStats.AttackSpeed,
            BaseAttackRange = data.BaseStats.AttackRange,
            BaseRegenRate = data.BaseStats.RegenRate,
            ScoreMultiplier = data.ScoreMultiplier
        };
        state.CurrentHp = state.MaxHp;

        // XP for first level
        state.XpToNextLevel = CalculateXpForLevel(1);

        // Starting weapon
        WeaponData weapon = WeaponDataLoader.Get(data.StartingWeaponId)
            ?? WeaponDataLoader.Get("makeshift_bow");

        if (weapon != null)
        {
            state.WeaponId = weapon.Id;
            state.WeaponDamage = weapon.Stats.GetValueOrDefault("damage", state.BaseDamage);
            state.WeaponAttackSpeed = weapon.Stats.GetValueOrDefault("attack_speed", 1f);
            state.WeaponProjectileCount = (int)weapon.Stats.GetValueOrDefault("projectile_count", 1f);
            state.WeaponProjectilePierce = (int)weapon.Stats.GetValueOrDefault("projectile_pierce", 0f);
        }

        // Apply passive perk
        if (!string.IsNullOrEmpty(data.PassivePerk))
        {
            PerkData passive = PerkDataLoader.Get(data.PassivePerk);
            if (passive != null)
                state.ApplyPerk(passive);
        }

        return state;
    }

    public float ComputeDamagePerHit()
    {
        float weaponDamage = WeaponDamage > 0 ? WeaponDamage : BaseDamage;
        float characterFactor = BaseDamage / 10f;
        float damage = weaponDamage * characterFactor * DamageMultiplier;

        if (BerserkerThreshold > 0f && HpRatio < BerserkerThreshold)
            damage *= BerserkerDamageMult;

        return damage;
    }

    public float ComputeAttacksPerSecond()
    {
        float mult = AttackSpeedMultiplier;
        if (KillSpeedActiveStacks > 0 && KillSpeedBonusPerKill > 0f)
            mult *= 1f + KillSpeedBonusPerKill * KillSpeedActiveStacks;

        return BaseAttackSpeed * WeaponAttackSpeed * mult;
    }

    public float ComputeEffectiveDps()
    {
        float damagePerHit = ComputeDamagePerHit();
        float attacksPerSec = ComputeAttacksPerSecond();
        float baseDps = damagePerHit * attacksPerSec;

        // Crit factor (expected value)
        float critFactor = 1f + CritChance * (CritMultiplier - 1f);

        // Projectile count
        int totalProjectiles = WeaponProjectileCount + ExtraProjectiles;

        // Pierce factor (each pierce adds partial DPS vs clustered enemies)
        int totalPierce = WeaponProjectilePierce + ProjectilePierce;
        float pierceFactor = 1f + totalPierce * 0.3f;

        // Ricochet (expected bounce damage)
        float ricochetFactor = 1f + RicochetChance * 0.5f;

        // Ignite DoT (expected value)
        float igniteDps = IgniteChance * IgniteDamage;

        return baseDps * critFactor * totalProjectiles * pierceFactor * ricochetFactor + igniteDps;
    }

    public void GainXp(float amount)
    {
        CurrentXp += amount;
        while (CurrentXp >= XpToNextLevel)
        {
            CurrentXp -= XpToNextLevel;
            Level++;
            XpToNextLevel = CalculateXpForLevel(Level);
            HasPendingLevelUp = true;
        }
    }

    public void ProcessKillSpeedDecay(float dt)
    {
        if (KillSpeedActiveStacks <= 0) return;

        KillSpeedTimer -= dt;
        if (KillSpeedTimer <= 0f)
        {
            KillSpeedActiveStacks = 0;
            KillSpeedTimer = 0f;
        }
    }

    public void OnKill()
    {
        if (KillSpeedBonusPerKill <= 0f) return;

        KillSpeedActiveStacks = Math.Min(KillSpeedActiveStacks + 1, KillSpeedMaxStacks);
        KillSpeedTimer = KillSpeedDuration;
    }

    public void ApplyPerk(PerkData data)
    {
        // Track stacks
        PerkStacks[data.Id] = PerkStacks.GetValueOrDefault(data.Id, 0) + 1;
        PerkIds.Add(data.Id);

        // Simple stat modifiers (from Effects list)
        if (data.Effects != null)
        {
            foreach (PerkEffect effect in data.Effects)
                ApplyStatModifier(effect.Stat, effect.Modifier, effect.ModifierType);
            return;
        }

        // Single stat modifier
        if (!string.IsNullOrEmpty(data.Stat))
        {
            ApplyStatModifier(data.Stat, data.Modifier, data.ModifierType);
        }

        // Complex effect
        if (data.Effect != null)
            ApplyComplexEffect(data);
    }

    private void ApplyStatModifier(string stat, float value, string modifierType)
    {
        switch (stat)
        {
            case "damage":
                if (modifierType == "multiplicative") DamageMultiplier *= value;
                break;
            case "speed":
                if (modifierType == "multiplicative") SpeedMultiplier *= value;
                break;
            case "max_hp":
                if (modifierType == "additive")
                {
                    BonusMaxHp += value;
                    CurrentHp += value;
                }
                else if (modifierType == "multiplicative")
                {
                    float oldMax = EffectiveMaxHp;
                    MaxHp *= value;
                    float newMax = EffectiveMaxHp;
                    CurrentHp = Math.Max(1f, CurrentHp + (newMax - oldMax));
                }
                break;
            case "attack_speed":
                if (modifierType == "multiplicative") AttackSpeedMultiplier *= value;
                break;
            case "projectile_count":
                if (modifierType == "additive") ExtraProjectiles += (int)value;
                break;
            case "aoe_radius":
                if (modifierType == "multiplicative") AoeMultiplier *= value;
                break;
            case "attack_range":
                if (modifierType == "multiplicative") AttackRangeMultiplier *= value;
                break;
            case "regen_rate":
                if (modifierType == "additive") BonusRegenRate += value;
                break;
            case "armor":
                if (modifierType == "additive") Armor += value;
                break;
            case "crit_chance":
                if (modifierType == "additive") CritChance += value;
                break;
            case "crit_multiplier":
                if (modifierType == "additive") CritMultiplier += value;
                break;
            case "projectile_pierce":
                if (modifierType == "additive") ProjectilePierce += (int)value;
                break;
        }
    }

    private void ApplyComplexEffect(PerkData data)
    {
        ComplexEffect fx = data.Effect;

        if (fx.Action == "modify_stat" && fx.Stat != null && fx.Trigger == "passive")
        {
            ApplyStatModifier(fx.Stat, fx.Modifier, fx.ModifierType);
            return;
        }

        if (fx.Trigger == "passive_conditional" && fx.Condition == "hp_percent_below" && fx.Action == "modify_stat")
        {
            BerserkerThreshold = fx.ConditionValue;
            BerserkerDamageMult += (fx.Modifier - 1f);
            return;
        }

        switch (fx.Action)
        {
            case "heal_percent_of_damage":
                VampirismPercent += fx.Value;
                break;
            case "apply_dot":
                IgniteChance += fx.Chance;
                IgniteDamage = Math.Max(IgniteDamage, fx.DotDamage);
                IgniteDuration = Math.Max(IgniteDuration, fx.DotDuration);
                break;
            case "bounce_to_nearby":
                RicochetChance += fx.Chance;
                break;
            case "reflect_damage_percent":
                ThornsPercent += fx.Value;
                break;
            case "execute_below_percent":
                ExecutionThreshold += fx.Value;
                break;
            case "revive":
                SecondWindAvailable = true;
                SecondWindHealPercent = fx.HealPercent;
                break;
            case "temporary_buff":
                KillSpeedBonusPerKill += (fx.Modifier - 1f);
                KillSpeedDuration = Math.Max(KillSpeedDuration, fx.Duration);
                KillSpeedMaxStacks = Math.Max(KillSpeedMaxStacks, fx.MaxBuffStacks);
                break;
            case "dodge":
                DodgeChance += fx.Chance;
                break;
        }
    }

    private static float CalculateXpForLevel(int level)
    {
        return 20f * MathF.Pow(level, 1.35f);
    }
}
