using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --check-connectivity : les lieux à atteindre (coffres, Mémoriaux, Failles) sont-ils accessibles à pied depuis le
/// départ, une fois les décors posés ? (plan 10 lot D). Grille fine sur toute la carte : bloquée hors des limites, sur
/// le bord effacé, et partout où un corps de la couche des obstacles (décors, Mémoriaux) passe à moins d'un rayon de
/// joueur. Parcours en largeur depuis le joueur ; un lieu est atteint si une case atteinte est à portée d'interaction.
/// Écrit connectivity.png (gris : bloqué, vert : atteint, rouge : lieu inaccessible) et une ligne RESULT.
/// </summary>
public partial class RunObservation
{
    private const float ConnectivityStep = 12f;
    private const uint ObstacleLayer = 4;

    private async Task CheckConnectivity()
    {
        await Frames(30);
        ulong started = Time.GetTicksMsec();
        TileMapLayer ground = _world.GetNode<TileMapLayer>("Ground");
        WorldGenerator generator = _world.Generator;
        int radius = generator.MapRadius;
        Vector2 min = ground.MapToLocal(new Vector2I(-radius, -radius)) - new Vector2(96f, 48f);
        Vector2 max = ground.MapToLocal(new Vector2I(radius, radius)) + new Vector2(96f, 48f);
        int width = Mathf.CeilToInt((max.X - min.X) / ConnectivityStep);
        int height = Mathf.CeilToInt((max.Y - min.Y) / ConnectivityStep);
        bool[] blocked = new bool[width * height];

        // Limites de la carte et bord effacé.
        for (int gy = 0; gy < height; gy++)
        {
            for (int gx = 0; gx < width; gx++)
            {
                Vector2I cell = ground.LocalToMap(min + new Vector2(gx, gy) * ConnectivityStep);
                blocked[gy * width + gx] = !generator.IsWithinBounds(cell.X, cell.Y) || generator.IsErased(cell.X, cell.Y);
            }
        }

        // Obstacles, élargis du rayon du joueur : le centre du joueur ne peut pas s'en approcher davantage.
        float playerRadius = _player.GetNode<CollisionShape2D>("CollisionShape2D").Shape is CircleShape2D circle ? circle.Radius : 12f;
        int shapes = 0;
        foreach (Node node in _world.FindChildren("*", "CollisionShape2D", true, false))
        {
            if (node is not CollisionShape2D { Disabled: false } shape || shape.GetParent() is not CollisionObject2D body
                || (body.CollisionLayer & ObstacleLayer) == 0)
                continue;
            Vector2[] polygon = ShapeOutline(shape);
            if (polygon == null)
                continue;
            BlockAround(blocked, width, height, min, polygon, playerRadius);
            shapes++;
        }

        // Parcours en largeur depuis la case libre la plus proche du joueur.
        int start = NearestFree(blocked, width, height, ToGrid(_player.GlobalPosition, min, width, height));
        bool[] reached = new bool[blocked.Length];
        Queue<int> queue = new();
        if (start >= 0)
        {
            reached[start] = true;
            queue.Enqueue(start);
        }
        int reachedCount = 0;
        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            reachedCount++;
            int x = index % width, y = index / width;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if ((dx == 0 && dy == 0) || nx < 0 || ny < 0 || nx >= width || ny >= height)
                        continue;
                    int next = ny * width + nx;
                    // En diagonale, pas de passage entre deux cases bloquées qui se touchent par le coin.
                    if (reached[next] || blocked[next] || (dx != 0 && dy != 0 && blocked[y * width + nx] && blocked[ny * width + x]))
                        continue;
                    reached[next] = true;
                    queue.Enqueue(next);
                }
            }
        }

        // Lieux à atteindre.
        float reach = _player.InteractRange;
        int total = 0;
        List<string> missing = new();
        List<Vector2> missingPositions = new();
        foreach (IInteractable target in Interactables.All)
        {
            if (!target.CanInteract)
                continue;
            total++;
            if (ReachedNear(reached, width, height, min, target.InteractPosition, reach))
                continue;
            missing.Add($"{((Node)target).GetType().Name}@{target.InteractPosition.X:F0},{target.InteractPosition.Y:F0}");
            missingPositions.Add(target.InteractPosition);
        }

        SaveConnectivityImage(blocked, reached, width, height, min, missingPositions);
        GD.Print(string.Create(CultureInfo.InvariantCulture,
            $"[RunObservation] RESULT connectivity targets={total} unreachable={missing.Count} shapes={shapes} reached_cells={reachedCount} grid={width}x{height} ms={Time.GetTicksMsec() - started} missing=[{string.Join(" ", missing)}]"));
    }

    private static Vector2[] ShapeOutline(CollisionShape2D node)
    {
        Transform2D transform = node.GlobalTransform;
        Vector2[] local = node.Shape switch
        {
            ConvexPolygonShape2D convex => convex.Points,
            RectangleShape2D rect => new[] { -rect.Size / 2f, new Vector2(rect.Size.X, -rect.Size.Y) / 2f, rect.Size / 2f, new Vector2(-rect.Size.X, rect.Size.Y) / 2f },
            CircleShape2D circle => Circle(circle.Radius),
            CapsuleShape2D capsule => Circle(Mathf.Max(capsule.Radius, capsule.Height / 2f)),
            _ => null,
        };
        if (local == null || local.Length < 3)
            return null;
        Vector2[] world = new Vector2[local.Length];
        for (int i = 0; i < local.Length; i++)
            world[i] = transform * local[i];
        return world;
    }

    private static Vector2[] Circle(float radius)
    {
        Vector2[] points = new Vector2[12];
        for (int i = 0; i < points.Length; i++)
            points[i] = Vector2.FromAngle(Mathf.Tau * i / points.Length) * radius;
        return points;
    }

    private static void BlockAround(bool[] blocked, int width, int height, Vector2 min, Vector2[] polygon, float margin)
    {
        Vector2 lo = polygon[0], hi = polygon[0];
        foreach (Vector2 point in polygon)
        {
            lo = lo.Min(point);
            hi = hi.Max(point);
        }
        int x0 = Mathf.Max(0, Mathf.FloorToInt((lo.X - margin - min.X) / ConnectivityStep));
        int x1 = Mathf.Min(width - 1, Mathf.CeilToInt((hi.X + margin - min.X) / ConnectivityStep));
        int y0 = Mathf.Max(0, Mathf.FloorToInt((lo.Y - margin - min.Y) / ConnectivityStep));
        int y1 = Mathf.Min(height - 1, Mathf.CeilToInt((hi.Y + margin - min.Y) / ConnectivityStep));
        float marginSq = margin * margin;
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                Vector2 point = min + new Vector2(x, y) * ConnectivityStep;
                if (Geometry2D.IsPointInPolygon(point, polygon) || DistanceSqToOutline(point, polygon) <= marginSq)
                    blocked[y * width + x] = true;
            }
        }
    }

    private static float DistanceSqToOutline(Vector2 point, Vector2[] polygon)
    {
        float best = float.MaxValue;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 closest = Geometry2D.GetClosestPointToSegment(point, polygon[i], polygon[(i + 1) % polygon.Length]);
            best = Mathf.Min(best, point.DistanceSquaredTo(closest));
        }
        return best;
    }

    private static int ToGrid(Vector2 position, Vector2 min, int width, int height)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt((position.X - min.X) / ConnectivityStep), 0, width - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt((position.Y - min.Y) / ConnectivityStep), 0, height - 1);
        return y * width + x;
    }

    private static int NearestFree(bool[] blocked, int width, int height, int from)
    {
        int fx = from % width, fy = from / width;
        for (int r = 0; r < 20; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    int x = fx + dx, y = fy + dy;
                    if (x >= 0 && y >= 0 && x < width && y < height && !blocked[y * width + x])
                        return y * width + x;
                }
        return -1;
    }

    private static bool ReachedNear(bool[] reached, int width, int height, Vector2 min, Vector2 target, float reach)
    {
        int cells = Mathf.CeilToInt(reach / ConnectivityStep);
        int center = ToGrid(target, min, width, height);
        int cx = center % width, cy = center / width;
        for (int dy = -cells; dy <= cells; dy++)
            for (int dx = -cells; dx <= cells; dx++)
            {
                int x = cx + dx, y = cy + dy;
                if (x < 0 || y < 0 || x >= width || y >= height || !reached[y * width + x])
                    continue;
                if ((min + new Vector2(x, y) * ConnectivityStep).DistanceTo(target) <= reach)
                    return true;
            }
        return false;
    }

    private void SaveConnectivityImage(bool[] blocked, bool[] reached, int width, int height, Vector2 min, List<Vector2> missing)
    {
        Image image = Image.CreateEmpty(width, height, false, Image.Format.Rgb8);
        Color blockedColor = new(0.35f, 0.35f, 0.38f), reachedColor = new(0.25f, 0.55f, 0.3f), freeColor = new(0.1f, 0.1f, 0.12f);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                image.SetPixel(x, y, blocked[index] ? blockedColor : reached[index] ? reachedColor : freeColor);
            }
        foreach (Vector2 position in missing)
        {
            int center = ToGrid(position, min, width, height);
            int cx = center % width, cy = center / width;
            for (int dy = -6; dy <= 6; dy++)
                for (int dx = -6; dx <= 6; dx++)
                    if (cx + dx >= 0 && cy + dy >= 0 && cx + dx < width && cy + dy < height)
                        image.SetPixel(cx + dx, cy + dy, Colors.Red);
        }
        image.SavePng($"{_output}/connectivity.png");
    }
}
