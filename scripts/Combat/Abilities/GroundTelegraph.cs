using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Combat.Abilities;

/// <summary>
/// Annonce au sol d'une attaque ennemie ou d'un événement : zone ou trajectoire en pixel art, au bord irrégulier
/// (sol assombri, front de remplissage selon l'avancement ; plan 25 S10). Détachée du transform de son
/// propriétaire pour rester fixe pendant que l'ennemi bouge. Toujours affichée : seule son opacité se règle.
/// </summary>
public partial class GroundTelegraph : Node2D
{
    /// <summary>Paliers de remplissage : l'annonce avance par poses lisibles, pas en glissement continu.</summary>
    private const int ProgressSteps = 12;
    private const float AreaDensity = 0.25f;
    /// <summary>Opacité d'une flaque (matière liquide) : épaisse, elle se lit comme un sol à éviter.</summary>
    private const float PuddleOpacity = 0.9f;
    private const float RingOpacity = 0.6f;

    private readonly PixelFx _fx;
    private int _progressStep = -1;

    /// <summary>
    /// Annonces de coup affichées (disques et couloirs, pas les simples limites de portée), lues par le bot de mesure
    /// qui les fuit (plan 42 M0d). Coordonnées au sol, comme le dessin.
    /// </summary>
    internal static readonly HashSet<GroundTelegraph> Shown = new();
    internal bool IsLane { get; private set; }
    internal Vector2 Origin => GlobalPosition;
    internal Vector2 GroundEnd { get; private set; }
    internal float GroundSize { get; private set; }
    /// <summary>Annonces successives d'un même marqueur réutilisé : chaque coup est une menace nouvelle.</summary>
    internal int Generation { get; private set; }

    public GroundTelegraph()
    {
        TopLevel = true;
        // Posée au sol : au-dessus du sol (−10) et des routes (−9), sous les corps (0), comme les décalques.
        ZAsRelative = false;
        ZIndex = -1;
        _fx = PixelFx.Create(_ => { });
        AddChild(_fx);
    }

    /// <summary>
    /// Disque au sol de rayon <paramref name="radius"/> : une ellipse deux fois plus large que haute à l'écran. Une annonce
    /// par défaut ; <paramref name="matter"/> en fait une matière posée (flaque).
    /// </summary>
    public void ShowCircle(Vector2 center, float radius, FxFamily family, ZoneMatter matter = ZoneMatter.Mark)
    {
        IsLane = false;
        GroundSize = radius;
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Zone, family, radius, 1f, 1f);
        spec.Squash = Iso.GroundSquash;
        spec.Matter = matter;
        Begin(center, spec);
    }

    /// <summary>Contour seul d'un disque au sol : une limite à connaître (portée), pas un coup qui arrive.</summary>
    public void ShowRing(Vector2 center, float radius, FxFamily family)
    {
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Zone, family, radius, 1f, 1f);
        spec.Squash = Iso.GroundSquash;
        spec.FillDensity = 0f;
        spec.ZIndex = 0;
        Shown.Remove(this);
        GlobalPosition = center;
        Visible = true;
        _progressStep = -1;
        _fx.Hold(center, spec, CombatFxSettings.EnemyOpacity * RingOpacity);
    }

    /// <summary>Couloir au sol entre deux points écran : sa largeur est une largeur au sol, projetée selon sa direction.</summary>
    public void ShowLine(Vector2 from, Vector2 to, float width, FxFamily family)
    {
        Vector2 ground = Iso.ToGround(to - from);
        IsLane = true;
        GroundEnd = ground;
        GroundSize = width;
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Lane, family, ground.Length(), width, 1f);
        spec.Angle = ground.Angle();
        spec.Squash = Iso.GroundSquash;
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
        // Le coup est parti : l'éclat n'est plus une menace à fuir.
        Shown.Remove(this);
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
        Shown.Remove(this);
    }

    public override void _ExitTree() => Shown.Remove(this);

    private void Begin(Vector2 origin, in PixelFxSpec baseSpec)
    {
        PixelFxSpec spec = baseSpec;
        spec.FillDensity = spec.Matter == ZoneMatter.Liquid ? PuddleOpacity : AreaDensity;
        spec.ProgressFill = true;
        spec.ZIndex = 0;
        GlobalPosition = origin;
        Visible = true;
        _progressStep = -1;
        _fx.Hold(origin, spec, CombatFxSettings.EnemyOpacity);
        SetProgress(0f);
        Generation++;
        Shown.Add(this);
    }
}
