using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Pose des pièces maîtresses d'une terre (plan 58), avant les autres lieux : la pièce du cœur autour du repaire, puis
/// quelques pièces tirées parmi celles du biome, écartées les unes des autres dans la bande des Mémoriaux. Une pièce
/// réserve toute son emprise ; un chemin peut la traverser, et un embranchement la relie au réseau quand il passe à
/// portée. Calcul pur, sur le thread de génération.
/// </summary>
public static class LandPieceComposer
{
    // Un pan de vide se creuse par cellules d'Effacement de 128 px, qui débordent l'ellipse jusqu'à une demi-diagonale.
    private const float VoidPathMarginPx = 96f;

    public static List<PlacedPiece> Plan(WorldGenerator generator, TerrainType[,] terrain, PathNetwork paths,
        HashSet<Vector2I> usedCells, LandPieceConfig config, Vector2 band, Vector2 heartAnchor, ulong seed)
    {
        List<PlacedPiece> placed = new();
        if (generator.ActiveBiomes.Count != 1 || !config.Biomes.TryGetValue(generator.ActiveBiomes[0].Id, out LandPieceConfig.BiomePieces biome))
            return placed;
        ulong started = Time.GetTicksMsec();
        PathStyle style = generator.ActiveBiomes[0].PathStyle ?? PathStyle.Default;
        RandomNumberGenerator rng = new() { Seed = seed ^ 0x58A1UL };

        // Le cœur ne se cherche pas : il entoure le repaire, et le joueur arrive à l'ouest de son centre. Son sol imposé
        // (exigé par LandPieceConfig) assèche l'eau qu'il couvrirait.
        if (biome.Heart != null)
        {
            if (!Fits(generator, paths, usedCells, config, biome.Heart, heartAnchor))
                GD.PushWarning($"[LandPieceComposer] Le cœur « {biome.Heart.Id} » déborde de la terre ou d'un lieu déjà posé");
            Settle(generator, terrain, paths, usedCells, biome.Heart, heartAnchor, false, style);
            placed.Add(new PlacedPiece(biome.Heart, heartAnchor, false, true));
        }

        List<LandPieceConfig.Piece> pool = new(biome.Pieces);
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = rng.RandiRange(0, i);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        int wanted = Math.Min(pool.Count, rng.RandiRange(config.MinPieces, config.MaxPieces));
        int taken = 0;
        foreach (LandPieceConfig.Piece piece in pool)
        {
            if (taken >= wanted)
                break;
            if (!TryPick(generator, paths, usedCells, config, band, piece, placed, rng, out Vector2 anchor))
            {
                GD.PushWarning($"[LandPieceComposer] Pas de place pour la pièce « {piece.Id} »");
                continue;
            }
            bool mirrored = rng.Randf() < 0.5f;
            Settle(generator, terrain, paths, usedCells, piece, anchor, mirrored, style);
            placed.Add(new PlacedPiece(piece, anchor, mirrored, false));
            taken++;
        }
        GD.Print($"[LandPieceComposer] {placed.Count} pièce(s) : {string.Join(", ", placed.ConvertAll(p => p.Piece.Id))}, "
            + $"{Time.GetTicksMsec() - started} ms");
        return placed;
    }

    /// <summary>Décors des pièces, chacune d'après son plan ; rend le nombre de décors posés.</summary>
    public static int Place(IReadOnlyList<PlacedPiece> pieces, Node2D container, ulong seed)
    {
        int placed = 0;
        List<CompositionSite> site = new(1);
        foreach (PlacedPiece piece in pieces)
        {
            site.Clear();
            site.Add(piece.Site);
            placed += SiteComposer.Place(site, piece.Piece.Plan, container, seed);
        }
        return placed;
    }

    /// <summary>Réserve l'emprise, impose le sol de la pièce dans son ellipse et la relie au chemin le plus proche s'il passe à portée.</summary>
    private static void Settle(WorldGenerator generator, TerrainType[,] terrain, PathNetwork paths, HashSet<Vector2I> usedCells,
        LandPieceConfig.Piece piece, Vector2 anchor, bool mirrored, PathStyle style)
    {
        SiteComposer.Reserve(usedCells, anchor, piece.Plan.HalfExtentPx);
        if (piece.Ground is TerrainType ground)
        {
            // Le sol imposé suit l'ellipse inscrite dans l'emprise : une clairière, une place, jamais un rectangle.
            Vector2 half = piece.Plan.HalfExtentPx;
            ForEachCell(anchor, half, cell =>
            {
                Vector2 offset = PathNetworkGenerator.CellCenter(cell) - anchor;
                if ((offset.X * offset.X) / (half.X * half.X) + (offset.Y * offset.Y) / (half.Y * half.Y) > 1f)
                    return;
                int gx = cell.X + generator.MapRadiusX;
                int gy = cell.Y + generator.MapRadiusY;
                if (generator.IsWithinBounds(cell.X, cell.Y) && gx >= 0 && gy >= 0 && gx < terrain.GetLength(0) && gy < terrain.GetLength(1))
                    terrain[gx, gy] = ground;
            });
        }
        if (paths != null)
            SiteComposer.TryConnect(paths, piece.Plan, anchor, mirrored, style);
    }

    /// <summary>Emplacements tirés dans la bande : garde le plus éloigné des pièces posées parmi les premiers valides.</summary>
    private static bool TryPick(WorldGenerator generator, PathNetwork paths, HashSet<Vector2I> usedCells, LandPieceConfig config, Vector2 band,
        LandPieceConfig.Piece piece, List<PlacedPiece> placed, RandomNumberGenerator rng, out Vector2 best)
    {
        best = default;
        float bestScore = float.MinValue;
        int radius = generator.MapRadiusX;
        float stretchY = (float)generator.MapRadiusY / generator.MapRadiusX;
        int valid = 0;
        for (int attempt = 0; attempt < config.Attempts && valid < config.CandidatesPerPiece; attempt++)
        {
            // Uniforme en surface dans la couronne, comme les Mémoriaux (SitePlacer).
            float distance = radius * Mathf.Sqrt(rng.RandfRange(band.X * band.X, band.Y * band.Y));
            float angle = rng.RandfRange(0f, Mathf.Tau);
            Vector2I cell = new(Mathf.RoundToInt(Mathf.Cos(angle) * distance), Mathf.RoundToInt(Mathf.Sin(angle) * distance * stretchY));
            Vector2 anchor = PathNetworkGenerator.CellCenter(cell);
            float nearest = float.MaxValue;
            foreach (PlacedPiece other in placed)
                nearest = Mathf.Min(nearest, anchor.DistanceTo(other.Anchor));
            if (nearest < config.MinSpacingPx || !Fits(generator, paths, usedCells, config, piece, anchor))
                continue;
            valid++;
            float score = Mathf.Min(nearest, config.MinSpacingPx * 2f) + rng.Randf();
            if (score > bestScore)
            {
                bestScore = score;
                best = anchor;
            }
        }
        return valid > 0;
    }

    /// <summary>
    /// Toute l'emprise dans la terre, non effacée, libre, dans le biome ; un peu d'eau tolérée sauf sous un sol imposé
    /// qui l'assèche. Les chemins peuvent la traverser, jamais son pan de vide. La place du Mémorial n'est jamais dans l'eau.
    /// </summary>
    private static bool Fits(WorldGenerator generator, PathNetwork paths, HashSet<Vector2I> usedCells, LandPieceConfig config,
        LandPieceConfig.Piece piece, Vector2 anchor)
    {
        int cells = 0;
        int water = 0;
        bool blocked = false;
        ForEachCell(anchor, piece.Plan.HalfExtentPx, cell =>
        {
            if (blocked)
                return;
            if (!generator.IsWithinBounds(cell.X, cell.Y) || generator.IsErased(cell.X, cell.Y) || usedCells.Contains(cell)
                || generator.GetBiomeIndex(cell.X, cell.Y) != 0)
            {
                blocked = true;
                return;
            }
            cells++;
            if (generator.GetTerrain(cell.X, cell.Y) == TerrainType.Water)
                water++;
        });
        if (blocked || cells == 0 || CrossesVoid(paths, piece, anchor))
            return false;
        if (piece.Ground == null && water > cells * config.MaxWaterShare)
            return false;
        // Retourné ou non, la place du Mémorial reste au sec : les deux côtés sont vérifiés.
        if (piece.Ground == null && piece.Memorial is Vector2 slot)
        {
            foreach (bool mirrored in new[] { false, true })
            {
                Vector2I at = PathNetworkGenerator.CellAt(anchor + SiteComposer.Mirror(slot, mirrored));
                if (generator.GetTerrain(at.X, at.Y) == TerrainType.Water)
                    return false;
            }
        }
        return true;
    }

    /// <summary>Un chemin du réseau passe-t-il dans le pan de vide, retourné ou non ? Un chemin ne mène pas au Néant.</summary>
    private static bool CrossesVoid(PathNetwork paths, LandPieceConfig.Piece piece, Vector2 anchor)
    {
        if (paths == null || piece.VoidCenter is not Vector2 center)
            return false;
        Vector2 radii = piece.VoidRadii + Vector2.One * VoidPathMarginPx;
        foreach (bool mirrored in new[] { false, true })
        {
            Vector2 at = anchor + SiteComposer.Mirror(center, mirrored);
            bool crossed = false;
            ForEachCell(at, radii, cell =>
            {
                if (!crossed && paths.Cells.Contains(cell)
                    && LandPieceConfig.InEllipse(PathNetworkGenerator.CellCenter(cell), at, radii))
                    crossed = true;
            });
            if (crossed)
                return true;
        }
        return false;
    }

    private static void ForEachCell(Vector2 anchor, Vector2 half, Action<Vector2I> visit)
    {
        Vector2I min = PathNetworkGenerator.CellAt(anchor - half);
        Vector2I max = PathNetworkGenerator.CellAt(anchor + half);
        for (int y = min.Y - 1; y <= max.Y + 1; y++)
        {
            for (int x = min.X - 1; x <= max.X + 1; x++)
            {
                Vector2 center = PathNetworkGenerator.CellCenter(new Vector2I(x, y));
                if (Mathf.Abs(center.X - anchor.X) <= half.X && Mathf.Abs(center.Y - anchor.Y) <= half.Y)
                    visit(new Vector2I(x, y));
            }
        }
    }
}
