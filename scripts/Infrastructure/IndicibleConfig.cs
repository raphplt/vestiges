using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Réglages de l'Indicible (plan 07 B3), lus depuis <c>data/scaling/indicible.json</c> et contrôlés en entier comme au
/// plan 26 Q6b : une configuration invalide est refusée avec le nom du champ, et le boss n'apparaît pas.
/// </summary>
public sealed class IndicibleConfig
{
	private const string ConfigPath = "res://data/scaling/indicible.json";
	/// <summary>Mains à la fois : au-delà, l'écran ne se lit plus et la réserve fondrait sous les tirs de zone.</summary>
	private const int MaxHands = 12;
	private static IndicibleConfig _cached;

	public string NameKey { get; private init; }
	public float FirstAttackSec { get; private init; }
	public IReadOnlyList<float> PhaseThresholds { get; private init; }
	public Color DarkColor { get; private init; }
	public float DarkFadeSec { get; private init; }
	public int HandMaxAlive { get; private init; }
	public int HandExtraPerPhase { get; private init; }
	public float HandInterval { get; private init; }
	public float HandDistanceMin { get; private init; }
	public float HandDistanceMax { get; private init; }
	public float HandRiseSec { get; private init; }
	public float HandLifeSec { get; private init; }
	public float GrabWarningSec { get; private init; }
	public float GrabRadius { get; private init; }
	public float GrabHitMargin { get; private init; }
	public float GrabDamageMultiplier { get; private init; }
	public float HandBodyRadius { get; private init; }
	public float WindSpeed { get; private init; }
	public float WindChangeMinSec { get; private init; }
	public float WindChangeMaxSec { get; private init; }
	public float LightningInterval { get; private init; }
	public float LightningWarning { get; private init; }
	public float LightningRadius { get; private init; }
	public float LightningHitMargin { get; private init; }
	public float LightningDamageMultiplier { get; private init; }
	public float TideBandSec { get; private init; }
	public float TideBandStep { get; private init; }
	public float TideWarningSec { get; private init; }
	public float TideStartDistance { get; private init; }
	public float TideEndDistance { get; private init; }
	public float TideTurnPauseSec { get; private init; }
	public float TideShallowWidth { get; private init; }
	public float TideShallowSlow { get; private init; }
	public float TideDeepSlow { get; private init; }
	public float TideDeepDamageMultiplier { get; private init; }
	public int TideExtraHands { get; private init; }
	public Color TideShallowColor { get; private init; }
	public Color TideDeepColor { get; private init; }
	public float TideOpacity { get; private init; }
	public float WaveInterval { get; private init; }
	public float WaveWarningSec { get; private init; }
	public float WaveSpeed { get; private init; }
	public float WaveHalfLength { get; private init; }
	public int WaveBreachMin { get; private init; }
	public int WaveBreachMax { get; private init; }
	public float WaveBreachWidth { get; private init; }
	public float WaveBreachSpread { get; private init; }
	public float WaveDamageMultiplier { get; private init; }
	public float WindowSec { get; private init; }
	public float WindowDistance { get; private init; }
	public float WindowRadius { get; private init; }
	public float WindowBonus { get; private init; }
	public Color WaveFoamColor { get; private init; }
	public Color WaveBreachColor { get; private init; }
	public string WaveWarningAudio { get; private init; }
	public string WaveCrashAudio { get; private init; }
	public string WindowOpenAudio { get; private init; }
	public string FxFamily { get; private init; }
	public string RiseAudio { get; private init; }
	public string HandRiseAudio { get; private init; }
	public string GrabWarningAudio { get; private init; }
	public string GrabImpactAudio { get; private init; }
	public string LightningWarningAudio { get; private init; }
	public string LightningStrikeAudio { get; private init; }
	public string DefeatedAudio { get; private init; }

	/// <summary>
	/// Configuration du jeu, lue une fois, avec les réglages communs des parties. Faux, avec la raison, si elle est
	/// absente ou invalide : le boss n'apparaît alors pas (<c>EndgameManager</c> le signale au début de la run).
	/// </summary>
	public static bool TryLoad(out IndicibleConfig config, out string error)
	{
		if (_cached != null)
		{
			config = _cached;
			error = null;
			return true;
		}
		config = null;
		if (!BossPartsConfig.TryLoad(out BossPartsConfig parts, out error))
			return false;
		using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			error = $"{ConfigPath} absent";
			return false;
		}
		if (!TryParse(file.GetAsText(), parts.MaxBodyRadius, out config, out string parseError))
		{
			error = $"{ConfigPath} : {parseError}";
			return false;
		}
		_cached = config;
		error = null;
		return true;
	}

	public static bool TryParse(string json, float maxBodyRadius, out IndicibleConfig config, out string error)
	{
		config = null;
		try
		{
			using JsonDocument document = JsonDocument.Parse(json);
			JsonElement root = document.RootElement;
			JsonConfigReader reader = new(root);
			reader.AllowOnly(root, "racine", "name_key", "first_attack_sec", "phase_thresholds", "darkness", "hands", "storm", "tide", "wave", "fx_family", "audio");
			JsonElement darkness = reader.Section("darkness");
			reader.AllowOnly(darkness, "darkness", "color", "fade_sec");
			JsonElement hands = reader.Section("hands");
			reader.AllowOnly(hands, "hands", "max_alive", "extra_per_phase", "interval_sec", "distance_min", "distance_max", "rise_sec",
				"life_sec", "grab_warning_sec", "grab_radius", "hit_margin", "damage_multiplier", "body_radius");
			JsonElement storm = reader.Section("storm");
			reader.AllowOnly(storm, "storm", "wind_speed", "wind_change_min_sec", "wind_change_max_sec", "lightning_interval_sec",
				"lightning_warning_sec", "lightning_radius", "lightning_hit_margin", "lightning_damage_multiplier");
			JsonElement tide = reader.Section("tide");
			reader.AllowOnly(tide, "tide", "band_sec", "band_step", "warning_sec", "start_distance", "end_distance", "turn_pause_sec",
				"shallow_width", "shallow_slow", "deep_slow", "deep_damage_multiplier", "extra_hands", "shallow_color", "deep_color", "opacity");
			JsonElement wave = reader.Section("wave");
			reader.AllowOnly(wave, "wave", "interval_sec", "warning_sec", "speed", "half_length", "breach_min", "breach_max", "breach_width",
				"breach_spread", "damage_multiplier", "window_sec", "window_distance", "window_radius", "window_bonus", "foam_color", "breach_color");
			JsonElement audio = reader.Section("audio");
			reader.AllowOnly(audio, "audio", "rise", "hand_rise", "grab_warning", "grab_impact", "lightning_warning", "lightning_strike", "defeated",
				"wave_warning", "wave_crash", "window_open");

			List<float> thresholds = new();
			foreach (JsonElement item in reader.List(root, "phase_thresholds", 1))
			{
				if (item.ValueKind != JsonValueKind.Number || item.GetSingle() <= 0f || item.GetSingle() >= 1f
					|| thresholds.Count > 0 && item.GetSingle() >= thresholds[^1])
				{
					reader.Fail("phase_thresholds : parts décroissantes dans ]0 ; 1[ attendues");
					break;
				}
				thresholds.Add(item.GetSingle());
			}

			IndicibleConfig parsed = new()
			{
				NameKey = reader.Text(root, "name_key"),
				FirstAttackSec = reader.NonNegative(root, "first_attack_sec"),
				PhaseThresholds = thresholds,
				DarkColor = reader.Rgb(darkness, "color"),
				DarkFadeSec = reader.Positive(darkness, "fade_sec"),
				HandMaxAlive = reader.Count(hands, "max_alive", MaxHands),
				HandExtraPerPhase = reader.Integer(hands, "extra_per_phase", 0, MaxHands),
				HandInterval = reader.Positive(hands, "interval_sec"),
				HandDistanceMin = reader.Positive(hands, "distance_min"),
				HandDistanceMax = reader.Positive(hands, "distance_max"),
				HandRiseSec = reader.Positive(hands, "rise_sec"),
				HandLifeSec = reader.Positive(hands, "life_sec"),
				GrabWarningSec = reader.Positive(hands, "grab_warning_sec"),
				GrabRadius = reader.Positive(hands, "grab_radius"),
				GrabHitMargin = reader.NonNegative(hands, "hit_margin"),
				GrabDamageMultiplier = reader.NonNegative(hands, "damage_multiplier"),
				HandBodyRadius = reader.Positive(hands, "body_radius"),
				WindSpeed = reader.NonNegative(storm, "wind_speed"),
				WindChangeMinSec = reader.Positive(storm, "wind_change_min_sec"),
				WindChangeMaxSec = reader.Positive(storm, "wind_change_max_sec"),
				LightningInterval = reader.Positive(storm, "lightning_interval_sec"),
				LightningWarning = reader.Positive(storm, "lightning_warning_sec"),
				LightningRadius = reader.Positive(storm, "lightning_radius"),
				LightningHitMargin = reader.NonNegative(storm, "lightning_hit_margin"),
				LightningDamageMultiplier = reader.NonNegative(storm, "lightning_damage_multiplier"),
				TideBandSec = reader.Positive(tide, "band_sec"),
				TideBandStep = reader.Positive(tide, "band_step"),
				TideWarningSec = reader.Positive(tide, "warning_sec"),
				TideStartDistance = reader.Positive(tide, "start_distance"),
				TideEndDistance = reader.Number(tide, "end_distance"),
				TideTurnPauseSec = reader.NonNegative(tide, "turn_pause_sec"),
				TideShallowWidth = reader.NonNegative(tide, "shallow_width"),
				TideShallowSlow = reader.Ratio(tide, "shallow_slow"),
				TideDeepSlow = reader.Ratio(tide, "deep_slow"),
				TideDeepDamageMultiplier = reader.NonNegative(tide, "deep_damage_multiplier"),
				TideExtraHands = reader.Integer(tide, "extra_hands", 0, MaxHands),
				TideShallowColor = reader.Rgb(tide, "shallow_color"),
				TideDeepColor = reader.Rgb(tide, "deep_color"),
				TideOpacity = reader.Ratio(tide, "opacity"),
				WaveInterval = reader.Positive(wave, "interval_sec"),
				WaveWarningSec = reader.Positive(wave, "warning_sec"),
				WaveSpeed = reader.Positive(wave, "speed"),
				WaveHalfLength = reader.Positive(wave, "half_length"),
				WaveBreachMin = reader.Count(wave, "breach_min", 4),
				WaveBreachMax = reader.Count(wave, "breach_max", 4),
				WaveBreachWidth = reader.Positive(wave, "breach_width"),
				WaveBreachSpread = reader.NonNegative(wave, "breach_spread"),
				WaveDamageMultiplier = reader.NonNegative(wave, "damage_multiplier"),
				WindowSec = reader.Positive(wave, "window_sec"),
				WindowDistance = reader.Positive(wave, "window_distance"),
				WindowRadius = reader.Positive(wave, "window_radius"),
				WindowBonus = reader.NonNegative(wave, "window_bonus"),
				WaveFoamColor = reader.Rgb(wave, "foam_color"),
				WaveBreachColor = reader.Rgb(wave, "breach_color"),
				WaveWarningAudio = reader.Text(audio, "wave_warning"),
				WaveCrashAudio = reader.Text(audio, "wave_crash"),
				WindowOpenAudio = reader.Text(audio, "window_open"),
				FxFamily = reader.Text(root, "fx_family"),
				RiseAudio = reader.Text(audio, "rise"),
				HandRiseAudio = reader.Text(audio, "hand_rise"),
				GrabWarningAudio = reader.Text(audio, "grab_warning"),
				GrabImpactAudio = reader.Text(audio, "grab_impact"),
				LightningWarningAudio = reader.Text(audio, "lightning_warning"),
				LightningStrikeAudio = reader.Text(audio, "lightning_strike"),
				DefeatedAudio = reader.Text(audio, "defeated"),
			};
			if (reader.Error == null && parsed.HandDistanceMax < parsed.HandDistanceMin)
				reader.Fail($"hands.distance_max ({parsed.HandDistanceMax}) inférieur à distance_min ({parsed.HandDistanceMin})");
			if (reader.Error == null && parsed.TideEndDistance >= parsed.TideStartDistance)
				reader.Fail("tide.end_distance doit être inférieur à start_distance : l'eau avance vers le joueur");
			if (reader.Error == null && parsed.WindChangeMaxSec < parsed.WindChangeMinSec)
				reader.Fail($"storm.wind_change_max_sec inférieur à wind_change_min_sec");
			if (reader.Error == null && parsed.WaveBreachMax < parsed.WaveBreachMin)
				reader.Fail("wave.breach_max inférieur à breach_min");
			if (reader.Error == null && parsed.WindowRadius > maxBodyRadius)
				reader.Fail($"wave.window_radius : {parsed.WindowRadius} au-delà du rayon de touche permis ({maxBodyRadius})");
			if (reader.Error == null && parsed.HandBodyRadius > maxBodyRadius)
				reader.Fail($"hands.body_radius : {parsed.HandBodyRadius} au-delà du rayon de touche permis ({maxBodyRadius})");
			if (reader.Error == null && !EnemyContract.IsFamily(parsed.FxFamily))
				reader.Fail($"fx_family : famille « {parsed.FxFamily} » inconnue");
			foreach (string sound in new[] { parsed.RiseAudio, parsed.HandRiseAudio, parsed.GrabWarningAudio, parsed.GrabImpactAudio,
				parsed.LightningWarningAudio, parsed.LightningStrikeAudio, parsed.DefeatedAudio, parsed.WaveWarningAudio, parsed.WaveCrashAudio,
				parsed.WindowOpenAudio })
				if (reader.Error == null && !DataKeySets.TopLevelKeys(AudioManager.SoundBankPath).Contains(sound))
					reader.Fail($"audio : son « {sound} » absent de la banque");
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
