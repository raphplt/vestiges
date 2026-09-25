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
		foreach (BuildingFootprint block in layout.Buildings)
		{
			if (block.Size.X < 4 || block.Size.Y < 4)
				continue;

			// Une seule rangée, au sud : elle s'élève au-dessus de la cour de son îlot plutôt que de la rue au nord.
			int southRow = block.Origin.Y + block.Size.Y - 1 - FrontInsetRows;
			placed += PlaceRow(block, southRow, ground, container, usedCells, cache, seed);

			// Cour intérieure : gravats, sous les rangées déjà réservées.
			if (block.Size.X * block.Size.Y >= 30)
				UrbanPropPlacer.PlaceInteriorDebris(block, ground, container, usedCells, cache, seed ^ (ulong)block.Origin.X, ref placed);
			UrbanPropPlacer.ReserveBuildingFootprint(block, usedCells, inset: 0);
		}
		return placed;
	}

	private static int PlaceRow(
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
			uint hash = UrbanPropPlacer.HashCell(new Vector2I(x, row), seed ^ 0xB17DUL);
			if (hash % 100 < AlleyChance)
			{
				x++;
				continue;
			}

			string style = PickStyle(block.Integrity, hash);
			(int width, string path) = PickModule(style, end - x + 1, hash >> 8);
			if (width == 0)
			{
				(width, path) = PickModule("ruin", end - x + 1, hash >> 8);
				if (width == 0)
					break;
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
