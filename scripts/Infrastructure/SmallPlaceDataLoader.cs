using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Un type de petit lieu (plan 22, lots C1 et C4) : les décors qui le portent, le geste, la récompense.</summary>
public sealed class SmallPlaceData
{
	public string Id { get; init; }
	public List<string> Sprites { get; init; } = new();
	public string PromptKey { get; init; }
	public float HoldSeconds { get; init; }
	public string Reward { get; init; }
	public float Amount { get; init; }
	public int AmountMin { get; init; }
	public int AmountMax { get; init; }
	public int Enemies { get; init; }
	public float AmbushRadiusPx { get; init; }
	public float AmbushTimeoutSeconds { get; init; }
	public int MaxPerMap { get; init; }
	/// <summary>Chance d'une récompense rare (arme du wagonnet, soin de la voiture) ; sinon de l'Essence.</summary>
	public float Chance { get; init; }
	/// <summary>Durée d'un effet (vitesse de l'abribus, flèches de la cabine), en secondes.</summary>
	public float DurationSeconds { get; init; }
	/// <summary>Portée d'un effet (lieux révélés par la cabine), en pixels.</summary>
	public float RadiusPx { get; init; }
	/// <summary>Lignes de lore possibles (clés de traduction), une tirée à l'usage.</summary>
	public List<string> Lore { get; init; } = new();
	public Color Color { get; init; }
	/// <summary>Famille d'effets des étincelles du signe (« essence », « pale »…).</summary>
	public string Family { get; init; }
}

/// <summary>Réglages des petits lieux (data/world/small_places.json).</summary>
public sealed class SmallPlaceConfig
{
	public bool Enabled { get; init; }
	public float MinSpacingPx { get; init; }
	public float SignRangePx { get; init; }
	public List<SmallPlaceData> Places { get; init; } = new();
}

/// <summary>
/// Petits lieux (data/world/small_places.json), contrôlés en entier (plan 26 Q7c) : chaque lieu nomme des décors
/// existants, une invite et des lignes traduites, une famille d'effets connue, et une récompense connue avec
/// exactement ses réglages.
/// </summary>
public static class SmallPlaceDataLoader
{
	private const string ConfigPath = "res://data/world/small_places.json";

	/// <summary>Récompenses lues par SmallPlaceDirector, et les réglages que chacune exige.</summary>
	private static readonly Dictionary<string, string[]> RewardParams = new()
	{
		["heal"] = new[] { "amount" },
		["essence"] = new[] { "amount_min", "amount_max" },
		["ambush"] = new[] { "enemies", "ambush_radius_px", "ambush_timeout_s", "amount" },
		["xp"] = new[] { "amount" },
		["essence_or_weapon"] = new[] { "chance", "amount_min", "amount_max" },
		["search"] = new[] { "chance", "amount", "amount_min", "amount_max" },
		["reveal"] = new[] { "radius_px", "duration_s" },
		["speed"] = new[] { "amount", "duration_s" },
		["lore_heal"] = new[] { "amount", "lore" },
	};
	private static readonly string[] PlaceKeys = { "id", "sprites", "prompt", "hold_seconds", "reward", "max_per_map", "color", "family" };
	private static readonly string[] PropManifests =
	{
		"res://assets/props/urban_ruins/props_manifest.json", "res://assets/props/forest/props_manifest.json",
		"res://assets/props/swamp/props_manifest.json", "res://assets/props/wild_fields/props_manifest.json",
		"res://assets/props/collapsed_quarry/props_manifest.json",
	};

	private static SmallPlaceConfig _config;
	private static string _loadError;

	public static SmallPlaceConfig Load()
	{
		if (_config != null)
			return _config;
		_config = new SmallPlaceConfig();
		HashSet<string> props = new();
		foreach (string manifest in PropManifests)
			props.UnionWith(DataKeySets.TopLevelKeys(manifest));
		string error = FileAccess.FileExists(ConfigPath)
			? Apply(FileAccess.GetFileAsString(ConfigPath), props, DataKeySets.StringList("res://data/enemies/_contract.json", "families"),
				key => TranslationServer.Translate(key) != key)
			: "absent";
		if (error != null)
		{
			_loadError = $"{ConfigPath} : {error}";
			GD.PushError($"[SmallPlaceDataLoader] {_loadError}");
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

	/// <summary>Identifiants des petits lieux du fichier, pour les Repères (lecture sans publication).</summary>
	public static IReadOnlyCollection<string> PlaceIds() => DataKeySets.ListIds(ConfigPath, "places");

	/// <summary>Contrôle un texte de petits lieux et ne le publie que s'il est entièrement valide. Rend l'erreur, ou null.</summary>
	public static string Apply(string json, IReadOnlyCollection<string> props, IReadOnlyCollection<string> families,
		Func<string, bool> translated)
	{
		try
		{
			using JsonDocument document = JsonDocument.Parse(json);
			JsonConfigReader reader = new(document.RootElement);
			JsonElement root = reader.Root;
			reader.AllowOnly(root, "petits lieux", "enabled", "min_spacing_px", "sign_range_px", "places");
			List<SmallPlaceData> places = new();
			HashSet<string> ids = new();
			foreach (JsonElement entry in reader.List(root, "places", 1))
			{
				if (entry.ValueKind != JsonValueKind.Object)
					return "chaque lieu doit être un objet";
				JsonConfigReader item = new(entry);
				string id = item.Text(entry, "id");
				string reward = item.OneOf(entry, "reward", RewardParams.Keys);
				string[] required = reward != null ? RewardParams[reward] : Array.Empty<string>();
				List<string> keys = new(PlaceKeys);
				keys.AddRange(required);
				item.AllowOnly(entry, "clés", keys.ToArray());
				bool Needs(string name) => Array.IndexOf(required, name) >= 0;
				int amountMin = Needs("amount_min") ? item.Integer(entry, "amount_min", 0, 100_000) : 0;
				int amountMax = Needs("amount_max") ? item.Integer(entry, "amount_max", 0, 100_000) : 0;
				if (item.Error == null && amountMax < amountMin)
					item.Fail(FormattableString.Invariant($"amount_max : {amountMax} inférieur à amount_min {amountMin}"));
				SmallPlaceData place = new()
				{
					Id = id,
					Sprites = Strings(item, entry, "sprites", props, "décor"),
					PromptKey = item.Text(entry, "prompt"),
					HoldSeconds = item.Positive(entry, "hold_seconds"),
					Reward = reward,
					Amount = Needs("amount") ? item.Positive(entry, "amount") : 0f,
					AmountMin = amountMin,
					AmountMax = amountMax,
					Enemies = Needs("enemies") ? item.Count(entry, "enemies", 50) : 0,
					AmbushRadiusPx = Needs("ambush_radius_px") ? item.Positive(entry, "ambush_radius_px") : 0f,
					AmbushTimeoutSeconds = Needs("ambush_timeout_s") ? item.Positive(entry, "ambush_timeout_s") : 0f,
					MaxPerMap = item.Integer(entry, "max_per_map", 0, 1000),
					Chance = Needs("chance") ? item.Chance(entry, "chance") : 0f,
					DurationSeconds = Needs("duration_s") ? item.Positive(entry, "duration_s") : 0f,
					RadiusPx = Needs("radius_px") ? item.Positive(entry, "radius_px") : 0f,
					Lore = Needs("lore") ? Strings(item, entry, "lore", null, "ligne") : new List<string>(),
					Color = item.Rgb(entry, "color"),
					Family = item.OneOf(entry, "family", families),
				};
				if (item.Error == null && !ids.Add(id))
					item.Fail("identifiant en double");
				List<string> textKeys = new(place.Lore) { place.PromptKey };
				foreach (string key in textKeys)
				{
					if (item.Error == null && key != null && !translated(key))
						item.Fail($"clé « {key} » absente des traductions");
				}
				if (item.Error != null)
					return $"lieu {id ?? "?"} : {item.Error}";
				places.Add(place);
			}
			SmallPlaceConfig config = new()
			{
				Enabled = reader.Flag(root, "enabled"),
				MinSpacingPx = reader.Positive(root, "min_spacing_px"),
				SignRangePx = reader.Positive(root, "sign_range_px"),
				Places = places,
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

	/// <summary>Liste non vide de textes ; chacun pris dans <paramref name="allowed"/> si elle est donnée.</summary>
	private static List<string> Strings(JsonConfigReader reader, JsonElement owner, string key, IReadOnlyCollection<string> allowed, string what)
	{
		List<string> values = new();
		foreach (JsonElement value in reader.List(owner, key, 1))
		{
			string text = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
			if (reader.Error == null && string.IsNullOrEmpty(text))
				reader.Fail($"{key} : texte non vide attendu");
			else if (reader.Error == null && allowed != null && !JsonConfigReader.Contains(allowed, text))
				reader.Fail($"{key} : {what} « {text} » inconnu des manifestes");
			values.Add(text);
		}
		return values;
	}
}
