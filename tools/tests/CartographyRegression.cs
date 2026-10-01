using System;
using System.Diagnostics;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.UI;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>Découverte, coordonnées négatives, recoloration et budget du raster sans lancer de fenêtre.</summary>
public partial class CartographyRegression : Node
{
    private int _checks;

    public override void _Ready()
    {
        try
        {
            CheckRaster();
            MeasureRaster();
            GD.Print($"[CartographyRegression] RESULT checks={_checks} failures=0");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError($"[CartographyRegression] {exception}");
            GetTree().Quit(1);
        }
    }

    private void CheckRaster()
    {
        int samples = 0;
        Vector2 first = Vector2.Zero, last = Vector2.Zero;
        using CartographyRaster raster = new(3, 2, 128, new Vector2I(-2, -1), point =>
        {
            if (samples++ == 0) first = point;
            last = point;
            return new Color((point.X + 256f) / 128f, (point.Y + 128f) / 128f, 0f);
        });
        Vector2I cell = new(-2, -1);
        Check(!raster.IsKnown(cell) && raster.Image.GetPixel(0, 0).A == 0f, "inconnu transparent");
        Check(!raster.Paint(cell, 2) && samples == 0, "le front ne découvre pas la carte");
        Check(!raster.Paint(new Vector2I(-3, -1), 0, true) && !raster.Paint(new Vector2I(1, 1), 0, true), "limites négatives et positives");
        Check(raster.Paint(cell, 0, true) && samples == 16, "seize relevés lors de la découverte");
        Check(first == new Vector2(-240, -112) && last == new Vector2(-144, -16), "centres des sous-cellules en coordonnées monde négatives");
        Check(raster.IsKnown(cell) && !raster.IsKnown(new Vector2I(-1, -1)), "pas de découverte voisine");
        Check(raster.Flush() && !raster.Flush(), "publication seulement si modifié");
        Color anchored = raster.Image.GetPixel(0, 0);
        Check(anchored.R < raster.Image.GetPixel(3, 0).R && anchored.G < raster.Image.GetPixel(0, 3).G, "détails de terrain conservés");
        Check(!raster.Paint(cell, 0, true) && samples == 16, "revisite sans nouveau relevé");
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) raster.Paint(cell, 0, true);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "mille revisites sans allocation gérée");
        Check(raster.Paint(cell, 2) && samples == 16, "recoloration sans lecture du terrain");
        raster.Flush();
        Check(raster.Image.GetPixel(0, 0) != anchored, "danger visible");
        raster.Paint(cell, 0);
        raster.Flush();
        Check(raster.Image.GetPixel(0, 0) == anchored, "pas de dérive des couleurs après recolorations");
        raster.Paint(cell, 4);
        raster.Flush();
        Check(raster.Image.GetPixel(0, 0) != raster.Image.GetPixel(1, 0) && raster.Image.GetPixel(0, 0).A > 0f, "Néant connu hachuré, distinct de l'inconnu");
        using CartographyRaster outside = new(1, 1, 128, Vector2I.Zero, _ => Colors.Transparent);
        outside.Paint(Vector2I.Zero, 4, true);
        outside.Flush();
        Check(outside.Image.GetPixel(0, 0).A == 0f, "extérieur du monde transparent même dans le Néant");
        MapPalette palette = MapPalette.Load();
        Check(palette.Terrain("urban_ruins", TerrainType.Concrete) != palette.Terrain("forest_reclaimed", TerrainType.Forest), "biomes distincts");
        Check(palette.Terrain(null, TerrainType.Grass) == palette.Terrain("forest_reclaimed", TerrainType.Grass), "monde sans biomes pris en charge");
    }

    private static void MeasureRaster()
    {
        // Même carte et même disque de découverte ; ce micro-banc CPU ne mesure ni les FPS ni le transfert GPU.
        const int width = 202, height = 102, repeats = 40;
        MapPalette palette = MapPalette.Load();
        Color ground = palette.Terrain("forest_reclaimed", TerrainType.Grass);
        double coarseMs = 0, detailedMs = 0;
        int samples = 0, bytes = 0;
        for (int pass = -3; pass < repeats; pass++)
        {
            using Image coarse = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
            Stopwatch watch = Stopwatch.StartNew();
            for (int x = -12; x <= 12; x++)
                for (int y = -12; y <= 12; y++)
                    if (x * x + y * y <= 144)
                        coarse.SetPixel(x + 50, y + 50, ground);
            watch.Stop();
            if (pass >= 0) coarseMs += watch.Elapsed.TotalMilliseconds;
            using CartographyRaster raster = new(width, height, 128, new Vector2I(-50, -50), _ => { samples++; return ground; });
            watch.Restart();
            for (int x = -12; x <= 12; x++)
                for (int y = -12; y <= 12; y++)
                    if (x * x + y * y <= 144)
                        raster.Paint(new Vector2I(x, y), 0, true);
            raster.Flush();
            watch.Stop();
            if (pass >= 0) detailedMs += watch.Elapsed.TotalMilliseconds;
            bytes = raster.BufferBytes;
        }
        GD.Print(FormattableString.Invariant($"[CartographyRegression] COST coarse_ms={coarseMs / repeats:F4} detailed_ms={detailedMs / repeats:F4} managed_bytes={bytes} raster_bytes={width * height * 64} samples_per_reveal={samples / (repeats + 3)}"));
    }

    private void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        _checks++;
        GD.Print($"[CartographyRegression] PASS {description}");
    }
}
