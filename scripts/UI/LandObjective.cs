using Godot;
using Vestiges.Core;

namespace Vestiges.UI;

/// <summary>
/// Annonces de la terre en cours (plan 51 P2c, plan 57 C3), une ligne en haut au centre sous la Série. Elle ne parle
/// qu'aux changements, quelques secondes : Mémorial ravivé, gardien levé seul, terre suivante, échos du late game. Le
/// suivi permanent est le compteur collé à la minimap (<see cref="LandMemorialPips"/>), et le carton d'arrivée donne le
/// lieu et la règle. Seule la dernière minute avant que le gardien se lève seul reste affichée, en rouge, avec son compte
/// à rebours (plan 57 C2). Elle s'efface sous le carton d'arrivée, qui occupe la même place. Coordonnées en unités de
/// référence 960×540.
/// </summary>
public partial class LandObjective : Label
{
    private const float HalfWidth = 200f;
    private const float FlashSeconds = 0.8f;
    private const float HoldSeconds = 3.5f;
    private const float FadeSeconds = 0.6f;
    private static readonly Color ReadyColor = new(0.98f, 0.82f, 0.45f);
    private static readonly Color CalmColor = new(0.9f, 0.88f, 0.82f);
    private static readonly Color DangerColor = new(1f, 0.45f, 0.38f);
    private static readonly Color FlashModulate = new(1.7f, 1.7f, 1.7f);
    private static readonly Color TickModulate = new(1.35f, 1.2f, 1.2f);
    private EventBus _eventBus;
    private Tween _tween;
    // Une annonce est en cours (passagère, ou le compte à rebours qui dure) ; le carton d'arrivée peut la masquer.
    private bool _showing;
    private bool _lasting;
    private bool _masked;
    private bool _known;
    private (int Land, int Lights, int Needed, int Stage) _objective;
    private int _rageCountdown = -1;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        HorizontalAlignment = HorizontalAlignment.Center;
        UITheme.SetCompactText(this, 16);
        AddThemeColorOverride("font_outline_color", UITheme.OutlineColor);
        AddThemeConstantOverride("outline_size", 4);
        AnchorLeft = 0.5f;
        AnchorRight = 0.5f;
        OffsetLeft = -HalfWidth;
        OffsetRight = HalfWidth;
        OffsetTop = StreakHud.Bottom + 2f;
        Visible = false;
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.LandObjectiveChanged += OnObjective;
        _eventBus.GuardianRageCountdown += OnRageCountdown;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.LandObjectiveChanged -= OnObjective;
            _eventBus.GuardianRageCountdown -= OnRageCountdown;
        }
    }

    /// <summary>Le carton d'arrivée paraît ou s'efface : la ligne lui laisse la place.</summary>
    public void SetMasked(bool masked)
    {
        _masked = masked;
        Visible = _showing && !_masked;
    }

    private void OnObjective(int land, int lights, int needed, int stage)
    {
        (int lastLand, int lastLights, int _, int lastStage) = _objective;
        bool sameLand = _known && land == lastLand;
        _objective = (land, lights, needed, stage);
        _known = true;
        if (RageShown)
            return;
        // L'arrivée sur une terre a son carton ; le gardien prêt aussi, et le combat sa barre de boss.
        string key = stage switch
        {
            0 when sameLand && lights > lastLights => "LAND_OBJECTIVE_LIGHTS",
            3 when lastStage != 3 => "LAND_OBJECTIVE_RAGE",
            4 when lastStage != 4 => "LAND_OBJECTIVE_PASSAGE",
            5 when sameLand && lights != lastLights => "LAND_OBJECTIVE_HOLD",
            6 when lastStage != 6 => "LAND_OBJECTIVE_ECHO",
            7 when lastStage != 7 => "LAND_OBJECTIVE_VICTORY",
            _ => null,
        };
        if (key == null)
            return;
        Text = string.Format(Tr(key), lights, needed);
        Announce(stage is 0 or 7 ? ReadyColor : stage is 3 or 6 ? DangerColor : CalmColor, false);
    }

    private bool RageShown => _rageCountdown >= 0 && _objective.Stage is 0 or 1;

    private void OnRageCountdown(int seconds)
    {
        bool started = _rageCountdown < 0 && seconds >= 0;
        _rageCountdown = seconds;
        if (seconds < 0)
        {
            if (_lasting)
                End();
            return;
        }
        if (!RageShown)
            return;
        Text = string.Format(Tr("LAND_OBJECTIVE_RAGE_SOON"), $"{seconds / 60}:{seconds % 60:D2}");
        // L'entrée dans l'alerte s'éclaire comme une annonce ; chaque seconde bat un peu.
        if (started)
            Announce(DangerColor, true);
        else
            Pulse(TickModulate);
    }

    private void Announce(Color color, bool lasting)
    {
        AddThemeColorOverride("font_color", color);
        _showing = true;
        _lasting = lasting;
        Visible = !_masked;
        Pulse(FlashModulate);
        if (lasting)
            return;
        _tween.TweenInterval(HoldSeconds);
        _tween.TweenProperty(this, "modulate:a", 0f, FadeSeconds);
        _tween.TweenCallback(Callable.From(End));
    }

    private void Pulse(Color from)
    {
        _tween?.Kill();
        Modulate = from;
        _tween = CreateTween();
        _tween.TweenProperty(this, "modulate", Colors.White, FlashSeconds);
    }

    private void End()
    {
        _tween?.Kill();
        _showing = false;
        _lasting = false;
        Visible = false;
        Modulate = Colors.White;
    }
}
