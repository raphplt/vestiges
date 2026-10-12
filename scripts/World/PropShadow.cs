using Godot;
using Vestiges.Core;

namespace Vestiges.World;

/// <summary>
/// Ombres des décors : ombre de contact sous leur emprise au sol (texture commune : GroundShadow), et ombre portée des
/// grands décors au soleil bas (plan 39 A3).
/// </summary>
public static class PropShadow
{
    /// <summary>
    /// Décalage à l'écran d'un pixel de hauteur et hauteur minimale d'un décor qui projette, posés par WorldSetup
    /// depuis data/world/atmosphere.json avant la création des décors. Nul : pas d'ombre portée.
    /// </summary>
    public static Vector2 CastPerHeight { get; set; }
    public static float CastMinHeightPx { get; set; } = float.MaxValue;

    // Partagé par toutes les ombres portées : le rendu par lots est conservé.
    private static ShaderMaterial _castMaterial;

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

    /// <summary>
    /// Ombre portée d'un sprite du décor (base ou canopée), avec sa texture, son décalage et sa position dans le nœud.
    /// Un point du sprite à <c>h</c> px au-dessus du pied du décor tombe au sol à <c>h × CastPerHeight</c> : la
    /// transformation retourne et cisaille le sprite. Le découpage à l'écran suit donc l'ombre réelle, sans shader de
    /// sommet. Les pixels sous le pied (le devant de l'emprise) passent un peu derrière : écart invisible au sol.
    /// </summary>
    public static Sprite2D CreateCast(Texture2D texture, Vector2 offset, Vector2 position)
    {
        _castMaterial ??= new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/prop_cast_shadow.gdshader") };
        Transform2D cast = new(Vector2.Right, -CastPerHeight, Vector2.Zero);
        return new Sprite2D
        {
            Texture = texture,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Offset = offset,
            Transform = cast * new Transform2D(0f, position),
            Material = _castMaterial,
            ZIndex = -1,
        };
    }
}
