using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Lecture stricte d'un fichier de réglages (plan 26 Q6b, Q7) : chaque valeur est contrôlée, la première erreur est
/// retenue avec le nom du champ, et les lectures suivantes rendent 0 sans la masquer. Le lecteur qui l'utilise ne
/// publie rien tant que <see cref="Error"/> n'est pas nulle. Les clés commençant par « _ » sont des commentaires.
/// </summary>
public sealed class JsonConfigReader
{
    private readonly JsonElement _root;

    public string Error { get; private set; }

    public JsonConfigReader(JsonElement root)
    {
        _root = root;
        if (root.ValueKind != JsonValueKind.Object)
            Error = "objet attendu à la racine";
    }

    public JsonElement Root => _root;

    public JsonElement Section(string name) => Section(_root, name);

    public JsonElement Section(JsonElement owner, string name)
    {
        if (Error == null && (owner.ValueKind != JsonValueKind.Object || !owner.TryGetProperty(name, out JsonElement section)
            || section.ValueKind != JsonValueKind.Object))
            Error = $"section {name} absente";
        return Error == null ? owner.GetProperty(name) : default;
    }

    /// <summary>Tout nombre fini.</summary>
    public float Number(JsonElement owner, string key) => Number(owner, key, _ => true, "");

    public float Positive(JsonElement owner, string key) => Number(owner, key, value => value > 0f, "strictement positif attendu");
    public float NonNegative(JsonElement owner, string key) => Number(owner, key, value => value >= 0f, "positif ou nul attendu");
    public float Ratio(JsonElement owner, string key) => Number(owner, key, value => value > 0f && value <= 1f, "part dans ]0 ; 1] attendue");

    /// <summary>Un compte : entier de 1 à <paramref name="max"/>.</summary>
    public int Count(JsonElement owner, string key, int max) => Integer(owner, key, 1, max);

    /// <summary>Entier de <paramref name="min"/> à <paramref name="max"/>.</summary>
    public int Integer(JsonElement owner, string key, int min, int max)
    {
        float value = Number(owner, key, number => number >= min && number <= max && number == Mathf.Floor(number),
            $"entier de {min} à {max} attendu");
        return (int)value;
    }

    /// <summary>Refuse une clé inconnue et une clé en double dans <paramref name="owner"/>.</summary>
    public void AllowOnly(JsonElement owner, string where, params string[] keys)
    {
        if (Error != null || owner.ValueKind != JsonValueKind.Object)
            return;
        HashSet<string> seen = new();
        foreach (JsonProperty property in owner.EnumerateObject())
        {
            if (!seen.Add(property.Name))
            {
                Error = $"{where} : clé « {property.Name} » en double";
                return;
            }
            if (!property.Name.StartsWith('_') && Array.IndexOf(keys, property.Name) < 0)
            {
                Error = $"{where} : clé « {property.Name} » inconnue ({string.Join(", ", keys)})";
                return;
            }
        }
    }

    /// <summary>Entrées d'une table nom → nombre, sans les commentaires ; une clé en double est refusée.</summary>
    public List<(string Key, JsonElement Value)> Entries(JsonElement owner, string where)
    {
        List<(string, JsonElement)> entries = new();
        if (Error != null || owner.ValueKind != JsonValueKind.Object)
            return entries;
        HashSet<string> seen = new();
        foreach (JsonProperty property in owner.EnumerateObject())
        {
            if (property.Name.StartsWith('_'))
                continue;
            if (!seen.Add(property.Name))
            {
                Error = $"{where} : clé « {property.Name} » en double";
                return entries;
            }
            entries.Add((property.Name, property.Value));
        }
        return entries;
    }

    /// <summary>Retient une erreur venue d'un contrôle propre au lecteur (référence inconnue…), si aucune ne la précède.</summary>
    public void Fail(string error) => Error ??= error;

    private float Number(JsonElement owner, string key, Func<float, bool> valid, string expectation)
    {
        if (Error != null)
            return 0f;
        if (owner.ValueKind != JsonValueKind.Object || !owner.TryGetProperty(key, out JsonElement element))
        {
            Error = $"{key} absent";
            return 0f;
        }
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetDouble(out double number) || !double.IsFinite(number))
        {
            Error = $"{key} : nombre fini attendu";
            return 0f;
        }
        float value = (float)number;
        if (!valid(value))
        {
            Error = FormattableString.Invariant($"{key} : {value} ({expectation})");
            return 0f;
        }
        return value;
    }
}
