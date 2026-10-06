using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>
/// Flaques de feu laissées par la Lanterne Mémorielle (effet spécial `ground_fire`) : toutes les demi-secondes, une
/// créature dans le rayon, mesuré au sol comme la zone tramée orange qui les montre, **brûle** (planche 05, R1) : le
/// statut ordinaire, aux mêmes dégâts par seconde, qui dure encore <c>burn_seconds</c> après le dernier passage. Des données tenues par CombatPools,
/// sans nœud par flaque (audit de performances §11).
/// </summary>
public sealed class GroundFire
{
    private const float TickSeconds = 0.5f;

    private struct Flame
    {
        public Vector2 Position;
        public float Dps;
        public float BurnSeconds;
        public float RadiusSq;
        public float Remaining;
        public float Tick;
        public AttackContext Source;
    }

    private readonly List<Flame> _flames = new();
    private readonly GroupCache _groupCache;

    public GroundFire(GroupCache groupCache)
    {
        _groupCache = groupCache;
    }

    public static void Spawn(Vector2 position, float damage, float duration, float radius, float burnSeconds, AttackContext source = default)
    {
        CombatPools.Instance?.AddGroundFire(position, damage, duration, radius, burnSeconds, source);
    }

    /// <summary><paramref name="damage"/> est ce qu'une demi-seconde de feu inflige.</summary>
    public void Add(Vector2 position, float damage, float duration, float radius, float burnSeconds, AttackContext source = default)
    {
        _flames.Add(new Flame
        {
            Source = source,
            Position = position,
            Dps = damage / TickSeconds,
            BurnSeconds = burnSeconds,
            RadiusSq = radius * radius,
            Remaining = duration,
            Tick = TickSeconds,
        });
    }

    public void Process(float delta)
    {
        for (int i = _flames.Count - 1; i >= 0; i--)
        {
            Flame flame = _flames[i];
            flame.Remaining -= delta;
            flame.Tick -= delta;
            if (flame.Tick <= 0f)
            {
                flame.Tick += TickSeconds;
                Burn(flame);
            }
            if (flame.Remaining <= 0f)
            {
                _flames[i] = _flames[^1];
                _flames.RemoveAt(_flames.Count - 1);
                continue;
            }
            _flames[i] = flame;
        }
    }

    private void Burn(in Flame flame)
    {
        foreach (Node node in _groupCache.GetEnemies())
        {
            if (node is Enemy { IsActive: true, IsDying: false } enemy && GodotObject.IsInstanceValid(enemy)
                && Iso.GroundDistanceSquared(enemy.GlobalPosition, flame.Position) < flame.RadiusSq)
                enemy.ApplyIgnite(flame.Dps, flame.BurnSeconds, flame.Source);
        }
    }
}
