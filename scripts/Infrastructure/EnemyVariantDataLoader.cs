using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Version renforcée d'une créature de base (élite, Souverain, Aberration).</summary>
public class EnemyVariantData
{
    /// <summary>Multiplicateur de PV : vise HpTarget, borné pour garder l'écart entre créatures fragiles et coriaces.</summary>
    public float HpMultiplierFor(float baseHp) =>
        HpTarget > 0f && baseHp > 0f ? Mathf.Clamp(HpTarget / baseHp, HpMultMin, HpMult) : HpMult;

    public string Id;
    public string TitleMasculine;
    public string TitleFeminine;
    /// <summary>PV visés avant mise à l'échelle du temps ; 0 = multiplicateur fixe <see cref="HpMult"/>.</summary>
    public float HpTarget;
    public float HpMultMin = 1f;
    public float HpMult = 1f;
    public float DamageMult = 1f;
    public float SpeedMult = 1f;
    public float Scale = 1f;
    public float XpMult = 1f;
    public int AffixCount;
    public int BonusEssence;
    public float WeaponDropChance;
    public string RewardChest;
    public float RewardChestChance;
    public Color OutlineColor;
    public string DeathShake;
    public bool Nameplate;
    public bool AberrationShader;
}

/// <summary>Propriété lisible ajoutée à une variante : une règle de combat, pas un simple bonus de chiffres.</summary>
public class EnemyAffixData
{
    public string Id;
    public string NameMasculine;
    public string NameFeminine;
    public Color Color;
    public float HpMult = 1f;
    public float DamageMult = 1f;
    public float SpeedMult = 1f;
    public float DamageTakenMult = 1f;
    public float RegenRatioPerSec;
    public float RegenPauseAfterHitSec;
    public float DeathExplosionRadius;
    public float DeathExplosionDamageMult;
}

public class NaturalEliteConfig
{
    public float StartSec = 90f;
    public float IntervalMinSec = 35f;
    public float IntervalMaxSec = 55f;
    public int MaxAlive = 2;
    public List<string> AffixPool = new();
}

public class PhaseModifierConfig
{
    public float CrisisAberrationChance;
    public float CrisisAberrationChancePerIntensity;
    public float AffixChanceCrisis;
    public float AffixChanceLateGame;
    public float AffixChanceEndgame;
    public List<string> AffixPool = new();
}

public static class EnemyVariantDataLoader
{
    private const string DataPath = "res://data/enemies/_variants.json";

    private static readonly Dictionary<string, EnemyVariantData> _variants = new();
    private static readonly Dictionary<string, EnemyAffixData> _affixes = new();
    private static bool _loaded;

    public static NaturalEliteConfig NaturalElites { get; private set; } = new();
    public static PhaseModifierConfig PhaseModifiers { get; private set; } = new();

    public static void Load()
    {
        if (_loaded)
            return;
        _loaded = true;

        FileAccess file = FileAccess.Open(DataPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError($"[EnemyVariantDataLoader] Cannot open {DataPath}");
            return;
        }

        Json json = new();
        Error error = json.Parse(file.GetAsText());
        file.Close();
        if (error != Error.Ok)
        {
            GD.PushError($"[EnemyVariantDataLoader] Parse error: {json.GetErrorMessage()}");
            return;
        }

        Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();
        foreach (KeyValuePair<Variant, Variant> entry in root["variants"].AsGodotDictionary())
            _variants[entry.Key.AsString()] = ParseVariant(entry.Key.AsString(), entry.Value.AsGodotDictionary());
        foreach (KeyValuePair<Variant, Variant> entry in root["affixes"].AsGodotDictionary())
            _affixes[entry.Key.AsString()] = ParseAffix(entry.Key.AsString(), entry.Value.AsGodotDictionary());

        Godot.Collections.Dictionary natural = root["natural_elites"].AsGodotDictionary();
        NaturalElites = new NaturalEliteConfig
        {
            StartSec = Float(natural, "start_sec", 90f),
            IntervalMinSec = Float(natural, "interval_min_sec", 35f),
            IntervalMaxSec = Float(natural, "interval_max_sec", 55f),
            MaxAlive = (int)Float(natural, "max_alive", 2f),
            AffixPool = Strings(natural, "affix_pool")
        };

        Godot.Collections.Dictionary phase = root["phase_modifiers"].AsGodotDictionary();
        PhaseModifiers = new PhaseModifierConfig
        {
            CrisisAberrationChance = Float(phase, "crisis_aberration_chance", 0f),
            CrisisAberrationChancePerIntensity = Float(phase, "crisis_aberration_chance_per_intensity", 0f),
            AffixChanceCrisis = Float(phase, "affix_chance_crisis", 0f),
            AffixChanceLateGame = Float(phase, "affix_chance_late_game", 0f),
            AffixChanceEndgame = Float(phase, "affix_chance_endgame", 0f),
            AffixPool = Strings(phase, "affix_pool")
        };

        GD.Print($"[EnemyVariantDataLoader] Loaded {_variants.Count} variants, {_affixes.Count} affixes");
    }

    public static EnemyVariantData GetVariant(string id)
    {
        Load();
        if (_variants.TryGetValue(id, out EnemyVariantData data))
            return data;
        GD.PushWarning($"[EnemyVariantDataLoader] Unknown variant: {id}");
        return null;
    }

    public static EnemyAffixData GetAffix(string id)
    {
        Load();
        if (_affixes.TryGetValue(id, out EnemyAffixData data))
            return data;
        GD.PushWarning($"[EnemyVariantDataLoader] Unknown affix: {id}");
        return null;
    }

    private static EnemyVariantData ParseVariant(string id, Godot.Collections.Dictionary dict)
    {
        return new EnemyVariantData
        {
            Id = id,
            TitleMasculine = String(dict, "title_m"),
            TitleFeminine = String(dict, "title_f"),
            HpTarget = Float(dict, "hp_target", 0f),
            HpMultMin = Float(dict, "hp_mult_min", 1f),
            HpMult = Float(dict, "hp_mult", 1f),
            DamageMult = Float(dict, "damage_mult", 1f),
            SpeedMult = Float(dict, "speed_mult", 1f),
            Scale = Float(dict, "scale", 1f),
            XpMult = Float(dict, "xp_mult", 1f),
            AffixCount = (int)Float(dict, "affix_count", 0f),
            BonusEssence = (int)Float(dict, "bonus_essence", 0f),
            WeaponDropChance = Float(dict, "weapon_drop_chance", 0f),
            RewardChest = String(dict, "reward_chest"),
            RewardChestChance = Float(dict, "reward_chest_chance", 0f),
            OutlineColor = Color.FromHtml(String(dict, "outline_color", "#D4A843")),
            DeathShake = String(dict, "death_shake", "medium"),
            Nameplate = dict.ContainsKey("nameplate") && dict["nameplate"].AsBool(),
            AberrationShader = dict.ContainsKey("aberration_shader") && dict["aberration_shader"].AsBool()
        };
    }

    private static EnemyAffixData ParseAffix(string id, Godot.Collections.Dictionary dict)
    {
        return new EnemyAffixData
        {
            Id = id,
            NameMasculine = String(dict, "name_m", id),
            NameFeminine = String(dict, "name_f", String(dict, "name_m", id)),
            Color = Color.FromHtml(String(dict, "color", "#FFFFFF")),
            HpMult = Float(dict, "hp_mult", 1f),
            DamageMult = Float(dict, "damage_mult", 1f),
            SpeedMult = Float(dict, "speed_mult", 1f),
            DamageTakenMult = Float(dict, "damage_taken_mult", 1f),
            RegenRatioPerSec = Float(dict, "regen_ratio_per_sec", 0f),
            RegenPauseAfterHitSec = Float(dict, "regen_pause_after_hit_sec", 0f),
            DeathExplosionRadius = Float(dict, "death_explosion_radius", 0f),
            DeathExplosionDamageMult = Float(dict, "death_explosion_damage_mult", 0f)
        };
    }

    private static float Float(Godot.Collections.Dictionary dict, string key, float fallback) =>
        dict.ContainsKey(key) ? (float)dict[key].AsDouble() : fallback;

    private static string String(Godot.Collections.Dictionary dict, string key, string fallback = "") =>
        dict.ContainsKey(key) ? dict[key].AsString() : fallback;

    private static List<string> Strings(Godot.Collections.Dictionary dict, string key)
    {
        List<string> result = new();
        if (!dict.ContainsKey(key))
            return result;
        foreach (Variant value in dict[key].AsGodotArray())
            result.Add(value.AsString());
        return result;
    }
}
