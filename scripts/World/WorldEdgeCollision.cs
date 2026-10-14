using Godot;

namespace Vestiges.World;

/// <summary>
/// Le bord du monde devient un mur physique lisse, sur la couche des décors : le joueur y glisse comme le long d'un
/// décor au lieu de s'y figer. La bordure effilochée (trous tirés au hasard dans <c>edge_fade_width</c>) formerait
/// des dents de scie et des recoins où toute poussée oblique se coince ; le mur est donc l'ellipse de la carte, posée
/// juste avant la première cellule qui peut être dissoute. La bande effilochée reste visible au-delà.
/// </summary>
public static class WorldEdgeCollision
{
    private const uint PropCollisionLayer = 4;
    // Des segments de ~60 px : l'écart à l'ellipse vraie reste sous le dixième de pixel.
    private const int Segments = 1024;

    /// <summary>
    /// Demi-axes en pixels de l'ellipse <see cref="WorldGenerator.EllipseDistance"/> = <paramref name="radius"/> :
    /// une colonne avance d'une tuile, un rang d'une demi-hauteur de tuile (grille iso en quinconce).
    /// </summary>
    public static Vector2 SemiAxes(TileMapLayer ground, WorldGenerator generator, float radius)
    {
        Vector2 tile = ground.TileSet.TileSize;
        return new Vector2(tile.X * radius, tile.Y * 0.5f * radius * generator.MapRadiusY / generator.MapRadiusX);
    }

    /// <summary>Crée le corps « WorldEdge » sous <paramref name="parent"/>, ellipse en coordonnées locales du sol.</summary>
    public static void Build(Node2D parent, TileMapLayer ground, Vector2 center, Vector2 semiAxes)
    {
        Vector2[] points = new Vector2[Segments * 2];
        Vector2 previous = center + new Vector2(semiAxes.X, 0f);
        for (int i = 1; i <= Segments; i++)
        {
            float angle = Mathf.Tau * i / Segments;
            Vector2 next = center + new Vector2(Mathf.Cos(angle) * semiAxes.X, Mathf.Sin(angle) * semiAxes.Y);
            points[(i - 1) * 2] = previous;
            points[(i - 1) * 2 + 1] = next;
            previous = next;
        }

        StaticBody2D body = new() { Name = "WorldEdge", CollisionLayer = PropCollisionLayer, CollisionMask = 0 };
        body.AddChild(new CollisionShape2D { Shape = new ConcavePolygonShape2D { Segments = points } });
        parent.AddChild(body);
        body.GlobalPosition = ground.GlobalPosition;
    }

    /// <summary>Vrai hors de l'ellipse : <paramref name="local"/> en coordonnées locales du sol.</summary>
    public static bool IsBeyond(Vector2 local, Vector2 center, Vector2 semiAxes)
    {
        Vector2 offset = (local - center) / semiAxes;
        return offset.LengthSquared() > 1f;
    }
}
