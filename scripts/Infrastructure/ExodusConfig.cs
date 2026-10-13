using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Exode dans les rues (plan 39 B3), lu depuis <c>data/world/exodus.json</c> et contrôlé en entier : fichier invalide,
/// la ville reste sans scènes ni semis de rue, avec un message.
/// </summary>
public sealed class ExodusConfig
{
    private const string ConfigPath = "res://data/world/exodus.json";

    public int RoadCellsPerScene { get; private init; }
    public float MinSpacingPx { get; private init; }
    public float JamShare { get; private init; }
    public int JamCarsMin { get; private init; }
    public int JamCarsMax { get; private init; }
    public float CarSpacingHorizontalPx { get; private init; }
    public float CarSpacingVerticalPx { get; private init; }
    public int LeftoversPerJamMin { get; private init; }
    public int LeftoversPerJamMax { get; private init; }
    public int LeftoversPerBarricadeMin { get; private init; }
    public int LeftoversPerBarricadeMax { get; private init; }
    public int QuietRadiusCells { get; private init; }
    /// <summary>Chance qu'une cellule de rue droite reçoive un décor semé hors scène, et part de voitures parmi eux.</summary>
    public float ScatterChance { get; private init; }
    public float ScatterCarShare { get; private init; }

    public static bool TryLoad(out ExodusConfig config, out string error)
    {
        using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            config = null;
            error = $"{ConfigPath} absent";
            return false;
        }
        if (!TryParse(file.GetAsText(), out config, out string parseError))
        {
            error = $"{ConfigPath} : {parseError}";
            return false;
        }
        error = null;
        return true;
    }

    public static bool TryParse(string json, out ExodusConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            JsonConfigReader reader = new(root);
            reader.AllowOnly(root, "racine", "road_cells_per_scene", "min_spacing_px", "jam_share", "jam_cars",
                "car_spacing_px", "leftovers_per_jam", "leftovers_per_barricade", "quiet_radius_cells", "street_scatter");
            int cellsPerScene = reader.Integer(root, "road_cells_per_scene", 40, 5000);
            float spacing = reader.Positive(root, "min_spacing_px");
            float jamShare = reader.Chance(root, "jam_share");
            (float carsMin, float carsMax) = reader.Range(root, "jam_cars", 2f, 6f);
            JsonElement carSpacing = reader.Section("car_spacing_px");
            reader.AllowOnly(carSpacing, "car_spacing_px", "horizontal", "vertical");
            float horizontal = reader.Positive(carSpacing, "horizontal");
            float vertical = reader.Positive(carSpacing, "vertical");
            (float jamLeftMin, float jamLeftMax) = reader.Range(root, "leftovers_per_jam", 0f, 6f);
            (float barLeftMin, float barLeftMax) = reader.Range(root, "leftovers_per_barricade", 0f, 4f);
            int quiet = reader.Integer(root, "quiet_radius_cells", 0, 8);
            JsonElement scatter = reader.Section("street_scatter");
            reader.AllowOnly(scatter, "street_scatter", "chance", "car_share");
            float scatterChance = reader.Chance(scatter, "chance");
            float carShare = reader.Chance(scatter, "car_share");
            // Des voitures et des bagages se comptent en entiers : 3,5 serait arrondi sans prévenir.
            foreach ((string key, float value) in new[] { ("jam_cars", carsMin), ("jam_cars", carsMax),
                         ("leftovers_per_jam", jamLeftMin), ("leftovers_per_jam", jamLeftMax),
                         ("leftovers_per_barricade", barLeftMin), ("leftovers_per_barricade", barLeftMax) })
            {
                if (value != Mathf.Floor(value))
                    reader.Fail($"{key} : nombres entiers attendus");
            }
            // Deux scènes plus proches qu'un écran se liraient comme une seule, et l'exode redeviendrait un semis.
            if (spacing < 600f)
                reader.Fail("min_spacing_px : au moins 600 attendu (deux scènes dans le même écran)");
            // Une voiture fait ~70 px de long sur une rue horizontale, ~40 px sur une verticale : plus serré, elles se chevauchent.
            if (horizontal < 60f || vertical < 36f)
                reader.Fail("car_spacing_px : au moins 60 (horizontal) et 36 (vertical) attendus (voitures qui se chevauchent)");
            error = reader.Error;
            if (error != null)
                return false;
            config = new ExodusConfig
            {
                RoadCellsPerScene = cellsPerScene, MinSpacingPx = spacing, JamShare = jamShare,
                JamCarsMin = Mathf.RoundToInt(carsMin), JamCarsMax = Mathf.RoundToInt(carsMax),
                CarSpacingHorizontalPx = horizontal, CarSpacingVerticalPx = vertical,
                LeftoversPerJamMin = Mathf.RoundToInt(jamLeftMin), LeftoversPerJamMax = Mathf.RoundToInt(jamLeftMax),
                LeftoversPerBarricadeMin = Mathf.RoundToInt(barLeftMin), LeftoversPerBarricadeMax = Mathf.RoundToInt(barLeftMax),
                QuietRadiusCells = quiet, ScatterChance = scatterChance, ScatterCarShare = carShare,
            };
            return true;
        }
        catch (JsonException ex)
        {
            error = $"JSON illisible : {ex.Message}";
            return false;
        }
    }
}
