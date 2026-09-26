using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>Réglages des jonctions entre biomes (<c>ground_blend</c> dans world_gen.json).</summary>
public struct GroundBlendConfig
{
    public bool Enabled;
    public float BandPx;
    public float EdgeNoise;
    public float NoiseScale;

    public static GroundBlendConfig Default => new()
    {
        Enabled = true,
        BandPx = 34f,
        EdgeNoise = 0.35f,
        NoiseScale = 0.045f,
    };
}

/// <summary>
/// Matériau du sol de la run (assets/shaders/ground.gdshader) : jonctions entre biomes et oubli du sol.
/// Pour les jonctions, décrit chaque cellule (biome, tuile posée) dans une petite texture et rassemble les tuiles
/// utilisées dans un atlas ; le shader mélange les matières par tramage le long des frontières. Construit une fois,
/// après la pose du terrain : aucun coût par frame côté CPU. L'oubli lit la mémoire publiée par ErasureManager.
/// </summary>
public static class GroundMaterial
{
    private const string ShaderPath = "res://assets/shaders/ground.gdshader";
    private const int AtlasColumns = 16;
    private const int TileWidth = 64;
    private const int TileHeight = 32;
    private const byte NoBlend = 255;

    public static void Apply(TileMapLayer ground, TileMapLayer roads, WorldGenerator generator, BiomeTileMapper tileMapper,
                             TerrainType[,] terrain, int radius, GroundBlendConfig config)
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
        int size = radius * 2 + 1;
        byte[] cells = new byte[size * size * 4];
        Dictionary<int, int> atlasIndexBySource = new();
        List<int> sources = new();

        for (int gy = 0; gy < size; gy++)
        {
            for (int gx = 0; gx < size; gx++)
            {
                int offset = (gy * size + gx) * 4;
                cells[offset] = NoBlend;

                int x = gx - radius;
                int y = gy - radius;
                if (!generator.IsWithinBounds(x, y) || generator.IsErased(x, y) || terrain[gx, gy] == TerrainType.Water)
                    continue;

                int sourceId = ground.GetCellSourceId(new Vector2I(x, y));
                int biome = generator.GetBiomeIndex(x, y);
                if (sourceId < 0 || biome < 0 || biome >= NoBlend / BiomeTileMapper.MaxMaterialsPerBiome)
                    continue;
                // Identifiant de mélange : le biome, ou la matière de la tuile quand le biome fond ses matières entre elles.
                int blendId = biome * BiomeTileMapper.MaxMaterialsPerBiome;
                if (generator.GetBiome(x, y)?.BlendTerrains == true)
                    blendId += tileMapper.GetMaterialOfSource(sourceId);

                if (!atlasIndexBySource.TryGetValue(sourceId, out int atlasIndex))
                {
                    atlasIndex = sources.Count;
                    atlasIndexBySource[sourceId] = atlasIndex;
                    sources.Add(sourceId);
                }
                cells[offset] = (byte)blendId;
                cells[offset + 1] = (byte)(atlasIndex & 0xFF);
                cells[offset + 2] = (byte)(atlasIndex >> 8);
            }
        }

        MarkBorderCells(cells, size);
        Image atlas = BuildAtlas(ground.TileSet, sources);
        if (atlas == null)
            return;

        material.SetShaderParameter("blend_enabled", true);
        material.SetShaderParameter("cell_map", ImageTexture.CreateFromImage(Image.CreateFromData(size, size, false, Image.Format.Rgba8, cells)));
        material.SetShaderParameter("tile_atlas", ImageTexture.CreateFromImage(atlas));
        material.SetShaderParameter("map_radius", radius);
        material.SetShaderParameter("atlas_columns", AtlasColumns);
        material.SetShaderParameter("band_px", config.BandPx);
        material.SetShaderParameter("edge_noise", config.EdgeNoise);
        material.SetShaderParameter("noise_scale", config.NoiseScale);
        GD.Print($"[GroundMaterial] {sources.Count} tuiles dans l'atlas, {size}×{size} cellules, construit en {Time.GetTicksMsec() - started} ms");
    }

    /// <summary>
    /// Alpha = 255 pour les cellules à moins de deux cases d'un autre biome : le shader ne cherche la frontière
    /// que là, les autres pixels sortent après une seule lecture. En grille « stacked », deux cases de distance
    /// valent ±2 colonnes et ±4 rangs.
    /// </summary>
    private static void MarkBorderCells(byte[] cells, int size)
    {
        for (int gy = 0; gy < size; gy++)
        {
            for (int gx = 0; gx < size; gx++)
            {
                int offset = (gy * size + gx) * 4;
                byte biome = cells[offset];
                if (biome == NoBlend)
                    continue;
                bool border = false;
                for (int dy = -4; dy <= 4 && !border; dy++)
                {
                    int ny = gy + dy;
                    if (ny < 0 || ny >= size)
                        continue;
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        int nx = gx + dx;
                        if (nx < 0 || nx >= size)
                            continue;
                        byte other = cells[(ny * size + nx) * 4];
                        if (other != NoBlend && other != biome)
                        {
                            border = true;
                            break;
                        }
                    }
                }
                cells[offset + 3] = border ? (byte)255 : (byte)0;
            }
        }
    }

    private static Image BuildAtlas(TileSet tileSet, List<int> sources)
    {
        if (sources.Count == 0)
            return null;

        int rows = (sources.Count + AtlasColumns - 1) / AtlasColumns;
        Image atlas = Image.CreateEmpty(AtlasColumns * TileWidth, rows * TileHeight, false, Image.Format.Rgba8);
        Rect2I tileRect = new(0, 0, TileWidth, TileHeight);
        for (int i = 0; i < sources.Count; i++)
        {
            if (tileSet.GetSource(sources[i]) is not TileSetAtlasSource source || source.Texture == null)
                continue;
            Image image = source.Texture.GetImage();
            if (image == null)
                continue;
            if (image.IsCompressed())
                image.Decompress();
            image.Convert(Image.Format.Rgba8);
            atlas.BlitRect(image, tileRect, new Vector2I(i % AtlasColumns * TileWidth, i / AtlasColumns * TileHeight));
        }
        return atlas;
    }
}
