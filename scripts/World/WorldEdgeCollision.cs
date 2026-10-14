using System;
using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Le vide du bord du monde (cellules hors carte ou dissoutes à la génération) devient un mur physique, sur la couche
/// des décors : le joueur y glisse comme le long d'un décor au lieu de s'y figer. Le mur suit le bord des tuiles de sol
/// (marching squares sur le réseau des centres de cellules), puis ses marches sont lissées en segments droits : sur
/// des losanges, un bord en biais est un escalier dont chaque marche arrête net une poussée oblique.
/// La grille vide ne change plus après la génération.
/// </summary>
public static class WorldEdgeCollision
{
    private const uint PropCollisionLayer = 4;
    // Le mur coupe chaque trajet sol → vide aux 35 % (le bord du losange est à 50 %) : un coin de cellule vide qui pointe
    // vers le sol reste hors d'atteinte du centre du joueur, et le mur reste à ~4 px du bord des tuiles.
    private const float WallFromGround = 0.35f;
    // Un segment lissé s'écarte d'au plus ceci du contour des tuiles, d'un côté comme de l'autre.
    private const float MaxWallShift = 12f;
    // Rayon du joueur (12 px) moins une marge : un coin de cellule vide plus loin que ceci côté sol serait atteignable.
    // Le contour brut laisse déjà les coins vides d'un bord vertical à 11,2 px côté sol.
    private const float PlayerReach = 11.5f;

    private readonly record struct EdgePoint(Vector2 Position, Vector2 VoidCenter);

    /// <summary>Crée le corps « WorldEdge » sous <paramref name="parent"/>, autour du sol de la carte générée.</summary>
    public static void Build(Node2D parent, TileMapLayer ground, WorldGenerator generator)
    {
        // Un rang de marge : le dernier sol peut toucher une cellule hors de la grille générée.
        Rect2I cells = new(-generator.MapRadiusX - 1, -generator.MapRadiusY - 1,
            generator.MapRadiusX * 2 + 3, generator.MapRadiusY * 2 + 3);
        Build(parent, ground, cells, (x, y) => !generator.IsErased(x, y));
    }

    /// <summary>Mur autour des cellules où <paramref name="isGround"/> est vrai, dans <paramref name="cells"/>.</summary>
    public static StaticBody2D Build(Node2D parent, TileMapLayer ground, Rect2I cells, Func<int, int, bool> isGround)
    {
        // Chaque passage sol → vide du contour est indexé par son arête (cellule de sol, cellule vide) : deux losanges
        // du réseau qui partagent cette arête retrouvent le même point, ce qui referme les contours.
        Dictionary<(Vector2I, Vector2I), (Vector2I, Vector2I)> next = new();
        // Hors du rectangle, tout est vide : les contours se referment. Les losanges du réseau débordent d'un rang pour
        // couvrir toutes les arêtes d'un sol du bord du rectangle.
        Func<int, int, bool> isInsideGround = (x, y) => cells.HasPoint(new Vector2I(x, y)) && isGround(x, y);
        for (int x = cells.Position.X - 1; x <= cells.End.X; x++)
        {
            for (int y = cells.Position.Y - 2; y <= cells.End.Y; y++)
                AddCellSegments(next, isInsideGround, x, y);
        }

        Vector2 halfTile = (Vector2)ground.TileSet.TileSize * 0.5f;
        List<Vector2> segments = new();
        HashSet<(Vector2I, Vector2I)> visited = new();
        List<EdgePoint> loop = new();
        foreach ((Vector2I, Vector2I) start in next.Keys)
        {
            if (visited.Contains(start))
                continue;
            loop.Clear();
            (Vector2I, Vector2I) current = start;
            while (visited.Add(current))
            {
                Vector2 groundCenter = ground.MapToLocal(current.Item1);
                Vector2 voidCenter = ground.MapToLocal(current.Item2);
                loop.Add(new EdgePoint(groundCenter.Lerp(voidCenter, WallFromGround), voidCenter));
                current = next[current];
            }
            Simplify(loop, halfTile, segments);
        }

        StaticBody2D body = new() { Name = "WorldEdge", CollisionLayer = PropCollisionLayer, CollisionMask = 0 };
        if (segments.Count > 0)
            body.AddChild(new CollisionShape2D { Shape = new ConcavePolygonShape2D { Segments = segments.ToArray() } });
        parent.AddChild(body);
        body.GlobalPosition = ground.GlobalPosition;
        return body;
    }

    /// <summary>
    /// Losange du réseau dont (<paramref name="x"/>, <paramref name="y"/>) est le sommet haut : ses quatre sommets sont
    /// des centres de cellules voisines par une arête (grille iso en quinconce, rang impair décalé d'une demi-tuile).
    /// Ses côtés se parcourent dans le sens horaire (haut, droite, bas, gauche) ; le contour va du passage vide → sol
    /// au passage sol → vide suivant, le sol à sa gauche à l'écran.
    /// </summary>
    private static void AddCellSegments(Dictionary<(Vector2I, Vector2I), (Vector2I, Vector2I)> next,
        Func<int, int, bool> isGround, int x, int y)
    {
        int left = (y & 1) != 0 ? x : x - 1;
        Span<Vector2I> corners = [new(x, y), new(left + 1, y + 1), new(x, y + 2), new(left, y + 1)];
        Span<bool> isCornerGround = stackalloc bool[4];
        int groundCount = 0;
        for (int i = 0; i < 4; i++)
        {
            isCornerGround[i] = isGround(corners[i].X, corners[i].Y);
            if (isCornerGround[i])
                groundCount++;
        }
        if (groundCount == 0 || groundCount == 4)
            return;

        // Chaque entrée dans le sol se relie à la sortie qui suit autour du même sol. Deux sols opposés par la pointe
        // gardent ainsi chacun leur mur : le passage en diagonale reste fermé.
        for (int i = 0; i < 4; i++)
        {
            int after = (i + 1) & 3;
            if (isCornerGround[i] || !isCornerGround[after])
                continue;
            int exit = after;
            while (isCornerGround[(exit + 1) & 3])
                exit = (exit + 1) & 3;
            next[(corners[after], corners[i])] = (corners[exit], corners[(exit + 1) & 3]);
        }
    }

    /// <summary>
    /// Remplace les marches d'un contour fermé (sol à gauche) par les plus longs segments droits possibles : chaque
    /// point sauté reste à <see cref="MaxWallShift"/> px du segment, et aucun coin des cellules vides longées ne passe
    /// côté sol au-delà de <see cref="PlayerReach"/>, où le centre du joueur pourrait l'atteindre.
    /// </summary>
    private static void Simplify(List<EdgePoint> loop, Vector2 halfTile, List<Vector2> segments)
    {
        int count = loop.Count;
        int from = 0;
        while (from < count)
        {
            int to = from + 1;
            for (int candidate = from + 2; candidate <= count; candidate++)
            {
                if (!Covers(loop, from, candidate, halfTile))
                    break;
                to = candidate;
            }
            segments.Add(loop[from].Position);
            segments.Add(loop[to % count].Position);
            from = to;
        }
    }

    private static bool Covers(List<EdgePoint> loop, int from, int to, Vector2 halfTile)
    {
        Vector2 a = loop[from].Position;
        Vector2 direction = loop[to % loop.Count].Position - a;
        float length = direction.Length();
        if (length < 0.001f)
            return false;
        Vector2 unit = direction / length;
        for (int i = from + 1; i < to; i++)
        {
            if (Mathf.Abs(unit.Cross(loop[i].Position - a)) > MaxWallShift)
                return false;
        }
        Span<Vector2> corners = [new(-halfTile.X, 0f), new(halfTile.X, 0f), new(0f, -halfTile.Y), new(0f, halfTile.Y)];
        for (int i = from; i <= to; i++)
        {
            Vector2 voidCenter = loop[i % loop.Count].VoidCenter;
            foreach (Vector2 corner in corners)
            {
                Vector2 offset = voidCenter + corner - a;
                float along = unit.Dot(offset);
                // Écran, y vers le bas : le sol est à gauche du trajet, où le produit vectoriel est négatif.
                if (along >= 0f && along <= length && -unit.Cross(offset) > PlayerReach)
                    return false;
            }
        }
        return true;
    }
}
