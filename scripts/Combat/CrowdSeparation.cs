using System.Collections.Generic;
using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Poussées de séparation de toute la foule, calculées une fois par tick (plan 29 F2). Les cibles de l'instantané de
/// <see cref="CrowdIndex"/> sont triées par case d'une grille dont la case vaut le rayon : chaque paire de voisines
/// n'est examinée qu'une fois, dans sa case et dans la moitié avant des cases voisines, et la poussée s'applique aux
/// deux. Une créature par case de 64 px parcourue à chaque tick coûtait 13 µs par créature à 1 000 (plan 29 §4, B).
/// </summary>
public static class CrowdSeparation
{
	// Moitié avant du voisinage : (0,0) se traite à part, les quatre autres voisines viennent de l'autre côté.
	private static readonly Vector2I[] ForwardNeighbours = { new(1, 0), new(-1, 1), new(0, 1), new(1, 1) };
	private static readonly Dictionary<long, int> CellStart = new();
	private static long[] _keys = new long[512];
	private static int[] _order = new int[512];
	private static Vector2[] _sorted = new Vector2[512];
	private static Vector2[] _push = new Vector2[512];
	// Une créature avance de quelques pixels par tick : la poussée calculée au tick précédent reste juste, et la
	// recalculer un tick sur deux divise son coût par deux.
	private const ulong RefreshTicks = 2;
	private static ulong _tick = ulong.MaxValue;
	private static float _radius;

	/// <summary>
	/// Somme des directions qui écartent <paramref name="self"/> de ses voisines à moins de <paramref name="radius"/>,
	/// pondérées de 1 au contact à 0 au bord, calculée au plus deux ticks plus tôt. Nulle pour une créature inscrite
	/// depuis ce calcul.
	/// </summary>
	public static Vector2 PushFor(Enemy self, float radius)
	{
		ulong tick = Engine.GetPhysicsFrames();
		if (tick - _tick >= RefreshTicks || tick < _tick || radius != _radius)
		{
			_tick = tick;
			_radius = radius;
			Compute(radius);
		}
		return self.CrowdPush;
	}

	private static void Compute(float radius)
	{
		int count = CrowdIndex.Snapshot(out Node2D[] targets, out Vector2[] positions);
		if (_keys.Length < count)
		{
			int capacity = count * 2;
			_keys = new long[capacity];
			_order = new int[capacity];
			_sorted = new Vector2[capacity];
			_push = new Vector2[capacity];
		}
		for (int i = 0; i < count; i++)
		{
			_keys[i] = Key(Cell(positions[i].X, radius), Cell(positions[i].Y, radius));
			_order[i] = i;
		}
		System.Array.Sort(_keys, _order, 0, count);
		CellStart.Clear();
		for (int k = 0; k < count; k++)
		{
			_sorted[k] = positions[_order[k]];
			_push[k] = Vector2.Zero;
			if (k == 0 || _keys[k] != _keys[k - 1])
				CellStart[_keys[k]] = k;
		}

		float radiusSq = radius * radius;
		int start = 0;
		while (start < count)
		{
			long key = _keys[start];
			int end = start + 1;
			while (end < count && _keys[end] == key)
				end++;
			for (int a = start; a < end; a++)
			{
				for (int b = a + 1; b < end; b++)
					Pair(a, b, radius, radiusSq);
			}
			int cx = (int)(key >> 32), cy = (int)(uint)key;
			foreach (Vector2I offset in ForwardNeighbours)
			{
				if (!CellStart.TryGetValue(Key(cx + offset.X, cy + offset.Y), out int other))
					continue;
				long otherKey = _keys[other];
				for (int b = other; b < count && _keys[b] == otherKey; b++)
				{
					for (int a = start; a < end; a++)
						Pair(a, b, radius, radiusSq);
				}
			}
			start = end;
		}

		for (int k = 0; k < count; k++)
		{
			if (targets[_order[k]] is ICrowdMember member)
				member.CrowdPush = _push[k];
		}
	}

	private static void Pair(int a, int b, float radius, float radiusSq)
	{
		Vector2 away = _sorted[a] - _sorted[b];
		float distanceSq = away.LengthSquared();
		if (distanceSq >= radiusSq)
			return;
		Vector2 direction;
		float weight;
		if (distanceSq < 0.0001f)
		{
			// Deux cibles confondues : une direction propre à la paire, opposée pour chacune, stable d'un tick à l'autre.
			direction = Vector2.FromAngle(_order[a] * 2.3999632f);
			weight = 1f;
		}
		else
		{
			float distance = Mathf.Sqrt(distanceSq);
			direction = away / distance;
			weight = 1f - distance / radius;
		}
		_push[a] += direction * weight;
		_push[b] -= direction * weight;
	}

	private static int Cell(float coordinate, float size) => Mathf.FloorToInt(coordinate / size);

	private static long Key(int cx, int cy) => ((long)cx << 32) | (uint)cy;
}
