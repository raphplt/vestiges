using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class RecipeIngredient
{
    public string Resource { get; set; }
    public int Amount { get; set; }
}

public class RecipeResult
{
    public string Type { get; set; }
    public Dictionary<string, float> Stats { get; set; } = new();
}

public class RecipeData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public List<RecipeIngredient> Ingredients { get; set; } = new();
    public float BuildTime { get; set; }
    public RecipeResult Result { get; set; }
}

public static class RecipeDataLoader
{
    private static readonly Dictionary<string, RecipeData> _cache = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        FileAccess file = FileAccess.Open("res://data/recipes/recipes.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[RecipeDataLoader] Cannot open data/recipes/recipes.json");
            return;
        }

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        Error error = json.Parse(jsonText);
        if (error != Error.Ok)
        {
            GD.PushError($"[RecipeDataLoader] Parse error: {json.GetErrorMessage()}");
            return;
        }

        Godot.Collections.Array array = json.Data.AsGodotArray();
        foreach (Variant item in array)
        {
            Godot.Collections.Dictionary dict = item.AsGodotDictionary();
            RecipeData data = ParseRecipe(dict);
            if (data != null)
                _cache[data.Id] = data;
        }

        _loaded = true;
        GD.Print($"[RecipeDataLoader] Loaded {_cache.Count} recipes");
    }

    public static RecipeData Get(string id)
    {
        if (!_loaded)
            Load();

        if (_cache.TryGetValue(id, out RecipeData data))
            return data;

        GD.PushError($"[RecipeDataLoader] Unknown recipe id: {id}");
        return null;
    }

    public static List<RecipeData> GetAll()
    {
        if (!_loaded)
            Load();

        return new List<RecipeData>(_cache.Values);
    }

    private static RecipeData ParseRecipe(Godot.Collections.Dictionary dict)
    {
        RecipeData data = new()
        {
            Id = dict["id"].AsString(),
            Name = dict["name"].AsString(),
            Category = dict["category"].AsString(),
            BuildTime = (float)dict["build_time"].AsDouble()
        };

        Godot.Collections.Array ingredients = dict["ingredients"].AsGodotArray();
        foreach (Variant ing in ingredients)
        {
            Godot.Collections.Dictionary ingDict = ing.AsGodotDictionary();
            data.Ingredients.Add(new RecipeIngredient
            {
                Resource = ingDict["resource"].AsString(),
                Amount = (int)ingDict["amount"].AsDouble()
            });
        }

        Godot.Collections.Dictionary result = dict["result"].AsGodotDictionary();
        data.Result = new RecipeResult
        {
            Type = result["type"].AsString()
        };

        Godot.Collections.Dictionary stats = result["stats"].AsGodotDictionary();
        foreach (string key in stats.Keys)
        {
            data.Result.Stats[key] = (float)stats[key].AsDouble();
        }

        return data;
    }
}
