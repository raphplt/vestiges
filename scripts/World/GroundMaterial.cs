using Godot;

namespace Vestiges.World;

/// <summary>Réglages des jonctions entre biomes (<c>ground_blend</c> dans world_gen.json).</summary>
public struct GroundBlendConfig
{
    public bool Enabled;
    public float BandPx;
    public float EdgeNoise;
    public float NoiseScale;
    // Part du grain fin qui ronge le bord des plaques (0 = bord lisse, 1 = grain seul).
    public float Grain;

    public static GroundBlendConfig Default => new()
    {
        Enabled = true,
        BandPx = 34f,
        EdgeNoise = 0.35f,
        NoiseScale = 0.045f,
        Grain = 0.3f,
    };
}

/// <summary>
/// Matériau du sol de la run (assets/shaders/ground.gdshader) : jonctions entre biomes et oubli du sol.
/// Pour les jonctions, décrit chaque cellule (biome, tuile posée) dans une petite texture ; le shader lit la matière
/// voisine dans l'atlas des tuiles du sol et la mélange par tramage le long des frontières. Construit une fois,
/// après la pose du terrain : aucun coût par frame côté CPU. L'oubli lit la mémoire publiée par ErasureManager.
/// </summary>
public static class GroundMaterial
{
    private const string ShaderPath = "res://assets/shaders/ground.gdshader";
    private const byte NoBlend = 255;

    public static void Apply(TileMapLayer ground, TileMapLayer roads, WorldGenerator generator, BiomeTileMapper tileMapper,
                             TerrainType[,] terrain, Vector2I radius, GroundBlendConfig config)
    {
        Shader shader = GD.Load<Shader>(ShaderPath);
        // Les routes s'effacent comme le sol mais ne participent pas aux jonctions.
        if (roads != null)
        {
            ShaderMaterial roadMaterial = new() { Shader = shader };
            roadMaterial.SetShaderParameter("blend_enabled", false);
            roads.Material = roadMaterial;
        }

        ShaderMaterial material = new() { Shader = shader };
        material.SetShaderParameter("blend_enabled", false);
        ground.Material = material;
        if (!config.Enabled)
            return;

        ulong started = Time.GetTicksMsec();
        Vector2I size = radius * 2 + Vector2I.One;
        byte[] cells = new byte[size.X * size.Y * 4];

        for (int gy = 0; gy < size.Y; gy++)
        {
            for (int gx = 0; gx < size.X; gx++)
            {
                int offset = (gy * size.X + gx) * 4;
                cells[offset] = NoBlend;

                int x = gx - radius.X;
                int y = gy - radius.Y;
                if (!generator.IsWithinBounds(x, y) || generator.IsErased(x, y))
                    continue;

                int tileId = tileMapper.TileIdAt(ground, new Vector2I(x, y));
                int biome = generator.GetBiomeIndex(x, y);
                if (tileId < 0 || biome < 0 || biome >= NoBlend / BiomeTileMapper.MaxMaterialsPerBiome)
                    continue;
                // Identifiant de mélange : le biome, ou la matière de la tuile quand le biome fond ses matières entre elles.
                // L'eau ne se fond que dans ces biomes-là (rives tramées), et toujours comme une matière à part.
                bool blendTerrains = generator.GetBiome(x, y)?.BlendTerrains == true;
                bool water = terrain[gx, gy] == TerrainType.Water;
                if (water && !blendTerrains)
                    continue;
                int blendId = biome * BiomeTileMapper.MaxMaterialsPerBiome;
                if (water)
                    blendId += BiomeTileMapper.MaxMaterialsPerBiome - 1;
                else if (blendTerrains)
                    blendId += tileMapper.GetMaterialOfTile(tileId);
                cells[offset] = (byte)blendId;
                cells[offset + 1] = (byte)(tileId & 0xFF);
                cells[offset + 2] = (byte)(tileId >> 8);
            }
        }

        MarkBorderCells(cells, size);
        Image atlas = tileMapper.AtlasImage;
        if (atlas == null)
            return;

        material.SetShaderParameter("blend_enabled", true);
        material.SetShaderParameter("cell_map", ImageTexture.CreateFromImage(Image.CreateFromData(size.X, size.Y, false, Image.Format.Rgba8, cells)));
        material.SetShaderParameter("tile_atlas", ImageTexture.CreateFromImage(atlas));
        material.SetShaderParameter("map_radius", radius);
        material.SetShaderParameter("atlas_columns", BiomeTileMapper.AtlasColumns);
        material.SetShaderParameter("band_px", config.BandPx);
        material.SetShaderParameter("edge_noise", config.EdgeNoise);
        material.SetShaderParameter("noise_scale", config.NoiseScale);
        material.SetShaderParameter("grain", config.Grain);
        GD.Print($"[GroundMaterial] {size.X}×{size.Y} cellules décrites en {Time.GetTicksMsec() - started} ms");
    }

    /// <summary>
    /// Encode dans le canal A de <c>cell_map</c> la présence et l'ordre des identifiants de mélange voisins dans le
    /// rayon maximal de frontière (band_px + déformation) :
    /// 0 = aucun voisin différent ; 85 = voisins d'identifiant supérieur uniquement ; 170 = inférieur uniquement ;
    /// 255 = les deux. Permet au shader d'écarter immédiatement la moitié des pixels où le bruit de lisière avance du
    /// côté opposé (plan 53 V3).
    /// </summary>
    private static void MarkBorderCells(byte[] cells, Vector2I size)
    {
        for (int gy = 0; gy < size.Y; gy++)
        {
            int cx = gxCenter(0, gy);
            for (int gx = 0; gx < size.X; gx++, cx += 64)
            {
                int offset = (gy * size.X + gx) * 4;
                byte biome = cells[offset];
                if (biome == NoBlend)
                    continue;
                bool hasHigher = false;
                bool hasLower = false;
                for (int dy = -3; dy <= 3 && !(hasHigher && hasLower); dy++)
                {
                    int ny = gy + dy;
                    if (ny < 0 || ny >= size.Y)
                        continue;
                    int nShift = (ny & 1) * 32;
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        int nx = gx + dx;
                        if (nx < 0 || nx >= size.X)
                            continue;
                        int nxCenter = nx * 64 + nShift;
                        if (Mathf.Abs(nxCenter - cx) > 108)
                            continue;
                        byte other = cells[(ny * size.X + nx) * 4];
                        if (other == NoBlend || other == biome)
                            continue;
                        if (other > biome)
                            hasHigher = true;
                        else
                            hasLower = true;
                    }
                }
                cells[offset + 3] = (hasHigher, hasLower) switch
                {
                    (true, true) => 255,
                    (true, false) => 85,
                    (false, true) => 170,
                    _ => 0,
                };
            }
        }

        static int gxCenter(int gx, int gy) => gx * 64 + (gy & 1) * 32;
    }
}
