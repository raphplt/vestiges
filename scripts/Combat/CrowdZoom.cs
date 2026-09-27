using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>
/// Léger recul de caméra quand l'écran se remplit (plan 02 J5) : au-delà d'un seuil de créatures visibles, le zoom
/// recule doucement jusqu'à 8 %, puis revient quand la vague se vide. Comptage quatre fois par seconde via GroupCache,
/// zoom lissé à chaque frame. Le zoom de base est celui de la caméra au démarrage.
/// </summary>
public partial class CrowdZoom : Node
{
    private const float CountInterval = 0.25f;
    private const int CrowdStart = 25;
    private const int CrowdFull = 70;
    private const float MaxPullBack = 0.08f;
    private const float Smoothing = 1.5f;

    private Camera2D _camera;
    private GroupCache _groups;
    private Vector2 _baseZoom;
    private float _timer;
    private float _target;
    private float _current;

    public void SetCamera(Camera2D camera)
    {
        _camera = camera;
        _baseZoom = camera.Zoom;
    }

    public override void _Ready()
    {
        _groups = GetNode<GroupCache>("/root/GroupCache");
    }

    public override void _Process(double delta)
    {
        if (_camera == null)
            return;
        float dt = (float)delta;
        _timer -= dt;
        if (_timer <= 0f)
        {
            _timer = CountInterval;
            int visible = CountVisible();
            _target = MaxPullBack * Mathf.Clamp((visible - CrowdStart) / (float)(CrowdFull - CrowdStart), 0f, 1f);
        }
        if (Mathf.IsEqualApprox(_current, _target))
            return;
        _current = Mathf.MoveToward(_current, _target, Smoothing * MaxPullBack * dt);
        _camera.Zoom = _baseZoom * (1f - _current);
    }

    private int CountVisible()
    {
        Vector2 center = _camera.GetScreenCenterPosition();
        Vector2 half = GetViewport().GetVisibleRect().Size / (2f * _camera.Zoom);
        int count = 0;
        foreach (Node node in _groups.GetEnemies())
        {
            if (node is not Enemy enemy || !enemy.IsActive || enemy.IsDying)
                continue;
            Vector2 offset = enemy.GlobalPosition - center;
            if (Mathf.Abs(offset.X) <= half.X && Mathf.Abs(offset.Y) <= half.Y)
                count++;
        }
        return count;
    }
}
