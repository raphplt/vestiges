using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;

namespace Vestiges.World3D;

/// <summary>
/// Les sortes de créatures d'une terre pour la horde 3D (plan 60, 60.2a, §30) : la liste <c>exploration_enemy_pool</c>
/// de la terre, chaque fiche de <c>data/enemies</c> convertie en cellules par <see cref="ReliefConfig.CellsPerPixel"/>.
/// Un id cité plusieurs fois pèse d'autant, multiplié par son <c>spawn_weight</c>, comme le tirage du jeu 2D.
/// Seules les créatures de rang normal dont le modèle est exporté viennent : les variantes arrivent en 60.2d, les gardiens
/// avec leurs repaires (60.5). Une tireuse dont une capacité remplace le tir (présage, tir visé) tire en attendant son
/// tir propre (60.2c).
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
            if (data == null || data.CombatType == EnemyCombatType.Boss || data.Tier != EnemyTier.Normal)
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
                Ranged = data.CombatType == EnemyCombatType.Ranged,
                AttackRange = data.Stats.AttackRange * cellsPerPixel,
                RangedCooldown = data.GetStat("ranged_cooldown"),
                ShotReach = (data.Stats.AttackRange + data.GetStat("projectile_reach_margin")) * cellsPerPixel,
                Perception = data.GetStat("perception") * cellsPerPixel,
                Leash = data.GetStat("leash") * cellsPerPixel,
            };
            byId[id] = kind;
            species.Add(kind);
        }
        return species.ToArray();
    }

    /// <summary>Le pistage commun du jeu 2D (<c>data/scaling/enemy_tracking.json</c>), converti en cellules.</summary>
    public static Crowd3D.Tracking TrackingOf(float cellsPerPixel)
    {
        const string path = "res://data/scaling/enemy_tracking.json";
        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        string error = "illisible";
        if (file == null || !EnemyTracking.TryParse(file.GetAsText(), out EnemyTracking.Settings s, out error))
        {
            GD.PushError($"[CrowdSpecies] {path} : {error} ; la horde ne perd jamais la trace");
            return Crowd3D.Tracking.Always;
        }
        return new Crowd3D.Tracking(s.EngageDistance * cellsPerPixel, s.LoseTrackDistance * cellsPerPixel, s.LoseTrackSeconds,
            s.WanderSpeedFactor, s.WanderTurnSeconds);
    }
}
