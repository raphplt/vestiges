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
        WeaponUpgradeDataLoader.Load();
        PerkDataLoader.Load();
        PassiveSouvenirDataLoader.Load();
        FusionDataLoader.Load();
        MetaSaveManager.Load();
        StartingKitDataLoader.Load();
        SouvenirDataLoader.Load();
        MutatorDataLoader.Load();

        // FragmentManager DOIT exister avant tout LevelUp —
        // certains nodes émettent XpGained/LevelUp pendant leur _Ready(),
        // avant que GameBootstrap ne finisse son setup.
        FragmentManager fragmentManager = GetNodeOrNull<FragmentManager>("../FragmentManager");
        if (fragmentManager == null)
        {
            fragmentManager = new FragmentManager { Name = "FragmentManager" };
            GetNode("..").CallDeferred("add_child", fragmentManager);
        }

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
                craftManager, inventory, progression, fragmentManager);
        }
        else
        {
            SetupNormalGame(player, perkManager, scoreManager, runTracker,
                craftManager, structureManager, inventory, progression, foyer,
                fragmentManager);
        }
    }

    private void SetupNormalGame(Player player, PerkManager perkManager,
        ScoreManager scoreManager, RunTracker runTracker, CraftManager craftManager,
        StructureManager structureManager, Inventory inventory,
        PlayerProgression progression, Node2D foyer,
        FragmentManager fragmentManager)
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

        // FragmentManager déjà créé dans _Ready() — juste wire l'UI
        levelUpScreen.SetFragmentManager(fragmentManager);

        // Perk Manager : gère les perks du monde (mémorial, coffres, POI)
        levelUpScreen.SetPerkManager(perkManager);
        gameOverScreen.SetScoreManager(scoreManager);

        craftManager.SetInventory(inventory);
        craftPanel.SetCraftManager(craftManager);
        craftPanel.SetInventory(inventory);
        craftPanel.SetStructureManager(structureManager);
        structurePlacer.SetStructureManager(structureManager);
        structurePlacer.SetCraftManager(craftManager);

        InitializeCharacterAndRun(player, perkManager, scoreManager, runTracker, inventory);
        ApplyMutators(scoreManager, dayNightCycle, worldSetup, foyer as Foyer);

        // Rattraper les level-ups manqués (XP gagnée avant que le setup soit complet)
        if (progression.CurrentLevel > 1 && fragmentManager.PendingChoices.Count == 0)
        {
            GD.Print($"[GameBootstrap] Catching up missed level-ups: player is level {progression.CurrentLevel}");
            fragmentManager.TriggerLevelUp(progression.CurrentLevel);
        }

        GD.Print($"[GameBootstrap] Run started with {player.CharacterId}");
    }

    private void SetupSimulation(BatchRunner batchRunner, Player player,
        PerkManager perkManager, ScoreManager scoreManager, RunTracker runTracker,
        CraftManager craftManager, Inventory inventory, PlayerProgression progression,
        FragmentManager fragmentManager)
    {
        SimulationRunConfig runConfig = batchRunner.CurrentRunConfig;

        // Skip UI wiring — LevelUpScreen.SetPerkManager() never called = no pause on level up
        // GameOverScreen.SetScoreManager() never called = null-safe SaveEndOfRun() is a no-op

        // Remove UI-only nodes for performance
        RemoveNodeIfExists("../HUD");
        RemoveNodeIfExists("../LevelUpScreen");
        RemoveNodeIfExists("../GameOverScreen");
        RemoveNodeIfExists("../CraftPanel");
        RemoveNodeIfExists("../PauseMenu");
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

        // Create and wire AIController (avec FragmentManager pour les level-ups)
        AIProfile profile = AIProfile.FromName(runConfig.ProfileName);
        PerkStrategy perkStrategy = PerkStrategy.FromName(runConfig.PerkStrategyName);

        AIController aiController = new() { Name = "AIController" };
        // Initialize before adding to tree — _Ready() needs the fields set by Initialize
        aiController.Initialize(player, perkManager, scoreManager, profile, perkStrategy, fragmentManager);
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

        // Rattraper les level-ups manqués en simulation
        if (progression.CurrentLevel > 1 && fragmentManager.PendingChoices.Count == 0)
        {
            GD.Print($"[GameBootstrap] SIM: Catching up missed level-ups: player is level {progression.CurrentLevel}");
            fragmentManager.TriggerLevelUp(progression.CurrentLevel);
        }

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

    private void ApplyMutators(ScoreManager scoreManager, DayNightCycle dayNightCycle,
        WorldSetup worldSetup, Foyer foyer)
    {
        GameManager gm = GetNode<GameManager>("/root/GameManager");
        System.Collections.Generic.List<string> activeMutators = gm.ActiveMutators;
        if (activeMutators == null || activeMutators.Count == 0)
            return;

        float totalMultiplier = 1f;
        System.Collections.Generic.Dictionary<string, float> scalingOverrides = new();

        foreach (string mutatorId in activeMutators)
        {
            MutatorData mutator = MutatorDataLoader.Get(mutatorId);
            if (mutator == null)
                continue;

            totalMultiplier *= mutator.ScoreMultiplier;

            switch (mutator.EffectType)
            {
                case "night_duration":
                    dayNightCycle.ApplyNightDurationMultiplier(mutator.EffectValue);
                    break;
                case "enemy_hp":
                    scalingOverrides["flat_hp_multiplier"] = mutator.EffectValue;
                    break;
                case "enemy_damage":
                    scalingOverrides["flat_dmg_multiplier"] = mutator.EffectValue;
                    break;
                case "no_safe_zone":
                    foyer?.DisableSafeZone();
                    break;
                case "spawn_rate":
                    scalingOverrides["base_spawn_interval"] = 1.80f * mutator.EffectValue;
                    break;
                case "no_pois":
                    worldSetup.PoisDisabled = true;
                    break;
            }

            GD.Print($"[GameBootstrap] Mutator applied: {mutator.Name}");
        }

        if (scalingOverrides.Count > 0)
        {
            SpawnManager spawnManager = GetNode<SpawnManager>("../SpawnManager");
            spawnManager.ApplyScalingOverrides(scalingOverrides);
        }

        scoreManager.SetMutatorMultiplier(totalMultiplier);
        GD.Print($"[GameBootstrap] {activeMutators.Count} mutator(s) active — score multiplier: x{totalMultiplier:F2}");
    }

    private void RemoveNodeIfExists(string path)
    {
        Node node = GetNodeOrNull(path);
        node?.QueueFree();
    }
}
