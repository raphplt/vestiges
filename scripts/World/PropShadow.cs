using Godot;
using Vestiges.Core;

namespace Vestiges.World;

/// <summary>
/// Ombre de contact des décors, placée sous leur emprise au sol (texture commune : GroundShadow).
/// </summary>
public static class PropShadow
{
    /// <summary>Ombre sous une emprise au sol (pixels relatifs au nœud), un peu plus large qu'elle.</summary>
    public static Sprite2D Create(Vector2[] ground)
    {
        float minX = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue, sumY = 0f;
        foreach (Vector2 point in ground)
        {
            minX = Mathf.Min(minX, point.X);
            maxX = Mathf.Max(maxX, point.X);
            maxY = Mathf.Max(maxY, point.Y);
            sumY += point.Y;
        }
        ImageTexture texture = GroundShadow.TextureFor(GroundShadow.SnapWidth((maxX - minX) * 1.15f));
        // Centrée sur l'emprise, sans dépasser nettement devant le décor.
        float centerY = Mathf.Min(sumY / ground.Length, maxY - texture.GetHeight() * 0.35f);
        return new Sprite2D
        {
            Texture = texture,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Position = new Vector2(Mathf.Round((minX + maxX) * 0.5f), Mathf.Round(centerY)),
            ZIndex = -1,
        };
    }
}
