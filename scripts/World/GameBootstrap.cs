using System.Threading.Tasks;
using Godot;
using Vestiges.Base;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Meta;
using Vestiges.Progression;
using Vestiges.Score;
using Vestiges.Simulation;
using Vestiges.Spawn;
using Vestiges.Infrastructure.Steam;
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
        Combat.VfxFactory.LoadSettings();
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
            // Simulation : tout synchrone, pas d'overlay
            WorldSetup worldSetup = GetNode<WorldSetup>("..");
            worldSetup.InitializeWorldSync();

            EnemyPool enemyPool = GetNode<EnemyPool>("../EnemyPool");
            enemyPool.PrewarmSync();

            SetupSimulation(batchRunner, player, perkManager, scoreManager, runTracker,
                craftManager, inventory, progression, fragmentManager);
        }
        else
        {
            // Mode normal : lancer l'initialisation async avec overlay
            _ = SetupNormalGameAsync(player, perkManager, scoreManager, runTracker,
                craftManager, structureManager, inventory, progression, foyer,
                fragmentManager);
        }
    }

    private async Task SetupNormalGameAsync(Player player, PerkManager perkManager,
        ScoreManager scoreManager, RunTracker runTracker, CraftManager craftManager,
        StructureManager structureManager, Inventory inventory,
        PlayerProgression progression, Node2D foyer,
        FragmentManager fragmentManager)
    {
        // --- Créer et afficher l'overlay de chargement ---
        GameLoadingOverlay overlay = new() { Name = "GameLoadingOverlay" };
        GetNode("..").AddChild(overlay);

        // Pause le gameplay pendant le chargement
        GetTree().Paused = true;
        overlay.ProcessMode = ProcessModeEnum.Always;

        // --- Shader warmup (force la compilation GPU pendant l'overlay) ---
        overlay.SetProgress("Préparation des shaders...");
        WarmupShaders();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // --- Initialisation du monde (étalée sur plusieurs frames) ---
        WorldSetup worldSetup = GetNode<WorldSetup>("..");
        await worldSetup.InitializeWorldAsync(step => overlay.SetProgress(step));

        // --- Prewarm EnemyPool (étalé) ---
        overlay.SetProgress("Préparation des créatures...");
        EnemyPool enemyPool = GetNode<EnemyPool>("../EnemyPool");
        await enemyPool.PrewarmAsync(4);

        // --- Wire des systèmes (rapide, synchrone) ---
        overlay.SetProgress("Initialisation...");

        StructurePlacer structurePlacer = GetNode<StructurePlacer>("../StructurePlacer");
        DayNightCycle dayNightCycle = GetNode<DayNightCycle>("../DayNightCycle");
        HUD hud = GetNode<HUD>("../HUD");
        LevelUpScreen levelUpScreen = GetNode<LevelUpScreen>("../LevelUpScreen");
        GameOverScreen gameOverScreen = GetNode<GameOverScreen>("../GameOverScreen");
        ChestLootScreen chestLootScreen = GetNodeOrNull<ChestLootScreen>("../ChestLootScreen");
        CraftPanel craftPanel = GetNode<CraftPanel>("../CraftPanel");

        hud.SetProgression(progression);
        hud.SetDayNightCycle(dayNightCycle);
        hud.SetCompassTargets(player, foyer);

        FogOfWar fogOfWar = GetNodeOrNull<FogOfWar>("../FogOfWar");
        hud.InitializeMinimap(worldSetup, fogOfWar);

        levelUpScreen.SetFragmentManager(fragmentManager);
        levelUpScreen.SetPerkManager(perkManager);
        gameOverScreen.SetScoreManager(scoreManager);

        if (chestLootScreen != null)
            player.SetChestLootScreen(chestLootScreen);

        craftManager.SetInventory(inventory);
        craftPanel.SetCraftManager(craftManager);
        craftPanel.SetInventory(inventory);
        craftPanel.SetStructureManager(structureManager);
        structurePlacer.SetStructureManager(structureManager);
        structurePlacer.SetCraftManager(craftManager);

        InitializeCharacterAndRun(player, perkManager, scoreManager, runTracker, inventory);
        ApplyMutators(scoreManager, dayNightCycle, worldSetup, foyer as Foyer);

        if (progression.CurrentLevel > 1 && fragmentManager.PendingChoices.Count == 0)
        {
            GD.Print($"[GameBootstrap] Catching up missed level-ups: player is level {progression.CurrentLevel}");
            fragmentManager.TriggerLevelUp(progression.CurrentLevel);
        }

        ZoneMemoryManager zoneMemoryManager = new() { Name = "ZoneMemoryManager" };
        GetNode("..").CallDeferred("add_child", zoneMemoryManager);

        CursedItemManager cursedItemManager = new() { Name = "CursedItemManager" };
        cursedItemManager.SetPerkManager(perkManager);
        GetNode("..").CallDeferred("add_child", cursedItemManager);

        Combat.ScreenShake screenShake = new() { Name = "ScreenShake" };
        screenShake.SetCamera(player.GetNode<Camera2D>("Camera"));
        GetNode("..").CallDeferred("add_child", screenShake);

        AmbientParticles ambientParticles = new() { Name = "AmbientParticles" };
        GetNode("..").CallDeferred("add_child", ambientParticles);

        EventBus eventBus = GetNode<EventBus>("/root/EventBus");
        Player levelUpPlayer = player;
        eventBus.LevelUp += (int _level) =>
        {
            if (IsInstanceValid(levelUpPlayer))
            {
                Node2D burst = Combat.VfxFactory.CreateLevelUpBurst(levelUpPlayer.GlobalPosition);
                if (burst != null)
                    GetTree().CurrentScene.AddChild(burst);
                Combat.ScreenShake.Instance?.ShakeMedium();
            }
        };

        DebugActionPanel debugPanel = new DebugActionPanel { Name = "DebugActionPanel" };
        GetNode("..").CallDeferred("add_child", debugPanel);

        // Steam : achievements et leaderboards (no-op si Steam inactif)
        if (SteamManager.IsActive)
        {
            SteamAchievements steamAchievements = new() { Name = "SteamAchievements" };
            GetNode("..").CallDeferred("add_child", steamAchievements);

            SteamLeaderboards steamLeaderboards = new() { Name = "SteamLeaderboards" };
            GetNode("..").CallDeferred("add_child", steamLeaderboards);
        }

        GD.Print($"[GameBootstrap] Run started with {player.CharacterId}");

        // --- Dépause et fade-out de l'overlay ---
        GetTree().Paused = false;
        overlay.FadeOut();
    }

    /// <summary>
    /// Pré-charge et force la compilation de tous les shaders du jeu
    /// en rendant un sprite invisible avec chaque material pendant 1 frame.
    /// </summary>
    private void WarmupShaders()
    {
        string[] shaderPaths = new[]
        {
            "res://assets/shaders/entity.gdshader",
            "res://assets/shaders/fog_of_war.gdshader",
            "res://assets/shaders/sway.gdshader",
            "res://assets/shaders/swamp_atmosphere.gdshader",
            "res://assets/shaders/hit_flash.gdshader",
            "res://assets/shaders/dissolve.gdshader",
            "res://assets/shaders/outline.gdshader",
            "res://assets/shaders/aberration_aura.gdshader",
            "res://assets/shaders/colorblind.gdshader",
        };

        Node2D warmupContainer = new() { Name = "_ShaderWarmup" };
        warmupContainer.Position = new Vector2(-9999, -9999);
        GetNode("..").AddChild(warmupContainer);

        foreach (string path in shaderPaths)
        {
            Shader shader = GD.Load<Shader>(path);
            if (shader == null) continue;

            Sprite2D sprite = new();
            sprite.Material = new ShaderMaterial { Shader = shader };
            sprite.Texture = GD.Load<Texture2D>("res://icon.svg");
            warmupContainer.AddChild(sprite);
        }

        // Supprimer après 2 frames (assez pour compiler les shaders)
        // Utilise un timer car on est en pause
        Timer cleanup = new()
        {
            WaitTime = 0.1f,
            OneShot = true,
            Autostart = true,
            ProcessMode = ProcessModeEnum.Always,
        };
        cleanup.Timeout += () =>
        {
            warmupContainer.QueueFree();
            cleanup.QueueFree();
        };
        GetNode("..").AddChild(cleanup);
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
        RemoveNodeIfExists("../ChestLootScreen");
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
