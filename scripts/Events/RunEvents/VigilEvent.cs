using Godot;

namespace Vestiges.Events.RunEvents;

/// <summary>
/// Veille : un cercle de mémoire se forme près du joueur. Y rester remplit la veille (elle se vide
/// lentement dehors) pendant que les créatures convergent. Récompense : soins, coffre, Essence.
/// Courte défense de position, jamais une base : le cercle disparaît dès la veille tenue.
/// </summary>
public sealed class VigilEvent : RunEvent
{
    private static readonly Color VigilColor = new(0.83f, 0.66f, 0.26f);

    private Vector2 _center;
    private float _radius;
    private float _held;
    private float _holdTarget;
    private float _pressureTimer;
    private RunEventMarker _marker;

    protected override void OnStart()
    {
        _center = Context.PickPointAhead(Data.Number("spawn_distance_min", 220f), Data.Number("spawn_distance_max", 320f));
        _radius = Data.Number("radius", 96f);
        _holdTarget = Data.Number("hold_sec", 14f);
        _pressureTimer = Data.Number("pressure_interval_sec", 2.2f);

        _marker = new RunEventMarker();
        Context.WorldRoot.AddChild(_marker);
        _marker.Setup(RunEventMarker.MarkerStyle.Zone, _center, _radius, VigilColor);

        Objective = Tr(Data.ObjectiveKey);
        HasTarget = true;
        Target = _center;
    }

    protected override void OnTick(float delta)
    {
        // Même aplatissement 2:1 que l'anneau dessiné au sol.
        Vector2 offset = Context.Player.GlobalPosition - _center;
        bool inside = offset.X * offset.X + offset.Y * offset.Y * 4f <= _radius * _radius;
        _held = inside ? _held + delta : Mathf.Max(0f, _held - Data.Number("drain_per_sec", 0.35f) * delta);
        Progress = _held / _holdTarget;
        _marker.SetProgress(Progress);
        _marker.SetActive(inside);

        _pressureTimer -= delta;
        if (_pressureTimer <= 0f)
        {
            _pressureTimer = Data.Number("pressure_interval_sec", 2.2f);
            SpawnPressure();
        }

        if (_held >= _holdTarget)
            Complete();
    }

    /// <summary>Les créatures viennent de l'extérieur du cercle, hors du cadre, vers la veille.</summary>
    private void SpawnPressure()
    {
        int count = (int)Data.Number("pressure_count", 2f);
        float distance = Context.Spawner.ViewHalfExtents.Length() * 0.85f;
        for (int i = 0; i < count; i++)
        {
            Vector2 position = Context.PointAround(_center, distance, distance + 80f);
            if (Context.Spawner.IsSpawnablePosition(position))
                Context.Spawner.SpawnEventEnemy(Context.Spawner.PickLocalEnemyId(position), position);
        }
    }

    private void Complete()
    {
        Context.HealRatio(Data.Number("reward_heal_ratio", 0.3f));
        Context.SpawnChest(Data.Text("reward_chest", "chest_common"), _center);
        int essence = (int)Data.Number("reward_essence", 4f);
        Context.GrantEssence(essence);
        Infrastructure.AudioManager.Play("sfx_souvenir_trouve", 0f, -2f);
        Succeed(RewardLine(essence, true, true, false));
    }

    protected override void OnTimeout() => Fail(Tr("EVENT_FAIL_VIGIL"));

    public override void Cleanup()
    {
        _marker?.QueueFree();
        _marker = null;
    }
}
