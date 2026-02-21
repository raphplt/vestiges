using Godot;
using Vestiges.Core;

namespace Vestiges.Score;

/// <summary>
/// Compteur de score basique : points par kill.
/// </summary>
public partial class ScoreManager : Node
{
    private const int PointsPerMeleeKill = 10;
    private const int PointsPerRangedKill = 15;

    private int _currentScore;
    private int _totalKills;
    private EventBus _eventBus;

    public int CurrentScore => _currentScore;
    public int TotalKills => _totalKills;

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EnemyKilled += OnEnemyKilled;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.EnemyKilled -= OnEnemyKilled;
    }

    private void OnEnemyKilled(string enemyId, Vector2 position)
    {
        _totalKills++;

        int points = enemyId == "fading_spitter" ? PointsPerRangedKill : PointsPerMeleeKill;
        _currentScore += points;

        _eventBus.EmitSignal(EventBus.SignalName.ScoreChanged, _currentScore);
    }
}
