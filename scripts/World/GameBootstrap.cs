using Godot;
using Vestiges.Base;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Meta;
using Vestiges.Progression;
using Vestiges.Score;
using Vestiges.Simulation;
using Vestiges.Spawn;
using Vestiges.UI;

namespace Vestiges.World;

/// <summary>
/// Wire les systèmes entre eux au démarrage de la scène.
/// Exécuté après tous les _Ready (grâce à l'ordre des enfants dans Main).
/// Lit le personnage sélectionné depuis GameManager (choisi dans le Hub).
/// Détecte le mode simulation si un BatchRunner existe dans /root/.
/// </summary>
public partial class GameBootstrap : Node
{
    private const string FallbackCharacterId = "traqueur";

    public override void _Ready()
    {
        CharacterDataLoader.Load();
        WeaponDataLoader.Load();
        PerkDataLoader.Load();
        MetaSaveManager.Load();
        StartingKitDataLoader.Load();
        SouvenirDataLoader.Load();

        // Detect simulation mode
        BatchRunner batchRunner = GetNodeOrNull<BatchRunner>("/root/BatchRunner");
        bool isSimulation = batchRunner != null;

        PlayerProgression progression = GetNode<PlayerProgression>("../Player/PlayerProgression");
        Inventory inventory = GetNode<Inventory>("../Player/Inventory");
        PerkManager perkManager = GetNode<PerkManager>("../PerkManager");
        ScoreManager scoreManager = GetNode<ScoreManager>("../ScoreManager");
        RunTracker runTracker = GetNode<RunTracker>("../RunTracker");
        CraftManager craftManager = GetNode<CraftManager>("../CraftManager");
        StructureManager structureManager = GetNode<StructureManager>("../StructureManager");
        Player player = GetNode<Player>("../Player");
        Node2D foyer = GetNodeOrNull<Node2D>("../Foyer");

        if (isSimulation)
        {
            SetupSimulation(batchRunner, player, perkManager, scoreManager, runTracker,
                craftManager, inventory, progression);
        }
        else
        {
            SetupNormalGame(player, perkManager, scoreManager, runTracker,
                craftManager, structureManager, inventory, progression, foyer);
        }
    }

    private void SetupNormalGame(Player player, PerkManager perkManager,
        ScoreManager scoreManager, RunTracker runTracker, CraftManager craftManager,
        StructureManager structureManager, Inventory inventory,
        PlayerProgression progression, Node2D foyer)
    {
        StructurePlacer structurePlacer = GetNode<StructurePlacer>("../StructurePlacer");
        DayNightCycle dayNightCycle = GetNode<DayNightCycle>("../DayNightCycle");
        HUD hud = GetNode<HUD>("../HUD");
        LevelUpScreen levelUpScreen = GetNode<LevelUpScreen>("../LevelUpScreen");
        GameOverScreen gameOverScreen = GetNode<GameOverScreen>("../GameOverScreen");
        CraftPanel craftPanel = GetNode<CraftPanel>("../CraftPanel");

        hud.SetProgression(progression);
        hud.SetDayNightCycle(dayNightCycle);
        hud.SetCompassTargets(player, foyer);

        WorldSetup worldSetup = GetNode<WorldSetup>("..");
        FogOfWar fogOfWar = GetNodeOrNull<FogOfWar>("../FogOfWar");
        hud.InitializeMinimap(worldSetup, fogOfWar);
        levelUpScreen.SetPerkManager(perkManager);
        gameOverScreen.SetScoreManager(scoreManager);

        craftManager.SetInventory(inventory);
        craftPanel.SetCraftManager(craftManager);
        craftPanel.SetInventory(inventory);
        craftPanel.SetStructureManager(structureManager);
        structurePlacer.SetStructureManager(structureManager);
        structurePlacer.SetCraftManager(craftManager);

        InitializeCharacterAndRun(player, perkManager, scoreManager, runTracker, inventory);

        GD.Print($"[GameBootstrap] Run started with {player.CharacterId}");
    }

    private void SetupSimulation(BatchRunner batchRunner, Player player,
        PerkManager perkManager, ScoreManager scoreManager, RunTracker runTracker,
        CraftManager craftManager, Inventory inventory, PlayerProgression progression)
    {
        SimulationRunConfig runConfig = batchRunner.CurrentRunConfig;

        // Skip UI wiring — LevelUpScreen.SetPerkManager() never called = no pause on level up
        // GameOverScreen.SetScoreManager() never called = null-safe SaveEndOfRun() is a no-op

        // Remove UI-only nodes for performance
        RemoveNodeIfExists("../HUD");
        RemoveNodeIfExists("../LevelUpScreen");
        RemoveNodeIfExists("../GameOverScreen");
        RemoveNodeIfExists("../CraftPanel");
        RemoveNodeIfExists("../StructurePlacer");
        RemoveNodeIfExists("../SouvenirPopup");
        RemoveNodeIfExists("../JournalScreen");
        RemoveNodeIfExists("../DebugOverlay");
        RemoveNodeIfExists("../FogOfWar");

        // Wire minimal gameplay systems
        craftManager.SetInventory(inventory);

        // Character setup — use config or fallback
        string characterId = runConfig.CharacterId ?? FallbackCharacterId;
        GameManager gm = GetNode<GameManager>("/root/GameManager");
        gm.SelectedCharacterId = characterId;

        CharacterData data = CharacterDataLoader.Get(characterId)
            ?? CharacterDataLoader.Get(FallbackCharacterId);

        player.InitializeCharacter(data);
        perkManager.ApplyPassivePerks(data.Id);
        scoreManager.SetCharacterMultiplier(data.ScoreMultiplier);
        scoreManager.SetRunTracker(runTracker);

        // Apply seed if specified
        if (runConfig.Seed > 0)
            gm.RunSeed = runConfig.Seed;

        // Apply scaling overrides (runtime, no file modification)
        if (runConfig.ScalingOverrides != null && runConfig.ScalingOverrides.Count > 0)
        {
            SpawnManager spawnManager = GetNode<SpawnManager>("../SpawnManager");
            spawnManager.ApplyScalingOverrides(runConfig.ScalingOverrides);
        }

        // Create and wire AIController
        AIProfile profile = AIProfile.FromName(runConfig.ProfileName);
        PerkStrategy perkStrategy = PerkStrategy.FromName(runConfig.PerkStrategyName);

        AIController aiController = new() { Name = "AIController" };
        // Initialize before adding to tree — _Ready() needs the fields set by Initialize
        aiController.Initialize(player, perkManager, scoreManager, profile, perkStrategy);
        player.IsAIControlled = true;

        // Deferred add — parent is still setting up children during _Ready()
        GetNode("..").CallDeferred("add_child", aiController);

        // Disable camera for performance
        Camera2D camera = player.GetNodeOrNull<Camera2D>("Camera");
        if (camera != null)
            camera.Enabled = false;

        // Set time scale
        Engine.TimeScale = runConfig.TimeScale;

        // Safety cap: force death after max duration (real-time seconds)
        float realTimeMaxSec = runConfig.MaxDurationSec / Mathf.Max(1f, runConfig.TimeScale);
        Timer maxDurationTimer = new()
        {
            WaitTime = realTimeMaxSec,
            OneShot = true,
            Autostart = true,
            ProcessMode = ProcessModeEnum.Always
        };
        maxDurationTimer.Timeout += () =>
        {
            if (!player.IsDead)
                player.TakeDamage(99999f);
        };
        GetNode("..").CallDeferred("add_child", maxDurationTimer);

        gm.ChangeState(GameManager.GameState.Run);

        GD.Print($"[GameBootstrap] SIMULATION run started — {data.Name}, " +
                 $"profile={profile.Name}, perks={runConfig.PerkStrategyName}, " +
                 $"timeScale={runConfig.TimeScale}x");
    }

    private void InitializeCharacterAndRun(Player player, PerkManager perkManager,
        ScoreManager scoreManager, RunTracker runTracker, Inventory inventory)
    {
        GameManager gm = GetNode<GameManager>("/root/GameManager");
        string characterId = gm.SelectedCharacterId;
        if (string.IsNullOrEmpty(characterId))
            characterId = FallbackCharacterId;

        CharacterData data = CharacterDataLoader.Get(characterId);
        if (data == null)
        {
            GD.PushError($"[GameBootstrap] Unknown character: {characterId}, falling back to {FallbackCharacterId}");
            data = CharacterDataLoader.Get(FallbackCharacterId);
        }

        player.InitializeCharacter(data);
        perkManager.ApplyPassivePerks(data.Id);
        scoreManager.SetCharacterMultiplier(data.ScoreMultiplier);
        scoreManager.SetRunTracker(runTracker);

        // Apply starting kit to inventory
        string selectedKit = MetaSaveManager.GetSelectedKit();
        if (!string.IsNullOrEmpty(selectedKit))
        {
            StartingKitData kit = StartingKitDataLoader.Get(selectedKit);
            if (kit != null)
            {
                foreach (System.Collections.Generic.KeyValuePair<string, int> item in kit.Contents)
                    inventory.Add(item.Key, item.Value);
                GD.Print($"[GameBootstrap] Applied starting kit: {kit.Name}");
            }
        }

        gm.ChangeState(GameManager.GameState.Run);
    }

    private void RemoveNodeIfExists(string path)
    {
        Node node = GetNodeOrNull(path);
        node?.QueueFree();
    }
}
