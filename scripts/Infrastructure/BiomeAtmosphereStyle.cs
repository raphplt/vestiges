using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Étalonnage et brume d'un biome (plan 39 A1), bloc « atmosphere » de <c>data/biomes/*.json</c>. L'étalonnage est
/// publié dans <c>world_grade.gdshaderinc</c> par <c>World/BiomeAtmosphere</c>.
/// </summary>
public readonly record struct BiomeAtmosphereStyle(
    Color Tint, float TintStrength, float Exposure, float Saturation, float Contrast, float FogDensity, Color FogColor)
{
    public static readonly BiomeAtmosphereStyle Neutral = new(Colors.White, 0f, 1f, 1f, 1f, 0f, new Color(0.5f, 0.5f, 0.5f));

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
