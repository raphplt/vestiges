using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Un bonus lâché (data/world/field_bonuses.json) : son effet et ses réglages.</summary>
public sealed class FieldBonusData
{
	public string Id { get; init; }
	public string NameKey { get; init; }
	public float Weight { get; init; }
	public string Effect { get; init; }
	public Color Color { get; init; }
	public Dictionary<string, float> Params { get; init; } = new();

	public float Param(string name, float fallback = 0f) => Params.TryGetValue(name, out float value) ? value : fallback;
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

public static class FieldBonusDataLoader
{
	private static FieldBonusConfig _config;

	public static FieldBonusConfig Load()
	{
		if (_config != null)
			return _config;

		using FileAccess file = FileAccess.Open("res://data/world/field_bonuses.json", FileAccess.ModeFlags.Read);
		Json json = new();
		if (file == null || json.Parse(file.GetAsText()) != Error.Ok || json.Data.VariantType != Variant.Type.Dictionary)
		{
			GD.PushError("[FieldBonusDataLoader] Cannot read data/world/field_bonuses.json");
			_config = new FieldBonusConfig();
			return _config;
		}
		Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();
		Dictionary<string, float> variants = new();
		foreach ((Variant key, Variant value) in root["variant_chance"].AsGodotDictionary())
			variants[key.AsString()] = (float)value.AsDouble();
		List<string> killPool = new();
		foreach (Variant id in root["kill_pool"].AsGodotArray())
			killPool.Add(id.AsString());
		List<FieldBonusData> bonuses = new();
		foreach (Variant entry in root["bonuses"].AsGodotArray())
		{
			Godot.Collections.Dictionary dict = entry.AsGodotDictionary();
			Dictionary<string, float> parameters = new();
			foreach ((Variant key, Variant value) in dict)
				if (value.VariantType is Variant.Type.Int or Variant.Type.Float && key.AsString() != "weight")
					parameters[key.AsString()] = (float)value.AsDouble();
			Godot.Collections.Array rgb = dict["color"].AsGodotArray();
			bonuses.Add(new FieldBonusData
			{
				Id = dict["id"].AsString(),
				NameKey = dict["name_key"].AsString(),
				Weight = (float)dict["weight"].AsDouble(),
				Effect = dict["effect"].AsString(),
				Color = new Color((float)rgb[0].AsDouble(), (float)rgb[1].AsDouble(), (float)rgb[2].AsDouble()),
				Params = parameters,
			});
		}
		_config = new FieldBonusConfig
		{
			Enabled = root["enabled"].AsBool(),
			MaxOnGround = (int)root["max_on_ground"].AsDouble(),
			LifetimeSeconds = (float)root["lifetime_s"].AsDouble(),
			BlinkSeconds = (float)root["blink_s"].AsDouble(),
			PickupPx = (float)root["pickup_px"].AsDouble(),
			VariantChance = variants,
			KillChance = (float)root["kill_chance"].AsDouble(),
			KillPool = killPool,
			CrisisEnd = (int)root["crisis_end"].AsDouble(),
			CrisisFirst = root["crisis_first"].AsString(),
			Bonuses = bonuses,
		};
		return _config;
	}
}
