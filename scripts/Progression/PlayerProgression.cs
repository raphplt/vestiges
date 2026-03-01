using Godot;
using Vestiges.Core;

namespace Vestiges.Progression;

/// <summary>
/// Gère l'XP, les niveaux et déclenche le level up.
/// Attaché au Player ou comme enfant du Player.
/// </summary>
public partial class PlayerProgression : Node
{
    private const float BaseXpToLevel = 20f;
    private const float XpScalingExponent = 1.35f;

    private float _currentXp;
    private int _currentLevel = 1;
    private float _xpToNextLevel;
    private EventBus _eventBus;

    public int CurrentLevel => _currentLevel;
    public float CurrentXp => _currentXp;
    public float XpToNextLevel => _xpToNextLevel;
    public float XpProgress => _xpToNextLevel > 0 ? _currentXp / _xpToNextLevel : 0f;

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.XpGained += OnXpGained;
        _xpToNextLevel = CalculateXpForLevel(_currentLevel);
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.XpGained -= OnXpGained;
    }

    private void OnXpGained(float amount)
    {
        _currentXp += amount;

        while (_currentXp >= _xpToNextLevel)
        {
            _currentXp -= _xpToNextLevel;
            _currentLevel++;
            _xpToNextLevel = CalculateXpForLevel(_currentLevel);
            _eventBus.EmitSignal(EventBus.SignalName.LevelUp, _currentLevel);
            GD.Print($"[Progression] Level up! Now level {_currentLevel} (next: {_xpToNextLevel} XP)");
        }
    }

    private static float CalculateXpForLevel(int level)
    {
        float baseXp = BaseXpToLevel * Mathf.Pow(level, XpScalingExponent);

        if (level <= 5)
        {
            float t = (level - 1) / 4f;
            float earlyMultiplier = Mathf.Lerp(1.65f, 1.20f, t);
            return baseXp * earlyMultiplier;
        }

        return baseXp;
    }
}
