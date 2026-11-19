using System.Collections.Generic;
using Godot;

using Vestiges.World3D;

namespace Vestiges.Tests;

/// <summary>
/// Contrôle des rebords du bac à sable de la verticalité (plan 59) : sur chaque marche et chaque falaise de l'arène, le
/// moteur du joueur de la run 3D (<see cref="PlayerMotor3D"/>) descend en marchant puis continue tout droit, et revient buter contre la paroi avant de repartir. Il ne
/// doit jamais rester collé. Lancé par <c>--check-edges</c>, sans rendu ni biome.
/// </summary>
public static class ReliefEdgeCheck
{
    private const float Dt = 1f / 60f;
    private static readonly Vector2I[] Directions = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

    /// <summary>Renvoie le nombre de rebords où le joueur reste collé, et les imprime.</summary>
    public static int Run(ReliefConfig config, ReliefArena arena)
    {
        int tested = 0;
        List<string> stuck = new();
        for (int y = 0; y < arena.Depth; y++)
        {
            for (int x = 0; x < arena.Width; x++)
            {
                float high = arena.CellHeight(x, y);
                // Les rampes et leurs talus n'ont pas de rebord franc à tester : on les monte en marchant.
                if (ReliefArena.IsVoid(high) || arena.IsRamp(x, y))
                    continue;
                foreach (Vector2I d in Directions)
                {
                    // Une marche ou une falaise vers le bas, puis trois cellules de sol plat derrière : la place de s'éloigner.
                    float low = arena.CellHeight(x + d.X, y + d.Y);
                    if (ReliefArena.IsVoid(low) || low >= high - 0.01f || arena.IsRamp(x + d.X, y + d.Y))
                        continue;
                    if (arena.CellHeight(x + 2 * d.X, y + 2 * d.Y) != low || arena.CellHeight(x + 3 * d.X, y + 3 * d.Y) != low)
                        continue;
                    // Les deux voisins latéraux de la paroi, au même niveau, sinon le joueur longe un coin.
                    Vector2I side = new(d.Y, d.X);
                    if (!SameRow(arena, x, y, side, high) || !SameRow(arena, x + d.X, y + d.Y, side, low))
                        continue;
                    tested++;
                    Vector2 dir = new(d.X, d.Y);
                    Vector2 top = new(x + 0.5f, y + 0.5f);

                    // Descendre : partir du centre du haut et marcher 0,8 s vers le bas.
                    PlayerMotor3D sim = new(config, arena);
                    sim.Teleport(top);
                    Walk(sim, dir, 0.8f);
                    float down = Progress(sim, top, dir);
                    // Puis buter contre la paroi depuis le bas (on monte si la marche le permet), et repartir.
                    Vector2 below = top + dir * 2.5f;
                    sim.Teleport(below);
                    Walk(sim, -dir, 0.8f);
                    // S'arrêter avant de repartir : l'inertie ne compte pas pour un collage.
                    Walk(sim, Vector2.Zero, 0.3f);
                    Vector2 hug = new(sim.Position.X, sim.Position.Y);
                    Walk(sim, dir, 0.3f);
                    float away = Progress(sim, hug, dir);

                    // Seuils en part de la course libre à la vitesse du personnage : collé, il n'en fait presque rien.
                    if (down < 0.4f * config.PlayerSpeed * 0.8f || away < 0.4f * config.PlayerSpeed * 0.3f)
                        stuck.Add($"({x},{y})→({d.X},{d.Y}) {high / arena.LevelHeight:0.##}→{low / arena.LevelHeight:0.##} étage : descente {down:0.00}, départ du pied {away:0.00}");
                }
            }
        }
        GD.Print($"[ReliefEdgeCheck] RESULT rebords={tested} colles={stuck.Count}");
        for (int i = 0; i < Mathf.Min(stuck.Count, 12); i++)
            GD.Print($"[ReliefEdgeCheck]   {stuck[i]}");
        ReportMovement(config, arena);
        return stuck.Count + CheckHighGround(config, arena);
    }

    /// <summary>
    /// Le mouvement de chaque personnage en mètres, à l'échelle des modèles : vitesse, saut (sommet, temps en l'air, portée
    /// en course) et portée de sa capacité, pour juger le rythme sans image.
    /// </summary>
    private static void ReportMovement(ReliefConfig config, ReliefArena arena)
    {
        string played = config.Character;
        // Mètres mesurés au corps : un monde agrandi (§28) a des cellules et des étages plus grands, pas des corps plus petits.
        float m = config.MetresPerCellAtBody;
        float apex = config.JumpVelocity * config.JumpVelocity / (2f * config.Gravity);
        float air = 2f * config.JumpVelocity / config.Gravity;
        GD.Print($"[ReliefEdgeCheck] Unités : une cellule fait {m:0.00} m au corps, {1f / config.CellsPerPixel:0} px du jeu 2D");
        GD.Print($"[ReliefEdgeCheck] Saut : sommet {apex * m:0.00} m ({apex / arena.LevelHeight:0.00} étage d'un étage de {arena.LevelHeight * m:0.00} m), {air:0.00} s en l'air, gravité {config.Gravity * m / 9.81f:0.0} g");
        foreach (string id in new[] { "traqueur", "vagabond", "forgeuse", "facteur", "eveillee", "scaphandriere" })
        {
            config.SetCharacter(id);
            Vestiges.Infrastructure.MobilityConfig mobility = config.Mobility;
            float speed = config.PlayerSpeed * m;
            GD.Print($"[ReliefEdgeCheck] {id} : {speed:0.0} m/s, saut en course {speed * air:0.0} m, {mobility.Name} {speed * mobility.SpeedMultiplier * mobility.DurationSeconds:0.0} m en {mobility.DurationSeconds:0.00} s, toutes les {mobility.CooldownSeconds:0.0} s");
        }
        config.SetCharacter(played);
    }

    /// <summary>
    /// Les falaises de trois à cinq étages (§240) : on y monte par leurs rampes, jamais d'un saut. Renvoie le nombre d'échecs.
    /// </summary>
    private static int CheckHighGround(ReliefConfig config, ReliefArena arena)
    {
        int failures = 0;
        PlayerMotor3D sim = new(config, arena);
        sim.Teleport(new Vector2(24f, 10.9f));
        Vector2 tower = new(7f, 6.5f);
        foreach (Vector2 goal in new Vector2[] { new(15f, 10.9f), new(7f, 12.4f), tower })
        {
            for (float t = 0f; t < 4f && new Vector2(sim.Position.X, sim.Position.Y).DistanceTo(goal) > 0.2f; t += Dt)
                sim.Step(Dt, (goal - new Vector2(sim.Position.X, sim.Position.Y)).Normalized());
        }
        float stairsLevel = sim.Level;
        if (stairsLevel < arena.GroundAt(tower.X, tower.Y) / arena.LevelHeight - 0.01f)
            failures++;

        // Saut depuis le pied de chaque paroi, droit dessus : il ne doit jamais y monter.
        (Vector2 From, Vector2 Dir, string Name)[] bonds =
        {
            (new Vector2(10f, 13.4f), new Vector2(0f, -1f), "haut palier"),
            (new Vector2(5f, 9.4f), new Vector2(0f, -1f), "tour"),
            (new Vector2(27f, 14.4f), new Vector2(0f, -1f), "plateau nord-est"),
            (new Vector2(32f, 9.4f), new Vector2(0f, -1f), "sommet nord-est"),
            (new Vector2(13.6f, 29f), new Vector2(-1f, 0f), "plateau sud-ouest"),
            (new Vector2(30.4f, 23f), new Vector2(1f, 0f), "plateau est"),
        };
        string bondReport = "";
        foreach ((Vector2 from, Vector2 dir, string name) in bonds)
        {
            sim = new PlayerMotor3D(config, arena);
            sim.Teleport(from);
            float start = sim.Level;
            sim.QueueJump();
            Walk(sim, dir, 1.2f);
            float gained = sim.Level - start;
            bondReport += $" {name}=+{gained:0.00}";
            if (gained > 0.01f)
                failures++;
        }
        GD.Print($"[ReliefEdgeCheck] RESULT rampes_jusqu_a_la_tour={stairsLevel:0.00} étage, saut seul :{bondReport} étage ; échecs={failures}");
        return failures;
    }

    private static bool SameRow(ReliefArena arena, int x, int y, Vector2I side, float height) =>
        arena.CellHeight(x + side.X, y + side.Y) == height && arena.CellHeight(x - side.X, y - side.Y) == height;

    private static void Walk(PlayerMotor3D sim, Vector2 dir, float seconds)
    {
        for (float t = 0f; t < seconds; t += Dt)
            sim.Step(Dt, dir);
    }

    private static float Progress(PlayerMotor3D sim, Vector2 from, Vector2 dir) =>
        (new Vector2(sim.Position.X, sim.Position.Y) - from).Dot(dir);
}
