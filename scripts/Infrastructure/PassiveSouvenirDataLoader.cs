using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Étagère d'un objet du catalogue (plan 35) : Écusson ou objet à effet.</summary>
public enum ObjectKind
{
	Badge,
	Item,
}

public class PassiveSouvenirData
{
	public string Id;
	public ObjectKind Kind;
	public string Name;
	/// <summary>Pour un Écusson, l'objet d'où il vient (« Ressort de sommier ») ; vide sinon.</summary>
	public string Subtitle = "";
	/// <summary>Rareté fixe de l’objet ; les Écussons suivent la rareté de leurs cartes.</summary>
	public string Rarity = "common";
	/// <summary>Maximum d’exemplaires ; 0 signifie illimité, la place du sac reste nécessaire.</summary>
	public int MaxCopies;
	/// <summary>Capacité unique explicitement limitée, même sans rareté légendaire.</summary>
	public bool Unique;
	/// <summary>Accès sans quête ; faux garde un nouvel objet verrouillé jusqu’à la livraison de sa quête.</summary>
	public bool InitialUnlock = true;
	public bool HasCopyLimit => MaxCopies > 0;
	public bool IsCopyLimitReached(int copies) => HasCopyLimit && copies >= MaxCopies;

	/// <summary>Écusson : ce que fait le joueur et qui remplit son fil (plan 35 I7) ; vide pour un objet.</summary>
	public string UsageTrigger = "";
	/// <summary>Quantité de <see cref="UsageTrigger"/> qui remplit le fil et donne un niveau.</summary>
	public float UsageThreshold;

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

	public bool IsBadge => Kind == ObjectKind.Badge;

	/// <summary>Type de déblocage qui garde cette fiche dans les quêtes.</summary>
	public UnlockKind UnlockKind => IsBadge ? UnlockKind.Badge : UnlockKind.Object;

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
	public const float MaxRarityGain = 3f;
	private static readonly Dictionary<string, PassiveSouvenirData> _cache = new();
	private static readonly List<PassiveSouvenirData> _all = new();
	private const string CatalogPath = "res://data/progression/passive_souvenirs.json";
	private static bool _loaded;
	private static string _loadError;

	/// <summary>Catalogue lu et contrôlé ; faux, avec la raison, s'il a été refusé (le chargement de la run s'arrête alors).</summary>
	public static bool TryLoad(out string error)
	{
		Load();
		error = _loadError;
		return error == null;
	}

	public static void Load()
	{
		if (_loaded)
			return;
		_loaded = true;

		string jsonText = FileAccess.FileExists(CatalogPath) ? FileAccess.GetFileAsString(CatalogPath) : null;
		string contractText = FileAccess.FileExists(ObjectDataValidator.ContractPath) ? FileAccess.GetFileAsString(ObjectDataValidator.ContractPath) : null;
		// Contrôle complet avant toute publication (plan 26 Q7b) : un catalogue refusé ne publie aucun objet.
		if (jsonText == null)
			_loadError = $"{CatalogPath} absent";
		else if (contractText == null)
			_loadError = $"{ObjectDataValidator.ContractPath} absent";
		else if (!ObjectDataValidator.TryParseContract(contractText, out ObjectDataValidator.Contract contract, out string contractError))
			_loadError = contractError;
		else if (ObjectDataValidator.Validate(jsonText, contract, path => ResourceLoader.Exists(path)) is string invalid)
			_loadError = $"{CatalogPath} : {invalid}";
		if (_loadError != null)
		{
			GD.PushError($"[PassiveSouvenirDataLoader] {_loadError}");
			return;
		}

		Json json = new();
		json.Parse(jsonText);

		Godot.Collections.Array array = json.Data.AsGodotArray();
		List<(PassiveSouvenirData Data, bool Enabled)> parsed = new();
		foreach (Variant item in array)
		{
			Godot.Collections.Dictionary dict = item.AsGodotDictionary();
			PassiveSouvenirData data = ParseEntry(dict);
			// Passif sans effet branché : gardé pour les sauvegardes, retiré des tirages (plan 18).
			parsed.Add((data, !dict.ContainsKey("enabled") || dict["enabled"].AsBool()));
		}
		foreach ((PassiveSouvenirData data, bool enabled) in parsed)
		{
			_cache[data.Id] = data;
			if (enabled)
				_all.Add(data);
		}

		GD.Print($"[PassiveSouvenirDataLoader] Loaded {_cache.Count} passive souvenirs");
	}

	/// <summary>Entrée déjà contrôlée par <see cref="ObjectDataValidator"/> : seuls les champs facultatifs ont un secours.</summary>
	private static PassiveSouvenirData ParseEntry(Godot.Collections.Dictionary dict)
	{
		Godot.Collections.Array color = dict["icon_color"].AsGodotArray();
		PassiveSouvenirData data = new()
		{
			Id = dict["id"].AsString(),
			Kind = dict["kind"].AsString() == "badge" ? ObjectKind.Badge : ObjectKind.Item,
			Name = dict["name"].AsString(),
			Subtitle = dict.ContainsKey("subtitle") ? dict["subtitle"].AsString() : "",
			Description = dict["description"].AsString(),
			Icon = dict.ContainsKey("icon") ? dict["icon"].AsString() : "",
			IconSmall = dict.ContainsKey("icon_small") ? dict["icon_small"].AsString() : "",
			IconColor = new Color((float)color[0].AsDouble(), (float)color[1].AsDouble(), (float)color[2].AsDouble()),
			MaxLevel = (int)dict["max_level"].AsDouble(),
			Survival = dict.ContainsKey("survival") && dict["survival"].AsBool(),
			InitialUnlock = !dict.ContainsKey("initial_unlock") || dict["initial_unlock"].AsBool(),
			OfferWeight = dict.ContainsKey("offer_weight") ? (float)dict["offer_weight"].AsDouble() : 1f,
		};
		foreach (Variant entry in dict["effects"].AsGodotArray())
		{
			Godot.Collections.Dictionary effect = entry.AsGodotDictionary();
			data.Effects.Add(new PassiveEffectData
			{
				Stat = effect["stat"].AsString(),
				ModifierType = effect.ContainsKey("modifier_type") ? effect["modifier_type"].AsString() : "multiplicative",
				Step = (float)effect["step"].AsDouble(),
			});
		}
		if (dict.ContainsKey("params"))
			ReadParameters(dict["params"].AsGodotDictionary(), data.Parameters);
		if (dict.ContainsKey("milestones"))
		{
			foreach (Variant entry in dict["milestones"].AsGodotArray())
			{
				Godot.Collections.Dictionary milestone = entry.AsGodotDictionary();
				Dictionary<string, float> parameters = new();
				if (milestone.ContainsKey("params"))
					ReadParameters(milestone["params"].AsGodotDictionary(), parameters);
				data.Milestones.Add(new ObjectMilestoneData
				{
					Level = (int)milestone["level"].AsDouble(),
					Effect = milestone["effect"].AsString(),
					Text = milestone["text"].AsString(),
					Parameters = parameters,
				});
			}
			data.Milestones.Sort((a, b) => a.Level.CompareTo(b.Level));
		}
		if (dict.ContainsKey("usage"))
		{
			Godot.Collections.Dictionary usage = dict["usage"].AsGodotDictionary();
			data.UsageTrigger = usage["trigger"].AsString();
			data.UsageThreshold = (float)usage["threshold"].AsDouble();
		}
		if (!data.IsBadge)
		{
			data.Rarity = dict["rarity"].AsString();
			data.MaxCopies = dict["max_copies"].AsInt32();
			data.Unique = dict.ContainsKey("unique") && dict["unique"].AsBool();
		}
		data.Stat = data.Effects[0].Stat;
		data.ModifierType = data.Effects[0].ModifierType;
		return data;
	}

	private static void ReadParameters(Godot.Collections.Dictionary values, Dictionary<string, float> target)
	{
		foreach (Variant key in values.Keys)
			target[key.AsString()] = (float)values[key].AsDouble();
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
