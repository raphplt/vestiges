using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>
/// Flaque de feu laissée par la Lanterne Mémorielle (effet spécial `ground_fire`) :
/// dégâts toutes les demi-secondes dans le rayon, zone tramée orange au sol.
/// </summary>
public partial class GroundFire : Node2D
{
    private const float TickSeconds = 0.5f;

    private float _damage;
    private float _radiusSq;
    private float _remaining;
    private float _tick;
    private GroupCache _groupCache;

    public static void Spawn(Node context, Vector2 position, float damage, float duration, float radius, GroupCache groupCache)
    {
        GroundFire fire = new()
        {
            Name = "GroundFire",
            GlobalPosition = position,
            _damage = damage,
            _radiusSq = radius * radius,
            _remaining = duration,
            _tick = TickSeconds,
            _groupCache = groupCache,
        };
        context.GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, fire);

        if (CombatPools.Instance == null)
            return;
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Zone, FxFamily.Fire, radius, 1f, duration);
        spec.Squash = 2f;
        spec.FillDensity = 0.3f;
        spec.ProgressFill = false;
        spec.Steps = 8;
        spec.FadeTail = 0.4f;
        spec.ZIndex = -1;
        CombatPools.Instance.PlayFx(position, spec, FxOwner.Player);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _remaining -= dt;
        _tick -= dt;
        if (_tick <= 0f)
        {
            _tick += TickSeconds;
            foreach (Node node in _groupCache.GetEnemies())
            {
                if (node is Enemy enemy && IsInstanceValid(enemy) && !enemy.IsDying
                    && enemy.GlobalPosition.DistanceSquaredTo(GlobalPosition) < _radiusSq)
                    enemy.TakeDamage(_damage);
            }
        }
        if (_remaining <= 0f)
            QueueFree();
    }
}
