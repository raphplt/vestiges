using Godot;
using Vestiges.Combat;

namespace Vestiges.Events.RunEvents;

/// <summary>
/// Souverain : version « boss » d'une créature locale, escortée de sa meute.
/// Il ne disparaît jamais de lui-même : il reste jusqu'à sa mort (DECISIONS §157). Mais la chasse ne bloque pas la
/// suite (§165) : à son terme, l'événement se clôt et le Souverain reste sur la carte, toujours à ta poursuite, avec le
/// coffre et l'Essence de sa variante ; seule la récompense de la chasse est perdue. Teste la puissance du build sur une
/// cible unique et résistante.
/// </summary>
public sealed class HuntEvent : RunEvent
{
    private Enemy _champion;
    private BossBarFeed _bar;
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
        _bar = new BossBarFeed(Context.EventBus, _champion.DisplayName, _champion.MaxHp);
    }

    protected override void OnTick(float delta)
    {
        if (_champion == null)
            return;
        // Garde-fou : un Souverain retiré sans mort (rendu au pool par un autre système) ne doit pas laisser une chasse
        // sans fin, qui bloquerait tous les événements suivants ; le pool a pu déjà le rendre à une autre créature.
        if (!_champion.IsActive || _champion.Modifiers.EventToken != Token)
        {
            _champion = null;
            _bar?.End(false);
            Fail(Tr("EVENT_FAIL_FADED"));
            return;
        }
        Target = _champion.GlobalPosition;
        Progress = 1f - _champion.HpRatio;
        _bar.Update(_champion.CurrentHp, _champion.MaxHp);
    }

    protected override void OnTimeout()
    {
        if (_champion == null)
            return;
        // Il reste lié à un événement (il ne perd jamais la trace du joueur), mais plus à celui-ci : la chasse laisse
        // place aux suivantes.
        string name = _champion.DisplayName;
        bool feminine = _champion.IsFeminine;
        _champion.Modifiers.EventToken = 0;
        _champion = null;
        _bar?.End(false);
        Fail(string.Format(Tr(feminine ? "EVENT_HUNT_LINGERS_F" : "EVENT_HUNT_LINGERS"), name));
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
        _bar?.End(true);
        // Le coffre rare et l'Essence propres au Souverain tombent à sa mort (variante).
        int variantEssence = Infrastructure.EnemyVariantDataLoader.GetVariant("champion")?.BonusEssence ?? 0;
        Succeed(RewardLine(essence + variantEssence, true, false, true));
    }

    public override void Cleanup()
    {
        if (_champion != null && _champion.IsActive && !_champion.IsDying)
            _champion.Modifiers.IsEventBound = false;
        _champion = null;
        _bar?.End(false);
    }
}
