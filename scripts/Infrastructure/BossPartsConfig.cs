using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Réglages communs des boss faits de parties (plan 07 B1), lus depuis <c>data/scaling/boss_parts.json</c> et contrôlés
/// en entier comme ceux de l'Indicible (plan 26 Q6b) : une configuration invalide est refusée avec le nom du champ, et
/// le boss qui en dépend n'apparaît pas.
/// </summary>
public sealed class BossPartsConfig
{
	private const string ConfigPath = "res://data/scaling/boss_parts.json";
	private static BossPartsConfig _cached;

	public float MaxBodyRadius { get; private init; }
	public float TargetBias { get; private init; }

	/// <summary>Configuration du jeu, lue une fois, et fiche des parties présente ; faux avec la raison sinon.</summary>
	public static bool TryLoad(out BossPartsConfig config, out string error)
	{
		if (_cached != null)
		{
			config = _cached;
			error = null;
			return true;
		}
		config = null;
		if (!EnemyDataLoader.Exists(EnemyGrammar.BossPartId))
		{
			error = $"fiche data/enemies/{EnemyGrammar.BossPartId}.json absente ou écartée";
			return false;
		}
		using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			error = $"{ConfigPath} absent";
			return false;
		}
		if (!TryParse(file.GetAsText(), out config, out string parseError))
		{
			error = $"{ConfigPath} : {parseError}";
			return false;
		}
		_cached = config;
		error = null;
		return true;
	}

	public static bool TryParse(string json, out BossPartsConfig config, out string error)
	{
		config = null;
		try
		{
			using JsonDocument document = JsonDocument.Parse(json);
			JsonConfigReader reader = new(document.RootElement);
			reader.AllowOnly(document.RootElement, "racine", "parts");
			JsonElement parts = reader.Section("parts");
			reader.AllowOnly(parts, "parts", "max_body_radius", "target_bias");
			BossPartsConfig parsed = new()
			{
				MaxBodyRadius = reader.Positive(parts, "max_body_radius"),
				TargetBias = reader.NonNegative(parts, "target_bias"),
			};
			error = reader.Error;
			config = error == null ? parsed : null;
			return error == null;
		}
		catch (JsonException ex)
		{
			error = $"JSON illisible : {ex.Message}";
			return false;
		}
	}
}
