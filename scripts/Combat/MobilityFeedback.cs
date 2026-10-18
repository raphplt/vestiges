using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Traînées recyclées et témoin de recharge ; aucune création de node à l'activation. Avec un sprite, chaque traînée
/// reprend la frame exacte du personnage en silhouette à sa couleur d'accent (plan 54 G3).
/// </summary>
public partial class MobilityFeedback : Node2D
{
    private const int TrailCount = 4;
    private const float TrailLifetime = 0.12f;
    private const float TrailInterval = 0.04f;
    // La silhouette tient un peu plus longtemps : elle se trame en s'effaçant au lieu de passer par trois formes.
    private const float SilhouetteLifetime = 0.18f;
    private const float SilhouetteAlpha = 1f;
    private static ShaderMaterial _silhouetteMaterial;
    private static readonly Color ReadyColor = new(0.55f, 0.88f, 0.9f, 0.9f);
    private static readonly Color BarBackColor = new(0.04f, 0.06f, 0.09f, 0.8f);
    // Esquive parfaite (plan 52 E2) : une onde qui s'ouvre au sol en quatre rayons, cuits au chargement.
    private const float DodgeRingSeconds = 0.3f;
    private static readonly int[] DodgeRingRadii = { 16, 21, 26, 31 };
    private static readonly Color DodgeRingColor = new(1f, 0.93f, 0.72f, 1f);
    // Retombée d'un saut du vide (plan 52 E3) : la même onde, couleur de poussière, un peu plus lente.
    private const float LandingRingSeconds = 0.36f;
    private static readonly Color LandingRingColor = new(0.86f, 0.78f, 0.62f, 1f);
    private readonly Sprite2D[] _trails = new Sprite2D[TrailCount];
    private readonly float[] _remaining = new float[TrailCount];
    private readonly Vector2[] _positions = new Vector2[TrailCount];
    private Texture2D[] _frames;
    private AnimatedSprite2D _source;
    private Color _accent = Colors.White;
    private float _lifetime = TrailLifetime;
    private float _trailTimer;
    private float _charge = 1f;
    private bool _wasDashing;
    private bool _dashVisible;
    private int _nextTrail;
    private float _dodgeRemaining;
    private bool _afterimages;
    // Images rémanentes de la Série (MONSTRE) : une silhouette laissée à cet intervalle tant que le joueur bouge.
    private const float AfterimageInterval = 0.07f;
    private float _landingRemaining;

    public override void _Ready()
    {
        // Même couche que le sol, derrière le joueur dans l'ordre de dessin de ses enfants.
        ShowBehindParent = true;
        _frames = VfxFactory.GetDashTrailTextures();
        foreach (int radius in DodgeRingRadii)
            PixelEllipseArt.Get(radius, false);
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
        _dodgeRemaining = Mathf.Max(0f, _dodgeRemaining - delta);
        _landingRemaining = Mathf.Max(0f, _landingRemaining - delta);
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
                float progress = 1f - _remaining[i] / _lifetime;
                if (_source != null)
                    _trails[i].Modulate = _accent with { A = SilhouetteAlpha * (1f - progress) };
                else
                {
                    _trails[i].Texture = _frames[Mathf.Min(2, (int)(progress * 3f))];
                    _trails[i].Modulate = new Color(1f, 1f, 1f, 1f - progress);
                }
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
        bool trailing = _dashVisible || (_afterimages && moved);
        if (trailing && _trailTimer <= 0f && VfxFactory.CurrentParticleLevel != ParticleLevel.Off)
        {
            PlaceTrail(position);
            float interval = _dashVisible ? TrailInterval : AfterimageInterval;
            _trailTimer = interval * (VfxFactory.CurrentParticleLevel == ParticleLevel.Reduced ? 2f : 1f);
        }
        QueueRedraw();
    }

    /// <summary>Images rémanentes continues tant que le joueur bouge (Série au rang MONSTRE).</summary>
    public void SetAfterimages(bool enabled) => _afterimages = enabled;

    /// <summary>Un coup évité pendant le dash : son clair et onde au sol, lisibles au milieu de la foule.</summary>
    public void PerfectDodge(string audio)
    {
        Infrastructure.AudioManager.Play(audio, 0.04f, -2f, 1.35f);
        _dodgeRemaining = DodgeRingSeconds;
        QueueRedraw();
    }

    /// <summary>Retombée sur le sol après un saut du vide : un choc sourd et une onde de poussière.</summary>
    public void VoidLeapLanded(string audio)
    {
        Infrastructure.AudioManager.Play(audio, 0.05f, -4f, 0.8f);
        _landingRemaining = LandingRingSeconds;
        QueueRedraw();
    }

    /// <summary>
    /// Source des silhouettes : le sprite du personnage et sa couleur d'accent ; <paramref name="source"/> null
    /// garde les formes génériques (personnage sans sprite).
    /// </summary>
    public void SetSilhouette(AnimatedSprite2D source, Color accent)
    {
        _source = source;
        _accent = accent;
        _lifetime = source != null ? SilhouetteLifetime : TrailLifetime;
        if (source != null)
            _silhouetteMaterial ??= new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/dash_silhouette.gdshader") };
        foreach (Sprite2D trail in _trails)
        {
            trail.Material = source != null ? _silhouetteMaterial : null;
            trail.Offset = source?.Offset ?? Vector2.Zero;
            trail.Rotation = 0f;
            trail.Scale = Vector2.One;
        }
    }

    private void PlaceTrail(Vector2 position)
    {
        Sprite2D trail = _trails[_nextTrail];
        if (_source != null)
        {
            // La frame affichée à cet instant, à la hauteur du sprite (saut, élan d'attaque) et avec son inclinaison.
            position += _source.Position;
            trail.Texture = _source.SpriteFrames.GetFrameTexture(_source.Animation, _source.Frame);
            trail.FlipH = _source.FlipH;
            trail.Rotation = _source.Rotation;
            trail.Scale = _source.Scale;
            trail.Modulate = _accent with { A = SilhouetteAlpha };
        }
        else
        {
            trail.Texture = _frames[0];
            trail.Modulate = Colors.White;
        }
        trail.GlobalPosition = position;
        _positions[_nextTrail] = position;
        trail.Visible = true;
        _remaining[_nextTrail] = _lifetime;
        _nextTrail = (_nextTrail + 1) % TrailCount;
    }

    public void Suspend()
    {
        _wasDashing = false;
        _dashVisible = false;
        _dodgeRemaining = 0f;
        _landingRemaining = 0f;
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

    /// <summary>Onde au sol qui s'ouvre en quatre rayons et s'efface.</summary>
    private void DrawRing(float progress, Color tint)
    {
        int radius = DodgeRingRadii[Mathf.Min(DodgeRingRadii.Length - 1, (int)(progress * DodgeRingRadii.Length))];
        DrawTexture(PixelEllipseArt.Get(radius, false), PixelEllipseArt.Offset(radius), tint with { A = 1f - progress * 0.8f });
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(-12f, 20f, 24f, 2f), BarBackColor);
        DrawRect(new Rect2(-12f, 20f, 24f * Mathf.Clamp(_charge, 0f, 1f), 2f), ReadyColor);
        if (_dodgeRemaining > 0f)
            DrawRing(1f - _dodgeRemaining / DodgeRingSeconds, DodgeRingColor);
        if (_landingRemaining > 0f)
            DrawRing(1f - _landingRemaining / LandingRingSeconds, LandingRingColor);
        if (_dashVisible)
            DrawTexture(PixelEllipseArt.Get(ReadyRingRadius, false), PixelEllipseArt.Offset(ReadyRingRadius), ReadyColor);
    }
}
