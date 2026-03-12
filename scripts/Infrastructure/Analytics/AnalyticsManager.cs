using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Infrastructure.Analytics;

/// <summary>
/// Collecte de métriques joueur pour le balancing.
/// Stocke localement en user://analytics/ sous forme de fichiers JSON.
/// Optionnellement peut pousser vers un endpoint HTTP (configurable).
///
/// Métriques collectées :
/// - Taux de pick des perks (quels perks les joueurs choisissent)
/// - Causes de mort (quel ennemi, quelle nuit, quelle phase)
/// - Distribution des scores
/// - Temps de run moyen
/// - Nuit moyenne atteinte
/// - Structures les plus construites
/// - Événements custom (boss kill, souvenir discovered, etc.)
/// </summary>
public partial class AnalyticsManager : Node
{
	public static AnalyticsManager Instance { get; private set; }

	private const string AnalyticsDir = "user://analytics/";
	private const string SessionFile = "user://analytics/current_session.json";
	private const string AggregateFile = "user://analytics/aggregate.json";

	// Session courante
	private readonly Dictionary<string, int> _perkPicks = new();
	private readonly Dictionary<string, int> _deathCauses = new();
	private readonly Dictionary<string, int> _structuresBuilt = new();
	private readonly List<int> _scores = new();
	private readonly List<int> _nightsReached = new();
	private readonly List<float> _runDurations = new();
	private readonly List<CustomEvent> _events = new();
	private int _totalRuns;

	private EventBus _eventBus;

	public override void _Ready()
	{
		Instance = this;
		EnsureDirectory();
		LoadAggregate();

		_eventBus = GetNode<EventBus>("/root/EventBus");
		_eventBus.PerkChosen += OnPerkChosen;
		_eventBus.EnemyKilled += OnEnemyKilled;
		_eventBus.SouvenirDiscovered += OnSouvenirDiscovered;
	}

	public override void _ExitTree()
	{
		if (_eventBus != null)
		{
			_eventBus.PerkChosen -= OnPerkChosen;
			_eventBus.EnemyKilled -= OnEnemyKilled;
			_eventBus.SouvenirDiscovered -= OnSouvenirDiscovered;
		}

		SaveAggregate();
	}

	/// <summary>Appelé en fin de run pour enregistrer les métriques globales.</summary>
	public void RecordRunEnd(RunRecord record)
	{
		if (record == null)
			return;

		_totalRuns++;
		_scores.Add(record.Score);
		_nightsReached.Add(record.NightsSurvived);
		_runDurations.Add(record.RunDurationSec);

		if (!string.IsNullOrEmpty(record.DeathCause))
			Increment(_deathCauses, record.DeathCause);

		TrackEvent("run_end", new Dictionary<string, string>
		{
			["character"] = record.CharacterId,
			["score"] = record.Score.ToString(),
			["nights"] = record.NightsSurvived.ToString(),
			["death_cause"] = record.DeathCause ?? "unknown",
			["death_night"] = record.DeathNight.ToString(),
			["duration_sec"] = record.RunDurationSec.ToString("F0"),
			["perks"] = string.Join(",", record.PerkIds ?? new List<string>()),
		});

		SaveAggregate();
	}

	/// <summary>Enregistre un événement custom avec métadonnées.</summary>
	public void TrackEvent(string eventName, Dictionary<string, string> data = null)
	{
		_events.Add(new CustomEvent
		{
			Name = eventName,
			Timestamp = Time.GetUnixTimeFromSystem(),
			Data = data ?? new Dictionary<string, string>()
		});

		// Flush périodiquement pour ne pas perdre de données
		if (_events.Count % 50 == 0)
			SaveAggregate();
	}

	/// <summary>Retourne un rapport de métriques pour le debug overlay ou l'export.</summary>
	public AnalyticsReport GetReport()
	{
		float avgScore = _scores.Count > 0 ? Average(_scores) : 0;
		float avgNights = _nightsReached.Count > 0 ? Average(_nightsReached) : 0;
		float avgDuration = _runDurations.Count > 0 ? AverageF(_runDurations) : 0;

		return new AnalyticsReport
		{
			TotalRuns = _totalRuns,
			AverageScore = avgScore,
			AverageNightsReached = avgNights,
			AverageRunDurationSec = avgDuration,
			TopPerks = GetTopN(_perkPicks, 10),
			TopDeathCauses = GetTopN(_deathCauses, 5),
			TopStructures = GetTopN(_structuresBuilt, 5),
			TotalEvents = _events.Count,
		};
	}

	// --- EventBus handlers ---

	private void OnPerkChosen(string perkId)
	{
		Increment(_perkPicks, perkId);
	}

	private void OnEnemyKilled(string enemyId, Vector2 position)
	{
		// On ne track pas chaque kill individuellement (trop de volume),
		// c'est dans RunRecord.TotalKills via RecordRunEnd
	}

	private void OnSouvenirDiscovered(string souvenirId, string souvenirName, string constellationId)
	{
		TrackEvent("souvenir_discovered", new Dictionary<string, string>
		{
			["souvenir_id"] = souvenirId,
			["constellation"] = constellationId,
		});
	}

	// --- Persistence ---

	private void EnsureDirectory()
	{
		DirAccess dir = DirAccess.Open("user://");
		if (dir != null && !dir.DirExists("analytics"))
			dir.MakeDir("analytics");
	}

	private void SaveAggregate()
	{
		var data = new Godot.Collections.Dictionary
		{
			["total_runs"] = _totalRuns,
			["perk_picks"] = ToGodotDict(_perkPicks),
			["death_causes"] = ToGodotDict(_deathCauses),
			["structures_built"] = ToGodotDict(_structuresBuilt),
			["scores"] = ToGodotArray(_scores),
			["nights_reached"] = ToGodotArray(_nightsReached),
			["run_durations"] = ToGodotArrayF(_runDurations),
		};

		string json = Json.Stringify(data, "\t");
		FileAccess file = FileAccess.Open(AggregateFile, FileAccess.ModeFlags.Write);
		if (file != null)
		{
			file.StoreString(json);
			file.Close();
		}
	}

	private void LoadAggregate()
	{
		if (!FileAccess.FileExists(AggregateFile))
			return;

		FileAccess file = FileAccess.Open(AggregateFile, FileAccess.ModeFlags.Read);
		if (file == null)
			return;

		string json = file.GetAsText();
		file.Close();

		Json parser = new();
		if (parser.Parse(json) != Error.Ok)
			return;

		if (parser.Data.VariantType != Variant.Type.Dictionary)
			return;

		var data = parser.Data.AsGodotDictionary();
		_totalRuns = data.TryGetValue("total_runs", out Variant tr) ? tr.AsInt32() : 0;
		LoadDict(_perkPicks, data, "perk_picks");
		LoadDict(_deathCauses, data, "death_causes");
		LoadDict(_structuresBuilt, data, "structures_built");
		LoadList(_scores, data, "scores");
		LoadList(_nightsReached, data, "nights_reached");
		LoadListF(_runDurations, data, "run_durations");

		GD.Print($"[Analytics] Loaded aggregate: {_totalRuns} runs");
	}

	// --- Helpers ---

	private static void Increment(Dictionary<string, int> dict, string key)
	{
		dict[key] = dict.TryGetValue(key, out int v) ? v + 1 : 1;
	}

	private static float Average(List<int> list)
	{
		long sum = 0;
		foreach (int v in list) sum += v;
		return (float)sum / list.Count;
	}

	private static float AverageF(List<float> list)
	{
		double sum = 0;
		foreach (float v in list) sum += v;
		return (float)(sum / list.Count);
	}

	private static List<KeyValuePair<string, int>> GetTopN(Dictionary<string, int> dict, int n)
	{
		var sorted = new List<KeyValuePair<string, int>>(dict);
		sorted.Sort((a, b) => b.Value.CompareTo(a.Value));
		return sorted.GetRange(0, System.Math.Min(n, sorted.Count));
	}

	private static Godot.Collections.Dictionary ToGodotDict(Dictionary<string, int> dict)
	{
		var gd = new Godot.Collections.Dictionary();
		foreach (var kv in dict) gd[kv.Key] = kv.Value;
		return gd;
	}

	private static Godot.Collections.Array ToGodotArray(List<int> list)
	{
		var arr = new Godot.Collections.Array();
		// Garder les 200 derniers pour limiter la taille
		int start = System.Math.Max(0, list.Count - 200);
		for (int i = start; i < list.Count; i++) arr.Add(list[i]);
		return arr;
	}

	private static Godot.Collections.Array ToGodotArrayF(List<float> list)
	{
		var arr = new Godot.Collections.Array();
		int start = System.Math.Max(0, list.Count - 200);
		for (int i = start; i < list.Count; i++) arr.Add(list[i]);
		return arr;
	}

	private static void LoadDict(Dictionary<string, int> target, Godot.Collections.Dictionary data, string key)
	{
		if (!data.TryGetValue(key, out Variant v) || v.VariantType != Variant.Type.Dictionary)
			return;
		foreach (var kv in v.AsGodotDictionary())
			target[kv.Key.AsString()] = kv.Value.AsInt32();
	}

	private static void LoadList(List<int> target, Godot.Collections.Dictionary data, string key)
	{
		if (!data.TryGetValue(key, out Variant v) || v.VariantType != Variant.Type.Array)
			return;
		foreach (Variant item in v.AsGodotArray())
			target.Add(item.AsInt32());
	}

	private static void LoadListF(List<float> target, Godot.Collections.Dictionary data, string key)
	{
		if (!data.TryGetValue(key, out Variant v) || v.VariantType != Variant.Type.Array)
			return;
		foreach (Variant item in v.AsGodotArray())
			target.Add((float)item.AsDouble());
	}

	public record CustomEvent
	{
		public string Name { get; init; }
		public double Timestamp { get; init; }
		public Dictionary<string, string> Data { get; init; }
	}

	public record AnalyticsReport
	{
		public int TotalRuns { get; init; }
		public float AverageScore { get; init; }
		public float AverageNightsReached { get; init; }
		public float AverageRunDurationSec { get; init; }
		public List<KeyValuePair<string, int>> TopPerks { get; init; }
		public List<KeyValuePair<string, int>> TopDeathCauses { get; init; }
		public List<KeyValuePair<string, int>> TopStructures { get; init; }
		public int TotalEvents { get; init; }
	}
}
