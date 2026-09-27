using System.Runtime.CompilerServices;
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
        CharacterDataLoader.Load();
        WeaponDataLoader.Load();
        WeaponUpgradeDataLoader.Load();
        PerkDataLoader.Load();
        PassiveSouvenirDataLoader.Load();
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

        // L'écran de chargement entre dans l'arbre avant le premier rendu de la scène : sans lui, la toute première
        // frame montrait le HUD et un monde vide.
        GameLoadingOverlay overlay = new() { Name = "GameLoadingOverlay", ProcessMode = ProcessModeEnum.Always };
        GetParent().CallDeferred(Node.MethodName.AddChild, overlay);

        _ = SetupNormalGameAsync(player, perkManager, scoreManager, runTracker,
            progression, fragmentManager, overlay);
    }

    private async Task SetupNormalGameAsync(Player player, PerkManager perkManager,
        ScoreManager scoreManager, RunTracker runTracker,
        PlayerProgression progression, FragmentManager fragmentManager, GameLoadingOverlay overlay)
    {
        // Le parent termine ses _Ready avant de recevoir les nœuds de warmup ; l'overlay s'anime pendant la pause.
        GetTree().Paused = true;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // --- Shader warmup (force la compilation GPU pendant l'overlay) ---
        overlay.SetProgress("Préparation des shaders...");
        WarmupShaders();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        LoadProfiler.Mark("shaders");

        // --- Initialisation du monde (étalée sur plusieurs frames) ---
        WorldSetup worldSetup = GetNode<WorldSetup>("..");
        await worldSetup.InitializeWorldAsync(step => overlay.SetProgress(step));

        // --- Prewarm EnemyPool (étalé) ---
        overlay.SetProgress("Préparation des créatures...");
        EnemyPool enemyPool = GetNode<EnemyPool>("../EnemyPool");
        await enemyPool.PrewarmAsync(4);
        LoadProfiler.Mark("créatures du pool");

        // --- Wire des systèmes (rapide, synchrone) ---
        overlay.SetProgress("Initialisation...");

        HUD hud = GetNode<HUD>("../HUD");
        LevelUpScreen levelUpScreen = GetNode<LevelUpScreen>("../LevelUpScreen");
        GameOverScreen gameOverScreen = GetNode<GameOverScreen>("../GameOverScreen");
        ChestLootScreen chestLootScreen = GetNodeOrNull<ChestLootScreen>("../ChestLootScreen");

        hud.SetProgression(progression);
        hud.SetPlayer(player);

        levelUpScreen.SetFragmentManager(fragmentManager);
        levelUpScreen.SetPerkManager(perkManager);
        gameOverScreen.SetScoreManager(scoreManager);

        if (chestLootScreen != null)
            player.ConfigureLoot(chestLootScreen, perkManager);

        Node sceneRoot = GetNode("..");
        ErasureManager erasureManager = new() { Name = "ErasureManager" };
        sceneRoot.AddChild(erasureManager);

        EssenceTracker essenceTracker = new() { Name = "EssenceTracker" };
        sceneRoot.AddChild(essenceTracker);

        PerilManager perilManager = new() { Name = "PerilManager" };
        sceneRoot.AddChild(perilManager);

        CrisisManager crisisManager = new() { Name = "CrisisManager" };
        sceneRoot.AddChild(crisisManager);

        RunEventDirector runEventDirector = new() { Name = "RunEventDirector" };
        sceneRoot.AddChild(runEventDirector);

        EndgameManager endgameManager = new() { Name = "EndgameManager" };
        sceneRoot.AddChild(endgameManager);

        ChoiceScreen choiceScreen = new() { Name = "ChoiceScreen" };
        sceneRoot.AddChild(choiceScreen);

        MemorialDirector memorialDirector = new() { Name = "MemorialDirector" };
        memorialDirector.Setup(choiceScreen, essenceTracker, erasureManager, perilManager);
        sceneRoot.AddChild(memorialDirector);

        RiftDirector riftDirector = new() { Name = "RiftDirector" };
        riftDirector.Setup(choiceScreen, perilManager, erasureManager);
        sceneRoot.AddChild(riftDirector);

        QuestManager questManager = new() { Name = "QuestManager" };
        sceneRoot.AddChild(questManager);

        hud.SetErasureManager(erasureManager);
        hud.SetEssenceTracker(essenceTracker);

        InitializeCharacterAndRun(player, perkManager, scoreManager, runTracker);

        if (progression.CurrentLevel > 1 && fragmentManager.PendingChoices.Count == 0)
        {
            GD.Print($"[GameBootstrap] Catching up missed level-ups: player is level {progression.CurrentLevel}");
            fragmentManager.TriggerLevelUp(progression.CurrentLevel);
        }

        Combat.CombatPools combatPools = new() { Name = "CombatPools" };
        GetNode("..").CallDeferred("add_child", combatPools);

        Combat.ScreenShake screenShake = new() { Name = "ScreenShake" };
        screenShake.SetCamera(player.GetNode<Camera2D>("Camera"));
        GetNode("..").CallDeferred("add_child", screenShake);

        Combat.CrowdZoom crowdZoom = new() { Name = "CrowdZoom" };
        crowdZoom.SetCamera(player.GetNode<Camera2D>("Camera"));
        GetNode("..").CallDeferred("add_child", crowdZoom);

        AmbientParticles ambientParticles = new() { Name = "AmbientParticles" };
        GetNode("..").CallDeferred("add_child", ambientParticles);

        GetNode("..").CallDeferred("add_child", new ErasureEchoes { Name = "ErasureEchoes" });
        GetNode("..").CallDeferred("add_child", new ErasureMotes { Name = "ErasureMotes" });
        GetNode("..").CallDeferred("add_child", new PoiGlints { Name = "PoiGlints" });
        GetNode("..").CallDeferred("add_child", new Events.CrisisAftermath { Name = "CrisisAftermath" });

        EventBus eventBus = GetNode<EventBus>("/root/EventBus");
        Player levelUpPlayer = player;
        GroupCache groupCache = GetNode<GroupCache>("/root/GroupCache");
        eventBus.LevelUp += (int _level) =>
        {
            if (IsInstanceValid(levelUpPlayer))
            {
                Combat.LevelUpFx.Play(levelUpPlayer, groupCache);
                Combat.ScreenShake.Instance?.ShakeMedium();
            }
        };

        GetNode("..").CallDeferred("add_child", new ErasureVeil { Name = "ErasureVeil" });
        GetNode("..").CallDeferred("add_child", new CrisisOmen { Name = "CrisisOmen" });

        DebugActionPanel debugPanel = new DebugActionPanel { Name = "DebugActionPanel" };
        GetNode("..").CallDeferred("add_child", debugPanel);

        if (SteamManager.IsActive)
            AddSteamServices();

        GD.Print($"[GameBootstrap] Run started with {player.CharacterId}");

        // --- Dépause et fade-out de l'overlay ---
        GetTree().Paused = false;
        LoadProfiler.Mark("systèmes de la run");
        overlay.FadeOut();
    }

    // Hors méthode principale : ces types nomment Steamworks, dont l'assemblage ne se charge pas hors x86/x64.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddSteamServices()
    {
        SteamAchievements steamAchievements = new() { Name = "SteamAchievements" };
        GetNode("..").CallDeferred("add_child", steamAchievements);

        SteamLeaderboards steamLeaderboards = new() { Name = "SteamLeaderboards" };
        GetNode("..").CallDeferred("add_child", steamLeaderboards);
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
