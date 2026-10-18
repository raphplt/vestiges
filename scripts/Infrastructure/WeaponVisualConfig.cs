using System;
using System.Text.Json;
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

    public const string FilePath = "res://data/weapons/weapon_visuals.json";

    private static WeaponVisualConfig _cached;
    private static string _loadError;

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

    /// <summary>Les réglages du jeu ; refusés, une exception qui nomme le champ (le bootstrap les a déjà contrôlés).</summary>
    public static WeaponVisualConfig Load()
    {
        if (!TryLoad(out WeaponVisualConfig config, out string error))
            throw new InvalidOperationException(error);
        return config;
    }

    /// <summary>Lus strictement une fois (plan 26 Q7g-4a) ; refusés, la raison, et la run ne démarre pas (GameBootstrap).</summary>
    public static bool TryLoad(out WeaponVisualConfig config, out string error)
    {
        if (_cached == null && _loadError == null)
        {
            using FileAccess file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
            _loadError = file == null ? $"{FilePath} : fichier illisible"
                : TryParse(file.GetAsText(), out _cached, out string parseError) ? null : $"{FilePath} : {parseError}";
        }
        config = _cached;
        error = _loadError;
        return error == null;
    }

    /// <summary>Tous les réglages sont obligatoires.</summary>
    public static bool TryParse(string text, out WeaponVisualConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement root = reader.Root;
            reader.AllowOnly(root, "visuels d'armes", "size_response", "max_scale", "shot_cooldown_share", "chained_shot_volume_db",
                "chained_shot_pitch_variance", "wave_interval_s", "cone_max_fronts");
            WeaponVisualConfig parsed = new()
            {
                SizeResponse = reader.NonNegative(root, "size_response"),
                MaxScale = reader.Positive(root, "max_scale"),
                ShotCooldownShare = reader.Chance(root, "shot_cooldown_share"),
                ChainedShotVolumeDb = reader.Number(root, "chained_shot_volume_db"),
                ChainedShotPitchVariance = reader.Chance(root, "chained_shot_pitch_variance"),
                WaveInterval = reader.NonNegative(root, "wave_interval_s"),
                ConeMaxFronts = reader.Integer(root, "cone_max_fronts", 1, 32),
            };
            if (reader.Error == null && parsed.MaxScale < 1f)
                reader.Fail(FormattableString.Invariant($"max_scale : {parsed.MaxScale} (au moins 1, l'échelle ne descend jamais sous 1)"));
            if (reader.Error == null && parsed.ChainedShotVolumeDb > 0f)
                reader.Fail(FormattableString.Invariant($"chained_shot_volume_db : {parsed.ChainedShotVolumeDb} (atténuation, 0 ou moins attendu)"));
            error = reader.Error;
            if (error != null)
                return false;
            config = parsed;
            return true;
        }
        catch (JsonException exception)
        {
            error = $"JSON invalide : {exception.Message}";
            return false;
        }
    }
}
