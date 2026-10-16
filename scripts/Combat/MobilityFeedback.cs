using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>Traînées recyclées et témoin de recharge ; aucune création de node à l'activation.</summary>
public partial class MobilityFeedback : Node2D
{
    private const int TrailCount = 4;
    private const float TrailLifetime = 0.12f;
    private const float TrailInterval = 0.04f;
    private static readonly Color ReadyColor = new(0.55f, 0.88f, 0.9f, 0.9f);
    private static readonly Color BarBackColor = new(0.04f, 0.06f, 0.09f, 0.8f);
    private static readonly Color TierMarkColor = new(0.9f, 0.86f, 0.78f, 0.45f);
    // Un palier de plus, une couleur plus chaude : du sable à l'ambre puis au blanc chaud.
    private static readonly Color[] ElanColors =
    {
        new(0.62f, 0.56f, 0.46f, 0.85f), new(0.86f, 0.62f, 0.3f, 0.9f), new(0.98f, 0.78f, 0.38f, 0.95f), new(1f, 0.94f, 0.8f, 1f),
    };
    private readonly Sprite2D[] _trails = new Sprite2D[TrailCount];
    private readonly float[] _remaining = new float[TrailCount];
    private readonly Vector2[] _positions = new Vector2[TrailCount];
    private Texture2D[] _frames;
    private float _trailTimer;
    private float _charge = 1f;
    private bool _wasDashing;
    private bool _dashVisible;
    private int _nextTrail;
    private float _elan;
    private int _elanTier;
    private IReadOnlyList<ElanTier> _elanTiers;

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

    /// <param name="from">Position avant le pas : un saut instantané (Plan de métro) laisse ses traînées sur le trajet.</param>
    public void UpdateFeedback(float delta, PlayerMobility mobility, Vector2 from, Vector2 position, bool moved)
    {
        _charge = 1f - mobility.CooldownRemaining / mobility.CooldownSeconds;
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

        if (mobility.BlinkedThisStep && moved && VfxFactory.CurrentParticleLevel != ParticleLevel.Off)
        {
            // Le saut se lit à ses silhouettes laissées en ligne, du départ vers l'arrivée.
            for (int i = 0; i < TrailCount; i++)
                PlaceTrail(from.Lerp(position, (float)i / TrailCount));
            _trailTimer = float.MaxValue;
        }

        _trailTimer -= delta;
        if (_dashVisible && _trailTimer <= 0f && VfxFactory.CurrentParticleLevel != ParticleLevel.Off)
        {
            PlaceTrail(position);
            _trailTimer = TrailInterval * (VfxFactory.CurrentParticleLevel == ParticleLevel.Reduced ? 2f : 1f);
        }
        QueueRedraw();
    }

    /// <summary>Jauge d'Élan sous la recharge du dash, avec les seuils de palier ; masquée tant que l'Élan est vide.</summary>
    public void SetElan(float value, int tier, IReadOnlyList<ElanTier> tiers)
    {
        _elan = value;
        _elanTier = tier;
        _elanTiers = tiers;
    }

    private void PlaceTrail(Vector2 position)
    {
        Sprite2D trail = _trails[_nextTrail];
        trail.GlobalPosition = position;
        _positions[_nextTrail] = position;
        trail.Texture = _frames[0];
        trail.Modulate = Colors.White;
        trail.Visible = true;
        _remaining[_nextTrail] = TrailLifetime;
        _nextTrail = (_nextTrail + 1) % TrailCount;
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

    // Anneau « dash prêt » posé au sol autour des pieds : ellipse 2:1 au pixel du monde (plan 37 M1).
    private const int ReadyRingRadius = 16;

    public override void _Draw()
    {
        DrawRect(new Rect2(-12f, 20f, 24f, 2f), BarBackColor);
        DrawRect(new Rect2(-12f, 20f, 24f * Mathf.Clamp(_charge, 0f, 1f), 2f), ReadyColor);
        if (_elan > 0f && _elanTiers != null)
        {
            DrawRect(new Rect2(-12f, 23f, 24f, 2f), BarBackColor);
            DrawRect(new Rect2(-12f, 23f, Mathf.Round(24f * _elan), 2f), ElanColors[Mathf.Min(_elanTier, ElanColors.Length - 1)]);
            for (int i = 0; i < _elanTiers.Count - 1; i++)
                DrawRect(new Rect2(-12f + Mathf.Round(24f * _elanTiers[i].From), 23f, 1f, 2f), TierMarkColor);
        }
        if (_dashVisible)
            DrawTexture(PixelEllipseArt.Get(ReadyRingRadius, false), PixelEllipseArt.Offset(ReadyRingRadius), ReadyColor);
    }
}
