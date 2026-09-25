using Godot;
using Vestiges.Combat;
using Vestiges.Combat.Abilities;

namespace Vestiges.Events.RunEvents;

/// <summary>
/// Vestige tombé : un fragment de mémoire chute à un endroit annoncé. L'impact écrase les créatures
/// (et le joueur resté dessous), puis des gardiens surgissent autour du vestige à récupérer.
/// Récompense : XP proportionnelle au niveau, Essence et soins. Oppose avidité et prudence.
/// </summary>
public sealed class FallenRelicEvent : RunEvent
{
    private static readonly Color RelicColor = new(0.37f, 0.77f, 0.77f);

    private Vector2 _position;
    private float _fallDelay;
    private float _fallElapsed;
    private bool _landed;
    private RunEventMarker _marker;
    private GroundTelegraph _telegraph;

    protected override void OnStart()
    {
        _position = Context.PickPointAhead(Data.Number("spawn_distance_min", 360f), Data.Number("spawn_distance_max", 500f));
        _fallDelay = Data.Number("fall_delay_sec", 5f);

        _telegraph = new GroundTelegraph();
        Context.WorldRoot.AddChild(_telegraph);
        _telegraph.ShowCircle(_position, Data.Number("impact_radius", 72f), FxFamily.Essence);

        _marker = new RunEventMarker();
        Context.WorldRoot.AddChild(_marker);
        _marker.Setup(RunEventMarker.MarkerStyle.Relic, _position, 16f, RelicColor);

        Objective = Tr("EVENT_RELIC_OBJECTIVE_FALL");
        HasTarget = true;
        Target = _position;
    }

    protected override void OnTick(float delta)
    {
        if (!_landed)
        {
            _fallElapsed += delta;
            float fall = _fallElapsed / _fallDelay;
            _telegraph.SetProgress(fall);
            _marker.SetFall(fall);
            Progress = fall;
            if (_fallElapsed >= _fallDelay)
                Land();
            return;
        }

        Progress = 1f - TimeRemaining / (Data.DurationSec - _fallDelay);
        // Le vestige refroidit un instant : les gardiens ont le temps d'entrer en jeu.
        _fallElapsed += delta;
        if (_fallElapsed < _fallDelay + Data.Number("pickup_delay_sec", 1.5f))
            return;
        float pickup = Data.Number("pickup_radius", 26f);
        if (Context.Player.GlobalPosition.DistanceSquaredTo(_position) <= pickup * pickup)
            Collect();
    }

    private void Land()
    {
        _landed = true;
        float radius = Data.Number("impact_radius", 72f);
        _telegraph.SetFlash(1f);
        Tween fade = _telegraph.CreateTween();
        fade.TweenMethod(Callable.From<float>(_telegraph.SetFlash), 1f, 0f, 0.35f);
        fade.TweenCallback(Callable.From(_telegraph.HideMarker));
        _marker.SetFall(1f);
        ScreenShake.Instance?.ShakeMedium();
        Infrastructure.AudioManager.Play("sfx_monde_dissolution", 0.05f, -2f);

        Context.DamageEnemiesInRadius(_position, radius, Data.Number("impact_enemy_damage", 60f));
        Context.DamagePlayerIfInside(_position, radius, Data.Number("impact_player_damage_ratio", 0.2f));

        int guardians = Mathf.Min((int)Data.Number("guardians_max", 12f),
            (int)(Data.Number("guardians_base", 5f) + Data.Number("guardians_per_minute", 0.5f) * Context.ElapsedMinutes));
        string enemyId = Context.Spawner.PickLocalEnemyId(_position);
        bool elite = Context.ElapsedSeconds >= Data.Number("elite_guardian_after_sec", 300f);
        for (int i = 0; i < guardians; i++)
        {
            Vector2 position = Context.PointAround(_position, radius + 10f, radius + 60f);
            if (Context.Spawner.IsSpawnablePosition(position))
                Context.Spawner.SpawnEventEnemy(enemyId, position, elite && i == 0 ? "elite" : null);
        }
        Objective = Tr(Data.ObjectiveKey);
    }

    private void Collect()
    {
        Context.SpawnXpBurst(Context.XpForLevelRatio(Data.Number("reward_level_ratio", 0.7f)), _position, 8);
        int essence = (int)Data.Number("reward_essence", 5f);
        Context.GrantEssence(essence);
        Context.HealRatio(Data.Number("reward_heal_ratio", 0.15f));
        Infrastructure.AudioManager.Play("sfx_artefact_trouve", 0f, -2f);
        Succeed(RewardLine(essence, false, true, true));
    }

    protected override void OnTimeout()
    {
        if (!_landed)
            Land();
        Fail(Tr("EVENT_FAIL_RELIC"));
    }

    public override void Cleanup()
    {
        _marker?.QueueFree();
        _marker = null;
        if (_telegraph != null && GodotObject.IsInstanceValid(_telegraph))
        {
            // L'éclat d'impact finit son fondu avant de libérer l'annonce ; à l'arrêt de la run, libération directe.
            if (_telegraph.IsInsideTree())
                _telegraph.GetTree().CreateTimer(0.5f).Timeout += _telegraph.QueueFree;
            else
                _telegraph.QueueFree();
        }
        _telegraph = null;
    }
}
