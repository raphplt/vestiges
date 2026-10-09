using System.Collections.Generic;
using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Contacts d'une orbe en orbite avec les créatures : rend celles qui viennent d'entrer dans son disque, comme le
/// faisait une zone physique quand les créatures avaient un corps (plan 29 B). Une créature qui sort puis revient est
/// frappée de nouveau.
/// </summary>
public sealed class OrbitContacts
{
	private HashSet<ulong> _touching = new();
	private HashSet<ulong> _next = new();

	/// <summary>Remplit <paramref name="entered"/> (vidée d'abord) des créatures entrées dans le disque depuis le dernier pas.</summary>
	public void Step(Vector2 center, float radius, List<Enemy> entered)
	{
		entered.Clear();
		_next.Clear();
		using CrowdQuery crowd = CrowdIndex.Near(center, radius + Enemy.LargestBodyRadius);
		foreach (Node2D node in crowd.Targets)
		{
			if (node is not Enemy { IsActive: true, IsDying: false, IsBurrowed: false } enemy)
				continue;
			float reach = radius + enemy.BodyRadius;
			if (enemy.GlobalPosition.DistanceSquaredTo(center) > reach * reach)
				continue;
			ulong id = enemy.GetInstanceId();
			_next.Add(id);
			if (!_touching.Contains(id))
				entered.Add(enemy);
		}
		(_touching, _next) = (_next, _touching);
	}
}
