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
    private const string FallbackCharacterId = "traqueur";

    private const string HubScenePath = "res://scenes/Hub.tscn";

    private enum LoadState { Running, Succeeded, Failed, Abandoned }

    private EventBus _eventBus;
    private Player _levelUpPlayer;
    private LoadState _loadState = LoadState.Running;
    private string _loadStep = "";
    private GameLoadingOverlay _overlay;

    /// <summary>
    /// Chargement déjà en échec. Une panne aux catalogues survient pendant le _Ready de ce nœud, avant celui de
    /// <see cref="WorldSetup"/> : la génération ne doit pas partir derrière l'écran d'erreur.
    /// </summary>
    public bool HasFailed => _loadState == LoadState.Failed;

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
        RequireCatalog(PerkDataLoader.TryLoad(out string perksError), perksError);
        RequireCatalog(CharacterDataLoader.TryLoad(out string charactersError), charactersError);
        // Les armes contrôlent leurs stats montables contre ce fichier.
        RequireCatalog(WeaponUpgradeDataLoader.TryLoad(out string upgradesError), upgradesError);
        WeaponDataLoader.Load();
        PassiveSouvenirDataLoader.Load();
        MetaSaveManager.Load();
        RequireCatalog(SouvenirDataLoader.TryLoad(out string souvenirsError), souvenirsError);
        RequireCatalog(LootTableLoader.TryLoad(out string lootError), lootError);
        RequireCatalog(PoiDataLoader.TryLoad(out string poisError), poisError);
        // Catalogues contrôlés en entier (plan 26 Q7) : un refus arrête le chargement sur l'écran d'erreur, avec son message.
        RequireCatalog(XpCurveConfig.TryLoad(out _, out string xpError), xpError);
        RequireCatalog(ScoreConfig.TryLoad(out _, out string scoreError), scoreError);
        RequireCatalog(PerilDataLoader.TryLoad(out string perilError), perilError);
        RequireCatalog(DifficultyDataLoader.TryLoad(out string difficultyError), difficultyError);
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
        RequireCatalog(WorldGenConfig.TryLoad(out _, out string worldError), worldError);
        RequireCatalog(FarmConfig.TryLoad(out _, out string farmsError), farmsError);
        RequireCatalog(CrisisConfig.TryLoad(out _, out string crisesError), crisesError);
        RequireCatalog(ErasureConfig.TryLoad(out _, out string erasureError), erasureError);
        RequireCatalog(RunEventDataLoader.TryLoad(out string runEventsError), runEventsError);
        RequireCatalog(SpawnFlowConfig.TryLoad(out _, out string spawnFlowError), spawnFlowError);
        RequireCatalog(CrowdDataLoader.TryLoad(out string crowdError), crowdError);
        RequireCatalog(EnemyVariantDataLoader.TryLoad(out string variantsError), variantsError);
        RequireCatalog(SitePlan.TryLoad(WorldSetup.QuarrySitesPath, "collapsed_quarry", WorldSetup.QuarryFolder, "chantiers",
            out _, out string quarryError), quarryError);
        RequireCatalog(CampUpgradeDataLoader.TryLoad(out string campError), campError);
        RequireCatalog(InventoryConfig.TryLoad(out string inventoryError), inventoryError);
        RequireCatalog(ItemLootConfig.TryLoad(out string itemLootError), itemLootError);

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
        await PreloadRunAssets();
        LoadProfiler.Mark("effets et scripts de la run");

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
        PauseMenu pauseMenu = GetNode<PauseMenu>("../PauseMenu");
        JournalScreen journal = GetNode<JournalScreen>("../JournalScreen");
        pauseMenu.JournalRequested += journal.Show;
        journal.Closed += pauseMenu.RestoreAfterJournal;

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

        // Sur les terres (plan 51), le gardien de chaque terre remplace la Barrière.
        if (!worldSetup.Land.IsLand)
            sceneRoot.AddChild(new BarrierDirector { Name = "BarrierDirector" });

        ChoiceScreen choiceScreen = new() { Name = "ChoiceScreen" };
        sceneRoot.AddChild(choiceScreen);
        player.ConfigureLoot(chestLootScreen, essenceTracker, choiceScreen);

        // Objets du monde (plan 35 I3) : objets lâchés par les élites et les Souverains.
        ItemDropDirector itemDrops = new() { Name = "ItemDropDirector" };
        itemDrops.Setup(GetNode("../PoiContainer"));
        sceneRoot.AddChild(itemDrops);

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

        // Puits (plan 44) : le seul lieu où l'on oublie une arme, un Écusson ou un objet.
        WellDirector wellDirector = new() { Name = "WellDirector" };
        wellDirector.Setup(choiceScreen, essenceTracker, erasureManager);
        sceneRoot.AddChild(wellDirector);
        sceneRoot.AddChild(riftDirector);

        // Petits lieux (plan 22, lot C1) : des décors déjà posés qui se souviennent.
        SmallPlaceDirector smallPlaces = new() { Name = "SmallPlaceDirector" };
        smallPlaces.Setup(GetNode<Vestiges.Spawn.SpawnManager>("../SpawnManager"), erasureManager, worldSetup.LandSeed);
        sceneRoot.AddChild(smallPlaces);
        // Passage d'une terre à l'autre (plan 51) ; il pose aussi les détails des décors, refaits à chaque terre.
        LandTransition landTransition = new() { Name = "LandTransition" };
        landTransition.Setup(worldSetup, player, smallPlaces, erasureManager, GetNodeOrNull<CanvasModulate>("../CanvasModulate"));
        sceneRoot.AddChild(landTransition);
        landTransition.PlaceLandDetails();
        // Gardien de chaque terre (plan 51 P3) : seulement quand les terres sont allumées.
        if (worldSetup.Land.IsLand)
        {
            GuardianDirector guardian = new() { Name = "GuardianDirector" };
            guardian.Setup(worldSetup, erasureManager, memorialDirector, landTransition,
                GetNode<Vestiges.Spawn.SpawnManager>("../SpawnManager"), perilManager);
            sceneRoot.AddChild(guardian);
        }
        LoadProfiler.Mark("directeurs et petits lieux");
        // Jeu toujours en pause : rendre la main ici coupe la dernière image figée du chargement en deux (plan 37 R5).
        await NextLoadingFrame();

        // Bonus lâchés (plan 24 C4) : gourde, aimant, couverture, café, pétard.
        sceneRoot.AddChild(new FieldBonusDirector { Name = "FieldBonusDirector" });

        // Collectes de la jeune génération rapprochées : pas de pause GC visible (plan 29).
        sceneRoot.AddChild(new GcPacer { Name = "GcPacer" });

        // Quêtes de déblocage (plan 06 §9) : suivies sans rien afficher, enregistrées dès qu'elles sont accomplies.
        sceneRoot.AddChild(new QuestTracker { Name = "QuestTracker" });

        hud.SetEssenceTracker(essenceTracker);

        InitializeCharacterAndRun(player, perkManager, scoreManager, runTracker, fragmentManager, essenceTracker, perilManager);
        LoadProfiler.Mark("personnage et run");

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
        if (DevelopmentMode.IsEnabled || DevelopmentMode.IsTestSession)
            AddDevelopmentTools();
        // Trace de survie des vraies parties (plan 42 M0e) ; les bancs ont leur propre relevé.
        if (!DevelopmentMode.IsTestSession)
        {
            PlayTraceRecorder recorder = new() { Name = "PlayTraceRecorder" };
            recorder.Setup(player);
            GetNode("..").CallDeferred("add_child", recorder);
        }
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

    /// <summary>
    /// Ce que la run chargeait sinon à son premier usage, en pleine partie (plan 29) : habitants des échos de
    /// l'Effacement (jusqu'à 130 ms dans les premières secondes), planches des projectiles, effet de mort, et scripts C#
    /// des effets nés en cours de run (≈ 50 ms pour l'anneau d'affixe au début de la première Résurgence). Une ressource
    /// par image, comme les créatures.
    /// </summary>
    private async Task PreloadRunAssets()
    {
        EchoConfig echoes = EchoConfig.Load();
        if (echoes.Enabled)
        {
            foreach (string id in echoes.Inhabitants)
            {
                Combat.CharacterSpriteLoader.LoadOrGet(id, id);
                await NextLoadingFrame();
            }
        }
        Combat.ProjectileSprites.Get("arrow");
        await NextLoadingFrame();
        Combat.DeathFx.Create(null).Free();
        await NextLoadingFrame();
        // Charger le script et compiler son constructeur : un nœud créé puis libéré, jamais ajouté à l'arbre.
        Func<Node>[] scripts =
        {
            () => new Combat.PixelGroundRing(), () => new Combat.EnemyStatusMarks(), () => new Combat.Abilities.GroundTelegraph(),
            () => new Combat.PixelFx(), () => new Combat.TickRosterDriver(), () => new GroundShadowLayer(), () => new MapLegendIcon(),
        };
        foreach (Func<Node> create in scripts)
        {
            create().Free();
            await NextLoadingFrame();
        }
    }

    private async Task NextLoadingFrame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        LoadGuard.EnsureAlive(this);
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
        GetNode("..").CallDeferred("add_child", new Development.DevelopmentRunTools { Name = "DevelopmentRunTools" });
        if (!DevelopmentMode.IsEnabled)
            return;
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
        ScoreManager scoreManager, RunTracker runTracker, FragmentManager fragmentManager, EssenceTracker essenceTracker,
        PerilManager perilManager)
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
        // Le Campement (plan 36) : ce qui a été acheté au Hub vaut pour toute la run. Le FragmentManager, ajouté en différé,
        // a passé son _Ready (bannissements de base) lors des attentes du chargement : ses ajouts ne sont pas écrasés.
        CampUpgrades.Totals camp = CampUpgrades.Apply(player, fragmentManager, essenceTracker, MetaSaveManager.GetCampRanks(),
            DevelopmentMode.IsEnabled);
        // Niveau de difficulté (plan 50 T3) : Péril de départ, visible au HUD, et Vestiges de fin de run.
        DifficultyLevel difficulty = DifficultyDataLoader.Get(gm.DifficultyId);
        scoreManager.VestigeMultiplier = (1f + camp.VestigeGain) * difficulty.Vestiges;
        perilManager.AddPeril(difficulty.StartPeril);
        perkManager.ApplyPassivePerks(data.Id);
        scoreManager.SetRunTracker(runTracker);
        gm.ActiveMutators = new System.Collections.Generic.List<string>();

        gm.ChangeState(GameManager.GameState.Run);
    }
}
