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

/// <summary>Recette de craft d'une arme (ingrédients nécessaires).</summary>
public class WeaponCraftRecipe
{
	public List<RecipeIngredient> Ingredients { get; set; } = new();
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
	public string DefaultFor { get; set; }
	public string Source { get; set; }
	public string RequiresSouvenir { get; set; }
	public WeaponCraftRecipe CraftRecipe { get; set; }
	public Dictionary<string, float> Stats { get; set; } = new();
	public WeaponOnHitEffect OnHitEffect { get; set; }
	public WeaponSpecialEffect SpecialEffect { get; set; }
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

    public static List<WeaponData> GetCraftableWeapons()
    {
        if (!_loaded)
            Load();

        List<WeaponData> result = new();
        foreach (WeaponData w in _allWeapons)
        {
            if (w.CraftRecipe != null && w.CraftRecipe.Ingredients.Count > 0)
                result.Add(w);
        }
        return result;
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
            AttackPattern = dict.ContainsKey("attack_pattern") ? dict["attack_pattern"].AsString() : "linear",
            DefaultFor = dict.ContainsKey("default_for") ? dict["default_for"].AsString() : null,
            Source = dict.ContainsKey("source") ? dict["source"].AsString() : null,
            RequiresSouvenir = dict.ContainsKey("requires_souvenir") ? dict["requires_souvenir"].AsString() : null
        };

        if (dict.ContainsKey("craft_recipe") && dict["craft_recipe"].VariantType == Variant.Type.Dictionary)
        {
            Godot.Collections.Dictionary recipeDict = dict["craft_recipe"].AsGodotDictionary();
            if (recipeDict.ContainsKey("ingredients"))
            {
                WeaponCraftRecipe recipe = new();
                Godot.Collections.Array ingredients = recipeDict["ingredients"].AsGodotArray();
                foreach (Variant ingEntry in ingredients)
                {
                    Godot.Collections.Dictionary ingDict = ingEntry.AsGodotDictionary();
                    recipe.Ingredients.Add(new RecipeIngredient
                    {
                        Resource = ingDict["resource"].AsString(),
                        Amount = (int)ingDict["amount"].AsDouble()
                    });
                }
                if (recipe.Ingredients.Count > 0)
                    weapon.CraftRecipe = recipe;
            }
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

        if (dict.ContainsKey("on_hit_effect"))
        {
            Godot.Collections.Dictionary ohe = dict["on_hit_effect"].AsGodotDictionary();
            weapon.OnHitEffect = new WeaponOnHitEffect
            {
                Type = ohe.ContainsKey("type") ? ohe["type"].AsString() : "",
                Value = ohe.ContainsKey("value") ? (float)ohe["value"].AsDouble() : 0f,
                Damage = ohe.ContainsKey("damage") ? (float)ohe["damage"].AsDouble() : 0f,
                Duration = ohe.ContainsKey("duration") ? (float)ohe["duration"].AsDouble() : 0f
            };
        }

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
