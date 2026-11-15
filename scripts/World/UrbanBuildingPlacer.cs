using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Borde les îlots urbains de rangées d'immeubles procéduraux (plan 08, lot P2b).
/// Une rangée longe la rue sud de chaque îlot ; elle s'élève au-dessus de la cour, jamais d'une rue.
/// Les modules disponibles sont lus dans le manifeste des décors : prop_bld_&lt;style&gt;_w&lt;largeur&gt;_&lt;a|b&gt;.
/// </summary>
public static class UrbanBuildingPlacer
{
	public const string SpritePrefix = "prop_bld_";
	private const string Folder = "res://assets/props/urban_ruins";

	// Profondeur d'un module en rangs de cellules (tools/sprites/props/buildings.py : DEPTH_ROWS).
	private const int ModuleDepthRows = 4;
	// Distance du centre du module au bord de l'îlot : la façade s'arrête juste avant le trottoir.
	private const int FrontInsetRows = 2;
	private const int AlleyChance = 22;
	// Un îlot assez profond (deux îlots fusionnés, plan 58 T3a) porte une seconde rangée en son milieu.
	private const int TallBlockRows = 22;
	// Pas entre rangées d'immeubles en redans sur les îlots coupés en biais par une diagonale (plan 58 T3e).
	private const int AngledRowStep = 5;
	private const int MinModuleWidth = 3;
	// Repère rare (plan 08 P2) : pylône dans une cour d'îlot. L'église est Saint-Aubin, au cœur de la ville (plan 58 T3b1).
	private const int MastChance = 10;
	private const int MaxMasts = 2;
	private const string MastSprite = "assets/props/urban_ruins/prop_radio_mast.png";
	// Deux repères du même genre à moins de cette distance à l'écran se répètent au lieu de guider.
	private const float LandmarkSpacingPx = 1600f;

	private static Dictionary<string, List<(int Width, string Path)>> _catalog;

	public static int Place(
		UrbanLayout layout,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		Dictionary<string, Texture2D> cache,
		ulong seed)
	{
		_catalog ??= LoadCatalog();
		if (_catalog.Count == 0)
		{
			GD.PushWarning("[UrbanBuildingPlacer] Aucun immeuble dans le manifeste : générer tools/generate_props.py urban_buildings");
			return 0;
		}

		int placed = 0;
		List<Vector2> masts = new();
		foreach (BuildingFootprint block in layout.Buildings)
		{
			if (block.Size.X < 4 || block.Size.Y < 4)
				continue;

			uint blockHash = UrbanPropPlacer.HashCell(block.Origin, seed ^ 0xC4A9EUL);
			Vector2 blockCenter = ground.MapToLocal(block.Origin + block.Size / 2);
			// Une seule rangée au sud sur les îlots droits (une seconde au milieu s'ils sont profonds) ; les îlots coupés en biais
			// étagent leurs rangées en redans du sud au nord pour suivre le bord de l'avenue diagonale (plan 58 T3e).
			int southRow = block.Origin.Y + block.Size.Y - 1 - FrontInsetRows;
			if (block.Angled)
			{
				for (int row = southRow; row >= block.Origin.Y + FrontInsetRows; row -= AngledRowStep)
					placed += PlaceRow(layout, block, row, ground, container, usedCells, cache, seed);
			}
			else
			{
				placed += PlaceRow(layout, block, southRow, ground, container, usedCells, cache, seed);
				if (block.Size.Y >= TallBlockRows)
					placed += PlaceRow(layout, block, southRow - block.Size.Y / 2, ground, container, usedCells, cache, seed);
			}

			// Pylône au milieu de la cour, derrière la rangée : il dépasse des toits.
			int courtyardEnd = southRow - ModuleDepthRows / 2 - 1;
			if (masts.Count < MaxMasts && courtyardEnd > block.Origin.Y + 1 && (blockHash >> 8) % 100 < MastChance
				&& FarFrom(masts, blockCenter))
			{
				Vector2I anchor = new(block.Origin.X + block.Size.X / 2, (block.Origin.Y + 1 + courtyardEnd) / 2);
				if (IsBuildingCell(layout, anchor) && UrbanPropPlacer.TryPlaceProp(MastSprite, anchor, ground, container, usedCells, cache))
				{
					masts.Add(blockCenter);
					placed++;
				}
			}

			// Cour intérieure : gravats, sous les rangées déjà réservées.
			if (block.Size.X * block.Size.Y >= 30)
				UrbanPropPlacer.PlaceInteriorDebris(layout, block, ground, container, usedCells, cache, seed ^ (ulong)block.Origin.X, ref placed);
			UrbanPropPlacer.ReserveBuildingFootprint(layout, block, usedCells, inset: 0);
		}
		GD.Print($"[UrbanBuildingPlacer] Repères : {masts.Count} pylône(s)");
		return placed;
	}

	/// <summary>Rangée de modules le long d'un rang de l'îlot, coupée de passages au hasard.</summary>
	private static int PlaceRow(
		UrbanLayout layout,
		BuildingFootprint block,
		int row,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		Dictionary<string, Texture2D> cache,
		ulong seed)
	{
		int placed = 0;
		int x = block.Origin.X + 1;
		int end = block.Origin.X + block.Size.X - 2;
		while (x <= end)
		{
			if (!IsBuildingCell(layout, new Vector2I(x, row)))
			{
				x++;
				continue;
			}
			uint hash = UrbanPropPlacer.HashCell(new Vector2I(x, row), seed ^ 0xB17DUL);
			if (hash % 100 < AlleyChance)
			{
				x++;
				continue;
			}

			string style = PickStyle(block.Integrity, hash);
			(int width, string path) = PickFittingModule(layout, usedCells, style, x, row, end - x + 1, hash >> 8);
			if (width == 0)
			{
				x++;
				continue;
			}
			Vector2I anchor = new(x + width / 2, row);
			if (UrbanPropPlacer.TryPlaceProp(path, anchor, ground, container, usedCells, cache))
			{
				placed++;
				Reserve(usedCells, x, width, row);
			}
			x += width;
		}
		return placed;
	}

	private static (int Width, string Path) PickFittingModule(UrbanLayout layout, HashSet<Vector2I> usedCells,
		string style, int startX, int row, int room, uint hash)
	{
		for (int maxRoom = room; maxRoom >= MinModuleWidth; maxRoom--)
		{
			(int width, string path) = PickModule(style, maxRoom, hash);
			if (width == 0)
				(width, path) = PickModule("ruin", maxRoom, hash);
			if (width == 0)
				return (0, null);
			if (IsFree(layout, usedCells, startX, width, row))
				return (width, path);
			maxRoom = width;
		}
		return (0, null);
	}

	private static bool FarFrom(List<Vector2> others, Vector2 point)
	{
		foreach (Vector2 other in others)
		{
			if (other.DistanceSquaredTo(point) < LandmarkSpacingPx * LandmarkSpacingPx)
				return false;
		}
		return true;
	}

	/// <summary>Îlot abîmé → ruines, sinon immeubles, commerces et maisons.</summary>
	private static string PickStyle(float integrity, uint hash)
	{
		int roll = (int)(hash % 100);
		if (integrity < 0.4f || (integrity < 0.55f && roll < 35))
			return "ruin";
		if (integrity < 0.6f && roll < 50)
			return "apartment_damaged";
		if (roll < 30)
			return "shop";
		return roll < 85 ? "apartment" : "house";
	}

	private static (int Width, string Path) PickModule(string style, int room, uint hash)
	{
		if (!_catalog.TryGetValue(style, out List<(int Width, string Path)> modules))
			return (0, null);
		List<(int Width, string Path)> fitting = new();
		foreach ((int Width, string Path) module in modules)
		{
			if (module.Width <= room)
				fitting.Add(module);
		}
		return fitting.Count == 0 ? (0, null) : fitting[(int)(hash % (uint)fitting.Count)];
	}

	private static bool IsBuildingCell(UrbanLayout layout, Vector2I cell) =>
		UrbanPropPlacer.GetCellType(layout, cell) is UrbanCellType.BuildingInterior or UrbanCellType.BuildingWall;

	private static bool IsFree(UrbanLayout layout, HashSet<Vector2I> usedCells, int startX, int width, int row)
	{
		for (int x = startX; x < startX + width; x++)
		{
			for (int y = row - ModuleDepthRows / 2; y <= row + ModuleDepthRows / 2; y++)
			{
				Vector2I cell = new(x, y);
				if (usedCells.Contains(cell) || !IsBuildingCell(layout, cell))
					return false;
			}
		}
		return true;
	}

	private static void Reserve(HashSet<Vector2I> usedCells, int startX, int width, int row)
	{
		for (int x = startX; x < startX + width; x++)
		{
			for (int y = row - ModuleDepthRows / 2; y <= row + ModuleDepthRows / 2; y++)
				usedCells.Add(new Vector2I(x, y));
		}
	}

	private static Dictionary<string, List<(int Width, string Path)>> LoadCatalog()
	{
		Dictionary<string, List<(int Width, string Path)>> catalog = new();
		foreach (string stem in PropManifest.Stems(Folder))
		{
			if (!stem.StartsWith(SpritePrefix))
				continue;
			// prop_bld_<style>_w<largeur>_<a|b>
			string body = stem[SpritePrefix.Length..];
			int widthMarker = body.LastIndexOf("_w");
			if (widthMarker < 0 || !int.TryParse(body[(widthMarker + 2)..body.LastIndexOf('_')], out int width))
				continue;
			string style = body[..widthMarker];
			if (!catalog.TryGetValue(style, out List<(int Width, string Path)> modules))
				catalog[style] = modules = new List<(int Width, string Path)>();
			modules.Add((width, $"assets/props/urban_ruins/{stem}.png"));
		}
		return catalog;
	}
}
