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
    /// <summary>Une chance : de 0 (jamais) à 1 (toujours).</summary>
    public float Chance(JsonElement owner, string key) => Number(owner, key, value => value >= 0f && value <= 1f, "chance dans [0 ; 1] attendue");

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

    /// <summary>Texte obligatoire et non vide.</summary>
    public string Text(JsonElement owner, string key)
    {
        if (Error != null)
            return null;
        if (owner.ValueKind != JsonValueKind.Object || !owner.TryGetProperty(key, out JsonElement element))
        {
            Error = $"{key} absent";
            return null;
        }
        if (element.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(element.GetString()))
        {
            Error = $"{key} : texte non vide attendu";
            return null;
        }
        return element.GetString();
    }

    /// <summary>Texte obligatoire pris dans <paramref name="allowed"/> (identifiant d'un autre catalogue).</summary>
    public string OneOf(JsonElement owner, string key, IReadOnlyCollection<string> allowed)
    {
        string value = Text(owner, key);
        if (value != null && !Contains(allowed, value))
            Error = $"{key} : « {value} » inconnu ({string.Join(", ", allowed)})";
        return Error == null ? value : null;
    }

    /// <summary>Chemin d'une ressource du projet (sans « res:// ») qui doit exister.</summary>
    public string Resource(JsonElement owner, string key, Func<string, bool> exists)
    {
        string path = Text(owner, key);
        if (path != null && !exists("res://" + path))
            Error = $"{key} : image « {path} » introuvable";
        return Error == null ? path : null;
    }

    /// <summary>Couleur écrite [r, g, b], chaque composante de 0 à 1.</summary>
    public Color Rgb(JsonElement owner, string key)
    {
        if (Error != null)
            return default;
        if (owner.ValueKind != JsonValueKind.Object || !owner.TryGetProperty(key, out JsonElement rgb)
            || rgb.ValueKind != JsonValueKind.Array || rgb.GetArrayLength() != 3)
        {
            Error = $"{key} : trois composantes de 0 à 1 attendues";
            return default;
        }
        float[] values = new float[3];
        for (int i = 0; i < 3; i++)
        {
            if (rgb[i].ValueKind != JsonValueKind.Number || rgb[i].GetDouble() < 0.0 || rgb[i].GetDouble() > 1.0)
            {
                Error = $"{key} : trois composantes de 0 à 1 attendues";
                return default;
            }
            values[i] = (float)rgb[i].GetDouble();
        }
        return new Color(values[0], values[1], values[2]);
    }

    /// <summary>Booléen facultatif, faux par défaut.</summary>
    public bool Flag(JsonElement owner, string key)
    {
        if (Error != null || owner.ValueKind != JsonValueKind.Object || !owner.TryGetProperty(key, out JsonElement element))
            return false;
        if (element.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            Error = $"{key} : booléen attendu";
            return false;
        }
        return element.GetBoolean();
    }

    /// <summary>Intervalle [min ; max] écrit comme un tableau de deux nombres, chacun au moins <paramref name="floor"/>.</summary>
    public (float Min, float Max) Range(JsonElement owner, string key, float floor, float ceiling = float.MaxValue)
    {
        if (Error != null)
            return (0f, 0f);
        if (owner.ValueKind != JsonValueKind.Object || !owner.TryGetProperty(key, out JsonElement element)
            || element.ValueKind != JsonValueKind.Array || element.GetArrayLength() != 2)
        {
            Error = $"{key} : intervalle [min, max] attendu";
            return (0f, 0f);
        }
        float min = (float)(element[0].ValueKind == JsonValueKind.Number ? element[0].GetDouble() : double.NaN);
        float max = (float)(element[1].ValueKind == JsonValueKind.Number ? element[1].GetDouble() : double.NaN);
        if (!float.IsFinite(min) || !float.IsFinite(max) || min < floor || max > ceiling || min > max)
        {
            Error = ceiling == float.MaxValue
                ? FormattableString.Invariant($"{key} : [{min}, {max}] (intervalle croissant, au moins {floor}, attendu)")
                : FormattableString.Invariant($"{key} : [{min}, {max}] (intervalle croissant dans [{floor} ; {ceiling}] attendu)");
            return (0f, 0f);
        }
        return (min, max);
    }

    /// <summary>Tableau obligatoire d'au moins <paramref name="minCount"/> éléments.</summary>
    public List<JsonElement> List(JsonElement owner, string key, int minCount)
    {
        List<JsonElement> items = new();
        if (Error != null)
            return items;
        if (owner.ValueKind != JsonValueKind.Object || !owner.TryGetProperty(key, out JsonElement element)
            || element.ValueKind != JsonValueKind.Array || element.GetArrayLength() < minCount)
        {
            Error = $"{key} : liste d'au moins {minCount} élément(s) attendue";
            return items;
        }
        foreach (JsonElement item in element.EnumerateArray())
            items.Add(item);
        return items;
    }

    /// <summary>La valeur figure dans la collection (identifiants d'un autre catalogue).</summary>
    public static bool Contains(IReadOnlyCollection<string> values, string value)
    {
        foreach (string candidate in values)
        {
            if (candidate == value)
                return true;
        }
        return false;
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
        if (!float.IsFinite(value))
        {
            Error = $"{key} : nombre fini attendu";
            return 0f;
        }
        if (!valid(value))
        {
            Error = FormattableString.Invariant($"{key} : {value} ({expectation})");
            return 0f;
        }
        return value;
    }
}
