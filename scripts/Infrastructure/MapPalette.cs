using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Vestiges.World;

namespace Vestiges.Infrastructure;

/// <summary>Fond cartographique et teintes d'Effacement, issus de la charte dans data/ui/map.json (lus strictement, plan 26 Q7g-4a).</summary>
public sealed class MapPalette
{
    public const string FilePath = "res://data/ui/map.json";
    private const string FallbackBiome = "forest_reclaimed";
    private const int PhaseCount = 5;
    private static readonly int TerrainCount = Enum.GetValues<TerrainType>().Length;

    private static MapPalette _instance;
    private static string _loadError;
    private readonly Dictionary<string, Color[]> _biomes = new();
    private readonly Color[] _phaseTints = new Color[PhaseCount];
    private readonly float[] _phaseBlends = new float[PhaseCount];
    private MapPalette()
    {
    }

    public Color Road { get; private init; }
    public Color Path { get; private init; }
    /// <summary>La palette du jeu ; refusée, une exception qui nomme le champ (le bootstrap l'a déjà contrôlée).</summary>
    public static MapPalette Load() => TryLoad(out string error) ? _instance : throw new InvalidOperationException(error);

    /// <summary>Palette contrôlée une fois (plan 26 Q7g-4a) ; refusée, la raison, et la run ne démarre pas (GameBootstrap).</summary>
    public static bool TryLoad(out string error)
    {
        if (_instance == null && _loadError == null)
        {
            using FileAccess file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
            _loadError = file == null ? $"{FilePath} : fichier illisible"
                : TryParse(file.GetAsText(), id => BiomeDataLoader.Get(id) != null, out _instance, out string parseError) ? null
                : $"{FilePath} : {parseError}";
        }
        error = _loadError;
        return error == null;
    }

    /// <summary>
    /// Couleurs HTML : route, sentier, quatre teintes de terrain par biome connu (la forêt sert de secours, elle est
    /// obligatoire), cinq teintes et cinq mélanges de phase d'Effacement.
    /// </summary>
    public static bool TryParse(string text, Func<string, bool> biomeExists, out MapPalette palette, out string error)
    {
        palette = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement root = reader.Root;
            reader.AllowOnly(root, "carte", "road", "path", "biomes", "phase_tints", "phase_blends");
            MapPalette parsed = new() { Road = Html(reader, root, "road"), Path = Html(reader, root, "path") };
            foreach ((string biome, JsonElement values) in reader.Entries(reader.Section(root, "biomes"), "biomes"))
            {
                if (!biomeExists(biome))
                    reader.Fail($"biomes : biome « {biome} » inconnu");
                parsed._biomes[biome] = ColorList(reader, values, $"biomes.{biome}", TerrainCount);
            }
            if (reader.Error == null && !parsed._biomes.ContainsKey(FallbackBiome))
                reader.Fail($"biomes : {FallbackBiome} attendu (secours d'un biome sans teintes)");
            ColorList(reader, JsonConfigReader.Has(root, "phase_tints") ? root.GetProperty("phase_tints") : default, "phase_tints", PhaseCount)
                .CopyTo(parsed._phaseTints, 0);
            List<JsonElement> blends = reader.List(root, "phase_blends", PhaseCount);
            for (int i = 0; i < blends.Count && reader.Error == null; i++)
            {
                if (blends.Count != PhaseCount || blends[i].ValueKind != JsonValueKind.Number || blends[i].GetDouble() < 0.0 || blends[i].GetDouble() > 1.0)
                    reader.Fail($"phase_blends : {PhaseCount} parts de 0 à 1 attendues");
                else
                    parsed._phaseBlends[i] = (float)blends[i].GetDouble();
            }
            error = reader.Error;
            if (error != null)
                return false;
            palette = parsed;
            return true;
        }
        catch (JsonException exception)
        {
            error = $"JSON invalide : {exception.Message}";
            return false;
        }
    }

    private static Color[] ColorList(JsonConfigReader reader, JsonElement values, string where, int count)
    {
        Color[] colors = new Color[count];
        if (reader.Error != null)
            return colors;
        if (values.ValueKind != JsonValueKind.Array || values.GetArrayLength() != count)
        {
            reader.Fail($"{where} : {count} couleurs attendues");
            return colors;
        }
        for (int i = 0; i < count; i++)
        {
            string html = values[i].ValueKind == JsonValueKind.String ? values[i].GetString() : null;
            if (html == null || !Color.HtmlIsValid(html))
            {
                reader.Fail($"{where} : couleur « {html} » invalide");
                return colors;
            }
            colors[i] = Color.FromHtml(html);
        }
        return colors;
    }

    private static Color Html(JsonConfigReader reader, JsonElement owner, string key)
    {
        string html = reader.Text(owner, key);
        if (html != null && Color.HtmlIsValid(html))
            return Color.FromHtml(html);
        reader.Fail($"{key} : couleur « {html} » invalide");
        return default;
    }

    public Color Terrain(string biomeId, TerrainType terrain) =>
        _biomes.TryGetValue(biomeId ?? FallbackBiome, out Color[] colors) ? colors[(int)terrain] : _biomes[FallbackBiome][(int)terrain];

    public Color PhaseColor(int phase) => _phaseTints[Mathf.Clamp(phase, 0, PhaseCount - 1)];

    public Color ApplyPhase(Color terrain, int phase, int x, int y)
    {
        phase = Mathf.Clamp(phase, 0, PhaseCount - 1);
        Color color = terrain.Lerp(_phaseTints[phase], _phaseBlends[phase]);
        // Le Néant déjà découvert se distingue de l'inconnu par une hachure, même sans couleur.
        if (phase == 4 && ((x + y) & 3) == 0)
            color = color.Lightened(0.25f);
        return color with { A = terrain.A };
    }
}
