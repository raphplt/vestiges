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

/// <summary>
/// Signatures des personnages (data/perks/perks.json), contrôlées en entier au chargement (plan 26 Q7f-2) : clés connues,
/// identifiants uniques, statistiques que le joueur applique, réglages de chaque mécanique bornés. Un fichier refusé
/// n'est pas publié et arrête le chargement de la run (GameBootstrap). Les blocs restent lus en <see cref="Variant"/> :
/// leurs bancs (PerkEffectsRegression) les appellent ainsi.
/// </summary>
public static class PerkDataLoader
{
    public const string DataPath = "res://data/perks/perks.json";

    private static readonly string[] PerkKeys =
    {
        "id", "name", "description", "stat", "modifier", "modifier_type", "is_passive", "character_id", "effects", "momentum",
        "echo", "opening_strike", "pressure", "route_endurance", "shockwave",
    };

    private static List<PerkData> _allPerks = new();
    private static bool _loaded;
    private static string _loadError;

    /// <summary>Le fichier lu et contrôlé ; faux, avec la raison, s'il a été refusé (la run ne démarre pas).</summary>
    public static bool TryLoad(out string error)
    {
        Load();
        error = _loadError;
        return error == null;
    }

    public static void Load()
    {
        if (_loaded)
            return;
        _loaded = true;
        _loadError = $"{DataPath} : lecture interrompue";
        using FileAccess file = FileAccess.Open(DataPath, FileAccess.ModeFlags.Read);
        _loadError = file == null ? $"{DataPath} : fichier illisible" : Apply(file.GetAsText());
        if (_loadError != null)
        {
            GD.PushError($"[PerkDataLoader] {_loadError}");
            return;
        }
        GD.Print($"[PerkDataLoader] Loaded {_allPerks.Count} character signatures");
    }

    /// <summary>Contrôle <paramref name="text"/> et ne publie qu'un fichier accepté ; rend l'erreur, nulle si tout est publié.</summary>
    public static string Apply(string text)
    {
        using Json json = new();
        if (json.Parse(text) != Error.Ok)
            return $"JSON invalide : {json.GetErrorMessage()}";
        if (json.Data.VariantType != Variant.Type.Array)
            return "liste de signatures attendue à la racine";
        List<PerkData> parsed = new();
        HashSet<string> ids = new();
        foreach (Variant item in json.Data.AsGodotArray())
        {
            // Un texte commençant par « _ » commente la liste.
            if (item.VariantType == Variant.Type.String && item.AsString().StartsWith('_'))
                continue;
            string where = item.VariantType == Variant.Type.Dictionary && item.AsGodotDictionary().TryGetValue("id", out Variant id)
                ? $"signature « {id} »" : "signature sans identifiant";
            try
            {
                PerkData perk = ReadPerk(item);
                if (!ids.Add(perk.Id))
                    throw new InvalidOperationException("identifiant déjà déclaré.");
                parsed.Add(perk);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
            {
                return $"{where} : {exception.Message}";
            }
        }
        if (parsed.Count == 0)
            return "aucune signature";
        _allPerks = parsed;
        return null;
    }

    private static PerkData ReadPerk(Variant item)
    {
        Godot.Collections.Dictionary dict = ReadSettings(item, "signature", PerkKeys);
        (string stat, float modifier, string modifierType) = ReadModifier(dict, "signature", optional: true);
        List<PerkEffect> effects = null;
        if (dict.TryGetValue("effects", out Variant effectsValue))
        {
            if (effectsValue.VariantType != Variant.Type.Array)
                throw new InvalidOperationException("effects : liste attendue.");
            effects = new List<PerkEffect>();
            foreach (Variant effectItem in effectsValue.AsGodotArray())
            {
                (string effectStat, float effectModifier, string effectType) = ReadModifier(
                    ReadSettings(effectItem, "effects", "stat", "modifier", "modifier_type"), "effects", optional: false);
                effects.Add(new PerkEffect { Stat = effectStat, Modifier = effectModifier, ModifierType = effectType });
            }
        }
        return new PerkData
        {
            Id = ReadText(dict, "id", required: true),
            Name = ReadText(dict, "name", required: true),
            Description = ReadText(dict, "description", required: false) ?? "",
            Stat = stat,
            Modifier = modifier,
            ModifierType = modifierType,
            IsPassive = dict.ContainsKey("is_passive") && ReadFlag(dict, "is_passive"),
            CharacterId = ReadText(dict, "character_id", required: false),
            Effects = effects,
            Momentum = dict.TryGetValue("momentum", out Variant momentum) ? ReadMomentum(momentum) : null,
            Echo = dict.TryGetValue("echo", out Variant echo) ? ReadEcho(echo) : null,
            OpeningStrike = dict.TryGetValue("opening_strike", out Variant opening) ? ReadOpeningStrike(opening) : null,
            Pressure = dict.TryGetValue("pressure", out Variant pressure) ? ReadPressure(pressure) : null,
            RouteEndurance = dict.TryGetValue("route_endurance", out Variant route) ? ReadRouteEndurance(route) : null,
            Shockwave = dict.TryGetValue("shockwave", out Variant shockwave) ? ReadShockwave(shockwave) : null,
        };
    }

    /// <summary>Statistique appliquée au joueur : les trois champs ensemble, un couple que le joueur sait appliquer.</summary>
    private static (string Stat, float Modifier, string ModifierType) ReadModifier(Godot.Collections.Dictionary dict, string where,
        bool optional)
    {
        if (optional && !dict.ContainsKey("stat") && !dict.ContainsKey("modifier") && !dict.ContainsKey("modifier_type"))
            return (null, 0f, null);
        string stat = ReadText(dict, "stat", required: true);
        string modifierType = ReadText(dict, "modifier_type", required: true);
        if (!PlayerModifiers.Applies(stat, modifierType))
            throw new InvalidOperationException($"{where} : « {stat} » en {modifierType} n'est pas appliqué au joueur.");
        return (stat, ReadNumber(dict, "modifier"), modifierType);
    }

    private static string ReadText(Godot.Collections.Dictionary dict, string key, bool required)
    {
        if (!dict.TryGetValue(key, out Variant value))
            return required ? throw new InvalidOperationException($"{key} : texte requis.") : null;
        if (value.VariantType != Variant.Type.String || (required && value.AsString().Length == 0))
            throw new InvalidOperationException($"{key} : texte{(required ? " non vide" : "")} requis.");
        return value.AsString();
    }

    private static bool ReadFlag(Godot.Collections.Dictionary dict, string key)
    {
        if (dict[key].VariantType != Variant.Type.Bool)
            throw new InvalidOperationException($"{key} : booléen requis.");
        return dict[key].AsBool();
    }

    internal static PerkMomentum ReadMomentum(Variant value)
    {
        Godot.Collections.Dictionary dict = ReadSettings(value, "momentum", "ramp_seconds", "speed_bonus", "attack_speed_bonus",
            "stop_grace_seconds");
        PerkMomentum config = new()
        {
            RampSeconds = ReadNumber(dict, "ramp_seconds"),
            SpeedBonus = ReadNumber(dict, "speed_bonus"),
            AttackSpeedBonus = ReadNumber(dict, "attack_speed_bonus"),
            StopGraceSeconds = ReadNumber(dict, "stop_grace_seconds"),
        };
        if (config.RampSeconds < 0.1f || config.RampSeconds > 60f || config.SpeedBonus < 0f || config.SpeedBonus > 5f
            || config.AttackSpeedBonus < 0f || config.AttackSpeedBonus > 5f || config.StopGraceSeconds < 0f || config.StopGraceSeconds > 10f)
            throw new InvalidOperationException("Élan : montée de 0,1 à 60 s, bonus de 0 à 5, arrêt toléré de 0 à 10 s.");
        return config;
    }

    internal static PerkEcho ReadEcho(Variant value)
    {
        Godot.Collections.Dictionary dict = ReadSettings(value, "echo", "seconds", "damage_ratio");
        PerkEcho config = new() { Seconds = ReadNumber(dict, "seconds"), DamageRatio = ReadNumber(dict, "damage_ratio") };
        if (config.Seconds < 0.1f || config.Seconds > 30f || config.DamageRatio < 0f || config.DamageRatio > 1f)
            throw new InvalidOperationException("Rémanence : durée de 0,1 à 30 s, part des dégâts de 0 à 1.");
        return config;
    }

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
            if (!key.AsString().StartsWith('_') && Array.IndexOf(allowed, key.AsString()) < 0)
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
