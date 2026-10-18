using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Pas d'amélioration d'une stat d'arme (data/weapons/weapon_upgrades.json).</summary>
public class WeaponUpgradeStatConfig
{
	public string Key { get; set; }
	/// <summary>Gain d'une amélioration Commune : fraction de la base (multiplicative) ou valeur ajoutée (additive).</summary>
	public float Step { get; set; }
	public bool Additive { get; set; }
	/// <summary>
	/// Stat entière fractionnaire (projectile, perforation, saut, orbe ; plan 23 R4) : son gain vient de la rareté, et
	/// sa partie décimale se joue à chaque attaque.
	/// </summary>
	public bool Integer { get; set; }
	/// <summary>Plafond de la valeur effective (angles), 0 si aucun.</summary>
	public float Max { get; set; }
}

/// <summary>
/// Montées d'armes (data/weapons/weapon_upgrades.json), lues strictement une fois (plan 26 Q7f-3). Un fichier refusé
/// n'est pas publié et arrête le chargement de la run (GameBootstrap).
/// </summary>
public static class WeaponUpgradeDataLoader
{
	public const string DataPath = "res://data/weapons/weapon_upgrades.json";
	private static readonly string[] Modes = { "multiplicative", "additive" };

	private static Dictionary<string, WeaponUpgradeStatConfig> _statConfigs = new();
	private static int _weaponMaxLevel = 50;
	private static bool _loaded;
	private static string _loadError;

	/// <summary>Le fichier lu et contrôlé ; faux, avec la raison, s'il a été refusé (la run ne démarre pas).</summary>
	public static bool TryLoad(out string error)
	{
		Load();
		error = _loadError;
		return error == null;
	}

	public static void Load()
	{
		if (_loaded)
			return;
		_loaded = true;
		_loadError = $"{DataPath} : lecture interrompue";
		using FileAccess file = FileAccess.Open(DataPath, FileAccess.ModeFlags.Read);
		_loadError = file == null ? $"{DataPath} : fichier illisible" : Apply(file.GetAsText());
		if (_loadError != null)
		{
			GD.PushError($"[WeaponUpgradeDataLoader] {_loadError}");
			return;
		}
		GD.Print($"[WeaponUpgradeDataLoader] Loaded {_statConfigs.Count} upgradeable stats, weapon max level = {_weaponMaxLevel}");
	}

	/// <summary>Contrôle <paramref name="text"/> et ne publie qu'un fichier accepté ; rend l'erreur, nulle si tout est publié.</summary>
	public static string Apply(string text)
	{
		try
		{
			using JsonDocument document = JsonDocument.Parse(text);
			JsonConfigReader reader = new(document.RootElement);
			JsonElement root = reader.Root;
			reader.AllowOnly(root, "montées d'armes", "weapon_max_level", "stats");
			int maxLevel = JsonConfigReader.Has(root, "weapon_max_level") ? reader.Integer(root, "weapon_max_level", 1, 1000) : 50;
			Dictionary<string, WeaponUpgradeStatConfig> configs = new();
			foreach ((string key, JsonElement stat) in reader.Entries(reader.Section(root, "stats"), "stats"))
			{
				if (stat.ValueKind != JsonValueKind.Object)
				{
					reader.Fail($"stats.{key} : objet attendu");
					break;
				}
				reader.AllowOnly(stat, $"stats.{key}", "step", "mode", "integer", "max");
				configs[key] = new WeaponUpgradeStatConfig
				{
					Key = key,
					Step = reader.Positive(stat, "step"),
					Additive = JsonConfigReader.Has(stat, "mode") && reader.OneOf(stat, "mode", Modes) == "additive",
					Integer = reader.Flag(stat, "integer"),
					Max = JsonConfigReader.Has(stat, "max") ? reader.NonNegative(stat, "max") : 0f,
				};
			}
			if (reader.Error == null && configs.Count == 0)
				reader.Fail("stats : au moins une stat montable attendue");
			if (reader.Error != null)
				return reader.Error;
			_statConfigs = configs;
			_weaponMaxLevel = maxLevel;
			return null;
		}
		catch (JsonException exception)
		{
			return $"JSON invalide : {exception.Message}";
		}
	}

	public static WeaponUpgradeStatConfig GetStatConfig(string key)
	{
		Load();
		return _statConfigs.GetValueOrDefault(key);
	}

	public static int GetWeaponMaxLevel()
	{
		Load();
		return _weaponMaxLevel;
	}
}
