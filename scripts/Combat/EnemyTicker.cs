using System.Collections.Generic;
using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Fait avancer toutes les créatures actives depuis une seule boucle C#. Godot appelait un <c>_PhysicsProcess</c> par
/// créature, et le passage du moteur au C# coûte à lui seul ≈ 9 µs par appel, plus que la logique de la créature
/// (plan 29 §1.3). Ici, un seul rappel moteur par tick, puis de simples appels C#.
/// Contrat : la pause de l'arbre arrête la boucle, mais le <c>ProcessMode</c> d'une créature ou de son conteneur n'est
/// plus lu ; pour figer une créature, <see cref="Enemy.SetTicking"/>.
/// </summary>
public partial class EnemyTicker : Node
{
	private static readonly List<Enemy> Ticking = new(512);
	private static EnemyTicker _driver;
	// Copie de la liste au début du tick : une créature qui meurt, appelle des renforts ou en tue une autre modifie la
	// liste pendant le parcours.
	private static Enemy[] _snapshot = new Enemy[512];

	internal static void Register(Enemy enemy)
	{
		if (enemy.TickSlot >= 0)
			return;
		enemy.TickSlot = Ticking.Count;
		Ticking.Add(enemy);
		EnsureDriver(enemy);
	}

	internal static void Unregister(Enemy enemy)
	{
		int slot = enemy.TickSlot;
		if (slot < 0)
			return;
		int last = Ticking.Count - 1;
		Enemy moved = Ticking[last];
		Ticking[slot] = moved;
		moved.TickSlot = slot;
		Ticking.RemoveAt(last);
		enemy.TickSlot = -1;
	}

	/// <summary>Le moteur de la boucle naît avec la première créature, à la racine : bancs et tests sans Main compris.</summary>
	private static void EnsureDriver(Enemy enemy)
	{
		if (_driver != null && IsInstanceValid(_driver))
			return;
		_driver = new EnemyTicker { Name = nameof(EnemyTicker) };
		enemy.GetTree().Root.CallDeferred(Node.MethodName.AddChild, _driver);
	}

	public override void _ExitTree()
	{
		if (_driver == this)
			_driver = null;
	}

	public override void _PhysicsProcess(double delta)
	{
		int count = Ticking.Count;
		if (_snapshot.Length < count)
			_snapshot = new Enemy[count * 2];
		Ticking.CopyTo(_snapshot);
		for (int i = 0; i < count; i++)
		{
			Enemy enemy = _snapshot[i];
			_snapshot[i] = null;
			// Retirée en cours de tick (rendue au pool, sortie de l'arbre) : elle ne joue plus ce tick.
			if (enemy.TickSlot < 0)
				continue;
			// Comme avec un rappel par nœud : une créature en faute ne fige pas celles qui la suivent.
			try
			{
				enemy.PhysicsTick(delta);
			}
			catch (System.Exception exception)
			{
				GD.PushError($"[EnemyTicker] {enemy.Name} : {exception}");
			}
		}
	}
}
