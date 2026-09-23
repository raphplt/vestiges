using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>Traînées recyclées et témoin de recharge ; aucune création de node à l'activation.</summary>
public partial class MobilityFeedback : Node2D
{
    private const int TrailCount = 4;
    private const float TrailLifetime = 0.12f;
    private const float TrailInterval = 0.04f;
    private static readonly Color ReadyColor = new(0.55f, 0.88f, 0.9f, 0.9f);
    private readonly Sprite2D[] _trails = new Sprite2D[TrailCount];
    private readonly float[] _remaining = new float[TrailCount];
    private readonly Vector2[] _positions = new Vector2[TrailCount];
    private Texture2D[] _frames;
    private float _trailTimer;
    private float _charge = 1f;
    private bool _wasDashing;
    private bool _dashVisible;
    private int _nextTrail;

    public override void _Ready()
    {
        // Même couche que le sol, derrière le joueur dans l'ordre de dessin de ses enfants.
        ShowBehindParent = true;
        _frames = VfxFactory.GetDashTrailTextures();
        for (int i = 0; i < TrailCount; i++)
        {
            _trails[i] = new Sprite2D
            {
                Texture = _frames[0],
                TextureFilter = TextureFilterEnum.Nearest,
                Visible = false,
                ShowBehindParent = true
            };
            AddChild(_trails[i]);
        }
    }

    public void UpdateFeedback(float delta, PlayerMobility mobility, Vector2 position, bool moved)
    {
        _charge = 1f - mobility.CooldownRemaining / mobility.Config.CooldownSeconds;
        _dashVisible = mobility.IsDashStep && moved;
        if (mobility.StartedThisStep)
        {
            Infrastructure.AudioManager.Play(mobility.Config.StartAudio, 0.05f, -3f);
            _trailTimer = 0f;
        }
        else if (_wasDashing && !mobility.IsDashing)
            Infrastructure.AudioManager.Play(mobility.Config.EndAudio, 0.05f, -7f);
        _wasDashing = mobility.IsDashing;

        for (int i = 0; i < TrailCount; i++)
        {
            _remaining[i] = Mathf.Max(0f, _remaining[i] - delta);
            _trails[i].Visible = _remaining[i] > 0f && VfxFactory.CurrentParticleLevel != ParticleLevel.Off;
            if (_trails[i].Visible)
            {
                _trails[i].GlobalPosition = _positions[i];
                float progress = 1f - _remaining[i] / TrailLifetime;
                _trails[i].Texture = _frames[Mathf.Min(2, (int)(progress * 3f))];
                _trails[i].Modulate = new Color(1f, 1f, 1f, 1f - progress);
            }
        }

        _trailTimer -= delta;
        if (_dashVisible && _trailTimer <= 0f && VfxFactory.CurrentParticleLevel != ParticleLevel.Off)
        {
            Sprite2D trail = _trails[_nextTrail];
            trail.GlobalPosition = position;
            _positions[_nextTrail] = position;
            trail.Texture = _frames[0];
            trail.Modulate = Colors.White;
            trail.Visible = true;
            _remaining[_nextTrail] = TrailLifetime;
            _nextTrail = (_nextTrail + 1) % TrailCount;
            _trailTimer = TrailInterval * (VfxFactory.CurrentParticleLevel == ParticleLevel.Reduced ? 2f : 1f);
        }
        QueueRedraw();
    }

    public void Suspend()
    {
        _wasDashing = false;
        _dashVisible = false;
        for (int i = 0; i < TrailCount; i++)
        {
            _remaining[i] = 0f;
            if (_trails[i] != null)
                _trails[i].Visible = false;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(-12f, 16f, 24f, 3f), new Color(0.04f, 0.06f, 0.09f, 0.8f));
        DrawRect(new Rect2(-12f, 16f, 24f * Mathf.Clamp(_charge, 0f, 1f), 3f), ReadyColor);
        if (_dashVisible)
            DrawArc(Vector2.Zero, 16f, 0f, Mathf.Tau, 16, ReadyColor, 1f);
    }
}
