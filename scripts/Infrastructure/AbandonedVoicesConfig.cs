using System;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Téléphones abandonnés (plan 39 C1), lus depuis <c>data/world/abandoned_voices.json</c> et contrôlés en entier :
/// fichier invalide ou son absent, les cabines restent muettes, avec un message.
/// </summary>
public sealed class AbandonedVoicesConfig
{
    private const string ConfigPath = "res://data/world/abandoned_voices.json";

    public int MaxHeard { get; private init; }
    /// <summary>Courbe d'atténuation des lecteurs 2D : au-delà de 1, le son reste discret jusqu'à ce qu'on s'approche.</summary>
    public float Attenuation { get; private init; }

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
            reader.AllowOnly(root, "racine", "folder", "max_heard", "attenuation", "phone");
            string folder = reader.Text(root, "folder");
            int maxHeard = reader.Integer(root, "max_heard", 1, 4);
            float attenuation = reader.Positive(root, "attenuation");

            JsonElement phone = reader.Section("phone");
            reader.AllowOnly(phone, "phone", "booth_id", "share", "hearing_px", "silence_px", "ring_db", "ring");
            string boothId = reader.Text(phone, "booth_id");
            float phoneShare = reader.Chance(phone, "share");
            float phoneHearing = reader.Positive(phone, "hearing_px");
            float silence = reader.Positive(phone, "silence_px");
            float ringDb = reader.Number(phone, "ring_db");
            string ringId = reader.Text(phone, "ring");
            string ring = ringId == null ? null : $"res://{folder}/{ringId}.wav";
            if (ring != null && !soundExists(ring))
                reader.Fail($"son « {ringId} » introuvable dans {folder}");

            // Un téléphone qui se tait avant d'être entendu ne sonnerait jamais.
            if (silence >= phoneHearing)
                reader.Fail("phone.silence_px : doit rester sous hearing_px");
            // Un niveau positif sature le bus des sons du monde.
            if (ringDb > 0f)
                reader.Fail("phone.ring_db : 0 au plus attendu");
            error = reader.Error;
            if (error != null)
                return false;
            config = new AbandonedVoicesConfig
            {
                MaxHeard = maxHeard, Attenuation = attenuation, PhoneBoothId = boothId, PhoneShare = phoneShare,
                PhoneHearingPx = phoneHearing, PhoneSilencePx = silence, PhoneRingDb = ringDb, RingPath = ring,
            };
            return true;
        }
        catch (JsonException ex)
        {
            error = $"JSON illisible : {ex.Message}";
            return false;
        }
    }
}
