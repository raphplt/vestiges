using Godot;

namespace Vestiges.Combat.Abilities;

/// <summary>
/// Annonce au sol d'une attaque ennemie ou d'un événement : zone circulaire ou trajectoire, en pixel art
/// (bord plein, intérieur tramé, remplissage selon l'avancement). Détachée du transform de son
/// propriétaire pour rester fixe pendant que l'ennemi bouge. Toujours affichée : seule son opacité se règle.
/// </summary>
public partial class GroundTelegraph : Node2D
{
    /// <summary>Paliers de remplissage : l'annonce avance par poses lisibles, pas en glissement continu.</summary>
    private const int ProgressSteps = 12;
    private const float AreaDensity = 0.25f;

    private readonly PixelFx _fx;
    private int _progressStep = -1;

    public GroundTelegraph()
    {
        TopLevel = true;
        // Au-dessus du sol (TileMapLayer à z 0) : un z négatif la cacherait sous les tuiles opaques.
        ZAsRelative = false;
        ZIndex = 1;
        _fx = PixelFx.Create(_ => { });
        AddChild(_fx);
    }

    public void ShowCircle(Vector2 center, float radius, FxFamily family)
    {
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Zone, family, radius, 1f, 1f);
        Begin(center, spec);
    }

    public void ShowLine(Vector2 from, Vector2 to, float width, FxFamily family)
    {
        Vector2 delta = to - from;
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Lane, family, delta.Length(), width, 1f);
        spec.Angle = delta.Angle();
        Begin(from, spec);
    }

    public void SetProgress(float progress)
    {
        int step = Mathf.RoundToInt(Mathf.Clamp(progress, 0f, 1f) * ProgressSteps);
        if (step == _progressStep)
            return;
        _progressStep = step;
        _fx.SetProgress((float)step / ProgressSteps);
    }

    /// <summary>Intensité 1 → 0 de l'éclat d'impact : zone pleine qui se défait en trame.</summary>
    public void SetFlash(float intensity)
    {
        float clamped = Mathf.Clamp(intensity, 0f, 1f);
        _fx.SetProgress(1f);
        _fx.SetFillDensity(1f);
        _fx.SetFade(1f - clamped);
        _progressStep = -1;
    }

    public void HideMarker()
    {
        _fx.Stop();
        Visible = false;
    }

    private void Begin(Vector2 origin, in PixelFxSpec baseSpec)
    {
        PixelFxSpec spec = baseSpec;
        spec.FillDensity = AreaDensity;
        spec.ProgressFill = true;
        spec.ZIndex = 0;
        GlobalPosition = origin;
        Visible = true;
        _progressStep = -1;
        _fx.Hold(origin, spec, CombatFxSettings.EnemyOpacity);
        SetProgress(0f);
    }
}
