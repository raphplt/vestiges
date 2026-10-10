using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>
/// Une rencontre de boss pour la barre du haut de l'écran (plan 07 B1b) : l'ouvre, la tient à jour et la referme une
/// seule fois. Chaque rencontre a son numéro, si bien qu'une fin tardive ne referme jamais la barre d'un boss suivant.
/// </summary>
public sealed class BossBarFeed
{
	private static int _lastId;
	private readonly EventBus _bus;
	private float _shownHp;

	public int Id { get; }
	public bool IsOpen { get; private set; }

	public BossBarFeed(EventBus bus, string bossName, float maxHp, int notches = 0)
	{
		_bus = bus;
		Id = ++_lastId;
		IsOpen = true;
		_shownHp = maxHp;
		_bus.EmitSignal(EventBus.SignalName.BossEncounterStarted, Id, bossName, maxHp, notches);
	}

	/// <summary>Sans effet si les PV n'ont pas changé : peut être appelé à chaque tick.</summary>
	public void Update(float currentHp, float maxHp)
	{
		if (!IsOpen || currentHp == _shownHp)
			return;
		_shownHp = currentHp;
		_bus.EmitSignal(EventBus.SignalName.BossHealthChanged, Id, currentHp, maxHp);
	}

	public void End(bool defeated)
	{
		if (!IsOpen)
			return;
		IsOpen = false;
		_bus.EmitSignal(EventBus.SignalName.BossEncounterEnded, Id, defeated);
	}
}
