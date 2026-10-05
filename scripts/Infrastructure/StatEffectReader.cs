using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Vestiges.Infrastructure;

/// <summary>
/// Lecture d'une liste d'effets de stat (bénédictions), contrôlée en entier (plan 26 Q7c) : identifiant unique, nom
/// traduit, statistique que le joueur applique lui-même, modificateur admis par le contrat des objets, valeur non nulle.
/// </summary>
internal static class StatEffectReader
{
    /// <summary>Rend l'erreur, ou null ; <paramref name="into"/> n'est rempli que si toute la liste est valide.</summary>
    public static string Read(string json, string key, ObjectDataValidator.Contract contract, Func<string, bool> translated,
        List<StatEffectData> into)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            reader.AllowOnly(reader.Root, "liste", key);
            List<StatEffectData> effects = new();
            HashSet<string> ids = new();
            foreach (JsonElement entry in reader.List(reader.Root, key, 1))
            {
                JsonConfigReader item = new(entry);
                string id = item.Text(entry, "id");
                item.AllowOnly(entry, "clés", "id", "name_key", "stat", "modifier_type", "amount");
                string nameKey = item.Text(entry, "name_key");
                string stat = item.Text(entry, "stat");
                string modifier = item.Text(entry, "modifier_type");
                float amount = item.Number(entry, "amount");
                if (item.Error == null && !ids.Add(id))
                    item.Fail("identifiant en double");
                if (item.Error == null && !translated(nameKey))
                    item.Fail($"name_key : clé « {nameKey} » absente des traductions");
                if (item.Error == null && !contract.PlayerStats.Contains(stat))
                    item.Fail($"stat : « {stat} » n'est pas une statistique du joueur");
                if (item.Error == null && !contract.Stats[stat].Modifiers.Contains(modifier))
                    item.Fail($"modifier_type : « {modifier} » non admis pour {stat} ({string.Join(", ", contract.Stats[stat].Modifiers)})");
                if (item.Error == null && amount == 0f)
                    item.Fail("amount : 0 (valeur non nulle attendue)");
                if (item.Error != null)
                    return $"{key} {id ?? "?"} : {item.Error}";
                effects.Add(new StatEffectData { Id = id, NameKey = nameKey, Stat = stat, ModifierType = modifier, Amount = amount });
            }
            if (reader.Error != null)
                return reader.Error;
            into.Clear();
            into.AddRange(effects);
            return null;
        }
        catch (JsonException ex)
        {
            return $"JSON illisible : {ex.Message}";
        }
    }
}
