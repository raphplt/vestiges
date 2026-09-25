using Godot;
using Vestiges.Combat;
using Vestiges.Combat.Abilities;

namespace Vestiges.Events.RunEvents;

/// <summary>
/// Averse d'éclats : des éclats de mémoire s'abattent autour du joueur, surtout sur les créatures.
/// Chaque impact est annoncé au sol ; il blesse aussi le joueur resté dessous.
/// Attirer les créatures sous les éclats rapporte de l'Essence : un danger qui se retourne.
/// </summary>
public sealed class ShardRainEvent : RunEvent
{
    private const int StrikePoolSize = 12;

    private readonly GroundTelegraph[] _strikes = new GroundTelegraph[StrikePoolSize];
    private readonly float[] _strikeTimers = new float[StrikePoolSize];
    private readonly float[] _flashTimers = new float[StrikePoolSize];
    private float _spawnTimer;
    private float _strikeDelay;
    private float _radius;
    private float _enemyDamage;
    private int _kills;
    private string _objectiveTemplate;

    protected override void OnStart()
    {
        _strikeDelay = Data.Number("strike_delay_sec", 1f);
        _radius = Data.Number("strike_radius", 36f);
        _enemyDamage = Data.Number("enemy_damage_base", 50f)
            * Mathf.Pow(1f + Data.Number("enemy_damage_growth_per_minute", 0.08f), Context.ElapsedMinutes);
        for (int i = 0; i < StrikePoolSize; i++)
        {
            _strikes[i] = new GroundTelegraph();
            Context.WorldRoot.AddChild(_strikes[i]);
        }
        _objectiveTemplate = Tr(Data.ObjectiveKey);
        Objective = string.Format(_objectiveTemplate, 0);
        HasTarget = false;
    }

    protected override void OnTick(float delta)
    {
        Progress = 1f - TimeRemaining / Data.DurationSec;

        for (int i = 0; i < StrikePoolSize; i++)
        {
            if (_strikeTimers[i] > 0f)
            {
                _strikeTimers[i] -= delta;
                _strikes[i].SetProgress(1f - _strikeTimers[i] / _strikeDelay);
                if (_strikeTimers[i] <= 0f)
                    Impact(i);
            }
            else if (_flashTimers[i] > 0f)
            {
                _flashTimers[i] -= delta;
                _strikes[i].SetFlash(_flashTimers[i] / 0.25f);
                if (_flashTimers[i] <= 0f)
                    _strikes[i].HideMarker();
            }
        }

        _spawnTimer -= delta;
        if (_spawnTimer <= 0f && TimeRemaining > _strikeDelay)
        {
            _spawnTimer = Data.Number("strike_interval_sec", 0.3f);
            LaunchStrike();
        }
    }

    private void LaunchStrike()
    {
        int slot = -1;
        for (int i = 0; i < StrikePoolSize; i++)
        {
            if (_strikeTimers[i] <= 0f && _flashTimers[i] <= 0f)
            {
                slot = i;
                break;
            }
        }
        if (slot < 0)
            return;

        _strikes[slot].ShowCircle(PickStrikePoint(), _radius, FxFamily.Hybrid);
        _strikeTimers[slot] = _strikeDelay;
    }

    /// <summary>Surtout sur une créature proche, parfois près du joueur, sinon au hasard dans la zone.</summary>
    private Vector2 PickStrikePoint()
    {
        Vector2 player = Context.Player.GlobalPosition;
        float range = Data.Number("strike_range", 330f);
        float roll = Context.Rng.Randf();
        if (roll < Data.Number("target_enemy_ratio", 0.65f))
        {
            // Tirage uniforme sans liste intermédiaire parmi les créatures à portée.
            Enemy chosen = null;
            int seen = 0;
            float rangeSq = range * range;
            foreach (Node node in Context.Groups.GetEnemies())
            {
                if (node is not Enemy enemy || !enemy.IsActive || enemy.IsDying)
                    continue;
                if (enemy.GlobalPosition.DistanceSquaredTo(player) > rangeSq)
                    continue;
                seen++;
                if (Context.Rng.RandiRange(1, seen) == 1)
                    chosen = enemy;
            }
            if (chosen != null)
                return chosen.GlobalPosition;
        }
        if (roll < Data.Number("target_enemy_ratio", 0.65f) + Data.Number("target_player_ratio", 0.2f))
            return Context.PointAround(player, 30f, 90f);
        return Context.PointAround(player, 60f, range);
    }

    private void Impact(int slot)
    {
        Vector2 position = _strikes[slot].GlobalPosition;
        _kills += Context.DamageEnemiesInRadius(position, _radius, _enemyDamage);
        Context.DamagePlayerIfInside(position, _radius, Data.Number("player_damage_ratio", 0.12f));
        _strikes[slot].SetFlash(1f);
        _flashTimers[slot] = 0.25f;
        ScreenShake.Instance?.ShakeLight();
        Objective = string.Format(_objectiveTemplate, _kills);
    }

    protected override void OnTimeout()
    {
        int essence = _kills / Mathf.Max(1, (int)Data.Number("essence_per_kills", 3f));
        Context.GrantEssence(essence);
        if (essence > 0)
            Succeed(string.Format(Tr("EVENT_SHARDS_SUMMARY"), _kills) + " · " + RewardLine(essence, false, false, false));
        else
            Fail(string.Format(Tr("EVENT_SHARDS_SUMMARY"), _kills));
    }

    public override void Cleanup()
    {
        for (int i = 0; i < StrikePoolSize; i++)
        {
            _strikes[i]?.QueueFree();
            _strikes[i] = null;
        }
    }
}
