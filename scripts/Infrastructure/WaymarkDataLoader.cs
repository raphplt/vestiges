using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Repères (data/world/waymarks.json, plan 22 §3 B) : Chance gagnée au premier usage de chaque type de lieu.</summary>
public sealed class WaymarkConfig
{
	public bool Enabled { get; init; }
	public float LuckPerType { get; init; }
	/// <summary>Type de lieu → clé de traduction de son nom, dans l'ordre des données.</summary>
	public Dictionary<string, string> NameKeys { get; init; } = new();
}

public static class WaymarkDataLoader
{
	private static WaymarkConfig _config;

	public static WaymarkConfig Load()
	{
		if (_config != null)
			return _config;

		using FileAccess file = FileAccess.Open("res://data/world/waymarks.json", FileAccess.ModeFlags.Read);
		Json json = new();
		if (file == null || json.Parse(file.GetAsText()) != Error.Ok)
		{
			GD.PushError("[WaymarkDataLoader] Cannot read data/world/waymarks.json");
			_config = new WaymarkConfig();
			return _config;
		}

		Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();
		Dictionary<string, string> names = new();
		foreach (Variant item in root["types"].AsGodotArray())
		{
			Godot.Collections.Dictionary type = item.AsGodotDictionary();
			names[type["id"].AsString()] = type["name_key"].AsString();
		}
		_config = new WaymarkConfig
		{
			Enabled = root.ContainsKey("enabled") && root["enabled"].AsBool(),
			LuckPerType = (float)root["luck_per_type"].AsDouble(),
			NameKeys = names,
		};
		return _config;
	}
}
