using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Contrôle d'une arme de <c>weapons.json</c> contre le contrat (plan 26 Q6a), avant toute conversion : clés, bornes,
/// réglages requis, références de son, d'effet et d'image. Le premier écart rencontré écarte l'arme, avec un message
/// qui nomme l'arme, la voie et le champ.
/// Le contrôle lit l'arme en <c>System.Text.Json</c> : parcourir les dictionnaires Godot créerait des centaines
/// d'enveloppes natives laissées au ramasse-miettes, assez pour faire fuir d'autres objets à la fermeture du moteur.
/// </summary>
public static class WeaponDataValidator
{
	private static readonly HashSet<string> WeaponKeys = new()
	{
		"id", "name", "description", "sprite", "tier", "type", "damage_type", "attack_pattern",
		"trigger_coefficient", "fx", "summary", "growth", "ascensions", "stats", "source", "lore_flavor", "attack_audio",
		"count_name_key", "special_effect", "default_for", "on_hit_effect",
	};

	private static readonly HashSet<string> AscensionKeys = new()
	{
		"id", "name", "description", "attack_pattern", "stat_multipliers", "stat_overrides", "on_hit_effect",
		"bonus_projectile_multiplier", "special_overrides", "flags", "params",
	};

	private static readonly HashSet<string> FxKeys = new() { "style", "family", "projectile" };

	/// <summary>Raison du refus de l'arme, ou null si elle respecte le contrat.</summary>
	public static string Validate(Godot.Collections.Dictionary weapon)
	{
		try
		{
			using JsonDocument document = JsonDocument.Parse(Json.Stringify(weapon));
			return Validate(document.RootElement);
		}
		catch (JsonException)
		{
			string id = weapon.ContainsKey("id") ? weapon["id"].AsString() : "?";
			return $"arme {id} : valeur non finie ou illisible";
		}
	}

	public static string Validate(JsonElement weapon)
	{
		string id = Text(weapon, "id") ?? "?";
		string where = $"arme {id}";
		if (!WeaponContract.IsUsable)
			return $"{where} : contrat des armes inutilisable (voir l'erreur de WeaponContract)";
		if (weapon.ValueKind != JsonValueKind.Object)
			return $"{where} : objet attendu";
		return UnknownKey(weapon, WeaponKeys, where)
			?? CheckStats(weapon, where)
			?? CheckGrowth(weapon, where)
			?? CheckOnHit(weapon, where)
			?? CheckSpecial(weapon, where)
			?? CheckAscensions(weapon, id)
			?? CheckReferences(weapon, where);
	}

	private static string CheckStats(JsonElement weapon, string where)
	{
		bool hasStats = TryObject(weapon, "stats", out JsonElement stats);
		if (hasStats)
		{
			foreach (JsonProperty stat in stats.EnumerateObject())
			{
				if (!WeaponContract.TryGetStat(stat.Name, out DataValueRule rule))
					return $"{where}, stats : stat « {stat.Name} » inconnue du contrat";
				string error = CheckNumber(stat.Value, rule, $"{where}, stats.{stat.Name}");
				if (error != null)
					return error;
			}
		}
		foreach (string required in WeaponContract.RequiredStats())
		{
			if (!hasStats || !stats.TryGetProperty(required, out _))
				return $"{where}, stats : {required} obligatoire";
		}
		return null;
	}

	private static string CheckGrowth(JsonElement weapon, string where)
	{
		if (!TryObject(weapon, "growth", out JsonElement growth))
			return null;
		foreach (JsonProperty entry in growth.EnumerateObject())
		{
			if (!WeaponContract.TryGetStat(entry.Name, out _))
				return $"{where}, growth : stat « {entry.Name} » inconnue du contrat";
			if (WeaponUpgradeDataLoader.GetStatConfig(entry.Name) == null)
				return $"{where}, growth : {entry.Name} n'a pas de réglage d'amélioration (weapon_upgrades.json)";
			string error = CheckNumber(entry.Value, DataValueRule.Positive, $"{where}, growth.{entry.Name}");
			if (error != null)
				return error;
		}
		return null;
	}

	private static string CheckOnHit(JsonElement owner, string where)
	{
		if (!TryObject(owner, "on_hit_effect", out JsonElement effect))
			return null;
		string type = Text(effect, "type") ?? "";
		if (!WeaponContract.TryParseOnHit(type, out OnHitEffectKind kind))
			return $"{where}, on_hit_effect : type « {type} » inconnu ({WeaponContract.OnHitKeys})";
		return CheckParameters(effect, WeaponContract.OnHitRules(kind), $"{where}, on_hit_effect {type}", key => key == "type");
	}

	private static string CheckSpecial(JsonElement weapon, string where)
	{
		if (!TryObject(weapon, "special_effect", out JsonElement effect))
			return null;
		string type = Text(effect, "type") ?? "";
		if (!WeaponContract.TryParseSpecial(type, out SpecialEffectKind kind))
			return $"{where}, special_effect : type « {type} » inconnu ({WeaponContract.SpecialKeys})";
		return CheckParameters(effect, WeaponContract.SpecialRules(kind), $"{where}, special_effect {type}",
			key => key == "type" || WeaponContract.IsAnnotation(key));
	}

	private static string CheckAscensions(JsonElement weapon, string id)
	{
		if (!weapon.TryGetProperty("ascensions", out JsonElement ascensions) || ascensions.ValueKind != JsonValueKind.Array)
			return null;
		SpecialEffectKind? special = null;
		if (TryObject(weapon, "special_effect", out JsonElement effect)
			&& WeaponContract.TryParseSpecial(Text(effect, "type") ?? "", out SpecialEffectKind kind))
			special = kind;

		foreach (JsonElement ascension in ascensions.EnumerateArray())
		{
			string where = $"arme {id}, voie {Text(ascension, "id") ?? "?"}";
			if (ascension.ValueKind != JsonValueKind.Object)
				return $"{where} : objet attendu";
			string error = UnknownKey(ascension, AscensionKeys, where)
				?? CheckStatTable(ascension, "stat_multipliers", where, multiplier: true)
				?? CheckStatTable(ascension, "stat_overrides", where, multiplier: false)
				?? CheckOnHit(ascension, where)
				?? CheckSpecialOverrides(ascension, special, where)
				?? CheckFlags(ascension, where);
			if (error != null)
				return error;
			if (ascension.TryGetProperty("bonus_projectile_multiplier", out JsonElement bonus))
			{
				error = CheckNumber(bonus, DataValueRule.NonNegative, $"{where}, bonus_projectile_multiplier");
				if (error != null)
					return error;
			}
		}
		return null;
	}

	private static string CheckStatTable(JsonElement ascension, string field, string where, bool multiplier)
	{
		if (!TryObject(ascension, field, out JsonElement table))
			return null;
		foreach (JsonProperty entry in table.EnumerateObject())
		{
			if (!WeaponContract.TryGetStat(entry.Name, out DataValueRule rule))
				return $"{where}, {field} : stat « {entry.Name} » inconnue du contrat";
			// Un multiplicateur est un facteur positif ; une valeur fixée suit les bornes de la stat.
			string error = CheckNumber(entry.Value, multiplier ? DataValueRule.Positive : rule, $"{where}, {field}.{entry.Name}");
			if (error != null)
				return error;
		}
		return null;
	}

	private static string CheckSpecialOverrides(JsonElement ascension, SpecialEffectKind? special, string where)
	{
		if (!TryObject(ascension, "special_overrides", out JsonElement overrides))
			return null;
		if (special == null)
			return $"{where}, special_overrides : l'arme n'a pas d'effet spécial";
		IReadOnlyDictionary<string, DataValueRule> rules = WeaponContract.SpecialRules(special.Value);
		foreach (JsonProperty entry in overrides.EnumerateObject())
		{
			if (!rules.TryGetValue(entry.Name, out DataValueRule rule))
				return $"{where}, special_overrides : réglage « {entry.Name} » inconnu de l'effet ({string.Join(", ", rules.Keys)})";
			string error = CheckNumber(entry.Value, rule, $"{where}, special_overrides.{entry.Name}");
			if (error != null)
				return error;
		}
		return null;
	}

	private static string CheckFlags(JsonElement ascension, string where)
	{
		Dictionary<string, DataValueRule> allowed = new();
		if (ascension.TryGetProperty("flags", out JsonElement flags) && flags.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement entry in flags.EnumerateArray())
			{
				string flag = entry.ValueKind == JsonValueKind.String ? entry.GetString() : entry.GetRawText();
				if (!WeaponContract.TryGetFlagRules(flag, out IReadOnlyDictionary<string, DataValueRule> rules))
					return $"{where}, flags : drapeau « {flag} » inconnu du contrat";
				foreach ((string name, DataValueRule rule) in rules)
					allowed[name] = rule;
			}
		}
		if (TryObject(ascension, "params", out JsonElement parameters))
			return CheckParameters(parameters, allowed, $"{where}, params", _ => false);
		foreach ((string name, DataValueRule rule) in allowed)
		{
			if (rule.Required)
				return $"{where}, params : réglage {name} obligatoire";
		}
		return null;
	}

	/// <summary>Chaque clé est un réglage connu (ou admise par <paramref name="skip"/>) ; chaque réglage requis est présent.</summary>
	private static string CheckParameters(JsonElement values, IReadOnlyDictionary<string, DataValueRule> rules, string where,
		System.Func<string, bool> skip)
	{
		foreach (JsonProperty entry in values.EnumerateObject())
		{
			if (skip(entry.Name))
				continue;
			if (!rules.TryGetValue(entry.Name, out DataValueRule rule))
				return $"{where} : réglage « {entry.Name} » inconnu ({string.Join(", ", rules.Keys)})";
			string error = CheckNumber(entry.Value, rule, $"{where}.{entry.Name}");
			if (error != null)
				return error;
		}
		foreach ((string name, DataValueRule rule) in rules)
		{
			if (rule.Required && !values.TryGetProperty(name, out _))
				return $"{where} : réglage {name} obligatoire";
		}
		return null;
	}

	private static string CheckReferences(JsonElement weapon, string where)
	{
		string sound = Text(weapon, "attack_audio");
		if (sound != null && !DataKeySets.TopLevelKeys(AudioManager.SoundBankPath).Contains(sound))
			return $"{where}, attack_audio : son « {sound} » absent de la banque";
		string sprite = Text(weapon, "sprite");
		if (sprite != null && !ResourceLoader.Exists("res://" + sprite))
			return $"{where}, sprite : image « {sprite} » introuvable";
		if (!TryObject(weapon, "fx", out JsonElement fx))
			return null;
		string error = UnknownKey(fx, FxKeys, $"{where}, fx");
		if (error != null)
			return error;
		string style = Text(fx, "style");
		if (style != null && !WeaponContract.IsStyle(style))
			return $"{where}, fx.style : style « {style} » inconnu du contrat";
		string family = Text(fx, "family");
		if (family != null && !WeaponContract.IsFamily(family))
			return $"{where}, fx.family : famille « {family} » inconnue du contrat";
		string projectile = Text(fx, "projectile");
		if (projectile != null && !WeaponContract.IsProjectileAlias(projectile)
			&& !DataKeySets.TopLevelKeys(WeaponVisualConfig.ProjectileManifestPath).Contains(projectile))
			return $"{where}, fx.projectile : projectile « {projectile} » absent du manifeste";
		return null;
	}

	private static string CheckNumber(JsonElement value, DataValueRule rule, string where)
	{
		if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out double number))
			return $"{where} : nombre attendu";
		string reason = rule.Reject((float)number);
		return reason == null ? null : $"{where} : {reason}";
	}

	private static string UnknownKey(JsonElement values, HashSet<string> known, string where)
	{
		foreach (JsonProperty entry in values.EnumerateObject())
		{
			if (!known.Contains(entry.Name))
				return $"{where} : champ « {entry.Name} » inconnu";
		}
		return null;
	}

	private static string Text(JsonElement owner, string key) =>
		owner.ValueKind == JsonValueKind.Object && owner.TryGetProperty(key, out JsonElement value) && value.ValueKind == JsonValueKind.String
			? value.GetString()
			: null;

	private static bool TryObject(JsonElement owner, string key, out JsonElement value)
	{
		value = default;
		return owner.ValueKind == JsonValueKind.Object && owner.TryGetProperty(key, out value) && value.ValueKind == JsonValueKind.Object;
	}
}
