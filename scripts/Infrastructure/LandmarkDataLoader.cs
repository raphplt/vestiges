using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Un groupe de lieux : combien, et dans quelle couronne de distance au départ (fraction du rayon).</summary>
public readonly record struct LandmarkBand(int Count, float Min, float Max);

/// <summary>Réglages des Mémoriaux (section <c>memorial</c> de data/world/landmarks.json).</summary>
public class MemorialConfig
{
    public string SpriteDormant;
    public string SpriteAwake;
    public string SpriteShard;
    public List<LandmarkBand> Placement = new();
    public float MinSpacingPx;
    public float HoldTime;
    public int Shards;
    public float ShardDistanceMin;
    public float ShardDistanceMax;
    public float ShardTime;
    /// <summary>Dernières secondes du ramassage : le tic-tac presse et le compte à rebours rougit (DECISIONS §154).</summary>
    public float ShardUrgentTime;
    public float ShardPickupPx;
    /// <summary>Cercle à tenir des terres (plan 51 P2b) : durée, rayon, redescente hors du cercle, abandon, foule.</summary>
    public float CircleSeconds;
    public float CircleRadiusPx;
    public float CircleLeaveDecay;
    public float CircleAbandonPx;
    public float CircleCrowdDensity;
    /// <summary>Vagues du cercle (plan 57 C4) : parts tenues où elles montent du bord, créatures par vague, annonce au sol.</summary>
    public float[] CircleWaveAt;
    public int CircleWaveCount;
    public float CircleWaveWarning;
    public string CircleWaveAudio;
    /// <summary>Dès cette part tenue, le cercle se resserre jusqu'à cette part de son rayon.</summary>
    public float CircleCloseFrom;
    public float CircleCloseShare;
    /// <summary>Onde du Mémorial ravivé : portée, part des PV ôtée à chaque créature, poussée, son.</summary>
    public float CircleBurstRadiusPx;
    public float CircleBurstDamageShare;
    public float CircleBurstPushPx;
    public string CircleBurstAudio;
    /// <summary>Le joueur sort du cercle : l'anneau rougit et ce son l'avertit.</summary>
    public string CircleLeaveAudio;
    public int BlessingChoices;
    public string BlessingMinRarity;
    public int HealCost;
    public float HealPercent;
    public float CostGrowth;
    public int LiftOubliCost;
    /// <summary>Relancer les trois bénédictions d'un Mémorial qu'on vient de raviver (plan 17, 4.5).</summary>
    public int BlessingRerollCost;
}

/// <summary>Réglages des Ateliers (section <c>workshop</c> de data/world/landmarks.json, plan 22 C2).</summary>
public class WorkshopConfig
{
    /// <summary>Posés en run ; en réserve sinon (plan 56 B2, <see cref="ReserveSwitch"/>).</summary>
    public bool Enabled;
    public string Sprite;
    public List<LandmarkBand> Placement = new();
    public float MinSpacingPx;
    public int WeaponCost;
    public string WeaponMinRarity;
    public int RetemperCost;
    /// <summary>Prix de l'objet que chaque Atelier met en vente (plan 35 I3).</summary>
    public int ItemCost;
    public int TemperUpgrades;
    public float CostGrowth;
}

/// <summary>Réglages des Failles (section <c>rift</c> de data/world/landmarks.json).</summary>
public class RiftConfig
{
    public string SpriteOpen;
    public string SpriteClosed;
    public List<LandmarkBand> Placement = new();
    public float MinSpacingPx;
    public float HoldTime;
    public int Offers;
    public string OfferMinRarity;
    public int PerilPerOffer;
    public int MaxOpen;
    public float SpawnChance;
    public float SpawnCooldown;
    public float SpawnDistanceMin;
    public float SpawnDistanceMax;
}

/// <summary>
/// Réglages des Puits (section <c>well</c> de data/world/landmarks.json, plan 44) : placement, prix du premier oubli de la
/// run, hausse par oubli, part des niveaux gardée en trace.
/// </summary>
public class WellConfig
{
    /// <summary>Posés en run ; en réserve sinon (plan 56 B2, <see cref="ReserveSwitch"/>).</summary>
    public bool Enabled;
    public string Sprite;
    public List<LandmarkBand> Placement = new();
    public float MinSpacingPx;
    public int ForgetCost;
    public float ForgetCostGrowth;
    public float ForgetTraceRatio;
}

/// <summary>Réglages des stèles du Péril (section <c>peril_stele</c> de data/world/landmarks.json, plan 28 P2).</summary>
public class PerilSteleConfig
{
    /// <summary>Posées en run ; en réserve sinon (plan 56 B2, <see cref="ReserveSwitch"/>).</summary>
    public bool Enabled;
    public string SpriteLit;
    public string SpriteSpent;
    public List<LandmarkBand> Placement = new();
    public float MinSpacingPx;
    public float HoldTime;
    public int Peril;
}

/// <summary>
/// Lieux du monde à trouver hors coffres : Mémoriaux, Ateliers, Failles, Puits et stèles du Péril (data/world/landmarks.json), contrôlés en
/// entier (plan 26 Q7c) : tous les champs sont obligatoires, les raretés minimales existent, les images aussi.
/// </summary>
public static class LandmarkDataLoader
{
    private const string ConfigPath = "res://data/world/landmarks.json";
    private static MemorialConfig _memorial;
    private static RiftConfig _rift;
    private static WorkshopConfig _workshop;
    private static WellConfig _well;
    private static PerilSteleConfig _perilStele;
    private static string _loadError;

    public static MemorialConfig Memorial
    {
        get
        {
            Load();
            return _memorial;
        }
    }

    public static WorkshopConfig Workshop
    {
        get
        {
            Load();
            return _workshop;
        }
    }

    public static WellConfig Well
    {
        get
        {
            Load();
            return _well;
        }
    }

    public static RiftConfig Rift
    {
        get
        {
            Load();
            return _rift;
        }
    }

    public static PerilSteleConfig PerilStele
    {
        get
        {
            Load();
            return _perilStele;
        }
    }

    /// <summary>Réglages lus et contrôlés ; faux, avec la raison, s'ils ont été refusés (le chargement de la run s'arrête).</summary>
    public static bool TryLoad(out string error)
    {
        Load();
        error = _loadError;
        return error == null;
    }

    private static void Load()
    {
        if (_memorial != null)
            return;
        // Refusés, les réglages restent vides : la run ne démarre pas (GameBootstrap).
        _memorial = new MemorialConfig();
        _rift = new RiftConfig();
        _workshop = new WorkshopConfig();
        _well = new WellConfig();
        _perilStele = new PerilSteleConfig();
        string error = FileAccess.FileExists(ConfigPath)
            ? Apply(FileAccess.GetFileAsString(ConfigPath), path => ResourceLoader.Exists(path), RarityIds())
            : "absent";
        if (error != null)
        {
            _loadError = $"{ConfigPath} : {error}";
            GD.PushError($"[LandmarkDataLoader] {_loadError}");
        }
    }

    /// <summary>Raretés d'amélioration connues : les raretés minimales des lieux en font partie.</summary>
    private static HashSet<string> RarityIds() => DataKeySets.ListIds("res://data/progression/upgrade_rarities.json", "rarities");

    /// <summary>Contrôle un texte de réglages ; ne le publie que s'il est entièrement valide. Rend l'erreur, ou null.</summary>
    public static string Apply(string json, Func<string, bool> exists, IReadOnlyCollection<string> rarityIds)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            reader.AllowOnly(reader.Root, "lieux", "memorial", "rift", "workshop", "well", "peril_stele");

            JsonElement m = reader.Section("memorial");
            reader.AllowOnly(m, "memorial", "sprite_dormant", "sprite_awake", "sprite_shard", "placement", "min_spacing_px",
                "hold_time", "shards", "shard_distance_px", "shard_time", "shard_urgent_time", "shard_pickup_px", "blessing_choices",
                "blessing_min_rarity", "services", "circle");
            JsonElement mc = reader.Section(m, "circle");
            reader.AllowOnly(mc, "memorial.circle", "seconds", "radius_px", "leave_decay", "abandon_px", "crowd_density", "waves",
                "closing", "burst", "leave_audio");
            JsonElement waves = reader.Section(mc, "waves");
            reader.AllowOnly(waves, "memorial.circle.waves", "at", "count", "warning_seconds", "audio");
            JsonElement closing = reader.Section(mc, "closing");
            reader.AllowOnly(closing, "memorial.circle.closing", "from", "radius_share");
            JsonElement burst = reader.Section(mc, "burst");
            reader.AllowOnly(burst, "memorial.circle.burst", "radius_px", "damage_share", "push_px", "audio");
            JsonElement ms = reader.Section(m, "services");
            reader.AllowOnly(ms, "memorial.services", "heal_cost", "heal_percent", "cost_growth", "lift_oubli_cost", "blessing_reroll_cost");
            (float shardMin, float shardMax) = reader.Range(m, "shard_distance_px", 0f);
            MemorialConfig memorial = new()
            {
                SpriteDormant = reader.Resource(m, "sprite_dormant", exists),
                SpriteAwake = reader.Resource(m, "sprite_awake", exists),
                SpriteShard = reader.Resource(m, "sprite_shard", exists),
                Placement = Bands(reader, m),
                MinSpacingPx = reader.Positive(m, "min_spacing_px"),
                HoldTime = reader.Positive(m, "hold_time"),
                Shards = reader.Count(m, "shards", 12),
                ShardDistanceMin = shardMin,
                ShardDistanceMax = shardMax,
                ShardTime = reader.Positive(m, "shard_time"),
                ShardUrgentTime = reader.Positive(m, "shard_urgent_time"),
                ShardPickupPx = reader.Positive(m, "shard_pickup_px"),
                CircleSeconds = reader.Positive(mc, "seconds"),
                CircleRadiusPx = reader.Positive(mc, "radius_px"),
                CircleLeaveDecay = reader.NonNegative(mc, "leave_decay"),
                CircleAbandonPx = reader.Positive(mc, "abandon_px"),
                CircleCrowdDensity = reader.Positive(mc, "crowd_density"),
                CircleWaveAt = WaveShares(reader, waves),
                CircleWaveCount = reader.Count(waves, "count", 40),
                CircleWaveWarning = reader.Positive(waves, "warning_seconds"),
                CircleWaveAudio = reader.Text(waves, "audio"),
                CircleCloseFrom = reader.Ratio(closing, "from"),
                CircleCloseShare = reader.Ratio(closing, "radius_share"),
                CircleBurstRadiusPx = reader.Positive(burst, "radius_px"),
                CircleBurstDamageShare = reader.Chance(burst, "damage_share"),
                CircleBurstPushPx = reader.NonNegative(burst, "push_px"),
                CircleBurstAudio = reader.Text(burst, "audio"),
                CircleLeaveAudio = reader.Text(mc, "leave_audio"),
                BlessingChoices = reader.Count(m, "blessing_choices", 6),
                BlessingMinRarity = reader.OneOf(m, "blessing_min_rarity", rarityIds),
                HealCost = reader.Integer(ms, "heal_cost", 0, 100_000),
                HealPercent = reader.Ratio(ms, "heal_percent"),
                CostGrowth = reader.NonNegative(ms, "cost_growth"),
                LiftOubliCost = reader.Integer(ms, "lift_oubli_cost", 0, 100_000),
                BlessingRerollCost = reader.Integer(ms, "blessing_reroll_cost", 0, 100_000),
            };
            if (reader.Error == null && memorial.CircleAbandonPx <= memorial.CircleRadiusPx)
                reader.Fail("memorial.circle.abandon_px : doit dépasser radius_px");
            if (memorial.CircleCloseFrom >= 1f)
                reader.Fail("memorial.circle.closing.from : doit rester sous 1 (le cercle se resserre avant la fin)");
            if (memorial.ShardUrgentTime >= memorial.ShardTime)
                reader.Fail("memorial.shard_urgent_time : doit rester sous shard_time");

            JsonElement r = reader.Section("rift");
            reader.AllowOnly(r, "rift", "sprite_open", "sprite_closed", "placement", "min_spacing_px", "hold_time", "offers",
                "offer_min_rarity", "peril_per_offer", "erased_spawn");
            JsonElement spawn = reader.Section(r, "erased_spawn");
            reader.AllowOnly(spawn, "rift.erased_spawn", "max_open", "chance", "cooldown_s", "distance_px");
            (float spawnMin, float spawnMax) = reader.Range(spawn, "distance_px", 0f);
            RiftConfig rift = new()
            {
                SpriteOpen = reader.Resource(r, "sprite_open", exists),
                SpriteClosed = reader.Resource(r, "sprite_closed", exists),
                Placement = Bands(reader, r),
                MinSpacingPx = reader.Positive(r, "min_spacing_px"),
                HoldTime = reader.Positive(r, "hold_time"),
                Offers = reader.Count(r, "offers", 6),
                OfferMinRarity = reader.OneOf(r, "offer_min_rarity", rarityIds),
                PerilPerOffer = reader.Integer(r, "peril_per_offer", 0, 100),
                MaxOpen = reader.Integer(spawn, "max_open", 0, 100),
                SpawnChance = reader.Chance(spawn, "chance"),
                SpawnCooldown = reader.NonNegative(spawn, "cooldown_s"),
                SpawnDistanceMin = spawnMin,
                SpawnDistanceMax = spawnMax,
            };

            JsonElement w = reader.Section("workshop");
            reader.AllowOnly(w, "workshop", "enabled", "sprite", "placement", "min_spacing_px", "services");
            JsonElement ws = reader.Section(w, "services");
            reader.AllowOnly(ws, "workshop.services", "weapon_cost", "weapon_min_rarity", "retemper_cost", "item_cost", "temper_upgrades",
                "cost_growth");
            WorkshopConfig workshop = new()
            {
                Enabled = reader.Flag(w, "enabled"),
                Sprite = reader.Resource(w, "sprite", exists),
                Placement = Bands(reader, w),
                MinSpacingPx = reader.Positive(w, "min_spacing_px"),
                WeaponCost = reader.Integer(ws, "weapon_cost", 0, 100_000),
                WeaponMinRarity = reader.OneOf(ws, "weapon_min_rarity", rarityIds),
                RetemperCost = reader.Integer(ws, "retemper_cost", 0, 100_000),
                ItemCost = reader.Integer(ws, "item_cost", 0, 100_000),
                TemperUpgrades = reader.Integer(ws, "temper_upgrades", 0, 100),
                CostGrowth = reader.NonNegative(ws, "cost_growth"),
            };
            JsonElement wl = reader.Section("well");
            reader.AllowOnly(wl, "well", "enabled", "sprite", "placement", "min_spacing_px", "forget");
            JsonElement forget = reader.Section(wl, "forget");
            reader.AllowOnly(forget, "well.forget", "cost", "cost_growth", "trace_ratio");
            WellConfig well = new()
            {
                Enabled = reader.Flag(wl, "enabled"),
                Sprite = reader.Resource(wl, "sprite", exists),
                Placement = Bands(reader, wl),
                MinSpacingPx = reader.Positive(wl, "min_spacing_px"),
                ForgetCost = reader.Integer(forget, "cost", 0, 100_000),
                ForgetCostGrowth = reader.NonNegative(forget, "cost_growth"),
                ForgetTraceRatio = reader.Ratio(forget, "trace_ratio"),
            };
            JsonElement st = reader.Section("peril_stele");
            reader.AllowOnly(st, "peril_stele", "enabled", "sprite_lit", "sprite_spent", "placement", "min_spacing_px", "hold_time", "peril");
            PerilSteleConfig perilStele = new()
            {
                Enabled = reader.Flag(st, "enabled"),
                SpriteLit = reader.Resource(st, "sprite_lit", exists),
                SpriteSpent = reader.Resource(st, "sprite_spent", exists),
                Placement = Bands(reader, st),
                MinSpacingPx = reader.Positive(st, "min_spacing_px"),
                HoldTime = reader.Positive(st, "hold_time"),
                Peril = reader.Integer(st, "peril", 1, 100),
            };
            if (reader.Error != null)
                return reader.Error;
            _memorial = memorial;
            _rift = rift;
            _workshop = workshop;
            _well = well;
            _perilStele = perilStele;
            _loadError = null;
            return null;
        }
        catch (JsonException ex)
        {
            return $"JSON illisible : {ex.Message}";
        }
    }

    /// <summary>Parts tenues où montent les vagues du cercle : dans ]0 ; 1[, croissantes.</summary>
    private static float[] WaveShares(JsonConfigReader reader, JsonElement waves)
    {
        List<JsonElement> list = reader.List(waves, "at", 0);
        float[] shares = new float[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            shares[i] = list[i].ValueKind == JsonValueKind.Number ? list[i].GetSingle() : float.NaN;
            if (!(shares[i] > 0f && shares[i] < 1f) || (i > 0 && shares[i] <= shares[i - 1]))
                reader.Fail($"memorial.circle.waves.at : parts croissantes dans ]0 ; 1[ attendues (« {list[i]} »)");
        }
        return shares;
    }

    /// <summary>Groupes de placement : combien, et dans quelle couronne (fraction du rayon de carte, de 0 à 1).</summary>
    private static List<LandmarkBand> Bands(JsonConfigReader reader, JsonElement owner)
    {
        List<LandmarkBand> bands = new();
        foreach (JsonElement group in reader.List(owner, "placement", 1))
        {
            reader.AllowOnly(group, "placement", "count", "band");
            int count = reader.Integer(group, "count", 0, 1000);
            (float min, float max) = reader.Range(group, "band", 0f, 1f);
            bands.Add(new LandmarkBand(count, min, max));
        }
        return bands;
    }
}
