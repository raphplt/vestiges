using System.Threading.Tasks;
using Godot;
using Vestiges.UI;

namespace Vestiges.Tests;

public partial class RunObservation
{
    /// <summary>Galerie de sprites au sol dans Main : aucun ramassage ou bonus simulé.</summary>
    private async Task CapturePickupArt()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        await Frames(90);
        using FileAccess file = FileAccess.Open("res://assets/vfx/pickups/pickups_manifest.json", FileAccess.ModeFlags.Read);
        Json json = new();
        json.Parse(file.GetAsText());
        Godot.Collections.Dictionary manifest = json.Data.AsGodotDictionary();
        Godot.Collections.Dictionary entries = manifest["pickups"].AsGodotDictionary();
        int idleFrames = manifest["idle_frames"].AsInt32();
        int disappearFrames = manifest["disappear_frames"].AsInt32();
        Texture2D[,] idle = new Texture2D[entries.Count, idleFrames];
        Texture2D[,] disappear = new Texture2D[entries.Count, disappearFrames];
        Sprite2D[] sprites = new Sprite2D[entries.Count];
        Sprite2D[] glows = new Sprite2D[entries.Count];
        int index = 0;
        const string folder = "res://assets/vfx/pickups/";
        foreach (Variant key in entries.Keys)
        {
            Godot.Collections.Dictionary entry = entries[key].AsGodotDictionary();
            Texture2D sheet = GD.Load<Texture2D>(folder + entry["idle"].AsString());
            Texture2D end = GD.Load<Texture2D>(folder + entry["disappear"].AsString());
            for (int frame = 0; frame < idleFrames; frame++)
                idle[index, frame] = new AtlasTexture { Atlas = sheet, Region = new Rect2(frame * 16, 0, 16, 16) };
            for (int frame = 0; frame < disappearFrames; frame++)
                disappear[index, frame] = new AtlasTexture { Atlas = end, Region = new Rect2(frame * 16, 0, 16, 16) };
            Vector2 position = _player.GlobalPosition + new Vector2((index - 2) * 30, 30);
            glows[index] = new Sprite2D
            {
                Texture = GD.Load<Texture2D>(folder + entry["glow"].AsString()), Position = position,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest, ZIndex = -1,
            };
            sprites[index] = new Sprite2D
            {
                Position = position + new Vector2(0, -10), TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            };
            _world.AddChild(glows[index]);
            _world.AddChild(sprites[index]);
            index++;
        }
        for (int frame = 0; frame < idleFrames; frame++)
        {
            for (int item = 0; item < sprites.Length; item++)
                sprites[item].Texture = idle[item, frame];
            await Frames(6);
            SaveFrame($"art-pickups-preview-idle-{frame}");
        }
        for (int frame = 0; frame < disappearFrames; frame++)
        {
            for (int item = 0; item < sprites.Length; item++)
            {
                sprites[item].Texture = disappear[item, frame];
                glows[item].Visible = false;
            }
            await Frames(6);
            SaveFrame($"art-pickups-preview-disappear-{frame}");
        }
        foreach (Sprite2D sprite in sprites)
            sprite.QueueFree();
        foreach (Sprite2D glow in glows)
            glow.QueueFree();
        GD.Print("[RunObservation] RESULT art_pickups_preview_only=True");
    }

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
