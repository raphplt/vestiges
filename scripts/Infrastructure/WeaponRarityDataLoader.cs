using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class WeaponRarityData
{
	public string Id { get; set; }
	public string DisplayName { get; set; }
	public Color Color { get; set; } = Colors.White;
	public float Weight { get; set; } = 1f;
	public float GlobalMultiplier { get; set; } = 1f;
	public float DamageMultiplier { get; set; } = 1f;
	public float AttackSpeedMultiplier { get; set; } = 1f;
	public float RangeMultiplier { get; set; } = 1f;
	public float HealMultiplier { get; set; } = 1f;
}

public static class WeaponRarityDataLoader
{
	private static readonly Dictionary<string, WeaponRarityData> _byId = new();
	private static readonly List<WeaponRarityData> _ordered = new();
	private static bool _loaded;

	public static void Load()
	{
		if (_loaded)
			return;

		FileAccess file = FileAccess.Open("res://data/scaling/weapon_rarity.json", FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PushError("[WeaponRarityDataLoader] Cannot open weapon_rarity.json");
			return;
		}

		string jsonText = file.GetAsText();
		file.Close();

		Json json = new();
		if (json.Parse(jsonText) != Error.Ok)
		{
			GD.PushError($"[WeaponRarityDataLoader] Parse error: {json.GetErrorMessage()}");
			return;
		}

		Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();
		Godot.Collections.Array entries = root.ContainsKey("rarities")
			? root["rarities"].AsGodotArray()
			: new Godot.Collections.Array();

		foreach (Variant entry in entries)
		{
			Godot.Collections.Dictionary dict = entry.AsGodotDictionary();
			WeaponRarityData rarity = new()
			{
				Id = dict.ContainsKey("id") ? dict["id"].AsString() : "common",
				DisplayName = dict.ContainsKey("display_name") ? dict["display_name"].AsString() : "Commun",
				Color = Color.FromHtml(dict.ContainsKey("color") ? dict["color"].AsString() : "#FFFFFF"),
				Weight = dict.ContainsKey("weight") ? (float)dict["weight"].AsDouble() : 1f,
				GlobalMultiplier = dict.ContainsKey("global_multiplier") ? (float)dict["global_multiplier"].AsDouble() : 1f,
				DamageMultiplier = dict.ContainsKey("damage_multiplier") ? (float)dict["damage_multiplier"].AsDouble() : 1f,
				AttackSpeedMultiplier = dict.ContainsKey("attack_speed_multiplier") ? (float)dict["attack_speed_multiplier"].AsDouble() : 1f,
				RangeMultiplier = dict.ContainsKey("range_multiplier") ? (float)dict["range_multiplier"].AsDouble() : 1f,
				HealMultiplier = dict.ContainsKey("heal_multiplier") ? (float)dict["heal_multiplier"].AsDouble() : 1f
			};

			_byId[rarity.Id] = rarity;
			_ordered.Add(rarity);
		}

		_loaded = _ordered.Count > 0;
		GD.Print($"[WeaponRarityDataLoader] Loaded {_ordered.Count} rarity profiles");
	}

	public static WeaponRarityData Get(string rarityId)
	{
		if (!_loaded)
			Load();

		if (string.IsNullOrWhiteSpace(rarityId))
			rarityId = "common";

		return _byId.TryGetValue(rarityId, out WeaponRarityData rarity)
			? rarity
			: _byId.GetValueOrDefault("common");
	}

	public static string RollDropRarity(int weaponTier)
	{
		if (!_loaded)
			Load();

		if (_ordered.Count == 0)
			return "common";

		float totalWeight = 0f;
		foreach (WeaponRarityData rarity in _ordered)
			totalWeight += GetTierAdjustedWeight(rarity, weaponTier);

		if (totalWeight <= 0f)
			return "common";

		float roll = (float)GD.Randf() * totalWeight;
		float cumulative = 0f;
		foreach (WeaponRarityData rarity in _ordered)
		{
			cumulative += GetTierAdjustedWeight(rarity, weaponTier);
			if (roll <= cumulative)
				return rarity.Id;
		}

		return _ordered[_ordered.Count - 1].Id;
	}

	public static string RollReforgeRarity(string currentRarity, int weaponTier)
	{
		if (!_loaded)
			Load();

		WeaponRarityData current = Get(currentRarity);
		float totalWeight = 0f;
		foreach (WeaponRarityData rarity in _ordered)
		{
			float weight = GetTierAdjustedWeight(rarity, weaponTier);
			if (rarity.GlobalMultiplier < current.GlobalMultiplier)
				weight *= 0.35f;
			totalWeight += weight;
		}

		if (totalWeight <= 0f)
			return currentRarity ?? "common";

		float roll = (float)GD.Randf() * totalWeight;
		float cumulative = 0f;
		foreach (WeaponRarityData rarity in _ordered)
		{
			float weight = GetTierAdjustedWeight(rarity, weaponTier);
			if (rarity.GlobalMultiplier < current.GlobalMultiplier)
				weight *= 0.35f;

			cumulative += weight;
			if (roll <= cumulative)
				return rarity.Id;
		}

		return currentRarity ?? "common";
	}

	private static float GetTierAdjustedWeight(WeaponRarityData rarity, int weaponTier)
	{
		float tierBias = rarity.Id switch
		{
			"common" => Mathf.Max(0.2f, 1.35f - weaponTier * 0.18f),
			"uncommon" => 0.9f + weaponTier * 0.10f,
			"rare" => Mathf.Max(0.35f, weaponTier * 0.35f),
			"epic" => weaponTier >= 2 ? weaponTier * 0.16f : 0.04f,
			"legendary" => weaponTier >= 3 ? weaponTier * 0.05f : 0.01f,
			_ => 1f
		};

		return rarity.Weight * tierBias;
	}
}
