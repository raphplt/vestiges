using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Invite du dash au premier danger (DECISIONS §184 : le premier testeur n'avait pas compris son intérêt). Quand une
/// créature s'approche, une ligne sous le personnage rappelle la touche et l'invulnérabilité ; elle se retire après
/// quelques secondes et revient au danger suivant, jusqu'à trois dashs faits. Une fois par profil.
/// </summary>
public partial class DashHint : Label
{
    private const string HintId = "dash_dodge";
    private const float DangerRadius = 160f;
    private const float CheckInterval = 0.2f;
    private const float ShowSeconds = 5f;
    private const int DashesToLearn = 3;
    private const float HintWidth = 360f;
    private const float HintTop = 52f;
    private static readonly Color WhiteOff = new(0xE8 / 255f, 0xE0 / 255f, 0xD4 / 255f);

    private GroupCache _groups;
    private float _check;
    private float _shownFor;
    private int _dashesAtStart = -1;
    private Tween _fade;

    public override void _Ready()
    {
        if (MetaSaveManager.HasSeenHint(HintId))
        {
            QueueFree();
            return;
        }
        _groups = GetNode<GroupCache>("/root/GroupCache");
        MouseFilter = MouseFilterEnum.Ignore;
        Modulate = Colors.Transparent;
        UITheme.SetCompactText(this, 11);
        AddThemeColorOverride("font_color", WhiteOff);
        AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        AddThemeConstantOverride("outline_size", 3);
        HorizontalAlignment = HorizontalAlignment.Center;
        AnchorLeft = 0.5f;
        AnchorRight = 0.5f;
        AnchorTop = 0.5f;
        AnchorBottom = 0.5f;
        OffsetLeft = -HintWidth / 2f;
        OffsetRight = HintWidth / 2f;
        OffsetTop = HintTop;
        OffsetBottom = HintTop + 16f;
    }

    public override void _Process(double delta)
    {
        if (_groups?.GetPlayer() is not Player player)
            return;
        if (_dashesAtStart < 0)
            _dashesAtStart = player.Mobility.DashesStarted;
        if (player.Mobility.DashesStarted - _dashesAtStart >= DashesToLearn)
        {
            MetaSaveManager.MarkHintSeen(HintId);
            FadeTo(Colors.Transparent, 0.6f);
            SetProcess(false);
            return;
        }
        if (_shownFor > 0f)
        {
            _shownFor -= (float)delta;
            if (_shownFor <= 0f)
                FadeTo(Colors.Transparent, 0.6f);
            return;
        }
        _check -= (float)delta;
        if (_check > 0f)
            return;
        _check = CheckInterval;
        if (!DangerNear(player.GlobalPosition))
            return;
        Text = string.Format(Tr("HINT_DASH_DODGE"), InputRemapManager.GetKeyName("mobility"));
        _shownFor = ShowSeconds;
        FadeTo(Colors.White, 0.3f);
    }

    private static bool DangerNear(Vector2 position)
    {
        using CrowdQuery query = CrowdIndex.Near(position, DangerRadius);
        foreach (Node2D target in query.Targets)
            if (target is Enemy && target.GlobalPosition.DistanceSquaredTo(position) <= DangerRadius * DangerRadius)
                return true;
        return false;
    }

    private void FadeTo(Color color, float seconds)
    {
        _fade?.Kill();
        _fade = CreateTween();
        _fade.TweenProperty(this, "modulate", color, seconds);
    }
}
