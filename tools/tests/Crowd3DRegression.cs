using System.Collections.Generic;
using Godot;
using Vestiges.World3D;

namespace Vestiges.Tests;

/// <summary>
/// Régression de la horde 3D (plan 60, 60.2b), sans rendu : sur une terre plate à deux paliers, une créature venue du
/// flux suit le joueur sans l'avoir repéré ; repérée puis distancée, elle perd sa trace, erre et se retire au loin. Une
/// tireuse s'arrête à portée et tire ; son tir touche le joueur sur le palier du dessus, à plat, et traverse un joueur
/// invulnérable ; prise à portée en pleine escalade, elle finit sa montée. Résultat : <c>[Crowd3DRegression] RESULT failures=N</c>.
/// </summary>
public partial class Crowd3DRegression : Node
{
    private const float Dt = 1f / 60f;
    private const int Melee = 0;
    private const int Shooter = 1;
    private const int Size = 48;

    private readonly List<string> _failures = new();
    private ReliefConfig _config;
    private ReliefArena _arena;

    public override void _Ready()
    {
        _config = ReliefConfig.Load();
        _config.Creatures = new[]
        {
            new Crowd3D.Species { Id = "melee", Speed = 1f, Hp = 10f, ContactDamage = 5f, Weight = 1f },
            new Crowd3D.Species
            {
                Id = "shooter", Speed = 1f, Hp = 10f, ContactDamage = 7f, Weight = 1f, Ranged = true,
                AttackRange = 3f, RangedCooldown = 1f, ShotReach = 6f,
            },
        };
        // L'ouest à un étage, l'est à deux : une falaise au milieu, que les tirs ignorent.
        string[] rows = new string[Size];
        for (int y = 0; y < Size; y++)
            rows[y] = new string('4', Size / 2) + new string('8', Size / 2);
        _arena = new ReliefArena(rows, _config.LevelHeight);
        _arena.Obstacles.Finish();

        UnseenCreatureFollows();
        LostCreatureWandersThenRetreats();
        ShooterStopsAndHitsAcrossLevels();
        InvulnerablePlayerLetsShotsPass();
        ShooterInRangeFinishesClimb();

        foreach (string failure in _failures)
            GD.PrintErr($"[Crowd3DRegression] ÉCHEC : {failure}");
        GD.Print($"[Crowd3DRegression] RESULT failures={_failures.Count}");
        GetTree().Quit(_failures.Count == 0 ? 0 : 1);
    }

    private static readonly Crowd3D.Tracking Tracking = new(5f, 9f, 1f, 0.35f, 2.5f);

    private (Crowd3D Crowd, PlayerMotor3D Player) Setup(Vector2 player)
    {
        PlayerMotor3D motor = new(_config, _arena);
        motor.Teleport(player);
        return (new Crowd3D(_config, _arena, _config.Creatures, Tracking, 8, 7), motor);
    }

    private Vector3 Ground(float x, float y) => new(x, y, _arena.GroundAt(x, y));

    private static void Run(Crowd3D crowd, PlayerMotor3D player, float seconds, bool holdPlayer = true)
    {
        for (float t = 0f; t < seconds; t += Dt)
        {
            if (holdPlayer)
                player.Hp = player.Config.PlayerHp;
            player.Step(Dt, Vector2.Zero);
            crowd.Step(Dt, player);
        }
    }

    private static float Distance(Crowd3D crowd, int i, PlayerMotor3D player) =>
        new Vector2(crowd.Position[i].X - player.Position.X, crowd.Position[i].Y - player.Position.Y).Length();

    private void Check(bool ok, string message)
    {
        if (!ok)
            _failures.Add(message);
        GD.Print($"[Crowd3DRegression] {(ok ? "ok" : "ÉCHEC")} : {message}");
    }

    private void UnseenCreatureFollows()
    {
        (Crowd3D crowd, PlayerMotor3D player) = Setup(new Vector2(6f, 24f));
        int i = crowd.SpawnAt(Ground(6f, 12f), Melee);
        float before = Distance(crowd, i, player);
        Run(crowd, player, 2f);
        Check(!crowd.Lost[i] && Distance(crowd, i, player) < before - 1.5f,
            $"une créature du flux, jamais à portée de repérage, suit le joueur ({before:0.0} → {Distance(crowd, i, player):0.0} cellules)");
    }

    private void LostCreatureWandersThenRetreats()
    {
        (Crowd3D crowd, PlayerMotor3D player) = Setup(new Vector2(6f, 24f));
        // Le retrait attend que l'errance soit vue.
        _config.DespawnDistance = 100f;
        int i = crowd.SpawnAt(Ground(6f, 21f), Melee);
        Run(crowd, player, 0.2f);
        player.Teleport(new Vector2(6f, 44f));
        Run(crowd, player, 1.5f);
        Check(crowd.Lost[i], "repérée puis distancée plus d'une seconde au-delà de la laisse, elle perd la trace");
        Vector3 at = crowd.Position[i];
        Run(crowd, player, 1f);
        float moved = new Vector2(crowd.Position[i].X - at.X, crowd.Position[i].Y - at.Y).Length();
        Check(moved > 0.1f && moved < 0.6f, $"perdue, elle erre au tiers de sa vitesse ({moved:0.00} cellule en 1 s)");
        _config.DespawnDistance = 12f;
        player.Teleport(new Vector2(40f, 44f));
        Run(crowd, player, 0.1f);
        Check(crowd.State[i] == Crowd3D.Life.Inactive && crowd.Retreats == 1, "perdue et au-delà du retrait, elle quitte la horde");
    }

    private void ShooterStopsAndHitsAcrossLevels()
    {
        (Crowd3D crowd, PlayerMotor3D player) = Setup(new Vector2(25.5f, 24f));
        int i = crowd.SpawnAt(Ground(18f, 24f), Shooter);
        Run(crowd, player, 8f);
        float dist = Distance(crowd, i, player);
        Check(dist <= 3.05f && dist > 2.4f, $"la tireuse s'arrête à portée, au pied de la falaise ({dist:0.00} cellules pour 3)");
        Check(crowd.Position[i].Z < player.Position.Z - 0.4f, "elle reste un étage plus bas que le joueur");
        Check(crowd.Shots >= 3 && crowd.ShotHits >= 2, $"ses tirs touchent le joueur un étage plus haut ({crowd.ShotHits} sur {crowd.Shots})");
    }

    private void InvulnerablePlayerLetsShotsPass()
    {
        (Crowd3D crowd, PlayerMotor3D player) = Setup(new Vector2(25.5f, 24f));
        crowd.SpawnAt(Ground(22.5f, 24f), Shooter);
        for (float t = 0f; t < 3f; t += Dt)
        {
            player.Invulnerable = 1f;
            player.Step(Dt, Vector2.Zero);
            crowd.Step(Dt, player);
        }
        Check(crowd.Shots >= 2 && crowd.ShotHits == 0 && player.Hp == _config.PlayerHp,
            $"un joueur invulnérable laisse passer les tirs ({crowd.Shots} tirs, {crowd.ShotHits} au but)");
    }

    private void ShooterInRangeFinishesClimb()
    {
        (Crowd3D crowd, PlayerMotor3D player) = Setup(new Vector2(29f, 24f));
        int i = crowd.SpawnAt(Ground(21f, 24f), Shooter);
        float seconds = 0f;
        while (!crowd.Climbing[i] && seconds < 6f)
        {
            Run(crowd, player, Dt);
            seconds += Dt;
        }
        Check(crowd.Climbing[i], $"hors de portée, la tireuse grimpe la falaise ({seconds:0.0} s)");
        player.Teleport(new Vector2(25.5f, 24f));
        Run(crowd, player, 4f);
        Check(!crowd.Climbing[i] && crowd.Position[i].X > 24f && crowd.Position[i].Z > 0.9f,
            $"prise à portée en route, elle finit sa montée et prend pied en haut (x {crowd.Position[i].X:0.00}, z {crowd.Position[i].Z:0.00})");
    }
}
