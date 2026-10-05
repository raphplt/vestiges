using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>
/// Flaques de feu laissées par la Lanterne Mémorielle (effet spécial `ground_fire`) : dégâts toutes les demi-secondes
/// dans le rayon, mesuré au sol comme la zone tramée orange qui les montre. Des données tenues par CombatPools,
/// sans nœud par flaque (audit de performances §11).
/// </summary>
public sealed class GroundFire
{
    private const float TickSeconds = 0.5f;

    private struct Flame
    {
        public Vector2 Position;
        public float Damage;
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

    public static void Spawn(Vector2 position, float damage, float duration, float radius, AttackContext source = default)
    {
        CombatPools.Instance?.AddGroundFire(position, damage, duration, radius, source);
    }

    public void Add(Vector2 position, float damage, float duration, float radius, AttackContext source = default)
    {
        _flames.Add(new Flame
        {
            Source = source,
            Position = position,
            Damage = damage,
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
                enemy.TakeDamage(flame.Damage, source: flame.Source);
        }
    }
}
