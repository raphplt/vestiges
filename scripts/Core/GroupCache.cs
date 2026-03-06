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
	private Array<Node> _resources = new();
	private Array<Node> _pois = new();
	private Node _player;
	private ulong _enemiesFrame;
	private ulong _structuresFrame;
	private ulong _resourcesFrame;
	private ulong _poisFrame;
	private ulong _playerFrame;

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

	public Array<Node> GetResources()
	{
		ulong frame = Engine.GetProcessFrames();
		if (frame != _resourcesFrame)
		{
			_resources = GetTree().GetNodesInGroup("resources");
			_resourcesFrame = frame;
		}
		return _resources;
	}

	public Array<Node> GetPois()
	{
		ulong frame = Engine.GetProcessFrames();
		if (frame != _poisFrame)
		{
			_pois = GetTree().GetNodesInGroup("pois");
			_poisFrame = frame;
		}
		return _pois;
	}

	public Node GetPlayer()
	{
		ulong frame = Engine.GetProcessFrames();
		if (frame != _playerFrame)
		{
			_player = GetTree().GetFirstNodeInGroup("player");
			_playerFrame = frame;
		}
		return _player;
	}
}
