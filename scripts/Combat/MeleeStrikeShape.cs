using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Forme de touche d'un coup de mêlée, la même que son dessin (DECISIONS §99, §100). L'attaque droite est un trait
/// vers la cible : elle touche une créature dont le corps recouvre le trait. L'arc et l'onde sont des traînées
/// aplaties (<see cref="Iso.SlashSquash"/>) : ils touchent jusqu'à leur bord, dans leur ouverture mesurée sur l'ellipse.
/// </summary>
public static class MeleeStrikeShape
{
    /// <summary>Demi-épaisseur du trait d'estoc dessiné (<c>PlayerAttackFx.PlayThrust</c>).</summary>
    public const float ThrustHalfWidth = 3.5f;

    public static bool IsThrust(AttackPatternKind pattern) => pattern == AttackPatternKind.Linear;

    /// <summary>Aplatissement de la forme : 1 pour le trait, celui des traînées sinon.</summary>
    public static float Squash(AttackPatternKind pattern) => IsThrust(pattern) ? 1f : Iso.SlashSquash;

    /// <summary>Écart en degrés entre la direction du coup et <paramref name="toEnemy"/>, mesuré sur l'ellipse de la traînée.</summary>
    public static float OffsetDegrees(Vector2 direction, Vector2 toEnemy, float squash) =>
        Mathf.RadToDeg(ToSlash(direction, squash).AngleTo(ToSlash(toEnemy, squash)));

    /// <summary>Direction écran tournée de <paramref name="degrees"/> sur l'ellipse : c'est ainsi que les frappes en éventail sont dessinées.</summary>
    public static Vector2 RotateOnSlash(Vector2 direction, float degrees, float squash)
    {
        Vector2 slash = ToSlash(direction, squash).Rotated(Mathf.DegToRad(degrees));
        return new Vector2(slash.X, slash.Y / squash).Normalized();
    }

    /// <summary>
    /// Le trait d'un estoc long de <paramref name="range"/> recouvre le corps de la créature, dont le centre tombe sur
    /// la longueur du trait : comme pour l'arc, l'allonge se compte au centre de la cible.
    /// </summary>
    public static bool ThrustTouches(Vector2 direction, Vector2 toEnemy, float range, float bodyRadius)
    {
        float along = toEnemy.Dot(direction);
        if (along < 0f || along > range)
            return false;
        float reach = bodyRadius + ThrustHalfWidth;
        return (toEnemy - direction * along).LengthSquared() <= reach * reach;
    }

    private static Vector2 ToSlash(Vector2 screen, float squash) => new(screen.X, screen.Y * squash);
}
