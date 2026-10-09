using System.Collections.Generic;
using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Liste de cibles prêtée par <see cref="CrowdIndex"/>, à rendre par <c>using</c>. Une requête faite pendant le parcours
/// d'une autre (un coup qui tue déclenche un objet) reçoit sa propre liste.
/// </summary>
public readonly struct CrowdQuery : System.IDisposable
{
	public readonly List<Node2D> Targets;

	internal CrowdQuery(List<Node2D> targets)
	{
		Targets = targets;
	}

	public void Dispose() => CrowdIndex.Return(Targets);
}
