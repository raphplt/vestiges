using Godot;
using Vestiges.Combat;

namespace Vestiges.Events.RunEvents;

/// <summary>
/// Harde : une bande de créatures traverse la zone en ligne droite et piétine ce qu'elle croise.
/// Abattre la majorité avant qu'elle ne passe rapporte XP renforcée, coffre et Essence.
/// Récompense les builds de zone et le placement de biais par rapport à la charge.
/// </summary>
public sealed class StampedeEvent : RunEvent
{
    private Enemy[] _members;
    private int _spawned;
    private int _kills;
    private int _required;
    private string _objectiveTemplate;
    private Vector2 _lastKillPosition;

    protected override void OnStart()
    {
        Vector2 player = Context.Player.GlobalPosition;
        float startDistance = Data.Number("start_distance", 620f);
        float bandWidth = Data.Number("band_width", 280f);
        // La bande passe à portée du joueur sans le viser exactement : il choisit de l'intercepter.
        // Plusieurs directions sont essayées pour partir d'un sol praticable (pas d'un lac).
        Vector2 direction = Vector2.Right;
        Vector2 center = player;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            direction = Vector2.FromAngle(Context.Rng.RandfRange(0f, Mathf.Tau));
            center = player - direction * startDistance + direction.Orthogonal() * Context.Rng.RandfRange(-90f, 90f);
            if (Context.Spawner.IsSpawnablePosition(center))
                break;
        }
        Vector2 across = direction.Orthogonal();

        int count = Mathf.Min((int)Data.Number("count_max", 34f),
            (int)(Data.Number("count_base", 14f) + Data.Number("count_per_minute", 1.5f) * Context.ElapsedMinutes));
        string enemyId = Context.Spawner.PickLocalEnemyId(center, Data.List("preferred_enemies"));
        float speedMult = Data.Number("travel_speed_mult", 1.7f);
        float xpMult = Data.Number("xp_mult", 1.5f);
        float travelDuration = Data.DurationSec + 6f;

        _members = new Enemy[count];
        for (int i = 0; i < count; i++)
        {
            Vector2 position = center;
            bool found = false;
            for (int attempt = 0; attempt < 4 && !found; attempt++)
            {
                position = center + across * Context.Rng.RandfRange(-bandWidth / 2f, bandWidth / 2f)
                    - direction * Context.Rng.RandfRange(0f, 220f);
                found = Context.Spawner.IsSpawnablePosition(position);
            }
            if (!found)
                continue;
            Enemy enemy = Context.Spawner.SpawnEventEnemy(enemyId, position);
            if (enemy == null)
                continue;
            enemy.Modifiers.EventToken = Token;
            enemy.Modifiers.IsEventBound = true;
            enemy.MultiplyXpReward(xpMult);
            enemy.StartTravel(direction, speedMult, travelDuration);
            _members[_spawned++] = enemy;
        }

        _required = Mathf.Max(1, Mathf.CeilToInt(_spawned * Data.Number("success_ratio", 0.6f)));
        _objectiveTemplate = Tr(Data.ObjectiveKey);
        UpdateObjective();
        HasTarget = true;
        Target = center;
        if (_spawned == 0)
            Fail("");
    }

    protected override void OnTick(float delta)
    {
        // La cible suit le premier membre encore debout : la flèche mène au cœur de la harde.
        for (int i = 0; i < _spawned; i++)
        {
            Enemy member = _members[i];
            if (member != null && member.IsActive && !member.IsDying && member.Modifiers.EventToken == Token)
            {
                Target = member.GlobalPosition;
                return;
            }
        }
    }

    public override void OnEnemyKilled(Vector2 position)
    {
        _kills++;
        _lastKillPosition = position;
        UpdateObjective();
        if (_kills >= _spawned)
            Complete();
    }

    protected override void OnTimeout() => Complete();

    private void Complete()
    {
        if (_kills < _required)
        {
            Fail(Tr("EVENT_FAIL_ESCAPED"));
            return;
        }
        int essence = (int)Data.Number("reward_essence", 4f);
        Context.GrantEssence(essence);
        Context.SpawnChest(Data.Text("reward_chest", "chest_common"), _lastKillPosition);
        Succeed(RewardLine(essence, true, false, false));
    }

    private void UpdateObjective()
    {
        Objective = string.Format(_objectiveTemplate, _kills, _required);
        Progress = Mathf.Clamp((float)_kills / _required, 0f, 1f);
    }

    public override void Cleanup()
    {
        // Les survivants redeviennent des créatures ordinaires, sans lien avec l'événement terminé.
        for (int i = 0; i < _spawned; i++)
        {
            Enemy member = _members[i];
            if (member != null && member.IsActive && member.Modifiers.EventToken == Token)
            {
                member.Modifiers.EventToken = 0;
                member.Modifiers.IsEventBound = false;
            }
        }
        _members = null;
    }
}
