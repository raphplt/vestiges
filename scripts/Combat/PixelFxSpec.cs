namespace Vestiges.Combat;

/// <summary>Description d'un effet de forme. Les distances sont en texels du monde.</summary>
public struct PixelFxSpec
{
    public PixelFxShape Shape;
    public FxFamily Family;
    public float Angle;
    public float Radius;
    public float Thickness;
    public float ArcHalf;
    public float Squash;
    public float Duration;
    public int Steps;
    /// <summary>Part finale de la durée pendant laquelle l'effet s'efface par tramage.</summary>
    public float FadeTail;
    public float FillDensity;
    /// <summary>Zone : un disque se remplit avec la progression (annonce d'un impact à venir).</summary>
    public bool ProgressFill;
    public int ZIndex;
    /// <summary>Rejoue la progression en boucle jusqu'à Stop() (effets entretenus, comme un cône continu).</summary>
    public bool Loop;

    public static PixelFxSpec Of(PixelFxShape shape, FxFamily family, float radius, float thickness, float duration)
    {
        return new PixelFxSpec
        {
            Shape = shape,
            Family = family,
            Radius = radius,
            Thickness = thickness,
            ArcHalf = 1f,
            Squash = 1f,
            Duration = duration,
            Steps = 6,
            FadeTail = 0.3f,
            FillDensity = 0.35f,
            ProgressFill = true,
            ZIndex = 1,
        };
    }
}
