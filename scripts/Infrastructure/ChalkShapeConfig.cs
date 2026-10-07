using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Vestiges.Combat;

namespace Vestiges.Infrastructure;

/// <summary>
/// Formes des Craies (plan 27 V2b), lues depuis <c>data/fx/chalk_shapes.json</c> et contrôlées en entier : une forme
/// sans dessin connu ou une famille inconnue est refusée avec le nom du champ, et les Craies restent sans forme.
/// </summary>
public sealed class ChalkShapeConfig
{
    private const string ConfigPath = "res://data/fx/chalk_shapes.json";

    /// <summary>Formes dont le tracé existe (ChalkDrawing).</summary>
    public static readonly string[] KnownShapes = { "star", "circle", "house", "sun" };

    public float DrawSeconds { get; private init; }
    public int DrawPoses { get; private init; }
    public float HoldSeconds { get; private init; }
    public float FadeSeconds { get; private init; }
    public float GapChance { get; private init; }
    public IReadOnlyList<(string Shape, FxFamily Family)> Shapes { get; private init; }

    public static bool TryLoad(out ChalkShapeConfig config, out string error)
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

    public static bool TryParse(string json, out ChalkShapeConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement root = document.RootElement;
            List<(string, FxFamily)> shapes = new();
            if (reader.Error == null)
            {
                foreach ((string shape, JsonElement value) in reader.Entries(reader.Section("shapes"), "shapes"))
                {
                    if (!JsonConfigReader.Contains(KnownShapes, shape))
                    {
                        reader.Fail($"shapes : forme « {shape} » sans tracé ({string.Join(", ", KnownShapes)})");
                        break;
                    }
                    string name = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
                    FxFamily family = PixelPalette.ParseFamily(name ?? "", (FxFamily)(-1));
                    if ((int)family < 0)
                    {
                        reader.Fail($"shapes.{shape} : famille « {name} » inconnue");
                        break;
                    }
                    shapes.Add((shape, family));
                }
                if (reader.Error == null && shapes.Count == 0)
                    reader.Fail("shapes : au moins une forme attendue");
            }
            ChalkShapeConfig parsed = new()
            {
                DrawSeconds = reader.Positive(root, "draw_seconds"),
                DrawPoses = reader.Count(root, "draw_poses", 8),
                HoldSeconds = reader.NonNegative(root, "hold_seconds"),
                FadeSeconds = reader.Positive(root, "fade_seconds"),
                GapChance = reader.Number(root, "gap_chance"),
                Shapes = shapes,
            };
            if (parsed.GapChance is < 0f or > 0.6f)
                reader.Fail("gap_chance : de 0 à 0,6 attendu");
            error = reader.Error;
            config = error == null ? parsed : null;
            return error == null;
        }
        catch (JsonException ex)
        {
            error = $"JSON illisible : {ex.Message}";
            return false;
        }
    }
}
