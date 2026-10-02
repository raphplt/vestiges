using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Règles communes de la défense du joueur, lues dans data/characters/defense.json.</summary>
public sealed class DefenseConfig
{
    public float HurtInvulnerabilitySeconds { get; private init; } = 0.25f;
    public float ShieldRechargeDelaySeconds { get; private init; } = 5f;
    public float ShieldRechargeSeconds { get; private init; } = 2f;
    public float ArmorHalfValue { get; private init; } = 15f;
    public float ArmorMaxReduction { get; private init; } = 0.75f;
    /// <summary>Plafond du vol de vie, en part des PV max par seconde (plan 21 G6b).</summary>
    public float LifestealMaxHpPerSecond { get; private init; } = 0.05f;
    /// <summary>Le vol de vie s'accumule et se rend à ce rythme : un soin par intervalle, pas un par coup.</summary>
    public float LifestealTickSeconds { get; private init; } = 0.25f;

    public static DefenseConfig Load()
    {
        FileAccess file = FileAccess.Open("res://data/characters/defense.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[DefenseConfig] Cannot open defense.json");
            return new DefenseConfig();
        }

        Json json = new();
        Error error = json.Parse(file.GetAsText());
        file.Close();
        if (error != Error.Ok)
        {
            GD.PushError($"[DefenseConfig] Parse error: {json.GetErrorMessage()}");
            return new DefenseConfig();
        }

        Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();
        DefenseConfig defaults = new();
        return new DefenseConfig
        {
            HurtInvulnerabilitySeconds = Read(dict, "hurt_invulnerability_seconds", defaults.HurtInvulnerabilitySeconds),
            ShieldRechargeDelaySeconds = Read(dict, "shield_recharge_delay_seconds", defaults.ShieldRechargeDelaySeconds),
            ShieldRechargeSeconds = Read(dict, "shield_recharge_seconds", defaults.ShieldRechargeSeconds),
            ArmorHalfValue = Read(dict, "armor_half_value", defaults.ArmorHalfValue),
            ArmorMaxReduction = Read(dict, "armor_max_reduction", defaults.ArmorMaxReduction),
            LifestealMaxHpPerSecond = Read(dict, "lifesteal_max_hp_per_second", defaults.LifestealMaxHpPerSecond),
            LifestealTickSeconds = Read(dict, "lifesteal_tick_seconds", defaults.LifestealTickSeconds),
        };
    }

    private static float Read(Godot.Collections.Dictionary dict, string key, float fallback) =>
        dict.ContainsKey(key) ? (float)dict[key].AsDouble() : fallback;
}
