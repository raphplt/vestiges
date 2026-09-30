using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class PassiveSouvenirData
{
	public string Id;
	public string Name;
	public string Description;
	public Color IconColor;
	public int MaxLevel;
	/// <summary>Stat et type du premier effet : icône et libellé principal de l'objet.</summary>
	public string Stat;
	public string ModifierType;
	public List<PassiveEffectData> Effects = new();
	/// <summary>Passif de survie (PV, régénération, armure, bouclier) : garanti au tirage tant que le joueur n'en a aucun.</summary>
	public bool Survival;
}

public static class PassiveSouvenirDataLoader
{
	private static readonly Dictionary<string, PassiveSouvenirData> _cache = new();
	private static readonly List<PassiveSouvenirData> _all = new();
	private static bool _loaded;

	public static void Load()
	{
		if (_loaded)
			return;

		FileAccess file = FileAccess.Open("res://data/progression/passive_souvenirs.json", FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PushError("[PassiveSouvenirDataLoader] Cannot open passive_souvenirs.json");
			_loaded = true;
			return;
		}

		string jsonText = file.GetAsText();
		file.Close();

		Json json = new();
		if (json.Parse(jsonText) != Error.Ok)
		{
			GD.PushError($"[PassiveSouvenirDataLoader] Parse error: {json.GetErrorMessage()}");
			_loaded = true;
			return;
		}

		Godot.Collections.Array array = json.Data.AsGodotArray();
		foreach (Variant item in array)
		{
			Godot.Collections.Dictionary dict = item.AsGodotDictionary();
			PassiveSouvenirData data = ParseEntry(dict);
			if (data == null)
				continue;
			_cache[data.Id] = data;
			// Passif sans effet branché : gardé pour les sauvegardes, retiré des tirages (plan 18).
			if (!dict.ContainsKey("enabled") || dict["enabled"].AsBool())
				_all.Add(data);
		}

		_loaded = true;
		GD.Print($"[PassiveSouvenirDataLoader] Loaded {_cache.Count} passive souvenirs");
	}

	private static PassiveSouvenirData ParseEntry(Godot.Collections.Dictionary dict)
	{
		if (!dict.ContainsKey("id"))
			return null;

		PassiveSouvenirData data = new()
		{
			Id = dict["id"].AsString(),
			Name = dict.ContainsKey("name") ? dict["name"].AsString() : dict["id"].AsString(),
			Description = dict.ContainsKey("description") ? dict["description"].AsString() : "",
			MaxLevel = dict.ContainsKey("max_level") ? (int)dict["max_level"].AsDouble() : 50,
			Survival = dict.ContainsKey("survival") && dict["survival"].AsBool()
		};

		if (dict.ContainsKey("icon_color"))
		{
			Godot.Collections.Array colorArr = dict["icon_color"].AsGodotArray();
			data.IconColor = new Color(
				(float)colorArr[0].AsDouble(),
				(float)colorArr[1].AsDouble(),
				(float)colorArr[2].AsDouble()
			);
		}

		if (!dict.ContainsKey("effects"))
		{
			GD.PushError($"[PassiveSouvenirDataLoader] {data.Id} : aucun effet défini");
			return null;
		}
		foreach (Variant entry in dict["effects"].AsGodotArray())
		{
			Godot.Collections.Dictionary effect = entry.AsGodotDictionary();
			data.Effects.Add(new PassiveEffectData
			{
				Stat = effect["stat"].AsString(),
				ModifierType = effect.ContainsKey("modifier_type") ? effect["modifier_type"].AsString() : "multiplicative",
				PerLevel = (float)effect["per_level"].AsDouble(),
			});
		}
		if (data.Effects.Count == 0)
		{
			GD.PushError($"[PassiveSouvenirDataLoader] {data.Id} : liste d'effets vide");
			return null;
		}
		foreach (PassiveEffectData effect in data.Effects)
		{
			// Un facteur nul ou négatif ferait diviser par zéro au passage d'un niveau à l'autre.
			if (effect.Multiplicative && effect.ValueAt(data.MaxLevel) <= 0f)
			{
				GD.PushError($"[PassiveSouvenirDataLoader] {data.Id} : {effect.Stat} s'annule avant le niveau {data.MaxLevel}");
				return null;
			}
		}
		data.Stat = data.Effects[0].Stat;
		data.ModifierType = data.Effects[0].ModifierType;

		return data;
	}

	public static PassiveSouvenirData Get(string id)
	{
		if (!_loaded) Load();
		return _cache.GetValueOrDefault(id);
	}

	public static List<PassiveSouvenirData> GetAll()
	{
		if (!_loaded) Load();
		return new List<PassiveSouvenirData>(_all);
	}
}
