using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.World;

namespace Vestiges.Tests;

public partial class MovementRegression
{
    /// <summary>Vraie initialisation Main ; seul l'état local du Néant est rendu déterministe.</summary>
    private async Task RunWorldIntegration()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        _manager.RunSeed = 221092026;
        _manager.SelectedCharacterId = "traqueur";
        WorldSetup world = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<WorldSetup>();
        GetTree().Root.AddChild(world);
        GetTree().CurrentScene = world;
        int waitingFrames = 0;
        while ((!world.IsWorldReady || GetTree().Paused || _manager.CurrentState != GameManager.GameState.Run) && waitingFrames < 1500)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            waitingFrames++;
        }
        Check(world.IsWorldReady && !GetTree().Paused && _manager.CurrentState == GameManager.GameState.Run,
            $"Main réelle : initialisation terminée en {waitingFrames} frames");
        if (!world.IsWorldReady || GetTree().Paused)
            return;
        _player = world.GetNode<Player>("Player");
        _sprite = _player.GetNode<AnimatedSprite2D>("Sprite");
        _player.SetPhysicsProcess(false);
        _player.IsGodMode = true;
        world.GetNode("SpawnManager").SetProcess(false);
        world.GetNode("SpawnManager").SetPhysicsProcess(false);
        ErasureManager erasure = world.GetNode<ErasureManager>("ErasureManager");
        erasure.SetProcess(false);
        // Lecture uniquement : cette liaison doit provenir du bootstrap réel, jamais du banc.
        Check(ReferenceEquals(ReadPlayerField<ErasureManager>("_erasureManager"), erasure),
            "Main réelle : Player relié au gestionnaire Effacement créé après son Ready");
        Check(ReferenceEquals(ReadPlayerField<WorldSetup>("_worldSetup"), world),
            "Main réelle : Player relié au générateur de terrain");
        _player.InitializeCharacter(Vestiges.Infrastructure.CharacterDataLoader.Get("traqueur"));
        Check(ReferenceEquals(ReadPlayerField<ErasureManager>("_erasureManager"), erasure),
            "initialisation personnage avec état déjà Run : liaison Effacement conservée");

        Dictionary<Vector2I, ErasureManager.ErasureZonePhase> phases =
            (Dictionary<Vector2I, ErasureManager.ErasureZonePhase>)typeof(ErasureManager)
                .GetField("_zonePhases", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(erasure);
        Vector2I region = new(1, 0);
        bool hadRegion = phases.TryGetValue(region, out ErasureManager.ErasureZonePhase oldPhase);
        phases[region] = ErasureManager.ErasureZonePhase.Void;
        await ReadyMobility();
        _player.Position = new Vector2(erasure.CellSize - 25f, 20f);
        Check(!world.IsWaterAt(_player.Position), "parcours Néant réel : départ sur sol ferme");
        SetActions(Vector2.Right);
        PressMobility();
        int steps = 0;
        do
        {
            await Step(1);
            ReleaseMobility();
            steps++;
        }
        while (_player.Mobility.IsDashing && steps < 9);
        SetActions(Vector2.Zero);
        Check(_player.Position.X >= erasure.CellSize - 2f && _player.Position.X < erasure.CellSize && _player.GetSlideCollisionCount() == 0,
            $"Main réelle : arrêt au Néant sans collision, position={_player.Position}, ticks={steps}");
        if (hadRegion)
            phases[region] = oldPhase;
        else
            phases.Remove(region);
        await CheckGeneratedWater(world);
        // Le pool historique garde ses instances préchauffées hors de l'arbre :
        // le banc les libère explicitement pour vérifier une fermeture sans erreurs RID.
        Vestiges.Spawn.EnemyPool pool = world.GetNode<Vestiges.Spawn.EnemyPool>("EnemyPool");
        Queue<Vestiges.Combat.Enemy> available = (Queue<Vestiges.Combat.Enemy>)typeof(Vestiges.Spawn.EnemyPool)
            .GetField("_available", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pool);
        while (available.TryDequeue(out Vestiges.Combat.Enemy enemy))
            enemy.Free();
        world.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task CheckGeneratedWater(WorldSetup world)
    {
        TileMapLayer ground = world.GetNode<TileMapLayer>("Ground");
        Vector2 waterStart = Vector2.Zero;
        bool found = false;
        float nearestDistance = float.MaxValue;
        ErasureManager erasure = world.GetNode<ErasureManager>("ErasureManager");
        int radius = world.Generator.MapRadius;
        // Chercher un trajet horizontal de 50 px entièrement dans une vraie nappe générée.
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (!world.Generator.IsWithinBounds(x, y) || world.Generator.GetTerrain(x, y) != TerrainType.Water)
                    continue;
                Vector2 origin = ground.ToGlobal(ground.MapToLocal(new Vector2I(x, y)));
                if (origin.LengthSquared() >= nearestDistance)
                    continue;
                bool allWater = true;
                for (int sample = 0; sample <= 50; sample += 2)
                {
                    Vector2 position = origin + Vector2.Right * sample;
                    Vector2I cell = ground.LocalToMap(ground.ToLocal(position));
                    if (!world.Generator.IsWithinBounds(cell.X, cell.Y) || world.Generator.IsErased(cell.X, cell.Y) || !world.IsWaterAt(position) || erasure.GetZonePhaseAt(position) == ErasureManager.ErasureZonePhase.Void)
                    {
                        allWater = false;
                        break;
                    }
                }
                if (allWater && !_player.TestMove(new Transform2D(0f, origin), Vector2.Right * 50f))
                {
                    waterStart = origin;
                    found = true;
                    nearestDistance = origin.LengthSquared();
                }
            }
        }
        Check(found, "monde généré : trajet continu dans l'eau trouvé");
        if (!found)
            return;
        await ReadyMobility();
        _player.Position = waterStart;
        SetActions(Vector2.Right);
        await Step(1);
        Check(Math.Abs(_player.Velocity.X - _player.Speed * 0.5f) < 0.1f,
            $"eau générée : marche={_player.Velocity.X:F3} px/s");
        _player.Position = waterStart;
        PressMobility();
        await Step(1);
        Check(_player.Mobility.IsDashing, "eau générée : dash démarré hors Néant et obstacles");
        ReleaseMobility();
        await Step(8);
        Vector2 traveled = _player.Position - waterStart;
        Check(traveled.DistanceTo(Vector2.Right * 43.2f) < 0.15f,
            $"eau générée : dash={traveled}, distance={traveled.Length():F3} px, départ={waterStart}");
        SetActions(Vector2.Zero);
    }
}
