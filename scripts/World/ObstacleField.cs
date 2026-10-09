using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Emprises au sol des décors bloquants (polygones convexes), tenues en C# dans une grille, pour que les créatures
/// les contournent sans corps physique (plan 29, lot B). Les décors ne bougent pas : chacun s'inscrit avec son
/// polygone local, et la grille le passe en coordonnées monde au premier tick où le décor est posé dans l'arbre.
/// <see cref="Clear"/> au début de chaque run : les décors de la run précédente ne bloquent plus rien.
/// </summary>
public static class ObstacleField
{
	private const float CellSize = 64f;
	// Deux passes : une créature coincée entre deux décors proches sort des deux.
	private const int ResolvePasses = 2;
	// Un décor inscrit mais jamais posé dans l'arbre cesse d'être attendu au bout de ce nombre de ticks.
	private const ulong PendingTicks = 600;

	private readonly struct Pending
	{
		public readonly Node2D Owner;
		public readonly Vector2[] LocalPoints;
		public readonly ulong Since;

		public Pending(Node2D owner, Vector2[] localPoints, ulong since)
		{
			Owner = owner;
			LocalPoints = localPoints;
			Since = since;
		}
	}

	private readonly struct Obstacle
	{
		public readonly Vector2[] Points;
		public readonly Rect2 Bounds;

		public Obstacle(Vector2[] points, Rect2 bounds)
		{
			Points = points;
			Bounds = bounds;
		}
	}

	private static readonly List<Pending> PendingList = new();
	private static readonly List<Obstacle> Obstacles = new();
	private static readonly Dictionary<long, List<int>> Cells = new();
	private static ulong _flushedTick = ulong.MaxValue;

	/// <summary>Oublie tous les décors : à appeler au début d'une run, avant que les siens ne s'inscrivent.</summary>
	public static void Clear()
	{
		PendingList.Clear();
		Obstacles.Clear();
		Cells.Clear();
		_flushedTick = ulong.MaxValue;
	}

	/// <summary>
	/// Inscrit l'emprise d'un décor bloquant, en coordonnées locales du décor. Comme <c>ConvexPolygonShape2D</c>, seule
	/// l'enveloppe convexe des points compte ; une emprise sans surface est ignorée.
	/// </summary>
	public static void Add(Node2D owner, Vector2[] localPoints)
	{
		if (localPoints == null || localPoints.Length < 3)
			return;
		Vector2[] hull = Geometry2D.ConvexHull(localPoints);
		// ConvexHull referme le contour en répétant le premier point.
		if (hull.Length > 1 && hull[0] == hull[^1])
			System.Array.Resize(ref hull, hull.Length - 1);
		if (hull.Length < 3 || Mathf.Abs(SignedArea(hull)) < 1f)
			return;
		PendingList.Add(new Pending(owner, hull, Engine.GetPhysicsFrames()));
	}

	/// <summary>
	/// Sort un disque de rayon <paramref name="radius"/> centré en <paramref name="position"/> des décors qu'il chevauche,
	/// par la plus courte translation : la composante qui entre dans le décor disparaît, celle qui le longe reste.
	/// Vrai si la position a changé.
	/// </summary>
	public static bool Resolve(ref Vector2 position, float radius)
	{
		Flush();
		if (Cells.Count == 0)
			return false;
		bool moved = false;
		for (int pass = 0; pass < ResolvePasses; pass++)
		{
			bool movedThisPass = false;
			int minX = Cell(position.X - radius), maxX = Cell(position.X + radius);
			int minY = Cell(position.Y - radius), maxY = Cell(position.Y + radius);
			for (int cx = minX; cx <= maxX; cx++)
			{
				for (int cy = minY; cy <= maxY; cy++)
				{
					if (!Cells.TryGetValue(Key(cx, cy), out List<int> ids))
						continue;
					foreach (int id in ids)
					{
						Obstacle obstacle = Obstacles[id];
						if (!obstacle.Bounds.Grow(radius).HasPoint(position))
							continue;
						if (PushOut(obstacle.Points, ref position, radius))
							movedThisPass = true;
					}
				}
			}
			if (!movedThisPass)
				break;
			moved = true;
		}
		return moved;
	}

	/// <summary>Passe en coordonnées monde les décors posés depuis le dernier tick ; une fois par tick au plus.</summary>
	private static void Flush()
	{
		ulong tick = Engine.GetPhysicsFrames();
		if (PendingList.Count == 0 || tick == _flushedTick)
			return;
		_flushedTick = tick;
		for (int i = PendingList.Count - 1; i >= 0; i--)
		{
			Pending pending = PendingList[i];
			bool valid = GodotObject.IsInstanceValid(pending.Owner);
			if (valid && pending.Owner.IsInsideTree())
			{
				Insert(pending.Owner.GlobalTransform, pending.LocalPoints);
				PendingList.RemoveAt(i);
			}
			else if (!valid || tick - pending.Since > PendingTicks)
			{
				PendingList.RemoveAt(i);
			}
		}
	}

	private static void Insert(Transform2D transform, Vector2[] localPoints)
	{
		Vector2[] points = new Vector2[localPoints.Length];
		Rect2 bounds = new(transform * localPoints[0], Vector2.Zero);
		for (int p = 0; p < points.Length; p++)
		{
			points[p] = transform * localPoints[p];
			bounds = bounds.Expand(points[p]);
		}
		int id = Obstacles.Count;
		Obstacles.Add(new Obstacle(points, bounds));
		for (int cx = Cell(bounds.Position.X); cx <= Cell(bounds.End.X); cx++)
		{
			for (int cy = Cell(bounds.Position.Y); cy <= Cell(bounds.End.Y); cy++)
			{
				long key = Key(cx, cy);
				if (!Cells.TryGetValue(key, out List<int> ids))
				{
					ids = new List<int>(2);
					Cells[key] = ids;
				}
				ids.Add(id);
			}
		}
	}

	/// <summary>Plus courte sortie d'un disque hors d'un polygone convexe non dégénéré, de sens quelconque.</summary>
	private static bool PushOut(Vector2[] points, ref Vector2 center, float radius)
	{
		float bestDistanceSq = float.MaxValue;
		Vector2 closest = Vector2.Zero;
		bool inside = true;
		float winding = 0f;
		for (int i = 0; i < points.Length; i++)
		{
			Vector2 a = points[i];
			Vector2 b = points[(i + 1) % points.Length];
			float cross = (b - a).Cross(center - a);
			// Le centre est dedans s'il reste strictement du même côté de toutes les arêtes.
			if (cross == 0f)
				inside = false;
			else if (winding == 0f)
				winding = Mathf.Sign(cross);
			else if (Mathf.Sign(cross) != winding)
				inside = false;
			Vector2 candidate = Geometry2D.GetClosestPointToSegment(center, a, b);
			float distanceSq = candidate.DistanceSquaredTo(center);
			if (distanceSq < bestDistanceSq)
			{
				bestDistanceSq = distanceSq;
				closest = candidate;
			}
		}
		float distance = Mathf.Sqrt(bestDistanceSq);
		if (inside)
		{
			Vector2 toEdge = distance > 0.0001f ? (closest - center) / distance : Vector2.Up;
			center = closest + toEdge * radius;
			return true;
		}
		if (distance >= radius)
			return false;
		Vector2 away = distance > 0.0001f ? (center - closest) / distance : Vector2.Up;
		center = closest + away * radius;
		return true;
	}

	private static float SignedArea(Vector2[] points)
	{
		float area = 0f;
		for (int i = 0; i < points.Length; i++)
			area += points[i].Cross(points[(i + 1) % points.Length]);
		return area * 0.5f;
	}

	private static int Cell(float coordinate) => Mathf.FloorToInt(coordinate / CellSize);

	private static long Key(int cx, int cy) => ((long)cx << 32) | (uint)cy;
}
