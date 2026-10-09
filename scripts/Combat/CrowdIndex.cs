using System.Collections.Generic;
using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Index des cibles hostiles vivantes (créatures, Indicible), tenu en C# avec une grille de hachage. Remplace le
/// parcours de tout le groupe « enemies », qui relisait chaque créature à travers l'interop pour chaque arme, chaque
/// projectile et chaque objet (plan 29, lot A).
/// La grille est reconstruite au plus une fois par tick physique et par image, ou après une inscription. Une requête
/// rend des candidats : les cibles vivantes dans le rayon augmenté d'une marge de déplacement ; l'appelant garde son
/// test exact.
/// </summary>
public static class CrowdIndex
{
	private const float CellSize = 64f;
	private const int TableSize = 4096;
	// Déplacement possible depuis la reconstruction (charge, recul) : une cible n'échappe pas à une requête faite
	// en cours de tick.
	private const float StaleMargin = 48f;

	private static readonly List<Node2D> Members = new(512);
	private static readonly int[] Heads = new int[TableSize];
	private static Node2D[] _grid = new Node2D[512];
	private static Vector2[] _positions = new Vector2[512];
	private static Vector2I[] _cells = new Vector2I[512];
	private static int[] _next = new int[512];
	private static int _gridCount;
	private static ulong _builtTick = ulong.MaxValue;
	private static ulong _builtFrame = ulong.MaxValue;
	private static bool _dirty = true;
	private static readonly Stack<List<Node2D>> Lists = new();

	public static int Count => Members.Count;

	public static void Register<T>(T target) where T : Node2D, ICrowdMember
	{
		if (target.CrowdSlot >= 0)
			return;
		target.CrowdSlot = Members.Count;
		Members.Add(target);
		_dirty = true;
	}

	public static void Unregister<T>(T target) where T : Node2D, ICrowdMember
	{
		int slot = target.CrowdSlot;
		if (slot < 0)
			return;
		int last = Members.Count - 1;
		Node2D moved = Members[last];
		Members[slot] = moved;
		((ICrowdMember)moved).CrowdSlot = slot;
		Members.RemoveAt(last);
		target.CrowdSlot = -1;
	}

	/// <summary>
	/// À appeler après avoir déplacé une cible hors de son tick (téléport, mise en place) : la grille sera relue à la
	/// prochaine requête. Les déplacements faits pendant un tick restent dans la marge et n'en ont pas besoin.
	/// </summary>
	public static void MarkMoved() => _dirty = true;

	/// <summary>Toutes les cibles vivantes, copiées : l'appelant peut en tuer pendant son parcours.</summary>
	public static CrowdQuery All()
	{
		List<Node2D> list = Rent();
		list.AddRange(Members);
		return new CrowdQuery(list);
	}

	/// <summary>Candidats à moins de <paramref name="radius"/> de <paramref name="center"/> (voir <see cref="Candidates"/>).</summary>
	public static CrowdQuery Near(Vector2 center, float radius)
	{
		List<Node2D> list = Rent();
		Candidates(center, radius, list);
		return new CrowdQuery(list);
	}

	private static List<Node2D> Rent() => Lists.Count > 0 ? Lists.Pop() : new List<Node2D>(64);

	internal static void Return(List<Node2D> list)
	{
		list.Clear();
		Lists.Push(list);
	}

	/// <summary>
	/// Cibles vivantes susceptibles d'être à moins de <paramref name="radius"/> de <paramref name="center"/> (liste vidée
	/// d'abord). Sur-ensemble : l'appelant applique sa propre distance, mesurée sur <c>GlobalPosition</c>.
	/// </summary>
	public static void Candidates(Vector2 center, float radius, List<Node2D> into)
	{
		into.Clear();
		Refresh();
		float reach = radius + StaleMargin;
		float reachSq = reach * reach;
		int minX = Cell(center.X - reach), maxX = Cell(center.X + reach);
		int minY = Cell(center.Y - reach), maxY = Cell(center.Y + reach);
		// Très grand rayon : parcourir les cibles coûte moins que les cases.
		if ((long)(maxX - minX + 1) * (maxY - minY + 1) > _gridCount)
		{
			for (int i = 0; i < _gridCount; i++)
				Accept(i, center, reachSq, into);
			return;
		}
		for (int cx = minX; cx <= maxX; cx++)
		{
			for (int cy = minY; cy <= maxY; cy++)
			{
				Vector2I cell = new(cx, cy);
				for (int i = Heads[Hash(cx, cy)]; i >= 0; i = _next[i])
				{
					if (_cells[i] == cell)
						Accept(i, center, reachSq, into);
				}
			}
		}
	}

	/// <summary>
	/// Poussée qui écarte <paramref name="self"/> des cibles à moins de <paramref name="radius"/> : somme des directions
	/// opposées, pondérées de 1 au contact à 0 au bord (plan 29 F2). Lit les positions de la grille, sans interop.
	/// </summary>
	public static Vector2 SeparationPush(Node2D self, Vector2 position, float radius)
	{
		Refresh();
		float radiusSq = radius * radius;
		int cx = Cell(position.X), cy = Cell(position.Y);
		int reach = Mathf.CeilToInt(radius / CellSize);
		Vector2 push = Vector2.Zero;
		for (int x = cx - reach; x <= cx + reach; x++)
		{
			for (int y = cy - reach; y <= cy + reach; y++)
			{
				Vector2I cell = new(x, y);
				for (int i = Heads[Hash(x, y)]; i >= 0; i = _next[i])
				{
					if (_cells[i] != cell || _grid[i] == self)
						continue;
					Vector2 away = position - _positions[i];
					float distanceSq = away.LengthSquared();
					if (distanceSq >= radiusSq)
						continue;
					// Deux créatures confondues : une direction propre à chacune, stable d'un tick à l'autre.
					if (distanceSq < 0.0001f)
					{
						push += Vector2.FromAngle(i * 2.3999632f);
						continue;
					}
					float distance = Mathf.Sqrt(distanceSq);
					push += away / distance * (1f - distance / radius);
				}
			}
		}
		return push;
	}

	private static void Accept(int index, Vector2 center, float reachSq, List<Node2D> into)
	{
		Node2D target = _grid[index];
		// Retirée depuis la reconstruction (morte, rendue au pool) : plus une cible.
		if (((ICrowdMember)target).CrowdSlot < 0 || center.DistanceSquaredTo(_positions[index]) > reachSq)
			return;
		into.Add(target);
	}

	private static void Refresh()
	{
		// Relue à chaque tick physique (les créatures y bougent) et à chaque image (l'Indicible bouge en _Process).
		ulong tick = Engine.GetPhysicsFrames();
		ulong frame = Engine.GetProcessFrames();
		if (!_dirty && tick == _builtTick && frame == _builtFrame)
			return;
		_dirty = false;
		_builtTick = tick;
		_builtFrame = frame;
		int count = Members.Count;
		if (_grid.Length < count)
		{
			int capacity = count * 2;
			_grid = new Node2D[capacity];
			_positions = new Vector2[capacity];
			_cells = new Vector2I[capacity];
			_next = new int[capacity];
		}
		System.Array.Fill(Heads, -1);
		for (int i = 0; i < count; i++)
		{
			Node2D target = Members[i];
			Vector2 position = target.GlobalPosition;
			Vector2I cell = new(Cell(position.X), Cell(position.Y));
			_grid[i] = target;
			_positions[i] = position;
			_cells[i] = cell;
			int head = Hash(cell.X, cell.Y);
			_next[i] = Heads[head];
			Heads[head] = i;
		}
		// Les anciennes entrées au-delà du compte ne retiennent plus de nœud.
		System.Array.Clear(_grid, count, _gridCount > count ? _gridCount - count : 0);
		_gridCount = count;
	}

	private static int Cell(float coordinate) => Mathf.FloorToInt(coordinate / CellSize);

	private static int Hash(int cx, int cy) => ((cx * 73856093) ^ (cy * 19349663)) & (TableSize - 1);
}
