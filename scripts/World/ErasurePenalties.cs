using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Ce que l'oubli coûte au joueur selon la phase de sa zone (plan 16 O4) : facteurs de vitesse et de dégâts,
/// adoucis par rapport à la Stratégie V2 §8. Réglages : player_penalties dans data/scaling/erasure.json.
/// </summary>
public static class ErasurePenalties
{
    /// <summary>Facteurs de vitesse et de dégâts, et intensité du voile d'écran qui le signale (0 à 1).</summary>
    public readonly record struct Penalty(float Speed, float Damage, float Veil);

    private static readonly Dictionary<ErasureManager.ErasureZonePhase, Penalty> ByPhase = new();
    private static bool _loaded;

    public static Penalty For(ErasureManager.ErasureZonePhase phase)
    {
        Load();
        return ByPhase.TryGetValue(phase, out Penalty penalty) ? penalty : new Penalty(1f, 1f, 0f);
    }

    private static void Load()
    {
        if (_loaded)
            return;
        _loaded = true;
        using FileAccess file = FileAccess.Open("res://data/scaling/erasure.json", FileAccess.ModeFlags.Read);
        if (file == null)
            return;
        Json json = new();
        if (json.Parse(file.GetAsText()) != Error.Ok)
        {
            GD.PushError($"[ErasurePenalties] erasure.json invalide : {json.GetErrorMessage()}");
            return;
        }
        Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();
        if (!root.ContainsKey("player_penalties"))
            return;
        Godot.Collections.Dictionary penalties = root["player_penalties"].AsGodotDictionary();
        Read(penalties, "frayed", ErasureManager.ErasureZonePhase.Frayed);
        Read(penalties, "erased", ErasureManager.ErasureZonePhase.Erased);
        Read(penalties, "void", ErasureManager.ErasureZonePhase.Void);
    }

    private static void Read(Godot.Collections.Dictionary penalties, string key, ErasureManager.ErasureZonePhase phase)
    {
        if (!penalties.ContainsKey(key))
            return;
        Godot.Collections.Dictionary entry = penalties[key].AsGodotDictionary();
        ByPhase[phase] = new Penalty(
            (float)entry.GetValueOrDefault("speed", 1f).AsDouble(),
            (float)entry.GetValueOrDefault("damage", 1f).AsDouble(),
            (float)entry.GetValueOrDefault("veil", 0f).AsDouble());
    }
}
