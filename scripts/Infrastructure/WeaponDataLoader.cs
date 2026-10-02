using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Effet on-hit d'une arme (saignement, slow, désorientation).</summary>
public class WeaponOnHitEffect
{
	public string Type { get; set; }
	public float Value { get; set; }
	public float Damage { get; set; }
	public float Duration { get; set; }
}

/// <summary>Effet spécial d'une arme (heal every N hits, delayed echo, ground fire, etc.).</summary>
public class WeaponSpecialEffect
{
	public string Type { get; set; }
	public Dictionary<string, float> Params { get; set; } = new();
	public List<string> Shapes { get; set; }
}

/// <summary>
/// Présentation d'une arme en combat (plan 08, effets d'attaque) : style du coup de mêlée,
/// famille de couleurs (sinon déduite du type de dégâts) et forme du projectile.
/// </summary>
public class WeaponFxData
{
	public string Style { get; set; }
	public string Family { get; set; }
	public string Projectile { get; set; }
}

public class WeaponData
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public int Tier { get; set; }
	public string Type { get; set; }
	public string DamageType { get; set; }
	public string AttackPattern { get; set; }
	/// <summary>Poids d'un impact pour les objets à déclencheur (plan 21 §7) : moins pour une arme rapide ou multiple.</summary>
	public float TriggerCoefficient { get; set; } = 1f;
	public string AttackAudio { get; set; }
	public string DefaultFor { get; set; }
	public string Sprite { get; set; }
	/// <summary>Version 16×16 portée en main (plan 17 lot 2C, option désactivée par défaut).</summary>
	public string HeldSprite { get; set; }
	public string Source { get; set; }
	public string RequiresSouvenir { get; set; }
	/// <summary>Ce que fait l'arme, en une phrase, sans chiffre (carte « Nouvelle » du level-up).</summary>
	public string Summary { get; set; }
	/// <summary>Une ligne sur l'ancien propriétaire de l'objet (pause, Collection).</summary>
	public string LoreFlavor { get; set; }
	/// <summary>Libellé du nombre de l'arme quand ce ne sont pas des projectiles : frappes, ondes (plan 21 G6a).</summary>
	public string CountNameKey { get; set; }
	public Dictionary<string, float> Stats { get; set; } = new();
	/// <summary>Stats qui peuvent monter à chaque amélioration, avec leur poids dans le tirage.</summary>
	public Dictionary<string, float> Growth { get; set; } = new();
	/// <summary>Deux voies d'ascension au niveau maximal (plan 21 §3), ou aucune.</summary>
	public List<WeaponAscensionData> Ascensions { get; set; } = new();
	public WeaponOnHitEffect OnHitEffect { get; set; }
	public WeaponSpecialEffect SpecialEffect { get; set; }
	public WeaponFxData Fx { get; set; } = new();
}

public static class WeaponDataLoader
{
    private static readonly List<WeaponData> _allWeapons = new();
    private static readonly Dictionary<string, WeaponData> _byId = new();
    private static readonly Dictionary<string, WeaponData> _defaultByCharacter = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        FileAccess file = FileAccess.Open("res://data/weapons/weapons.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[WeaponDataLoader] Cannot open weapons.json");
            return;
        }

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        if (json.Parse(jsonText) != Error.Ok)
        {
            GD.PushError($"[WeaponDataLoader] Parse error: {json.GetErrorMessage()}");
            return;
        }

        Godot.Collections.Array array = json.Data.AsGodotArray();
        foreach (Variant item in array)
        {
            if (item.VariantType != Variant.Type.Dictionary)
                continue;

            Godot.Collections.Dictionary dict = item.AsGodotDictionary();
            if (!dict.ContainsKey("id"))
                continue;

            WeaponData weapon = ParseWeapon(dict);
            if (weapon == null || string.IsNullOrEmpty(weapon.Id))
                continue;

            _allWeapons.Add(weapon);
            _byId[weapon.Id] = weapon;

            if (!string.IsNullOrEmpty(weapon.DefaultFor))
                _defaultByCharacter[weapon.DefaultFor] = weapon;
        }

        _loaded = true;
        GD.Print($"[WeaponDataLoader] Loaded {_allWeapons.Count} weapons");
    }

    public static WeaponData Get(string id)
    {
        if (!_loaded)
            Load();

        if (string.IsNullOrEmpty(id))
            return null;

        return _byId.GetValueOrDefault(id);
    }

    public static WeaponData GetDefaultForCharacter(string characterId)
    {
        if (!_loaded)
            Load();

        if (string.IsNullOrEmpty(characterId))
            return null;

        return _defaultByCharacter.GetValueOrDefault(characterId);
    }

    public static List<WeaponData> GetAll()
    {
        if (!_loaded)
            Load();

        return _allWeapons;
    }

    private static WeaponOnHitEffect ParseOnHit(Godot.Collections.Dictionary ohe) => new()
    {
        Type = ohe.ContainsKey("type") ? ohe["type"].AsString() : "",
        Value = ohe.ContainsKey("value") ? (float)ohe["value"].AsDouble() : 0f,
        Damage = ohe.ContainsKey("damage") ? (float)ohe["damage"].AsDouble() : 0f,
        Duration = ohe.ContainsKey("duration") ? (float)ohe["duration"].AsDouble() : 0f
    };

    private static Dictionary<string, float> ParseFloats(Godot.Collections.Dictionary dict, string key)
    {
        Dictionary<string, float> values = new();
        if (!dict.ContainsKey(key))
            return values;
        Godot.Collections.Dictionary entries = dict[key].AsGodotDictionary();
        foreach (Variant name in entries.Keys)
            values[name.AsString()] = (float)entries[name].AsDouble();
        return values;
    }

    private static WeaponAscensionData ParseAscension(string weaponId, Godot.Collections.Dictionary dict)
    {
        HashSet<string> flags = new();
        if (dict.ContainsKey("flags"))
            foreach (Variant flag in dict["flags"].AsGodotArray())
                flags.Add(flag.AsString());
        return new WeaponAscensionData
        {
            Id = dict.ContainsKey("id") ? dict["id"].AsString() : weaponId,
            Name = dict.ContainsKey("name") ? dict["name"].AsString() : weaponId,
            Description = dict.ContainsKey("description") ? dict["description"].AsString() : "",
            AttackPattern = dict.ContainsKey("attack_pattern") ? dict["attack_pattern"].AsString() : null,
            StatMultipliers = ParseFloats(dict, "stat_multipliers"),
            StatOverrides = ParseFloats(dict, "stat_overrides"),
            OnHitEffect = dict.ContainsKey("on_hit_effect") ? ParseOnHit(dict["on_hit_effect"].AsGodotDictionary()) : null,
            BonusProjectileMultiplier = dict.ContainsKey("bonus_projectile_multiplier") ? (float)dict["bonus_projectile_multiplier"].AsDouble() : 1f,
            SpecialOverrides = ParseFloats(dict, "special_overrides"),
            Flags = flags,
            Parameters = ParseFloats(dict, "params"),
        };
    }

    private static WeaponData ParseWeapon(Godot.Collections.Dictionary dict)
    {
        WeaponData weapon = new()
        {
            Id = dict["id"].AsString(),
            Name = dict.ContainsKey("name") ? dict["name"].AsString() : "",
            Description = dict.ContainsKey("description") ? dict["description"].AsString() : "",
            Tier = dict.ContainsKey("tier") ? (int)dict["tier"].AsDouble() : 1,
            Type = dict.ContainsKey("type") ? dict["type"].AsString() : "ranged",
            DamageType = dict.ContainsKey("damage_type") ? dict["damage_type"].AsString() : "physical",
            AttackAudio = dict.ContainsKey("attack_audio") ? dict["attack_audio"].AsString() : null,
            AttackPattern = dict.ContainsKey("attack_pattern") ? dict["attack_pattern"].AsString() : "linear",
            TriggerCoefficient = dict.ContainsKey("trigger_coefficient") ? (float)dict["trigger_coefficient"].AsDouble() : 1f,
            DefaultFor = dict.ContainsKey("default_for") ? dict["default_for"].AsString() : null,
            Sprite = dict.ContainsKey("sprite") ? dict["sprite"].AsString() : null,
            HeldSprite = dict.ContainsKey("held_sprite") ? dict["held_sprite"].AsString() : null,
            Source = dict.ContainsKey("source") ? dict["source"].AsString() : null,
            RequiresSouvenir = dict.ContainsKey("requires_souvenir") ? dict["requires_souvenir"].AsString() : null,
            Summary = dict.ContainsKey("summary") ? dict["summary"].AsString() : "",
            LoreFlavor = dict.ContainsKey("lore_flavor") ? dict["lore_flavor"].AsString() : "",
            CountNameKey = dict.ContainsKey("count_name_key") ? dict["count_name_key"].AsString() : null
        };

        if (dict.ContainsKey("growth"))
        {
            foreach ((Variant key, Variant value) in dict["growth"].AsGodotDictionary())
                weapon.Growth[key.AsString()] = (float)value.AsDouble();
        }
        else
        {
            weapon.Growth["damage"] = 3f;
            weapon.Growth["attack_speed"] = 2f;
        }

        if (dict.ContainsKey("stats"))
        {
            Godot.Collections.Dictionary statsDict = dict["stats"].AsGodotDictionary();
            foreach (Variant key in statsDict.Keys)
            {
                string statKey = key.AsString();
                Variant value = statsDict[key];
                if (value.VariantType is Variant.Type.Int or Variant.Type.Float)
                    weapon.Stats[statKey] = (float)value.AsDouble();
            }
        }

        if (dict.ContainsKey("fx"))
        {
            Godot.Collections.Dictionary fx = dict["fx"].AsGodotDictionary();
            weapon.Fx = new WeaponFxData
            {
                Style = fx.ContainsKey("style") ? fx["style"].AsString() : null,
                Family = fx.ContainsKey("family") ? fx["family"].AsString() : null,
                Projectile = fx.ContainsKey("projectile") ? fx["projectile"].AsString() : null
            };
        }

        if (dict.ContainsKey("on_hit_effect"))
            weapon.OnHitEffect = ParseOnHit(dict["on_hit_effect"].AsGodotDictionary());
        if (dict.ContainsKey("ascensions"))
            foreach (Variant entry in dict["ascensions"].AsGodotArray())
                weapon.Ascensions.Add(ParseAscension(weapon.Id, entry.AsGodotDictionary()));
        if (weapon.Ascensions.Count is not (0 or 2))
            GD.PushError($"[WeaponDataLoader] {weapon.Id} : {weapon.Ascensions.Count} voies d'ascension, il en faut deux");

        if (dict.ContainsKey("special_effect"))
        {
            Godot.Collections.Dictionary se = dict["special_effect"].AsGodotDictionary();
            WeaponSpecialEffect effect = new()
            {
                Type = se.ContainsKey("type") ? se["type"].AsString() : ""
            };
            foreach (Variant key in se.Keys)
            {
                string k = key.AsString();
                if (k == "type" || k == "description" || k == "visual")
                    continue;
                if (k == "shapes" && se[key].VariantType == Variant.Type.Array)
                {
                    effect.Shapes = new List<string>();
                    foreach (Variant shape in se[key].AsGodotArray())
                        effect.Shapes.Add(shape.AsString());
                    continue;
                }
                Variant val = se[key];
                if (val.VariantType is Variant.Type.Int or Variant.Type.Float)
                    effect.Params[k] = (float)val.AsDouble();
            }
            weapon.SpecialEffect = effect;
        }

        return weapon;
    }
}
