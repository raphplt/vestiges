using Godot;
using Vestiges.Combat;
using Vestiges.Core;

namespace Vestiges.World;

/// <summary>
/// Ancrage de mémoire au centre de la map.
/// Émet de la lumière (PointLight2D) — seul refuge visuel la nuit.
/// Zone de sécurité visible autour du Foyer.
/// </summary>
public partial class Foyer : Node2D
{
    public const float SafeRadius = 150f;
    public const float DefaultSafeRadius = 150f;

    private float _effectiveSafeRadius = DefaultSafeRadius;
    private PointLight2D _light;
    private Polygon2D _safeZone;
    private EventBus _eventBus;

    private float _dayEnergy = 0.6f;
    private float _nightEnergy = 1.8f;
    private float _dayRange = 1.0f;
    private float _nightRange = 1.5f;

    private GpuParticles2D _flame;
    private AudioStreamPlayer _crackleSfx;
    private AudioStreamPlayer _auraSfx;

    public override void _Ready()
    {
        _light = GetNode<PointLight2D>("Light");
        _eventBus = GetNode<EventBus>("/root/EventBus");

        _eventBus.DayPhaseChanged += OnDayPhaseChanged;

        _light.Energy = _dayEnergy;
        _light.TextureScale = _dayRange;

        _flame = VfxFactory.CreateFlameParticles(1.5f);
        if (_flame != null)
        {
            _flame.Position = new Vector2(0, -8);
            AddChild(_flame);
        }

        CreateSafeZoneVisual();
        StartFoyerAudio();
    }

    private void StartFoyerAudio()
    {
        _crackleSfx = new AudioStreamPlayer { Bus = "SFX", Name = "FoyerCrackle" };
        _auraSfx    = new AudioStreamPlayer { Bus = "Ambiance", Name = "FoyerAura", VolumeDb = -6f };
        AddChild(_crackleSfx);
        AddChild(_auraSfx);
        LoadAndLoop(_crackleSfx, "res://assets/audio/sfx/foyer/sfx_foyer_crepitement.wav");
        LoadAndLoop(_auraSfx,    "res://assets/audio/sfx/foyer/sfx_foyer_aura.wav");
    }

    private static void LoadAndLoop(AudioStreamPlayer player, string path)
    {
        AudioStream stream = GD.Load<AudioStream>(path);
        if (stream == null)
            return;
        if (stream is AudioStreamWav wav)
            wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        player.Stream = stream;
        player.Play();
    }

    /// <summary>Joue le son d'intensification du foyer (upgrade ou buff).</summary>
    public void PlayUpgradeSound()
    {
        Infrastructure.AudioManager.Play("sfx_foyer_upgrade", 0f);
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.DayPhaseChanged -= OnDayPhaseChanged;
    }

    private void CreateSafeZoneVisual()
    {
        _safeZone = new Polygon2D();

        int segments = 32;
        Vector2[] points = new Vector2[segments];
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.Tau * i / segments;
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _effectiveSafeRadius;
        }

        _safeZone.Polygon = points;
        _safeZone.Color = new Color(1f, 0.85f, 0.4f, 0.06f);
        _safeZone.ZIndex = -1;
        AddChild(_safeZone);
    }

    public float EffectiveSafeRadius => _effectiveSafeRadius;

    /// <summary>Désactive la zone de sécurité (mutateur "Foyer éteint").</summary>
    public void DisableSafeZone()
    {
        _effectiveSafeRadius = 0f;
        if (_safeZone != null)
            _safeZone.Visible = false;
        GD.Print("[Foyer] Safe zone disabled (mutator)");
    }

    private void OnDayPhaseChanged(string phase)
    {
        float targetEnergy;
        float targetRange;
        float targetAlpha;

        switch (phase)
        {
            case "Night":
                targetEnergy = _nightEnergy;
                targetRange = _nightRange;
                targetAlpha = 0.12f;
                break;
            case "Dawn":
            case "Day":
                targetEnergy = _dayEnergy;
                targetRange = _dayRange;
                targetAlpha = 0.06f;
                break;
            case "Dusk":
                targetEnergy = (_dayEnergy + _nightEnergy) / 2f;
                targetRange = (_dayRange + _nightRange) / 2f;
                targetAlpha = 0.09f;
                break;
            default:
                targetEnergy = _dayEnergy;
                targetRange = _dayRange;
                targetAlpha = 0.06f;
                break;
        }

        Tween tween = CreateTween();
        tween.SetParallel();
        tween.TweenProperty(_light, "energy", targetEnergy, 2f)
            .SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(_light, "texture_scale", targetRange, 2f)
            .SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(_safeZone, "color:a", targetAlpha, 2f)
            .SetTrans(Tween.TransitionType.Sine);
    }
}
