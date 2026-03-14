using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.Events;
using Vestiges.Infrastructure;
using Vestiges.Meta;
using Vestiges.Progression;
using Vestiges.Score;
using Vestiges.Spawn;
using Vestiges.Infrastructure.Steam;
using Vestiges.UI;

namespace Vestiges.World;

/// <summary>
/// Wire les systèmes entre eux au démarrage de la scène.
/// Exécuté après tous les _Ready (grâce à l'ordre des enfants dans Main).
/// Lit le personnage sélectionné depuis GameManager (choisi dans le Hub).
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
        WeaponRarityDataLoader.Load();
        PerkDataLoader.Load();
        PassiveSouvenirDataLoader.Load();
        FusionDataLoader.Load();
        MetaSaveManager.Load();
        SouvenirDataLoader.Load();

        // FragmentManager DOIT exister avant tout LevelUp —
        // certains nodes émettent XpGained/LevelUp pendant leur _Ready(),
        // avant que GameBootstrap ne finisse son setup.
        FragmentManager fragmentManager = GetNodeOrNull<FragmentManager>("../FragmentManager");
        if (fragmentManager == null)
        {
            fragmentManager = new FragmentManager { Name = "FragmentManager" };
            GetNode("..").CallDeferred("add_child", fragmentManager);
        }

        PlayerProgression progression = GetNode<PlayerProgression>("../Player/PlayerProgression");
        PerkManager perkManager = GetNode<PerkManager>("../PerkManager");
        ScoreManager scoreManager = GetNode<ScoreManager>("../ScoreManager");
        RunTracker runTracker = GetNode<RunTracker>("../RunTracker");
        Player player = GetNode<Player>("../Player");

        _ = SetupNormalGameAsync(player, perkManager, scoreManager, runTracker,
            progression, fragmentManager);
    }

    private async Task SetupNormalGameAsync(Player player, PerkManager perkManager,
        ScoreManager scoreManager, RunTracker runTracker,
        PlayerProgression progression, FragmentManager fragmentManager)
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

        HUD hud = GetNode<HUD>("../HUD");
        LevelUpScreen levelUpScreen = GetNode<LevelUpScreen>("../LevelUpScreen");
        GameOverScreen gameOverScreen = GetNode<GameOverScreen>("../GameOverScreen");
        ChestLootScreen chestLootScreen = GetNodeOrNull<ChestLootScreen>("../ChestLootScreen");

        hud.SetProgression(progression);
        hud.SetCompassTargets(player, null);

        FogOfWar fogOfWar = GetNodeOrNull<FogOfWar>("../FogOfWar");
        hud.InitializeMinimap(worldSetup, fogOfWar);

        levelUpScreen.SetFragmentManager(fragmentManager);
        levelUpScreen.SetPerkManager(perkManager);
        gameOverScreen.SetScoreManager(scoreManager);

        if (chestLootScreen != null)
            player.SetChestLootScreen(chestLootScreen);

        Node sceneRoot = GetNode("..");
        ErasureManager erasureManager = new() { Name = "ErasureManager" };
        sceneRoot.AddChild(erasureManager);

        EssenceTracker essenceTracker = new() { Name = "EssenceTracker" };
        sceneRoot.AddChild(essenceTracker);

        CrisisManager crisisManager = new() { Name = "CrisisManager" };
        sceneRoot.AddChild(crisisManager);

        EndgameManager endgameManager = new() { Name = "EndgameManager" };
        sceneRoot.AddChild(endgameManager);

        AltarManager altarManager = new() { Name = "AltarManager" };
        sceneRoot.AddChild(altarManager);

        QuestManager questManager = new() { Name = "QuestManager" };
        sceneRoot.AddChild(questManager);

        hud.SetErasureManager(erasureManager);
        hud.SetCrisisManager(crisisManager);
        hud.SetEssenceTracker(essenceTracker);

        InitializeCharacterAndRun(player, perkManager, scoreManager, runTracker);

        if (progression.CurrentLevel > 1 && fragmentManager.PendingChoices.Count == 0)
        {
            GD.Print($"[GameBootstrap] Catching up missed level-ups: player is level {progression.CurrentLevel}");
            fragmentManager.TriggerLevelUp(progression.CurrentLevel);
        }

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
    private void InitializeCharacterAndRun(Player player, PerkManager perkManager,
        ScoreManager scoreManager, RunTracker runTracker)
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
        gm.ActiveMutators = new System.Collections.Generic.List<string>();

        gm.ChangeState(GameManager.GameState.Run);
    }
}
