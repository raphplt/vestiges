using System.Collections.Generic;
using Godot;

namespace Vestiges.Tests;

/// <summary>
/// Les décors d'une terre pour le bac à sable low poly (plan 60 §19, lot C4c) : la table de semis du jeu en 2D
/// (<c>data/props/&lt;terre&gt;.json</c> : densité par cellule, poids, décors bloquants) appliquée aux modèles de la
/// bibliothèque de l'export de masse (<c>build/models</c>, convertie par <c>tools/convert_models.sh</c>). Un décor
/// plat, sorti sans triangle de l'export (flaque, marelle), est écarté : il deviendra un décalque (C5). Les zones
/// rythmées de la terre (clairières, fourrés) et les décors posés par d'autres systèmes (immeubles, fermes) n'y sont
/// pas. L'emprise d'un décor bloquant est sa boîte réduite, en attendant les emprises du lot C2.
/// </summary>
public sealed class ReliefBiomeProps
{
    // La part de la boîte d'un modèle qui bloque : une canopée ou un toit débordent de ce qui touche le sol.
    private const float FootprintShare = 0.6f;

    private readonly List<(ReliefLowPoly.Model Model, string Path, float Weight, bool Blocking)> _entries = new();
    private readonly float _density;
    private float _totalWeight;

    public string Biome { get; }
    public int Skipped { get; private set; }

    public ReliefBiomeProps(string biome, string libraryRoot)
    {
        Biome = biome;
        Dictionary<string, Godot.Collections.Dictionary> library = ReadIndex(libraryRoot);
        using FileAccess file = FileAccess.Open($"res://data/props/{biome}.json", FileAccess.ModeFlags.Read)
            ?? throw new System.IO.FileNotFoundException($"Terre inconnue : data/props/{biome}.json");
        Godot.Collections.Dictionary table = Json.ParseString(file.GetAsText()).AsGodotDictionary();
        _density = (float)table["density"].AsDouble();
        Dictionary<string, ReliefLowPoly.Model> loaded = new();
        foreach (Variant value in table["props"].AsGodotArray())
        {
            Godot.Collections.Dictionary prop = value.AsGodotDictionary();
            string id = prop["id"].AsString();
            string sprite = prop.TryGetValue("sprite_base", out Variant path) ? path.AsString().GetFile().GetBaseName().TrimSuffix("_base") : id;
            Godot.Collections.Dictionary entry = Find(library, $"prop_{id}", id, sprite);
            if (entry == null || entry.ContainsKey("error") || entry["max_triangles"].AsInt32() == 0)
            {
                Skipped++;
                continue;
            }
            string model = entry["path"].AsString().GetBaseName();
            float weight = (float)prop["weight"].AsDouble();
            if (!loaded.TryGetValue(model, out ReliefLowPoly.Model mesh))
                loaded[model] = mesh = ReliefLowPoly.LoadModelAt($"{libraryRoot}/{model}", true);
            _entries.Add((mesh, model, weight, prop["blocking"].AsBool()));
            _totalWeight += weight;
        }
        if (_entries.Count == 0)
            throw new System.InvalidOperationException($"Aucun modèle de {biome} dans {libraryRoot} : python3 tools/export_lowpoly.py --all");
        GD.Print($"[ReliefBiomeProps] {biome} : {_entries.Count} modèles, {Skipped} écartés (plats ou absents), densité {_density}");
    }

    /// <summary>
    /// Sème les décors sur les cellules plates, comme le jeu en 2D : une chance <c>density</c> par cellule, puis un décor
    /// tiré selon les poids. Un décor bloquant tourne d'un quart de tour (son emprise reste alignée), les autres librement.
    /// </summary>
    public void Scatter(ReliefArena arena, ReliefSim sim, List<ReliefLowPoly.Placement> placed, IReadOnlyList<Vector2> keepClear,
        Vector2 spawn, float cellsPerMetre, ulong seed)
    {
        RandomNumberGenerator rng = new() { Seed = seed };
        for (int y = 0; y < arena.Depth; y++)
        {
            for (int x = 0; x < arena.Width; x++)
            {
                if (rng.Randf() >= _density)
                    continue;
                (ReliefLowPoly.Model model, string path, _, bool blocking) = Pick(rng.Randf() * _totalWeight);
                Vector2 at = new(x + rng.RandfRange(0.25f, 0.75f), y + rng.RandfRange(0.25f, 0.75f));
                float yaw = blocking ? 90f * rng.RandiRange(0, 3) : rng.RandfRange(0f, 360f);
                if (!ReliefLowPoly.IsFlatInterior(arena, x, y) || at.DistanceTo(spawn) < 2.5f
                    || ReliefLowPoly.DistanceToPath(at, keepClear) < 1.4f || ReliefLowPoly.Crowded(placed, at, cellsPerMetre))
                    continue;
                placed.Add(new ReliefLowPoly.Placement(model, path, at, yaw));
                if (blocking)
                {
                    Aabb footprint = new(model.Bounds.Position, model.Bounds.Size * FootprintShare);
                    ReliefLowPoly.AddFootprint(sim, footprint, at, yaw, cellsPerMetre);
                }
            }
        }
    }

    private (ReliefLowPoly.Model, string, float, bool) Pick(float roll)
    {
        foreach ((ReliefLowPoly.Model, string, float Weight, bool) entry in _entries)
        {
            roll -= entry.Weight;
            if (roll < 0f)
                return entry;
        }
        return _entries[^1];
    }

    /// <summary>Les entrées de l'index de la bibliothèque, par nom de modèle, de sprite source ou de variante écartée.</summary>
    private static Dictionary<string, Godot.Collections.Dictionary> ReadIndex(string root)
    {
        using FileAccess file = FileAccess.Open($"{root}/index.json", FileAccess.ModeFlags.Read)
            ?? throw new System.IO.FileNotFoundException($"Bibliothèque absente : {root}/index.json (python3 tools/export_lowpoly.py --all)");
        Dictionary<string, Godot.Collections.Dictionary> byName = new();
        foreach (Variant value in Json.ParseString(file.GetAsText()).AsGodotDictionary()["models"].AsGodotArray())
        {
            Godot.Collections.Dictionary entry = value.AsGodotDictionary();
            if (entry["catalog"].AsString() is "characters" or "creatures")
                continue;
            byName.TryAdd(entry["id"].AsString(), entry);
            foreach (string key in new[] { "sources", "dropped_variants" })
            {
                if (entry.TryGetValue(key, out Variant names))
                {
                    foreach (Variant name in names.AsGodotArray())
                        byName.TryAdd(name.AsString(), entry);
                }
            }
        }
        return byName;
    }

    private static Godot.Collections.Dictionary Find(Dictionary<string, Godot.Collections.Dictionary> library, params string[] names)
    {
        foreach (string name in names)
        {
            if (library.TryGetValue(name, out Godot.Collections.Dictionary entry))
                return entry;
        }
        return null;
    }
}
