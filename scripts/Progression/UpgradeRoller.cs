using System;
using System.Collections.Generic;
using System.Text.Json;
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
	/// <summary>Chance, par cran de montée, de passer de cette rareté à la suivante (DECISIONS §53).</summary>
	public float BumpChance;
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
	/// Rareté tirée selon les poids, puis une chance de monter d'un rang par cran, propre au rang où l'on est
	/// (<c>bump_chance</c>, de plus en plus faible vers épique et légendaire) :
	/// trois crans d'oubli donnent trois chances, jamais plus d'un rang chacune. Un cran fractionnaire (Chance)
	/// compte au prorata.
	/// </summary>
	public static UpgradeRarity RollRarity(float bumpSteps, RandomNumberGenerator rng) => RollRarity(bumpSteps, rng, out _);

	/// <summary>Comme <see cref="RollRarity(float, RandomNumberGenerator)"/>, en rendant aussi la rareté tirée avant la montée.</summary>
	public static UpgradeRarity RollRarity(float bumpSteps, RandomNumberGenerator rng, out UpgradeRarity rolled)
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

		rolled = _rarities[rank];
		int fullSteps = Mathf.FloorToInt(bumpSteps);
		int trials = fullSteps + (rng.Randf() < bumpSteps - fullSteps ? 1 : 0);
		for (int i = 0; i < trials && rank < _rarities.Count - 1; i++)
		{
			if (rng.Randf() < _rarities[rank].BumpChance)
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
	/// entière de la rareté, en fraction (plan 23 R4). <paramref name="extraStats"/> : stats en plus de la rareté
	/// (Trempe de l'Atelier, plan 22 C2), dans la limite des stats que l'arme peut monter.
	/// </summary>
	public static List<StatGain> RollWeaponGains(WeaponInstance weapon, UpgradeRarity rarity, RandomNumberGenerator rng, int extraStats = 0)
	{
		List<StatGain> gains = new();
		Dictionary<string, float> pool = new(weapon.Base.Growth);
		for (int i = 0; i < rarity.WeaponStats + extraStats && pool.Count > 0; i++)
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
					return option.WithWeaponUpgrade(rarity, RollWeaponGains(weapon, rarity, rng, player.TemperCharges > 0 ? 1 : 0));
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

	private const string ConfigPath = "res://data/progression/upgrade_rarities.json";

	private static void Load()
	{
		if (_loaded)
			return;
		if (!TryLoad(out string error))
			GD.PushError($"[UpgradeRoller] {error}");
	}

	/// <summary>Lit et contrôle tout le fichier (plan 26 Q7b) ; rien n'est publié s'il est invalide.</summary>
	public static bool TryLoad(out string error)
	{
		_loaded = true;
		if (_rarities.Count > 0)
		{
			error = null;
			return true;
		}
		error = FileAccess.FileExists(ConfigPath) ? Apply(FileAccess.GetFileAsString(ConfigPath)) : "absent";
		if (error != null)
			error = $"{ConfigPath} : {error}";
		return error == null;
	}

	/// <summary>Contrôle un texte de raretés et ne le publie que s'il est entièrement valide. Rend l'erreur, ou null.</summary>
	public static string Apply(string json)
	{
		try
		{
			using JsonDocument document = JsonDocument.Parse(json);
			JsonConfigReader reader = new(document.RootElement);
			JsonElement root = reader.Root;
			reader.AllowOnly(root, "raretés", "rarities", "luck_steps_per_point", "zone_steps");
			if (reader.Error != null)
				return reader.Error;
			if (!root.TryGetProperty("rarities", out JsonElement list) || list.ValueKind != JsonValueKind.Array || list.GetArrayLength() == 0)
				return "rarities : liste non vide attendue";
			List<UpgradeRarity> rarities = new();
			HashSet<string> ids = new();
			foreach (JsonElement entry in list.EnumerateArray())
			{
				JsonConfigReader item = new(entry);
				string id = entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty("id", out JsonElement idValue)
					&& idValue.ValueKind == JsonValueKind.String ? idValue.GetString() : null;
				string where = $"rareté {id ?? "?"}";
				if (string.IsNullOrEmpty(id))
					return $"rareté n°{rarities.Count} : id absent";
				if (!ids.Add(id))
					return $"{where} : identifiant en double";
				item.AllowOnly(entry, "clés", "id", "weight", "bump_chance", "weapon_stats", "weapon_gain", "integer_gain", "passive_gain");
				UpgradeRarity rarity = new()
				{
					Id = id,
					Weight = item.Positive(entry, "weight"),
					BumpChance = item.NonNegative(entry, "bump_chance"),
					WeaponStats = item.Integer(entry, "weapon_stats", 1, 8),
					WeaponGain = item.Positive(entry, "weapon_gain"),
					IntegerGain = item.Positive(entry, "integer_gain"),
					PassiveGain = item.Positive(entry, "passive_gain"),
					Rank = rarities.Count,
				};
				if (item.Error == null && rarity.BumpChance > 1f)
					item.Fail(FormattableString.Invariant($"bump_chance : {rarity.BumpChance} (part dans [0 ; 1] attendue)"));
				// Les objets vérifient qu'un facteur ne s'annule pas au gain le plus fort qu'ils supposent.
				if (item.Error == null && rarity.PassiveGain > PassiveSouvenirDataLoader.MaxRarityGain)
					item.Fail(FormattableString.Invariant($"passive_gain : {rarity.PassiveGain} (au plus {PassiveSouvenirDataLoader.MaxRarityGain}, borne des objets)"));
				if (item.Error != null)
					return $"{where}, {item.Error}";
				rarities.Add(rarity);
			}
			float luckSteps = reader.NonNegative(root, "luck_steps_per_point");
			JsonElement zones = reader.Section("zone_steps");
			reader.AllowOnly(zones, "zone_steps", "fragile", "frayed", "erased");
			int fragile = reader.Integer(zones, "fragile", 0, 10);
			int frayed = reader.Integer(zones, "frayed", 0, 10);
			int erased = reader.Integer(zones, "erased", 0, 10);
			if (reader.Error != null)
				return reader.Error;
			_rarities.Clear();
			_rarities.AddRange(rarities);
			_luckStepsPerPoint = luckSteps;
			_zoneSteps[ErasureManager.ErasureZonePhase.Fragile] = fragile;
			_zoneSteps[ErasureManager.ErasureZonePhase.Frayed] = frayed;
			_zoneSteps[ErasureManager.ErasureZonePhase.Erased] = erased;
			_zoneSteps[ErasureManager.ErasureZonePhase.Void] = erased;
			return null;
		}
		catch (JsonException ex)
		{
			return $"JSON illisible : {ex.Message}";
		}
	}
}
