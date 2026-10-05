using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Gère l'XP, les niveaux et déclenche le level up.
/// Attaché au Player ou comme enfant du Player.
/// </summary>
public partial class PlayerProgression : Node
{
    private float _currentXp;
    private int _currentLevel = 1;
    private float _xpToNextLevel;
    private float _xpMultiplier = 1f;
    private EventBus _eventBus;
    private XpCurveConfig _curve;
    private Player _player;

    public int CurrentLevel => _currentLevel;
    public float CurrentXp => _currentXp;
    public float XpToNextLevel => _xpToNextLevel;
    public float XpProgress => _xpToNextLevel > 0 ? _currentXp / _xpToNextLevel : 0f;

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _player = GetParent() as Player;
        _eventBus.XpGained += OnXpGained;
        _eventBus.DifficultyModifierChanged += OnDifficultyModifierChanged;
        // Courbe refusée : le chargement de la run s'arrête sur son message (GameBootstrap) ; aucun niveau ne s'atteint.
        _xpToNextLevel = XpCurveConfig.TryLoad(out _curve, out _) ? _curve.CostOf(_currentLevel) : float.PositiveInfinity;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.XpGained -= OnXpGained;
            _eventBus.DifficultyModifierChanged -= OnDifficultyModifierChanged;
        }
    }

    private void OnDifficultyModifierChanged(float enemyCountMult, float enemyHpMult, float enemyDmgMult, float xpMult)
    {
        _xpMultiplier = xpMult;
    }

    private void OnXpGained(float amount)
    {
        _currentXp += amount * _xpMultiplier * (_player?.XpGainMultiplier ?? 1f);

        while (_currentXp >= _xpToNextLevel)
        {
            _currentXp -= _xpToNextLevel;
            _currentLevel++;
            _xpToNextLevel = _curve.CostOf(_currentLevel);
            _eventBus.EmitSignal(EventBus.SignalName.LevelUp, _currentLevel);
            GD.Print($"[Progression] Level up! Now level {_currentLevel} (next: {_xpToNextLevel} XP)");
        }
    }
}
