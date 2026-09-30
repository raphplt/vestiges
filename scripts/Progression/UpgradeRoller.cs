using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Progression;

/// <summary>Une rareté d'amélioration (data/progression/upgrade_rarities.json).</summary>
public class UpgradeRarity
{
	public string Id;
	public float Weight;
	public int WeaponStats;
	public float WeaponGain;
	/// <summary>Gain d'une stat entière tirée (0,5 en commune à 3 en légendaire ; plan 23 R4).</summary>
	public float IntegerGain;
	/// <summary>Multiple du pas d'un objet pour une carte de cette rareté (plan 23 R3), et des bénédictions des Mémoriaux.</summary>
	public float PassiveGain;
	public int Rank;
}

/// <summary>
/// Tirage des améliorations du level-up (plan 17, lot 1B) : la rareté (poids, puis montées dues à la Chance et
/// à l'oubli de la zone), puis les stats de l'arme qui montent et de combien. Aucun état : tout est rejoué à la demande.
/// </summary>
public static class UpgradeRoller
{
	private static readonly List<UpgradeRarity> _rarities = new();
	private static readonly Dictionary<ErasureManager.ErasureZonePhase, int> _zoneSteps = new();
	private static float _bumpChancePerStep = 0.3f;
	private static float _luckStepsPerPoint = 10f;
	private static bool _loaded;

	public static IReadOnlyList<UpgradeRarity> Rarities
	{
		get
		{
			Load();
			return _rarities;
		}
	}

	public static UpgradeRarity Get(string id)
	{
		Load();
		foreach (UpgradeRarity rarity in _rarities)
			if (rarity.Id == id)
				return rarity;
		return _rarities.Count > 0 ? _rarities[0] : null;
	}

	/// <summary>Crans de montée : la Chance du joueur, l'oubli de la zone où il se tient et le Péril de la run.</summary>
	public static float BumpSteps(float luck, ErasureManager.ErasureZonePhase phase, int peril)
	{
		Load();
		return luck * _luckStepsPerPoint + _zoneSteps.GetValueOrDefault(phase) + PerilDataLoader.RaritySteps(peril);
	}

	/// <summary>
	/// Rareté tirée selon les poids, puis une chance de monter d'un rang par cran (<c>bump_chance_per_step</c>) :
	/// trois crans d'oubli donnent trois chances, jamais plus d'un rang chacune. Un cran fractionnaire (Chance)
	/// compte au prorata.
	/// </summary>
	public static UpgradeRarity RollRarity(float bumpSteps, RandomNumberGenerator rng)
	{
		Load();
		float total = 0f;
		foreach (UpgradeRarity rarity in _rarities)
			total += rarity.Weight;
		float roll = rng.Randf() * total;
		int rank = 0;
		for (; rank < _rarities.Count - 1; rank++)
		{
			roll -= _rarities[rank].Weight;
			if (roll < 0f)
				break;
		}

		int fullSteps = Mathf.FloorToInt(bumpSteps);
		int trials = fullSteps + (rng.Randf() < bumpSteps - fullSteps ? 1 : 0);
		for (int i = 0; i < trials && rank < _rarities.Count - 1; i++)
		{
			if (rng.Randf() < _bumpChancePerStep)
				rank++;
		}
		return _rarities[rank];
	}

	/// <summary>Rareté tirée comme <see cref="RollRarity"/>, relevée à <paramref name="minId"/> au moins.</summary>
	public static UpgradeRarity RollRarityAtLeast(float bumpSteps, string minId, RandomNumberGenerator rng)
	{
		UpgradeRarity rarity = RollRarity(bumpSteps, rng);
		UpgradeRarity min = Get(minId);
		return rarity.Rank < min.Rank ? min : rarity;
	}

	/// <summary>
	/// Gains d'une amélioration d'arme : stats montables tirées selon leur poids (sans répétition). Une stat de pas
	/// gagne son pas au gain de la rareté ; une stat entière (projectile, perforation, saut, orbe) gagne la part
	/// entière de la rareté, en fraction (plan 23 R4).
	/// </summary>
	public static List<StatGain> RollWeaponGains(WeaponInstance weapon, UpgradeRarity rarity, RandomNumberGenerator rng)
	{
		List<StatGain> gains = new();
		Dictionary<string, float> pool = new(weapon.Base.Growth);
		for (int i = 0; i < rarity.WeaponStats && pool.Count > 0; i++)
		{
			string stat = WeightedPick(pool, rng);
			pool.Remove(stat);
			WeaponUpgradeStatConfig config = WeaponUpgradeDataLoader.GetStatConfig(stat);
			if (config != null)
				gains.Add(new StatGain(stat, config.Integer ? rarity.IntegerGain : config.Step * rarity.WeaponGain));
		}
		return gains;
	}

	/// <summary>
	/// Gains d'une amélioration à la rareté donnée : stats tirées pour une arme, niveaux gagnés pour un objet.
	/// </summary>
	public static FragmentOption RollGains(FragmentOption option, Player player, UpgradeRarity rarity, RandomNumberGenerator rng)
	{
		if (option.Type == "weapon_upgrade")
		{
			foreach (WeaponInstance weapon in player.WeaponSlots)
				if (weapon.Id == option.Id)
					return option.WithWeaponUpgrade(rarity, RollWeaponGains(weapon, rarity, rng));
			return option;
		}

		return option.Type == "passive_upgrade" ? option.WithPassiveUpgrade(rarity) : option;
	}

	private static string WeightedPick(Dictionary<string, float> weights, RandomNumberGenerator rng)
	{
		float total = 0f;
		foreach (float weight in weights.Values)
			total += weight;
		float roll = rng.Randf() * total;
		string last = null;
		foreach ((string key, float weight) in weights)
		{
			last = key;
			roll -= weight;
			if (roll < 0f)
				return key;
		}
		return last;
	}

	private static void Load()
	{
		if (_loaded)
			return;
		_loaded = true;

		using FileAccess file = FileAccess.Open("res://data/progression/upgrade_rarities.json", FileAccess.ModeFlags.Read);
		Json json = new();
		if (file == null || json.Parse(file.GetAsText()) != Error.Ok)
		{
			GD.PushError("[UpgradeRoller] Cannot read data/progression/upgrade_rarities.json");
			return;
		}

		Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();
		int rank = 0;
		foreach (Variant item in root["rarities"].AsGodotArray())
		{
			Godot.Collections.Dictionary dict = item.AsGodotDictionary();
			_rarities.Add(new UpgradeRarity
			{
				Id = dict["id"].AsString(),
				Weight = (float)dict["weight"].AsDouble(),
				WeaponStats = (int)dict["weapon_stats"].AsDouble(),
				WeaponGain = (float)dict["weapon_gain"].AsDouble(),
				IntegerGain = (float)dict["integer_gain"].AsDouble(),
				PassiveGain = (float)dict["passive_gain"].AsDouble(),
				Rank = rank++,
			});
		}

		_bumpChancePerStep = (float)root["bump_chance_per_step"].AsDouble();
		_luckStepsPerPoint = (float)root["luck_steps_per_point"].AsDouble();
		Godot.Collections.Dictionary zones = root["zone_steps"].AsGodotDictionary();
		_zoneSteps[ErasureManager.ErasureZonePhase.Fragile] = (int)zones["fragile"].AsDouble();
		_zoneSteps[ErasureManager.ErasureZonePhase.Frayed] = (int)zones["frayed"].AsDouble();
		_zoneSteps[ErasureManager.ErasureZonePhase.Erased] = (int)zones["erased"].AsDouble();
		_zoneSteps[ErasureManager.ErasureZonePhase.Void] = (int)zones["erased"].AsDouble();
	}
}
