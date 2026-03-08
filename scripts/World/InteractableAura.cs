using Godot;

namespace Vestiges.World;

/// <summary>
/// Halo discret pour distinguer les éléments interactifs du décor inerte.
/// Effet volontairement léger : ancrage au sol, pulse lent et petite lueur mémoire optionnelle.
/// </summary>
public partial class InteractableAura : Node2D
{
    private Polygon2D _groundGlow;
    private Polygon2D _crownGlow;
    private Polygon2D _mote;

    private float _pulseSpeed = 1f;
    private float _baseAlpha = 0.14f;
    private float _pulseAlpha = 0.05f;
    private float _crownBaseAlpha = 0.08f;
    private float _crownPulseAlpha = 0.04f;
    private float _groundScaleAmplitude = 0.06f;
    private float _crownScaleAmplitude = 0.04f;
    private float _moteAmplitude = 2.5f;
    private float _moteBaseAlpha = 0.2f;
    private float _motePulseAlpha = 0.16f;
    private float _phase;

    private Vector2 _groundBaseScale = Vector2.One;
    private Vector2 _crownBaseScale = Vector2.One;
    private Vector2 _moteBasePosition = Vector2.Zero;
    private bool _animateMote;
    private bool _isActive = true;

    public override void _Ready()
    {
        ZIndex = -1;
        ProcessMode = ProcessModeEnum.Inherit;
    }

    public void Configure(
        Color baseColor,
        Color accentColor,
        float radius,
        float height,
        bool withMote = false,
        float pulseSpeed = 1f,
        float baseAlpha = 0.14f,
        float pulseAlpha = 0.05f,
        float crownAlpha = 0.08f,
        float crownPulseAlpha = 0.04f)
    {
        _pulseSpeed = pulseSpeed;
        _baseAlpha = baseAlpha;
        _pulseAlpha = pulseAlpha;
        _crownBaseAlpha = crownAlpha;
        _crownPulseAlpha = crownPulseAlpha;
        _animateMote = withMote;
        _phase = (float)GD.RandRange(0.0, Mathf.Tau);

        EnsureNodes();

        float groundWidth = radius;
        float groundHeight = Mathf.Max(3f, radius * 0.38f);
        _groundGlow.Polygon = new Vector2[]
        {
            new(-groundWidth, 0),
            new(0, -groundHeight),
            new(groundWidth, 0),
            new(0, groundHeight)
        };
        _groundGlow.Position = new Vector2(0f, 3f);
        _groundGlow.Color = new Color(baseColor, _baseAlpha);
        _groundBaseScale = Vector2.One;

        float crownWidth = radius * 0.52f;
        float crownHeight = Mathf.Max(4f, height * 0.22f);
        _crownGlow.Polygon = new Vector2[]
        {
            new(-crownWidth, 0),
            new(0, -crownHeight),
            new(crownWidth, 0),
            new(0, crownHeight)
        };
        _crownGlow.Position = new Vector2(0f, -height);
        _crownGlow.Color = new Color(accentColor, _crownBaseAlpha);
        _crownBaseScale = Vector2.One;
        _crownGlow.Visible = true;

        _mote.Polygon = new Vector2[]
        {
            new(0f, -2f),
            new(2f, 0f),
            new(0f, 2f),
            new(-2f, 0f)
        };
        _moteBasePosition = new Vector2(radius * 0.15f, -height - 5f);
        _mote.Position = _moteBasePosition;
        _mote.Color = new Color(accentColor.Lightened(0.1f), _moteBaseAlpha);
        _mote.Visible = _animateMote;

        Visible = true;
        _isActive = true;
    }

    public void SetActive(bool isActive)
    {
        _isActive = isActive;
        Visible = isActive;
    }

    public override void _Process(double delta)
    {
        if (!_isActive || _groundGlow == null)
            return;

        float time = (float)Time.GetTicksMsec() * 0.001f;
        float wave = 0.5f + 0.5f * Mathf.Sin(time * _pulseSpeed + _phase);
        float sharper = wave * wave;

        _groundGlow.Modulate = new Color(1f, 1f, 1f, _baseAlpha + sharper * _pulseAlpha);
        _groundGlow.Scale = _groundBaseScale * (1f + (wave - 0.5f) * _groundScaleAmplitude);

        _crownGlow.Modulate = new Color(1f, 1f, 1f, _crownBaseAlpha + sharper * _crownPulseAlpha);
        _crownGlow.Scale = _crownBaseScale * (1f + (wave - 0.5f) * _crownScaleAmplitude);

        if (_animateMote && _mote != null)
        {
            float moteWave = 0.5f + 0.5f * Mathf.Sin(time * (_pulseSpeed * 1.35f) + _phase + 0.8f);
            _mote.Position = _moteBasePosition + new Vector2(0f, -moteWave * _moteAmplitude);
            _mote.Modulate = new Color(1f, 1f, 1f, _moteBaseAlpha + moteWave * _motePulseAlpha);
        }
    }

    private void EnsureNodes()
    {
        if (_groundGlow == null)
        {
            _groundGlow = new Polygon2D { Name = "GroundGlow", ZIndex = -2 };
            AddChild(_groundGlow);
        }

        if (_crownGlow == null)
        {
            _crownGlow = new Polygon2D { Name = "CrownGlow", ZIndex = -1 };
            AddChild(_crownGlow);
        }

        if (_mote == null)
        {
            _mote = new Polygon2D { Name = "Mote", ZIndex = 1 };
            AddChild(_mote);
        }
    }
}
