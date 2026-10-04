using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Contrat des armes (plan 26 Q6a), lu depuis <c>data/weapons/weapon_contract.json</c> : stats, effets, réglages et
/// drapeaux que le combat sait lire, avec bornes et secours. Les types d'effets restent ceux du code ; un contrat qui
/// ne les couvre pas exactement est signalé au chargement.
/// </summary>
public static class WeaponContract
{
	/// <summary>Une valeur admise : bornes, entière ou non, secours (sans secours, elle est obligatoire).</summary>
	public readonly record struct ValueRule(float? Default, float Min, float Max, bool Integer)
	{
		/// <summary>Un facteur ou un poids : toute valeur strictement positive.</summary>
		public static readonly ValueRule Positive = new(null, float.Epsilon, float.MaxValue, false);
		public static readonly ValueRule NonNegative = new(null, 0f, float.MaxValue, false);

		public bool Required => Default == null;

		/// <summary>Raison du refus, ou null si la valeur convient.</summary>
		public string Reject(float value)
		{
			if (!float.IsFinite(value))
				return "valeur non finie";
			if (value < Min || value > Max)
			{
				if (Min == float.Epsilon)
					return $"{value} : strictement positif attendu";
				if (Max == float.MaxValue)
					return $"{value} inférieur au minimum {Min}";
				return $"{value} hors de [{Min} ; {Max}]";
			}
			if (Integer && value != Mathf.Floor(value))
				return $"{value} n'est pas entier";
			return null;
		}
	}

	private const string ContractPath = "res://data/weapons/weapon_contract.json";
	private static readonly string[] Sections = { "stats", "on_hit_effects", "special_effects", "ascension_flags", "special_effect_annotations", "fx" };
	private static readonly Dictionary<string, ValueRule> NoRules = new();

	private static readonly (string Key, OnHitEffectKind Kind)[] OnHitKinds =
	{
		("dot", OnHitEffectKind.Bleed), ("slow", OnHitEffectKind.Slow),
		("disorient", OnHitEffectKind.Disorient), ("freeze", OnHitEffectKind.Freeze),
	};

	private static readonly (string Key, SpecialEffectKind Kind)[] SpecialKinds =
	{
		("heal_every_n_hits", SpecialEffectKind.HealEveryNHits), ("instant_disintegrate", SpecialEffectKind.InstantDisintegrate),
		("delayed_echo", SpecialEffectKind.DelayedEcho), ("ground_fire", SpecialEffectKind.GroundFire),
		("local_time_slow", SpecialEffectKind.LocalTimeSlow), ("random_shape", SpecialEffectKind.RandomShape),
		("sustained_cone", SpecialEffectKind.SustainedCone),
	};

	private static readonly Dictionary<string, ValueRule> _stats = new();
	private static readonly Dictionary<OnHitEffectKind, Dictionary<string, ValueRule>> _onHit = new();
	private static readonly Dictionary<SpecialEffectKind, Dictionary<string, ValueRule>> _special = new();
	private static readonly Dictionary<string, Dictionary<string, ValueRule>> _flags = new();
	private static readonly HashSet<string> _annotations = new();
	private static readonly HashSet<string> _styles = new();
	private static readonly HashSet<string> _projectileAliases = new();
	private static readonly HashSet<string> _families = new();
	private static bool _loaded;
	private static bool _usable;

	/// <summary>Faux si le contrat manque ou est incomplet : aucune arme ne peut alors être validée.</summary>
	public static bool IsUsable
	{
		get
		{
			Load();
			return _usable;
		}
	}

	public static string OnHitKeys => string.Join(", ", OnHitKinds.Select(entry => entry.Key));
	public static string SpecialKeys => string.Join(", ", SpecialKinds.Select(entry => entry.Key));

	public static bool TryGetStat(string key, out ValueRule rule)
	{
		Load();
		return _stats.TryGetValue(key, out rule);
	}

	/// <summary>Valeur d'une stat qu'une arme ne déclare pas ; 0 pour une stat absente du contrat.</summary>
	public static float StatDefault(string key)
	{
		Load();
		return _stats.TryGetValue(key, out ValueRule rule) ? rule.Default ?? 0f : 0f;
	}

	public static IEnumerable<string> RequiredStats()
	{
		Load();
		return _stats.Where(entry => entry.Value.Required).Select(entry => entry.Key);
	}

	public static bool TryParseOnHit(string key, out OnHitEffectKind kind)
	{
		foreach ((string name, OnHitEffectKind value) in OnHitKinds)
		{
			if (name != key)
				continue;
			kind = value;
			return true;
		}
		kind = default;
		return false;
	}

	public static bool TryParseSpecial(string key, out SpecialEffectKind kind)
	{
		foreach ((string name, SpecialEffectKind value) in SpecialKinds)
		{
			if (name != key)
				continue;
			kind = value;
			return true;
		}
		kind = default;
		return false;
	}

	public static IReadOnlyDictionary<string, ValueRule> OnHitRules(OnHitEffectKind kind)
	{
		Load();
		return _onHit.TryGetValue(kind, out Dictionary<string, ValueRule> rules) ? rules : NoRules;
	}

	public static IReadOnlyDictionary<string, ValueRule> SpecialRules(SpecialEffectKind kind)
	{
		Load();
		return _special.TryGetValue(kind, out Dictionary<string, ValueRule> rules) ? rules : NoRules;
	}

	/// <summary>Réglages d'un drapeau de voie ; faux pour un drapeau inconnu.</summary>
	public static bool TryGetFlagRules(string flag, out IReadOnlyDictionary<string, ValueRule> rules)
	{
		Load();
		bool known = _flags.TryGetValue(flag, out Dictionary<string, ValueRule> found);
		rules = found;
		return known;
	}

	public static bool IsAnnotation(string key)
	{
		Load();
		return _annotations.Contains(key);
	}

	public static bool IsStyle(string style)
	{
		Load();
		return _styles.Contains(style);
	}

	/// <summary>Famille de couleurs d'effet qu'une arme peut déclarer (<c>fx.family</c>).</summary>
	public static bool IsFamily(string family)
	{
		Load();
		return _families.Contains(family);
	}

	public static bool IsProjectileAlias(string projectile)
	{
		Load();
		return _projectileAliases.Contains(projectile);
	}

	private static void Load()
	{
		if (_loaded)
			return;
		_loaded = true;

		using FileAccess file = FileAccess.Open(ContractPath, FileAccess.ModeFlags.Read);
		using Json json = new();
		if (file == null || json.Parse(file.GetAsText()) != Error.Ok)
		{
			GD.PushError($"[WeaponContract] {ContractPath} illisible");
			return;
		}

		Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();
		foreach (string section in Sections)
		{
			if (!root.ContainsKey(section))
			{
				GD.PushError($"[WeaponContract] {ContractPath} : section {section} absente, aucune arme ne sera chargée");
				return;
			}
		}
		_usable = true;
		ReadRules(root["stats"].AsGodotDictionary(), _stats);

		foreach ((Variant key, Variant value) in root["on_hit_effects"].AsGodotDictionary())
		{
			if (TryParseOnHit(key.AsString(), out OnHitEffectKind kind))
				ReadRules(value.AsGodotDictionary(), _onHit[kind] = new Dictionary<string, ValueRule>());
			else
				GD.PushError($"[WeaponContract] effet à l'impact « {key} » inconnu du code ({OnHitKeys})");
		}
		foreach ((string key, OnHitEffectKind kind) in OnHitKinds)
		{
			if (!_onHit.ContainsKey(kind))
				GD.PushError($"[WeaponContract] effet à l'impact « {key} » absent du contrat");
		}

		foreach ((Variant key, Variant value) in root["special_effects"].AsGodotDictionary())
		{
			if (TryParseSpecial(key.AsString(), out SpecialEffectKind kind))
				ReadRules(value.AsGodotDictionary(), _special[kind] = new Dictionary<string, ValueRule>());
			else
				GD.PushError($"[WeaponContract] effet spécial « {key} » inconnu du code ({SpecialKeys})");
		}
		foreach ((string key, SpecialEffectKind kind) in SpecialKinds)
		{
			if (!_special.ContainsKey(kind))
				GD.PushError($"[WeaponContract] effet spécial « {key} » absent du contrat");
		}

		foreach ((Variant key, Variant value) in root["ascension_flags"].AsGodotDictionary())
			ReadRules(value.AsGodotDictionary(), _flags[key.AsString()] = new Dictionary<string, ValueRule>());
		foreach (Variant key in root["special_effect_annotations"].AsGodotArray())
			_annotations.Add(key.AsString());
		Godot.Collections.Dictionary fx = root["fx"].AsGodotDictionary();
		foreach (Variant style in fx["styles"].AsGodotArray())
			_styles.Add(style.AsString());
		foreach (Variant alias in fx["projectile_aliases"].AsGodotArray())
			_projectileAliases.Add(alias.AsString());
		foreach (Variant family in fx["families"].AsGodotArray())
			_families.Add(family.AsString());
	}

	private static void ReadRules(Godot.Collections.Dictionary entries, Dictionary<string, ValueRule> target)
	{
		foreach ((Variant key, Variant value) in entries)
		{
			string name = key.AsString();
			if (name.StartsWith('_'))
				continue;
			Godot.Collections.Dictionary rule = value.AsGodotDictionary();
			target[name] = new ValueRule(
				rule.ContainsKey("default") ? (float)rule["default"].AsDouble() : null,
				rule.ContainsKey("min") ? (float)rule["min"].AsDouble() : float.MinValue,
				rule.ContainsKey("max") ? (float)rule["max"].AsDouble() : float.MaxValue,
				rule.ContainsKey("integer") && rule["integer"].AsBool());
		}
	}
}
