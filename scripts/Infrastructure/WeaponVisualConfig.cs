using System;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Visuels des armes du joueur (data/weapons/weapon_visuals.json) : échelle selon la stat de taille (plan 21 G6e) et
/// étalement du nombre à l'écran, tirs à la chaîne et ondes successives (plan 21 G6g, DECISIONS §123).
/// </summary>
public sealed class WeaponVisualConfig
{
    /// <summary>Manifeste des sprites de projectiles, lu par le combat et par le contrôle des armes.</summary>
    public const string ProjectileManifestPath = "res://assets/vfx/projectiles/projectiles_manifest.json";

    private static WeaponVisualConfig _cached;

    public float SizeResponse { get; private init; }
    public float MaxScale { get; private init; }
    /// <summary>Part du temps de recharge de l'arme sur laquelle les tirs visés d'une attaque se répartissent à intervalles égaux.</summary>
    public float ShotCooldownShare { get; private init; }
    /// <summary>Son d'un tir qui part après le premier : atténuation en dB et variation de hauteur.</summary>
    public float ChainedShotVolumeDb { get; private init; }
    public float ChainedShotPitchVariance { get; private init; }
    /// <summary>Écart entre deux ondes successives d'une frappe circulaire, en secondes.</summary>
    public float WaveInterval { get; private init; }
    /// <summary>Fronts d'onde dessinés au plus dans le cône du Transistor.</summary>
    public int ConeMaxFronts { get; private init; }

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
            ShotCooldownShare = (float)data["shot_cooldown_share"].AsDouble(),
            ChainedShotVolumeDb = (float)data["chained_shot_volume_db"].AsDouble(),
            ChainedShotPitchVariance = (float)data["chained_shot_pitch_variance"].AsDouble(),
            WaveInterval = (float)data["wave_interval_s"].AsDouble(),
            ConeMaxFronts = (int)data["cone_max_fronts"].AsDouble(),
        };
        return _cached;
    }
}
