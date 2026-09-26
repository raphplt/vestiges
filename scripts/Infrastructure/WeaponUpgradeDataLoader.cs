using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Pas d'amélioration d'une stat d'arme (data/weapons/weapon_upgrades.json).</summary>
public class WeaponUpgradeStatConfig
{
	public string Key { get; set; }
	/// <summary>Gain d'une amélioration Commune : fraction de la base (multiplicative) ou valeur ajoutée (additive).</summary>
	public float Step { get; set; }
	public bool Additive { get; set; }
	/// <summary>Plafond de la valeur effective (angles), 0 si aucun.</summary>
	public float Max { get; set; }
}

public static class WeaponUpgradeDataLoader
{
	private static readonly Dictionary<string, WeaponUpgradeStatConfig> _statConfigs = new();
	private static int _weaponMaxLevel = 50;
	private static bool _loaded;

	public static void Load()
	{
		if (_loaded)
			return;

		using FileAccess file = FileAccess.Open("res://data/weapons/weapon_upgrades.json", FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PushError("[WeaponUpgradeDataLoader] Cannot open weapon_upgrades.json");
			return;
		}

		Json json = new();
		if (json.Parse(file.GetAsText()) != Error.Ok)
		{
			GD.PushError($"[WeaponUpgradeDataLoader] Parse error: {json.GetErrorMessage()}");
			return;
		}

		Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();
		if (root.ContainsKey("weapon_max_level"))
			_weaponMaxLevel = (int)root["weapon_max_level"].AsDouble();

		foreach ((Variant key, Variant value) in root["stats"].AsGodotDictionary())
		{
			Godot.Collections.Dictionary stat = value.AsGodotDictionary();
			_statConfigs[key.AsString()] = new WeaponUpgradeStatConfig
			{
				Key = key.AsString(),
				Step = (float)stat["step"].AsDouble(),
				Additive = stat.ContainsKey("mode") && stat["mode"].AsString() == "additive",
				Max = stat.ContainsKey("max") ? (float)stat["max"].AsDouble() : 0f,
			};
		}

		_loaded = true;
		GD.Print($"[WeaponUpgradeDataLoader] Loaded {_statConfigs.Count} upgradeable stats, weapon max level = {_weaponMaxLevel}");
	}

	public static WeaponUpgradeStatConfig GetStatConfig(string key)
	{
		if (!_loaded)
			Load();
		return _statConfigs.GetValueOrDefault(key);
	}

	public static int GetWeaponMaxLevel()
	{
		if (!_loaded)
			Load();
		return _weaponMaxLevel;
	}
}
