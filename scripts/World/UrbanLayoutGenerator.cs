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
	public int MapRadius;
}

/// <summary>
/// Post-traitement de la grille de terrain pour le biome urban_ruins.
/// Stamp une grille de rues et des ilots de batiments sur les cells urbaines,
/// puis mute le tableau TerrainType en consequence.
/// </summary>
public class UrbanLayoutGenerator
{
	private readonly RandomNumberGenerator _rng = new();
	private readonly int _mapRadius;
	private readonly int _size;

	// Parametres de generation
	private const int RoadSpacingBase = 14;
	private const int RoadSpacingVariance = 2;
	private const int RoadWidth = 1;
	private const int WobblePeriod = 9999;
	private const int WobbleAmplitude = 0;
	private const float RoadCollapseChance = 0.0f;
	private const int CollapseMinLen = 2;
	private const int CollapseMaxLen = 4;
	private const float IntegrityMin = 0.3f;
	private const float IntegrityMax = 0.9f;
	private const int MinBuildingSize = 6;

	public UrbanLayoutGenerator(ulong seed, int mapRadius)
	{
		_rng.Seed = seed ^ 0xC17EFACE;
		_mapRadius = mapRadius;
		_size = mapRadius * 2 + 1;
	}

	/// <summary>
	/// Point d'entree principal. Mute le terrain in-place et retourne le layout.
	/// </summary>
	public UrbanLayout Apply(TerrainType[,] terrain, WorldGenerator generator, string urbanBiomeId)
	{
		UrbanLayout layout = new()
		{
			CellGrid = new UrbanCellType[_size, _size],
			MapRadius = _mapRadius,
		};

		// Step 1 : collecter les cells urbaines
		HashSet<Vector2I> urbanCells = new();
		int minX = int.MaxValue, maxX = int.MinValue;
		int minY = int.MaxValue, maxY = int.MinValue;

		for (int x = -_mapRadius; x <= _mapRadius; x++)
		{
			for (int y = -_mapRadius; y <= _mapRadius; y++)
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
				 $"{layout.Buildings.Count} buildings, {layout.WallCells.Count} wall cells");

		return layout;
	}

	private void GenerateRoads(UrbanLayout layout, HashSet<Vector2I> urbanCells,
		int minX, int maxX, int minY, int maxY)
	{
		// Routes horizontales (est-ouest)
		int y = minY + RoadSpacingBase / 2;
		while (y <= maxY)
		{
			int width = RoadWidth;
			int wobble = 0;

			for (int x = minX; x <= maxX; x++)
			{
				// Wobble periodique
				if ((x - minX) % WobblePeriod == 0)
					wobble = _rng.RandiRange(-WobbleAmplitude, WobbleAmplitude);

				for (int w = 0; w < width; w++)
				{
					Vector2I cell = new(x, y + w + wobble);
					if (urbanCells.Contains(cell))
					{
						layout.RoadCells.Add(cell);
						SetCell(layout, cell, UrbanCellType.Road);
					}
				}
			}

			// Collapse aleatoire
			if (_rng.Randf() < RoadCollapseChance)
			{
				int gapStart = _rng.RandiRange(minX + 3, maxX - 3);
				int gapLen = _rng.RandiRange(CollapseMinLen, CollapseMaxLen);
				for (int gx = gapStart; gx < gapStart + gapLen; gx++)
				{
					for (int w = 0; w < width; w++)
					{
						Vector2I cell = new(gx, y + w);
						layout.RoadCells.Remove(cell);
						SetCell(layout, cell, UrbanCellType.None);
					}
				}
			}

			y += RoadSpacingBase + _rng.RandiRange(-RoadSpacingVariance, RoadSpacingVariance);
		}

		// Routes verticales (nord-sud)
		int x2 = minX + RoadSpacingBase / 2;
		while (x2 <= maxX)
		{
			int width = RoadWidth;
			int wobble = 0;

			for (int yi = minY; yi <= maxY; yi++)
			{
				if ((yi - minY) % WobblePeriod == 0)
					wobble = _rng.RandiRange(-WobbleAmplitude, WobbleAmplitude);

				for (int w = 0; w < width; w++)
				{
					Vector2I cell = new(x2 + w + wobble, yi);
					if (urbanCells.Contains(cell))
					{
						layout.RoadCells.Add(cell);
						SetCell(layout, cell, UrbanCellType.Road);
					}
				}
			}

			if (_rng.Randf() < RoadCollapseChance)
			{
				int gapStart = _rng.RandiRange(minY + 3, maxY - 3);
				int gapLen = _rng.RandiRange(CollapseMinLen, CollapseMaxLen);
				for (int gy = gapStart; gy < gapStart + gapLen; gy++)
				{
					for (int w = 0; w < width; w++)
					{
						Vector2I cell = new(x2 + w, gy);
						layout.RoadCells.Remove(cell);
						SetCell(layout, cell, UrbanCellType.None);
					}
				}
			}

			x2 += RoadSpacingBase + _rng.RandiRange(-RoadSpacingVariance, RoadSpacingVariance);
		}

		// Trottoirs : cells adjacentes aux routes qui ne sont pas elles-memes des routes
		HashSet<Vector2I> sidewalks = new();
		foreach (Vector2I roadCell in layout.RoadCells)
		{
			for (int dx = -1; dx <= 1; dx++)
			{
				for (int dy = -1; dy <= 1; dy++)
				{
					if (dx == 0 && dy == 0) continue;
					Vector2I neighbor = new(roadCell.X + dx, roadCell.Y + dy);
					if (urbanCells.Contains(neighbor) && !layout.RoadCells.Contains(neighbor))
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

	private void ExtractBuildings(UrbanLayout layout, HashSet<Vector2I> urbanCells)
	{
		// Flood-fill les zones non-route/non-trottoir pour trouver les ilots
		HashSet<Vector2I> visited = new();
		HashSet<Vector2I> nonRoad = new();

		foreach (Vector2I cell in urbanCells)
		{
			if (!layout.RoadCells.Contains(cell) && !layout.SidewalkCells.Contains(cell))
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

	private void StampTerrain(TerrainType[,] terrain, UrbanLayout layout)
	{
		for (int gx = 0; gx < _size; gx++)
		{
			for (int gy = 0; gy < _size; gy++)
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
		int gy = worldCell.Y + _mapRadius;
		if (gx >= 0 && gx < _size && gy >= 0 && gy < _size)
			layout.CellGrid[gx, gy] = type;
	}
}
