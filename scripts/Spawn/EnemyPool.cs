using System.Collections.Generic;
using Godot;
using Vestiges.Combat;

namespace Vestiges.Spawn;

public partial class EnemyPool : Node
{
    [Export] public int InitialSize = 20;

    private PackedScene _enemyScene;
    private readonly Queue<Enemy> _available = new();
    private int _totalCreated;

    public int ActiveCount => _totalCreated - _available.Count;

    public override void _Ready()
    {
        _enemyScene = GD.Load<PackedScene>("res://scenes/enemies/Enemy.tscn");
        Prewarm();
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

    private void Prewarm()
    {
        for (int i = 0; i < InitialSize; i++)
        {
            Enemy enemy = CreateInstance();
            enemy.Reset();
            _available.Enqueue(enemy);
        }

        GD.Print($"[EnemyPool] Prewarmed {InitialSize} enemies");
    }

    private Enemy CreateInstance()
    {
        Enemy enemy = _enemyScene.Instantiate<Enemy>();
        _totalCreated++;
        return enemy;
    }
}
