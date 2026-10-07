using System.Text.Json;
using Godot;
using Vestiges.Combat;

namespace Vestiges.Infrastructure;

/// <summary>
/// Lecture des statuts sur le sprite des créatures (plan 27 V1a), lue depuis <c>data/fx/status_visuals.json</c> et
/// contrôlée en entier : un réglage invalide est refusé avec le nom du champ, et les créatures restent sans marque
/// plutôt que de jouer avec des valeurs inventées.
/// </summary>
public sealed class StatusVisualConfig
{
    private const string ConfigPath = "res://data/fx/status_visuals.json";
    /// <summary>Profondeur du givre et de la chaleur : borne des boucles déroulées d'entity.gdshader (MAX_EDGE_DEPTH).</summary>
    private const int MaxEdgeDepthPx = 4;
    /// <summary>Étoiles, gouttes ou braises d'une créature : au-delà, une foule marquée devient illisible.</summary>
    private const int MaxMarkCount = 6;
    /// <summary>Au-delà, les fêlures ne se liraient plus comme telles.</summary>
    private const int MaxCrackSpacingPx = 16;

    public Color FrozenTint { get; private init; }
    public Color FrostColor { get; private init; }
    public int FrostDepthPx { get; private init; }
    public Color FragileTint { get; private init; }
    public Color CrackColor { get; private init; }
    public int CrackSpacingPx { get; private init; }
    public Color BurnTint { get; private init; }
    public Color HeatColor { get; private init; }
    public int HeatDepthPx { get; private init; }
    public Color SlowTint { get; private init; }
    public float MinAnimationTempo { get; private init; }

    /// <summary>Marques autour du sprite (V1b) : poses clés par seconde.</summary>
    public float MarkPoseFps { get; private init; }
    public FxRamp StarRamp { get; private init; }
    public int StarCount { get; private init; }
    public FxRamp DropRamp { get; private init; }
    public int DropCount { get; private init; }
    public FxRamp EmberRamp { get; private init; }
    public int EmberMinCount { get; private init; }
    public int EmberMaxCount { get; private init; }
    public float EmberMaxHpSharePerExtra { get; private init; }

    /// <summary>Chiffres des dégâts sur la durée (V2a) : intervalle d'affichage et couleurs.</summary>
    public float DotNumberInterval { get; private init; }
    public Color BurnNumberColor { get; private init; }
    public Color BleedNumberColor { get; private init; }

    public static bool TryLoad(out StatusVisualConfig config, out string error)
    {
        using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            config = null;
            error = $"{ConfigPath} absent";
            return false;
        }
        if (!TryParse(file.GetAsText(), out config, out string parseError))
        {
            error = $"{ConfigPath} : {parseError}";
            return false;
        }
        error = null;
        return true;
    }

    public static bool TryParse(string json, out StatusVisualConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement frozen = reader.Section("frozen");
            JsonElement fragile = reader.Section("fragile");
            JsonElement burn = reader.Section("burn");
            JsonElement slow = reader.Section("slow");
            JsonElement marks = reader.Section("marks");
            JsonElement stars = reader.Section(marks, "stars");
            JsonElement drops = reader.Section(marks, "drops");
            JsonElement embers = reader.Section(marks, "embers");
            JsonElement numbers = reader.Section("numbers");
            StatusVisualConfig parsed = new()
            {
                FrozenTint = Tint(reader, frozen),
                FrostColor = Family(reader, frozen, "frost_family").Light,
                FrostDepthPx = reader.Integer(frozen, "frost_depth_px", 0, MaxEdgeDepthPx),
                FragileTint = Tint(reader, fragile),
                CrackColor = Family(reader, fragile, "crack_family").Light,
                CrackSpacingPx = Spacing(reader, fragile),
                BurnTint = Tint(reader, burn),
                HeatColor = Family(reader, burn, "heat_family").Mid,
                HeatDepthPx = reader.Integer(burn, "heat_depth_px", 0, MaxEdgeDepthPx),
                SlowTint = Tint(reader, slow),
                MinAnimationTempo = reader.Ratio(slow, "min_animation_tempo"),
                MarkPoseFps = reader.Positive(marks, "pose_fps"),
                StarRamp = Family(reader, stars, "family"),
                StarCount = reader.Count(stars, "count", MaxMarkCount),
                DropRamp = Family(reader, drops, "family"),
                DropCount = reader.Count(drops, "count", MaxMarkCount),
                EmberRamp = Family(reader, embers, "family"),
                EmberMinCount = reader.Count(embers, "min_count", MaxMarkCount),
                EmberMaxCount = reader.Count(embers, "max_count", MaxMarkCount),
                EmberMaxHpSharePerExtra = reader.Ratio(embers, "max_hp_share_per_extra"),
                DotNumberInterval = reader.Positive(numbers, "interval_sec"),
                BurnNumberColor = Family(reader, numbers, "burn_family").Mid,
                BleedNumberColor = Family(reader, numbers, "bleed_family").Mid,
            };
            if (parsed.MarkPoseFps is < 2f or > 24f)
                reader.Fail("pose_fps : de 2 à 24 attendu");
            if (parsed.DotNumberInterval is > 0f and (< 0.1f or > 2f))
                reader.Fail("interval_sec : de 0,1 à 2 s attendu");
            if (parsed.EmberMaxCount < parsed.EmberMinCount)
                reader.Fail("embers : max_count inférieur à min_count");
            error = reader.Error;
            config = error == null ? parsed : null;
            return error == null;
        }
        catch (JsonException ex)
        {
            error = $"JSON illisible : {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Teinte : couleur moyenne de la famille (la claire tire au blanc et se confond avec le flash d'un coup), sa
    /// force portée par l'alpha (0 : aucune teinte).
    /// </summary>
    private static Color Tint(JsonConfigReader reader, JsonElement section)
    {
        Color mid = Family(reader, section, "tint_family").Mid;
        return mid with { A = reader.Chance(section, "tint_strength") };
    }

    private static FxRamp Family(JsonConfigReader reader, JsonElement section, string key)
    {
        string name = reader.Text(section, key);
        if (name == null)
            return default;
        FxFamily family = PixelPalette.ParseFamily(name, (FxFamily)(-1));
        if ((int)family < 0)
        {
            reader.Fail($"{key} : famille « {name} » inconnue");
            return default;
        }
        return PixelPalette.Ramp(family);
    }

    /// <summary>Espacement des fêlures : 0 les coupe, sinon au moins 3 pixels pour qu'elles ne couvrent pas le sprite.</summary>
    private static int Spacing(JsonConfigReader reader, JsonElement section)
    {
        int spacing = reader.Integer(section, "crack_spacing_px", 0, MaxCrackSpacingPx);
        if (spacing is 1 or 2)
            reader.Fail("crack_spacing_px : 0 ou au moins 3 attendu");
        return spacing;
    }
}
