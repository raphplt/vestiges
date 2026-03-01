using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Simulation;

public enum PerkStrategyType
{
    Random,
    Survival,
    Damage,
    Balanced
}

/// <summary>
/// Stratégie de sélection de perks pour l'IA.
/// Évalue chaque perk proposé et choisit selon le profil de stratégie.
/// </summary>
public class PerkStrategy
{
    private readonly PerkStrategyType _type;
    public PerkStrategyType StrategyType => _type;

    private static readonly HashSet<string> SurvivalStats = new()
        { "max_hp", "regen_rate", "armor", "speed" };

    private static readonly HashSet<string> DamageStats = new()
        { "damage", "attack_speed", "crit_chance", "crit_multiplier", "projectile_count", "projectile_pierce" };

    public PerkStrategy(PerkStrategyType type)
    {
        _type = type;
    }

    public string SelectPerk(string[] choices, Player player)
    {
        if (choices.Length == 0) return null;
        if (choices.Length == 1 || _type == PerkStrategyType.Random)
            return choices[(int)(GD.Randi() % (uint)choices.Length)];

        string best = choices[0];
        float bestScore = -1f;

        foreach (string perkId in choices)
        {
            PerkData data = PerkDataLoader.Get(perkId);
            if (data == null) continue;

            float score = ScorePerk(data, player);
            if (score > bestScore)
            {
                bestScore = score;
                best = perkId;
            }
        }
        return best;
    }

    private float ScorePerk(PerkData data, Player player)
    {
        float score = 1f;

        string stat = data.Stat ?? "";
        bool isSurvival = SurvivalStats.Contains(stat);
        bool isDamage = DamageStats.Contains(stat);

        // Complex effects get a base bonus
        bool hasComplexEffect = data.Effect != null;
        if (hasComplexEffect)
        {
            string action = data.Effect.Action ?? "";
            isSurvival = isSurvival || action is "revive" or "dodge" or "reflect_damage_percent" or "heal_percent_of_damage";
            isDamage = isDamage || action is "apply_dot" or "bounce_to_nearby" or "execute_below_percent" or "temporary_buff";
        }

        switch (_type)
        {
            case PerkStrategyType.Survival:
                score = isSurvival ? 10f : isDamage ? 3f : 5f;
                if (player.CurrentHp / player.EffectiveMaxHp < 0.5f && stat is "regen_rate" or "max_hp")
                    score *= 2f;
                break;

            case PerkStrategyType.Damage:
                score = isDamage ? 10f : isSurvival ? 3f : 5f;
                break;

            case PerkStrategyType.Balanced:
                score = data.Weight * 5f;
                if (isSurvival) score *= 1.5f;
                if (isDamage) score *= 1.5f;
                break;
        }

        score *= data.Rarity switch
        {
            "rare" => 1.5f,
            "uncommon" => 1.2f,
            _ => 1f
        };

        return score;
    }

    public static PerkStrategy FromName(string name) => new(name switch
    {
        "random" => PerkStrategyType.Random,
        "survival" => PerkStrategyType.Survival,
        "damage" => PerkStrategyType.Damage,
        "balanced" => PerkStrategyType.Balanced,
        _ => PerkStrategyType.Random
    });
}
