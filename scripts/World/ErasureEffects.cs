using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Ce que l'oubli coûte et offre selon la phase d'une zone (plan 16 O4–O5). Coût pour le joueur qui s'y tient :
/// vitesse, dégâts et voile d'écran, adoucis par rapport à la Stratégie V2 §8. Gain pour qui y combat : score majoré
/// et chance d'Essence en plus à chaque créature abattue. Réglages : zone_effects dans data/scaling/erasure.json.
/// </summary>
public static class ErasureEffects
{
    public readonly record struct Effect(float Speed, float Damage, float Veil, float ScoreBonus, float EssenceChance)
    {
        public static readonly Effect None = new(1f, 1f, 0f, 0f, 0f);
    }

    private static readonly Dictionary<ErasureManager.ErasureZonePhase, Effect> ByPhase = new();
    private static bool _loaded;

    public static Effect For(ErasureManager.ErasureZonePhase phase)
    {
        Load();
        return ByPhase.TryGetValue(phase, out Effect effect) ? effect : Effect.None;
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
            GD.PushError($"[ErasureEffects] erasure.json invalide : {json.GetErrorMessage()}");
            return;
        }
        Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();
        if (!root.ContainsKey("zone_effects"))
            return;
        Godot.Collections.Dictionary effects = root["zone_effects"].AsGodotDictionary();
        Read(effects, "fragile", ErasureManager.ErasureZonePhase.Fragile);
        Read(effects, "frayed", ErasureManager.ErasureZonePhase.Frayed);
        Read(effects, "erased", ErasureManager.ErasureZonePhase.Erased);
        Read(effects, "void", ErasureManager.ErasureZonePhase.Void);
    }

    private static void Read(Godot.Collections.Dictionary effects, string key, ErasureManager.ErasureZonePhase phase)
    {
        if (!effects.ContainsKey(key))
            return;
        Godot.Collections.Dictionary entry = effects[key].AsGodotDictionary();
        ByPhase[phase] = new Effect(
            (float)entry.GetValueOrDefault("speed", 1f).AsDouble(),
            (float)entry.GetValueOrDefault("damage", 1f).AsDouble(),
            (float)entry.GetValueOrDefault("veil", 0f).AsDouble(),
            (float)entry.GetValueOrDefault("score_bonus", 0f).AsDouble(),
            (float)entry.GetValueOrDefault("essence_chance", 0f).AsDouble());
    }
}
