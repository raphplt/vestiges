using Godot;
using Vestiges.Core;

namespace Vestiges.UI;

/// <summary>
/// Objectif de la terre en cours (plan 51 P2c), une ligne au-dessus de la minimap : Mémoriaux ravivés, puis réveiller le
/// gardien, le battre, la terre suivante. Cachée hors des terres.
/// </summary>
public partial class LandObjective : Label
{
    // Calé sur la minimap (Minimap : 112 px de côté, 10 px du bord droit, 64 px du bas).
    private const float RightMargin = 10f;
    private const float BottomOffset = 64f + 112f + 10f;
    private static readonly Color ReadyColor = new(0.98f, 0.82f, 0.45f);
    private static readonly Color CalmColor = new(0.9f, 0.88f, 0.82f);
    private static readonly Color DangerColor = new(1f, 0.45f, 0.38f);
    private EventBus _eventBus;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        HorizontalAlignment = HorizontalAlignment.Right;
        UITheme.SetCompactText(this, 10);
        AddThemeColorOverride("font_shadow_color", Colors.Black);
        AddThemeConstantOverride("shadow_offset_x", 1);
        AddThemeConstantOverride("shadow_offset_y", 1);
        Visible = false;
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.LandObjectiveChanged += OnObjective;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.LandObjectiveChanged -= OnObjective;
    }

    private void OnObjective(int land, int lights, int needed, int stage)
    {
        string key = stage switch
        {
            0 => "LAND_OBJECTIVE_LIGHTS",
            1 => "LAND_OBJECTIVE_READY",
            2 => "LAND_OBJECTIVE_FIGHT",
            3 => "LAND_OBJECTIVE_RAGE",
            _ => "LAND_OBJECTIVE_PASSAGE",
        };
        Text = string.Format(Tr(key), land + 1, lights, needed);
        AddThemeColorOverride("font_color", stage == 1 ? ReadyColor : stage == 3 ? DangerColor : CalmColor);
        Visible = true;
        Place();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
            Place();
    }

    private void Place()
    {
        if (GetParent() is not Control parent)
            return;
        Size = new Vector2(Mathf.Max(GetCombinedMinimumSize().X, 220f), GetCombinedMinimumSize().Y);
        Position = new Vector2(parent.Size.X - Size.X - RightMargin, parent.Size.Y - BottomOffset - Size.Y);
    }
}
