using System;
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
/// Le chargement a trois issues (plan 26 Q4) : réussite, échec affiché avec un retour au camp, ou abandon quand la
/// scène est quittée en route.
/// </summary>
public partial class GameBootstrap : Node
{
    private const string FallbackCharacterId = "vagabond";

    private const string HubScenePath = "res://scenes/Hub.tscn";

    private enum LoadState { Running, Succeeded, Failed, Abandoned }

    private EventBus _eventBus;
    private Player _levelUpPlayer;
    private LoadState _loadState = LoadState.Running;
    private string _loadStep = "";
    private GameLoadingOverlay _overlay;

    public override void _Ready()
    {
        // L'écran de chargement entre dans l'arbre avant le premier rendu de la scène : sans lui, la toute première
        // frame montrait le HUD et un monde vide.
        _overlay = new GameLoadingOverlay { Name = "GameLoadingOverlay", ProcessMode = ProcessModeEnum.Always };
        GetParent().CallDeferred(Node.MethodName.AddChild, _overlay);
        _ = RunLoadAsync();
    }

    /// <summary>Seule entrée du chargement : aucune exception n'en sort, chaque issue est réglée ici.</summary>
    private async Task RunLoadAsync()
    {
        try
        {
            await SetupNormalGameAsync();
            _loadState = LoadState.Succeeded;
        }
        catch (Exception exception) when (exception is LoadAbandonedException || !LoadGuard.IsAlive(this))
        {
            _loadState = LoadState.Abandoned;
            GD.Print($"[GameBootstrap] Chargement abandonné à l'étape « {_loadStep} » : la scène a été quittée.");
        }
        catch (Exception exception)
        {
            _loadState = LoadState.Failed;
            GD.PushError($"[GameBootstrap] Chargement en échec à l'étape « {_loadStep} » : {exception.GetType().Name} : {exception.Message}\n{exception}");
            // L'arbre reste en pause derrière l'écran d'erreur, qui fonctionne en pause : la run à moitié construite
            // ne s'anime pas, et le bouton rend la main.
            ShowFailureOrLeave(exception.Message);
        }
    }

    /// <summary>Écran d'erreur ; s'il ne peut pas se construire, retour direct au camp plutôt qu'une pause sans issue.</summary>
    private void ShowFailureOrLeave(string message)
    {
        try
        {
            if (IsInstanceValid(_overlay))
            {
                _overlay.ShowFailure(_loadStep, message, ReturnToHub);
                return;
            }
        }
        catch (Exception exception)
        {
            GD.PushError($"[GameBootstrap] Écran d'erreur impossible, retour au camp : {exception.Message}");
        }
        ReturnToHub();
    }

    private static void RequireCatalog(bool valid, string error)
    {
        if (!valid)
            throw new InvalidOperationException($"catalogue refusé, {error}");
    }

    private void ReturnToHub()
    {
        if (!LoadGuard.IsAlive(this))
            return;
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile(HubScenePath);
    }

    /// <summary>Nomme l'étape en cours pour le journal et l'écran d'erreur, et fait avancer l'écran de chargement.</summary>
    private void Step(string text)
    {
        _loadStep = text.TrimEnd('.');
        if (IsInstanceValid(_overlay))
            _overlay.SetProgress(text);
    }

    private async Task SetupNormalGameAsync()
    {
        // Le parent termine ses _Ready avant de recevoir les nœuds de warmup ; l'overlay s'anime pendant la pause.
        GetTree().Paused = true;

        Step("Catalogues");
        LoadGuard.InjectFault("catalogues");
        CharacterDataLoader.Load();
        WeaponDataLoader.Load();
        WeaponUpgradeDataLoader.Load();
        PerkDataLoader.Load();
        PassiveSouvenirDataLoader.Load();
        MetaSaveManager.Load();
        SouvenirDataLoader.Load();
        // Catalogues contrôlés en entier (plan 26 Q7) : un refus arrête le chargement sur l'écran d'erreur, avec son message.
        RequireCatalog(XpCurveConfig.TryLoad(out _, out string xpError), xpError);
        RequireCatalog(ScoreConfig.TryLoad(out _, out string scoreError), scoreError);
        RequireCatalog(PerilDataLoader.TryLoad(out string perilError), perilError);
        RequireCatalog(PassiveSouvenirDataLoader.TryLoad(out string objectsError), objectsError);
        RequireCatalog(UpgradeRoller.TryLoad(out string raritiesError), raritiesError);
        RequireCatalog(LevelUpOfferConfig.TryLoad(out _, out string offerError), offerError);
        RequireCatalog(LandmarkDataLoader.TryLoad(out string landmarksError), landmarksError);
        RequireCatalog(BlessingDataLoader.TryLoad(out string blessingsError), blessingsError);
        RequireCatalog(OubliDataLoader.TryLoad(out string oublisError), oublisError);
        RequireCatalog(ChestDataLoader.TryLoad(out string chestsError), chestsError);
        RequireCatalog(FieldBonusDataLoader.TryLoad(out string bonusesError), bonusesError);
        RequireCatalog(SmallPlaceDataLoader.TryLoad(out string placesError), placesError);
        RequireCatalog(WaymarkDataLoader.TryLoad(out string waymarksError), waymarksError);
        RequireCatalog(BiomeDataLoader.TryLoad(out string biomesError), biomesError);

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

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        LoadGuard.EnsureAlive(this);

        // Le viewport de préchauffage rend réellement ses échantillons, sous l'écran de chargement.
        Step("Préparation des shaders...");
        await ShaderWarmup.RenderAsync(this);
        LoadGuard.EnsureAlive(this);
        LoadProfiler.Mark("shaders");

        // --- Initialisation du monde (étalée sur plusieurs frames) ---
        WorldSetup worldSetup = GetNode<WorldSetup>("..");
        await worldSetup.InitializeWorldAsync(Step);
        LoadGuard.EnsureAlive(this);

        // --- Prewarm EnemyPool (étalé) ---
        Step("Préparation des créatures...");
        EnemyPool enemyPool = GetNode<EnemyPool>("../EnemyPool");
        await enemyPool.PrewarmAsync(4);
        LoadGuard.EnsureAlive(this);
        LoadProfiler.Mark("créatures du pool");
        await PreloadEnemySprites();
        LoadProfiler.Mark("animations des créatures");

        // --- Wire des systèmes (rapide, synchrone) ---
        Step("Initialisation...");

        HUD hud = GetNode<HUD>("../HUD");
        LevelUpScreen levelUpScreen = GetNode<LevelUpScreen>("../LevelUpScreen");
        GameOverScreen gameOverScreen = GetNode<GameOverScreen>("../GameOverScreen");
        ChestLootScreen chestLootScreen = GetNodeOrNull<ChestLootScreen>("../ChestLootScreen");

        hud.SetProgression(progression);
        hud.SetPlayer(player);

        levelUpScreen.SetFragmentManager(fragmentManager);
        gameOverScreen.SetScoreManager(scoreManager);
        gameOverScreen.SetRunTracker(runTracker);

        if (chestLootScreen != null)
            player.ConfigureLoot(chestLootScreen);

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

        LandmarkReveal landmarkReveal = new() { Name = "LandmarkReveal" };
        landmarkReveal.Setup(player.GetNode<Camera2D>("Camera"));
        sceneRoot.AddChild(landmarkReveal);

        MemorialDirector memorialDirector = new() { Name = "MemorialDirector" };
        memorialDirector.Setup(choiceScreen, landmarkReveal, essenceTracker, erasureManager, perilManager);
        sceneRoot.AddChild(memorialDirector);

        WorkshopDirector workshopDirector = new() { Name = "WorkshopDirector" };
        workshopDirector.Setup(choiceScreen, essenceTracker, erasureManager);
        sceneRoot.AddChild(workshopDirector);

        RiftDirector riftDirector = new() { Name = "RiftDirector" };
        riftDirector.Setup(choiceScreen, landmarkReveal, perilManager, erasureManager);
        sceneRoot.AddChild(riftDirector);

        // Petits lieux (plan 22, lot C1) : des décors déjà posés qui se souviennent.
        SmallPlaceDirector smallPlaces = new() { Name = "SmallPlaceDirector" };
        smallPlaces.Setup(GetNode<Vestiges.Spawn.SpawnManager>("../SpawnManager"), erasureManager, worldSetup.Seed);
        sceneRoot.AddChild(smallPlaces);
        smallPlaces.PlacePlaces(GetNode("../PropContainer"), GetNode<Node2D>("../PoiContainer"));

        // Bonus lâchés (plan 24 C4) : gourde, aimant, couverture, café, pétard.
        sceneRoot.AddChild(new FieldBonusDirector { Name = "FieldBonusDirector" });

        QuestManager questManager = new() { Name = "QuestManager" };
        sceneRoot.AddChild(questManager);

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

        // La mort se joue dans le monde avant le bilan (plan 02 M1).
        DeathSequence deathSequence = new() { Name = "DeathSequence" };
        deathSequence.Setup(player.GetNode<Camera2D>("Camera"), screenShake, crowdZoom);
        deathSequence.Finished += gameOverScreen.Reveal;
        GetNode("..").CallDeferred("add_child", deathSequence);

        AmbientParticles ambientParticles = new() { Name = "AmbientParticles" };
        GetNode("..").CallDeferred("add_child", ambientParticles);

        GetNode("..").CallDeferred("add_child", new ErasureEchoes { Name = "ErasureEchoes" });
        GetNode("..").CallDeferred("add_child", new ErasureMotes { Name = "ErasureMotes" });
        GetNode("..").CallDeferred("add_child", new PoiGlints { Name = "PoiGlints" });
        GetNode("..").CallDeferred("add_child", new Events.CrisisAftermath { Name = "CrisisAftermath" });

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _levelUpPlayer = player;
        _eventBus.LevelUp += OnLevelUp;

        GetNode("..").CallDeferred("add_child", new ErasureVeil { Name = "ErasureVeil" });
        GetNode("..").CallDeferred("add_child", new CrisisOmen { Name = "CrisisOmen" });
        GetNode("..").CallDeferred("add_child", new GrassTrample { Name = "GrassTrample" });

#if TOOLS
        if (DevelopmentMode.IsEnabled)
            AddDevelopmentTools();
#endif

        if (SteamManager.IsActive)
            AddSteamServices();

        GD.Print($"[GameBootstrap] Run started with {player.CharacterId}");

        // --- Dépause et fade-out de l'overlay ---
        GetTree().Paused = false;
        LoadProfiler.Mark("systèmes de la run");
        _overlay.FadeOut();
    }

    /// <summary>
    /// Animations de toutes les espèces chargées sous l'écran de chargement, une par image : chargées à la première
    /// apparition, elles coûtaient 16 à 25 ms chacune, une image sautée à chaque nouvelle espèce en pleine partie.
    /// </summary>
    private async Task PreloadEnemySprites()
    {
        foreach (string id in EnemyDataLoader.GetAllIds())
        {
            EnemyData data = EnemyDataLoader.Get(id);
            if (string.IsNullOrEmpty(data?.Visual.SpriteFolder))
                continue;
            Combat.EnemySpriteLoader.LoadOrGet(id, data.Visual.SpriteFolder);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            LoadGuard.EnsureAlive(this);
        }
    }

    public override void _ExitTree()
    {
        // Scène quittée en plein chargement (retour au Hub, fermeture) : la pause posée par le chargement ne doit pas
        // geler la scène suivante.
        if (_loadState == LoadState.Running)
            GetTree().Paused = false;
        // L'EventBus survit à la run : sans désabonnement, chaque run laisserait un rappel de plus.
        if (_eventBus != null)
            _eventBus.LevelUp -= OnLevelUp;
    }

    private void OnLevelUp(int newLevel)
    {
        if (!IsInstanceValid(_levelUpPlayer))
            return;
        Combat.LevelUpFx.Play(_levelUpPlayer);
        Combat.ScreenShake.Instance?.ShakeMedium();
    }

#if TOOLS
    private void AddDevelopmentTools()
    {
        GetNode("..").CallDeferred("add_child", new DebugOverlay { Name = "DebugOverlay" });
        GetNode("..").CallDeferred("add_child", new DebugActionPanel { Name = "DebugActionPanel" });
    }
#endif

    // Hors méthode principale : ces types nomment Steamworks, dont l'assemblage ne se charge pas hors x86/x64.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddSteamServices()
    {
        SteamAchievements steamAchievements = new() { Name = "SteamAchievements" };
        GetNode("..").CallDeferred("add_child", steamAchievements);
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
