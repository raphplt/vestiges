using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Gain d'un Repère : une stat, ou des jetons de choix (relances, bannissements gratuits).</summary>
public sealed class WaymarkReward
{
	public string NameKey { get; init; }
	public string Stat { get; init; }
	public float Amount { get; init; }
	public string ModifierType { get; init; } = "additive";
	public int Rerolls { get; init; }
	public int Banishes { get; init; }
}

/// <summary>Repères (data/world/waymarks.json, plan 22 §3 B, plan 24 D3) : gain au premier usage de chaque type de lieu.</summary>
public sealed class WaymarkConfig
{
	public bool Enabled { get; init; }
	/// <summary>Type de lieu → son gain, dans l'ordre des données.</summary>
	public Dictionary<string, WaymarkReward> Rewards { get; init; } = new();
}

public static class WaymarkDataLoader
{
	private static WaymarkConfig _config;

	public static WaymarkConfig Load()
	{
		if (_config != null)
			return _config;

		using FileAccess file = FileAccess.Open("res://data/world/waymarks.json", FileAccess.ModeFlags.Read);
		Json json = new();
		if (file == null || json.Parse(file.GetAsText()) != Error.Ok)
		{
			GD.PushError("[WaymarkDataLoader] Cannot read data/world/waymarks.json");
			_config = new WaymarkConfig();
			return _config;
		}

		// Clés manquantes : Repères désactivés plutôt qu'une run qui ne démarre pas (le joueur crée ce composant).
		Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();
		if (!root.ContainsKey("types"))
		{
			GD.PushError("[WaymarkDataLoader] waymarks.json : 'types' est requis");
			_config = new WaymarkConfig();
			return _config;
		}
		Dictionary<string, WaymarkReward> rewards = new();
		foreach (Variant item in root["types"].AsGodotArray())
		{
			Godot.Collections.Dictionary type = item.AsGodotDictionary();
			if (!type.ContainsKey("id") || !type.ContainsKey("name_key"))
			{
				GD.PushError("[WaymarkDataLoader] Type sans 'id' ou 'name_key' ignoré");
				continue;
			}
			WaymarkReward reward = new()
			{
				NameKey = type["name_key"].AsString(),
				Stat = type.ContainsKey("stat") ? type["stat"].AsString() : null,
				Amount = type.ContainsKey("amount") ? (float)type["amount"].AsDouble() : 0f,
				ModifierType = type.ContainsKey("modifier_type") ? type["modifier_type"].AsString() : "additive",
				Rerolls = type.ContainsKey("rerolls") ? (int)type["rerolls"].AsDouble() : 0,
				Banishes = type.ContainsKey("banishes") ? (int)type["banishes"].AsDouble() : 0,
			};
			if (reward.Stat == null && reward.Rerolls == 0 && reward.Banishes == 0)
				GD.PushError($"[WaymarkDataLoader] Repère {type["id"]} sans gain");
			rewards[type["id"].AsString()] = reward;
		}
		_config = new WaymarkConfig
		{
			Enabled = root.ContainsKey("enabled") && root["enabled"].AsBool(),
			Rewards = rewards,
		};
		return _config;
	}
}
