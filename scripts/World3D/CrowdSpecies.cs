using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World3D;

/// <summary>
/// Les sortes de créatures d'une terre pour la horde 3D (plan 60, 60.2a, §30) : la liste <c>exploration_enemy_pool</c>
/// de la terre, chaque fiche de <c>data/enemies</c> convertie en cellules par <see cref="ReliefConfig.CellsPerPixel"/>.
/// Un id cité plusieurs fois pèse d'autant, multiplié par son <c>spawn_weight</c>, comme le tirage du jeu 2D.
/// Seules les créatures de mêlée et de rang normal, dont le modèle est exporté, viennent pour l'instant : les tireuses
/// arrivent en 60.2b, les variantes en 60.2d, les gardiens avec leurs repaires (60.5).
/// </summary>
public static class CrowdSpecies
{
    /// <param name="libraryRoot">La bibliothèque de l'export de masse (<c>build/models</c>), sous <c>res://</c>.</param>
    public static Crowd3D.Species[] FromLand(string land, float cellsPerPixel, string libraryRoot)
    {
        BiomeData biome = BiomeDataLoader.Get(land);
        if (biome == null)
        {
            GD.PushError($"[CrowdSpecies] Terre inconnue : {land}");
            return System.Array.Empty<Crowd3D.Species>();
        }
        EnemyDataLoader.Load();
        Dictionary<string, Crowd3D.Species> byId = new();
        HashSet<string> seen = new();
        List<Crowd3D.Species> species = new();
        foreach (string id in biome.ExplorationEnemyPool)
        {
            if (byId.TryGetValue(id, out Crowd3D.Species known))
            {
                known.Weight += EnemyDataLoader.Get(id).GetStat("spawn_weight");
                continue;
            }
            if (!seen.Add(id))
                continue;
            EnemyData data = EnemyDataLoader.Get(id);
            if (data == null || data.CombatType != EnemyCombatType.Melee || data.Tier != EnemyTier.Normal)
                continue;
            string folder = string.IsNullOrEmpty(data.Visual?.SpriteFolder) ? id : data.Visual.SpriteFolder;
            string root = $"{libraryRoot}/creatures/{folder}";
            if (!FileAccess.FileExists($"{root}.json"))
            {
                GD.PushWarning($"[CrowdSpecies] Modèle 3D de {id} absent : python3 tools/export_lowpoly.py --creature {folder} ; écartée");
                continue;
            }
            Crowd3D.Species kind = new()
            {
                Id = id,
                Speed = data.Stats.Speed * cellsPerPixel,
                Hp = data.Stats.Hp,
                ContactDamage = data.Stats.Damage,
                Weight = data.GetStat("spawn_weight"),
                FromMinute = data.GetStat("spawn_from_minute"),
                ModelRoot = root,
            };
            byId[id] = kind;
            species.Add(kind);
        }
        return species.ToArray();
    }
}
