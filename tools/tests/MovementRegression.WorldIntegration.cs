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
    /// <summary>Néant (mémoire nulle) : le joueur, sans invincibilité, y perd des PV en continu.</summary>
    private async Task CheckVoidDamage(WorldSetup world)
    {
        ErasureManager erasure = world.GetNode<ErasureManager>("ErasureManager");
        _player.Position = Vector2.Zero;
        Vector2I center = new(0, 0);
        for (int y = -2; y <= 2; y++)
            for (int x = -2; x <= 2; x++)
                erasure.OverrideMemory(center + new Vector2I(x, y), 0f);
        _player.IsGodMode = false;
        float before = _player.CurrentHp;
        erasure.SetProcess(true);
        await Step(80);
        erasure.SetProcess(false);
        float after = _player.CurrentHp;
        ErasureEffects.Effect inVoid = ReadPlayerField<ErasureEffects.Effect>("_erasurePenalty");
        // Ce que l'oubli offre : la même créature rapporte plus au Néant qu'en zone ancrée, loin de là.
        Vestiges.Score.ScoreManager score = world.GetNode<Vestiges.Score.ScoreManager>("ScoreManager");
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        int start = score.CurrentScore;
        bus.EmitSignal(EventBus.SignalName.EnemyKilled, "rodeur", Vector2.Zero);
        int inVoidPoints = score.CurrentScore - start;
        start = score.CurrentScore;
        bus.EmitSignal(EventBus.SignalName.EnemyKilled, "rodeur", new Vector2(0f, -3000f));
        int anchoredPoints = score.CurrentScore - start;
        _player.IsGodMode = true;
        for (int y = -2; y <= 2; y++)
            for (int x = -2; x <= 2; x++)
                erasure.OverrideMemory(center + new Vector2I(x, y), 1f);
        Check(after < before && after > 0f, $"Néant : dégâts continus, PV {before} → {after} en 80 ticks");
        Check(inVoidPoints > anchoredPoints && anchoredPoints > 0, $"Néant : kill à {inVoidPoints} points contre {anchoredPoints} en zone ancrée");
        Check(inVoid.Speed < 1f && inVoid.Damage < 1f, $"Néant : vitesse ×{inVoid.Speed} et dégâts ×{inVoid.Damage} appliqués au joueur");
        erasure.SetProcess(true);
        await Step(40);
        erasure.SetProcess(false);
        ErasureEffects.Effect restored = ReadPlayerField<ErasureEffects.Effect>("_erasurePenalty");
        Check(restored.Speed == 1f && restored.Damage == 1f, "retour en zone ancrée : pénalités levées");
    }

    /// <summary>Marche vers l'est jusqu'au bord de la carte générée : le joueur ne quitte jamais le sol.</summary>
    private async Task CheckWorldEdge(WorldSetup world)
    {
        TileMapLayer ground = world.GetNode<TileMapLayer>("Ground");
        int edgeX = 0;
        while (edgeX < world.Generator.MapRadius + 2 && world.Generator.IsWithinBounds(edgeX, 0) && !world.Generator.IsErased(edgeX, 0))
            edgeX++;
        Vector2 start = ground.MapToLocal(new Vector2I(edgeX - 3, 0));
        _player.Position = start;
        SetActions(Vector2.Right);
        bool leftGround = false;
        for (int i = 0; i < 150; i++)
        {
            await Step(1);
            Vector2I cell = ground.LocalToMap(ground.ToLocal(_player.GlobalPosition));
            leftGround |= !world.Generator.IsWithinBounds(cell.X, cell.Y) || world.Generator.IsErased(cell.X, cell.Y);
        }
        SetActions(Vector2.Zero);
        Check(!leftGround && _player.Position.X > start.X,
            $"bord du monde : la marche s'arrête au sol, bord x={edgeX}, position={_player.Position}");
        Vector2 origin = Vector2.Zero;
        _player.Position = origin;
        await Step(10);
    }

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
        await CheckWorldEdge(world);
        await CheckVoidDamage(world);
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
