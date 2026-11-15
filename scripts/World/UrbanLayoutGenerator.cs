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
	Park,
}

/// Direction portée par une cellule de chaussée (combinaisons possibles aux croisements).
[System.Flags]
public enum RoadDir : byte
{
	None = 0,
	Orthogonal = 1 << 0,
	DiagSe = 1 << 1,
	DiagSw = 1 << 2,
}

/// Place en étoile ouverte à l'ancrage ou au croisement d'avenues en diagonale (plan 58 T3e).
public readonly record struct StarPlaza(Vector2I CenterCell, Vector2 CenterWorld, float RadiusPx);

/// Parc urbain : îlot vert de pelouse traversé d'allées, bordé d'arbres, parfois doté d'un bassin (plan 58 T3e).
public sealed class UrbanPark
{
	public Rect2I Bounds;
	public Vector2 CenterWorld;
	public bool HasPond;
	public readonly List<Vector2I> PerimeterCells = new();
	public readonly List<Vector2I> AlleyCells = new();
	public readonly List<Vector2I> LawnCells = new();
	public readonly List<Vector2I> PondCells = new();
}

/// Empreinte rectangulaire d'un batiment.
public struct BuildingFootprint
{
	public Vector2I Origin; // coin haut-gauche (world coords)
	public Vector2I Size;   // largeur, hauteur en cells
	public float Integrity; // 0.0-1.0 : etat de conservation
	public int WallMask;    // bitmask : Top=1, Right=2, Bottom=4, Left=8
	/// <summary>Îlot coupé en biais par une diagonale ou un pan coupé de place en étoile.</summary>
	public bool Angled;
}

/// Resultat complet du layout urbain.
public class UrbanLayout
{
	public UrbanCellType[,] CellGrid;
	public RoadDir[,] RoadDirs;
	public List<BuildingFootprint> Buildings = new();
	public HashSet<Vector2I> WallCells = new();
	public HashSet<Vector2I> RoadCells = new();
	public HashSet<Vector2I> SidewalkCells = new();
	/// <summary>Chaussée des avenues, bordées d'arbres (plan 58 T3a).</summary>
	public HashSet<Vector2I> AvenueCells = new();
	/// <summary>Cellules de chaussée appartenant à une avenue en diagonale (plan 58 T3e).</summary>
	public HashSet<Vector2I> DiagonalCells = new();
	/// <summary>Rues effondrées : la chaussée s'ouvre en cratère de gravats (plan 58 T3a).</summary>
	public HashSet<Vector2I> CraterCells = new();
	/// <summary>Îlots restés ouverts en place plantée (plan 58 T3a).</summary>
	public List<Rect2I> Squares = new();
	/// <summary>Places en étoile à la convergence des diagonales (plan 58 T3e).</summary>
	public List<StarPlaza> StarPlazas = new();
	/// <summary>Parcs verts urbains avec allées et bassin éventuel (plan 58 T3e).</summary>
	public List<UrbanPark> Parks = new();
	/// <summary>Cellules d'herbe ou d'eau appartenant à un parc (plan 58 T3e).</summary>
	public HashSet<Vector2I> ParkCells = new();
	/// <summary>Cellules de bassin au centre d'un parc (plan 58 T3e).</summary>
	public HashSet<Vector2I> ParkPondCells = new();
	/// <summary>Demi-largeur de la grille, en colonnes (CellGrid[x + MapRadius, y + MapRadiusY]).</summary>
	public int MapRadius;
	/// <summary>Demi-hauteur de la grille, en rangées (plan 22 C6).</summary>
	public int MapRadiusY;

	public RoadDir GetRoadDir(int x, int y)
	{
		if (RoadDirs == null || CellGrid == null)
			return RoadDir.None;
		int gx = x + MapRadius;
		int gy = y + MapRadiusY;
		if (gx < 0 || gy < 0 || gx >= CellGrid.GetLength(0) || gy >= CellGrid.GetLength(1))
			return RoadDir.None;
		if (CellGrid[gx, gy] != UrbanCellType.Road)
			return RoadDir.None;
		RoadDir dir = RoadDirs[gx, gy];
		return dir != RoadDir.None ? dir : RoadDir.Orthogonal;
	}
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
			RoadDirs = new RoadDir[_size, _sizeY],
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

		GD.Print($"[UrbanLayout] Layout complete: {layout.RoadCells.Count} road cells ({layout.DiagonalCells.Count} diag), " +
				 $"{layout.Buildings.Count} buildings, {layout.StarPlazas.Count} stars, {layout.Squares.Count} squares, " +
				 $"{layout.Parks.Count} parks, {layout.CraterCells.Count} crater cells, {layout.WallCells.Count} wall cells");

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
			HashSet<Vector2I> carved = new();
			for (int y = from.Y - 1; y <= to.Y + 1; y++)
			{
				for (int x = from.X - 1; x <= to.X + 1; x++)
				{
					Vector2I cell = new(x, y);
					Vector2 offset = PathNetworkGenerator.CellCenter(cell) - piece.Anchor;
					if ((offset.X * offset.X) / (half.X * half.X) + (offset.Y * offset.Y) / (half.Y * half.Y) > 1f)
						continue;
					carved.Add(cell);
					layout.RoadCells.Remove(cell);
					layout.SidewalkCells.Remove(cell);
					layout.WallCells.Remove(cell);
					layout.AvenueCells.Remove(cell);
					layout.DiagonalCells.Remove(cell);
					layout.CraterCells.Remove(cell);
					layout.ParkCells.Remove(cell);
					layout.ParkPondCells.Remove(cell);
					int gx = x + layout.MapRadius;
					int gy = y + layout.MapRadiusY;
					if (gx >= 0 && gy >= 0 && gx < layout.CellGrid.GetLength(0) && gy < layout.CellGrid.GetLength(1))
					{
						layout.CellGrid[gx, gy] = paving;
						if (layout.RoadDirs != null)
							layout.RoadDirs[gx, gy] = RoadDir.None;
					}
				}
			}
			layout.StarPlazas.RemoveAll(star =>
			{
				Vector2 offset = star.CenterWorld - piece.Anchor;
				return (offset.X * offset.X) / (half.X * half.X) + (offset.Y * offset.Y) / (half.Y * half.Y) <= 1f;
			});
			foreach (UrbanPark park in layout.Parks)
			{
				park.PerimeterCells.RemoveAll(carved.Contains);
				park.AlleyCells.RemoveAll(carved.Contains);
				park.LawnCells.RemoveAll(carved.Contains);
				park.PondCells.RemoveAll(carved.Contains);
			}
			layout.Parks.RemoveAll(park =>
			{
				Vector2 offset = park.CenterWorld - piece.Anchor;
				bool centerCarved = (offset.X * offset.X) / (half.X * half.X) + (offset.Y * offset.Y) / (half.Y * half.Y) <= 1f;
				return centerCarved || (park.PerimeterCells.Count + park.AlleyCells.Count + park.LawnCells.Count == 0);
			});
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

		// Avenues en diagonale et places en étoile aux carrefours d'ancrage et de croisement (plan 58 T3e).
		GenerateDiagonals(layout, urbanCells, columns, rows, avenueColumn, avenueRow, minY, maxY);

		// Rues effondrées : le milieu d'un long tronçon droit s'ouvre en cratère, loin des croisements et des diagonales.
		foreach (List<Vector2I> segment in segments)
		{
			// Le premier point d'un tronçon est souvent un croisement : c'est son milieu qui dit s'il appartient à une avenue.
			if (segment.Count < CraterMinSegment || layout.AvenueCells.Contains(segment[segment.Count / 2]) || _rng.Randf() >= _city.CraterChance)
				continue;
			int length = _rng.RandiRange(_city.CraterLength.X, _city.CraterLength.Y);
			int start = _rng.RandiRange(segment.Count / 3, Mathf.Max(segment.Count / 3, segment.Count * 2 / 3 - length));
			for (int s = start; s < start + length && s < segment.Count; s++)
			{
				if (layout.DiagonalCells.Contains(segment[s]) || NearStarPlaza(layout, segment[s], 1.1f)
					|| !UrbanPropPlacer.IsStraightRoadCell(layout, segment[s]))
					break;
				layout.CraterCells.Add(segment[s]);
			}
		}
		foreach (Vector2I crater in layout.CraterCells)
		{
			layout.RoadCells.Remove(crater);
			SetCell(layout, crater, UrbanCellType.Plaza);
		}

		// Places en étoile : disque dallé central dégagé de toute chaussée ; le pourtour ajoute son chanfrein de trottoir.
		HashSet<Vector2I> starFloor = new();
		HashSet<Vector2I> starSidewalkRing = new();
		foreach (StarPlaza star in layout.StarPlazas)
		{
			int reachX = Mathf.CeilToInt(star.RadiusPx / 64f) + 2;
			int reachY = Mathf.CeilToInt(star.RadiusPx / 16f) + 2;
			for (int dy = -reachY; dy <= reachY; dy++)
			{
				for (int dx = -reachX; dx <= reachX; dx++)
				{
					Vector2I cell = new(star.CenterCell.X + dx, star.CenterCell.Y + dy);
					if (!urbanCells.Contains(cell))
						continue;
					float isoDist = IsoDist(PathNetworkGenerator.CellCenter(cell), star.CenterWorld);
					if (isoDist <= star.RadiusPx * 0.72f)
					{
						layout.RoadCells.Remove(cell);
						layout.AvenueCells.Remove(cell);
						layout.DiagonalCells.Remove(cell);
						layout.CraterCells.Remove(cell);
						int gx = cell.X + _mapRadius;
						int gy = cell.Y + _mapRadiusY;
						if (layout.RoadDirs != null && gx >= 0 && gy >= 0 && gx < _size && gy < _sizeY)
							layout.RoadDirs[gx, gy] = RoadDir.None;
						SetCell(layout, cell, UrbanCellType.Plaza);
						starFloor.Add(cell);
					}
					else if (isoDist <= star.RadiusPx && !layout.RoadCells.Contains(cell) && !layout.CraterCells.Contains(cell))
					{
						starSidewalkRing.Add(cell);
					}
				}
			}
		}

		// Trottoirs : cells adjacentes aux routes qui ne sont pas elles-memes des routes. Une avenue a des trottoirs de
		// deux rangs le long d'une rue horizontale, où poussent ses arbres ; une diagonale porte deux cellules en biais.
		HashSet<Vector2I> sidewalks = new(starSidewalkRing);
		List<Vector2I> streets = new(layout.RoadCells);
		streets.AddRange(layout.CraterCells);
		foreach (Vector2I roadCell in streets)
		{
			bool diag = layout.DiagonalCells.Contains(roadCell);
			int reachX = diag ? 2 : 1;
			int reachY = diag || layout.AvenueCells.Contains(roadCell) ? 2 : 1;
			Vector2 roadCenter = diag ? PathNetworkGenerator.CellCenter(roadCell) : Vector2.Zero;
			for (int dx = -reachX; dx <= reachX; dx++)
			{
				for (int dy = -reachY; dy <= reachY; dy++)
				{
					if (dx == 0 && dy == 0) continue;
					Vector2I neighbor = new(roadCell.X + dx, roadCell.Y + dy);
					if (diag && IsoDist(PathNetworkGenerator.CellCenter(neighbor), roadCenter) > 108f)
						continue;
					if (urbanCells.Contains(neighbor) && !layout.RoadCells.Contains(neighbor)
						&& !layout.CraterCells.Contains(neighbor) && !starFloor.Contains(neighbor))
						sidewalks.Add(neighbor);
				}
			}
		}

		foreach (Vector2I sw in sidewalks)
		{
			if (starFloor.Contains(sw))
				continue;
			layout.SidewalkCells.Add(sw);
			SetCell(layout, sw, UrbanCellType.Sidewalk);
		}
	}

	private void GenerateDiagonals(UrbanLayout layout, HashSet<Vector2I> urbanCells, List<int> columns, List<int> rows,
		bool[] avenueColumn, bool[] avenueRow, int minY, int maxY)
	{
		if (columns.Count < 2 || rows.Count < 2 || _city.DiagonalCount.Y <= 0)
			return;

		// Carrefours candidats au cœur de la ville, en écartant le centre du parvis Saint-Aubin quand il reste assez de place.
		List<Vector2I> preferred = new();
		List<Vector2I> fallback = new();
		for (int i = 0; i < columns.Count; i++)
		{
			for (int j = 0; j < rows.Count; j++)
			{
				Vector2I candidate = new(columns[i], rows[j]);
				if (!urbanCells.Contains(candidate))
					continue;
				bool edge = i == 0 || i == columns.Count - 1 || j == 0 || j == rows.Count - 1;
				float centerDist = PathNetworkGenerator.CellCenter(candidate).Length();
				if (!edge && centerDist >= 620f)
				{
					// Doubler le poids des croisements déjà marqués comme avenues.
					preferred.Add(candidate);
					if (avenueColumn[i] || avenueRow[j])
						preferred.Add(candidate);
				}
				else
				{
					fallback.Add(candidate);
				}
			}
		}

		Shuffle(preferred);
		Shuffle(fallback);
		List<Vector2I> pool = new(preferred);
		pool.AddRange(fallback);

		int wanted = _rng.RandiRange(_city.DiagonalCount.X, _city.DiagonalCount.Y);
		float minAnchorSpacing = Mathf.Max(450f, _city.SquareSpacingPx * 0.55f);
		List<Vector2I> anchors = new();
		foreach (Vector2I candidate in pool)
		{
			if (anchors.Count >= wanted)
				break;
			Vector2 world = PathNetworkGenerator.CellCenter(candidate);
			bool far = true;
			foreach (Vector2I chosen in anchors)
			{
				if (PathNetworkGenerator.CellCenter(chosen).DistanceTo(world) < minAnchorSpacing)
				{
					far = false;
					break;
				}
			}
			if (far)
				anchors.Add(candidate);
		}

		Dictionary<int, List<Vector2I>> seByRow = new();
		List<Vector2I> starCandidates = new();
		for (int index = 0; index < anchors.Count; index++)
		{
			Vector2I anchor = anchors[index];
			bool se = index % 2 == 0;
			RoadDir dir = se ? RoadDir.DiagSe : RoadDir.DiagSw;
			List<Vector2I> line = new();
			for (int y = minY; y <= maxY; y++)
			{
				int x = se
					? anchor.X + ((y - anchor.Y + (anchor.Y & 1)) >> 1)
					: anchor.X - ((y - anchor.Y + 1 - (anchor.Y & 1)) >> 1);
				line.Add(new Vector2I(x, y));
			}
			List<Vector2I> stamped = Stamp(layout, urbanCells, line, true, dir);
			if (stamped.Count < 6)
				continue;

			starCandidates.Add(anchor);
			foreach (Vector2I cell in stamped)
			{
				if (se)
				{
					if (!seByRow.TryGetValue(cell.Y, out List<Vector2I> list))
						seByRow[cell.Y] = list = new List<Vector2I>();
					list.Add(cell);
				}
				else if (seByRow.TryGetValue(cell.Y, out List<Vector2I> seList))
				{
					foreach (Vector2I seCell in seList)
					{
						if (Mathf.Abs(seCell.X - cell.X) <= 1)
							starCandidates.Add(cell);
					}
				}
			}
		}

		float minStarSpacing = Mathf.Max(450f, _city.SquareSpacingPx * 0.5f);
		foreach (Vector2I center in starCandidates)
		{
			if (!urbanCells.Contains(center))
				continue;
			Vector2 centerWorld = PathNetworkGenerator.CellCenter(center);
			if (NearStarPlaza(layout, center, 1f, minStarSpacing))
				continue;
			layout.StarPlazas.Add(new StarPlaza(center, centerWorld, _city.StarRadiusPx));
		}
	}

	private void Shuffle<T>(List<T> items)
	{
		for (int i = items.Count - 1; i > 0; i--)
		{
			int j = _rng.RandiRange(0, i);
			(items[i], items[j]) = (items[j], items[i]);
		}
	}

	private static float IsoDist(Vector2 a, Vector2 b)
	{
		float dx = a.X - b.X;
		float dy = (a.Y - b.Y) * 2f;
		return Mathf.Sqrt(dx * dx + dy * dy);
	}

	private static bool NearStarPlaza(UrbanLayout layout, Vector2I cell, float radiusScale, float overrideSpacing = 0f)
	{
		Vector2 world = PathNetworkGenerator.CellCenter(cell);
		foreach (StarPlaza star in layout.StarPlazas)
		{
			if (overrideSpacing > 0f)
			{
				if (star.CenterWorld.DistanceTo(world) < overrideSpacing)
					return true;
			}
			else if (IsoDist(world, star.CenterWorld) <= star.RadiusPx * radiusScale)
			{
				return true;
			}
		}
		return false;
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
	private List<Vector2I> Stamp(UrbanLayout layout, HashSet<Vector2I> urbanCells, List<Vector2I> cells, bool avenue,
		RoadDir dir = RoadDir.Orthogonal)
	{
		List<Vector2I> stamped = new();
		foreach (Vector2I cell in cells)
		{
			if (!urbanCells.Contains(cell))
				continue;
			layout.RoadCells.Add(cell);
			if (avenue)
				layout.AvenueCells.Add(cell);
			if ((dir & (RoadDir.DiagSe | RoadDir.DiagSw)) != 0)
				layout.DiagonalCells.Add(cell);
			int gx = cell.X + _mapRadius;
			int gy = cell.Y + _mapRadiusY;
			if (layout.RoadDirs != null && gx >= 0 && gy >= 0 && gx < _size && gy < _sizeY)
				layout.RoadDirs[gx, gy] |= dir;
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
			int gx = cell.X + _mapRadius;
			int gy = cell.Y + _mapRadiusY;
			if (layout.CellGrid[gx, gy] == UrbanCellType.Plaza)
				continue;
			if (!layout.RoadCells.Contains(cell) && !layout.SidewalkCells.Contains(cell) && !layout.CraterCells.Contains(cell))
				nonRoad.Add(cell);
		}

		List<(List<Vector2I> Cells, Rect2I Bounds)> candidates = new();
		foreach (Vector2I cell in nonRoad)
		{
			if (visited.Contains(cell))
				continue;

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

			if (block.Count < 4 || bw < MinBuildingSize || bh < MinBuildingSize)
			{
				// Trop petit (ou pointe coupée en biais trop fine) → placette dallée
				foreach (Vector2I c in block)
					SetCell(layout, c, UrbanCellType.Plaza);
				continue;
			}

			candidates.Add((block, new Rect2I(bMinX, bMinY, bw, bh)));
		}

		// Parcs verts en premier : loin du cœur Saint-Aubin pour survivre à sa découpe, au moins deux quand la carte s'y prête.
		Shuffle(candidates);
		HashSet<int> usedAsOpenSpace = new();
		for (int pass = 0; pass < 2; pass++)
		{
			for (int idx = 0; idx < candidates.Count; idx++)
			{
				if (usedAsOpenSpace.Contains(idx))
					continue;
				(List<Vector2I> block, Rect2I bounds) = candidates[idx];
				if (block.Count < _city.ParkMinCells)
					continue;
				Vector2 center = PathNetworkGenerator.CellCenter(bounds.GetCenter());
				if (pass == 0 && center.Length() < 600f)
					continue;
				if (!FarFromOpenSpaces(layout, bounds, _city.ParkSpacingPx))
					continue;
				bool pick = pass == 0 ? _rng.Randf() < _city.ParkChance : layout.Parks.Count < 2;
				if (!pick)
					continue;
				BuildPark(layout, block, bounds);
				usedAsOpenSpace.Add(idx);
				if (pass == 1 && layout.Parks.Count >= 2)
					break;
			}
			if (layout.Parks.Count >= 2)
				break;
		}

		for (int idx = 0; idx < candidates.Count; idx++)
		{
			if (usedAsOpenSpace.Contains(idx))
				continue;
			(List<Vector2I> block, Rect2I bounds) = candidates[idx];

			// Place : un îlot sur quelques-uns reste ouvert, loin des autres places et des parcs (plan 58 T3a).
			if (block.Count >= _city.SquareMinCells && _rng.Randf() < _city.SquareChance
				&& FarFromOpenSpaces(layout, bounds, _city.SquareSpacingPx))
			{
				foreach (Vector2I c in block)
					SetCell(layout, c, UrbanCellType.Plaza);
				layout.Squares.Add(bounds);
				continue;
			}

			int bMinX = bounds.Position.X;
			int bMaxX = bounds.End.X - 1;
			int bMinY = bounds.Position.Y;
			int bMaxY = bounds.End.Y - 1;
			int bw = bounds.Size.X;
			int bh = bounds.Size.Y;
			bool angled = block.Count < bw * bh * 0.85f;

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
				Angled = angled,
			};
			layout.Buildings.Add(fp);

			// Marquer les cells : perimetre = mur (y compris bords biseautés), interieur = batiment
			HashSet<Vector2I> blockSet = new(block);
			foreach (Vector2I c in block)
			{
				bool isPerimeter = false;

				if (c.X == bMinX && (wallMask & 8) != 0) isPerimeter = true;
				if (c.X == bMaxX && (wallMask & 2) != 0) isPerimeter = true;
				if (c.Y == bMinY && (wallMask & 1) != 0) isPerimeter = true;
				if (c.Y == bMaxY && (wallMask & 4) != 0) isPerimeter = true;
				if (angled && (!blockSet.Contains(c + Vector2I.Left) || !blockSet.Contains(c + Vector2I.Right)
					|| !blockSet.Contains(c + Vector2I.Up) || !blockSet.Contains(c + Vector2I.Down)))
					isPerimeter = true;

				if (isPerimeter && _rng.Randf() < integrity)
				{
					SetCell(layout, c, UrbanCellType.BuildingWall);
					layout.WallCells.Add(c);
				}
				else
				{
					SetCell(layout, c, UrbanCellType.BuildingInterior);
				}
			}
		}
	}

	private void BuildPark(UrbanLayout layout, List<Vector2I> block, Rect2I bounds)
	{
		HashSet<Vector2I> blockSet = new(block);
		Vector2I centerCell = bounds.GetCenter();
		if (!blockSet.Contains(centerCell))
		{
			Vector2 approx = PathNetworkGenerator.CellCenter(centerCell);
			float bestDist = float.MaxValue;
			foreach (Vector2I candidate in block)
			{
				float d = PathNetworkGenerator.CellCenter(candidate).DistanceSquaredTo(approx);
				if (d < bestDist)
				{
					bestDist = d;
					centerCell = candidate;
				}
			}
		}
		Vector2 center = PathNetworkGenerator.CellCenter(centerCell);
		float halfIso = Mathf.Min(bounds.Size.X * 32f, bounds.Size.Y * 16f);
		bool hasPond = _rng.Randf() < _city.ParkPondChance && bounds.Size.X >= 8 && bounds.Size.Y >= 8 && halfIso >= 120f;
		float pondRadius = Mathf.Clamp(halfIso * 0.28f, 52f, 96f);
		float hubRadius = hasPond ? pondRadius + 44f : Mathf.Clamp(halfIso * 0.35f, 56f, 110f);

		UrbanPark park = new()
		{
			Bounds = bounds,
			CenterWorld = center,
			HasPond = hasPond,
		};

		foreach (Vector2I c in block)
		{
			float d = IsoDist(PathNetworkGenerator.CellCenter(c), center);
			bool onPerimeter = !blockSet.Contains(c + Vector2I.Left) || !blockSet.Contains(c + Vector2I.Right)
				|| !blockSet.Contains(c + Vector2I.Up) || !blockSet.Contains(c + Vector2I.Down);
			bool onAlley = c.X == centerCell.X || Mathf.Abs(c.Y - centerCell.Y) <= 1;

			if (hasPond && d <= pondRadius && !onPerimeter)
			{
				SetCell(layout, c, UrbanCellType.Park);
				layout.ParkCells.Add(c);
				layout.ParkPondCells.Add(c);
				park.PondCells.Add(c);
			}
			else if ((d <= hubRadius && !onPerimeter) || onAlley)
			{
				SetCell(layout, c, UrbanCellType.Plaza);
				park.AlleyCells.Add(c);
			}
			else
			{
				SetCell(layout, c, UrbanCellType.Park);
				layout.ParkCells.Add(c);
				if (onPerimeter)
					park.PerimeterCells.Add(c);
				else
					park.LawnCells.Add(c);
			}
		}
		layout.Parks.Add(park);
	}

	private static bool FarFromOpenSpaces(UrbanLayout layout, Rect2I bounds, float spacingPx)
	{
		Vector2 center = PathNetworkGenerator.CellCenter(bounds.GetCenter());
		float sq = spacingPx * spacingPx;
		foreach (Rect2I square in layout.Squares)
		{
			if (PathNetworkGenerator.CellCenter(square.GetCenter()).DistanceSquaredTo(center) < sq)
				return false;
		}
		foreach (UrbanPark park in layout.Parks)
		{
			if (park.CenterWorld.DistanceSquaredTo(center) < sq)
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
					case UrbanCellType.Park:
						terrain[gx, gy] = layout.ParkPondCells.Contains(new Vector2I(gx - _mapRadius, gy - _mapRadiusY))
							? TerrainType.Water
							: TerrainType.Forest;
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
