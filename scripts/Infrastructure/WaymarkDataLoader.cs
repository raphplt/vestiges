using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Gain d'un Repère : une stat, ou des jetons de choix (relances, bannissements gratuits).</summary>
public sealed class WaymarkReward
{
	public string NameKey { get; init; }
	public string Stat { get; init; }
	public float Amount { get; init; }
	public string ModifierType { get; init; }
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

/// <summary>
/// Repères (data/world/waymarks.json), contrôlés en entier (plan 26 Q7c) : chaque type est un petit lieu ou un lieu
/// connu, porte un nom traduit et un seul gain (une statistique du joueur avec un modificateur admis, des relances ou
/// des bannissements).
/// </summary>
public static class WaymarkDataLoader
{
	private const string ConfigPath = "res://data/world/waymarks.json";

	/// <summary>Lieux qui ne sont pas des petits lieux et que Waymarks marque : coffre, Mémorial, Faille, Atelier.</summary>
	private static readonly string[] LandmarkTypes = { "chest", "memorial", "rift", "workshop" };

	private static WaymarkConfig _config;
	private static string _loadError;

	public static WaymarkConfig Load()
	{
		if (_config != null)
			return _config;
		_config = new WaymarkConfig();
		List<string> types = new(SmallPlaceDataLoader.PlaceIds());
		types.AddRange(LandmarkTypes);
		string contract = FileAccess.FileExists(ObjectDataValidator.ContractPath) ? FileAccess.GetFileAsString(ObjectDataValidator.ContractPath) : null;
		string error = contract == null || !ObjectDataValidator.TryParseContract(contract, out ObjectDataValidator.Contract objects, out _)
			? "contrat des objets illisible"
			: FileAccess.FileExists(ConfigPath)
				? Apply(FileAccess.GetFileAsString(ConfigPath), types, objects, key => TranslationServer.Translate(key) != key)
				: "absent";
		if (error != null)
		{
			_loadError = $"{ConfigPath} : {error}";
			GD.PushError($"[WaymarkDataLoader] {_loadError}");
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

	/// <summary>Contrôle un texte de Repères et ne le publie que s'il est entièrement valide. Rend l'erreur, ou null.</summary>
	public static string Apply(string json, IReadOnlyCollection<string> types, ObjectDataValidator.Contract objects,
		Func<string, bool> translated)
	{
		try
		{
			using JsonDocument document = JsonDocument.Parse(json);
			JsonConfigReader reader = new(document.RootElement);
			JsonElement root = reader.Root;
			reader.AllowOnly(root, "Repères", "enabled", "types");
			Dictionary<string, WaymarkReward> rewards = new();
			foreach (JsonElement entry in reader.List(root, "types", 1))
			{
				if (entry.ValueKind != JsonValueKind.Object)
					return "chaque type doit être un objet";
				JsonConfigReader item = new(entry);
				string id = item.OneOf(entry, "id", types);
				item.AllowOnly(entry, "clés", "id", "name_key", "stat", "amount", "modifier_type", "rerolls", "banishes");
				bool stat = entry.TryGetProperty("stat", out _);
				bool rerolls = entry.TryGetProperty("rerolls", out _);
				bool banishes = entry.TryGetProperty("banishes", out _);
				if (item.Error == null && (stat ? 1 : 0) + (rerolls ? 1 : 0) + (banishes ? 1 : 0) != 1)
					item.Fail("un seul gain attendu : stat, rerolls ou banishes");
				WaymarkReward reward = new()
				{
					NameKey = item.Text(entry, "name_key"),
					Stat = stat ? item.Text(entry, "stat") : null,
					Amount = stat ? item.Positive(entry, "amount") : 0f,
					ModifierType = stat ? item.Text(entry, "modifier_type") : "additive",
					Rerolls = rerolls ? item.Count(entry, "rerolls", 10) : 0,
					Banishes = banishes ? item.Count(entry, "banishes", 10) : 0,
				};
				if (item.Error == null && stat && !objects.PlayerStats.Contains(reward.Stat))
					item.Fail($"stat : « {reward.Stat} » n'est pas une statistique du joueur");
				if (item.Error == null && stat && !objects.Stats[reward.Stat].Modifiers.Contains(reward.ModifierType))
					item.Fail($"modifier_type : « {reward.ModifierType} » non admis pour {reward.Stat}");
				if (item.Error == null && !stat && (entry.TryGetProperty("amount", out _) || entry.TryGetProperty("modifier_type", out _)))
					item.Fail("amount et modifier_type ne vont qu'avec stat");
				if (item.Error == null && !translated(reward.NameKey))
					item.Fail($"name_key : clé « {reward.NameKey} » absente des traductions");
				if (item.Error == null && !rewards.TryAdd(id, reward))
					item.Fail("type en double");
				if (item.Error != null)
					return $"Repère {id ?? "?"} : {item.Error}";
			}
			if (reader.Error == null && !root.TryGetProperty("enabled", out _))
				reader.Fail("enabled absent");
			bool enabled = reader.Flag(root, "enabled");
			if (reader.Error != null)
				return reader.Error;
			_config = new WaymarkConfig { Enabled = enabled, Rewards = rewards };
			_loadError = null;
			return null;
		}
		catch (JsonException ex)
		{
			return $"JSON illisible : {ex.Message}";
		}
	}
}
