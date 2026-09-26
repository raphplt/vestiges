using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Colonne de lumière à la couleur d'une rareté, dessinée par light_column.gdshader sur la grille des texels.
/// Se lit de loin, et dépasse du bord de l'écran avant que sa source n'y entre. Pied au point d'origine du nœud.
/// </summary>
public partial class LightColumn : Sprite2D
{
    private static readonly StringName SizeParam = "size_px";
    private static readonly StringName CoreParam = "core_width";
    private static readonly StringName GroundParam = "ground_radius";
    private static readonly StringName SeedParam = "seed";
    private static readonly StringName LightParam = "c_light";
    private static readonly StringName MidParam = "c_mid";
    private static readonly StringName DarkParam = "c_dark";

    private static Shader _shader;
    private static Texture2D _pixel;

    public LightColumn()
    {
        _shader ??= GD.Load<Shader>("res://assets/shaders/light_column.gdshader");
        if (_pixel == null)
        {
            Image image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
            image.Fill(Colors.White);
            _pixel = ImageTexture.CreateFromImage(image);
        }
        Texture = _pixel;
        TextureFilter = TextureFilterEnum.Nearest;
        Material = new ShaderMaterial { Shader = _shader };
    }

    /// <param name="height">Hauteur de la colonne en pixels monde, au-dessus du pied.</param>
    /// <param name="coreWidth">Demi-largeur du cœur clair, en texels (la forme varie avec la rareté).</param>
    public void Configure(RarityColors colors, float height, float coreWidth, float groundRadius = 11f)
    {
        // Dimensions paires : les bords du quad tombent sur la grille des texels.
        float width = Mathf.Ceil(Mathf.Max(coreWidth + 7f, groundRadius)) * 2f + 2f;
        float totalHeight = Mathf.Ceil((height + groundRadius) / 2f) * 2f;
        Scale = new Vector2(width, totalHeight);
        // Le centre de la tache au sol tombe sur l'origine du nœud.
        Position = new Vector2(0f, -totalHeight * 0.5f + groundRadius * 0.5f);

        ShaderMaterial material = (ShaderMaterial)Material;
        material.SetShaderParameter(SizeParam, new Vector2(width, totalHeight));
        material.SetShaderParameter(CoreParam, coreWidth);
        material.SetShaderParameter(GroundParam, groundRadius);
        material.SetShaderParameter(SeedParam, GD.Randf());
        material.SetShaderParameter(LightParam, colors.Light);
        material.SetShaderParameter(MidParam, colors.Main);
        material.SetShaderParameter(DarkParam, colors.Dark);
    }
}
