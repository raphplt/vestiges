using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// Classification des cellules dans le layout urbain (metadata, pas terrain).
public enum UrbanCellType
{
	None = 0,
	Road,
	Sidewalk,
	BuildingInterior,
	BuildingWall,
	Plaza,
}

/// Empreinte rectangulaire d'un batiment.
public struct BuildingFootprint
{
	public Vector2I Origin; // coin haut-gauche (world coords)
	public Vector2I Size;   // largeur, hauteur en cells
	public float Integrity; // 0.0-1.0 : etat de conservation
	public int WallMask;    // bitmask : Top=1, Right=2, Bottom=4, Left=8
}

/// Resultat complet du layout urbain.
public class UrbanLayout
{
	public UrbanCellType[,] CellGrid;
	public List<BuildingFootprint> Buildings = new();
	public HashSet<Vector2I> WallCells = new();
	public HashSet<Vector2I> RoadCells = new();
	public HashSet<Vector2I> SidewalkCells = new();
	/// <summary>Chaussée des avenues, bordées d'arbres (plan 58 T3a).</summary>
	public HashSet<Vector2I> AvenueCells = new();
	/// <summary>Rues effondrées : la chaussée s'ouvre en cratère de gravats (plan 58 T3a).</summary>
	public HashSet<Vector2I> CraterCells = new();
	/// <summary>Îlots restés ouverts en place plantée (plan 58 T3a).</summary>
	public List<Rect2I> Squares = new();
	/// <summary>Demi-largeur de la grille, en colonnes (CellGrid[x + MapRadius, y + MapRadiusY]).</summary>
	public int MapRadius;
	/// <summary>Demi-hauteur de la grille, en rangées (plan 22 C6).</summary>
	public int MapRadiusY;
}

/// <summary>
/// Post-traitement de la grille de terrain pour le biome urban_ruins.
/// Stamp une grille de rues et des ilots de batiments sur les cells urbaines,
/// puis mute le tableau TerrainType en consequence.
/// La grille suit <see cref="CityGrid"/> (plan 58 T3a) : les rues restent sur les axes de l'écran (grille « stacked »),
/// la ville ondule par écarts variés, décrochements aux croisements, tronçons supprimés et impasses.
/// </summary>
public class UrbanLayoutGenerator
{
	private readonly RandomNumberGenerator _rng = new();
	private readonly CityGrid _city;
	private readonly int _mapRadius;
	private readonly int _mapRadiusY;
	private readonly int _size;
	private readonly int _sizeY;

	private const float IntegrityMin = 0.3f;
	private const float IntegrityMax = 0.9f;
	private const int MinBuildingSize = 6;
	// Un cratère se creuse loin des croisements, dans un tronçon assez long pour qu'on le contourne de l'œil.
	private const int CraterMinSegment = 8;

	public UrbanLayoutGenerator(ulong seed, int mapRadius, int mapRadiusY, CityGrid city)
	{
		_rng.Seed = seed ^ 0xC17EFACE;
		_city = city;
		_mapRadius = mapRadius;
		_mapRadiusY = mapRadiusY;
		_size = mapRadius * 2 + 1;
		_sizeY = mapRadiusY * 2 + 1;
	}

	/// <summary>
	/// Point d'entree principal. Mute le terrain in-place et retourne le layout.
	/// </summary>
	public UrbanLayout Apply(TerrainType[,] terrain, WorldGenerator generator, string urbanBiomeId)
	{
		UrbanLayout layout = new()
		{
			CellGrid = new UrbanCellType[_size, _sizeY],
			MapRadius = _mapRadius,
			MapRadiusY = _mapRadiusY,
		};

		// Step 1 : collecter les cells urbaines
		HashSet<Vector2I> urbanCells = new();
		int minX = int.MaxValue, maxX = int.MinValue;
		int minY = int.MaxValue, maxY = int.MinValue;

		for (int x = -_mapRadius; x <= _mapRadius; x++)
		{
			for (int y = -_mapRadiusY; y <= _mapRadiusY; y++)
			{
				if (!generator.IsWithinBounds(x, y) || generator.IsErased(x, y))
					continue;
				if (generator.GetTerrain(x, y) == TerrainType.Water)
					continue;
				if (generator.GetBiomeId(x, y) != urbanBiomeId)
					continue;

				urbanCells.Add(new Vector2I(x, y));
				if (x < minX) minX = x;
				if (x > maxX) maxX = x;
				if (y < minY) minY = y;
				if (y > maxY) maxY = y;
			}
		}

		if (urbanCells.Count < 50)
		{
			GD.Print($"[UrbanLayout] Too few urban cells ({urbanCells.Count}), skipping layout");
			return layout;
		}

		GD.Print($"[UrbanLayout] {urbanCells.Count} urban cells, bbox=[{minX},{minY}]-[{maxX},{maxY}]");

		// Step 2 : generer la grille de rues
		GenerateRoads(layout, urbanCells, minX, maxX, minY, maxY);

		// Step 3 : definir les ilots de batiments (flood-fill non-route)
		ExtractBuildings(layout, urbanCells);

		// Step 4 : muter le terrain
		StampTerrain(terrain, layout);

		GD.Print($"[UrbanLayout] Layout complete: {layout.RoadCells.Count} road cells, " +
				 $"{layout.Buildings.Count} buildings, {layout.Squares.Count} squares, {layout.CraterCells.Count} crater cells, " +
				 $"{layout.WallCells.Count} wall cells");

		return layout;
	}

	/// <summary>
	/// Les pièces trouent la grille (plan 58 T3a) : sous l'ellipse de chacune, ni rue, ni trottoir, ni mur. Les rues
	/// s'arrêtent à son bord ; les immeubles s'écartent de son emprise réservée (UrbanBuildingPlacer). Une pièce pavée
	/// (sans sol imposé, ou de béton) prend le dallage des places ; une pièce d'herbe garde le sol de son terrain.
	/// </summary>
	public static void Carve(UrbanLayout layout, IReadOnlyList<PlacedPiece> pieces)
	{
		if (layout == null)
			return;
		foreach (PlacedPiece piece in pieces)
		{
			UrbanCellType paving = piece.Piece.Ground is null or TerrainType.Concrete ? UrbanCellType.Plaza : UrbanCellType.None;
			Vector2 half = piece.Piece.Plan.HalfExtentPx;
			Vector2I from = PathNetworkGenerator.CellAt(piece.Anchor - half);
			Vector2I to = PathNetworkGenerator.CellAt(piece.Anchor + half);
			for (int y = from.Y - 1; y <= to.Y + 1; y++)
			{
				for (int x = from.X - 1; x <= to.X + 1; x++)
				{
					Vector2I cell = new(x, y);
					Vector2 offset = PathNetworkGenerator.CellCenter(cell) - piece.Anchor;
					if ((offset.X * offset.X) / (half.X * half.X) + (offset.Y * offset.Y) / (half.Y * half.Y) > 1f)
						continue;
					layout.RoadCells.Remove(cell);
					layout.SidewalkCells.Remove(cell);
					layout.WallCells.Remove(cell);
					layout.AvenueCells.Remove(cell);
					layout.CraterCells.Remove(cell);
					int gx = x + layout.MapRadius;
					int gy = y + layout.MapRadiusY;
					if (gx >= 0 && gy >= 0 && gx < layout.CellGrid.GetLength(0) && gy < layout.CellGrid.GetLength(1))
						layout.CellGrid[gx, gy] = paving;
				}
			}
		}
	}

	private void GenerateRoads(UrbanLayout layout, HashSet<Vector2I> urbanCells,
		int minX, int maxX, int minY, int maxY)
	{
		List<int> columns = Lines(minX, maxX, _city.ColumnSpacing);
		List<int> rows = Lines(minY, maxY, _city.RowSpacing);
		bool[] avenueRow = new bool[rows.Count];
		bool[] avenueColumn = new bool[columns.Count];
		for (int j = 0; j < rows.Count; j++)
			avenueRow[j] = _rng.Randf() < _city.AvenueChance;
		for (int i = 0; i < columns.Count; i++)
			avenueColumn[i] = _rng.Randf() < _city.AvenueChance;

		// Tronçons horizontaux : rangée j, intervalle i entre les colonnes i − 1 et i (0 et columns.Count : les bords).
		// Chacun peut décrocher de quelques rangs ou disparaître (deux îlots fusionnent) ; une avenue reste droite et entière.
		int intervals = columns.Count + 1;
		int[,] jog = new int[rows.Count, intervals];
		bool[,] kept = new bool[rows.Count, intervals];
		List<List<Vector2I>> segments = new();
		for (int j = 0; j < rows.Count; j++)
		{
			for (int i = 0; i < intervals; i++)
			{
				kept[j, i] = avenueRow[j] || _rng.Randf() >= _city.MergeChance;
				if (!avenueRow[j] && _rng.Randf() < _city.JogChance)
					jog[j, i] = _rng.RandiRange(1, _city.JogRows) * (_rng.Randf() < 0.5f ? -1 : 1);
				if (!kept[j, i])
					continue;
				int fromX = i == 0 ? minX : columns[i - 1];
				int toX = i == columns.Count ? maxX : columns[i];
				List<Vector2I> segment = new();
				for (int x = fromX; x <= toX; x++)
					segment.Add(new Vector2I(x, rows[j] + jog[j, i]));
				segments.Add(Stamp(layout, urbanCells, segment, avenueRow[j]));
			}
		}

		// Tronçons verticaux : colonne i, entre les rangées k − 1 et k. Supprimé, ou réduit à une ruelle en impasse.
		for (int i = 0; i < columns.Count; i++)
		{
			for (int k = 0; k <= rows.Count; k++)
			{
				int fromY = k == 0 ? minY : rows[k - 1];
				int toY = k == rows.Count ? maxY : rows[k];
				if (!avenueColumn[i])
				{
					if (_rng.Randf() < _city.MergeChance)
						continue;
					if (_rng.Randf() < _city.DeadEndChance)
					{
						int middle = (fromY + toY) / 2;
						if (_rng.Randf() < 0.5f)
							toY = middle;
						else
							fromY = middle;
					}
				}
				List<Vector2I> segment = new();
				for (int y = fromY; y <= toY; y++)
					segment.Add(new Vector2I(columns[i], y));
				segments.Add(Stamp(layout, urbanCells, segment, avenueColumn[i]));
			}
		}

		// Raccords des décrochements : au croisement, la colonne relie les tronçons de part et d'autre et la rue verticale.
		for (int j = 0; j < rows.Count; j++)
		{
			for (int i = 0; i < columns.Count; i++)
			{
				int low = 0, high = 0;
				bool any = false;
				foreach (int side in new[] { i, i + 1 })
				{
					if (!kept[j, side])
						continue;
					low = any ? Mathf.Min(low, jog[j, side]) : Mathf.Min(0, jog[j, side]);
					high = any ? Mathf.Max(high, jog[j, side]) : Mathf.Max(0, jog[j, side]);
					any = true;
				}
				if (!any)
					continue;
				List<Vector2I> connector = new();
				for (int dy = low; dy <= high; dy++)
					connector.Add(new Vector2I(columns[i], rows[j] + dy));
				Stamp(layout, urbanCells, connector, false);
			}
		}

		// Rues effondrées : le milieu d'un long tronçon droit s'ouvre en cratère, loin des croisements.
		foreach (List<Vector2I> segment in segments)
		{
			// Le premier point d'un tronçon est souvent un croisement : c'est son milieu qui dit s'il appartient à une avenue.
			if (segment.Count < CraterMinSegment || layout.AvenueCells.Contains(segment[segment.Count / 2]) || _rng.Randf() >= _city.CraterChance)
				continue;
			int length = _rng.RandiRange(_city.CraterLength.X, _city.CraterLength.Y);
			int start = _rng.RandiRange(segment.Count / 3, Mathf.Max(segment.Count / 3, segment.Count * 2 / 3 - length));
			for (int s = start; s < start + length && s < segment.Count; s++)
			{
				if (!UrbanPropPlacer.IsStraightRoadCell(layout, segment[s]))
					break;
				layout.CraterCells.Add(segment[s]);
			}
		}
		foreach (Vector2I crater in layout.CraterCells)
		{
			layout.RoadCells.Remove(crater);
			SetCell(layout, crater, UrbanCellType.Plaza);
		}

		// Trottoirs : cells adjacentes aux routes qui ne sont pas elles-memes des routes. Une avenue a des trottoirs de
		// deux rangs le long d'une rue horizontale, où poussent ses arbres. Un cratère garde les trottoirs de sa rue.
		HashSet<Vector2I> sidewalks = new();
		List<Vector2I> streets = new(layout.RoadCells);
		streets.AddRange(layout.CraterCells);
		foreach (Vector2I roadCell in streets)
		{
			int reachY = layout.AvenueCells.Contains(roadCell) ? 2 : 1;
			for (int dx = -1; dx <= 1; dx++)
			{
				for (int dy = -reachY; dy <= reachY; dy++)
				{
					if (dx == 0 && dy == 0) continue;
					Vector2I neighbor = new(roadCell.X + dx, roadCell.Y + dy);
					if (urbanCells.Contains(neighbor) && !layout.RoadCells.Contains(neighbor) && !layout.CraterCells.Contains(neighbor))
						sidewalks.Add(neighbor);
				}
			}
		}

		foreach (Vector2I sw in sidewalks)
		{
			layout.SidewalkCells.Add(sw);
			SetCell(layout, sw, UrbanCellType.Sidewalk);
		}
	}

	/// <summary>Positions des rues d'un axe : un premier demi-écart, puis des écarts tirés entre min et max.</summary>
	private List<int> Lines(int from, int to, Vector2I spacing)
	{
		List<int> lines = new();
		int at = from + _rng.RandiRange(spacing.X, spacing.Y) / 2;
		while (at <= to - spacing.X / 2)
		{
			lines.Add(at);
			at += _rng.RandiRange(spacing.X, spacing.Y);
		}
		return lines;
	}

	/// <summary>Pose les cellules urbaines d'un tronçon en chaussée ; rend celles qui ont été posées, dans l'ordre.</summary>
	private List<Vector2I> Stamp(UrbanLayout layout, HashSet<Vector2I> urbanCells, List<Vector2I> cells, bool avenue)
	{
		List<Vector2I> stamped = new();
		foreach (Vector2I cell in cells)
		{
			if (!urbanCells.Contains(cell))
				continue;
			layout.RoadCells.Add(cell);
			if (avenue)
				layout.AvenueCells.Add(cell);
			SetCell(layout, cell, UrbanCellType.Road);
			stamped.Add(cell);
		}
		return stamped;
	}

	private void ExtractBuildings(UrbanLayout layout, HashSet<Vector2I> urbanCells)
	{
		// Flood-fill les zones non-route/non-trottoir pour trouver les ilots
		HashSet<Vector2I> visited = new();
		HashSet<Vector2I> nonRoad = new();

		foreach (Vector2I cell in urbanCells)
		{
			if (!layout.RoadCells.Contains(cell) && !layout.SidewalkCells.Contains(cell) && !layout.CraterCells.Contains(cell))
				nonRoad.Add(cell);
		}

		foreach (Vector2I cell in nonRoad)
		{
			if (visited.Contains(cell))
				continue;

			// Flood-fill ce bloc
			List<Vector2I> block = new();
			Queue<Vector2I> queue = new();
			queue.Enqueue(cell);
			visited.Add(cell);

			while (queue.Count > 0)
			{
				Vector2I current = queue.Dequeue();
				block.Add(current);

				foreach (Vector2I dir in new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
				{
					Vector2I neighbor = current + dir;
					if (nonRoad.Contains(neighbor) && !visited.Contains(neighbor))
					{
						visited.Add(neighbor);
						queue.Enqueue(neighbor);
					}
				}
			}

			if (block.Count < 4)
			{
				// Trop petit → plaza
				foreach (Vector2I c in block)
					SetCell(layout, c, UrbanCellType.Plaza);
				continue;
			}

			// Calculer la bounding box du bloc
			int bMinX = int.MaxValue, bMaxX = int.MinValue;
			int bMinY = int.MaxValue, bMaxY = int.MinValue;
			foreach (Vector2I c in block)
			{
				if (c.X < bMinX) bMinX = c.X;
				if (c.X > bMaxX) bMaxX = c.X;
				if (c.Y < bMinY) bMinY = c.Y;
				if (c.Y > bMaxY) bMaxY = c.Y;
			}

			int bw = bMaxX - bMinX + 1;
			int bh = bMaxY - bMinY + 1;

			if (bw < MinBuildingSize || bh < MinBuildingSize)
			{
				foreach (Vector2I c in block)
					SetCell(layout, c, UrbanCellType.Plaza);
				continue;
			}

			// Place : un îlot sur quelques-uns reste ouvert, loin des autres places (plan 58 T3a).
			Rect2I bounds = new(bMinX, bMinY, bw, bh);
			if (block.Count >= _city.SquareMinCells && _rng.Randf() < _city.SquareChance && FarFromSquares(layout, bounds))
			{
				foreach (Vector2I c in block)
					SetCell(layout, c, UrbanCellType.Plaza);
				layout.Squares.Add(bounds);
				continue;
			}

			// Creer le batiment
			float integrity = _rng.RandfRange(IntegrityMin, IntegrityMax);
			int wallMask = 0;
			if (_rng.Randf() < integrity) wallMask |= 1; // Top
			if (_rng.Randf() < integrity) wallMask |= 2; // Right
			if (_rng.Randf() < integrity) wallMask |= 4; // Bottom
			if (_rng.Randf() < integrity) wallMask |= 8; // Left

			// Au moins 2 cotes ont des murs pour que ca ressemble a un batiment
			int wallCount = 0;
			for (int bit = 0; bit < 4; bit++)
				if ((wallMask & (1 << bit)) != 0) wallCount++;
			if (wallCount < 2)
			{
				// Forcer 2 cotes adjacents
				wallMask |= 1 | 2; // Top + Right minimum
			}

			BuildingFootprint fp = new()
			{
				Origin = new Vector2I(bMinX, bMinY),
				Size = new Vector2I(bw, bh),
				Integrity = integrity,
				WallMask = wallMask,
			};
			layout.Buildings.Add(fp);

			// Marquer les cells : perimetre = mur, interieur = batiment
			HashSet<Vector2I> blockSet = new(block);
			foreach (Vector2I c in block)
			{
				bool isPerimeter = false;

				// Verifier si c'est un bord du bloc
				if (c.X == bMinX && (wallMask & 8) != 0) isPerimeter = true; // Left wall
				if (c.X == bMaxX && (wallMask & 2) != 0) isPerimeter = true; // Right wall
				if (c.Y == bMinY && (wallMask & 1) != 0) isPerimeter = true; // Top wall
				if (c.Y == bMaxY && (wallMask & 4) != 0) isPerimeter = true; // Bottom wall

				if (isPerimeter)
				{
					// Probabilite par cell basee sur integrity
					if (_rng.Randf() < integrity)
					{
						SetCell(layout, c, UrbanCellType.BuildingWall);
						layout.WallCells.Add(c);
					}
					else
					{
						SetCell(layout, c, UrbanCellType.BuildingInterior);
					}
				}
				else
				{
					SetCell(layout, c, UrbanCellType.BuildingInterior);
				}
			}
		}
	}

	private bool FarFromSquares(UrbanLayout layout, Rect2I bounds)
	{
		Vector2 center = PathNetworkGenerator.CellCenter(bounds.GetCenter());
		foreach (Rect2I square in layout.Squares)
		{
			if (PathNetworkGenerator.CellCenter(square.GetCenter()).DistanceSquaredTo(center) < _city.SquareSpacingPx * _city.SquareSpacingPx)
				return false;
		}
		return true;
	}

	private void StampTerrain(TerrainType[,] terrain, UrbanLayout layout)
	{
		for (int gx = 0; gx < _size; gx++)
		{
			for (int gy = 0; gy < _sizeY; gy++)
			{
				UrbanCellType cellType = layout.CellGrid[gx, gy];
				if (cellType == UrbanCellType.None)
					continue;

				switch (cellType)
				{
					case UrbanCellType.Road:
					case UrbanCellType.BuildingInterior:
					case UrbanCellType.BuildingWall:
					case UrbanCellType.Plaza:
						terrain[gx, gy] = TerrainType.Concrete;
						break;
					case UrbanCellType.Sidewalk:
						terrain[gx, gy] = TerrainType.Grass;
						break;
				}
			}
		}
	}

	private void SetCell(UrbanLayout layout, Vector2I worldCell, UrbanCellType type)
	{
		int gx = worldCell.X + _mapRadius;
		int gy = worldCell.Y + _mapRadiusY;
		if (gx >= 0 && gx < _size && gy >= 0 && gy < _sizeY)
			layout.CellGrid[gx, gy] = type;
	}
}
