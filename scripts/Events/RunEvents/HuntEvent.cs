using Godot;
using Vestiges.Combat;

namespace Vestiges.Events.RunEvents;

/// <summary>
/// Souverain : version « boss » d'une créature locale, escortée de sa meute.
/// À abattre avant son effacement ; le coffre et l'Essence de la variante s'y ajoutent.
/// Teste la puissance du build sur une cible unique et résistante.
/// </summary>
public sealed class HuntEvent : RunEvent
{
    private Enemy _champion;
    private string _objectiveTemplate;

    protected override void OnStart()
    {
        Vector2 origin = Context.PickPointAhead(Data.Number("spawn_distance_min", 380f), Data.Number("spawn_distance_max", 520f));
        string enemyId = Context.Spawner.PickLocalEnemyId(origin);
        _champion = Context.Spawner.SpawnEventEnemy(enemyId, origin, "champion");
        if (_champion == null)
        {
            Fail("");
            return;
        }
        _champion.Modifiers.EventToken = Token;
        _champion.Modifiers.IsEventBound = true;

        int escorts = Mathf.Min((int)Data.Number("escort_max", 8f),
            (int)(Data.Number("escort_base", 3f) + Data.Number("escort_per_minute", 0.4f) * Context.ElapsedMinutes));
        for (int i = 0; i < escorts; i++)
        {
            Vector2 position = Context.PointAround(origin, 30f, 70f);
            if (Context.Spawner.IsSpawnablePosition(position))
                Context.Spawner.SpawnEventEnemy(enemyId, position);
        }

        _objectiveTemplate = Tr(_champion.IsFeminine ? Data.ObjectiveKey + "_F" : Data.ObjectiveKey);
        Objective = string.Format(_objectiveTemplate, _champion.DisplayName);
        HasTarget = true;
        Target = origin;
    }

    protected override void OnTick(float delta)
    {
        if (_champion == null)
            return;
        Target = _champion.GlobalPosition;
        Progress = 1f - _champion.HpRatio;
    }

    public override void OnEnemyKilled(Vector2 position)
    {
        int essence = (int)Data.Number("reward_essence", 4f);
        Context.GrantEssence(essence);
        bool lateChest = Context.ElapsedSeconds >= Data.Number("late_reward_after_sec", 720f);
        if (lateChest)
            Context.SpawnChest(Data.Text("late_reward_chest", "chest_epic"), position + new Vector2(24f, 0f));
        Progress = 1f;
        _champion = null;
        // Le coffre rare et l'Essence propres au Souverain tombent à sa mort (variante).
        int variantEssence = Infrastructure.EnemyVariantDataLoader.GetVariant("champion")?.BonusEssence ?? 0;
        Succeed(RewardLine(essence + variantEssence, true, false, true));
    }

    protected override void OnTimeout()
    {
        _champion?.Vanish();
        _champion = null;
        Fail(Tr("EVENT_FAIL_FADED"));
    }

    public override void Cleanup()
    {
        if (_champion != null && _champion.IsActive && !_champion.IsDying)
            _champion.Modifiers.IsEventBound = false;
        _champion = null;
    }
}
