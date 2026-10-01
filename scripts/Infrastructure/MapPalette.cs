using System.Collections.Generic;
using Godot;
using Vestiges.World;

namespace Vestiges.Infrastructure;

/// <summary>Fond cartographique et teintes d'Effacement, issus de la charte dans data/ui/map.json.</summary>
public sealed class MapPalette
{
    private static MapPalette _instance;
    private readonly Dictionary<string, Color[]> _biomes = new();
    private readonly Color[] _phaseTints = new Color[5];
    private readonly float[] _phaseBlends = new float[5];
    public Color Road { get; private set; }
    public Color Path { get; private set; }
    public static MapPalette Load() => _instance ??= new MapPalette();

    private MapPalette()
    {
        using FileAccess file = FileAccess.Open("res://data/ui/map.json", FileAccess.ModeFlags.Read);
        using Json json = new();
        if (file == null || json.Parse(file.GetAsText()) != Error.Ok)
            throw new System.InvalidOperationException("Palette cartographique invalide.");
        Godot.Collections.Dictionary data = json.Data.AsGodotDictionary();
        Road = Color.FromHtml(data["road"].AsString());
        Path = Color.FromHtml(data["path"].AsString());
        foreach (KeyValuePair<Variant, Variant> entry in data["biomes"].AsGodotDictionary())
        {
            Godot.Collections.Array values = entry.Value.AsGodotArray();
            Color[] colors = new Color[4];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = Color.FromHtml(values[i].AsString());
            _biomes[entry.Key.AsString()] = colors;
        }
        for (int i = 0; i < 5; i++)
        {
            _phaseTints[i] = Color.FromHtml(data["phase_tints"].AsGodotArray()[i].AsString());
            _phaseBlends[i] = (float)data["phase_blends"].AsGodotArray()[i].AsDouble();
        }
    }

    public Color Terrain(string biomeId, TerrainType terrain) =>
        _biomes.TryGetValue(biomeId ?? "forest_reclaimed", out Color[] colors) ? colors[(int)terrain] : _biomes["forest_reclaimed"][(int)terrain];

    public Color PhaseColor(int phase) => _phaseTints[Mathf.Clamp(phase, 0, 4)];

    public Color ApplyPhase(Color terrain, int phase, int x, int y)
    {
        phase = Mathf.Clamp(phase, 0, 4);
        Color color = terrain.Lerp(_phaseTints[phase], _phaseBlends[phase]);
        // Le Néant déjà découvert se distingue de l'inconnu par une hachure, même sans couleur.
        if (phase == 4 && ((x + y) & 3) == 0)
            color = color.Lightened(0.25f);
        return color with { A = terrain.A };
    }
}
