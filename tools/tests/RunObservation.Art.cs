using System.Threading.Tasks;
using Godot;
using Vestiges.UI;

namespace Vestiges.Tests;

public partial class RunObservation
{
    /// <summary>États visuels injectés dans la vraie Main ; aucune statistique de combat n'est modifiée.</summary>
    private async Task CaptureVitals()
    {
        await PrepareCloseUpScene();
        // Fixer la taille après le chargement : certains bureaux maximisent la fenêtre à l'ouverture.
        string resolution = Argument(OS.GetCmdlineUserArgs(), "--vitals-resolution", "1920x1080");
        string[] dimensions = resolution.Split('x');
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        DisplayServer.WindowSetSize(new Vector2I(int.Parse(dimensions[0]), int.Parse(dimensions[1])));
        await Frames(10);
        VitalsDisplay vitals = _world.GetNode<VitalsDisplay>("HUD/HudRoot/Vitals");
        Vestiges.Core.EventBus bus = GetNode<Vestiges.Core.EventBus>("/root/EventBus");
        vitals.SetLevel(4);
        vitals.SetHealth(80f, 80f);
        await Seconds(0.5);
        SaveFrame("vitals-01-full");
        vitals.SetHealth(36f, 80f);
        await Frames(2);
        SaveFrame("vitals-02-damage-trail");
        await Seconds(1);
        SaveFrame("vitals-03-wounded");
        vitals.SetHealth(16f, 80f);
        await Seconds(1);
        SaveFrame("vitals-04-critical");
        vitals.SetHealth(0f, 80f);
        await Seconds(1);
        SaveFrame("vitals-05-empty");
        vitals.SetLevel(99);
        vitals.SetLevel(100);
        vitals.SetHealth(1234f, 5678f);
        await Frames(3);
        SaveFrame("vitals-06-large-level-flash");
        await Seconds(0.5);
        SaveFrame("vitals-07-large-values");
        vitals.SetLevel(12);
        vitals.SetHealth(60f, 80f);
        vitals.SetShield(15f, 20f);
        bus.EmitSignal(Vestiges.Core.EventBus.SignalName.PlayerDamaged, 60f, 80f);
        bus.PublishSpecializationGauge(new Vestiges.Core.SpecializationGauge(_player.GetInstanceId(),
            Vestiges.Progression.SpecializationRuntime.OverhealReserveEffect, "", 12f, 20f));
        bus.PublishSpecializationGauge(new Vestiges.Core.SpecializationGauge(_player.GetInstanceId(),
            Vestiges.Progression.SpecializationRuntime.RallyEffect, "", 10f, 80f, 3f, 4f));
        await Seconds(0.5);
        SaveFrame("vitals-08-shield-perks");

        Label level = vitals.GetNode<Label>("Level");
        Label caption = vitals.GetNode<Label>("LevelCaption");
        Label health = vitals.GetNode<Label>("HealthValue");
        Control perks = vitals.GetNode<Control>("SurvivalPerks");
        Rect2 bounds = new(Vector2.Zero, vitals.Size);
        if (!bounds.Encloses(level.GetRect()) || !bounds.Encloses(caption.GetRect()) ||
            !bounds.Encloses(health.GetRect()) || caption.GetRect().End.Y > level.Position.Y ||
            health.GetRect().End.Y > perks.Position.Y)
            throw new System.InvalidOperationException("Les textes de la plaque PV/niveau débordent.");
        using Image screenshot = GetViewport().GetTexture().GetImage();
        GD.Print($"[RunObservation] RESULT vitals=True image={screenshot.GetSize()} plate={vitals.Size} level={level.GetRect()} health={health.GetRect()}");
    }

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
        bar.Flash();
        await Frames(3);
        SaveFrame("art-hud-level-flash");
        GD.Print("[RunObservation] RESULT art_hud=True seals_preview_only=True");
    }
}
