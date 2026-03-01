using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class PassiveSouvenirData
{
	public string Id;
	public string Name;
	public string Description;
	public Color IconColor;
	public int MaxLevel;
	public string Stat;
	public string ModifierType;
	public float[] PerLevel;
	public string FusionWith;
	public string FusionResult;
}

public static class PassiveSouvenirDataLoader
{
	private static readonly Dictionary<string, PassiveSouvenirData> _cache = new();
	private static readonly List<PassiveSouvenirData> _all = new();
	private static bool _loaded;

	public static void Load()
	{
		if (_loaded)
			return;

		FileAccess file = FileAccess.Open("res://data/progression/passive_souvenirs.json", FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PushError("[PassiveSouvenirDataLoader] Cannot open passive_souvenirs.json");
			_loaded = true;
			return;
		}

		string jsonText = file.GetAsText();
		file.Close();

		Json json = new();
		if (json.Parse(jsonText) != Error.Ok)
		{
			GD.PushError($"[PassiveSouvenirDataLoader] Parse error: {json.GetErrorMessage()}");
			_loaded = true;
			return;
		}

		Godot.Collections.Array array = json.Data.AsGodotArray();
		foreach (Variant item in array)
		{
			Godot.Collections.Dictionary dict = item.AsGodotDictionary();
			PassiveSouvenirData data = ParseEntry(dict);
			if (data != null)
			{
				_cache[data.Id] = data;
				_all.Add(data);
			}
		}

		_loaded = true;
		GD.Print($"[PassiveSouvenirDataLoader] Loaded {_cache.Count} passive souvenirs");
	}

	private static PassiveSouvenirData ParseEntry(Godot.Collections.Dictionary dict)
	{
		if (!dict.ContainsKey("id"))
			return null;

		PassiveSouvenirData data = new()
		{
			Id = dict["id"].AsString(),
			Name = dict.ContainsKey("name") ? dict["name"].AsString() : dict["id"].AsString(),
			Description = dict.ContainsKey("description") ? dict["description"].AsString() : "",
			MaxLevel = dict.ContainsKey("max_level") ? (int)dict["max_level"].AsDouble() : 5,
			Stat = dict.ContainsKey("stat") ? dict["stat"].AsString() : "",
			ModifierType = dict.ContainsKey("modifier_type") ? dict["modifier_type"].AsString() : "multiplicative",
			FusionWith = dict.ContainsKey("fusion_with") ? dict["fusion_with"].AsString() : "",
			FusionResult = dict.ContainsKey("fusion_result") ? dict["fusion_result"].AsString() : ""
		};

		if (dict.ContainsKey("icon_color"))
		{
			Godot.Collections.Array colorArr = dict["icon_color"].AsGodotArray();
			data.IconColor = new Color(
				(float)colorArr[0].AsDouble(),
				(float)colorArr[1].AsDouble(),
				(float)colorArr[2].AsDouble()
			);
		}

		if (dict.ContainsKey("per_level"))
		{
			Godot.Collections.Array lvlArr = dict["per_level"].AsGodotArray();
			data.PerLevel = new float[lvlArr.Count];
			for (int i = 0; i < lvlArr.Count; i++)
				data.PerLevel[i] = (float)lvlArr[i].AsDouble();
		}

		return data;
	}

	public static PassiveSouvenirData Get(string id)
	{
		if (!_loaded) Load();
		return _cache.GetValueOrDefault(id);
	}

	public static List<PassiveSouvenirData> GetAll()
	{
		if (!_loaded) Load();
		return new List<PassiveSouvenirData>(_all);
	}
}
