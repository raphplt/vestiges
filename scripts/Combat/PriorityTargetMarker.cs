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
        GlobalPosition = _target.GlobalPosition.Round();
        _pulse += (float)delta * 4f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        // Coins d'un pixel de large, qui s'écartent par paliers d'un pixel (plan 37 M1) au lieu de glisser.
        int spread = Mathf.RoundToInt(1f + Mathf.Sin(_pulse));
        int rx = Mathf.RoundToInt(RadiusX) + spread;
        int ry = Mathf.RoundToInt(RadiusY) + spread;
        int arm = Mathf.RoundToInt(Corner);
        int armY = Mathf.Max(2, Mathf.RoundToInt(Corner * 0.5f));
        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sy = -1; sy <= 1; sy += 2)
            {
                // Coin au pixel : le trait horizontal part du coin vers l'intérieur, le vertical aussi.
                int x = sx < 0 ? -rx : rx - 1;
                int y = sy < 0 ? -ry : ry - 1;
                DrawRect(new Rect2(sx < 0 ? x : x - arm + 1, y, arm, 1), MarkColor);
                DrawRect(new Rect2(x, sy < 0 ? y : y - armY + 1, 1, armY), MarkColor);
            }
        }
    }
}
