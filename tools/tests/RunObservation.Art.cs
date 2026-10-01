using System.Threading.Tasks;
using Godot;
using Vestiges.UI;

namespace Vestiges.Tests;

public partial class RunObservation
{
    /// <summary>Textures du HUD dans Main ; les sceaux sont présentés en galerie tant que le plan 24 L4 manque.</summary>
    private async Task CaptureHudArt()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        await Frames(90);
        XpBar bar = _world.GetNode<XpBar>("HUD/HudRoot/XpBar");
        bar.SetRatio(0.62f, true);
        await Frames(90);
        SaveFrame("art-hud-xp");
        CanvasLayer gallery = new() { Layer = 30 };
        _world.AddChild(gallery);
        HBoxContainer row = new() { Position = new Vector2(60, 180) };
        gallery.AddChild(row);
        foreach (string color in new[] { "red", "green", "blue" })
        {
            Texture2D sheet = GD.Load<Texture2D>($"res://assets/ui/hud/plan25/quest_seal_{color}.png");
            foreach (int step in new[] { 0, 4, 8 })
            {
                TextureRect icon = new()
                {
                    Texture = new AtlasTexture { Atlas = sheet, Region = new Rect2(step * 16, 0, 16, 16) },
                    TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                    CustomMinimumSize = new Vector2(48, 48),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                };
                row.AddChild(icon);
            }
        }
        await Frames(2);
        SaveFrame("art-seals-preview-only");
        gallery.QueueFree();
        bar.Flash();
        await Frames(3);
        SaveFrame("art-hud-level-flash");
        GD.Print("[RunObservation] RESULT art_hud=True seals_preview_only=True");
    }
}
