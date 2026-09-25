using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-map : répartition des biomes autour du point d'apparition, mesurée sur plusieurs seeds,
/// images de la grille de biomes et vue dézoomée de la vraie scène.
/// </summary>
public partial class RunObservation
{
    private static readonly int[] MeasuredRadii = { 10, 20, 40, 80 };

    private static readonly Dictionary<string, Color> BiomeColors = new()
    {
        { "forest_reclaimed", new Color("3f7a3a") },
        { "urban_ruins", new Color("8a8a92") },
        { "swamp", new Color("2f6b6b") },
        { "wild_fields", new Color("c9b458") },
        { "collapsed_quarry", new Color("9a6a44") },
    };

    /// <summary>Statistiques pures (sans scène) : un générateur par seed.</summary>
    private static void AccumulateTerrain(WorldGenerator generator, int radius, Dictionary<string, int[]> byBiome,
                                          long[] diagonalSame, ref long diagonalPairs)
    {
        int terrainCount = Enum.GetValues<TerrainType>().Length;
        for (int y = -radius; y < radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y > radius * radius * 0.8f)
                    continue;
                TerrainType here = generator.GetTerrain(x, y);
                string biome = generator.GetBiome(x, y)?.Id ?? "?";
                if (!byBiome.TryGetValue(biome, out int[] counts))
                    byBiome[biome] = counts = new int[terrainCount];
                counts[(int)here]++;
                bool odd = (y & 1) != 0;
                diagonalSame[0] += generator.GetTerrain(x + (odd ? 1 : 0), y + 1) == here ? 1 : 0;
                diagonalSame[1] += generator.GetTerrain(x + (odd ? 0 : -1), y + 1) == here ? 1 : 0;
                diagonalPairs++;
            }
        }
    }

    private void MeasureBiomeLayout(ulong firstSeed, int seedCount, int imageCount)
    {
        BiomeDataLoader.Load();
        WorldGenConfig config = WorldGenConfig.Load();
        List<BiomeData> biomes = new();
        foreach (string id in config.AvailableBiomes)
            biomes.Add(BiomeDataLoader.Get(id) ?? throw new InvalidOperationException($"Biome inconnu dans world_gen.json : {id}"));

        double[] dominantShare = new double[MeasuredRadii.Length];
        double[] distinctCount = new double[MeasuredRadii.Length];
        double firstBorderSum = 0;
        int firstBorderMax = 0;
        StringBuilder perSeed = new();
        ulong generationUsec = 0;
        int regionSum = 0;
        Dictionary<string, int[]> terrainByBiome = new();
        // Stries : même terrain que le voisin ↘ contre le voisin ↙ (grille stacked). Isotrope → deux valeurs proches.
        long[] diagonalSame = new long[2];
        long diagonalPairs = 0;

        for (int index = 0; index < seedCount; index++)
        {
            ulong seed = firstSeed + (ulong)index;
            WorldGenerator generator = new(config.MapRadius, config.SpawnClearance, config.CaIterations, config.Zones, seed, config.EdgeFadeWidth)
            {
                BiomeLayout = config.BiomeLayout
            };
            ulong started = Time.GetTicksUsec();
            generator.Generate(biomes, config.BiomeCount);
            generationUsec += Time.GetTicksUsec() - started;
            regionSum += generator.BiomeRegionCount;

            for (int r = 0; r < MeasuredRadii.Length; r++)
            {
                (float share, int distinct) = MeasureDisc(generator, MeasuredRadii[r]);
                dominantShare[r] += share;
                distinctCount[r] += distinct;
            }
            int border = DistanceToOtherBiome(generator);
            firstBorderSum += border;
            firstBorderMax = Mathf.Max(firstBorderMax, border);
            perSeed.Append(CultureInfo.InvariantCulture, $" {seed}:{border}");

            AccumulateTerrain(generator, config.MapRadius, terrainByBiome, diagonalSame, ref diagonalPairs);
            if (index < imageCount)
                SaveBiomeImage(generator, $"{_output}/biomes-{seed}.png");
        }

        StringBuilder result = new("[RunObservation] RESULT map");
        result.Append(CultureInfo.InvariantCulture, $" seeds={seedCount} radius={config.MapRadius}");
        for (int r = 0; r < MeasuredRadii.Length; r++)
        {
            result.Append(CultureInfo.InvariantCulture,
                $" r{MeasuredRadii[r]}:dominant={dominantShare[r] / seedCount:P0},biomes={distinctCount[r] / seedCount:0.0}");
        }
        result.Append(CultureInfo.InvariantCulture, $" regions={(double)regionSum / seedCount:0} generate_ms={generationUsec / 1000.0 / seedCount:0}");
        result.Append(CultureInfo.InvariantCulture, $" first_border_mean={firstBorderSum / seedCount:0.0} max={firstBorderMax}");
        GD.Print(result.ToString());
        StringBuilder terrain = new("[RunObservation] RESULT terrain");
        terrain.Append(CultureInfo.InvariantCulture,
            $" diag_same_se={(double)diagonalSame[0] / diagonalPairs:P1} diag_same_sw={(double)diagonalSame[1] / diagonalPairs:P1}");
        foreach ((string biome, int[] counts) in terrainByBiome)
        {
            int total = 0;
            foreach (int count in counts)
                total += count;
            terrain.Append(CultureInfo.InvariantCulture, $" {biome}:");
            for (int t = 0; t < counts.Length; t++)
                terrain.Append(CultureInfo.InvariantCulture, $"{(TerrainType)t}={(double)counts[t] / total:P0},");
        }
        GD.Print(terrain.ToString());
        GD.Print($"[RunObservation] first_border per seed:{perSeed}");
    }

    /// <summary>Part du biome majoritaire et nombre de biomes présents (≥ 3 % des cellules) dans un disque autour du spawn.</summary>
    private static (float share, int distinct) MeasureDisc(WorldGenerator generator, int radius)
    {
        Dictionary<int, int> counts = new();
        int total = 0;
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x * x + y * y > radius * radius || generator.IsErased(x, y))
                    continue;
                int biome = generator.GetBiomeIndex(x, y);
                counts[biome] = counts.GetValueOrDefault(biome) + 1;
                total++;
            }
        }
        int best = 0;
        int distinct = 0;
        foreach (int count in counts.Values)
        {
            best = Mathf.Max(best, count);
            if (count >= total * 0.03f)
                distinct++;
        }
        return (total == 0 ? 0f : (float)best / total, distinct);
    }

    /// <summary>Distance (en cellules) du spawn à la première cellule d'un autre biome.</summary>
    private static int DistanceToOtherBiome(WorldGenerator generator)
    {
        int origin = generator.GetBiomeIndex(0, 0);
        for (int radius = 1; radius <= generator.MapRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius || generator.IsErased(x, y))
                        continue;
                    if (generator.GetBiomeIndex(x, y) != origin)
                        return radius;
                }
            }
        }
        return generator.MapRadius;
    }

    private static void SaveBiomeImage(WorldGenerator generator, string path)
    {
        int radius = generator.MapRadius;
        int size = radius * 2 + 1;
        using Image image = Image.CreateEmpty(size, size, false, Image.Format.Rgb8);
        image.Fill(Colors.Black);
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (generator.IsErased(x, y))
                    continue;
                Color color = BiomeColors.GetValueOrDefault(generator.GetBiomeId(x, y), Colors.Magenta);
                if (generator.GetTerrain(x, y) == TerrainType.Water)
                    color = color.Darkened(0.45f);
                image.SetPixel(x + radius, y + radius, color);
            }
        }
        // Repères : 20 et 40 cellules autour du spawn (≈ un et deux écrans).
        foreach (int ring in new[] { 20, 40 })
        {
            for (int step = 0; step < 360; step++)
            {
                float angle = Mathf.DegToRad(step);
                image.SetPixel(radius + Mathf.RoundToInt(Mathf.Cos(angle) * ring), radius + Mathf.RoundToInt(Mathf.Sin(angle) * ring), Colors.White);
            }
        }
        image.Resize(size * 3, size * 3, Image.Interpolation.Nearest);
        image.SavePng(path);
    }

    /// <summary>Vue dézoomée de la scène réelle autour du spawn (sol, props, biomes rendus).</summary>
    private async Task CaptureWorldOverview()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        _player.AIInputOverride = Vector2.Zero;
        Node2D fog = _world.GetNodeOrNull<Node2D>("FogOfWar");
        if (fog != null)
            fog.Visible = false;
        Vector2 initialZoom = _camera.Zoom;
        // L'écran de chargement s'efface après l'initialisation du monde.
        await Frames(90);
        float[] zooms = { 2f, 0.5f, 0.25f };
        for (int index = 0; index < zooms.Length; index++)
        {
            _camera.Zoom = new Vector2(zooms[index], zooms[index]);
            await Frames(30);
            using Image image = GetViewport().GetTexture().GetImage();
            image.SavePng($"{_output}/overview-zoom{zooms[index].ToString("0.##", CultureInfo.InvariantCulture)}.png");
        }
        _camera.Zoom = initialZoom;
        GD.Print($"[RunObservation] Vues d'ensemble écrites dans {_output}");
    }

    /// <summary>
    /// --capture-props : pour chaque biome, place le joueur au cœur de la zone la plus chargée en décors
    /// et capture l'écran, formes de collision visibles (le drapeau de debug est posé avant le chargement).
    /// </summary>
    /// <summary>Par biome : le point le plus chargé en décors (le décor qui a le plus de voisins à moins de 240 px).</summary>
    private List<(string Biome, Vector2 Point, int Neighbours, int Total)> FindPropHotspots()
    {
        Dictionary<string, List<Vector2>> propsByBiome = new();
        foreach (Node child in _world.GetNode("PropContainer").GetChildren())
        {
            if (child is not EnvironmentProp prop)
                continue;
            string biome = _world.GetBiomeAt(prop.GlobalPosition)?.Id ?? "?";
            if (!propsByBiome.TryGetValue(biome, out List<Vector2> list))
                propsByBiome[biome] = list = new List<Vector2>();
            list.Add(prop.GlobalPosition);
        }

        List<(string, Vector2, int, int)> result = new();
        foreach ((string biome, List<Vector2> positions) in propsByBiome)
        {
            Vector2 best = positions[0];
            int bestCount = -1;
            // Échantillon borné : le décompte des voisins est quadratique.
            int step = Mathf.Max(1, positions.Count / 400);
            for (int i = 0; i < positions.Count; i += step)
            {
                int count = 0;
                foreach (Vector2 other in positions)
                    if (positions[i].DistanceSquaredTo(other) < 240f * 240f)
                        count++;
                if (count > bestCount)
                {
                    bestCount = count;
                    best = positions[i];
                }
            }
            result.Add((biome, best, bestCount, positions.Count));
        }
        return result;
    }

    private async Task CapturePropHotspots()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Vestiges.Combat.Enemy existing && existing.IsActive)
                _world.GetNode<Vestiges.Spawn.EnemyPool>("EnemyPool").Return(existing);
        Node2D fog = _world.GetNodeOrNull<Node2D>("FogOfWar");
        if (fog != null)
            fog.Visible = false;
        await Frames(90);

        _player.AIInputOverride = Vector2.Zero;
        foreach ((string biome, Vector2 best, int bestCount, int total) in FindPropHotspots())
        {
            _player.GlobalPosition = best + new Vector2(0f, 24f);
            _camera.ResetSmoothing();
            await Frames(20);
            using Image image = GetViewport().GetTexture().GetImage();
            image.SavePng($"{_output}/props-{biome}.png");
            GD.Print($"[RunObservation] props {biome}: {total} décors, {bestCount} autour du point capturé {best}");

            // Joueur juste derrière le décor le plus haut de la zone : tri en profondeur et transparence.
            EnvironmentProp tallest = null;
            foreach (Node child in _world.GetNode("PropContainer").GetChildren())
            {
                if (child is EnvironmentProp prop && prop.GlobalPosition.DistanceSquaredTo(best) < 240f * 240f
                    && (tallest == null || prop.Footprint.VisibleHeight > tallest.Footprint.VisibleHeight))
                    tallest = prop;
            }
            if (tallest == null)
                continue;
            _player.GlobalPosition = tallest.GlobalPosition + new Vector2(0f, -Mathf.Min(24f, tallest.Footprint.VisibleHeight * 0.4f));
            _camera.ResetSmoothing();
            await Frames(20);
            using Image behind = GetViewport().GetTexture().GetImage();
            behind.SavePng($"{_output}/props-{biome}-behind.png");
        }
    }
}
