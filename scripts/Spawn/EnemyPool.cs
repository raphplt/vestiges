using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;

namespace Vestiges.Spawn;

public partial class EnemyPool : Node
{
	[Export] public int InitialSize = 20;

	private PackedScene _enemyScene;
	private readonly Queue<Enemy> _available = new();
	private int _totalCreated;
	private bool _prewarmed;

	public int ActiveCount => _totalCreated - _available.Count;

	public override void _Ready()
	{
		_enemyScene = GD.Load<PackedScene>("res://scenes/enemies/Enemy.tscn");
		// Ne plus prewarm ici — GameBootstrap appelle PrewarmAsync() ou PrewarmSync()
	}

	public Enemy Get()
	{
		Enemy enemy;

		if (_available.Count > 0)
		{
			enemy = _available.Dequeue();
		}
		else
		{
			enemy = CreateInstance();
		}

		return enemy;
	}

	public void Return(Enemy enemy)
	{
		enemy.Reset();

		Node parent = enemy.GetParent();
		if (parent != null)
			parent.RemoveChild(enemy);

		_available.Enqueue(enemy);
	}

	/// <summary>Prewarm étalé sur plusieurs frames (4 ennemis par frame).</summary>
	public async Task PrewarmAsync(int perFrame = 4)
	{
		if (_prewarmed) return;
		_prewarmed = true;

		for (int i = 0; i < InitialSize; i++)
		{
			Enemy enemy = CreateInstance();
			enemy.Reset();
			_available.Enqueue(enemy);

			if ((i + 1) % perFrame == 0)
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		GD.Print($"[EnemyPool] Prewarmed {InitialSize} enemies (async)");
	}

	/// <summary>Prewarm synchrone (pour simulation).</summary>
	public void PrewarmSync()
	{
		if (_prewarmed) return;
		_prewarmed = true;

		for (int i = 0; i < InitialSize; i++)
		{
			Enemy enemy = CreateInstance();
			enemy.Reset();
			_available.Enqueue(enemy);
		}

		GD.Print($"[EnemyPool] Prewarmed {InitialSize} enemies (sync)");
	}

	private Enemy CreateInstance()
	{
		Enemy enemy = _enemyScene.Instantiate<Enemy>();
		_totalCreated++;
		return enemy;
	}
}
