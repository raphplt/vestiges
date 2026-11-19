using Godot;

namespace Vestiges.World3D;

/// <summary>
/// Ombre ronde posée à plat sous un corps, sur le sol réel : en l'air, elle reste en bas et rapetisse, et dit la hauteur
/// du saut.
/// </summary>
public static class ReliefShadow
{
    private static ImageTexture _texture;

    public static Sprite3D Create(Node3D parent, string name)
    {
        Sprite3D shadow = new()
        {
            Name = name,
            Texture = _texture ??= BuildTexture(),
            PixelSize = 1f / 26f,
            Shaded = false,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            AlphaCut = SpriteBase3D.AlphaCutMode.Disabled,
            RotationDegrees = new Vector3(-90f, 0f, 0f),
            Modulate = new Color(1f, 1f, 1f, 0.55f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        parent.AddChild(shadow);
        return shadow;
    }

    /// <summary>
    /// Pose l'ombre sous <paramref name="at"/> (repère du relief) ; avec <paramref name="footRadius"/>, sur le plus haut sol
    /// sous le disque du corps, comme le joueur qui tient au bord d'un rebord.
    /// </summary>
    public static void Place(Sprite3D shadow, ReliefArena arena, Vector3 at, float size, float footRadius = 0f)
    {
        float ground = footRadius > 0f ? arena.HighestUnder(at.X, at.Y, footRadius) : arena.GroundAt(at.X, at.Y);
        if (ReliefArena.IsVoid(ground))
        {
            shadow.Visible = false;
            return;
        }
        float height = Mathf.Max(0f, at.Z - ground);
        shadow.Visible = true;
        shadow.Position = new Vector3(at.X, ground + 0.012f, at.Y);
        float scale = size * Mathf.Clamp(1f - height * 0.35f, 0.45f, 1f);
        shadow.Scale = new Vector3(scale, scale, scale);
    }

    private static ImageTexture BuildTexture()
    {
        const int size = 16;
        Image image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x - size / 2f + 0.5f, y - size / 2f + 0.5f).Length() / (size / 2f);
                image.SetPixel(x, y, d < 1f ? new Color(0.02f, 0.01f, 0.05f, d < 0.7f ? 1f : 0.6f) : Colors.Transparent);
            }
        }
        return ImageTexture.CreateFromImage(image);
    }
}
