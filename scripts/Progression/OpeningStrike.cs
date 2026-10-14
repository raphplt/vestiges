using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Œil d'aigle : une chance supplémentaire de critique au premier impact direct sur une créature indemne.
/// Le niveau du joueur fait progresser cette chance ; le critique ordinaire reste un seul multiplicateur.
/// </summary>
public partial class OpeningStrike : Node
{
    private Player _player;
    private PerkOpeningStrike _config;
    private PlayerProgression _progression;
    private EventBus _events;
    private int _level = 1;

    public float Chance => ChanceAt(_level);

    public void Initialize(Player player, PerkOpeningStrike config)
    {
        _player = player;
        _config = config;
        player.OpeningStrike = this;
    }

    public override void _Ready()
    {
        _progression = _player.GetNodeOrNull<PlayerProgression>("PlayerProgression");
        _level = _progression?.CurrentLevel ?? 1;
        _events = GetNode<EventBus>("/root/EventBus");
        _events.LevelUp += OnLevelUp;
    }

    public override void _ExitTree()
    {
        if (_events != null)
            _events.LevelUp -= OnLevelUp;
        if (_player != null && IsInstanceValid(_player) && _player.OpeningStrike == this)
            _player.OpeningStrike = null;
    }

    public float ChanceAt(int level) => Mathf.Clamp(_config.BaseChance
        + Mathf.Max(0, level - 1) * _config.GainPerLevel, 0f, _config.MaxChance);

    private void OnLevelUp(int level) => _level = _progression?.CurrentLevel ?? level;

    internal bool Eligible(AttackContext context) => !_player.IsDead
        && context.OwnerId == _player.GetInstanceId() && context.IsDirectWeapon && !context.IsEcho;

    internal float CombinedChance(Enemy enemy) => Mathf.Clamp(_player.CritChance + (enemy.IsUndamaged ? Chance : 0f), 0f, 1f);

    internal bool Roll(Enemy enemy, AttackContext context)
    {
        if (!Eligible(context))
            return false;
        float chance = CombinedChance(enemy);
        return chance > 0f && RunRandom.Combat.Randf() < chance;
    }
}
