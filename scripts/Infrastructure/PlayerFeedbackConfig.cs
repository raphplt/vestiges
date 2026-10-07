using System.Text.Json;
using Godot;
using Vestiges.Combat;

namespace Vestiges.Infrastructure;

/// <summary>
/// Ce que le joueur ressent quand il est touché (plan 27 V3), lu depuis <c>data/fx/player_feedback.json</c> et contrôlé
/// en entier : refusé, le jeu garde ses retours d'avant (éclair clair, secousse moyenne) et le signale une fois.
/// </summary>
public sealed class PlayerFeedbackConfig
{
    private const string ConfigPath = "res://data/fx/player_feedback.json";
    private static PlayerFeedbackConfig _cached;
    private static bool _tried;

    public Color HurtFlashColor { get; private init; }
    public float HurtFlashSeconds { get; private init; }
    public float HurtShakeTrauma { get; private init; }
    public float HurtShakeTraumaPerMaxHp { get; private init; }
    public Color VignetteColor { get; private init; }
    public float VignetteMaxOpacity { get; private init; }
    public float VignetteSeconds { get; private init; }
    public float VignetteThickness { get; private init; }
    public float VignetteSideBias { get; private init; }
    public Color NumberColor { get; private init; }
    /// <summary>Toile (V3b) : teinte (force dans l'alpha), couleur et espacement des fils sur le sprite.</summary>
    public Color WebTint { get; private init; }
    public Color WebThreadColor { get; private init; }
    public int WebThreadSpacingPx { get; private init; }
    /// <summary>Effacement qui ralentit le joueur (V3b) : famille de la poussière pâle et son intervalle.</summary>
    public FxFamily ErasureFamily { get; private init; }
    public Color ErasureTint { get; private init; }
    /// <summary>Défense et soins (V3c).</summary>
    public FxFamily ShieldFamily { get; private init; }
    public FxFamily ParryFamily { get; private init; }
    public float ParryMinShare { get; private init; }
    public float ParryMinHp { get; private init; }
    public FxFamily HealFamily { get; private init; }
    public float HealMinInterval { get; private init; }
    /// <summary>Néant (V3d) : couleur de la vignette qui pulse à chaque tranche, force dans l'alpha.</summary>
    public Color VoidVignetteColor { get; private init; }
    public float ErasureInterval { get; private init; }

    /// <summary>Réglages lus une fois ; null s'ils sont refusés (signalé une fois).</summary>
    public static PlayerFeedbackConfig Get()
    {
        if (_tried)
            return _cached;
        _tried = true;
        using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (file == null)
            GD.PushError($"[PlayerFeedbackConfig] {ConfigPath} absent");
        else if (!TryParse(file.GetAsText(), out _cached, out string error))
            GD.PushError($"[PlayerFeedbackConfig] {ConfigPath} : {error}");
        return _cached;
    }

    public static bool TryParse(string json, out PlayerFeedbackConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement hurt = reader.Section("hurt");
            JsonElement vignette = reader.Section("vignette");
            JsonElement number = reader.Section("number");
            JsonElement web = reader.Section("web");
            JsonElement erasure = reader.Section("erasure");
            JsonElement defense = reader.Section("defense");
            JsonElement voidSection = reader.Section("void");
            PlayerFeedbackConfig parsed = new()
            {
                HurtFlashColor = Family(reader, hurt, "flash_family"),
                HurtFlashSeconds = reader.Positive(hurt, "flash_seconds"),
                HurtShakeTrauma = reader.Chance(hurt, "shake_trauma"),
                HurtShakeTraumaPerMaxHp = reader.NonNegative(hurt, "shake_trauma_per_max_hp"),
                VignetteColor = Family(reader, vignette, "family"),
                VignetteMaxOpacity = reader.Chance(vignette, "max_opacity"),
                VignetteSeconds = reader.Positive(vignette, "seconds"),
                VignetteThickness = reader.Ratio(vignette, "thickness"),
                VignetteSideBias = reader.Chance(vignette, "side_bias"),
                NumberColor = Family(reader, number, "family"),
                WebTint = Family(reader, web, "tint_family") with { A = reader.Chance(web, "tint_strength") },
                WebThreadColor = Ramp(reader, web, "thread_family").Light,
                WebThreadSpacingPx = reader.Integer(web, "thread_spacing_px", 3, 16),
                ErasureFamily = ParseFamily(reader, erasure, "family"),
                ErasureInterval = reader.Positive(erasure, "interval_sec"),
                ErasureTint = Family(reader, erasure, "family") with { A = reader.Chance(erasure, "tint_strength") },
                ShieldFamily = ParseFamily(reader, defense, "shield_family"),
                ParryFamily = ParseFamily(reader, defense, "parry_family"),
                ParryMinShare = reader.Ratio(defense, "parry_min_share"),
                ParryMinHp = reader.NonNegative(defense, "parry_min_hp"),
                HealFamily = ParseFamily(reader, defense, "heal_family"),
                HealMinInterval = reader.NonNegative(defense, "heal_min_interval_sec"),
                VoidVignetteColor = Ramp(reader, voidSection, "family").Light with { A = reader.Chance(voidSection, "max_opacity") },
            };
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

    /// <summary>Couleur moyenne de la famille nommée.</summary>
    private static Color Family(JsonConfigReader reader, JsonElement section, string key) => Ramp(reader, section, key).Mid;

    private static FxRamp Ramp(JsonConfigReader reader, JsonElement section, string key) =>
        PixelPalette.Ramp(ParseFamily(reader, section, key));

    private static FxFamily ParseFamily(JsonConfigReader reader, JsonElement section, string key)
    {
        string name = reader.Text(section, key);
        if (name == null)
            return FxFamily.Physical;
        FxFamily family = PixelPalette.ParseFamily(name, (FxFamily)(-1));
        if ((int)family >= 0)
            return family;
        reader.Fail($"{key} : famille « {name} » inconnue");
        return FxFamily.Physical;
    }
}
