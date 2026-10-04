using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Contrat des fiches de créatures (plan 26 Q6c), lu depuis <c>data/enemies/_contract.json</c> : stats, formes,
/// secousses, familles et, pour chaque capacité du code, ses réglages numériques et textuels avec bornes et secours.
/// Un contrat qui ne couvre pas exactement les capacités de <see cref="EnemyGrammar"/> est refusé au chargement.
/// </summary>
public static class EnemyContract
{
	/// <summary>Sorte de référence d'un réglage textuel.</summary>
	public enum TextKind
	{
		Family,
		Sound,
		Enemy,
		Shake,
	}

	/// <summary>Réglage textuel : sa sorte de référence et son secours (null : obligatoire ; vide : aucun).</summary>
	public readonly record struct TextRule(TextKind Kind, string Default);

	/// <summary>Réglages admis par une capacité ; un réglage numérique facultatif n'a pas de secours, le code le calcule.</summary>
	public sealed class AbilityRules
	{
		public Dictionary<string, DataValueRule> Numbers { get; } = new();
		public HashSet<string> OptionalNumbers { get; } = new();
		public Dictionary<string, TextRule> Texts { get; } = new();
	}

	private const string ContractPath = "res://data/enemies/_contract.json";

	/// <summary>Contenu d'un contrat lu ; un contrat candidat se lit dans ses propres tables.</summary>
	private sealed class Tables
	{
		public readonly Dictionary<string, DataValueRule> Stats = new();
		public readonly Dictionary<EnemyAbilityKind, AbilityRules> Abilities = new();
		public readonly HashSet<string> Shapes = new();
		public readonly HashSet<string> Shakes = new();
		public readonly HashSet<string> Families = new();
	}

	private static readonly Tables _tables = new();
	private static bool _loaded;
	private static bool _usable;

	/// <summary>Faux si le contrat manque, est illisible ou incomplet : aucune fiche ne peut alors être validée.</summary>
	public static bool IsUsable
	{
		get
		{
			Load();
			return _usable;
		}
	}

	public static bool TryGetStat(string key, out DataValueRule rule)
	{
		Load();
		return _tables.Stats.TryGetValue(key, out rule);
	}

	public static IEnumerable<string> RequiredStats()
	{
		Load();
		return _tables.Stats.Where(entry => entry.Value.Required).Select(entry => entry.Key);
	}

	/// <summary>Valeur d'une stat qu'une fiche ne déclare pas ; 0 pour une stat absente du contrat.</summary>
	public static float StatDefault(string key)
	{
		Load();
		return _tables.Stats.TryGetValue(key, out DataValueRule rule) ? rule.Default ?? 0f : 0f;
	}

	public static AbilityRules Rules(EnemyAbilityKind kind)
	{
		Load();
		return _tables.Abilities.TryGetValue(kind, out AbilityRules rules) ? rules : null;
	}

	public static bool IsShape(string shape)
	{
		Load();
		return _tables.Shapes.Contains(shape);
	}

	public static bool IsShake(string shake)
	{
		Load();
		return _tables.Shakes.Contains(shake);
	}

	public static bool IsFamily(string family)
	{
		Load();
		return _tables.Families.Contains(family);
	}

	/// <summary>Familles admises, pour les comparer à celles que le rendu sait dessiner.</summary>
	public static IReadOnlyCollection<string> Families
	{
		get
		{
			Load();
			return _tables.Families;
		}
	}

	/// <summary>Raison du refus d'un contrat candidat, ou null ; le contrat chargé n'est pas touché.</summary>
	public static string Check(string jsonText) => Parse(jsonText, new Tables());

	private static void Load()
	{
		if (_loaded)
			return;
		_loaded = true;

		using FileAccess file = FileAccess.Open(ContractPath, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PushError($"[EnemyContract] {ContractPath} introuvable : aucune créature ne sera chargée");
			return;
		}

		string error = Parse(file.GetAsText(), _tables);
		if (error != null)
		{
			GD.PushError($"[EnemyContract] {ContractPath} : {error} ; aucune créature ne sera chargée");
			return;
		}
		_usable = true;
	}

	private static string Parse(string jsonText, Tables tables)
	{
		try
		{
			using JsonDocument document = JsonDocument.Parse(jsonText);
			return Read(document.RootElement, tables);
		}
		catch (JsonException ex)
		{
			return $"illisible : {ex.Message}";
		}
		catch (System.InvalidOperationException ex)
		{
			return $"forme inattendue : {ex.Message}";
		}
		catch (KeyNotFoundException ex)
		{
			return $"forme inattendue : {ex.Message}";
		}
	}

	private static string Read(JsonElement root, Tables tables)
	{
		if (root.ValueKind != JsonValueKind.Object)
			return "objet attendu";
		foreach (string section in new[] { "stats", "shapes", "shakes", "families", "abilities" })
		{
			if (!root.TryGetProperty(section, out _))
				return $"section « {section} » absente";
		}

		ReadRules(root.GetProperty("stats"), tables.Stats);
		ReadSet(root.GetProperty("shapes"), tables.Shapes);
		ReadSet(root.GetProperty("shakes"), tables.Shakes);
		ReadSet(root.GetProperty("families"), tables.Families);

		// La table des clés doit couvrir chaque sorte : sinon une capacité du code n'aurait ni clé ni règles.
		foreach (EnemyAbilityKind kind in System.Enum.GetValues<EnemyAbilityKind>())
		{
			if (!EnemyGrammar.TryParseAbility(EnemyGrammar.Key(kind), out EnemyAbilityKind parsed) || parsed != kind)
				return $"capacité {kind} sans clé dans EnemyGrammar";
		}

		JsonElement abilities = root.GetProperty("abilities");
		foreach (JsonProperty entry in abilities.EnumerateObject())
		{
			if (entry.Name.StartsWith('_'))
				continue;
			if (!EnemyGrammar.TryParseAbility(entry.Name, out _))
				return $"capacité « {entry.Name} » inconnue du code ({EnemyGrammar.AbilityKeys})";
		}
		foreach (string key in EnemyGrammar.AllAbilityKeys)
		{
			if (!abilities.TryGetProperty(key, out JsonElement ability))
				return $"capacité « {key} » du code absente du contrat";
			if (ability.TryGetProperty("same_as", out JsonElement sameAs))
			{
				string source = sameAs.GetString();
				if (!abilities.TryGetProperty(source ?? "", out ability) || ability.TryGetProperty("same_as", out _))
					return $"capacité « {key} » : same_as « {source} » sans règles propres";
			}
			EnemyGrammar.TryParseAbility(key, out EnemyAbilityKind kind);
			string error = ReadAbility(ability, key, out AbilityRules rules);
			if (error != null)
				return error;
			tables.Abilities[kind] = rules;
		}
		return null;
	}

	private static string ReadAbility(JsonElement ability, string key, out AbilityRules rules)
	{
		rules = new AbilityRules();
		if (ability.TryGetProperty("numbers", out JsonElement numbers))
		{
			ReadRules(numbers, rules.Numbers);
			foreach (JsonProperty entry in numbers.EnumerateObject())
			{
				if (entry.Value.ValueKind == JsonValueKind.Object && entry.Value.TryGetProperty("optional", out JsonElement optional) && optional.GetBoolean())
					rules.OptionalNumbers.Add(entry.Name);
			}
		}
		if (!ability.TryGetProperty("texts", out JsonElement texts))
			return null;

		foreach (JsonProperty entry in texts.EnumerateObject())
		{
			if (entry.Name.StartsWith('_'))
				continue;
			string kindKey = entry.Value.TryGetProperty("kind", out JsonElement kindValue) ? kindValue.GetString() : null;
			TextKind? kind = kindKey switch
			{
				"family" => TextKind.Family,
				"sound" => TextKind.Sound,
				"enemy" => TextKind.Enemy,
				"shake" => TextKind.Shake,
				_ => null,
			};
			if (kind == null)
				return $"capacité « {key} », texte « {entry.Name} » : sorte « {kindKey} » inconnue (family, sound, enemy, shake)";
			string fallback = entry.Value.TryGetProperty("default", out JsonElement value) ? value.GetString() : null;
			rules.Texts[entry.Name] = new TextRule(kind.Value, fallback);
		}
		return null;
	}

	private static void ReadRules(JsonElement entries, Dictionary<string, DataValueRule> target)
	{
		foreach (JsonProperty entry in entries.EnumerateObject())
		{
			if (entry.Name.StartsWith('_'))
				continue;
			JsonElement rule = entry.Value;
			target[entry.Name] = new DataValueRule(
				rule.TryGetProperty("default", out JsonElement fallback) ? fallback.GetSingle() : null,
				rule.TryGetProperty("min", out JsonElement min) ? min.GetSingle() : float.MinValue,
				rule.TryGetProperty("max", out JsonElement max) ? max.GetSingle() : float.MaxValue,
				rule.TryGetProperty("integer", out JsonElement integer) && integer.GetBoolean());
		}
	}

	private static void ReadSet(JsonElement values, HashSet<string> target)
	{
		foreach (JsonElement value in values.EnumerateArray())
			target.Add(value.GetString());
	}
}
