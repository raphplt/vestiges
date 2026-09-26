using Godot;

namespace Vestiges.Core;

/// <summary>
/// Projection de la vue isométrique 2:1 : le sol est couché, une longueur au sol vaut sa moitié à l'écran en vertical.
/// « Au sol » = repère où les distances sont vraies (y doublé) ; « écran » = coordonnées du monde Godot.
/// Une forme posée au sol se calcule au sol puis se projette ; un cercle au sol devient une ellipse deux fois plus large.
/// </summary>
public static class Iso
{
    /// <summary>Rapport hauteur/largeur d'une forme posée au sol (même valeur que le paramètre squash de pixel_fx).</summary>
    public const float GroundSquash = 2f;

    public static Vector2 ToGround(Vector2 screen) => new(screen.X, screen.Y * GroundSquash);

    public static Vector2 ToScreen(Vector2 ground) => new(ground.X, ground.Y / GroundSquash);

    /// <summary>Distance au carré mesurée au sol : un rayon au sol touche sur l'ellipse dessinée, pas sur un cercle écran.</summary>
    public static float GroundDistanceSquared(Vector2 a, Vector2 b) => ToGround(b - a).LengthSquared();

    /// <summary>Distance au sol d'un point au segment [a, b] (points en coordonnées écran).</summary>
    public static float GroundDistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 p = ToGround(point);
        Vector2 start = ToGround(a);
        Vector2 ab = ToGround(b) - start;
        float t = ab.LengthSquared() > 0f ? Mathf.Clamp((p - start).Dot(ab) / ab.LengthSquared(), 0f, 1f) : 0f;
        return p.DistanceTo(start + ab * t);
    }
}
