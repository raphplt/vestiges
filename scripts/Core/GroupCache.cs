using Godot;
using Godot.Collections;

namespace Vestiges.Core;

/// <summary>
/// Cache centralisé pour GetNodesInGroup(). Appelle une seule fois par frame
/// et partage le résultat avec tous les consommateurs (ennemis, tourelles, projectiles).
/// Autoload — jamais instancié manuellement.
/// </summary>
public partial class GroupCache : Node
{
	private Array<Node> _enemies = new();
	private Array<Node> _structures = new();
	private ulong _enemiesFrame;
	private ulong _structuresFrame;

	public Array<Node> GetEnemies()
	{
		ulong frame = Engine.GetProcessFrames();
		if (frame != _enemiesFrame)
		{
			_enemies = GetTree().GetNodesInGroup("enemies");
			_enemiesFrame = frame;
		}
		return _enemies;
	}

	public Array<Node> GetStructures()
	{
		ulong frame = Engine.GetProcessFrames();
		if (frame != _structuresFrame)
		{
			_structures = GetTree().GetNodesInGroup("structures");
			_structuresFrame = frame;
		}
		return _structures;
	}
}
