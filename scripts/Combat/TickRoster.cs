using System.Collections.Generic;
using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Registre de nœuds avancés depuis une seule boucle C# par tick physique (plan 29). Godot appelle le pont C# pour
/// chaque nœud dont le traitement est actif : ≈ 3 à 9 µs par nœud et par tick, même pour un rappel vide, plus que la
/// logique d'une créature ou d'un projectile. Ici, un seul rappel moteur par registre, puis de simples appels C#.
/// Contrat : la pause de l'arbre arrête la boucle, le <c>ProcessMode</c> du nœud n'est pas lu.
/// </summary>
public sealed class TickRoster<T> where T : Node, ITicked
{
	private readonly string _name;
	private readonly List<T> _items = new(256);
	// Copie de la liste au début du tick : un nœud qui en retire ou en ajoute d'autres modifie la liste.
	private T[] _snapshot = new T[256];
	private TickRosterDriver _driver;

	public TickRoster(string name)
	{
		_name = name;
	}

	public int Count => _items.Count;

	public void Add(T item)
	{
		if (item.TickSlot >= 0)
			return;
		item.TickSlot = _items.Count;
		_items.Add(item);
		EnsureDriver(item);
	}

	public void Remove(T item)
	{
		int slot = item.TickSlot;
		if (slot < 0)
			return;
		int last = _items.Count - 1;
		T moved = _items[last];
		_items[slot] = moved;
		moved.TickSlot = slot;
		_items.RemoveAt(last);
		item.TickSlot = -1;
	}

	/// <summary>Le moteur de la boucle naît avec le premier nœud, à la racine : bancs et tests sans Main compris.</summary>
	private void EnsureDriver(T item)
	{
		if (_driver != null && GodotObject.IsInstanceValid(_driver))
			return;
		_driver = new TickRosterDriver { Name = _name, Tick = TickAll };
		item.GetTree().Root.CallDeferred(Node.MethodName.AddChild, _driver);
	}

	private void TickAll(double delta)
	{
		int count = _items.Count;
		if (_snapshot.Length < count)
			_snapshot = new T[count * 2];
		_items.CopyTo(_snapshot);
		for (int i = 0; i < count; i++)
		{
			T item = _snapshot[i];
			_snapshot[i] = null;
			// Retiré en cours de tick (rendu au pool, sorti de l'arbre) : il ne joue plus ce tick.
			if (item.TickSlot < 0)
				continue;
			// Comme avec un rappel par nœud : un nœud en faute ne fige pas ceux qui le suivent.
			try
			{
				item.PhysicsTick(delta);
			}
			catch (System.Exception exception)
			{
				GD.PushError($"[{_name}] {item.Name} : {exception}");
			}
		}
	}
}
