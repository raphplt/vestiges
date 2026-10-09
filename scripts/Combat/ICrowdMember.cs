namespace Vestiges.Combat;

/// <summary>Cible recensée par <see cref="CrowdIndex"/> : son rang dans l'index, −1 hors de l'index.</summary>
public interface ICrowdMember
{
	int CrowdSlot { get; set; }
}
