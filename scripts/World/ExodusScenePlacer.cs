using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Les rues racontent l'exode (plan 39 B3) : quelques scènes rares le long des rues de la ville, posées avant le semis
/// aléatoire, qui leur laisse un vide autour. Embouteillage figé (voitures portières ouvertes, bagages lâchés sur un
/// trottoir, flèche peinte devant) ou barrage d'évacuation près d'un carrefour (barrière en travers, plots, panneau,
/// flèche). Tous fuyaient vers l'extérieur de la carte, loin du départ de l'Effacement : le sens des voitures et des
/// flèches suit la rue dans cette direction. Une voiture ne prend que la chaussée, d'une cellule de large : les
/// trottoirs restent libres et la rue praticable.
/// </summary>
public static class ExodusScenePlacer
{
	private const string Folder = "assets/props/urban_ruins/";
	private static readonly string[] CarPaints = { "red", "teal", "cream" };
	private static readonly string[] Leftovers = {
		Folder + "prop_exodus_suitcase_open.png",
		Folder + "prop_exodus_luggage.png",
		Folder + "prop_exodus_stroller.png",
		Folder + "prop_exodus_cart.png",
		Folder + "prop_exodus_boxes.png",
	};
	private const string ConesSprite = Folder + "prop_exodus_cones.png";
	// Distance (cellules) jusqu'où un barrage cherche son carrefour le long de la rue.
	private const int BarricadeSearchCells = 16;
	// Bagages et panneaux tombent sur le trottoir, à cette distance de l'axe de la rue (px).
	private const float SidewalkReachHorizontalPx = 24f;
	private const float SidewalkReachVerticalPx = 36f;
	// Le panneau fait 44 px de large : au bord d'une rue verticale, il s'écarte d'une demi-largeur de plus que les bagages.
	private const float SignReachVerticalPx = 58f;

	/// <summary>Une scène posée : son ancrage (dernière voiture de la file ou barrière, coordonnées locales) et sa nature.</summary>
	public readonly record struct Scene(Vector2 Position, bool Jam);

	/// <summary>Décors d'une scène prévus avant pose : une scène incomplète n'est pas posée du tout.</summary>
	private readonly record struct Placement(string Sprite, Vector2I Cell, Vector2 Position);

	/// <summary>Pose les scènes et les renvoie.</summary>
	public static List<Scene> Place(
		UrbanLayout layout,
		HashSet<Vector2I> intersections,
		ExodusConfig config,
		TileMapLayer ground,
		Node2D container,
		HashSet<Vector2I> usedCells,
		HashSet<Vector2I> streetProps,
		Dictionary<string, Texture2D> cache,
		ulong seed)
	{
		List<Scene> scenes = new();
		List<Vector2> anchors = new();
		int target = layout.RoadCells.Count / config.RoadCellsPerScene;
		if (target == 0)
			return scenes;
		// Une file de voitures demande une longue rue droite libre, un barrage presque rien : sans quota, les barrages
		// prendraient la place des embouteillages.
		int jamTarget = Mathf.RoundToInt(target * config.JamShare);

		// Ordre de candidature tiré par la seed, indépendant de l'ordre d'insertion des rues.
		List<Vector2I> candidates = new();
		foreach (Vector2I cell in layout.RoadCells)
		{
			if (IsFreeStraightRoad(layout, intersections, usedCells, cell))
				candidates.Add(cell);
		}
		ulong salt = seed ^ 0xE70D05UL;
		candidates.Sort((a, b) =>
		{
			int order = UrbanPropPlacer.HashCell(a, salt).CompareTo(UrbanPropPlacer.HashCell(b, salt));
			return order != 0 ? order : a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y);
		});

		int jams = 0;
		int barricades = 0;
		int props = 0;
		List<Placement> plan = new();
		float minSpacingSq = config.MinSpacingPx * config.MinSpacingPx;
		foreach (Vector2I cell in candidates)
		{
			if (anchors.Count >= target)
				break;
			if (IsTooClose(anchors, UrbanPropPlacer.StreetPoint(layout, ground, cell), minSpacingSq))
				continue;

			uint hash = UrbanPropPlacer.HashCell(cell, salt ^ 0x5CE7EUL);
			bool jam = jams < jamTarget && (barricades >= target - jamTarget || jams * (target - jamTarget) <= barricades * jamTarget);
			bool planned = TryPlan(jam, layout, intersections, config, ground, usedCells, cell, hash, plan);
			// Le type voulu ne tient pas ici : l'autre, s'il lui reste de la place, plutôt que de perdre la candidate.
			if (!planned && (jam ? barricades < target - jamTarget : jams < jamTarget))
			{
				jam = !jam;
				planned = TryPlan(jam, layout, intersections, config, ground, usedCells, cell, hash, plan);
			}
			// Un barrage se pose au carrefour le plus proche, parfois loin de la candidate : l'écart se juge sur la scène.
			if (!planned || IsTooClose(anchors, plan[0].Position, minSpacingSq))
				continue;
			Vector2 anchor = plan[0].Position;
			for (int i = 0; i < plan.Count; i++)
			{
				Placement placement = plan[i];
				Vector2 offset = placement.Position - ground.MapToLocal(placement.Cell);
				if (UrbanPropPlacer.TryPlaceProp(placement.Sprite, placement.Cell, ground, container, usedCells, cache, offset))
				{
					props++;
					Quiet(streetProps, placement.Cell, config.QuietRadiusCells);
				}
			}
			anchors.Add(anchor);
			scenes.Add(new Scene(anchor, jam));
			if (jam)
				jams++;
			else
				barricades++;
		}

		GD.Print($"[ExodusScenePlacer] {anchors.Count}/{target} scènes ({jams} embouteillages, {barricades} barrages), {props} décors");
		return scenes;
	}

	private static bool TryPlan(bool jam, UrbanLayout layout, HashSet<Vector2I> intersections, ExodusConfig config,
		TileMapLayer ground, HashSet<Vector2I> usedCells, Vector2I cell, uint hash, List<Placement> plan)
	{
		plan.Clear();
		return jam
			? PlanJam(layout, intersections, config, ground, usedCells, cell, hash, plan)
			: PlanBarricade(layout, intersections, config, ground, usedCells, cell, hash, plan);
	}

	/// <summary>
	/// Embouteillage : une file de voitures sur la chaussée, dans le sens de la fuite, puis une flèche peinte devant la
	/// première ; des bagages lâchés sur un seul trottoir, celui par où l'on est parti à pied.
	/// </summary>
	private static bool PlanJam(UrbanLayout layout, HashSet<Vector2I> intersections, ExodusConfig config, TileMapLayer ground,
		HashSet<Vector2I> usedCells, Vector2I start, uint hash, List<Placement> plan)
	{
		bool horizontal = layout.RoadCells.Contains(start + Vector2I.Right);
		Vector2I step = FlightStep(start, horizontal);
		string direction = DirectionName(step);
		float spacing = horizontal ? config.CarSpacingHorizontalPx : config.CarSpacingVerticalPx;
		int cars = config.JamCarsMin + (int)(hash % (uint)(config.JamCarsMax - config.JamCarsMin + 1));
		// Une voiture toutes les deux cellules en horizontal (64 px) ; en vertical, une rangée ne descend que de 16 px.
		int maxSteps = cars * (horizontal ? 2 : 4) + 4;

		List<Vector2> carPositions = new();
		Vector2I cell = start;
		Vector2 last = Vector2.Inf;
		for (int i = 0; i < maxSteps && carPositions.Count < cars; i++, cell += step)
		{
			if (!IsFreeStraightRoad(layout, intersections, usedCells, cell))
				return false;
			Vector2 position = UrbanPropPlacer.StreetPoint(layout, ground, cell);
			if (carPositions.Count > 0 && position.DistanceTo(last) < spacing)
				continue;
			string paint = CarPaints[(int)((hash >> 4) + (uint)carPositions.Count * 7u) % CarPaints.Length];
			plan.Add(new Placement($"{Folder}prop_exodus_car_{paint}_{direction}.png", cell, position));
			carPositions.Add(position);
			last = position;
		}
		if (carPositions.Count < cars)
			return false;

		// Flèche au sol devant la voiture de tête, si la rue continue droite assez loin.
		for (int i = 0; i < 4; i++, cell += step)
		{
			if (!IsFreeStraightRoad(layout, intersections, usedCells, cell))
				break;
			Vector2 position = UrbanPropPlacer.StreetPoint(layout, ground, cell);
			if (position.DistanceTo(last) < spacing * 1.2f)
				continue;
			plan.Add(new Placement($"{Folder}prop_exodus_arrow_{direction}.png", cell, position));
			break;
		}

		int leftovers = Roll(config.LeftoversPerJamMin, config.LeftoversPerJamMax, hash >> 8);
		float side = (hash & 0x100u) != 0 ? 1f : -1f;
		for (int i = 0; i < leftovers; i++)
		{
			uint draw = UrbanPropPlacer.HashCell(start, (ulong)(hash ^ (uint)(i * 0x9E37)));
			Vector2 car = carPositions[(int)(draw % (uint)carPositions.Count)];
			float jitter = ((draw >> 8) % 21u) - 10f;
			string sprite = Leftovers[(int)((draw >> 16) % (uint)Leftovers.Length)];
			// Trottoir bouché par un immeuble : le bagage tombe de l'autre côté de la rue.
			for (int attempt = 0; attempt < 2; attempt++)
			{
				float s = attempt == 0 ? side : -side;
				Vector2 position = car + (horizontal
					? new Vector2(jitter * 2f, s * (SidewalkReachHorizontalPx + jitter * 0.4f))
					: new Vector2(s * (SidewalkReachVerticalPx + jitter * 0.4f), jitter));
				if (TryPlanSidewalk(layout, ground, usedCells, plan, sprite, position))
					break;
			}
		}
		return true;
	}

	/// <summary>
	/// Barrage d'évacuation, deux cellules après un carrefour : barrière en travers de la chaussée, côté départ (on ne
	/// revient pas vers l'Effacement), plots puis flèche peinte plus loin dans le sens de la fuite, panneau sur le trottoir
	/// devant la caméra.
	/// </summary>
	private static bool PlanBarricade(UrbanLayout layout, HashSet<Vector2I> intersections, ExodusConfig config, TileMapLayer ground,
		HashSet<Vector2I> usedCells, Vector2I start, uint hash, List<Placement> plan)
	{
		bool horizontal = layout.RoadCells.Contains(start + Vector2I.Right);
		Vector2I axis = horizontal ? Vector2I.Right : Vector2I.Down;
		Vector2I step = FlightStep(start, horizontal);
		// La recherche remonte vers le départ : passé l'axe 0, le sens de fuite s'inverserait, la scène est refusée.
		if (!FindBarricadeCell(layout, intersections, start, -step, out Vector2I cell)
			|| FlightStep(cell, horizontal) != step
			|| !IsFreeStraightRoad(layout, intersections, usedCells, cell))
			return false;

		Vector2I away = step;
		string direction = DirectionName(step);
		Vector2 barrier = UrbanPropPlacer.StreetPoint(layout, ground, cell);
		// En travers de la rue : l'axe du modèle suit l'autre axe de l'écran.
		plan.Add(new Placement($"{Folder}prop_exodus_barrier_{(horizontal ? "y" : "x")}.png", cell, barrier));

		// En vertical, une rangée ne descend que de 16 px : plots et flèche se posent plus loin en rangées.
		int reach = horizontal ? 1 : 3;
		Vector2I conesCell = cell + away * reach;
		Vector2I arrowCell = cell + away * reach * 2;
		if (!IsFreeStraightRoad(layout, intersections, usedCells, conesCell)
			|| !IsFreeStraightRoad(layout, intersections, usedCells, arrowCell))
			return false;
		plan.Add(new Placement(ConesSprite, conesCell, UrbanPropPlacer.StreetPoint(layout, ground, conesCell)));
		plan.Add(new Placement($"{Folder}prop_exodus_arrow_{direction}.png", arrowCell,
			UrbanPropPlacer.StreetPoint(layout, ground, arrowCell)));

		// Panneau au sud ou à l'est de la barrière, face à la caméra, lisible sans être masqué par elle.
		Vector2 signPosition = barrier + (horizontal ? new Vector2(0f, SidewalkReachHorizontalPx + 4f) : new Vector2(SignReachVerticalPx, 8f));
		if (!TryPlanSidewalk(layout, ground, usedCells, plan, $"{Folder}prop_exodus_sign_{direction}.png", signPosition))
			return false;

		int leftovers = Roll(config.LeftoversPerBarricadeMin, config.LeftoversPerBarricadeMax, hash >> 8);
		for (int i = 0; i < leftovers; i++)
		{
			uint draw = UrbanPropPlacer.HashCell(cell, (ulong)(hash ^ (uint)(i * 0x9E37)));
			Vector2 position = barrier + (horizontal ? new Vector2(-28f, -SidewalkReachHorizontalPx) : new Vector2(-SidewalkReachVerticalPx, -12f));
			TryPlanSidewalk(layout, ground, usedCells, plan, Leftovers[(int)((draw >> 16) % (uint)Leftovers.Length)], position);
		}
		return true;
	}

	/// <summary>
	/// Cellule du barrage : en remontant la rue depuis la candidate vers <paramref name="toward"/>, la première à deux
	/// cellules d'un carrefour.
	/// </summary>
	private static bool FindBarricadeCell(UrbanLayout layout, HashSet<Vector2I> intersections, Vector2I start, Vector2I toward,
		out Vector2I cell)
	{
		for (int k = 0; k <= BarricadeSearchCells; k++)
		{
			cell = start + toward * k;
			if (!layout.RoadCells.Contains(cell))
				break;
			if (intersections.Contains(cell + toward * 2))
				return true;
		}
		cell = start;
		return false;
	}

	/// <summary>
	/// Prévoit un petit décor sur un trottoir libre, ou sur la chaussée près d'un angle (il ne bloque pas) ; jamais dans
	/// un mur ni sur une case déjà prévue.
	/// </summary>
	private static bool TryPlanSidewalk(UrbanLayout layout, TileMapLayer ground, HashSet<Vector2I> usedCells,
		List<Placement> plan, string sprite, Vector2 position)
	{
		Vector2I cell = ground.LocalToMap(position);
		if (usedCells.Contains(cell) || (!layout.SidewalkCells.Contains(cell) && !layout.RoadCells.Contains(cell)))
			return false;
		foreach (Placement placement in plan)
		{
			if (placement.Cell == cell)
				return false;
		}
		plan.Add(new Placement(sprite, cell, position));
		return true;
	}

	/// <summary>Sens de la fuite le long de la rue : vers l'extérieur de la carte, loin du départ (0, 0).</summary>
	private static Vector2I FlightStep(Vector2I cell, bool horizontal)
	{
		if (horizontal)
			return cell.X >= 0 ? Vector2I.Right : Vector2I.Left;
		return cell.Y >= 0 ? Vector2I.Down : Vector2I.Up;
	}

	private static string DirectionName(Vector2I step)
	{
		if (step == Vector2I.Right)
			return "e";
		if (step == Vector2I.Left)
			return "w";
		return step == Vector2I.Down ? "s" : "n";
	}

	private static bool IsFreeStraightRoad(UrbanLayout layout, HashSet<Vector2I> intersections, HashSet<Vector2I> usedCells, Vector2I cell)
	{
		return layout.RoadCells.Contains(cell) && !usedCells.Contains(cell)
			&& UrbanPropPlacer.IsStraightRoadCell(layout, cell) && !UrbanPropPlacer.IsNearIntersection(intersections, cell);
	}

	private static bool IsTooClose(List<Vector2> anchors, Vector2 position, float minSpacingSq)
	{
		foreach (Vector2 anchor in anchors)
		{
			if (anchor.DistanceSquaredTo(position) < minSpacingSq)
				return true;
		}
		return false;
	}

	/// <summary>
	/// Vide autour d'un décor de scène : le semis aléatoire évite ces cellules (voisinage de <c>IsCrowded</c>). Une rangée
	/// ne descend que de 16 px, d'où un rayon double en y.
	/// </summary>
	private static void Quiet(HashSet<Vector2I> streetProps, Vector2I cell, int radius)
	{
		for (int dx = -radius; dx <= radius; dx++)
		{
			for (int dy = -radius * 2; dy <= radius * 2; dy++)
				streetProps.Add(new Vector2I(cell.X + dx, cell.Y + dy));
		}
	}

	private static int Roll(int min, int max, uint hash) => min + (int)(hash % (uint)(max - min + 1));
}
