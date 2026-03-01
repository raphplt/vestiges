using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class FusionData
{
	public string Id;
	public string Name;
	public string Description;
	public string WeaponId;
	public string PassiveId;
	public string Type;
	public string DamageType;
	public string AttackPattern;
	public Dictionary<string, float> Stats = new();
	public string SpecialEffectType;
	public Dictionary<string, float> SpecialEffectParams = new();
	public string LoreFlavor;
}

public static class FusionDataLoader
{
	private static readonly Dictionary<string, FusionData> _cache = new();
	private static readonly List<FusionData> _all = new();
	private static bool _loaded;

	public static void Load()
	{
		if (_loaded)
			return;

		FileAccess file = FileAccess.Open("res://data/progression/fusions.json", FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PushError("[FusionDataLoader] Cannot open fusions.json");
			_loaded = true;
			return;
		}

		string jsonText = file.GetAsText();
		file.Close();

		Json json = new();
		if (json.Parse(jsonText) != Error.Ok)
		{
			GD.PushError($"[FusionDataLoader] Parse error: {json.GetErrorMessage()}");
			_loaded = true;
			return;
		}

		Godot.Collections.Array array = json.Data.AsGodotArray();
		foreach (Variant item in array)
		{
			Godot.Collections.Dictionary dict = item.AsGodotDictionary();
			FusionData data = ParseEntry(dict);
			if (data != null)
			{
				_cache[data.Id] = data;
				_all.Add(data);
			}
		}

		_loaded = true;
		GD.Print($"[FusionDataLoader] Loaded {_cache.Count} fusions");
	}

	private static FusionData ParseEntry(Godot.Collections.Dictionary dict)
	{
		if (!dict.ContainsKey("id"))
			return null;

		FusionData data = new()
		{
			Id = dict["id"].AsString(),
			Name = dict.ContainsKey("name") ? dict["name"].AsString() : "",
			Description = dict.ContainsKey("description") ? dict["description"].AsString() : "",
			WeaponId = dict.ContainsKey("weapon_id") ? dict["weapon_id"].AsString() : "",
			PassiveId = dict.ContainsKey("passive_id") ? dict["passive_id"].AsString() : "",
			Type = dict.ContainsKey("type") ? dict["type"].AsString() : "melee",
			DamageType = dict.ContainsKey("damage_type") ? dict["damage_type"].AsString() : "physical",
			AttackPattern = dict.ContainsKey("attack_pattern") ? dict["attack_pattern"].AsString() : "arc",
			LoreFlavor = dict.ContainsKey("lore_flavor") ? dict["lore_flavor"].AsString() : ""
		};

		if (dict.ContainsKey("stats"))
		{
			Godot.Collections.Dictionary statsDict = dict["stats"].AsGodotDictionary();
			foreach (Variant key in statsDict.Keys)
				data.Stats[key.AsString()] = (float)statsDict[key].AsDouble();
		}

		if (dict.ContainsKey("special_effect"))
		{
			Godot.Collections.Dictionary seDict = dict["special_effect"].AsGodotDictionary();
			data.SpecialEffectType = seDict.ContainsKey("type") ? seDict["type"].AsString() : "";
			foreach (Variant key in seDict.Keys)
			{
				string k = key.AsString();
				if (k != "type")
				{
					Variant val = seDict[key];
					if (val.VariantType == Variant.Type.Float || val.VariantType == Variant.Type.Int)
						data.SpecialEffectParams[k] = (float)val.AsDouble();
				}
			}
		}

		return data;
	}

	public static FusionData Get(string id)
	{
		if (!_loaded) Load();
		return _cache.GetValueOrDefault(id);
	}

	public static List<FusionData> GetAll()
	{
		if (!_loaded) Load();
		return new List<FusionData>(_all);
	}

	/// <summary>
	/// Trouve une fusion possible pour une arme + passif donné.
	/// Retourne null si aucune fusion n'existe pour cette combinaison.
	/// </summary>
	public static FusionData FindFusion(string weaponId, string passiveId)
	{
		if (!_loaded) Load();

		foreach (FusionData fusion in _all)
		{
			if (fusion.WeaponId == weaponId && fusion.PassiveId == passiveId)
				return fusion;
		}
		return null;
	}

	/// <summary>
	/// Retourne toutes les fusions disponibles pour les combinaisons arme+passif données.
	/// </summary>
	public static List<FusionData> FindAvailableFusions(
		IEnumerable<string> maxedWeaponIds,
		IEnumerable<string> maxedPassiveIds)
	{
		if (!_loaded) Load();

		HashSet<string> weapons = new(maxedWeaponIds);
		HashSet<string> passives = new(maxedPassiveIds);
		List<FusionData> available = new();

		foreach (FusionData fusion in _all)
		{
			if (weapons.Contains(fusion.WeaponId) && passives.Contains(fusion.PassiveId))
				available.Add(fusion);
		}

		return available;
	}
}
