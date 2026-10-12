using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Radios et téléphones abandonnés (plan 39 C1), lus depuis <c>data/world/abandoned_voices.json</c> et contrôlés en
/// entier : fichier invalide ou son absent, la ville reste muette, avec un message.
/// </summary>
public sealed class AbandonedVoicesConfig
{
    private const string ConfigPath = "res://data/world/abandoned_voices.json";

    /// <summary>Un son de la boucle d'une radio et le silence qui le suit. <see cref="Word"/> : il peut se perdre.</summary>
    public readonly record struct RadioStep(string Path, float GapAfterS, bool Word);

    public int MaxHeard { get; private init; }
    /// <summary>Courbe d'atténuation des lecteurs 2D : au-delà de 1, le son reste discret jusqu'à ce qu'on s'approche.</summary>
    public float Attenuation { get; private init; }

    public float RadioHearingPx { get; private init; }
    public float RadioVoiceDb { get; private init; }
    public float RadioStaticDb { get; private init; }
    public float RadioDropoutStaticDb { get; private init; }
    /// <summary>Écart de hauteur tiré par radio, pour que deux postes ne parlent pas d'une même voix.</summary>
    public float RadioPitchJitter { get; private init; }
    public float MemoryWhole { get; private init; }
    public float MemorySilent { get; private init; }
    public string StaticPath { get; private init; }
    /// <summary>Boucle complète d'une radio : bip, puis chaque mot de chaque message, avec ses silences.</summary>
    public IReadOnlyList<RadioStep> RadioLoop { get; private init; }

    public string PhoneBoothId { get; private init; }
    public float PhoneShare { get; private init; }
    public float PhoneHearingPx { get; private init; }
    public float PhoneSilencePx { get; private init; }
    public float PhoneRingDb { get; private init; }
    public string RingPath { get; private init; }

    public static bool TryLoad(out AbandonedVoicesConfig config, out string error)
    {
        using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            config = null;
            error = $"{ConfigPath} absent";
            return false;
        }
        if (!TryParse(file.GetAsText(), path => ResourceLoader.Exists(path), out config, out string parseError))
        {
            error = $"{ConfigPath} : {parseError}";
            return false;
        }
        error = null;
        return true;
    }

    public static bool TryParse(string json, Func<string, bool> soundExists, out AbandonedVoicesConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            JsonConfigReader reader = new(root);
            reader.AllowOnly(root, "racine", "folder", "max_heard", "attenuation", "radio", "phone");
            string folder = reader.Text(root, "folder");
            int maxHeard = reader.Integer(root, "max_heard", 1, 4);
            float attenuation = reader.Positive(root, "attenuation");

            string Sound(JsonElement owner, string key)
            {
                string id = reader.Text(owner, key);
                return id == null ? null : SoundPath(reader, folder, id, soundExists);
            }

            JsonElement radio = reader.Section("radio");
            reader.AllowOnly(radio, "radio", "hearing_px", "voice_db", "static_db", "dropout_static_db", "word_gap_s",
                "sentence_gap_s", "message_gap_s", "cycle_gap_s", "memory_whole", "memory_silent", "pitch_jitter", "chime", "static", "messages");
            float radioHearing = reader.Positive(radio, "hearing_px");
            float voiceDb = reader.Number(radio, "voice_db");
            float staticDb = reader.Number(radio, "static_db");
            float dropoutDb = reader.Number(radio, "dropout_static_db");
            float wordGap = reader.NonNegative(radio, "word_gap_s");
            float sentenceGap = reader.NonNegative(radio, "sentence_gap_s");
            float messageGap = reader.NonNegative(radio, "message_gap_s");
            float cycleGap = reader.Positive(radio, "cycle_gap_s");
            float memoryWhole = reader.Chance(radio, "memory_whole");
            float memorySilent = reader.Chance(radio, "memory_silent");
            float pitchJitter = reader.NonNegative(radio, "pitch_jitter");
            string chime = Sound(radio, "chime");
            string staticPath = Sound(radio, "static");

            List<RadioStep> loop = new() { new RadioStep(chime, sentenceGap, false) };
            List<JsonElement> messages = reader.List(radio, "messages", 1);
            for (int m = 0; m < messages.Count && reader.Error == null; m++)
            {
                List<JsonElement> sentences = ListOf(reader, messages[m], "messages");
                for (int s = 0; s < sentences.Count && reader.Error == null; s++)
                {
                    List<JsonElement> words = ListOf(reader, sentences[s], "messages (phrase)");
                    for (int w = 0; w < words.Count && reader.Error == null; w++)
                    {
                        if (words[w].ValueKind != JsonValueKind.String)
                        {
                            reader.Fail("messages : identifiants de sons attendus");
                            break;
                        }
                        float gap = w < words.Count - 1 ? wordGap
                            : s < sentences.Count - 1 ? sentenceGap
                            : m < messages.Count - 1 ? messageGap
                            : cycleGap;
                        loop.Add(new RadioStep(SoundPath(reader, folder, words[w].GetString(), soundExists), gap, true));
                    }
                }
            }

            JsonElement phone = reader.Section("phone");
            reader.AllowOnly(phone, "phone", "booth_id", "share", "hearing_px", "silence_px", "ring_db", "ring");
            string boothId = reader.Text(phone, "booth_id");
            float phoneShare = reader.Chance(phone, "share");
            float phoneHearing = reader.Positive(phone, "hearing_px");
            float silence = reader.Positive(phone, "silence_px");
            float ringDb = reader.Number(phone, "ring_db");
            string ring = Sound(phone, "ring");

            // Sous memory_silent, plus aucun mot ; au-dessus de memory_whole, tous : l'ordre inverse n'a pas de sens.
            if (memorySilent >= memoryWhole)
                reader.Fail("memory_silent : doit rester sous memory_whole");
            // Un téléphone qui se tait avant d'être entendu ne sonnerait jamais.
            if (silence >= phoneHearing)
                reader.Fail("phone.silence_px : doit rester sous hearing_px");
            // Au-delà, la voix devient une autre voix et le message ne se lit plus comme une même annonce.
            if (pitchJitter > 0.15f)
                reader.Fail("radio.pitch_jitter : 0,15 au plus attendu");
            // Des niveaux positifs saturent le bus des sons du monde.
            if (voiceDb > 0f || staticDb > 0f || dropoutDb > 0f || ringDb > 0f)
                reader.Fail("niveaux (…_db) : 0 au plus attendu");
            error = reader.Error;
            if (error != null)
                return false;
            config = new AbandonedVoicesConfig
            {
                MaxHeard = maxHeard, Attenuation = attenuation, RadioPitchJitter = pitchJitter, PhoneBoothId = boothId,
                RadioHearingPx = radioHearing, RadioVoiceDb = voiceDb, RadioStaticDb = staticDb, RadioDropoutStaticDb = dropoutDb,
                MemoryWhole = memoryWhole, MemorySilent = memorySilent, StaticPath = staticPath, RadioLoop = loop,
                PhoneShare = phoneShare, PhoneHearingPx = phoneHearing, PhoneSilencePx = silence, PhoneRingDb = ringDb, RingPath = ring,
            };
            return true;
        }
        catch (JsonException ex)
        {
            error = $"JSON illisible : {ex.Message}";
            return false;
        }
    }

    private static List<JsonElement> ListOf(JsonConfigReader reader, JsonElement element, string where)
    {
        List<JsonElement> items = new();
        if (element.ValueKind != JsonValueKind.Array || element.GetArrayLength() == 0)
        {
            reader.Fail($"{where} : liste non vide attendue");
            return items;
        }
        foreach (JsonElement item in element.EnumerateArray())
            items.Add(item);
        return items;
    }

    private static string SoundPath(JsonConfigReader reader, string folder, string id, Func<string, bool> soundExists)
    {
        string path = $"res://{folder}/{id}.wav";
        if (!soundExists(path))
            reader.Fail($"son « {id} » introuvable dans {folder}");
        return path;
    }
}
