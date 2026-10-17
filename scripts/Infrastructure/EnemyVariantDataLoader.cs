using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// PV et dégâts d'une variante selon le niveau du joueur à son apparition (plan 42 S1, DECISIONS §160). Les dégâts par
/// seconde d'un joueur suivent son niveau bien mieux que le temps de run (matrice M1 : corrélation 0,97 contre 0,78) ;
/// les PV visent donc une durée de combat face au joueur moyen de ce niveau.
/// </summary>
public sealed class VariantLevelScaling
{
    /// <summary>Durée de combat visée face aux dégâts par seconde médians du niveau.</summary>
    public float FightSeconds;
    public float DpsCoefficient;
    public float DpsExponent;
    /// <summary>Échelle des dégâts, à la place de celle du temps : base + pente × niveau.</summary>
    public float DamageBase;
    public float DamagePerLevel;

    public float Hp(int level) => FightSeconds * DpsCoefficient * Mathf.Pow(Mathf.Max(1, level), DpsExponent);

    public float DamageScale(int level) => DamageBase + DamagePerLevel * Mathf.Max(1, level);
}

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
    /// <summary>PV et dégâts calés sur le niveau du joueur plutôt que sur le temps (Souverain, plan 42 S1) ; null sinon.</summary>
    public VariantLevelScaling LevelScaling;
    public float HpMultMin = 1f;
    public float HpMult = 1f;
    public float DamageMult = 1f;
    public float SpeedMult = 1f;
    public float Scale = 1f;
    public float XpMult = 1f;
    public int AffixCount;
    /// <summary>Affixes propres à la variante (plan 30 T2 : pas de Tenace sur un Souverain qui doit tenir 30 s) ; vide = liste commune.</summary>
    public List<string> AffixPool = new();
    public int BonusEssence;
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
    /// <summary>Fin de run (plan 47) : de LateFromMinute à LateFullMinute, les élites passent progressivement aux valeurs Late*.</summary>
    public float LateFromMinute = float.MaxValue;
    public float LateFullMinute = float.MaxValue;
    public float LateIntervalMinSec;
    public float LateIntervalMaxSec;
    public int LateMaxAlive;
    public float LateHpMult = 1f;
    public float LateDamageMult = 1f;
    /// <summary>Part de la montée (0 à 1) à partir de laquelle une élite porte un affixe de plus.</summary>
    public float LateExtraAffixAt = 2f;

    /// <summary>Avancement de la montée de fin de run, de 0 (avant LateFromMinute) à 1 (après LateFullMinute).</summary>
    public float LateRamp(float elapsedMinutes) => LateFullMinute <= LateFromMinute
        ? (elapsedMinutes >= LateFromMinute ? 1f : 0f)
        : Mathf.Clamp((elapsedMinutes - LateFromMinute) / (LateFullMinute - LateFromMinute), 0f, 1f);
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

/// <summary>
/// Variantes, affixes, élites naturelles et modificateurs de phase (data/enemies/_variants.json), lus strictement une
/// fois (plan 26 Q7e-2). Un fichier refusé n'est pas publié et arrête le chargement de la run (GameBootstrap) ; un champ
/// facultatif absent garde la valeur qu'avait le lecteur d'avant.
/// </summary>
public static class EnemyVariantDataLoader
{
    public const string DataPath = "res://data/enemies/_variants.json";

    /// <summary>Variantes que le code nomme (élites naturelles et de micro-événement, Souverains, Aberrations de crise).</summary>
    public static readonly string[] VariantIds = { "elite", "champion", "aberration" };
    private static readonly string[] DeathShakes = { "medium", "heavy" };

    private static Dictionary<string, EnemyVariantData> _variants = new();
    private static Dictionary<string, EnemyAffixData> _affixes = new();
    private static bool _loaded;
    private static string _loadError;

    public static NaturalEliteConfig NaturalElites { get; private set; } = new();
    public static PhaseModifierConfig PhaseModifiers { get; private set; } = new();

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
        if (file == null)
            _loadError = $"{DataPath} : fichier illisible";
        else if (!ChestDataLoader.TryLoad(out string chestError))
            _loadError = $"{DataPath} : {chestError}";
        else
            _loadError = Apply(file.GetAsText(), id => ChestDataLoader.Get(id) != null);
        if (_loadError != null)
        {
            GD.PushError($"[EnemyVariantDataLoader] {_loadError}");
            return;
        }
        GD.Print($"[EnemyVariantDataLoader] Loaded {_variants.Count} variants, {_affixes.Count} affixes");
    }

    /// <summary>
    /// Contrôle <paramref name="text"/> et ne publie qu'un fichier accepté ; rend l'erreur, nulle si tout est publié.
    /// <paramref name="chestExists"/> reconnaît un coffre du catalogue (coffre de récompense).
    /// </summary>
    public static string Apply(string text, Func<string, bool> chestExists)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement root = reader.Root;
            reader.AllowOnly(root, "variantes", "variants", "affixes", "natural_elites", "phase_modifiers");

            Dictionary<string, EnemyAffixData> affixes = new();
            foreach ((string id, JsonElement entry) in reader.Entries(reader.Section(root, "affixes"), "affixes"))
            {
                if (IsObject(reader, entry, $"affixe {id}"))
                    affixes[id] = ParseAffix(reader, id, entry);
            }
            List<string> affixIds = new(affixes.Keys);

            Dictionary<string, EnemyVariantData> variants = new();
            foreach ((string id, JsonElement entry) in reader.Entries(reader.Section(root, "variants"), "variants"))
            {
                if (Array.IndexOf(VariantIds, id) < 0)
                    reader.Fail($"variants : variante « {id} » inconnue du code ({string.Join(", ", VariantIds)})");
                if (IsObject(reader, entry, $"variante {id}"))
                    variants[id] = ParseVariant(reader, id, entry, affixIds, chestExists);
            }
            foreach (string id in VariantIds)
            {
                if (reader.Error == null && !variants.ContainsKey(id))
                    reader.Fail($"variants : variante « {id} » absente");
            }
            NaturalEliteConfig natural = ParseNaturalElites(reader, reader.Section(root, "natural_elites"), affixIds);
            PhaseModifierConfig phase = ParsePhaseModifiers(reader, reader.Section(root, "phase_modifiers"), affixIds);
            if (reader.Error != null)
                return reader.Error;
            _variants = variants;
            _affixes = affixes;
            NaturalElites = natural;
            PhaseModifiers = phase;
            return null;
        }
        catch (JsonException exception)
        {
            return $"JSON invalide : {exception.Message}";
        }
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

    private static EnemyVariantData ParseVariant(JsonConfigReader reader, string id, JsonElement entry, List<string> affixIds,
        Func<string, bool> chestExists)
    {
        string where = $"variante {id}";
        reader.AllowOnly(entry, where, "title_m", "title_f", "hp_target", "hp_mult_min", "hp_mult", "damage_mult", "speed_mult",
            "scale", "xp_mult", "affix_count", "affix_pool", "bonus_essence", "reward_chest", "reward_chest_chance",
            "outline_color", "death_shake", "nameplate", "aberration_shader", "level_scaling");
        EnemyVariantData variant = new()
        {
            Id = id,
            TitleMasculine = OptionalText(reader, entry, "title_m", "", true),
            TitleFeminine = OptionalText(reader, entry, "title_f", "", true),
            HpTarget = NonNegative(reader, entry, "hp_target", 0f),
            HpMultMin = Positive(reader, entry, "hp_mult_min", 1f),
            HpMult = Positive(reader, entry, "hp_mult", 1f),
            DamageMult = Positive(reader, entry, "damage_mult", 1f),
            SpeedMult = Positive(reader, entry, "speed_mult", 1f),
            Scale = Positive(reader, entry, "scale", 1f),
            XpMult = NonNegative(reader, entry, "xp_mult", 1f),
            AffixCount = JsonConfigReader.Has(entry, "affix_count") ? reader.Integer(entry, "affix_count", 0, 8) : 0,
            AffixPool = Affixes(reader, entry, "affix_pool", affixIds, where, false),
            BonusEssence = JsonConfigReader.Has(entry, "bonus_essence") ? reader.Integer(entry, "bonus_essence", 0, 10000) : 0,
            RewardChest = OptionalText(reader, entry, "reward_chest", "", true),
            RewardChestChance = JsonConfigReader.Has(entry, "reward_chest_chance") ? reader.Chance(entry, "reward_chest_chance") : 0f,
            OutlineColor = HtmlColor(reader, entry, "outline_color", "#D4A843"),
            DeathShake = JsonConfigReader.Has(entry, "death_shake") ? reader.OneOf(entry, "death_shake", DeathShakes) : "medium",
            Nameplate = reader.Flag(entry, "nameplate"),
            AberrationShader = reader.Flag(entry, "aberration_shader"),
            LevelScaling = JsonConfigReader.Has(entry, "level_scaling")
                ? ParseLevelScaling(reader, reader.Section(entry, "level_scaling"), where)
                : null,
        };
        if (reader.Error == null && variant.HpMultMin > variant.HpMult)
            reader.Fail(FormattableString.Invariant($"{where} : hp_mult_min ≤ hp_mult attendu ({variant.HpMultMin} > {variant.HpMult})"));
        if (reader.Error == null && variant.RewardChest.Length > 0 && !chestExists(variant.RewardChest))
            reader.Fail($"{where} : reward_chest « {variant.RewardChest} » inconnu");
        return variant;
    }

    private static VariantLevelScaling ParseLevelScaling(JsonConfigReader reader, JsonElement section, string where)
    {
        reader.AllowOnly(section, $"{where}, level_scaling", "fight_seconds", "dps_coefficient", "dps_exponent", "damage_base",
            "damage_per_level");
        return new VariantLevelScaling
        {
            FightSeconds = Positive(reader, section, "fight_seconds", 30f),
            DpsCoefficient = Positive(reader, section, "dps_coefficient", 0.38f),
            DpsExponent = NonNegative(reader, section, "dps_exponent", 2.28f),
            DamageBase = NonNegative(reader, section, "damage_base", 1f),
            DamagePerLevel = NonNegative(reader, section, "damage_per_level", 0f),
        };
    }

    private static EnemyAffixData ParseAffix(JsonConfigReader reader, string id, JsonElement entry)
    {
        reader.AllowOnly(entry, $"affixe {id}", "name_m", "name_f", "color", "hp_mult", "damage_mult", "speed_mult",
            "damage_taken_mult", "regen_ratio_per_sec", "regen_pause_after_hit_sec", "death_explosion_radius",
            "death_explosion_damage_mult");
        string nameMasculine = OptionalText(reader, entry, "name_m", id, false);
        return new EnemyAffixData
        {
            Id = id,
            NameMasculine = nameMasculine,
            NameFeminine = OptionalText(reader, entry, "name_f", nameMasculine, false),
            Color = HtmlColor(reader, entry, "color", "#FFFFFF"),
            HpMult = Positive(reader, entry, "hp_mult", 1f),
            DamageMult = Positive(reader, entry, "damage_mult", 1f),
            SpeedMult = Positive(reader, entry, "speed_mult", 1f),
            DamageTakenMult = Positive(reader, entry, "damage_taken_mult", 1f),
            RegenRatioPerSec = JsonConfigReader.Has(entry, "regen_ratio_per_sec") ? reader.Chance(entry, "regen_ratio_per_sec") : 0f,
            RegenPauseAfterHitSec = NonNegative(reader, entry, "regen_pause_after_hit_sec", 0f),
            DeathExplosionRadius = NonNegative(reader, entry, "death_explosion_radius", 0f),
            DeathExplosionDamageMult = NonNegative(reader, entry, "death_explosion_damage_mult", 0f),
        };
    }

    private static NaturalEliteConfig ParseNaturalElites(JsonConfigReader reader, JsonElement section, List<string> affixIds)
    {
        reader.AllowOnly(section, "natural_elites", "start_sec", "interval_min_sec", "interval_max_sec", "max_alive", "late",
            "affix_pool");
        NaturalEliteConfig config = new()
        {
            StartSec = NonNegative(reader, section, "start_sec", 90f),
            IntervalMinSec = Positive(reader, section, "interval_min_sec", 35f),
            IntervalMaxSec = Positive(reader, section, "interval_max_sec", 55f),
            MaxAlive = JsonConfigReader.Has(section, "max_alive") ? reader.Integer(section, "max_alive", 0, 100) : 2,
            AffixPool = Affixes(reader, section, "affix_pool", affixIds, "natural_elites", false),
        };
        Ordered(reader, "natural_elites", "interval_min_sec", config.IntervalMinSec, "interval_max_sec", config.IntervalMaxSec);
        if (!JsonConfigReader.Has(section, "late"))
            return config;
        JsonElement late = reader.Section(section, "late");
        reader.AllowOnly(late, "natural_elites.late", "from_minute", "full_minute", "interval_min_sec", "interval_max_sec",
            "max_alive", "hp_mult", "damage_mult", "extra_affix_at");
        config.LateFromMinute = NonNegative(reader, late, "from_minute", float.MaxValue);
        config.LateFullMinute = NonNegative(reader, late, "full_minute", config.LateFromMinute);
        config.LateIntervalMinSec = Positive(reader, late, "interval_min_sec", config.IntervalMinSec);
        config.LateIntervalMaxSec = Positive(reader, late, "interval_max_sec", config.IntervalMaxSec);
        config.LateMaxAlive = JsonConfigReader.Has(late, "max_alive") ? reader.Integer(late, "max_alive", 0, 100) : config.MaxAlive;
        config.LateHpMult = Positive(reader, late, "hp_mult", 1f);
        config.LateDamageMult = Positive(reader, late, "damage_mult", 1f);
        config.LateExtraAffixAt = NonNegative(reader, late, "extra_affix_at", 2f);
        Ordered(reader, "natural_elites.late", "interval_min_sec", config.LateIntervalMinSec, "interval_max_sec", config.LateIntervalMaxSec);
        return config;
    }

    private static PhaseModifierConfig ParsePhaseModifiers(JsonConfigReader reader, JsonElement section, List<string> affixIds)
    {
        reader.AllowOnly(section, "phase_modifiers", "crisis_aberration_chance", "crisis_aberration_chance_per_intensity",
            "affix_chance_crisis", "affix_chance_late_game", "affix_chance_endgame", "affix_pool");
        return new PhaseModifierConfig
        {
            CrisisAberrationChance = Chance(reader, section, "crisis_aberration_chance"),
            CrisisAberrationChancePerIntensity = Chance(reader, section, "crisis_aberration_chance_per_intensity"),
            AffixChanceCrisis = Chance(reader, section, "affix_chance_crisis"),
            AffixChanceLateGame = Chance(reader, section, "affix_chance_late_game"),
            AffixChanceEndgame = Chance(reader, section, "affix_chance_endgame"),
            // Un tirage par créature : un affixe répété pèse davantage.
            AffixPool = Affixes(reader, section, "affix_pool", affixIds, "phase_modifiers", true),
        };
    }

    /// <summary>Affixes connus ; sans doublon pour un tirage sans remise (variantes, élites naturelles).</summary>
    private static List<string> Affixes(JsonConfigReader reader, JsonElement owner, string key, List<string> affixIds, string where,
        bool repeatsAllowed)
    {
        List<string> pool = new();
        if (!JsonConfigReader.Has(owner, key))
            return pool;
        foreach (JsonElement item in reader.List(owner, key, 0))
        {
            string id = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            if (id == null || !affixIds.Contains(id))
                reader.Fail($"{where}, {key} : affixe « {id} » inconnu");
            else if (!repeatsAllowed && pool.Contains(id))
                reader.Fail($"{where}, {key} : affixe « {id} » en double");
            else
                pool.Add(id);
        }
        return pool;
    }

    private static bool IsObject(JsonConfigReader reader, JsonElement entry, string where)
    {
        if (entry.ValueKind == JsonValueKind.Object)
            return true;
        reader.Fail($"{where} : objet attendu");
        return false;
    }

    private static void Ordered(JsonConfigReader reader, string where, string lowKey, float low, string highKey, float high)
    {
        if (reader.Error == null && low > high)
            reader.Fail(FormattableString.Invariant($"{where} : {lowKey} ≤ {highKey} attendu ({low} > {high})"));
    }

    /// <summary>Texte facultatif ; <paramref name="emptyAllowed"/> admet "" (pas de titre, pas de coffre).</summary>
    private static string OptionalText(JsonConfigReader reader, JsonElement owner, string key, string fallback, bool emptyAllowed)
    {
        if (!owner.TryGetProperty(key, out JsonElement element))
            return fallback;
        if (element.ValueKind == JsonValueKind.String && (emptyAllowed || element.GetString().Length > 0))
            return element.GetString();
        reader.Fail(emptyAllowed ? $"{key} : texte attendu" : $"{key} : texte non vide attendu");
        return fallback;
    }

    private static Color HtmlColor(JsonConfigReader reader, JsonElement owner, string key, string fallback)
    {
        string html = OptionalText(reader, owner, key, fallback, false);
        if (Color.HtmlIsValid(html))
            return Color.FromHtml(html);
        reader.Fail($"{key} : couleur « {html} » invalide (#RGB, #RRGGBB ou #RRGGBBAA attendu)");
        return Color.FromHtml(fallback);
    }

    private static float Positive(JsonConfigReader reader, JsonElement owner, string key, float fallback) =>
        JsonConfigReader.Has(owner, key) ? reader.Positive(owner, key) : fallback;

    private static float NonNegative(JsonConfigReader reader, JsonElement owner, string key, float fallback) =>
        JsonConfigReader.Has(owner, key) ? reader.NonNegative(owner, key) : fallback;

    private static float Chance(JsonConfigReader reader, JsonElement owner, string key) =>
        JsonConfigReader.Has(owner, key) ? reader.Chance(owner, key) : 0f;
}
