using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

public class SouvenirData
{
    public string Id;
    public string Name;
    public string ConstellationId;
    public string Text;
}

public class ConstellationData
{
    public string Id;
    public string Name;
    public string Description;
    public Color Color;
    public int Order;
}

/// <summary>
/// Constellations et Souvenirs (data/souvenirs/), lus strictement une fois (plan 26 Q7f-3) : identifiants uniques,
/// textes non vides, couleurs et rangs valides, chaque Souvenir dans une constellation connue. Un catalogue refusé n'est
/// pas publié et arrête le chargement de la run (GameBootstrap).
/// </summary>
public static class SouvenirDataLoader
{
    public const string ConstellationsPath = "res://data/souvenirs/constellations.json";
    public const string SouvenirsPath = "res://data/souvenirs/souvenirs.json";

    private static Dictionary<string, SouvenirData> _souvenirCache = new();
    private static Dictionary<string, ConstellationData> _constellationCache = new();
    private static bool _loaded;
    private static string _loadError;

    /// <summary>Les deux fichiers lus et contrôlés ; faux, avec la raison, s'ils ont été refusés (la run ne démarre pas).</summary>
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
        _loadError = "Souvenirs : lecture interrompue";
        _loadError = Apply(Read(ConstellationsPath), Read(SouvenirsPath));
        if (_loadError != null)
        {
            GD.PushError($"[SouvenirDataLoader] {_loadError}");
            return;
        }
        GD.Print($"[SouvenirDataLoader] Loaded {_constellationCache.Count} constellations, {_souvenirCache.Count} souvenirs");
    }

    /// <summary>Contrôle les deux catalogues et ne publie que s'ils sont acceptés ; rend l'erreur, nulle si tout est publié.</summary>
    public static string Apply(string constellationsText, string souvenirsText)
    {
        Dictionary<string, ConstellationData> constellations = new();
        string error = ReadList(ConstellationsPath, constellationsText, (reader, entry) =>
        {
            reader.AllowOnly(entry, "constellation", "id", "name", "description", "color", "order");
            ConstellationData data = new()
            {
                Id = reader.Text(entry, "id"),
                Name = reader.Text(entry, "name"),
                Description = reader.Text(entry, "description"),
                Order = reader.Integer(entry, "order", 0, 1000),
            };
            string color = reader.Text(entry, "color");
            if (reader.Error == null && !Color.HtmlIsValid(color))
                reader.Fail($"color : couleur « {color} » invalide");
            if (reader.Error != null)
                return;
            data.Color = Color.FromHtml(color);
            if (constellations.ContainsKey(data.Id))
                reader.Fail("identifiant déjà déclaré");
            foreach (ConstellationData other in constellations.Values)
            {
                if (reader.Error == null && other.Order == data.Order)
                    reader.Fail($"order : rang {data.Order} déjà pris par « {other.Id} »");
            }
            constellations[data.Id] = data;
        });
        if (error != null)
            return error;
        Dictionary<string, SouvenirData> souvenirs = new();
        error = ReadList(SouvenirsPath, souvenirsText, (reader, entry) =>
        {
            reader.AllowOnly(entry, "souvenir", "id", "name", "constellation", "text");
            SouvenirData data = new()
            {
                Id = reader.Text(entry, "id"),
                Name = reader.Text(entry, "name"),
                ConstellationId = reader.Text(entry, "constellation"),
                Text = reader.Text(entry, "text"),
            };
            if (reader.Error != null)
                return;
            if (souvenirs.ContainsKey(data.Id))
                reader.Fail("identifiant déjà déclaré");
            else if (!constellations.ContainsKey(data.ConstellationId))
                reader.Fail($"constellation « {data.ConstellationId} » inconnue");
            souvenirs[data.Id] = data;
        });
        if (error != null)
            return error;
        _constellationCache = constellations;
        _souvenirCache = souvenirs;
        return null;
    }

    public static SouvenirData Get(string id)
    {
        Load();
        return id != null && _souvenirCache.TryGetValue(id, out SouvenirData data) ? data : null;
    }

    public static List<SouvenirData> GetAll()
    {
        Load();
        return new List<SouvenirData>(_souvenirCache.Values);
    }

    public static List<SouvenirData> GetByConstellation(string constellationId)
    {
        Load();
        List<SouvenirData> result = new();
        foreach (SouvenirData s in _souvenirCache.Values)
        {
            if (s.ConstellationId == constellationId)
                result.Add(s);
        }
        return result;
    }

    public static ConstellationData GetConstellation(string id)
    {
        Load();
        return id != null && _constellationCache.TryGetValue(id, out ConstellationData data) ? data : null;
    }

    public static List<ConstellationData> GetAllConstellations()
    {
        Load();
        List<ConstellationData> list = new(_constellationCache.Values);
        list.Sort((a, b) => a.Order.CompareTo(b.Order));
        return list;
    }

    private static string Read(string path)
    {
        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        return file?.GetAsText();
    }

    /// <summary>Liste non vide d'objets, chacun lu par <paramref name="parse"/> ; la première erreur nomme le fichier et l'entrée.</summary>
    private static string ReadList(string path, string text, Action<JsonConfigReader, JsonElement> parse)
    {
        if (text == null)
            return $"{path} : fichier illisible";
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
                return $"{path} : liste non vide attendue à la racine";
            int index = 0;
            foreach (JsonElement entry in document.RootElement.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object)
                    return $"{path}, entrée {index} : objet attendu";
                JsonConfigReader reader = new(entry);
                parse(reader, entry);
                if (reader.Error != null)
                    return $"{path}, entrée {index} : {reader.Error}";
                index++;
            }
            return null;
        }
        catch (JsonException exception)
        {
            return $"{path} : JSON invalide : {exception.Message}";
        }
    }
}
