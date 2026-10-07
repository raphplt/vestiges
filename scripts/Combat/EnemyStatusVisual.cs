using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Statuts lisibles sur le sprite d'une créature (plan 27 V1a) : teinte de priorité (figé > Fragile > brûlure >
/// ralenti), givre, fêlures et bord chaud tramés par entity.gdshader, et rythme de l'animation. La créature lui passe
/// ses états à chaque tick ; le matériau n'est écrit qu'au changement d'état, jamais à chaque image.
/// </summary>
public sealed class EnemyStatusVisual
{
    private static readonly StringName StatusTintParam = "status_tint";
    private static readonly StringName FrostColorParam = "frost_color";
    private static readonly StringName FrostDepthParam = "frost_depth";
    private static readonly StringName CrackColorParam = "crack_color";
    private static readonly StringName CrackSpacingParam = "crack_spacing";
    private static readonly StringName HeatColorParam = "heat_color";
    private static readonly StringName HeatDepthParam = "heat_depth";

    private static StatusVisualConfig _config;
    private static bool _configTried;

    /// <summary>États qui changent le matériau ; saignement et désorientation se lisent autour du sprite (V1b).</summary>
    private const StatusMarks MaterialMarks = StatusMarks.Frozen | StatusMarks.Slowed | StatusMarks.Burning | StatusMarks.Fragile;

    private ShaderMaterial _material;
    private StatusMarks _shown;

    /// <summary>Facteur de cadence de l'animation : 0 figée sur sa pose, le facteur de marche si ralentie.</summary>
    public float AnimationTempo { get; private set; } = 1f;

    /// <summary>
    /// Matériau neuf (configuration de la créature) ou null (retour au pool, créature sans sprite) : un matériau neuf
    /// est neutre, rien n'est donc à effacer.
    /// </summary>
    public void Attach(ShaderMaterial material)
    {
        _material = material;
        _shown = StatusMarks.None;
        AnimationTempo = 1f;
    }

    /// <summary>Mort : l'animation reprend sa cadence, les marques du matériau restent pendant la dissolution.</summary>
    public void ReleaseTempo() => AnimationTempo = 1f;

    /// <param name="moveFactor">Part de la vitesse de marche gardée (0 figée).</param>
    /// <param name="keepsActing">Mini-boss et boss figés agissent encore : leur animation ne s'arrête pas.</param>
    public void Update(StatusMarks marks, float moveFactor, bool keepsActing)
    {
        StatusVisualConfig config = Config();
        if (config == null)
            return;
        if ((marks & StatusMarks.Frozen) != 0)
            AnimationTempo = keepsActing ? 1f : 0f;
        else if ((marks & StatusMarks.Slowed) != 0)
            AnimationTempo = Mathf.Clamp(moveFactor, config.MinAnimationTempo, 1f);
        else
            AnimationTempo = 1f;

        marks &= MaterialMarks;
        if (marks == _shown || _material == null)
            return;
        StatusMarks changed = marks ^ _shown;
        _shown = marks;
        _material.SetShaderParameter(StatusTintParam, PriorityTint(marks, config));
        if ((changed & StatusMarks.Frozen) != 0)
        {
            _material.SetShaderParameter(FrostColorParam, config.FrostColor);
            _material.SetShaderParameter(FrostDepthParam, (marks & StatusMarks.Frozen) != 0 ? config.FrostDepthPx : 0);
        }
        if ((changed & StatusMarks.Fragile) != 0)
        {
            _material.SetShaderParameter(CrackColorParam, config.CrackColor);
            _material.SetShaderParameter(CrackSpacingParam, (marks & StatusMarks.Fragile) != 0 ? config.CrackSpacingPx : 0);
        }
        if ((changed & StatusMarks.Burning) != 0)
        {
            _material.SetShaderParameter(HeatColorParam, config.HeatColor);
            _material.SetShaderParameter(HeatDepthParam, (marks & StatusMarks.Burning) != 0 ? config.HeatDepthPx : 0);
        }
    }

    private static Color PriorityTint(StatusMarks marks, StatusVisualConfig config)
    {
        if ((marks & StatusMarks.Frozen) != 0)
            return config.FrozenTint;
        if ((marks & StatusMarks.Fragile) != 0)
            return config.FragileTint;
        if ((marks & StatusMarks.Burning) != 0)
            return config.BurnTint;
        if ((marks & StatusMarks.Slowed) != 0)
            return config.SlowTint;
        return Colors.Transparent;
    }

    /// <summary>Réglages lus une fois ; refusés, ils sont signalés une fois et les créatures restent sans marque.</summary>
    private static StatusVisualConfig Config()
    {
        if (_configTried)
            return _config;
        _configTried = true;
        if (!StatusVisualConfig.TryLoad(out _config, out string error))
            GD.PushError($"[EnemyStatusVisual] {error}");
        return _config;
    }
}
