namespace Vestiges.Combat;

/// <summary>Nœud avancé par un <see cref="TickRoster{T}"/> plutôt que par un <c>_PhysicsProcess</c> à lui.</summary>
public interface ITicked
{
	/// <summary>Rang dans son registre, −1 hors du registre ; tenu par le registre.</summary>
	int TickSlot { get; set; }

	void PhysicsTick(double delta);
}
