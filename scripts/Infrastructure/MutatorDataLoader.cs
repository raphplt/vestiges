using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class MutatorData
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public float ScoreMultiplier { get; set; } = 1f;
	public int UnlockNights { get; set; }
	public string EffectType { get; set; }
	public float EffectValue { get; set; }
}

public static class MutatorDataLoader
{
	private static readonly List<MutatorData> _allMutators = new();
	private static readonly Dictionary<string, MutatorData> _byId = new();
	private static bool _loaded;

	public static void Load()
	{
		if (_loaded)
			return;

		FileAccess file = FileAccess.Open("res://data/mutators/mutators.json", FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PushError("[MutatorDataLoader] Cannot open mutators.json");
			return;
		}

		string jsonText = file.GetAsText();
		file.Close();

		Json json = new();
		if (json.Parse(jsonText) != Error.Ok)
		{
			GD.PushError($"[MutatorDataLoader] Parse error: {json.GetErrorMessage()}");
			return;
		}

		Godot.Collections.Array array = json.Data.AsGodotArray();
		foreach (Variant item in array)
		{
			Godot.Collections.Dictionary dict = item.AsGodotDictionary();

			MutatorData mutator = new()
			{
				Id = dict["id"].AsString(),
				Name = dict["name"].AsString(),
				Description = dict["description"].AsString(),
				ScoreMultiplier = (float)dict["score_multiplier"].AsDouble(),
				UnlockNights = dict["unlock_nights"].AsInt32(),
				EffectType = dict["effect_type"].AsString(),
				EffectValue = (float)dict["effect_value"].AsDouble()
			};

			_allMutators.Add(mutator);
			_byId[mutator.Id] = mutator;
		}

		_loaded = true;
		GD.Print($"[MutatorDataLoader] Loaded {_allMutators.Count} mutators");
	}

	public static MutatorData Get(string id)
	{
		if (!_loaded)
			Load();

		return _byId.GetValueOrDefault(id);
	}

	public static List<MutatorData> GetAll()
	{
		if (!_loaded)
			Load();

		return _allMutators;
	}
}
