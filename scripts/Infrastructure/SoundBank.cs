using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Vestiges.Infrastructure;

/// <summary>Réglages d'un son de la banque (data/audio/sounds.json).</summary>
public sealed record SoundBankEntry(string Path, float VolumeDb, ulong MinIntervalMs, int Priority, int MaxVoices, string Bus,
    float? VolumeVarianceDb, (float Db, float Seconds)? MusicDuck, IReadOnlyList<string> Variants);

/// <summary>
/// Banque de sons lue strictement (plan 26 Q7h-1) : fichiers présents, bus existants, bornes de volume, de voix et de
/// priorité. <c>effect_id</c> relie un son à ses crédits (outils de génération) et n'est pas lu par le jeu.
/// </summary>
public static class SoundBank
{
    private const int DefaultPriority = 20;
    private const int DefaultMaxVoices = 2;

    /// <summary><paramref name="busExists"/> reconnaît un bus du mixage ; un son sans bus joue sur <paramref name="defaultBus"/>.</summary>
    public static bool TryParse(string text, Func<string, bool> resourceExists, Func<string, bool> busExists, string defaultBus,
        out Dictionary<string, SoundBankEntry> bank, out string error)
    {
        bank = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonConfigReader reader = new(document.RootElement);
            Dictionary<string, SoundBankEntry> parsed = new();
            foreach ((string key, JsonElement entry) in reader.Entries(reader.Root, "sons"))
            {
                if (entry.ValueKind != JsonValueKind.Object)
                {
                    reader.Fail($"son {key} : objet attendu");
                    break;
                }
                SoundBankEntry sound = Parse(reader, key, entry, resourceExists, busExists, defaultBus);
                if (reader.Error != null)
                {
                    error = $"son {key} : {reader.Error}";
                    return false;
                }
                parsed[key] = sound;
            }
            if (reader.Error == null && parsed.Count == 0)
                reader.Fail("aucun son");
            error = reader.Error;
            if (error != null)
                return false;
            bank = parsed;
            return true;
        }
        catch (JsonException exception)
        {
            error = $"JSON invalide : {exception.Message}";
            return false;
        }
    }

    private static SoundBankEntry Parse(JsonConfigReader reader, string key, JsonElement entry, Func<string, bool> resourceExists,
        Func<string, bool> busExists, string defaultBus)
    {
        reader.AllowOnly(entry, "réglages", "path", "volume_db", "min_interval_ms", "priority", "max_voices", "bus",
            "volume_variance_db", "duck_music_db", "duck_sec", "variants", "effect_id");
        string path = ExistingFile(reader, entry, "path", resourceExists);
        float volume = reader.Number(entry, "volume_db");
        if (reader.Error == null && (volume < -80f || volume > 24f))
            reader.Fail(FormattableString.Invariant($"volume_db : {volume} (de -80 à 24 dB attendu)"));
        ulong interval = (ulong)reader.Integer(entry, "min_interval_ms", 0, 600000);
        int priority = JsonConfigReader.Has(entry, "priority") ? reader.Integer(entry, "priority", 0, 1000) : DefaultPriority;
        int voices = JsonConfigReader.Has(entry, "max_voices") ? reader.Integer(entry, "max_voices", 1, 64) : DefaultMaxVoices;
        string bus = JsonConfigReader.Has(entry, "bus") ? reader.Text(entry, "bus") : defaultBus;
        if (reader.Error == null && !busExists(bus))
            reader.Fail($"bus « {bus} » inconnu du mixage");
        float? variance = JsonConfigReader.Has(entry, "volume_variance_db") ? reader.NonNegative(entry, "volume_variance_db") : null;
        (float, float)? duck = null;
        if (JsonConfigReader.Has(entry, "duck_music_db") || JsonConfigReader.Has(entry, "duck_sec"))
        {
            // L'atténuation de la musique et sa durée vont ensemble.
            float db = reader.Number(entry, "duck_music_db");
            float seconds = reader.Positive(entry, "duck_sec");
            if (reader.Error == null && db > 0f)
                reader.Fail(FormattableString.Invariant($"duck_music_db : {db} (atténuation, 0 ou moins attendu)"));
            duck = (db, seconds);
        }
        List<string> variants = new();
        if (JsonConfigReader.Has(entry, "variants"))
        {
            foreach (JsonElement item in reader.List(entry, "variants", 2))
            {
                string variant = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
                if (string.IsNullOrEmpty(variant) || !resourceExists(variant))
                    reader.Fail($"variants : fichier « {variant} » introuvable");
                else
                    variants.Add(variant);
            }
        }
        if (JsonConfigReader.Has(entry, "effect_id"))
            reader.Text(entry, "effect_id");
        return new SoundBankEntry(path, volume, interval, priority, voices, bus, variance, duck, variants);
    }

    private static string ExistingFile(JsonConfigReader reader, JsonElement entry, string key, Func<string, bool> resourceExists)
    {
        string path = reader.Text(entry, key);
        if (path != null && !resourceExists(path))
            reader.Fail($"{key} : fichier « {path} » introuvable");
        return path;
    }
}
