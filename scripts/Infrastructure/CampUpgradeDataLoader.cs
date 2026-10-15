using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Vestiges.Core;

namespace Vestiges.Infrastructure;

/// <summary>
/// Catalogue du Campement (data/progression/camp_upgrades.json, plan 36), contrôlé en entier au chargement : un fichier
/// invalide est refusé avec la raison, et rien n'est publié. Une statistique doit être lue par le joueur avec ce
/// modificateur (<see cref="PlayerModifiers"/>).
/// </summary>
public static class CampUpgradeDataLoader
{
    private const string ConfigPath = "res://data/progression/camp_upgrades.json";
    private const int MaxRanks = 20;
    private const int MaxPrice = 1_000_000;

    private static readonly List<CampUpgradeData> _upgrades = new();
    private static readonly Dictionary<string, CampUpgradeData> _byId = new();
    private static int _baseWeaponSlots = Player.MaxWeaponSlots;
    private static bool _loaded;
    private static string _loadError;

    private static readonly Dictionary<string, CampBranch> Branches = new()
    {
        ["capacities"] = CampBranch.Capacities,
        ["stats"] = CampBranch.Stats,
        ["comfort"] = CampBranch.Comfort,
    };

    private static readonly Dictionary<string, CampEffect> Effects = new()
    {
        ["weapon_slots"] = CampEffect.WeaponSlots,
        ["bag_slots"] = CampEffect.BagSlots,
        ["rerolls"] = CampEffect.Rerolls,
        ["banishes"] = CampEffect.Banishes,
        ["start_essence"] = CampEffect.StartEssence,
        ["vestige_gain"] = CampEffect.VestigeGain,
        ["stat"] = CampEffect.Stat,
    };

    /// <summary>
    /// Couples statistique / modificateur que <see cref="Player.ApplyPerkModifier"/> applique. Le contrat des objets ne
    /// convient pas : il n'admet que l'additif pour les PV, alors que le Campement les donne en pourcentage.
    /// </summary>
    private static readonly HashSet<(string Stat, string Modifier)> PlayerModifiers = new()
    {
        ("max_hp", "additive"), ("max_hp", "multiplicative"), ("damage", "multiplicative"), ("speed", "multiplicative"),
        ("attack_speed", "multiplicative"), ("aoe_radius", "multiplicative"), ("attack_range", "multiplicative"),
        ("regen_rate", "additive"), ("lifesteal", "additive"), ("armor", "additive"), ("crit_chance", "additive"),
        ("crit_multiplier", "additive"), ("projectile_pierce", "additive"), ("xp_magnet_radius", "multiplicative"),
        ("luck", "additive"), ("xp_gain", "multiplicative"), ("dash_recharge", "multiplicative"),
        ("projectile_bonus", "additive"), ("status_duration", "multiplicative"),
    };

    public static IReadOnlyList<CampUpgradeData> All
    {
        get
        {
            Load();
            return _upgrades;
        }
    }

    /// <summary>Emplacements d'armes d'un profil qui n'a rien acheté.</summary>
    public static int BaseWeaponSlots
    {
        get
        {
            Load();
            return _baseWeaponSlots;
        }
    }

    public static CampUpgradeData Get(string id)
    {
        Load();
        return id != null && _byId.TryGetValue(id, out CampUpgradeData upgrade) ? upgrade : null;
    }

    /// <summary>Catalogue lu et contrôlé ; faux, avec la raison, s'il a été refusé (le chargement de la run s'arrête).</summary>
    public static bool TryLoad(out string error)
    {
        Load();
        error = _loadError;
        return error == null;
    }

    /// <summary>Contrôle un texte du catalogue et ne le publie que s'il est entièrement valide. Rend l'erreur, ou null.</summary>
    public static string Apply(string json)
    {
        Func<string, bool> translated = key => TranslationServer.Translate(key) != key;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            reader.AllowOnly(reader.Root, "Campement", "base_weapon_slots", "upgrades");
            int baseSlots = reader.Integer(reader.Root, "base_weapon_slots", 1, Player.MaxWeaponSlots);
            List<CampUpgradeData> upgrades = new();
            HashSet<string> ids = new();
            int weaponSlotUpgrades = 0;
            foreach (JsonElement entry in reader.List(reader.Root, "upgrades", 1))
            {
                if (reader.Error != null)
                    break;
                JsonConfigReader item = new(entry);
                string id = item.Text(entry, "id");
                CampUpgradeData upgrade = ReadUpgrade(item, entry, translated);
                if (item.Error == null && !ids.Add(id))
                    item.Fail("identifiant en double");
                if (item.Error == null && upgrade.Effect == CampEffect.WeaponSlots)
                {
                    weaponSlotUpgrades++;
                    if (baseSlots + upgrade.TotalAt(upgrade.MaxRank) > Player.MaxWeaponSlots)
                        item.Fail($"emplacements d'armes : {baseSlots} + {upgrade.TotalAt(upgrade.MaxRank)} dépasse {Player.MaxWeaponSlots}");
                }
                if (item.Error == null && weaponSlotUpgrades > 1)
                    item.Fail("une seule amélioration d'emplacements d'armes admise");
                if (item.Error != null)
                    return $"amélioration {id ?? "?"} : {item.Error}";
                upgrades.Add(upgrade);
            }
            if (reader.Error != null)
                return reader.Error;

            _baseWeaponSlots = baseSlots;
            _upgrades.Clear();
            _upgrades.AddRange(upgrades);
            _byId.Clear();
            foreach (CampUpgradeData upgrade in upgrades)
                _byId[upgrade.Id] = upgrade;
            return null;
        }
        catch (JsonException ex)
        {
            return $"JSON illisible : {ex.Message}";
        }
    }

    private static CampUpgradeData ReadUpgrade(JsonConfigReader item, JsonElement entry, Func<string, bool> translated)
    {
        item.AllowOnly(entry, "clés", "id", "branch", "effect", "stat", "modifier_type", "step", "percent", "prices", "name_key",
            "desc_key");
        CampUpgradeData upgrade = new()
        {
            Id = item.Text(entry, "id"),
            Branch = Branches.GetValueOrDefault(item.OneOf(entry, "branch", Branches.Keys) ?? "capacities"),
            Effect = Effects.GetValueOrDefault(item.OneOf(entry, "effect", Effects.Keys) ?? "stat"),
            Percent = item.Flag(entry, "percent"),
            NameKey = item.Text(entry, "name_key"),
            DescKey = item.Text(entry, "desc_key"),
        };
        if (upgrade.Effect == CampEffect.Stat)
        {
            upgrade.Stat = item.Text(entry, "stat");
            upgrade.ModifierType = item.Text(entry, "modifier_type");
            if (item.Error == null && !PlayerModifiers.Contains((upgrade.Stat, upgrade.ModifierType)))
                item.Fail($"stat : « {upgrade.Stat} » en {upgrade.ModifierType} n'est pas lu par le joueur");
            upgrade.Step = item.Positive(entry, "step");
        }
        else
        {
            if (item.Error == null && (entry.TryGetProperty("stat", out _) || entry.TryGetProperty("modifier_type", out _)))
                item.Fail("stat et modifier_type ne valent que pour l'effet stat");
            // Emplacements, places du sac, relances, bannissements et Essence se comptent en unités entières.
            upgrade.Step = upgrade.Effect == CampEffect.VestigeGain
                ? item.Positive(entry, "step")
                : item.Integer(entry, "step", 1, 1000);
        }

        List<JsonElement> prices = item.List(entry, "prices", 1);
        if (item.Error == null && prices.Count > MaxRanks)
            item.Fail($"prices : {prices.Count} rangs, {MaxRanks} au plus");
        upgrade.Prices = new int[prices.Count];
        for (int i = 0; i < prices.Count && item.Error == null; i++)
        {
            if (prices[i].ValueKind != JsonValueKind.Number || !prices[i].TryGetInt32(out int price) || price < 1 || price > MaxPrice)
                item.Fail($"prices[{i}] : entier de 1 à {MaxPrice} attendu");
            else if (i > 0 && price < upgrade.Prices[i - 1])
                item.Fail($"prices[{i}] : {price} inférieur au rang précédent ({upgrade.Prices[i - 1]})");
            else
                upgrade.Prices[i] = price;
        }
        if (item.Error == null && !translated(upgrade.NameKey))
            item.Fail($"name_key : clé « {upgrade.NameKey} » absente des traductions");
        if (item.Error == null && !translated(upgrade.DescKey))
            item.Fail($"desc_key : clé « {upgrade.DescKey} » absente des traductions");
        return upgrade;
    }

    private static void Load()
    {
        if (_loaded)
            return;
        _loaded = true;
        string error = FileAccess.FileExists(ConfigPath) ? Apply(FileAccess.GetFileAsString(ConfigPath)) : "absent";
        if (error != null)
        {
            _loadError = $"{ConfigPath} : {error}";
            GD.PushError($"[CampUpgradeDataLoader] {_loadError}");
        }
    }
}
