using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Vestiges.Infrastructure;

/// <summary>
/// Contrôle complet de data/progression/passive_souvenirs.json contre data/progression/objects_contract.json (plan 26
/// Q7b), avant toute publication : la première erreur nomme l'objet et le champ.
/// </summary>
public static class ObjectDataValidator
{
    public const string ContractPath = "res://data/progression/objects_contract.json";
    private const int MaxLevelBound = 100;

    private static readonly string[] ObjectKeys =
    {
        "id", "name", "description", "icon", "icon_small", "icon_color", "max_level", "effects", "milestones",
        "survival", "offer_weight", "params", "enabled",
    };
    private static readonly string[] EffectKeys = { "stat", "modifier_type", "step" };
    private static readonly string[] MilestoneKeys = { "level", "effect", "text", "params" };
    private static readonly Regex ParameterToken = new(@"\{(\w+)%?\}");

    /// <summary>Le contrat lu : statistiques (modificateurs, réglages), statistiques inactives, effets de palier.</summary>
    public sealed class Contract
    {
        public readonly Dictionary<string, (HashSet<string> Modifiers, string[] Params)> Stats = new();
        public readonly HashSet<string> InactiveStats = new();
        public readonly Dictionary<string, string[]> Milestones = new();
    }

    public static bool TryParseContract(string json, out Contract contract, out string error)
    {
        contract = new Contract();
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            foreach (JsonProperty stat in root.GetProperty("stats").EnumerateObject())
            {
                HashSet<string> modifiers = new();
                foreach (JsonElement modifier in stat.Value.GetProperty("modifiers").EnumerateArray())
                    modifiers.Add(modifier.GetString());
                contract.Stats[stat.Name] = (modifiers, Strings(stat.Value.GetProperty("params")));
            }
            foreach (JsonElement stat in root.GetProperty("inactive_stats").EnumerateArray())
                contract.InactiveStats.Add(stat.GetString());
            foreach (JsonProperty milestone in root.GetProperty("milestones").EnumerateObject())
                contract.Milestones[milestone.Name] = Strings(milestone.Value);
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            contract = null;
            error = $"contrat des objets illisible : {ex.Message}";
            return false;
        }
    }

    /// <summary>Première erreur du catalogue, ou null.</summary>
    /// <param name="resourceExists">Une icône doit exister dans le projet.</param>
    public static string Validate(string json, Contract contract, Func<string, bool> resourceExists)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return "tableau d'objets attendu";
            HashSet<string> ids = new();
            int index = 0;
            foreach (JsonElement item in document.RootElement.EnumerateArray())
            {
                string error = ValidateObject(item, index++, contract, resourceExists, ids);
                if (error != null)
                    return error;
            }
            return null;
        }
        catch (JsonException ex)
        {
            return $"JSON illisible : {ex.Message}";
        }
    }

    private static string ValidateObject(JsonElement item, int index, Contract contract, Func<string, bool> resourceExists,
        HashSet<string> ids)
    {
        if (item.ValueKind != JsonValueKind.Object)
            return $"objet n°{index} : objet attendu";
        string id = Text(item, "id");
        if (string.IsNullOrEmpty(id))
            return $"objet n°{index} : id absent";
        string where = $"objet {id}";
        if (!ids.Add(id))
            return $"{where} : identifiant en double";
        JsonConfigReader reader = new(item);
        reader.AllowOnly(item, where, ObjectKeys);
        if (reader.Error != null)
            return reader.Error;
        bool enabled = !item.TryGetProperty("enabled", out JsonElement enabledValue) || enabledValue.ValueKind == JsonValueKind.True;
        if (string.IsNullOrEmpty(Text(item, "name")))
            return $"{where} : name absent ou vide";
        // Un objet désactivé, gardé pour les sauvegardes, n'est jamais montré : il peut rester sans description.
        if (Text(item, "description") == null || (enabled && Text(item, "description").Length == 0))
            return $"{where} : description absente{(enabled ? " ou vide" : "")}";
        foreach (string field in new[] { "icon", "icon_small" })
        {
            if (item.TryGetProperty(field, out JsonElement icon)
                && (icon.ValueKind != JsonValueKind.String || !resourceExists(icon.GetString())))
                return $"{where}, {field} : image « {icon} » introuvable";
        }
        if (!item.TryGetProperty("icon_color", out JsonElement color) || color.ValueKind != JsonValueKind.Array
            || color.GetArrayLength() != 3 || !AllNumbersIn(color, 0.0, 1.0))
            return $"{where}, icon_color : trois composantes de 0 à 1 attendues";
        foreach (string flag in new[] { "enabled", "survival" })
        {
            if (item.TryGetProperty(flag, out JsonElement value) && value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return $"{where}, {flag} : booléen attendu";
        }
        int maxLevel = reader.Integer(item, "max_level", 1, MaxLevelBound);
        if (item.TryGetProperty("offer_weight", out _))
            reader.Positive(item, "offer_weight");
        if (reader.Error != null)
            return $"{where}, {reader.Error}";

        HashSet<string> parameters = new();
        if (item.TryGetProperty("params", out JsonElement parameterTable))
        {
            if (parameterTable.ValueKind != JsonValueKind.Object)
                return $"{where}, params : objet attendu";
            foreach ((string name, _) in reader.Entries(parameterTable, $"{where}, params"))
            {
                reader.NonNegative(parameterTable, name);
                parameters.Add(name);
            }
            if (reader.Error != null)
                return $"{where}, params : {reader.Error}";
        }
        string effectsError = ValidateEffects(item, where, enabled, maxLevel, contract, parameters);
        if (effectsError != null)
            return effectsError;
        foreach (Match token in ParameterToken.Matches(Text(item, "description")))
        {
            if (!parameters.Contains(token.Groups[1].Value))
                return $"{where}, description : réglage « {token.Groups[1].Value} » absent de params";
        }
        return item.TryGetProperty("milestones", out JsonElement milestones)
            ? ValidateMilestones(milestones, where, maxLevel, contract)
            : null;
    }

    private static string ValidateEffects(JsonElement item, string where, bool enabled, int maxLevel, Contract contract,
        HashSet<string> parameters)
    {
        if (!item.TryGetProperty("effects", out JsonElement effects) || effects.ValueKind != JsonValueKind.Array
            || effects.GetArrayLength() < 1 || effects.GetArrayLength() > PassiveSouvenirDataLoader.MaxEffects)
            return $"{where}, effects : 1 à {PassiveSouvenirDataLoader.MaxEffects} effets attendus";
        // Les réglages exigés sont ceux de toutes les statistiques de l'objet : chaque effet lit les siens.
        HashSet<string> required = new();
        foreach (JsonElement effect in effects.EnumerateArray())
        {
            JsonConfigReader reader = new(effect);
            reader.AllowOnly(effect, $"{where}, effet", EffectKeys);
            float step = reader.Number(effect, "step");
            // Un objet désactivé, gardé pour les sauvegardes, peut porter un pas nul.
            if (reader.Error == null && enabled && step == 0f)
                reader.Fail("step : 0 (pas non nul attendu)");
            if (reader.Error != null)
                return $"{where}, effet : {reader.Error}";
            string stat = Text(effect, "stat");
            string modifier = effect.TryGetProperty("modifier_type", out _) ? Text(effect, "modifier_type") : "multiplicative";
            if (contract.Stats.TryGetValue(stat ?? "", out (HashSet<string> Modifiers, string[] Params) rule))
            {
                if (!rule.Modifiers.Contains(modifier ?? ""))
                    return $"{where}, {stat} : modificateur « {modifier} » non admis ({string.Join(", ", rule.Modifiers)})";
                required.UnionWith(rule.Params);
            }
            else if (enabled || !contract.InactiveStats.Contains(stat ?? ""))
            {
                return $"{where}, effet : statistique « {stat} » inconnue du contrat";
            }
            // Un facteur nul ou négatif ferait diviser par zéro au passage d'un niveau à l'autre : vérifié au pire cas,
            // toutes les cartes au gain maximal de rareté.
            if (modifier == "multiplicative" && 1f + step * maxLevel * PassiveSouvenirDataLoader.MaxRarityGain <= 0f)
                return $"{where}, {stat} : le facteur s'annule avant le niveau {maxLevel}";
        }
        foreach (string name in required)
        {
            if (!parameters.Contains(name))
                return $"{where}, params : {name} absent (exigé par ses statistiques)";
        }
        foreach (string given in parameters)
        {
            if (!required.Contains(given))
                return $"{where}, params : réglage « {given} » inconnu de ses statistiques ({string.Join(", ", required)})";
        }
        return null;
    }

    private static string ValidateMilestones(JsonElement milestones, string where, int maxLevel, Contract contract)
    {
        if (milestones.ValueKind != JsonValueKind.Array)
            return $"{where}, milestones : tableau attendu";
        foreach (JsonElement milestone in milestones.EnumerateArray())
        {
            JsonConfigReader reader = new(milestone);
            reader.AllowOnly(milestone, $"{where}, palier", MilestoneKeys);
            int level = reader.Integer(milestone, "level", 2, maxLevel);
            if (reader.Error != null)
                return $"{where}, palier : {reader.Error}";
            string effect = Text(milestone, "effect");
            string at = $"{where}, palier {level}";
            if (!contract.Milestones.TryGetValue(effect ?? "", out string[] required))
                return $"{at} : effet « {effect} » inconnu du contrat";
            if (string.IsNullOrEmpty(Text(milestone, "text")))
                return $"{at} : texte absent ou vide";
            HashSet<string> given = new();
            if (milestone.TryGetProperty("params", out JsonElement table))
            {
                if (table.ValueKind != JsonValueKind.Object)
                    return $"{at}, params : objet attendu";
                foreach ((string name, _) in reader.Entries(table, $"{at}, params"))
                {
                    reader.Number(table, name);
                    if (Array.IndexOf(required, name) < 0)
                        reader.Fail($"réglage « {name} » inconnu de {effect} ({string.Join(", ", required)})");
                    given.Add(name);
                }
                if (reader.Error != null)
                    return $"{at}, params : {reader.Error}";
            }
            foreach (string name in required)
            {
                if (!given.Contains(name))
                    return $"{at}, params : {name} absent (exigé par {effect})";
            }
        }
        return null;
    }

    private static bool AllNumbersIn(JsonElement array, double min, double max)
    {
        foreach (JsonElement value in array.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out double number) || number < min || number > max)
                return false;
        }
        return true;
    }

    private static string Text(JsonElement owner, string key) =>
        owner.TryGetProperty(key, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string[] Strings(JsonElement array)
    {
        List<string> values = new();
        foreach (JsonElement value in array.EnumerateArray())
            values.Add(value.GetString());
        return values.ToArray();
    }
}
