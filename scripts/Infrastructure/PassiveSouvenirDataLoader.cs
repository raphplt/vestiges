using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class PassiveSouvenirData
{
	public string Id;
	public string Name;
	public string Description;
	public string Icon;
	public string IconSmall;
	public Color IconColor;
	public int MaxLevel;
	/// <summary>Stat et type du premier effet : icône et libellé principal de l'objet.</summary>
	public string Stat;
	public string ModifierType;
	public List<PassiveEffectData> Effects = new();
	/// <summary>Réglages propres à l'objet (durée d'une Brûlure, force d'un ralentissement…).</summary>
	public Dictionary<string, float> Parameters = new();
	/// <summary>Paliers de l'objet, par niveau croissant (plan 21 §4).</summary>
	public List<ObjectMilestoneData> Milestones = new();
	/// <summary>Passif de survie (PV, régénération, armure, bouclier) : garanti au tirage tant que le joueur n'en a aucun.</summary>
	public bool Survival;
	/// <summary>Poids de l'objet dans les offres de niveau, 1 par défaut (plan 21 G6a : Papier carbone favorisé).</summary>
	public float OfferWeight = 1f;

	private static readonly System.Text.RegularExpressions.Regex ParameterToken = new(@"\{(\w+)(%?)\}");
	private static readonly System.Globalization.CultureInfo French = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");

	/// <summary>
	/// Règle de l'objet, chiffres compris : {param} affiche un réglage de <see cref="Parameters"/>, {param%} le même en
	/// pourcentage (DECISIONS §53). Le texte suit ainsi les réglages sans être réécrit.
	/// </summary>
	public string RuleText() => ParameterToken.Replace(Description, match =>
	{
		if (!Parameters.TryGetValue(match.Groups[1].Value, out float value))
		{
			GD.PushError($"[PassiveSouvenirData] Paramètre {match.Groups[1].Value} absent de {Id}.");
			return match.Value;
		}
		return match.Groups[2].Value == "%"
			? (value * 100f).ToString("0.#", French) + " %"
			: value.ToString("0.##", French);
	});
}

public static class PassiveSouvenirDataLoader
{
	/// <summary>Effets au plus par objet (tampon du joueur pour appliquer une carte sans allouer).</summary>
	public const int MaxEffects = 8;
	/// <summary>Gain d'une carte légendaire, le plus fort (upgrade_rarities.json) : borne de la vérification des facteurs.</summary>
	private const float MaxRarityGain = 3f;
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
			Icon = dict.ContainsKey("icon") ? dict["icon"].AsString() : "",
			IconSmall = dict.ContainsKey("icon_small") ? dict["icon_small"].AsString() : "",
			MaxLevel = dict.ContainsKey("max_level") ? (int)dict["max_level"].AsDouble() : 30,
			Survival = dict.ContainsKey("survival") && dict["survival"].AsBool(),
			OfferWeight = dict.ContainsKey("offer_weight") ? (float)dict["offer_weight"].AsDouble() : 1f
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
			if (!effect.ContainsKey("step"))
			{
				GD.PushError($"[PassiveSouvenirDataLoader] {data.Id} : effet {effect["stat"]} sans step");
				return null;
			}
			data.Effects.Add(new PassiveEffectData
			{
				Stat = effect["stat"].AsString(),
				ModifierType = effect.ContainsKey("modifier_type") ? effect["modifier_type"].AsString() : "multiplicative",
				Step = (float)effect["step"].AsDouble(),
			});
		}
		if (data.Effects.Count == 0 || data.Effects.Count > MaxEffects)
		{
			GD.PushError($"[PassiveSouvenirDataLoader] {data.Id} : {data.Effects.Count} effets, entre 1 et {MaxEffects} attendus");
			return null;
		}
		foreach (PassiveEffectData effect in data.Effects)
		{
			// Un facteur nul ou négatif ferait diviser par zéro au passage d'un niveau à l'autre : on le vérifie
			// au pire cas, toutes les cartes au gain maximal de rareté.
			if (effect.Multiplicative && 1f + effect.Step * data.MaxLevel * MaxRarityGain <= 0f)
			{
				GD.PushError($"[PassiveSouvenirDataLoader] {data.Id} : {effect.Stat} s'annule avant le niveau {data.MaxLevel}");
				return null;
			}
		}
		if (dict.ContainsKey("params"))
		{
			Godot.Collections.Dictionary values = dict["params"].AsGodotDictionary();
			foreach (Variant key in values.Keys)
				data.Parameters[key.AsString()] = (float)values[key].AsDouble();
		}
		if (dict.ContainsKey("milestones") && !ParseMilestones(data, dict["milestones"].AsGodotArray()))
			return null;
		data.Stat = data.Effects[0].Stat;
		data.ModifierType = data.Effects[0].ModifierType;

		return data;
	}

	private static bool ParseMilestones(PassiveSouvenirData data, Godot.Collections.Array entries)
	{
		foreach (Variant entry in entries)
		{
			Godot.Collections.Dictionary milestone = entry.AsGodotDictionary();
			if (!milestone.ContainsKey("level") || !milestone.ContainsKey("effect") || !milestone.ContainsKey("text"))
			{
				GD.PushError($"[PassiveSouvenirDataLoader] {data.Id} : palier sans niveau, effet ou texte");
				return false;
			}
			Dictionary<string, float> parameters = new();
			if (milestone.ContainsKey("params"))
			{
				Godot.Collections.Dictionary values = milestone["params"].AsGodotDictionary();
				foreach (Variant key in values.Keys)
					parameters[key.AsString()] = (float)values[key].AsDouble();
			}
			int level = (int)milestone["level"].AsDouble();
			if (level < 2 || level > data.MaxLevel)
			{
				GD.PushError($"[PassiveSouvenirDataLoader] {data.Id} : palier au niveau {level}, hors de 2 à {data.MaxLevel}");
				return false;
			}
			data.Milestones.Add(new ObjectMilestoneData
			{
				Level = level,
				Effect = milestone["effect"].AsString(),
				Text = milestone["text"].AsString(),
				Parameters = parameters,
			});
		}
		data.Milestones.Sort((a, b) => a.Level.CompareTo(b.Level));
		return true;
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
