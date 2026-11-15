using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Choix des emplacements des lieux à trouver (coffres, Mémoriaux, Failles), avant les décors : chaque lieu réserve
/// autour de lui un dégagement que les placeurs de décors respectent, pour qu'aucun arbre ni immeuble ne le masque.
/// Un seul tirage, par la graine du monde, pour tous les lieux : ils s'écartent aussi les uns des autres.
/// </summary>
public sealed class SitePlacer
{
    private const float CellWidth = 64f;
    private const float RowHeight = 16f;

    private readonly WorldGenerator _generator;
    private readonly TileMapLayer _ground;
    private readonly HashSet<Vector2I> _usedCells;
    private readonly UrbanLayout _urban;
    private readonly WildFieldsLayout _fields;
    private readonly ChestPlacementData _rules;
    private readonly RandomNumberGenerator _rng;
    private readonly List<Vector2> _placed = new();

    public SitePlacer(WorldGenerator generator, TileMapLayer ground, HashSet<Vector2I> usedCells, ulong seed,
        UrbanLayout urban, WildFieldsLayout fields, ChestPlacementData rules)
    {
        _generator = generator;
        _ground = ground;
        _usedCells = usedCells;
        _urban = urban;
        _fields = fields;
        _rules = rules;
        _rng = new RandomNumberGenerator { Seed = seed ^ 0xC4E57UL };
    }

    public int PlacedCount => _placed.Count;

    /// <summary>Couronne imposée aux Mémoriaux sur une terre (plan 51) ; nulle sur la grande carte.</summary>
    public Vector2? MemorialBand { get; init; }

    /// <summary>Sortes de lieux que <see cref="Scaled"/> réduit sur une terre (clés de <c>lands.count_scales</c>).</summary>
    public static readonly string[] ScaledKinds = { "chests", "memorial", "rift", "workshop", "peril_stele", "well" };

    /// <summary>Part gardée par sorte de lieu sur une terre plus petite que la grande carte (plan 51) ; 1 par défaut.</summary>
    public Func<string, float> CountScale { get; init; } = _ => 1f;

    // Reste de chaque sorte d'un groupe à l'autre : la somme des comptes réduits suit la part, arrondie au plus près.
    private readonly Dictionary<string, float> _countCarry = new();

    /// <summary>Compte d'un groupe de lieux réduit à la part de la terre, sans perte d'un groupe à l'autre.</summary>
    public int Scaled(string kind, int count)
    {
        float scale = CountScale(kind);
        if (scale == 1f)
            return count;
        float carry = _countCarry.GetValueOrDefault(kind, 0.5f) + count * scale;
        int kept = Mathf.FloorToInt(carry);
        _countCarry[kind] = carry - kept;
        return kept;
    }

    /// <summary>
    /// Emplacement dans la couronne [<paramref name="bandMin"/>, <paramref name="bandMax"/>] (fraction du rayon de carte),
    /// à au moins <paramref name="minSpacingPx"/> des lieux déjà posés. Réserve son dégagement.
    /// </summary>
    public bool TryPlace(float bandMin, float bandMax, float minSpacingPx, out Vector2 position)
    {
        position = default;
        // Une terre toute urbaine (plan 51) n'a pas de rue assez large pour le dégagement des immeubles : second essai
        // où le lieu n'évite que les immeubles qui le toucheraient.
        if (!TryPickCell(bandMin, bandMax, minSpacingPx, false, out Vector2I cell)
            && (_urban == null || !TryPickCell(bandMin, bandMax, minSpacingPx, true, out cell)))
            return false;

        position = _ground.MapToLocal(cell);
        ForEachCellInClearance(cell, _rules.Clearance, c => _usedCells.Add(c));
        _placed.Add(position);
        return true;
    }

    /// <summary>
    /// Lieu dont la place est déjà choisie (Mémorial d'une pièce de la terre, plan 58) : les autres lieux s'en écartent
    /// comme d'un lieu tiré, et son dégagement est réservé.
    /// </summary>
    public void Claim(Vector2 position)
    {
        ForEachCellInClearance(_ground.LocalToMap(position), _rules.Clearance, c => _usedCells.Add(c));
        _placed.Add(position);
    }

    /// <summary>Plusieurs candidats valides ; garde celui qui est sur un chemin et loin des autres lieux.</summary>
    private bool TryPickCell(float bandMin, float bandMax, float minSpacingPx, bool nearBuildings, out Vector2I best)
    {
        best = default;
        float bestScore = float.MinValue;
        int radius = _generator.MapRadiusX;
        // Couronne en unités de largeur, étirée en hauteur comme la carte (plan 22 C6).
        float stretchY = (float)_generator.MapRadiusY / _generator.MapRadiusX;
        float innerSq = bandMin * bandMin;
        float outerSq = bandMax * bandMax;
        int valid = 0;

        for (int attempt = 0; attempt < _rules.AttemptsPerChest && valid < _rules.CandidatesPerChest; attempt++)
        {
            // Uniforme en surface dans la couronne : sans la racine, les lieux s'entasseraient au bord intérieur.
            float distance = radius * Mathf.Sqrt(_rng.RandfRange(innerSq, outerSq));
            float angle = _rng.RandfRange(0f, Mathf.Tau);
            Vector2I cell = new(Mathf.RoundToInt(Mathf.Cos(angle) * distance), Mathf.RoundToInt(Mathf.Sin(angle) * distance * stretchY));
            if (!IsCellAllowed(cell, nearBuildings))
                continue;

            float nearest = NearestDistance(_ground.MapToLocal(cell));
            if (nearest < minSpacingPx)
                continue;

            valid++;
            float score = Mathf.Min(nearest / minSpacingPx, 2f);
            if (IsOnPath(cell))
                score += _rules.PathBonus;
            if (score > bestScore)
            {
                bestScore = score;
                best = cell;
            }
        }

        return valid > 0;
    }

    private bool IsCellAllowed(Vector2I cell, bool nearBuildings)
    {
        if (!_generator.IsWithinBounds(cell.X, cell.Y) || _generator.IsErased(cell.X, cell.Y))
            return false;
        if (_generator.GetTerrain(cell.X, cell.Y) == TerrainType.Water)
            return false;

        bool free = true;
        ForEachCellInClearance(cell, _rules.Clearance, c =>
        {
            if (_usedCells.Contains(c))
                free = false;
        });
        if (!free || _urban == null)
            return free;

        // Un immeuble posé devant le lieu, ou le lieu dans un îlot, le rendrait invisible.
        ForEachCellInClearance(cell, nearBuildings ? _rules.Clearance : _rules.BuildingClearance, c =>
        {
            UrbanCellType type = UrbanCell(c);
            if (type == UrbanCellType.BuildingInterior || type == UrbanCellType.BuildingWall)
                free = false;
        });
        return free;
    }

    private bool IsOnPath(Vector2I cell)
    {
        if (_fields != null && _fields.PathCells.Contains(cell))
            return true;
        if (_urban == null)
            return false;
        UrbanCellType type = UrbanCell(cell);
        return type is UrbanCellType.Road or UrbanCellType.Sidewalk or UrbanCellType.Plaza or UrbanCellType.Park;
    }

    private UrbanCellType UrbanCell(Vector2I cell)
    {
        int gx = cell.X + _urban.MapRadius;
        int gy = cell.Y + _urban.MapRadiusY;
        if (gx < 0 || gy < 0 || gx >= _urban.CellGrid.GetLength(0) || gy >= _urban.CellGrid.GetLength(1))
            return UrbanCellType.None;
        return _urban.CellGrid[gx, gy];
    }

    /// <summary>
    /// Cellules dont le centre tombe dans la zone en pixels autour de <paramref name="cell"/>. Grille « stacked » :
    /// une colonne décale de 64 px, un rang de 16 px en quinconce, d'où la zone en pixels et non en cellules.
    /// </summary>
    private void ForEachCellInClearance(Vector2I cell, PixelClearance clearance, System.Action<Vector2I> visit)
    {
        Vector2 origin = _ground.MapToLocal(cell);
        int rowMin = cell.Y - Mathf.CeilToInt(clearance.North / RowHeight);
        int rowMax = cell.Y + Mathf.CeilToInt(clearance.South / RowHeight);
        int columnSpan = Mathf.CeilToInt(clearance.Side / CellWidth) + 1;
        for (int y = rowMin; y <= rowMax; y++)
        {
            for (int x = cell.X - columnSpan; x <= cell.X + columnSpan; x++)
            {
                Vector2I candidate = new(x, y);
                if (Mathf.Abs(_ground.MapToLocal(candidate).X - origin.X) <= clearance.Side)
                    visit(candidate);
            }
        }
    }

    private float NearestDistance(Vector2 position)
    {
        float nearest = float.MaxValue;
        foreach (Vector2 other in _placed)
            nearest = Mathf.Min(nearest, position.DistanceTo(other));
        return nearest;
    }
}
