using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Propagation (plan 05 §3.3) : à une élimination attribuée, le ralentissement et la désorientation natifs encore
/// actifs sur la victime passent à son plus proche voisin vivant dans le rayon, avec leur intensité et leur durée
/// restante. Un contrôle reçu ainsi ne se transmet plus ; les règles de fusion de l'ennemi n'affaiblissent rien.
/// </summary>
public sealed class ControlPropagation
{
    private const float LinkHeight = 11f;
    private const float LinkSeconds = 0.3f;

    private readonly ulong _ownerId;
    private readonly float _radiusSq;
    private readonly GroupCache _groupCache;

    public ControlPropagation(ulong ownerId, PerkSpecializationData perk, GroupCache groupCache)
    {
        _ownerId = ownerId;
        float radius = SpecializationRuntime.Parameter(perk, "radius_pixels");
        _radiusSq = radius * radius;
        _groupCache = groupCache;
    }

    /// <summary>Receveur des contrôles, ou null si la victime n'en portait aucun de transmissible ou n'a pas de voisin.</summary>
    public Enemy Resolve(in EnemyKillResult kill)
    {
        bool slow = kill.Slow.CanPropagate(_ownerId);
        bool disorientation = kill.Disorientation.CanPropagate(_ownerId);
        if (!slow && !disorientation)
            return null;
        Enemy receiver = Nearest(kill.Position, kill.Target);
        if (receiver == null)
            return null;
        if (slow)
            receiver.ApplySlow(kill.Slow.Strength, kill.Slow.Remaining, kill.Slow.Source, ControlOrigin.Propagated);
        if (disorientation)
            receiver.ApplyDisorient(kill.Disorientation.Remaining, kill.Disorientation.Source, ControlOrigin.Propagated);
        PlayLink(kill.Position, receiver.GlobalPosition);
        return receiver;
    }

    private Enemy Nearest(Vector2 position, EnemyLife victim)
    {
        Enemy nearest = null;
        float best = _radiusSq;
        foreach (Node node in _groupCache.GetEnemies())
        {
            if (node is not Enemy { IsActive: true, IsDying: false } enemy || enemy.Life == victim)
                continue;
            float distanceSq = enemy.GlobalPosition.DistanceSquaredTo(position);
            if (distanceSq > best)
                continue;
            best = distanceSq;
            nearest = enemy;
        }
        return nearest;
    }

    /// <summary>Bref trait pâle de la victime au receveur, par le pool d'effets.</summary>
    private static void PlayLink(Vector2 from, Vector2 to)
    {
        if (CombatPools.Instance == null)
            return;
        Vector2 delta = to - from;
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Beam, FxFamily.Pale, delta.Length() * 0.5f, 3f, LinkSeconds);
        spec.Angle = delta.Angle();
        spec.Steps = 4;
        spec.ZIndex = 2;
        CombatPools.Instance.PlayFx((from + to) * 0.5f + new Vector2(0f, -LinkHeight), spec, FxOwner.Player);
    }
}
