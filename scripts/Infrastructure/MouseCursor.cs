using System;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Curseurs pixel art du jeu (plan 37 H5, DECISIONS §117) : flèche de pierre, main au survol d'un bouton. Agrandis d'un
/// facteur entier selon la hauteur de la fenêtre (×2 en 1080p, ×4 en 2160p), recalculés au redimensionnement. Caché en
/// combat, où la souris ne sert à rien : il revient dès qu'un menu met le jeu en pause.
/// </summary>
public static class MouseCursor
{
    private const string Folder = "res://assets/ui/cursor/";
    private const float ReferenceHeight = 540f;

    private static SceneTree _tree;
    private static Func<bool> _inCombat;
    private static int _scale;
    private static bool _hidden;

    public static void Install(SceneTree tree, Func<bool> inCombat)
    {
        if (DisplayServer.GetName() == "headless" || _tree != null)
            return;
        _tree = tree;
        _inCombat = inCombat;
        tree.Root.SizeChanged += ApplyScale;
        tree.ProcessFrame += UpdateVisibility;
        ApplyScale();
    }

    /// <summary>Rend les curseurs du système avant de quitter : leurs textures ne survivent pas au moteur.</summary>
    public static void Release()
    {
        if (_tree == null)
            return;
        _tree.Root.SizeChanged -= ApplyScale;
        _tree.ProcessFrame -= UpdateVisibility;
        Input.SetCustomMouseCursor(null, Input.CursorShape.Arrow);
        Input.SetCustomMouseCursor(null, Input.CursorShape.PointingHand);
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _tree = null;
        _scale = 0;
    }

    private static void ApplyScale()
    {
        int scale = Mathf.Max(1, Mathf.RoundToInt(_tree.Root.Size.Y / ReferenceHeight));
        if (scale == _scale)
            return;
        _scale = scale;
        using FileAccess file = FileAccess.Open(Folder + "cursor_manifest.json", FileAccess.ModeFlags.Read);
        Godot.Collections.Dictionary manifest = Json.ParseString(file.GetAsText()).AsGodotDictionary();
        SetShape(manifest, "arrow", Input.CursorShape.Arrow, scale);
        SetShape(manifest, "hand", Input.CursorShape.PointingHand, scale);
        GD.Print($"[MouseCursor] Curseurs pixel art ×{scale} pour une fenêtre de {_tree.Root.Size.Y} px de haut");
    }

    private static void SetShape(Godot.Collections.Dictionary manifest, string name, Input.CursorShape shape, int scale)
    {
        Godot.Collections.Dictionary entry = manifest[name].AsGodotDictionary();
        Image image = GD.Load<Texture2D>(Folder + entry["file"].AsString()).GetImage();
        if (image.IsCompressed())
            image.Decompress();
        image.Resize(image.GetWidth() * scale, image.GetHeight() * scale, Image.Interpolation.Nearest);
        Godot.Collections.Array hotspot = entry["hotspot"].AsGodotArray();
        Input.SetCustomMouseCursor(ImageTexture.CreateFromImage(image), shape,
            new Vector2(hotspot[0].AsSingle(), hotspot[1].AsSingle()) * scale);
    }

    private static void UpdateVisibility()
    {
        bool hide = _inCombat() && !_tree.Paused;
        if (hide == _hidden)
            return;
        _hidden = hide;
        Input.MouseMode = hide ? Input.MouseModeEnum.Hidden : Input.MouseModeEnum.Visible;
    }
}
