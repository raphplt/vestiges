using System;
using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Visuels des armes du joueur (data/weapons/weapon_visuals.json) : échelle selon la stat de taille (plan 21 G6e) et
/// étalement du nombre à l'écran, rafales et ondes successives (plan 21 G6g).
/// </summary>
public sealed class WeaponVisualConfig
{
    private static WeaponVisualConfig _cached;

    public float SizeResponse { get; private init; }
    public float MaxScale { get; private init; }
    /// <summary>Écart entre deux projectiles d'une rafale sur une même cible, en secondes.</summary>
    public float VolleyInterval { get; private init; }
    /// <summary>Durée maximale d'une rafale : au-delà, l'écart se resserre.</summary>
    public float VolleyMaxSpan { get; private init; }
    /// <summary>Écart entre deux ondes successives d'une frappe circulaire, en secondes.</summary>
    public float WaveInterval { get; private init; }
    /// <summary>Fronts d'onde dessinés au plus dans le cône du Transistor.</summary>
    public int ConeMaxFronts { get; private init; }
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
            VolleyInterval = (float)data["volley_interval_s"].AsDouble(),
            VolleyMaxSpan = (float)data["volley_max_s"].AsDouble(),
            WaveInterval = (float)data["wave_interval_s"].AsDouble(),
            ConeMaxFronts = (int)data["cone_max_fronts"].AsDouble(),
        };
        if (data.ContainsKey("projectile_base_scale"))
            foreach ((Variant key, Variant value) in data["projectile_base_scale"].AsGodotDictionary())
                _cached._projectileBaseScale[key.AsString()] = (float)value.AsDouble();
        return _cached;
    }
}
