using System;
using Godot;

namespace Vestiges.Combat;

/// <summary>Seul rappel physique d'un <see cref="TickRoster{T}"/> : Godot ne connaît pas les classes génériques.</summary>
public partial class TickRosterDriver : Node
{
	internal Action<double> Tick;

	public override void _PhysicsProcess(double delta) => Tick?.Invoke(delta);
}
