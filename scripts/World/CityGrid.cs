using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Réglage de la grille de la Haute-Ville (plan 58 T3a), clé « city » de <c>data/props/urban_ruins.json</c>. Écarts
/// variés entre les rues, décrochements aux croisements, tronçons supprimés (îlots fusionnés), ruelles en impasse, rues
/// effondrées en cratère, avenues plantées et places ouvertes. Les décors s'y nomment par leur identifiant du fichier.
/// </summary>
public sealed class CityGrid
{
    /// <summary>Un décor tiré au poids : ses images et s'il bloque, repris de sa fiche du fichier.</summary>
    public sealed class Dressing
    {
        public string Sprite;
        public string Canopy;
        public float CanopyOffsetY;
        public bool Blocking;
        public float Weight;

        public static Dressing Of(PropDefinition prop, float weight) => new()
        {
            Sprite = prop.SpriteBasePath,
            Canopy = prop.SpriteCanopyPath,
            CanopyOffsetY = prop.CanopyOffsetY,
            Blocking = prop.Blocking,
            Weight = weight,
        };
    }

    public Vector2I RowSpacing;
    public Vector2I ColumnSpacing;
    public float JogChance;
    public int JogRows;
    public float MergeChance;
    public float DeadEndChance;
    public float CraterChance;
    public Vector2I CraterLength;
    public readonly List<Dressing> CraterProps = new();
    public float AvenueChance;
    public float AvenueTreeSpacingPx;
    public readonly List<Dressing> AvenueTrees = new();
    public float SquareChance;
    public int SquareMinCells;
    public float SquareSpacingPx;
    public readonly List<Dressing> SquareTrees = new();
    /// <summary>Pas du quinconce d'une place : en colonnes (X) et en rangs (Y).</summary>
    public Vector2I SquareTreeEvery;
    public float SquareDensity;
    public readonly List<Dressing> SquareProps = new();
    public Vector2I DiagonalCount;
    public float StarRadiusPx;
    public readonly List<Dressing> StarCenterProps = new();
    public float ParkChance;
    public int ParkMinCells;
    public float ParkSpacingPx;
    public float ParkPondChance;
    public readonly List<Dressing> ParkTrees = new();
    public readonly List<Dressing> ParkProps = new();

    public static CityGrid Parse(JsonConfigReader reader, JsonElement element, IReadOnlyDictionary<string, PropDefinition> props)
    {
        reader.AllowOnly(element, "city", "row_spacing", "column_spacing", "jogs", "merge_chance", "dead_end_chance", "craters",
            "avenues", "squares", "diagonals", "parks");
        CityGrid city = new()
        {
            RowSpacing = Range(reader, element, "row_spacing", 6, 40),
            ColumnSpacing = Range(reader, element, "column_spacing", 5, 30),
            MergeChance = reader.Between(element, "merge_chance", 0f, 0.4f),
            DeadEndChance = reader.Between(element, "dead_end_chance", 0f, 0.5f),
        };

        JsonElement jogs = reader.Section(element, "jogs");
        reader.AllowOnly(jogs, "city.jogs", "chance", "rows");
        city.JogChance = reader.Chance(jogs, "chance");
        city.JogRows = reader.Integer(jogs, "rows", 1, 4);
        // Un décrochement plus ample que le quart de l'écart le plus serré collerait deux rues l'une à l'autre.
        if (reader.Error == null && city.JogRows * 4 > city.RowSpacing.X)
            reader.Fail($"city.jogs : {city.JogRows} rangs de décrochement pour un écart de {city.RowSpacing.X} rangs, le quart au plus");

        JsonElement craters = reader.Section(element, "craters");
        reader.AllowOnly(craters, "city.craters", "chance", "length", "weights");
        city.CraterChance = reader.Between(craters, "chance", 0f, 0.5f);
        city.CraterLength = Range(reader, craters, "length", 1, 6);
        Weights(reader, craters, "weights", "city.craters", props, city.CraterProps);

        JsonElement avenues = reader.Section(element, "avenues");
        reader.AllowOnly(avenues, "city.avenues", "chance", "trees", "tree_spacing_px");
        city.AvenueChance = reader.Between(avenues, "chance", 0f, 0.6f);
        Weights(reader, avenues, "trees", "city.avenues", props, city.AvenueTrees);
        city.AvenueTreeSpacingPx = reader.Between(avenues, "tree_spacing_px", 64f, 1024f);

        JsonElement squares = reader.Section(element, "squares");
        reader.AllowOnly(squares, "city.squares", "chance", "min_cells", "spacing_px", "trees", "tree_every", "density", "weights");
        city.SquareChance = reader.Between(squares, "chance", 0f, 0.4f);
        city.SquareMinCells = reader.Integer(squares, "min_cells", 16, 4096);
        city.SquareSpacingPx = reader.Between(squares, "spacing_px", 256f, 8192f);
        Weights(reader, squares, "trees", "city.squares", props, city.SquareTrees);
        JsonElement every = reader.Section(squares, "tree_every");
        reader.AllowOnly(every, "city.squares.tree_every", "columns", "rows");
        city.SquareTreeEvery = new Vector2I(reader.Integer(every, "columns", 2, 16), reader.Integer(every, "rows", 2, 16));
        city.SquareDensity = reader.Chance(squares, "density");
        Weights(reader, squares, "weights", "city.squares", props, city.SquareProps);

        JsonElement diagonals = reader.Section(element, "diagonals");
        reader.AllowOnly(diagonals, "city.diagonals", "count", "star_radius_px", "star_center");
        city.DiagonalCount = Range(reader, diagonals, "count", 1, 4);
        city.StarRadiusPx = reader.Between(diagonals, "star_radius_px", 128f, 384f);
        Weights(reader, diagonals, "star_center", "city.diagonals", props, city.StarCenterProps);

        JsonElement parks = reader.Section(element, "parks");
        reader.AllowOnly(parks, "city.parks", "chance", "min_cells", "spacing_px", "pond_chance", "trees", "weights");
        city.ParkChance = reader.Between(parks, "chance", 0f, 0.4f);
        city.ParkMinCells = reader.Integer(parks, "min_cells", 16, 4096);
        city.ParkSpacingPx = reader.Between(parks, "spacing_px", 256f, 8192f);
        city.ParkPondChance = reader.Chance(parks, "pond_chance");
        Weights(reader, parks, "trees", "city.parks", props, city.ParkTrees);
        Weights(reader, parks, "weights", "city.parks", props, city.ParkProps);
        return city;
    }

    /// <summary>Tire un décor au poids d'après un hachage ; nul si la liste est vide.</summary>
    public static Dressing Pick(List<Dressing> dressings, uint hash)
    {
        float total = 0f;
        foreach (Dressing dressing in dressings)
            total += dressing.Weight;
        if (total <= 0f)
            return null;
        float roll = (hash % 10000u) / 10000f * total;
        foreach (Dressing dressing in dressings)
        {
            roll -= dressing.Weight;
            if (roll < 0f)
                return dressing;
        }
        return dressings[^1];
    }

    /// <summary>Paire { "min", "max" } d'entiers bornés, min ≤ max.</summary>
    private static Vector2I Range(JsonConfigReader reader, JsonElement owner, string key, int low, int high)
    {
        JsonElement range = reader.Section(owner, key);
        reader.AllowOnly(range, $"city.{key}", "min", "max");
        Vector2I value = new(reader.Integer(range, "min", low, high), reader.Integer(range, "max", low, high));
        if (reader.Error == null && value.X > value.Y)
            reader.Fail($"city.{key} : min {value.X} au-dessus de max {value.Y}");
        return value;
    }

    /// <summary>Décors tirés au poids, { identifiant : poids }, sous la clé <paramref name="key"/>.</summary>
    private static void Weights(JsonConfigReader reader, JsonElement owner, string key, string where,
        IReadOnlyDictionary<string, PropDefinition> props, List<Dressing> into)
    {
        JsonElement weights = reader.Section(owner, key);
        foreach ((string id, JsonElement _) in reader.Entries(weights, $"{where}.{key}"))
        {
            if (!props.TryGetValue(id, out PropDefinition prop))
            {
                reader.Fail($"{where} : décor « {id} » absent de props");
                return;
            }
            into.Add(Dressing.Of(prop, reader.Positive(weights, id)));
        }
        if (reader.Error == null && into.Count == 0)
            reader.Fail($"{where}.{key} : au moins un décor attendu");
    }
}
