using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class WeaponUpgradeStatConfig
{
	public string Key { get; set; }
	public int MaxLevel { get; set; }
	public float PerLevel { get; set; }
	public string Mode { get; set; }
	public string DisplayName { get; set; }
	public List<string> Types { get; set; }
}

public static class WeaponUpgradeDataLoader
{
	private static readonly Dictionary<string, WeaponUpgradeStatConfig> _statConfigs = new();
	private static readonly List<string> _statOrder = new();
	private static int _weaponMaxLevel = 5;
	private static bool _loaded;

	public static void Load()
	{
		if (_loaded)
			return;

		FileAccess file = FileAccess.Open("res://data/weapons/weapon_upgrades.json", FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PushError("[WeaponUpgradeDataLoader] Cannot open weapon_upgrades.json");
			return;
		}

		string jsonText = file.GetAsText();
		file.Close();

		Json json = new();
		if (json.Parse(jsonText) != Error.Ok)
		{
			GD.PushError($"[WeaponUpgradeDataLoader] Parse error: {json.GetErrorMessage()}");
			return;
		}

		Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();

		if (root.ContainsKey("weapon_max_level"))
			_weaponMaxLevel = (int)root["weapon_max_level"].AsDouble();

		ParseStats(root);

		_loaded = true;
		GD.Print($"[WeaponUpgradeDataLoader] Loaded {_statConfigs.Count} upgradeable stats, weapon max level = {_weaponMaxLevel}");
	}

	private static void ParseStats(Godot.Collections.Dictionary root)
	{
		if (!root.ContainsKey("stats"))
			return;

		Godot.Collections.Dictionary statsDict = root["stats"].AsGodotDictionary();
		foreach (Variant key in statsDict.Keys)
		{
			string statKey = key.AsString();
			Godot.Collections.Dictionary statData = statsDict[key].AsGodotDictionary();

			WeaponUpgradeStatConfig config = new()
			{
				Key = statKey,
				MaxLevel = statData.ContainsKey("max_level") ? (int)statData["max_level"].AsDouble() : 3,
				PerLevel = statData.ContainsKey("per_level") ? (float)statData["per_level"].AsDouble() : 0.1f,
				Mode = statData.ContainsKey("mode") ? statData["mode"].AsString() : "multiplicative",
				DisplayName = statData.ContainsKey("display_name") ? statData["display_name"].AsString() : statKey
			};

			if (statData.ContainsKey("types"))
			{
				config.Types = new List<string>();
				Godot.Collections.Array typesArray = statData["types"].AsGodotArray();
				foreach (Variant t in typesArray)
					config.Types.Add(t.AsString());
			}

			_statConfigs[statKey] = config;
			_statOrder.Add(statKey);
		}
	}

	public static WeaponUpgradeStatConfig GetStatConfig(string stat)
	{
		if (!_loaded)
			Load();

		return _statConfigs.GetValueOrDefault(stat);
	}

	public static List<string> GetAllUpgradeableStats()
	{
		if (!_loaded)
			Load();

		return _statOrder;
	}

	public static int GetWeaponMaxLevel()
	{
		if (!_loaded)
			Load();

		return _weaponMaxLevel;
	}
}
