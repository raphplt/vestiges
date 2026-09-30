using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Chemins de terre entre les régions de la mosaïque (plan 10 T3).
/// Un arbre couvrant relie les centres des régions voisines, plus quelques boucles et le point de départ ;
/// chaque liaison est tracée par A* sur la grille, avec un coût qui suit le terrain : l'eau et les immeubles
/// sont infranchissables, les rues et les chemins déjà tracés sont bon marché (les chemins rejoignent les rues
/// et se raccordent entre eux), un bruit lent les fait serpenter. Les tronçons sur les rues ne sont pas redessinés.
/// Calcul CPU au chargement uniquement.
/// </summary>
public static class PathNetworkGenerator
{
    private const float EdgeStep = 45.254834f;   // voisin par une arête : (±32, ±32) au sol
    private const float CornerStep = 64f;        // voisin par un coin : (±64, 0) ou (0, ±64) au sol
    private const float HeuristicWeight = 0.5f;
    private const int ChaikinPasses = 3;
    private const int StyleSmoothing = 6;
    private const float TaperPx = 72f;
    private const float WobbleEnvelopePx = 60f;

    private static readonly int[] EvenRowEdge = { -1, -1, 0, -1, -1, 1, 0, 1 };
    private static readonly int[] OddRowEdge = { 0, -1, 1, -1, 0, 1, 1, 1 };
    private static readonly int[] Corner = { -1, 0, 1, 0, 0, -2, 0, 2 };

    public static PathNetwork Build(WorldGenerator generator, TerrainType[,] terrain, UrbanLayout urban,
                                    WildFieldsLayout fields, PathNetworkConfig config, float regionSpacing, ulong seed)
    {
        PathNetwork network = new();
        if (!config.Enabled)
            return network;

        ulong started = Time.GetTicksMsec();
        Router router = new(generator, terrain, urban, fields, config, seed);
        List<int> nodes = new();
        List<Vector2> nodeCenters = new();
        foreach (Vector2 center in generator.BiomeRegionCenters)
        {
            Vector2I cell = new(Mathf.RoundToInt(center.X), Mathf.RoundToInt(center.Y));
            int node = router.NearestPassable(cell, 8, preferRoad: true);
            if (node < 0 || nodes.Contains(node))
                continue;
            nodes.Add(node);
            nodeCenters.Add(center);
        }
        int spawnNode = router.NearestPassable(Vector2I.Zero, 6, preferRoad: false);
        if (nodes.Count < 2)
            return network;

        List<(int A, int B)> links = PlanLinks(nodes, nodeCenters, spawnNode, config, regionSpacing, seed);
        Dictionary<int, int> degree = new();
        List<List<int>> segments = new();
        foreach ((int a, int b) in links)
        {
            List<int> route = router.FindRoute(a, b);
            if (route == null)
                continue;
            degree[a] = degree.GetValueOrDefault(a) + 1;
            degree[b] = degree.GetValueOrDefault(b) + 1;
            router.SplitIntoSegments(route, segments);
            router.Claim(route);
        }

        PathStyle[] styles = BiomeStyles(generator, config);
        foreach (List<int> segment in segments)
        {
            bool startTaper = degree.GetValueOrDefault(segment[0]) == 1;
            bool endTaper = degree.GetValueOrDefault(segment[^1]) == 1;
            PathStroke stroke = BuildStroke(router, generator, segment, styles, config, startTaper, endTaper, seed);
            if (stroke == null)
                continue;
            network.Strokes.Add(stroke);
            MarkCells(stroke, network.Cells);
        }

        GD.Print($"[PathNetwork] {nodes.Count} régions, {links.Count} liaisons, {network.Strokes.Count} tronçons, " +
                 $"{network.Cells.Count} cellules, {router.Expansions} cellules explorées, {Time.GetTicksMsec() - started} ms");
        return network;
    }

    /// <summary>Arbre couvrant des régions voisines (Kruskal), boucles tirées par hash, liaisons du départ.</summary>
    private static List<(int, int)> PlanLinks(List<int> nodes, List<Vector2> centers, int spawnNode,
                                              PathNetworkConfig config, float spacing, ulong seed)
    {
        float neighbourSq = spacing * config.NeighbourFactor * spacing * config.NeighbourFactor;
        float extraSq = spacing * config.ExtraEdgeFactor * spacing * config.ExtraEdgeFactor;
        List<(float Distance, int I, int J)> candidates = new();
        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = i + 1; j < nodes.Count; j++)
            {
                float distanceSq = centers[i].DistanceSquaredTo(centers[j]);
                if (distanceSq <= neighbourSq)
                    candidates.Add((distanceSq, i, j));
            }
        }
        candidates.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        int[] parent = new int[nodes.Count];
        for (int i = 0; i < parent.Length; i++)
            parent[i] = i;
        int Find(int i)
        {
            while (parent[i] != i)
                i = parent[i] = parent[parent[i]];
            return i;
        }

        List<(int, int)> tree = new();
        List<(int, int)> extras = new();
        foreach ((float distanceSq, int i, int j) in candidates)
        {
            int ri = Find(i);
            int rj = Find(j);
            if (ri != rj)
            {
                parent[ri] = rj;
                tree.Add((nodes[i], nodes[j]));
            }
            else if (distanceSq <= extraSq && CellHash.Of(i, j, seed ^ 0x9A7D5UL) % 1000 < (uint)(config.ExtraEdgeChance * 1000f))
            {
                extras.Add((nodes[i], nodes[j]));
            }
        }

        // Une région isolée (plus proche voisine au-delà du voisinage) est rattachée à la région la plus proche d'une autre composante.
        while (true)
        {
            int root = Find(0);
            float best = float.MaxValue;
            int bestI = -1;
            int bestJ = -1;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (Find(i) == root)
                    continue;
                for (int j = 0; j < nodes.Count; j++)
                {
                    if (Find(j) != root)
                        continue;
                    float distanceSq = centers[i].DistanceSquaredTo(centers[j]);
                    if (distanceSq < best)
                    {
                        best = distanceSq;
                        bestI = i;
                        bestJ = j;
                    }
                }
            }
            if (bestI < 0)
                break;
            parent[Find(bestI)] = root;
            tree.Add((nodes[bestI], nodes[bestJ]));
        }

        List<(int, int)> links = new(tree);
        if (spawnNode >= 0)
        {
            List<int> nearest = new();
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i] != spawnNode)
                    nearest.Add(i);
            nearest.Sort((a, b) => centers[a].LengthSquared().CompareTo(centers[b].LengthSquared()));
            for (int k = 0; k < Math.Min(config.SpawnLinks, nearest.Count); k++)
                links.Add((spawnNode, nodes[nearest[k]]));
        }
        links.AddRange(extras);
        return links;
    }

    private static PathStyle[] BiomeStyles(WorldGenerator generator, PathNetworkConfig config)
    {
        PathStyle[] styles = new PathStyle[generator.ActiveBiomes.Count];
        for (int i = 0; i < styles.Length; i++)
            styles[i] = generator.ActiveBiomes[i].PathStyle ?? config.DefaultStyle;
        return styles;
    }

    private static PathStroke BuildStroke(Router router, WorldGenerator generator, List<int> cells, PathStyle[] styles,
                                          PathNetworkConfig config, bool startTaper, bool endTaper, ulong seed)
    {
        if (cells.Count < 2)
            return null;
        List<Vector2> points = new(cells.Count);
        foreach (int cell in cells)
            points.Add(CellCenter(router.CellOf(cell)));
        for (int pass = 0; pass < ChaikinPasses; pass++)
            points = Chaikin(points);

        int count = points.Count;
        float[] arc = new float[count];
        for (int i = 1; i < count; i++)
            arc[i] = arc[i - 1] + Iso.ToGround(points[i] - points[i - 1]).Length();
        float length = arc[count - 1];
        if (length < 24f)
            return null;

        // Ondulation latérale au sol, nulle aux extrémités pour que les raccords restent en place.
        FastNoiseLite wobble = new()
        {
            Seed = (int)(CellHash.Of(cells[0], cells[^1], seed) & 0x7FFFFFFF),
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            Frequency = 0.012f,
        };
        Vector2[] placed = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            float envelope = Mathf.Clamp(Mathf.Min(arc[i], length - arc[i]) / WobbleEnvelopePx, 0f, 1f);
            Vector2 normal = GroundNormal(points, i);
            placed[i] = points[i] + Iso.ToScreen(normal * wobble.GetNoise1D(arc[i]) * config.WobblePx * envelope);
        }

        float[] widths = new float[count];
        Color[] colors = new Color[count];
        for (int i = 0; i < count; i++)
        {
            Vector2I cell = CellAt(placed[i]);
            int biome = generator.GetBiomeIndex(cell.X, cell.Y);
            PathStyle style = biome >= 0 && biome < styles.Length ? styles[biome] : config.DefaultStyle;
            widths[i] = style.WidthPx;
            colors[i] = new Color(style.Tone, style.Ruts);
        }
        widths = Smooth(widths);
        colors = Smooth(colors);

        for (int i = 0; i < count; i++)
        {
            if (startTaper)
                widths[i] *= Mathf.Lerp(0.3f, 1f, Mathf.Clamp(arc[i] / TaperPx, 0f, 1f));
            if (endTaper)
                widths[i] *= Mathf.Lerp(0.3f, 1f, Mathf.Clamp((length - arc[i]) / TaperPx, 0f, 1f));
        }

        return new PathStroke { Points = placed, Widths = widths, Styles = colors };
    }

    /// <summary>
    /// Embranchement d'un point (cour de ferme) vers un chemin : courbe de Bézier qui passe près de <paramref name="via"/>
    /// (la sortie de la cour), plus étroite que le chemin, qui s'élargit en le rejoignant. Ajouté avant les maillages.
    /// </summary>
    public static void AddSpur(PathNetwork network, Vector2 from, Vector2 via, Vector2 to, PathStyle style, float widthFactor)
    {
        float length = Iso.ToGround(to - from).Length();
        if (length < 24f)
            return;
        Vector2 control = via;
        int count = Math.Max(3, Mathf.CeilToInt(length / 10f));
        Vector2[] points = new Vector2[count];
        float[] widths = new float[count];
        Color[] styles = new Color[count];
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            points[i] = from.Lerp(control, t).Lerp(control.Lerp(to, t), t);
            widths[i] = style.WidthPx * Mathf.Lerp(widthFactor * 0.8f, 1f, t * t);
            styles[i] = new Color(style.Tone, style.Ruts);
        }
        PathStroke stroke = new() { Points = points, Widths = widths, Styles = styles };
        network.Strokes.Add(stroke);
        MarkCells(stroke, network.Cells);
    }

    /// <summary>Point de chemin le plus proche (distance au sol) plus bas que <paramref name="minY"/> à l'écran, ou false si aucun n'est à portée.</summary>
    public static bool TryNearestPoint(PathNetwork network, Vector2 from, float reach, float minY, out Vector2 nearest)
    {
        nearest = Vector2.Zero;
        float best = reach * reach;
        bool found = false;
        foreach (PathStroke stroke in network.Strokes)
        {
            foreach (Vector2 point in stroke.Points)
            {
                if (point.Y < minY)
                    continue;
                float distance = Iso.GroundDistanceSquared(from, point);
                if (distance < best)
                {
                    best = distance;
                    nearest = point;
                    found = true;
                }
            }
        }
        return found;
    }

    private static void MarkCells(PathStroke stroke, HashSet<Vector2I> cells)
    {
        for (int i = 0; i < stroke.Points.Length; i++)
        {
            Vector2 side = Iso.ToScreen(GroundNormal(stroke.Points, i) * stroke.Widths[i] * 0.5f);
            cells.Add(CellAt(stroke.Points[i]));
            cells.Add(CellAt(stroke.Points[i] + side));
            cells.Add(CellAt(stroke.Points[i] - side));
        }
    }

    /// <summary>Normale unitaire du tracé au point i, dans le repère au sol (la profondeur de l'écran dépliée ×2).</summary>
    public static Vector2 GroundNormal(IReadOnlyList<Vector2> points, int i)
    {
        Vector2 previous = points[Math.Max(0, i - 1)];
        Vector2 next = points[Math.Min(points.Count - 1, i + 1)];
        Vector2 tangent = Iso.ToGround(next - previous).Normalized();
        return new Vector2(-tangent.Y, tangent.X);
    }

    /// <summary>Grille isométrique « stacked » : centre de (x, y) = (64x + 32·(y impair) + 32, 16y + 16), comme ground.gdshader.</summary>
    public static Vector2 CellCenter(Vector2I cell)
    {
        return new Vector2(cell.X * 64f + ((cell.Y & 1) != 0 ? 32f : 0f) + 32f, cell.Y * 16f + 16f);
    }

    /// <summary>Cellule dont le losange contient le point : l'un des deux rangs qui l'encadrent.</summary>
    public static Vector2I CellAt(Vector2 p)
    {
        int row = Mathf.FloorToInt((p.Y - 16f) / 16f);
        Vector2I best = Vector2I.Zero;
        float bestDistance = float.MaxValue;
        for (int k = 0; k < 2; k++)
        {
            int r = row + k;
            float offset = (r & 1) != 0 ? 32f : 0f;
            Vector2I c = new(Mathf.RoundToInt((p.X - 32f - offset) / 64f), r);
            Vector2 d = (p - CellCenter(c)).Abs();
            float distance = d.X / 32f + d.Y / 16f;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = c;
            }
        }
        return best;
    }

    private static List<Vector2> Chaikin(List<Vector2> points)
    {
        List<Vector2> result = new(points.Count * 2) { points[0] };
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[i + 1];
            result.Add(a.Lerp(b, 0.25f));
            result.Add(a.Lerp(b, 0.75f));
        }
        result.Add(points[^1]);
        return result;
    }

    private static float[] Smooth(float[] values)
    {
        float[] result = new float[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            int from = Math.Max(0, i - StyleSmoothing);
            int to = Math.Min(values.Length - 1, i + StyleSmoothing);
            float sum = 0f;
            for (int k = from; k <= to; k++)
                sum += values[k];
            result[i] = sum / (to - from + 1);
        }
        return result;
    }

    private static Color[] Smooth(Color[] values)
    {
        Color[] result = new Color[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            int from = Math.Max(0, i - StyleSmoothing);
            int to = Math.Min(values.Length - 1, i + StyleSmoothing);
            Vector4 sum = Vector4.Zero;
            for (int k = from; k <= to; k++)
                sum += new Vector4(values[k].R, values[k].G, values[k].B, values[k].A);
            sum /= to - from + 1;
            result[i] = new Color(sum.X, sum.Y, sum.Z, sum.W);
        }
        return result;
    }

    /// <summary>Grille de coûts et A* réutilisable (tableaux alloués une fois, remis à zéro par un tampon de passage).</summary>
    private sealed class Router
    {
        private const float Impassable = -1f;

        private readonly WorldGenerator _generator;
        private readonly PathNetworkConfig _config;
        private readonly int _radius;
        private readonly int _radiusY;
        private readonly int _size;
        private readonly int _sizeY;
        private readonly float[] _cost;
        private readonly bool[] _road;
        private readonly bool[] _claimed;
        private readonly float[] _g;
        private readonly int[] _parent;
        private readonly int[] _stamp;
        private readonly PriorityQueue<int, float> _open = new();
        private int _pass;

        public long Expansions { get; private set; }

        public Router(WorldGenerator generator, TerrainType[,] terrain, UrbanLayout urban, WildFieldsLayout fields,
                      PathNetworkConfig config, ulong seed)
        {
            _generator = generator;
            _config = config;
            _radius = generator.MapRadiusX;
            _radiusY = generator.MapRadiusY;
            _size = _radius * 2 + 1;
            _sizeY = _radiusY * 2 + 1;
            int total = _size * _sizeY;
            _cost = new float[total];
            _road = new bool[total];
            _claimed = new bool[total];
            _g = new float[total];
            _parent = new int[total];
            _stamp = new int[total];

            FastNoiseLite noise = new()
            {
                Seed = (int)((seed ^ 0x7A745UL) & 0x7FFFFFFF),
                NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
                Frequency = config.NoiseFrequency,
            };
            float limit = _radius - config.EdgeMargin;
            for (int gy = 0; gy < _sizeY; gy++)
            {
                for (int gx = 0; gx < _size; gx++)
                {
                    int x = gx - _radius;
                    int y = gy - _radiusY;
                    int index = gy * _size + gx;
                    _cost[index] = Impassable;
                    if (generator.EllipseDistance(x, y) > limit || !generator.IsWithinBounds(x, y) || generator.IsErased(x, y))
                        continue;
                    TerrainType type = terrain[gx, gy];
                    if (type == TerrainType.Water)
                        continue;

                    // Grille « stacked » : un rang vaut une demi-colonne au sol, le bruit est échantillonné au sol.
                    float wander = Mathf.Max(0.2f, 1f + config.NoiseAmplitude * noise.GetNoise2D(x, y * 0.5f));
                    UrbanCellType urbanType = urban?.CellGrid[gx, gy] ?? UrbanCellType.None;
                    switch (urbanType)
                    {
                        case UrbanCellType.Road:
                            _cost[index] = config.RoadCost;
                            _road[index] = true;
                            continue;
                        case UrbanCellType.BuildingInterior:
                        case UrbanCellType.BuildingWall:
                            continue;
                        case UrbanCellType.Sidewalk:
                            _cost[index] = config.SidewalkCost;
                            continue;
                        case UrbanCellType.Plaza:
                            _cost[index] = config.PlazaCost;
                            continue;
                    }

                    if (fields != null && fields.CellGrid[gx, gy] == WildFieldCellType.Path)
                        _cost[index] = config.FieldLaneCost * wander;
                    else if (type == TerrainType.Forest)
                        _cost[index] = config.ForestCost * wander;
                    else
                        _cost[index] = wander;
                }
            }
        }

        public Vector2I CellOf(int index) => new(index % _size - _radius, index / _size - _radiusY);

        private int IndexOf(int x, int y)
        {
            int gx = x + _radius;
            int gy = y + _radiusY;
            if (gx < 0 || gy < 0 || gx >= _size || gy >= _sizeY)
                return -1;
            return gy * _size + gx;
        }

        /// <summary>Cellule franchissable la plus proche (une rue de préférence, pour entrer en ville par la chaussée).</summary>
        public int NearestPassable(Vector2I around, int reach, bool preferRoad)
        {
            int best = -1;
            int bestRoad = -1;
            float bestDistance = float.MaxValue;
            float bestRoadDistance = float.MaxValue;
            for (int dy = -reach * 2; dy <= reach * 2; dy++)
            {
                for (int dx = -reach; dx <= reach; dx++)
                {
                    int index = IndexOf(around.X + dx, around.Y + dy);
                    if (index < 0 || _cost[index] < 0f)
                        continue;
                    float distance = Iso.ToGround(CellCenter(new Vector2I(around.X + dx, around.Y + dy)) - CellCenter(around)).LengthSquared();
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = index;
                    }
                    if (_road[index] && distance < bestRoadDistance)
                    {
                        bestRoadDistance = distance;
                        bestRoad = index;
                    }
                }
            }
            return preferRoad && bestRoad >= 0 ? bestRoad : best;
        }

        public List<int> FindRoute(int start, int goal)
        {
            _pass++;
            _open.Clear();
            Vector2 goalCenter = CellCenter(CellOf(goal));
            float minCost = Mathf.Min(Mathf.Min(_config.RoadCost, _config.ExistingPathCost), _config.FieldLaneCost);
            float heuristicScale = minCost * HeuristicWeight;

            _stamp[start] = _pass;
            _g[start] = 0f;
            _parent[start] = -1;
            _open.Enqueue(start, 0f);
            int expansions = 0;
            while (_open.TryDequeue(out int current, out float priority))
            {
                if (current == goal)
                    break;
                Vector2I cell = CellOf(current);
                float h = Iso.ToGround(CellCenter(cell) - goalCenter).Length() * heuristicScale;
                if (priority > _g[current] + h + 0.001f)
                    continue;
                if (++expansions > _config.MaxExpansions)
                {
                    Expansions += expansions;
                    return null;
                }

                int[] edge = (cell.Y & 1) != 0 ? OddRowEdge : EvenRowEdge;
                for (int k = 0; k < 8; k += 2)
                {
                    Relax(current, cell.X + edge[k], cell.Y + edge[k + 1], EdgeStep, goalCenter, heuristicScale);
                    Relax(current, cell.X + Corner[k], cell.Y + Corner[k + 1], CornerStep, goalCenter, heuristicScale);
                }
            }
            Expansions += expansions;
            if (_stamp[goal] != _pass)
                return null;

            List<int> route = new();
            for (int at = goal; at >= 0; at = _parent[at])
                route.Add(at);
            route.Reverse();
            return route;
        }

        private void Relax(int from, int x, int y, float step, Vector2 goalCenter, float heuristicScale)
        {
            int index = IndexOf(x, y);
            if (index < 0 || _cost[index] < 0f)
                return;
            float g = _g[from] + step * 0.5f * (_cost[from] + _cost[index]);
            if (_stamp[index] == _pass && g >= _g[index])
                return;
            _stamp[index] = _pass;
            _g[index] = g;
            _parent[index] = from;
            float h = Iso.ToGround(CellCenter(new Vector2I(x, y)) - goalCenter).Length() * heuristicScale;
            _open.Enqueue(index, g + h);
        }

        /// <summary>
        /// Découpe un itinéraire en tronçons à dessiner : on saute les rues et les chemins déjà tracés,
        /// en gardant une cellule de recouvrement à chaque bout pour le raccord.
        /// </summary>
        public void SplitIntoSegments(List<int> route, List<List<int>> segments)
        {
            List<int> current = null;
            for (int i = 0; i < route.Count; i++)
            {
                int index = route[i];
                bool covered = _road[index] || _claimed[index];
                if (!covered)
                {
                    if (current == null)
                    {
                        current = new List<int>();
                        if (i > 0)
                            current.Add(route[i - 1]);
                    }
                    current.Add(index);
                    continue;
                }
                if (current != null)
                {
                    current.Add(index);
                    segments.Add(current);
                    current = null;
                }
            }
            if (current != null)
                segments.Add(current);
        }

        public void Claim(List<int> route)
        {
            foreach (int index in route)
            {
                _claimed[index] = true;
                if (!_road[index])
                    _cost[index] = Mathf.Min(_cost[index], _config.ExistingPathCost);
            }
        }
    }
}
