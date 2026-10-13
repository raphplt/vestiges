using System.Collections.Generic;
using System.Linq;
using Godot;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --census-props (avec le relevé de densité) : décors de la carte comptés par sprite, au début de la run. Une ligne
/// RESULT donne le total et les familles de barrières (clôtures, barrages, plots), pour mesurer un retrait avant/après.
/// </summary>
public partial class RunObservation
{
    private static readonly string[] BarrierWords = { "fence", "barrier", "barricade", "cone", "hedge", "wall" };

    private void PrintPropCensus()
    {
        Dictionary<string, int> counts = new();
        TileMapLayer ground = _world.GetNode<TileMapLayer>("Ground");
        int total = 0;
        // Récursif et sans filtre de visibilité : les décors loin de la caméra sont masqués, pas absents.
        Stack<Node> pending = new();
        pending.Push(_world.GetNode("PropContainer"));
        while (pending.Count > 0)
        {
            Node node = pending.Pop();
            if (node is not EnvironmentProp prop)
            {
                foreach (Node child in node.GetChildren())
                    pending.Push(child);
                continue;
            }
            Sprite2D sprite = prop.GetChildren().OfType<Sprite2D>().FirstOrDefault();
            string file = sprite?.Texture?.ResourcePath.GetFile().GetBaseName() ?? "?";
            // Variantes (_v2, sens de fuite…) regroupées sous leur famille.
            string family = System.Text.RegularExpressions.Regex.Replace(file, @"(_v\d+|_[nsewxyh]|_broken)+$", "");
            if (BarrierWords.Any(word => family.Contains(word)))
            {
                Vector2I cell = ground.LocalToMap(ground.ToLocal(prop.GlobalPosition));
                family = $"{family}@{_world.Generator.GetBiomeId(cell.X, cell.Y)}";
            }
            counts[family] = counts.GetValueOrDefault(family) + 1;
            total++;
        }
        IEnumerable<string> barriers = counts.Where(entry => BarrierWords.Any(word => entry.Key.Contains(word)))
            .OrderByDescending(entry => entry.Value).Select(entry => $"{entry.Key}:{entry.Value}");
        GD.Print($"[RunObservation] RESULT census props={total} barriers={string.Join(',', barriers)}");
    }
}
