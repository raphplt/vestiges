using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Un bonus lâché (data/world/field_bonuses.json) : son effet et ses réglages.</summary>
public sealed class FieldBonusData
{
	public string Id { get; init; }
	public string Sprite { get; init; }
	public string NameKey { get; init; }
	public float Weight { get; init; }
	public string Effect { get; init; }
	public Color Color { get; init; }
	public Dictionary<string, float> Params { get; init; } = new();

	/// <summary>Réglage de l'effet ; le chargement exige chacun de ceux que l'effet lit.</summary>
	public float Param(string name) => Params[name];
}

/// <summary>Bonus lâchés (plan 24 C4) : sources, limites et catalogue.</summary>
public sealed class FieldBonusConfig
{
	public bool Enabled { get; init; }
	public int MaxOnGround { get; init; }
	public float LifetimeSeconds { get; init; }
	public float BlinkSeconds { get; init; }
	public float PickupPx { get; init; }
	public Dictionary<string, float> VariantChance { get; init; } = new();
	public float KillChance { get; init; }
	public List<string> KillPool { get; init; } = new();
	public int CrisisEnd { get; init; }
	public string CrisisFirst { get; init; }
	public List<FieldBonusData> Bonuses { get; init; } = new();

	public FieldBonusData Get(string id) => Bonuses.Find(bonus => bonus.Id == id);
}

/// <summary>
/// Bonus lâchés (data/world/field_bonuses.json), contrôlés en entier (plan 26 Q7c) : sources, limites, et pour chaque
/// bonus un effet connu avec exactement ses réglages, une image du manifeste des ramassables et un nom traduit.
/// </summary>
public static class FieldBonusDataLoader
{
	private const string ConfigPath = "res://data/world/field_bonuses.json";

	/// <summary>Effets lus par FieldBonusDirector, et les réglages que chacun exige.</summary>
	private static readonly Dictionary<string, string[]> EffectParams = new()
	{
		["heal"] = new[] { "ratio" },
		["magnet"] = new[] { "multiplier", "duration_s" },
		["shield"] = new[] { "ratio", "duration_s" },
		["frenzy"] = new[] { "amount", "duration_s" },
		["blast"] = new[] { "radius_px", "hp_ratio", "knockback_px" },
	};
	private static readonly string[] BonusKeys = { "id", "sprite", "name_key", "weight", "effect", "color" };

	private static FieldBonusConfig _config;
	private static string _loadError;

	public static FieldBonusConfig Load()
	{
		if (_config != null)
			return _config;
		_config = new FieldBonusConfig();
		string error = FileAccess.FileExists(ConfigPath)
			? Apply(FileAccess.GetFileAsString(ConfigPath),
				DataKeySets.SectionKeys("res://assets/vfx/pickups/pickups_manifest.json", "pickups"),
				DataKeySets.SectionKeys("res://data/enemies/_variants.json", "variants"),
				key => TranslationServer.Translate(key) != key)
			: "absent";
		if (error != null)
		{
			_loadError = $"{ConfigPath} : {error}";
			GD.PushError($"[FieldBonusDataLoader] {_loadError}");
		}
		return _config;
	}

	/// <summary>Réglages lus et contrôlés ; faux, avec la raison, s'ils ont été refusés (la run ne démarre pas).</summary>
	public static bool TryLoad(out string error)
	{
		Load();
		error = _loadError;
		return error == null;
	}

	/// <summary>Contrôle un texte de bonus et ne le publie que s'il est entièrement valide. Rend l'erreur, ou null.</summary>
	public static string Apply(string json, IReadOnlyCollection<string> sprites, IReadOnlyCollection<string> variants,
		Func<string, bool> translated)
	{
		try
		{
			using JsonDocument document = JsonDocument.Parse(json);
			JsonConfigReader reader = new(document.RootElement);
			JsonElement root = reader.Root;
			reader.AllowOnly(root, "bonus lâchés", "enabled", "max_on_ground", "lifetime_s", "blink_s", "pickup_px",
				"variant_chance", "kill_chance", "kill_pool", "crisis_end", "crisis_first", "bonuses");
			List<FieldBonusData> bonuses = new();
			HashSet<string> ids = new();
			foreach (JsonElement entry in reader.List(root, "bonuses", 1))
			{
				JsonConfigReader item = new(entry);
				string id = item.Text(entry, "id");
				string effect = item.OneOf(entry, "effect", EffectParams.Keys);
				string[] required = effect != null ? EffectParams[effect] : Array.Empty<string>();
				List<string> keys = new(BonusKeys);
				keys.AddRange(required);
				item.AllowOnly(entry, "clés", keys.ToArray());
				Dictionary<string, float> parameters = new();
				foreach (string name in required)
					parameters[name] = item.Positive(entry, name);
				FieldBonusData bonus = new()
				{
					Id = id,
					Sprite = item.OneOf(entry, "sprite", sprites),
					NameKey = item.Text(entry, "name_key"),
					Weight = item.Positive(entry, "weight"),
					Effect = effect,
					Color = item.Rgb(entry, "color"),
					Params = parameters,
				};
				if (item.Error == null && !ids.Add(id))
					item.Fail("identifiant en double");
				if (item.Error == null && !translated(bonus.NameKey))
					item.Fail($"name_key : clé « {bonus.NameKey} » absente des traductions");
				if (item.Error != null)
					return $"bonus {id ?? "?"} : {item.Error}";
				bonuses.Add(bonus);
			}
			JsonElement chances = reader.Section("variant_chance");
			Dictionary<string, float> variantChance = new();
			foreach ((string variant, _) in reader.Entries(chances, "variant_chance"))
			{
				variantChance[variant] = reader.Chance(chances, variant);
				if (reader.Error == null && !JsonConfigReader.Contains(variants, variant))
					reader.Fail($"variant_chance : variante « {variant} » inconnue");
			}
			List<string> killPool = new();
			foreach (JsonElement id in reader.List(root, "kill_pool", 1))
			{
				string value = id.ValueKind == JsonValueKind.String ? id.GetString() : null;
				if (reader.Error == null && (value == null || !ids.Contains(value)))
					reader.Fail($"kill_pool : bonus « {id} » inconnu");
				killPool.Add(value);
			}
			FieldBonusConfig config = new()
			{
				Enabled = reader.Flag(root, "enabled"),
				MaxOnGround = reader.Integer(root, "max_on_ground", 0, 100),
				LifetimeSeconds = reader.Positive(root, "lifetime_s"),
				BlinkSeconds = reader.NonNegative(root, "blink_s"),
				PickupPx = reader.Positive(root, "pickup_px"),
				VariantChance = variantChance,
				KillChance = reader.Chance(root, "kill_chance"),
				KillPool = killPool,
				CrisisEnd = reader.Integer(root, "crisis_end", 0, 20),
				CrisisFirst = reader.OneOf(root, "crisis_first", ids),
				Bonuses = bonuses,
			};
			if (reader.Error == null && !root.TryGetProperty("enabled", out _))
				reader.Fail("enabled absent");
			if (reader.Error != null)
				return reader.Error;
			_config = config;
			_loadError = null;
			return null;
		}
		catch (JsonException ex)
		{
			return $"JSON illisible : {ex.Message}";
		}
	}

}
