using System;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Réglages de l'Indicible (plan 26 Q6b), lus depuis <c>data/scaling/indicible.json</c> et contrôlés en entier :
/// une configuration invalide est refusée avec un message qui nomme le champ, plutôt que jouée avec un secours.
/// Le rythme, le couloir et les bords changent le combat ; la section <c>decor</c> ne change que l'image.
/// </summary>
public sealed class IndicibleConfig
{
	private const string ConfigPath = "res://data/scaling/indicible.json";
	/// <summary>Tentacules ou yeux : au-delà, la boucle d'attaque s'emballerait.</summary>
	private const int MaxCount = 32;
	private static IndicibleConfig _cached;

	public float EnrageHpRatio { get; private init; }
	public float TentacleInterval { get; private init; }
	public float FirstAttackRatio { get; private init; }
	public float EnragedIntervalRatio { get; private init; }
	public int TentacleCount { get; private init; }
	public int EnragedTentacleCount { get; private init; }
	public float WarningDuration { get; private init; }
	public float TentacleWidth { get; private init; }
	public float TentacleLength { get; private init; }
	public float TargetJitter { get; private init; }
	public float EdgeSpread { get; private init; }
	public float EdgeHalfLength { get; private init; }
	public float EdgeHalfThickness { get; private init; }
	public int EyeCount { get; private init; }
	public float EyeSpread { get; private init; }
	public float EyeOffset { get; private init; }
	public float EyeFirstShift { get; private init; }
	public float EyeShiftInterval { get; private init; }
	public float EyeShiftDuration { get; private init; }
	public float StrikeVisualDuration { get; private init; }

	/// <summary>
	/// Configuration du jeu, lue une fois. Faux, avec la raison, si elle est absente ou invalide : le boss n'apparaît
	/// alors pas (<c>EndgameManager</c> le signale au début de la run) plutôt que de jouer avec des valeurs inventées.
	/// </summary>
	public static bool TryLoad(out IndicibleConfig config, out string error)
	{
		if (_cached != null)
		{
			config = _cached;
			error = null;
			return true;
		}
		using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			config = null;
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

	public static bool TryParse(string json, out IndicibleConfig config, out string error)
	{
		config = null;
		try
		{
			using JsonDocument document = JsonDocument.Parse(json);
			JsonConfigReader reader = new(document.RootElement);
			JsonElement tentacles = reader.Section("tentacles");
			JsonElement edges = reader.Section("edges");
			JsonElement decor = reader.Section("decor");
			IndicibleConfig parsed = new()
			{
				EnrageHpRatio = reader.Ratio(document.RootElement, "enrage_hp_ratio"),
				TentacleInterval = reader.Positive(tentacles, "interval_sec"),
				FirstAttackRatio = reader.Ratio(tentacles, "first_attack_ratio"),
				EnragedIntervalRatio = reader.Ratio(tentacles, "enraged_interval_ratio"),
				TentacleCount = reader.Count(tentacles, "count", MaxCount),
				EnragedTentacleCount = reader.Count(tentacles, "enraged_count", MaxCount),
				WarningDuration = reader.Positive(tentacles, "warning_sec"),
				TentacleWidth = reader.Positive(tentacles, "width"),
				TentacleLength = reader.Positive(tentacles, "length"),
				TargetJitter = reader.NonNegative(tentacles, "target_jitter"),
				EdgeSpread = reader.Positive(edges, "spread"),
				EdgeHalfLength = reader.Positive(edges, "half_length"),
				EdgeHalfThickness = reader.Positive(edges, "half_thickness"),
				EyeCount = reader.Count(decor, "eye_count", MaxCount),
				EyeSpread = reader.Positive(decor, "eye_spread"),
				EyeOffset = reader.NonNegative(decor, "eye_offset"),
				EyeFirstShift = reader.Positive(decor, "eye_first_shift_sec"),
				EyeShiftInterval = reader.Positive(decor, "eye_shift_interval_sec"),
				EyeShiftDuration = reader.Positive(decor, "eye_shift_sec"),
				StrikeVisualDuration = reader.Positive(decor, "strike_visual_sec"),
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
