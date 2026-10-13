using System;
using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class PerkEffect
{
    public string Stat { get; set; }
    public float Modifier { get; set; }
    public string ModifierType { get; set; }
}

/// <summary>Signature de départ d'un personnage : des modificateurs de stats appliqués une fois (data/perks/perks.json).</summary>
public class PerkData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Stat { get; set; }
    public float Modifier { get; set; }
    public string ModifierType { get; set; }
    public bool IsPassive { get; set; }
    public string CharacterId { get; set; }
    public List<PerkEffect> Effects { get; set; }
    /// <summary>Élan d'une signature qui récompense le mouvement continu (Tournée du Facteur), sinon null.</summary>
    public PerkMomentum Momentum { get; set; }
    /// <summary>Rémanence d'une signature qui répète un tir après la mobilité (Double de l'Éveillée), sinon null.</summary>
    public PerkEcho Echo { get; set; }
    /// <summary>Réserve qui suspend les malus de déplacement et de dégâts de l'Effacement, sinon null.</summary>
    public PerkAir Air { get; set; }
}

/// <summary>Durées de la réserve d'air : consommation complète et recharge complète hors danger.</summary>
public sealed record PerkAirAudio(string Consume, string Empty, float MinIntervalSeconds);

public sealed class PerkAir
{
    public float Seconds { get; }
    public float RechargeSeconds { get; }
    public PerkAirAudio Audio { get; }

    public PerkAir(float seconds, float rechargeSeconds, PerkAirAudio audio = null)
    {
        if (!float.IsFinite(seconds) || seconds <= 0f || !float.IsFinite(rechargeSeconds) || rechargeSeconds <= 0f)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Les durées d'air doivent être finies et strictement positives.");
        Seconds = seconds;
        RechargeSeconds = rechargeSeconds;
        Audio = audio;
    }
}

/// <summary>Réglages de la rémanence : sa durée et la part des dégâts du tir qu'elle répète.</summary>
public class PerkEcho
{
    public float Seconds { get; set; }
    public float DamageRatio { get; set; }
}

/// <summary>Réglages de l'élan : bonus au plein, temps pour l'atteindre, arrêt toléré avant la remise à zéro.</summary>
public class PerkMomentum
{
    public float RampSeconds { get; set; }
    public float SpeedBonus { get; set; }
    public float AttackSpeedBonus { get; set; }
    public float StopGraceSeconds { get; set; }
}

public static class PerkDataLoader
{
    private static readonly List<PerkData> _allPerks = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        FileAccess file = FileAccess.Open("res://data/perks/perks.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[PerkDataLoader] Cannot open perks.json");
            return;
        }

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        if (json.Parse(jsonText) != Error.Ok)
        {
            GD.PushError($"[PerkDataLoader] Parse error: {json.GetErrorMessage()}");
            return;
        }

        // Ne publier aucune signature si un réglage du fichier est refusé.
        List<PerkData> parsed = new();
        foreach (Variant item in json.Data.AsGodotArray())
        {
            if (item.VariantType != Variant.Type.Dictionary)
                continue;

            Godot.Collections.Dictionary dict = item.AsGodotDictionary();

            List<PerkEffect> effects = null;
            if (dict.ContainsKey("effects"))
            {
                effects = new List<PerkEffect>();
                foreach (Variant effectItem in dict["effects"].AsGodotArray())
                {
                    Godot.Collections.Dictionary effectDict = effectItem.AsGodotDictionary();
                    effects.Add(new PerkEffect
                    {
                        Stat = effectDict.ContainsKey("stat") ? effectDict["stat"].AsString() : null,
                        Modifier = effectDict.ContainsKey("modifier") ? (float)effectDict["modifier"].AsDouble() : 0f,
                        ModifierType = effectDict.ContainsKey("modifier_type") ? effectDict["modifier_type"].AsString() : null
                    });
                }
            }

            PerkData perk = new()
            {
                Id = dict["id"].AsString(),
                Name = dict["name"].AsString(),
                Description = dict.ContainsKey("description") ? dict["description"].AsString() : "",
                Stat = dict.ContainsKey("stat") ? dict["stat"].AsString() : null,
                Modifier = dict.ContainsKey("modifier") ? (float)dict["modifier"].AsDouble() : 0f,
                ModifierType = dict.ContainsKey("modifier_type") ? dict["modifier_type"].AsString() : null,
                IsPassive = dict.ContainsKey("is_passive") && dict["is_passive"].AsBool(),
                CharacterId = dict.ContainsKey("character_id") ? dict["character_id"].AsString() : null,
                Effects = effects,
                Momentum = dict.ContainsKey("momentum") ? ReadMomentum(dict["momentum"].AsGodotDictionary()) : null,
                Echo = dict.ContainsKey("echo") ? ReadEcho(dict["echo"].AsGodotDictionary()) : null,
                Air = dict.ContainsKey("air") ? ReadAir(dict["air"]) : null,
            };
            parsed.Add(perk);
        }

        _allPerks.AddRange(parsed);
        _loaded = true;
        GD.Print($"[PerkDataLoader] Loaded {_allPerks.Count} character signatures");
    }

    private static PerkMomentum ReadMomentum(Godot.Collections.Dictionary dict) => new()
    {
        RampSeconds = Mathf.Max(0.1f, (float)dict["ramp_seconds"].AsDouble()),
        SpeedBonus = (float)dict["speed_bonus"].AsDouble(),
        AttackSpeedBonus = (float)dict["attack_speed_bonus"].AsDouble(),
        StopGraceSeconds = (float)dict["stop_grace_seconds"].AsDouble(),
    };

    private static PerkEcho ReadEcho(Godot.Collections.Dictionary dict) => new()
    {
        Seconds = Mathf.Max(0.1f, (float)dict["seconds"].AsDouble()),
        DamageRatio = Mathf.Clamp((float)dict["damage_ratio"].AsDouble(), 0f, 1f),
    };

    internal static PerkAir ReadAir(Variant value)
    {
        if (value.VariantType != Variant.Type.Dictionary)
            throw new InvalidOperationException("air : objet attendu.");
        Godot.Collections.Dictionary dict = value.AsGodotDictionary();
        foreach (Variant key in dict.Keys)
            if (key.AsString() is not ("seconds" or "recharge_seconds" or "audio"))
                throw new InvalidOperationException($"air : clé inconnue « {key} ».");
        return new PerkAir(ReadAirSeconds(dict, "seconds"), ReadAirSeconds(dict, "recharge_seconds"),
            dict.TryGetValue("audio", out Variant audio) ? ReadAirAudio(audio) : null);
    }

    private static PerkAirAudio ReadAirAudio(Variant value)
    {
        if (value.VariantType != Variant.Type.Dictionary)
            throw new InvalidOperationException("air.audio : objet attendu.");
        Godot.Collections.Dictionary audio = value.AsGodotDictionary();
        foreach (Variant key in audio.Keys)
            if (key.AsString() is not ("consume" or "empty" or "min_interval_seconds"))
                throw new InvalidOperationException($"air.audio : clé inconnue « {key} ».");
        string Sound(string key)
        {
            if (!audio.TryGetValue(key, out Variant item) || item.VariantType != Variant.Type.String
                || string.IsNullOrWhiteSpace(item.AsString())
                || !DataKeySets.TopLevelKeys(AudioManager.SoundBankPath).Contains(item.AsString()))
                throw new InvalidOperationException($"air.audio.{key} : son de la banque requis.");
            return item.AsString();
        }
        float interval = ReadAirSeconds(audio, "min_interval_seconds");
        if (interval < 1f || interval > 30f)
            throw new InvalidOperationException("air.audio : intervalle entre 1 et 30 secondes requis.");
        return new PerkAirAudio(Sound("consume"), Sound("empty"), interval);
    }

    private static float ReadAirSeconds(Godot.Collections.Dictionary dict, string key)
    {
        if (!dict.TryGetValue(key, out Variant value) || value.VariantType is not (Variant.Type.Float or Variant.Type.Int))
            throw new InvalidOperationException($"air.{key} : nombre requis.");
        float seconds = (float)value.AsDouble();
        if (!float.IsFinite(seconds) || seconds <= 0f)
            throw new InvalidOperationException($"air.{key} : durée finie strictement positive requise.");
        return seconds;
    }

    public static List<PerkData> GetAll()
    {
        if (!_loaded)
            Load();

        return _allPerks;
    }
}
