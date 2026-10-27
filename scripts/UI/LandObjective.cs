using Godot;
using Vestiges.Core;

namespace Vestiges.UI;

/// <summary>
/// Objectif de la terre en cours (plan 51 P2c), une ligne en haut au centre sous la Série (plan 57 C1 : au-dessus de la
/// minimap, personne ne la voyait) : Mémoriaux ravivés, puis réveiller le gardien, le battre, la terre suivante ; sur la
/// terre du late game, les échos vaincus. Elle s'éclaire quand elle change, et s'efface sous le carton d'arrivée, qui
/// occupe la même place. La dernière minute avant que le gardien se lève seul, elle passe au rouge et compte à rebours
/// (plan 57 C2). Cachée hors des terres. Coordonnées en unités de référence 960×540.
/// </summary>
public partial class LandObjective : Label
{
    private const float HalfWidth = 200f;
    private const float FlashSeconds = 0.8f;
    private static readonly Color ReadyColor = new(0.98f, 0.82f, 0.45f);
    private static readonly Color CalmColor = new(0.9f, 0.88f, 0.82f);
    private static readonly Color DangerColor = new(1f, 0.45f, 0.38f);
    private static readonly Color FlashModulate = new(1.7f, 1.7f, 1.7f);
    private static readonly Color TickModulate = new(1.35f, 1.2f, 1.2f);
    private EventBus _eventBus;
    private Tween _flash;
    private bool _active;
    private bool _masked;
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
        Visible = _active && !_masked;
        if (Visible)
            Flash(FlashModulate);
    }

    private void OnObjective(int land, int lights, int needed, int stage)
    {
        _objective = (land, lights, needed, stage);
        Refresh();
        _active = true;
        Visible = !_masked;
        if (Visible)
            Flash(FlashModulate);
    }

    private void OnRageCountdown(int seconds)
    {
        bool started = _rageCountdown < 0 && seconds >= 0;
        _rageCountdown = seconds;
        if (!_active)
            return;
        Refresh();
        // Chaque seconde de l'alerte bat un peu ; son entrée s'éclaire comme un changement d'objectif.
        if (Visible && seconds >= 0)
            Flash(started ? FlashModulate : TickModulate);
    }

    private void Refresh()
    {
        (int land, int lights, int needed, int stage) = _objective;
        if (_rageCountdown >= 0 && stage is 0 or 1)
        {
            Text = string.Format(Tr("LAND_OBJECTIVE_RAGE_SOON"), land + 1, $"{_rageCountdown / 60}:{_rageCountdown % 60:D2}");
            AddThemeColorOverride("font_color", DangerColor);
            return;
        }
        string key = stage switch
        {
            0 => "LAND_OBJECTIVE_LIGHTS",
            1 => "LAND_OBJECTIVE_READY",
            2 => "LAND_OBJECTIVE_FIGHT",
            3 => "LAND_OBJECTIVE_RAGE",
            5 => "LAND_OBJECTIVE_HOLD",
            6 => "LAND_OBJECTIVE_ECHO",
            7 => "LAND_OBJECTIVE_VICTORY",
            _ => "LAND_OBJECTIVE_PASSAGE",
        };
        Text = string.Format(Tr(key), land + 1, lights, needed);
        AddThemeColorOverride("font_color", stage is 1 or 7 ? ReadyColor : stage is 3 or 6 ? DangerColor : CalmColor);
    }

    private void Flash(Color from)
    {
        _flash?.Kill();
        Modulate = from;
        _flash = CreateTween();
        _flash.TweenProperty(this, "modulate", Colors.White, FlashSeconds);
    }
}
