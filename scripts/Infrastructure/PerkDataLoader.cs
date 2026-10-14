using System;
using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class PerkEffect
{
    public string Stat { get; set; }
    public float Modifier { get; set; }
    public string ModifierType { get; set; }
}

/// <summary>Signature de départ d'un personnage : des modificateurs de stats appliqués une fois (data/perks/perks.json).</summary>
public class PerkData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Stat { get; set; }
    public float Modifier { get; set; }
    public string ModifierType { get; set; }
    public bool IsPassive { get; set; }
    public string CharacterId { get; set; }
    public List<PerkEffect> Effects { get; set; }
    /// <summary>Élan d'une signature qui récompense le mouvement continu (Tournée du Facteur), sinon null.</summary>
    public PerkMomentum Momentum { get; set; }
    /// <summary>Rémanence d'une signature qui répète une attaque après la mobilité (Double de l'Éveillée), sinon null.</summary>
    public PerkEcho Echo { get; set; }
    /// <summary>Critique d’ouverture progressant avec le niveau, sinon null.</summary>
    public PerkOpeningStrike OpeningStrike { get; set; }
    /// <summary>Gain offensif près du front de l’Effacement, sinon null.</summary>
    public PerkPressure Pressure { get; set; }
    public PerkRouteEndurance RouteEndurance { get; set; }
    public PerkShockwave Shockwave { get; set; }
}

public sealed record PerkRouteEndurance(int CellsPerCharge, int MaxCharges, float DamageReduction)
{
    public void Validate()
    {
        if (CellsPerCharge < 1 || CellsPerCharge > 1000000 || MaxCharges < 1 || MaxCharges > 16
            || !float.IsFinite(DamageReduction) || DamageReduction <= 0f || DamageReduction >= 1f)
            throw new InvalidOperationException("Endurance : cellules et charges positives, réduction strictement entre 0 et 1.");
    }
}

public sealed record PerkShockwave(int AttacksPerWave, float Radius, float DamageRatio, float Knockback)
{
    public void Validate()
    {
        if (AttacksPerWave < 1 || AttacksPerWave > 128 || !float.IsFinite(Radius) || Radius <= 0f || Radius > 1000f
            || !float.IsFinite(DamageRatio) || DamageRatio <= 0f || DamageRatio > 5f
            || !float.IsFinite(Knockback) || Knockback < 0f || Knockback > 80f)
            throw new InvalidOperationException("Redresser : fréquence positive, rayon et dégâts finis positifs, recul de 0 à 80.");
    }
}

public sealed class PerkOpeningStrike
{
    public float BaseChance { get; }
    public float GainPerLevel { get; }
    public float MaxChance { get; }

    public PerkOpeningStrike(float baseChance, float gainPerLevel, float maxChance)
    {
        if (!float.IsFinite(baseChance) || !float.IsFinite(gainPerLevel) || !float.IsFinite(maxChance)
            || baseChance < 0f || gainPerLevel < 0f || gainPerLevel > 1f || maxChance < baseChance || maxChance > 1f)
            throw new ArgumentOutOfRangeException(nameof(baseChance), "Critique d’ouverture : probabilités finies entre 0 et 1, plafond au moins égal à la base.");
        BaseChance = baseChance;
        GainPerLevel = gainPerLevel;
        MaxChance = maxChance;
    }
}

public sealed class PerkPressure
{
    public float FrayedBonus { get; }
    public float ErasedBonus { get; }

    public PerkPressure(float frayedBonus, float erasedBonus)
    {
        if (!float.IsFinite(frayedBonus) || !float.IsFinite(erasedBonus) || frayedBonus < 0f
            || erasedBonus < frayedBonus || erasedBonus > 10f)
            throw new ArgumentOutOfRangeException(nameof(frayedBonus), "Pression : bonus finis, positifs et croissants, au maximum 10.");
        FrayedBonus = frayedBonus;
        ErasedBonus = erasedBonus;
    }
}

/// <summary>Réglages de la rémanence : sa durée et la part des dégâts du tir qu'elle répète.</summary>
public class PerkEcho
{
    public float Seconds { get; set; }
    public float DamageRatio { get; set; }
}

/// <summary>Réglages de l'élan : bonus au plein, temps pour l'atteindre, arrêt toléré avant la remise à zéro.</summary>
public class PerkMomentum
{
    public float RampSeconds { get; set; }
    public float SpeedBonus { get; set; }
    public float AttackSpeedBonus { get; set; }
    public float StopGraceSeconds { get; set; }
}

public static class PerkDataLoader
{
    private static readonly List<PerkData> _allPerks = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        FileAccess file = FileAccess.Open("res://data/perks/perks.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[PerkDataLoader] Cannot open perks.json");
            return;
        }

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        if (json.Parse(jsonText) != Error.Ok)
        {
            GD.PushError($"[PerkDataLoader] Parse error: {json.GetErrorMessage()}");
            return;
        }

        // Ne publier aucune signature si un réglage du fichier est refusé.
        List<PerkData> parsed = new();
        foreach (Variant item in json.Data.AsGodotArray())
        {
            if (item.VariantType != Variant.Type.Dictionary)
                continue;

            Godot.Collections.Dictionary dict = item.AsGodotDictionary();

            List<PerkEffect> effects = null;
            if (dict.ContainsKey("effects"))
            {
                effects = new List<PerkEffect>();
                foreach (Variant effectItem in dict["effects"].AsGodotArray())
                {
                    Godot.Collections.Dictionary effectDict = effectItem.AsGodotDictionary();
                    effects.Add(new PerkEffect
                    {
                        Stat = effectDict.ContainsKey("stat") ? effectDict["stat"].AsString() : null,
                        Modifier = effectDict.ContainsKey("modifier") ? (float)effectDict["modifier"].AsDouble() : 0f,
                        ModifierType = effectDict.ContainsKey("modifier_type") ? effectDict["modifier_type"].AsString() : null
                    });
                }
            }

            PerkData perk = new()
            {
                Id = dict["id"].AsString(),
                Name = dict["name"].AsString(),
                Description = dict.ContainsKey("description") ? dict["description"].AsString() : "",
                Stat = dict.ContainsKey("stat") ? dict["stat"].AsString() : null,
                Modifier = dict.ContainsKey("modifier") ? (float)dict["modifier"].AsDouble() : 0f,
                ModifierType = dict.ContainsKey("modifier_type") ? dict["modifier_type"].AsString() : null,
                IsPassive = dict.ContainsKey("is_passive") && dict["is_passive"].AsBool(),
                CharacterId = dict.ContainsKey("character_id") ? dict["character_id"].AsString() : null,
                Effects = effects,
                Momentum = dict.ContainsKey("momentum") ? ReadMomentum(dict["momentum"].AsGodotDictionary()) : null,
                Echo = dict.ContainsKey("echo") ? ReadEcho(dict["echo"].AsGodotDictionary()) : null,
                OpeningStrike = dict.ContainsKey("opening_strike") ? ReadOpeningStrike(dict["opening_strike"]) : null,
                Pressure = dict.ContainsKey("pressure") ? ReadPressure(dict["pressure"]) : null,
                RouteEndurance = dict.ContainsKey("route_endurance") ? ReadRouteEndurance(dict["route_endurance"]) : null,
                Shockwave = dict.ContainsKey("shockwave") ? ReadShockwave(dict["shockwave"]) : null,
            };
            parsed.Add(perk);
        }

        _allPerks.AddRange(parsed);
        _loaded = true;
        GD.Print($"[PerkDataLoader] Loaded {_allPerks.Count} character signatures");
    }

    private static PerkMomentum ReadMomentum(Godot.Collections.Dictionary dict) => new()
    {
        RampSeconds = Mathf.Max(0.1f, (float)dict["ramp_seconds"].AsDouble()),
        SpeedBonus = (float)dict["speed_bonus"].AsDouble(),
        AttackSpeedBonus = (float)dict["attack_speed_bonus"].AsDouble(),
        StopGraceSeconds = (float)dict["stop_grace_seconds"].AsDouble(),
    };

    private static PerkEcho ReadEcho(Godot.Collections.Dictionary dict) => new()
    {
        Seconds = Mathf.Max(0.1f, (float)dict["seconds"].AsDouble()),
        DamageRatio = Mathf.Clamp((float)dict["damage_ratio"].AsDouble(), 0f, 1f),
    };

    internal static PerkOpeningStrike ReadOpeningStrike(Variant value)
    {
        Godot.Collections.Dictionary dict = ReadSettings(value, "opening_strike", "base_chance", "gain_per_level", "max_chance");
        try
        {
            return new PerkOpeningStrike(ReadNumber(dict, "base_chance"), ReadNumber(dict, "gain_per_level"), ReadNumber(dict, "max_chance"));
        }
        catch (ArgumentOutOfRangeException exception) { throw new InvalidOperationException(exception.Message, exception); }
    }

    internal static PerkPressure ReadPressure(Variant value)
    {
        Godot.Collections.Dictionary dict = ReadSettings(value, "pressure", "frayed_bonus", "erased_bonus");
        try { return new PerkPressure(ReadNumber(dict, "frayed_bonus"), ReadNumber(dict, "erased_bonus")); }
        catch (ArgumentOutOfRangeException exception) { throw new InvalidOperationException(exception.Message, exception); }
    }

    private static Godot.Collections.Dictionary ReadSettings(Variant value, string name, params string[] allowed)
    {
        if (value.VariantType != Variant.Type.Dictionary)
            throw new InvalidOperationException($"{name} : objet attendu.");
        Godot.Collections.Dictionary dict = value.AsGodotDictionary();
        foreach (Variant key in dict.Keys)
            if (Array.IndexOf(allowed, key.AsString()) < 0)
                throw new InvalidOperationException($"{name} : clé inconnue « {key} ».");
        return dict;
    }

    internal static PerkRouteEndurance ReadRouteEndurance(Variant value)
    {
        Godot.Collections.Dictionary dict = ReadSettings(value, "route_endurance", "cells_per_charge", "max_charges", "damage_reduction");
        PerkRouteEndurance config = new(ReadInteger(dict, "cells_per_charge"), ReadInteger(dict, "max_charges"), ReadNumber(dict, "damage_reduction"));
        config.Validate();
        return config;
    }

    internal static PerkShockwave ReadShockwave(Variant value)
    {
        Godot.Collections.Dictionary dict = ReadSettings(value, "shockwave", "attacks_per_wave", "radius", "damage_ratio", "knockback");
        PerkShockwave config = new(ReadInteger(dict, "attacks_per_wave"), ReadNumber(dict, "radius"), ReadNumber(dict, "damage_ratio"), ReadNumber(dict, "knockback"));
        config.Validate();
        return config;
    }

    private static int ReadInteger(Godot.Collections.Dictionary dict, string key)
    {
        float number = ReadNumber(dict, key);
        if (number < 1f || number > 1000000f || MathF.Truncate(number) != number)
            throw new InvalidOperationException($"Signature.{key} : entier positif requis.");
        return (int)number;
    }

    private static float ReadNumber(Godot.Collections.Dictionary dict, string key)
    {
        if (!dict.TryGetValue(key, out Variant value) || value.VariantType is not (Variant.Type.Float or Variant.Type.Int))
            throw new InvalidOperationException($"Signature.{key} : nombre requis.");
        float number = (float)value.AsDouble();
        if (!float.IsFinite(number))
            throw new InvalidOperationException($"Signature.{key} : nombre fini requis.");
        return number;
    }

    public static List<PerkData> GetAll()
    {
        if (!_loaded)
            Load();

        return _allPerks;
    }
}
