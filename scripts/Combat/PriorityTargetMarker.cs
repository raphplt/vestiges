using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Repère de Convergence (plan 05 §3.1) : quatre coins au sol autour de la cible prioritaire effectivement suivie,
/// sans marque de dégâts. Suit sa cible tant que cette vie dure ; s'efface dès qu'elle meurt ou que le nœud est recyclé.
/// </summary>
public partial class PriorityTargetMarker : Node2D
{
    private const float RadiusX = 15f;
    private const float RadiusY = 7.5f;
    private const float Corner = 5f;
    private static readonly Color MarkColor = new(1f, 0.82f, 0.45f, 0.85f);

    private Enemy _target;
    private EnemyLife _life;
    private float _pulse;

    public Enemy Target => Visible ? _target : null;

    public override void _Ready()
    {
        // Décalque au sol : sous les entités, au-dessus du sol et des routes.
        ZIndex = -1;
        Visible = false;
        SetProcess(false);
    }

    public void Track(Enemy target)
    {
        _target = target;
        _life = target.Life;
        Scale = Vector2.One * Mathf.Max(1f, target.Scale.X);
        GlobalPosition = target.GlobalPosition;
        Visible = true;
        SetProcess(true);
    }

    public void Release()
    {
        _target = null;
        Visible = false;
        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(_target) || _target.IsDying || !_target.IsActive || _target.Life != _life)
        {
            Release();
            return;
        }
        GlobalPosition = _target.GlobalPosition;
        _pulse += (float)delta * 4f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float spread = 1f + 0.08f * Mathf.Sin(_pulse);
        float rx = RadiusX * spread;
        float ry = RadiusY * spread;
        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sy = -1; sy <= 1; sy += 2)
            {
                Vector2 corner = new(sx * rx, sy * ry);
                DrawLine(corner, corner - new Vector2(sx * Corner, 0f), MarkColor, 1.5f);
                DrawLine(corner, corner - new Vector2(0f, sy * Corner * 0.5f), MarkColor, 1.5f);
            }
        }
    }
}
