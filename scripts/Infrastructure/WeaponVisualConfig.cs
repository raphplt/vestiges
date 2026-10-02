using System;
using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Échelle visuelle des armes du joueur selon la stat de taille (data/weapons/weapon_visuals.json, plan 21 G6e).</summary>
public sealed class WeaponVisualConfig
{
    private static WeaponVisualConfig _cached;

    public float SizeResponse { get; private init; }
    public float MaxScale { get; private init; }
    private readonly Dictionary<string, float> _projectileBaseScale = new();

    /// <summary>Échelle de base d'un sprite de projectile du joueur, 1 s'il se lit déjà (DECISIONS §53).</summary>
    public float ProjectileBaseScale(string spriteId) => _projectileBaseScale.GetValueOrDefault(spriteId ?? "", 1f);

    /// <summary>Échelle d'un visuel d'arme pour un multiplicateur de taille donné, jamais sous 1 ni au-delà du plafond.</summary>
    public float ScaleFor(float sizeMultiplier) => Mathf.Clamp(1f + (sizeMultiplier - 1f) * SizeResponse, 1f, MaxScale);

    public static WeaponVisualConfig Load()
    {
        if (_cached != null)
            return _cached;

        using FileAccess file = FileAccess.Open("res://data/weapons/weapon_visuals.json", FileAccess.ModeFlags.Read);
        if (file == null)
            throw new InvalidOperationException("Échelle visuelle des armes absente.");
        using Json json = new();
        if (json.Parse(file.GetAsText()) != Error.Ok || json.Data.VariantType != Variant.Type.Dictionary)
            throw new InvalidOperationException("Échelle visuelle des armes invalide.");
        Godot.Collections.Dictionary data = json.Data.AsGodotDictionary();
        _cached = new WeaponVisualConfig
        {
            SizeResponse = (float)data["size_response"].AsDouble(),
            MaxScale = (float)data["max_scale"].AsDouble(),
        };
        if (data.ContainsKey("projectile_base_scale"))
            foreach ((Variant key, Variant value) in data["projectile_base_scale"].AsGodotDictionary())
                _cached._projectileBaseScale[key.AsString()] = (float)value.AsDouble();
        return _cached;
    }
}
