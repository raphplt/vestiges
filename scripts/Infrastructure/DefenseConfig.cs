using System;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Règles communes de la défense du joueur (data/characters/defense.json), lues strictement une fois (plan 26 Q7g-3).
/// Un fichier refusé arrête le chargement de la run (GameBootstrap) ; un champ absent garde sa valeur d'avant.
/// </summary>
public sealed class DefenseConfig
{
    public float HurtInvulnerabilitySeconds { get; private init; } = 0.25f;
    /// <summary>Chaque perte de PV suspend la régénération passive pendant ce délai.</summary>
    public float RegenDelayAfterDamageSeconds { get; private init; } = 2f;
    public float ShieldRechargeDelaySeconds { get; private init; } = 5f;
    public float ShieldRechargeSeconds { get; private init; } = 2f;
    public float ArmorHalfValue { get; private init; } = 15f;
    public float ArmorMaxReduction { get; private init; } = 0.75f;
    /// <summary>Plafond du vol de vie, en part des PV max par seconde (plan 21 G6b).</summary>
    public float LifestealMaxHpPerSecond { get; private init; } = 0.05f;
    /// <summary>Le vol de vie s'accumule et se rend à ce rythme : un soin par intervalle, pas un par coup.</summary>
    public float LifestealTickSeconds { get; private init; } = 0.25f;

    public const string FilePath = "res://data/characters/defense.json";

    private static DefenseConfig _loaded;
    private static string _loadError;
    private static bool _attempted;

    /// <summary>Les réglages du jeu ; refusés, une exception qui nomme le champ (le bootstrap les a déjà contrôlés).</summary>
    public static DefenseConfig Load()
    {
        if (!TryLoad(out DefenseConfig config, out string error))
            throw new InvalidOperationException(error);
        return config;
    }

    public static bool TryLoad(out DefenseConfig config, out string error)
    {
        if (!_attempted)
        {
            _attempted = true;
            _loadError = $"{FilePath} : lecture interrompue";
            using FileAccess file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
            if (file == null)
                _loadError = $"{FilePath} : fichier illisible";
            else if (TryParse(file.GetAsText(), out _loaded, out _loadError))
                _loadError = null;
            else
                _loadError = $"{FilePath} : {_loadError}";
        }
        config = _loaded;
        error = _loadError;
        return error == null;
    }

    public static bool TryParse(string text, out DefenseConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement root = reader.Root;
            reader.AllowOnly(root, "défense", "hurt_invulnerability_seconds", "regen_delay_after_damage_seconds",
                "shield_recharge_delay_seconds", "shield_recharge_seconds", "armor_half_value", "armor_max_reduction",
                "lifesteal_max_hp_per_second", "lifesteal_tick_seconds");
            DefenseConfig defaults = new();
            DefenseConfig parsed = new()
            {
                HurtInvulnerabilitySeconds = Read(root, "hurt_invulnerability_seconds", defaults.HurtInvulnerabilitySeconds, reader.NonNegative),
                RegenDelayAfterDamageSeconds = Read(root, "regen_delay_after_damage_seconds", defaults.RegenDelayAfterDamageSeconds, reader.NonNegative),
                ShieldRechargeDelaySeconds = Read(root, "shield_recharge_delay_seconds", defaults.ShieldRechargeDelaySeconds, reader.NonNegative),
                ShieldRechargeSeconds = Read(root, "shield_recharge_seconds", defaults.ShieldRechargeSeconds, reader.Positive),
                ArmorHalfValue = Read(root, "armor_half_value", defaults.ArmorHalfValue, reader.Positive),
                ArmorMaxReduction = Read(root, "armor_max_reduction", defaults.ArmorMaxReduction, reader.Chance),
                LifestealMaxHpPerSecond = Read(root, "lifesteal_max_hp_per_second", defaults.LifestealMaxHpPerSecond, reader.Chance),
                LifestealTickSeconds = Read(root, "lifesteal_tick_seconds", defaults.LifestealTickSeconds, reader.Positive),
            };
            error = reader.Error;
            if (error != null)
                return false;
            config = parsed;
            return true;
        }
        catch (JsonException exception)
        {
            error = $"JSON invalide : {exception.Message}";
            return false;
        }
    }

    private static float Read(JsonElement owner, string key, float fallback, Func<JsonElement, string, float> rule) =>
        JsonConfigReader.Has(owner, key) ? rule(owner, key) : fallback;
}
