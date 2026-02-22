using Godot;
using Vestiges.Core;

namespace Vestiges.World;

public enum DayPhase
{
    Day,
    Dusk,
    Night,
    Dawn
}

/// <summary>
/// Gère le cycle jour/nuit : timer, transitions visuelles via CanvasModulate.
/// Émet DayPhaseChanged à chaque transition de phase.
/// </summary>
public partial class DayNightCycle : Node
{
    private float _dayDuration;
    private float _duskDuration;
    private float _nightDuration;
    private float _dawnDuration;

    private DayPhase _currentPhase = DayPhase.Day;
    private float _phaseElapsed;
    private int _currentNight;

    private CanvasModulate _canvasModulate;
    private EventBus _eventBus;
    private Tween _colorTween;

    private static readonly Color DayColor = new(1.0f, 0.95f, 0.85f);
    private static readonly Color DuskColor = new(0.55f, 0.45f, 0.65f);
    private static readonly Color NightColor = new(0.08f, 0.06f, 0.12f);

    public DayPhase CurrentPhase => _currentPhase;
    public int CurrentNight => _currentNight;

    /// <summary>Progression dans la phase courante, de 0 à 1.</summary>
    public float PhaseProgress
    {
        get
        {
            float duration = GetCurrentPhaseDuration();
            if (duration <= 0f)
                return 1f;
            return Mathf.Clamp(_phaseElapsed / duration, 0f, 1f);
        }
    }

    public override void _Ready()
    {
        LoadConfig();

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _canvasModulate = GetNode<CanvasModulate>("../CanvasModulate");

        _canvasModulate.Color = DayColor;
        _currentPhase = DayPhase.Day;
        _phaseElapsed = 0f;
        _currentNight = 0;

        _eventBus.EmitSignal(EventBus.SignalName.DayPhaseChanged, "Day");

        GD.Print($"[DayNightCycle] Started — Day duration: {_dayDuration}s, Night: {_nightDuration}s");
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _phaseElapsed += dt;

        float duration = GetCurrentPhaseDuration();
        if (_phaseElapsed >= duration)
        {
            AdvancePhase();
        }
    }

    private void AdvancePhase()
    {
        DayPhase previousPhase = _currentPhase;

        switch (_currentPhase)
        {
            case DayPhase.Day:
                _currentPhase = DayPhase.Dusk;
                TransitionColor(DuskColor, 3f);
                break;

            case DayPhase.Dusk:
                _currentPhase = DayPhase.Night;
                _currentNight++;
                TransitionColor(NightColor, 2f);
                break;

            case DayPhase.Night:
                _currentPhase = DayPhase.Dawn;
                TransitionColor(DayColor, 5f);
                break;

            case DayPhase.Dawn:
                _currentPhase = DayPhase.Day;
                _canvasModulate.Color = DayColor;
                break;
        }

        _phaseElapsed = 0f;
        _eventBus.EmitSignal(EventBus.SignalName.DayPhaseChanged, _currentPhase.ToString());

        GD.Print($"[DayNightCycle] {previousPhase} → {_currentPhase} (Night #{_currentNight})");
    }

    private void TransitionColor(Color target, float duration)
    {
        _colorTween?.Kill();
        _colorTween = CreateTween();
        _colorTween.TweenProperty(_canvasModulate, "color", target, duration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
    }

    private float GetCurrentPhaseDuration()
    {
        return _currentPhase switch
        {
            DayPhase.Day => _dayDuration,
            DayPhase.Dusk => _duskDuration,
            DayPhase.Night => _nightDuration,
            DayPhase.Dawn => _dawnDuration,
            _ => _dayDuration
        };
    }

    private void LoadConfig()
    {
        FileAccess file = FileAccess.Open("res://data/scaling/day_night_cycle.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[DayNightCycle] Cannot open day_night_cycle.json, using defaults");
            SetDefaults();
            return;
        }

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        if (json.Parse(jsonText) != Error.Ok)
        {
            GD.PushError($"[DayNightCycle] Parse error: {json.GetErrorMessage()}");
            SetDefaults();
            return;
        }

        Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();
        _dayDuration = (float)dict["day_duration"].AsDouble();
        _duskDuration = (float)dict["dusk_duration"].AsDouble();
        _nightDuration = (float)dict["night_duration"].AsDouble();
        _dawnDuration = (float)dict["dawn_duration"].AsDouble();
    }

    private void SetDefaults()
    {
        _dayDuration = 120f;
        _duskDuration = 30f;
        _nightDuration = 90f;
        _dawnDuration = 10f;
    }
}
