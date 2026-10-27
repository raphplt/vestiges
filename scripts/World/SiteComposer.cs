using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Pose des lieux composés (fermes des champs, chantiers de la carrière) : un par région du biome, au plus près de son
/// centre, là où toute l'emprise tient dans le biome, hors de l'eau et des chemins, et près d'un chemin qu'un
/// embranchement rejoint. Les cellules du lieu sont réservées avant les points d'intérêt et les décors génériques.
/// </summary>
public static class SiteComposer
{
    public static List<CompositionSite> Plan(WorldGenerator generator, PathNetwork paths, HashSet<Vector2I> usedCells,
                                             SitePlan plan, ulong seed)
    {
        List<CompositionSite> sites = new();
        if (!plan.Enabled || paths == null)
            return sites;
        ulong started = Time.GetTicksMsec();

        int biomeIndex = -1;
        for (int i = 0; i < generator.ActiveBiomes.Count; i++)
            if (generator.ActiveBiomes[i].Id == plan.BiomeId)
                biomeIndex = i;
        if (biomeIndex < 0)
            return sites;
        PathStyle style = generator.ActiveBiomes[biomeIndex].PathStyle ?? PathStyle.Default;

        List<Vector2I> candidates = new();
        foreach (Vector2 center in generator.BiomeRegionCenters)
        {
            Vector2I origin = new(Mathf.RoundToInt(center.X), Mathf.RoundToInt(center.Y));
            if (generator.GetBiomeIndex(origin.X, origin.Y) != biomeIndex)
                continue;

            // Du centre de la région vers l'extérieur ; un rang vaut une demi-colonne au sol.
            candidates.Clear();
            int reach = plan.SearchRadiusCells;
            for (int dy = -reach * 2; dy <= reach * 2; dy += 2)
                for (int dx = -reach; dx <= reach; dx++)
                    candidates.Add(new Vector2I(origin.X + dx, origin.Y + dy));
            Vector2 originPoint = PathNetworkGenerator.CellCenter(origin);
            candidates.Sort((a, b) => Iso.GroundDistanceSquared(originPoint, PathNetworkGenerator.CellCenter(a))
                .CompareTo(Iso.GroundDistanceSquared(originPoint, PathNetworkGenerator.CellCenter(b))));

            foreach (Vector2I candidate in candidates)
            {
                Vector2 anchor = PathNetworkGenerator.CellCenter(candidate);
                if (!Fits(generator, biomeIndex, paths, usedCells, anchor, plan.HalfExtentPx))
                    continue;
                bool mirrored = CellHash.Of(candidate.X, candidate.Y, seed ^ 0xFA4AUL) % 2 == 1;
                if (!TryConnect(paths, plan, anchor, mirrored, style))
                    continue;

                Reserve(usedCells, anchor, plan.HalfExtentPx);
                sites.Add(new CompositionSite(anchor, mirrored));
                break;
            }
        }
        GD.Print($"[SiteComposer] {sites.Count} {plan.LogName}, {Time.GetTicksMsec() - started} ms");
        return sites;
    }

    /// <summary>
    /// L'embranchement sort du lieu vers le sud, puis rejoint le chemin le plus proche de cette sortie : au sud de
    /// préférence, sinon sur ses côtés, jamais au nord (il traverserait les bâtiments). Faux sans chemin à portée.
    /// </summary>
    public static bool TryConnect(PathNetwork paths, SitePlan plan, Vector2 anchor, bool mirrored, PathStyle style)
    {
        Vector2 spurStart = anchor + Mirror(plan.SpurStart, mirrored);
        Vector2 exit = new(spurStart.X, anchor.Y + plan.HalfExtentPx.Y + 24f);
        if (!PathNetworkGenerator.TryNearestPoint(paths, exit, plan.PathReachPx, exit.Y, out Vector2 junction)
            && !PathNetworkGenerator.TryNearestPoint(paths, exit, plan.PathReachPx, anchor.Y, out junction))
            return false;
        PathNetworkGenerator.AddSpur(paths, spurStart, exit, junction, style, plan.SpurWidthFactor);
        return true;
    }

    public static int Place(List<CompositionSite> sites, SitePlan plan, Node2D container, ulong seed)
    {
        Dictionary<string, Texture2D> cache = new();
        Dictionary<string, int> groupPicks = new();
        int placed = 0;
        foreach (CompositionSite site in sites)
        {
            ulong salt = seed ^ CellHash.Of(Mathf.RoundToInt(site.Anchor.X), Mathf.RoundToInt(site.Anchor.Y), 0xFA4BUL);
            PickGroups(plan, salt, groupPicks);
            for (int index = 0; index < plan.Elements.Count; index++)
            {
                SitePlan.Element element = plan.Elements[index];
                if (element.Group != null && groupPicks[element.Group] != index)
                    continue;
                if (element.Chance < 1f && CellHash.Of(index, 17, salt) % 1000 >= (uint)(element.Chance * 1000f))
                    continue;

                int count = 1;
                Vector2 step = Vector2.Zero;
                if (element.To is Vector2 to && element.Step > 0f)
                {
                    float length = element.At.DistanceTo(to);
                    count = Mathf.FloorToInt(length / element.Step) + 1;
                    step = (to - element.At).Normalized() * element.Step;
                }
                for (int k = 0; k < count; k++)
                {
                    uint pick = CellHash.Of(index, k, salt);
                    string stem = element.Sprites[(int)(pick % (uint)element.Sprites.Length)];
                    Texture2D texture = Load(plan.Folder, stem, cache);
                    if (texture == null)
                        continue;
                    EnvironmentProp prop = new();
                    prop.GlobalPosition = site.Anchor + Mirror(element.At + step * k, site.Mirrored);
                    container.AddChild(prop);
                    prop.Initialize(texture, CanopyOf(plan.Folder, stem, cache), 0f, element.Blocking);
                    placed++;
                }
            }
        }
        return placed;
    }

    /// <summary>Un élément tiré par groupe de variantes, d'après le lieu : les autres du groupe ne sont pas posés.</summary>
    private static void PickGroups(SitePlan plan, ulong salt, Dictionary<string, int> picks)
    {
        picks.Clear();
        Dictionary<string, int> seen = new();
        for (int index = 0; index < plan.Elements.Count; index++)
        {
            string group = plan.Elements[index].Group;
            if (group == null)
                continue;
            // Tirage au fil de l'eau : le n-ième membre remplace l'élu une fois sur n, chacun a la même chance.
            int count = seen.GetValueOrDefault(group) + 1;
            seen[group] = count;
            if (count == 1 || CellHash.Of(index, count, salt ^ 0x6A0BUL) % (uint)count == 0)
                picks[group] = index;
        }
    }

    /// <summary>Canopée d'un tronc nommé « …_base » quand le dossier en a une (« …_canopy ») : arbres et vergers.</summary>
    private static Texture2D CanopyOf(string folder, string stem, Dictionary<string, Texture2D> cache)
    {
        if (!stem.EndsWith("_base", System.StringComparison.Ordinal))
            return null;
        string canopy = $"{stem[..^5]}_canopy";
        // Une absence se retient aussi : le test de fichier ne se refait pas à chaque décor.
        if (cache.TryGetValue(canopy, out Texture2D known))
            return known;
        if (ResourceLoader.Exists($"{folder}{canopy}.png"))
            return Load(folder, canopy, cache);
        cache[canopy] = null;
        return null;
    }

    public static Vector2 Mirror(Vector2 offset, bool mirrored) => mirrored ? new Vector2(-offset.X, offset.Y) : offset;

    public static bool Fits(WorldGenerator generator, int biomeIndex, PathNetwork paths, HashSet<Vector2I> usedCells,
                             Vector2 anchor, Vector2 half)
    {
        Vector2I min = PathNetworkGenerator.CellAt(anchor - half);
        Vector2I max = PathNetworkGenerator.CellAt(anchor + half);
        for (int y = min.Y - 1; y <= max.Y + 1; y++)
        {
            for (int x = min.X - 1; x <= max.X + 1; x++)
            {
                Vector2 center = PathNetworkGenerator.CellCenter(new Vector2I(x, y));
                if (Mathf.Abs(center.X - anchor.X) > half.X || Mathf.Abs(center.Y - anchor.Y) > half.Y)
                    continue;
                Vector2I cell = new(x, y);
                if (!generator.IsWithinBounds(x, y) || generator.IsErased(x, y) || generator.GetBiomeIndex(x, y) != biomeIndex
                    || generator.GetTerrain(x, y) == TerrainType.Water || paths.Cells.Contains(cell) || usedCells.Contains(cell))
                    return false;
            }
        }
        return true;
    }

    public static void Reserve(HashSet<Vector2I> usedCells, Vector2 anchor, Vector2 half)
    {
        Vector2I min = PathNetworkGenerator.CellAt(anchor - half);
        Vector2I max = PathNetworkGenerator.CellAt(anchor + half);
        for (int y = min.Y - 1; y <= max.Y + 1; y++)
        {
            for (int x = min.X - 1; x <= max.X + 1; x++)
            {
                Vector2 center = PathNetworkGenerator.CellCenter(new Vector2I(x, y));
                if (Mathf.Abs(center.X - anchor.X) <= half.X && Mathf.Abs(center.Y - anchor.Y) <= half.Y)
                    usedCells.Add(new Vector2I(x, y));
            }
        }
    }

    public static Texture2D Load(string folder, string stem, Dictionary<string, Texture2D> cache)
    {
        if (cache.TryGetValue(stem, out Texture2D texture))
            return texture;
        string path = $"{folder}{stem}.png";
        texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
        if (texture == null)
            GD.PushWarning($"[SiteComposer] décor introuvable : {path}");
        cache[stem] = texture;
        return texture;
    }
}
