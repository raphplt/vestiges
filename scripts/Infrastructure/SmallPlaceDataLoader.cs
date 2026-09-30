using System.Collections.Generic;
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

public static class SmallPlaceDataLoader
{
	private static SmallPlaceConfig _config;

	public static SmallPlaceConfig Load()
	{
		if (_config != null)
			return _config;

		using FileAccess file = FileAccess.Open("res://data/world/small_places.json", FileAccess.ModeFlags.Read);
		Json json = new();
		if (file == null || json.Parse(file.GetAsText()) != Error.Ok)
		{
			GD.PushError("[SmallPlaceDataLoader] Cannot read data/world/small_places.json");
			_config = new SmallPlaceConfig();
			return _config;
		}

		Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();
		List<SmallPlaceData> places = new();
		foreach (Variant item in root["places"].AsGodotArray())
		{
			Godot.Collections.Dictionary dict = item.AsGodotDictionary();
			List<string> sprites = new();
			foreach (Variant sprite in dict["sprites"].AsGodotArray())
				sprites.Add(sprite.AsString());
			List<string> lore = new();
			if (dict.ContainsKey("lore"))
				foreach (Variant line in dict["lore"].AsGodotArray())
					lore.Add(line.AsString());
			Godot.Collections.Array color = dict["color"].AsGodotArray();
			places.Add(new SmallPlaceData
			{
				Id = dict["id"].AsString(),
				Sprites = sprites,
				PromptKey = dict["prompt"].AsString(),
				HoldSeconds = Float(dict, "hold_seconds"),
				Reward = dict["reward"].AsString(),
				Amount = Float(dict, "amount"),
				AmountMin = (int)Float(dict, "amount_min"),
				AmountMax = (int)Float(dict, "amount_max"),
				Enemies = (int)Float(dict, "enemies"),
				AmbushRadiusPx = Float(dict, "ambush_radius_px"),
				AmbushTimeoutSeconds = Float(dict, "ambush_timeout_s"),
				MaxPerMap = (int)Float(dict, "max_per_map"),
				Chance = Float(dict, "chance"),
				DurationSeconds = Float(dict, "duration_s"),
				RadiusPx = Float(dict, "radius_px"),
				Lore = lore,
				Color = new Color((float)color[0].AsDouble(), (float)color[1].AsDouble(), (float)color[2].AsDouble()),
				Family = dict.ContainsKey("family") ? dict["family"].AsString() : null,
			});
		}
		_config = new SmallPlaceConfig
		{
			Enabled = root.ContainsKey("enabled") && root["enabled"].AsBool(),
			MinSpacingPx = Float(root, "min_spacing_px"),
			SignRangePx = Float(root, "sign_range_px"),
			Places = places,
		};
		return _config;
	}

	private static float Float(Godot.Collections.Dictionary dict, string key) =>
		dict.ContainsKey(key) ? (float)dict[key].AsDouble() : 0f;
}
