using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Étalonnage et brume d'un biome (plan 39 A1), bloc « atmosphere » de <c>data/biomes/*.json</c>. L'étalonnage est
/// publié dans <c>world_grade.gdshaderinc</c> par <c>World/BiomeAtmosphere</c>, avec les ombres de nuages (A2) :
/// part du ciel couverte et noirceur de leur ombre, et l'opacité des ombres portées des grands décors (A3). Grime : dosage
/// de la direction crade (plan 40) dans ce biome, dont la recette est commune (<c>data/world/atmosphere.json</c>).
/// </summary>
public readonly record struct BiomeAtmosphereStyle(
    Color Tint, float TintStrength, float Exposure, float Saturation, float Contrast, float FogDensity, Color FogColor,
    float CloudCover, float CloudOpacity, float ShadowOpacity, float Grime)
{
    public static readonly BiomeAtmosphereStyle Neutral = new(Colors.White, 0f, 1f, 1f, 1f, 0f, new Color(0.5f, 0.5f, 0.5f), 0f, 0f, 0f, 0f);

    /// <summary>
    /// Multiplicateur de couleur du shader : la teinte est ramenée à une luminance de 1 pour colorer sans assombrir,
    /// dosée par sa force, puis multipliée par l'exposition.
    /// </summary>
    public Vector3 GradeMultiplier()
    {
        float luma = Tint.R * 0.299f + Tint.G * 0.587f + Tint.B * 0.114f;
        Vector3 tint = luma > 0.001f ? new Vector3(Tint.R, Tint.G, Tint.B) / luma : Vector3.One;
        return Vector3.One.Lerp(tint, TintStrength) * Exposure;
    }
}
