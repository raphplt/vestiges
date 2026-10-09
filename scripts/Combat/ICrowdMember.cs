namespace Vestiges.Combat;

/// <summary>Cible recensée par <see cref="CrowdIndex"/> : son rang dans l'index, −1 hors de l'index.</summary>
public interface ICrowdMember
{
	int CrowdSlot { get; set; }

	/// <summary>Poussée de séparation du tick, calculée pour toute la foule par <see cref="CrowdSeparation"/>.</summary>
	Godot.Vector2 CrowdPush { get; set; }
}
