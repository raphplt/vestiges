using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Contrôle d'une fiche de créature contre le contrat (plan 26 Q6c), avant toute conversion : champs, grammaire, stats,
/// visuel, capacités et leurs réglages, sons, familles. Le premier écart écarte la fiche, avec un message qui nomme la
/// créature et le champ. Les créatures citées (renforts) sont rendues à part : <see cref="EnemyDataLoader"/> les
/// contrôle une fois toutes les fiches lues. Lecture en <c>System.Text.Json</c>, sans dictionnaire Godot.
/// </summary>
public static class EnemyDataValidator
{
	private static readonly HashSet<string> EnemyKeys = new()
	{
		"id", "name", "type", "behavior", "tier", "grammatical_gender", "attack_audio", "stats", "visual", "abilities", "pack_family",
	};

	private static readonly HashSet<string> VisualKeys = new()
	{
		"color", "shape", "size", "sprite_folder", "sprite_feet_offset", "projectile",
	};

	private static readonly HashSet<string> ProjectileKeys = new() { "sprite", "family" };

	/// <summary>Raison du refus de la fiche, ou null ; <paramref name="enemyReferences"/> reçoit les créatures qu'elle cite.</summary>
	public static string Validate(JsonElement enemy, List<string> enemyReferences)
	{
		string id = Text(enemy, "id");
		string where = $"créature {id ?? "?"}";
		if (!EnemyContract.IsUsable)
			return $"{where} : contrat des créatures inutilisable (voir l'erreur de EnemyContract)";
		if (enemy.ValueKind != JsonValueKind.Object)
			return $"{where} : objet attendu";
		return UnknownKey(enemy, EnemyKeys, where)
			?? CheckIdentity(enemy, id, where)
			?? CheckStats(enemy, where)
			?? CheckVisual(enemy, where)
			?? CheckAbilities(enemy, where, enemyReferences);
	}

	private static string CheckIdentity(JsonElement enemy, string id, string where)
	{
		if (string.IsNullOrEmpty(id))
			return $"{where} : id obligatoire";
		if (string.IsNullOrEmpty(Text(enemy, "name")))
			return $"{where} : name obligatoire";

		string type = Text(enemy, "type");
		if (type == null)
			return $"{where} : type obligatoire ({EnemyGrammar.CombatTypeKeys})";
		if (!EnemyGrammar.TryParseCombatType(type, out _))
			return $"{where}, type : « {type} » inconnu ({EnemyGrammar.CombatTypeKeys})";

		string error = CheckOptionalText(enemy, "behavior", where);
		if (error != null)
			return error;
		string behavior = Text(enemy, "behavior");
		EnemyBehavior parsedBehavior = EnemyBehavior.Default;
		if (behavior != null && !EnemyGrammar.TryParseBehavior(behavior, out parsedBehavior))
			return $"{where}, behavior : « {behavior} » inconnu ({EnemyGrammar.BehaviorKeys})";

		error = CheckOptionalText(enemy, "tier", where);
		if (error != null)
			return error;
		string tier = Text(enemy, "tier");
		if (tier != null && !EnemyGrammar.TryParseTier(tier, out _))
			return $"{where}, tier : « {tier} » inconnu ({EnemyGrammar.TierKeys})";

		error = CheckOptionalText(enemy, "grammatical_gender", where);
		if (error != null)
			return error;
		string gender = Text(enemy, "grammatical_gender");
		if (gender != null && gender != "f" && gender != "m")
			return $"{where}, grammatical_gender : « {gender} » inconnu (f, m)";

		error = CheckOptionalText(enemy, "attack_audio", where);
		if (error != null)
			return error;
		string sound = Text(enemy, "attack_audio");
		if (sound != null && !IsSound(sound))
			return $"{where}, attack_audio : son « {sound} » absent de la banque";

		error = CheckOptionalText(enemy, "pack_family", where);
		if (error != null)
			return error;
		string packFamily = Text(enemy, "pack_family");
		if (packFamily != null && packFamily.Length == 0)
			return $"{where}, pack_family : famille de meute vide";
		if (parsedBehavior == EnemyBehavior.Pack && packFamily == null)
			return $"{where} : comportement pack sans pack_family";
		return null;
	}

	private static string CheckStats(JsonElement enemy, string where)
	{
		if (!TryObject(enemy, "stats", out JsonElement stats))
			return $"{where} : objet stats obligatoire";
		foreach (JsonProperty stat in stats.EnumerateObject())
		{
			if (!EnemyContract.TryGetStat(stat.Name, out DataValueRule rule))
				return $"{where}, stats : stat « {stat.Name} » inconnue du contrat";
			string error = CheckNumber(stat.Value, rule, $"{where}, stats.{stat.Name}");
			if (error != null)
				return error;
		}
		foreach (string required in EnemyContract.RequiredStats())
		{
			if (!stats.TryGetProperty(required, out _))
				return $"{where}, stats : stat {required} obligatoire";
		}
		return null;
	}

	private static string CheckVisual(JsonElement enemy, string where)
	{
		if (!TryObject(enemy, "visual", out JsonElement visual))
			return $"{where} : objet visual obligatoire";
		where += ", visual";
		string error = UnknownKey(visual, VisualKeys, where);
		if (error != null)
			return error;

		string color = Text(visual, "color");
		if (color == null || !Color.HtmlIsValid(color))
			return $"{where}.color : couleur « {color} » invalide";
		string shape = Text(visual, "shape");
		if (shape == null || !EnemyContract.IsShape(shape))
			return $"{where}.shape : forme « {shape} » inconnue du contrat";
		if (!visual.TryGetProperty("size", out JsonElement size))
			return $"{where}.size : obligatoire";
		error = CheckNumber(size, DataValueRule.Positive, $"{where}.size");
		if (error != null)
			return error;
		error = CheckOptionalText(visual, "sprite_folder", where);
		if (error != null)
			return error;
		if (visual.TryGetProperty("sprite_feet_offset", out JsonElement feet))
		{
			error = CheckNumber(feet, DataValueRule.NonNegative, $"{where}.sprite_feet_offset");
			if (error != null)
				return error;
		}

		if (!visual.TryGetProperty("projectile", out JsonElement projectile))
			return null;
		if (projectile.ValueKind != JsonValueKind.Object)
			return $"{where}.projectile : objet attendu";
		error = UnknownKey(projectile, ProjectileKeys, $"{where}.projectile");
		if (error != null)
			return error;
		string sprite = Text(projectile, "sprite");
		if (sprite != null && !DataKeySets.TopLevelKeys(WeaponVisualConfig.ProjectileManifestPath).Contains(sprite))
			return $"{where}.projectile.sprite : projectile « {sprite} » absent du manifeste";
		string family = Text(projectile, "family");
		if (family != null && !EnemyContract.IsFamily(family))
			return $"{where}.projectile.family : famille « {family} » inconnue du contrat";
		return null;
	}

	private static string CheckAbilities(JsonElement enemy, string where, List<string> enemyReferences)
	{
		if (!enemy.TryGetProperty("abilities", out JsonElement abilities))
			return null;
		if (abilities.ValueKind != JsonValueKind.Object)
			return $"{where}, abilities : objet attendu";

		foreach (JsonProperty ability in abilities.EnumerateObject())
		{
			string at = $"{where}, abilities.{ability.Name}";
			if (!EnemyGrammar.TryParseAbility(ability.Name, out EnemyAbilityKind kind))
				return $"{where}, abilities : capacité « {ability.Name} » inconnue ({EnemyGrammar.AbilityKeys})";
			if (ability.Value.ValueKind != JsonValueKind.Object)
				return $"{at} : objet attendu";
			string error = CheckAbility(ability.Value, EnemyContract.Rules(kind), at, enemyReferences);
			if (error != null)
				return error;
		}
		return null;
	}

	private static string CheckAbility(JsonElement values, EnemyContract.AbilityRules rules, string where, List<string> enemyReferences)
	{
		foreach (JsonProperty entry in values.EnumerateObject())
		{
			string at = $"{where}.{entry.Name}";
			if (rules.Numbers.TryGetValue(entry.Name, out DataValueRule number))
			{
				string error = CheckNumber(entry.Value, number, at);
				if (error != null)
					return error;
			}
			else if (rules.Texts.TryGetValue(entry.Name, out EnemyContract.TextRule text))
			{
				string error = CheckReference(entry.Value, text, at, enemyReferences);
				if (error != null)
					return error;
			}
			else
			{
				return $"{where} : réglage « {entry.Name} » inconnu du contrat";
			}
		}

		foreach ((string name, DataValueRule rule) in rules.Numbers)
		{
			if (rule.Required && !rules.OptionalNumbers.Contains(name) && !values.TryGetProperty(name, out _))
				return $"{where} : réglage {name} obligatoire";
		}
		foreach ((string name, EnemyContract.TextRule rule) in rules.Texts)
		{
			if (rule.Default == null && !values.TryGetProperty(name, out _))
				return $"{where} : réglage {name} obligatoire";
		}
		return null;
	}

	private static string CheckReference(JsonElement value, EnemyContract.TextRule rule, string where, List<string> enemyReferences)
	{
		if (value.ValueKind != JsonValueKind.String)
			return $"{where} : texte attendu";
		string text = value.GetString();
		// Le secours vide signifie « aucun » (pas de son, pas d'éclair) : la donnée peut le dire explicitement.
		if (text.Length == 0 && rule.Default == "")
			return null;
		return rule.Kind switch
		{
			EnemyContract.TextKind.Family when !EnemyContract.IsFamily(text) => $"{where} : famille « {text} » inconnue du contrat",
			EnemyContract.TextKind.Shake when !EnemyContract.IsShake(text) => $"{where} : secousse « {text} » inconnue du contrat",
			EnemyContract.TextKind.Sound when !IsSound(text) => $"{where} : son « {text} » absent de la banque",
			EnemyContract.TextKind.Enemy when text.Length == 0 => $"{where} : créature vide",
			EnemyContract.TextKind.Enemy => AddReference(enemyReferences, text),
			_ => null,
		};
	}

	private static string AddReference(List<string> enemyReferences, string id)
	{
		enemyReferences?.Add(id);
		return null;
	}

	private static bool IsSound(string key) => DataKeySets.TopLevelKeys(AudioManager.SoundBankPath).Contains(key);

	private static string CheckNumber(JsonElement value, DataValueRule rule, string where)
	{
		if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out double number))
			return $"{where} : nombre attendu";
		string reason = rule.Reject((float)number);
		return reason == null ? null : $"{where} : {reason}";
	}

	private static string CheckOptionalText(JsonElement owner, string key, string where)
	{
		if (owner.TryGetProperty(key, out JsonElement value) && value.ValueKind != JsonValueKind.String)
			return $"{where}, {key} : texte attendu";
		return null;
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
