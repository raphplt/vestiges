using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World.Lore;

namespace Vestiges.World;

/// <summary>
/// Génère le monde procédural au lancement : terrain via WorldGenerator,
/// puis place POI, coffres, décor et éléments de lore.
/// </summary>
public partial class WorldSetup : Node2D
{
    private TileMapLayer _ground;
    private TileMapLayer _roadOverlay;
    private RoadTiles _roadTiles;
    private Node2D _poiContainer;
    private HashSet<Vector2I> _usedCells = new();
    private PoiManager _poiManager;
    private FogOfWar _fogOfWar;
    private PropSpawner _propSpawner;
    private BiomeAtmosphere _biomeAtmosphere;

    private WorldGenerator _generator;
    private WorldGenConfig _config;
    private LandSpec _land;
    private TileSet _baseTileSet;
    // Nœuds propres à la carte en cours, libérés au changement de terre (plan 51) ; le reste de Main porte la run.
    private readonly List<Node> _landNodes = new();
    private BiomeTileMapper _tileMapper;

    /// <summary>Seed de la run : celle du Hub, ou tirée à l'entrée dans l'arbre (voir <see cref="_EnterTree"/>).</summary>
    public ulong Seed { get; set; }

    /// <summary>Terre en cours (plan 51) : la grande carte de la run tant que les terres sont éteintes.</summary>
    public LandSpec Land => _land;

    /// <summary>Graine de la carte en cours : celle de la run sur la première terre, dérivée de son rang ensuite.</summary>
    public ulong LandSeed => _land.Seed;

    /// <summary>Si true, aucun POI n'est généré (mutateur "Isolement"). Les coffres restent.</summary>
    public bool PoisDisabled { get; set; }

    /// <summary>Référence publique au générateur pour les autres systèmes.</summary>
    public WorldGenerator Generator => _generator;
    /// <summary>Emprise des chemins, réutilisée par la cartographie sans parcourir les décors.</summary>
    public IReadOnlySet<Vector2I> PathCells => _pathCells;

    /// <summary>Checks if a world position is impassable (water or erased void).</summary>
    public bool IsWaterAt(Vector2 worldPos)
    {
        if (_generator == null || _ground == null)
            return false;
        Vector2I cell = _ground.LocalToMap(_ground.ToLocal(worldPos));
        if (_generator.IsErased(cell.X, cell.Y))
            return true;
        return _generator.GetTerrain(cell.X, cell.Y) == TerrainType.Water;
    }

    /// <summary>Retourne le BiomeData à une position monde, ou null.</summary>
    public BiomeData GetBiomeAt(Vector2 worldPos)
    {
        if (_generator == null || _ground == null)
            return null;
        Vector2I cell = _ground.LocalToMap(_ground.ToLocal(worldPos));
        return _generator.GetBiome(cell.X, cell.Y);
    }

    // Données générées en _Ready() (CPU rapide), consommées par InitializeWorldAsync()
    private TerrainType[,] _terrain;
    private UrbanLayout _urbanLayout;
    private WildFieldsLayout _wildFieldsLayout;
    // Zones du terrain rythmé par cellule (plan 58 T1a), nul si aucun biome actif n'a de rythme.
    private byte[,] _rhythmZones;
    private PathNetwork _pathNetwork;
    // Gardée après la construction : la cartographie n'échantillonne une cellule qu'à sa découverte, bien plus tard.
    private IReadOnlySet<Vector2I> _pathCells;
    private FarmConfig _farmConfig;
    private List<CompositionSite> _farms = new();
    private SitePlan _quarryPlan;
    private List<CompositionSite> _quarrySites = new();
    private List<Vector2> _storyScenes = new();
    private List<ExodusScenePlacer.Scene> _exodusScenes = new();
    private List<PlacedPiece> _pieces = new();

    /// <summary>Pièces maîtresses de la terre en cours, cœur compris (plan 58) ; aucune sur la grande carte.</summary>
    public IReadOnlyList<PlacedPiece> Pieces => _pieces;

    /// <summary>Positions des scènes-récits des champs (plan 08 P4b-4).</summary>
    public IReadOnlyList<Vector2> StoryScenes => _storyScenes;
    /// <summary>Ancrage des scènes de l'exode dans les rues (plan 39 B3).</summary>
    public IReadOnlyList<ExodusScenePlacer.Scene> ExodusScenes => _exodusScenes;

    /// <summary>Centres des cours de ferme des Champs Sauvages (plan 08 P4b).</summary>
    public IEnumerable<Vector2> FarmAnchors
    {
        get
        {
            foreach (CompositionSite farm in _farms)
                yield return farm.Anchor;
        }
    }

    /// <summary>Centres des chantiers de la Carrière Effondrée (entrée de galerie, voie, baraque).</summary>
    public IEnumerable<Vector2> QuarrySiteAnchors
    {
        get
        {
            foreach (CompositionSite site in _quarrySites)
                yield return site.Anchor;
        }
    }

    /// <summary>Indique si l'initialisation async est terminée.</summary>
    public bool IsWorldReady { get; private set; }

    /// <summary>Rectangle monde qui contient toute la carte (minimap) : des coins de la grille, d'une cellule de marge.</summary>
    public Rect2 WorldBounds
    {
        get
        {
            Vector2 min = _ground.MapToLocal(new Vector2I(-_config.MapRadius - 1, -_config.MapRadiusY - 1));
            Vector2 max = _ground.MapToLocal(new Vector2I(_config.MapRadius + 1, _config.MapRadiusY + 1));
            return new Rect2(_ground.GlobalPosition + min, max - min);
        }
    }

    /// <summary>
    /// La seed est fixée à l'entrée dans l'arbre, avant le _Ready des enfants (le joueur, ses objets) : tous les flux de
    /// tirage de la run en dérivent (plan 26 Q8c).
    /// </summary>
    public override void _EnterTree()
    {
        // Seed du Hub, ou 0 = aléatoire.
        GameManager gm = GetNodeOrNull<GameManager>("/root/GameManager");
        if (gm != null && gm.RunSeed != 0)
            Seed = gm.RunSeed;
        while (Seed == 0)
            Seed = GD.Randi();
        // Publiée tout de suite : le record, l'historique et le bilan gardent la carte d'une run aléatoire.
        if (gm != null)
            gm.EffectiveSeed = Seed;
        RunRandom.Begin(Seed);
    }

    public override void _Ready()
    {
        LoadProfiler.Begin();
        // Les décors de la run précédente ne bloquent plus les créatures ; ceux de celle-ci vont s'inscrire.
        ObstacleField.Clear();
        EnemyDataLoader.Load();
        // Le bootstrap affiche le diagnostic ; ne pas lancer de génération sur un catalogue refusé. Une génération
        // lancée derrière l'écran d'erreur tournerait encore sur son thread après le retour au camp.
        if (GetNodeOrNull<GameBootstrap>("GameBootstrap") is { HasFailed: true } || !BiomeDataLoader.TryLoad(out _))
            return;
        PoiDataLoader.Load();
        LootTableLoader.Load();
        ChestDataLoader.Load();

        _ground = GetNode<TileMapLayer>("Ground");
        _poiContainer = GetNode<Node2D>("PoiContainer");

        // Réglages du monde refusés : même règle que les biomes, le bootstrap affiche le diagnostic.
        if (!WorldGenConfig.TryLoad(out _config, out _))
            return;
        _land = LandSpec.For(0, _config, Seed);
        _land.ApplyTo(_config);
        PropRules.Current = _config.PropRules;
        // Ombres portées des décors (plan 39 A3) : réglées avant leur création. Fichier invalide : ombres de contact seules.
        // Paramètres › Ombres : désactivées, les décors ne créent même pas leur ombre (aucun coût).
        CombatFxSettings.PublishShadowQuality();
        if (CombatFxSettings.ShadowQuality != ShadowQuality.Off && AtmosphereConfig.TryLoad(out AtmosphereConfig atmosphere, out _))
        {
            PropShadow.CastPerHeight = atmosphere.CastShadowPerHeight;
            PropShadow.CastMinHeightPx = atmosphere.CastShadowMinHeightPx;
        }
        else
        {
            PropShadow.CastPerHeight = Vector2.Zero;
        }


        // Génération du monde (calcul pur) sur un thread : l'écran de chargement s'affiche et s'anime pendant ce temps.
        // Ses résultats ne sont lus qu'après l'attente, au début d'InitializeWorldAsync.
        _generation = Task.Run(GenerateWorld);
    }

    private Task _generation;
    /// <summary>Chantiers de la carrière (plan 08), contrôlés au chargement par <see cref="GameBootstrap"/>.</summary>
    public const string QuarrySitesPath = "res://data/world/quarry_sites.json";
    public const string QuarryFolder = "res://assets/props/collapsed_quarry/";
    private WorldGenerator _builtGenerator;
    private RandomNumberGenerator _loreRng;
    // Décors en attente, hors de l'arbre, entre les placeurs et le découpage en tronçons.
    private Node2D _propStaging;

    /// <summary>
    /// Terrain, biomes, plans de la ville, du marais et des champs, chemins, fermes et chantiers. Calcul pur, sans nœud
    /// ni ressource du moteur hors bruits, lectures de fichiers et ResourceLoader.Exists : peut tourner hors du thread
    /// principal. Chronométré étape par étape.
    /// </summary>
    private void GenerateWorld()
    {
        LoadGuard.InjectFault("génération");
        ulong step = Time.GetTicksUsec();
        // Générateur local, échangé sur le fil principal après l'attente : le joueur, les apparitions et la minimap lisent
        // Generator en continu, ils ne doivent jamais voir une carte à moitié construite (plan 51, changement de terre).
        WorldGenerator generator = new WorldGenerator(
            _config.MapRadius,
            _config.MapRadiusY,
            _config.SpawnClearance,
            _config.CaIterations,
            _config.Zones,
            LandSeed,
            _config.EdgeFadeWidth
        )
        {
            BiomeLayout = _config.BiomeLayout
        };

        // Générer le terrain (CPU pur, rapide)
        List<BiomeData> availableBiomes = LoadAvailableBiomes();
        // Une terre n'a qu'un biome (plan 51) ; sans biome du tout, la génération retombe sur le terrain générique.
        if (availableBiomes.Count >= 1)
            _terrain = generator.Generate(availableBiomes, _config.BiomeCount);
        else
            _terrain = generator.Generate();
        LoadProfiler.Span("terrain et biomes (thread)", ref step);
        // Clairières, fourrés… : le sol des zones se pose avant les chemins et les pièces, qui imposent ensuite le leur.
        _rhythmZones = TerrainRhythm.Apply(generator, _terrain, PropSpawner.LoadRhythms(), LandSeed);
        LoadProfiler.Span("terrain rythmé (thread)", ref step);

        // Layouts urbains et champs (CPU pur)
        if (generator.ActiveBiomes.Any(b => b.Id == "urban_ruins"))
        {
            UrbanLayoutGenerator urbanGen = new(LandSeed, _config.MapRadius, _config.MapRadiusY);
            _urbanLayout = urbanGen.Apply(_terrain, generator, "urban_ruins");
            _roadTiles = RoadTileGenerator.Build(_urbanLayout);
        }
        if (generator.ActiveBiomes.Any(b => b.Id == "wild_fields"))
        {
            WildFieldsLayoutGenerator wildFieldsGen = new(LandSeed, _config.MapRadius, _config.MapRadiusY);
            _wildFieldsLayout = wildFieldsGen.Apply(_terrain, generator, "wild_fields");
        }

        LoadProfiler.Span("plans de la ville, de ses chaussées et des champs (thread)", ref step);
        // Chemins de terre entre les régions, raccordés aux rues (plan 10 T3) : rien ne pousse dessus.
        _pathNetwork = PathNetworkGenerator.Build(generator, _terrain, _urbanLayout, _wildFieldsLayout, _rhythmZones,
                                                  _config.Paths, _config.BiomeLayout.RegionSpacing, LandSeed);
        LoadProfiler.Span("chemins (thread)", ref step);

        // Pièces maîtresses de la terre (plan 58) : avant les fermes et les chantiers, qui s'écartent de leur emprise.
        _pieces = _land.IsLand && _land.MemorialBand is Vector2 band
            ? LandPieceComposer.Plan(generator, _terrain, _pathNetwork, _usedCells, LandPieceConfig.Load(), band,
                GuardianConfig.Load(out _)?.LairOffset ?? Vector2.Zero, LandSeed)
            : new List<PlacedPiece>();
        LoadProfiler.Span("pièces de la terre (thread)", ref step);
        // Fermes des champs (plan 08 P4b) : cellules réservées avant les points d'intérêt, embranchement vers un chemin.
        _farmConfig = FarmConfig.Load();
        _farms = SiteComposer.Plan(generator, _pathNetwork, _usedCells, _farmConfig.Site, LandSeed);
        // Chantiers de la carrière : galerie étayée, voie et wagonnets, baraque et machines groupées.
        _quarryPlan = SitePlan.Load(QuarrySitesPath, "collapsed_quarry", QuarryFolder, "chantiers");
        _quarrySites = SiteComposer.Plan(generator, _pathNetwork, _usedCells, _quarryPlan, LandSeed);
        // Tous les chemins tracés, raccords compris : ceux qui franchissent un chenal y reçoivent une passerelle.
        PathNetworkGenerator.BridgeChannels(_pathNetwork, _terrain, _rhythmZones, generator);
        LoadProfiler.Span("fermes et chantiers (thread)", ref step);
        _builtGenerator = generator;

    }

    /// <summary>TileSet dupliqué, correspondance des tuiles et routes générées : sur le thread principal, après la génération.</summary>
    private void PrepareTiles()
    {
        // Toujours depuis le TileSet de la scène : une terre suivante ne recopie pas l'atlas de la précédente.
        _baseTileSet ??= _ground.TileSet;
        _ground.TileSet = _baseTileSet.Duplicate() as TileSet;
        EnsureRoadOverlayLayer();
        _tileMapper = new BiomeTileMapper();
        _tileMapper.Initialize(_ground.TileSet, _generator.ActiveBiomes);

        // Chaussées de la ville : une tuile par motif de voisinage, ses variantes d'usure (plan 37 M3).
        int urbanIndex = _tileMapper.GetBiomeIndex("urban_ruins");
        if (_roadTiles != null && urbanIndex >= 0)
        {
            foreach (KeyValuePair<string, Image[]> group in _roadTiles.Groups)
                _tileMapper.RegisterRuntimeTileGroup(urbanIndex, group.Key, RoadTileGenerator.TexturesOf(group.Key, group.Value));
            _tileMapper.SetRoadTiles(_roadTiles);
            GD.Print($"[WorldSetup] Chaussées : {_roadTiles.Groups.Count} motifs pour {_roadTiles.Cells.Count} cellules");
        }
        if (_wildFieldsLayout != null)
            _tileMapper.SetWildFieldsLayout(_wildFieldsLayout);
        _tileMapper.BuildAtlas();
    }

    /// <summary>
    /// Initialisation lourde étalée sur plusieurs frames.
    /// Appelé par GameBootstrap après _Ready() de tous les nodes.
    /// Reporte la progression via onProgress(stepName).
    /// </summary>
    public async Task InitializeWorldAsync(Action<string> onProgress = null)
    {
        onProgress?.Invoke("Création du monde...");
        await BuildCurrentLandAsync(onProgress);
    }

    /// <summary>
    /// Construit la terre de rang <paramref name="index"/> à la place de la carte vidée par <see cref="ClearWorldAsync"/>
    /// (plan 51) : mêmes étapes que le premier chargement, génération sur un thread pendant que le jeu attend.
    /// </summary>
    public async Task BuildLandAsync(int index, Action<string> onProgress = null, string biomeId = null)
    {
        LoadProfiler.Begin();
        _config = WorldGenConfig.Load();
        _land = LandSpec.For(index, _config, Seed, biomeId);
        _land.ApplyTo(_config);
        _generation = Task.Run(GenerateWorld);
        await BuildCurrentLandAsync(onProgress);
    }

    private async Task BuildCurrentLandAsync(Action<string> onProgress)
    {
        CreateVoidBackground();

        await _generation;
        LoadGuard.EnsureAlive(this);
        _generator = _builtGenerator;
        _builtGenerator = null;
        PrepareTiles();
        LoadProfiler.Mark("génération attendue, tuiles préparées");
        await ApplyTerrainAsync(_terrain, _urbanLayout, onProgress);
        _landNodes.Add(WorldEdgeCollision.Build(this, _ground, _generator));
        LoadProfiler.Mark("tuiles et bord du monde");
        GroundMaterial.Apply(_ground, _roadOverlay, _generator, _tileMapper, _terrain, new Vector2I(_config.MapRadius, _config.MapRadiusY), _config.GroundBlend);
        AddPaths();
        LoadProfiler.Mark("matériau du sol et chemins");

        onProgress?.Invoke("Brouillard de guerre...");
        InitializeFog();
        await YieldFrame();
        LoadProfiler.Mark("brouillard");

        if (_config.PoisEnabled && !PoisDisabled)
        {
            onProgress?.Invoke("Points d'intérêt...");
            SpawnPois();
            await YieldFrame();
        }
        // Avant les décors : chaque coffre et chaque lieu réserve son dégagement.
        SpawnSites();
        LoadProfiler.Mark("points d'intérêt, coffres et lieux");

        onProgress?.Invoke("Décors...");
        await SpawnEnvironmentPropsAsync(_urbanLayout);
        LoadGuard.InjectFault("décors");
        LoadProfiler.Mark("décors des biomes");
        await YieldFrame();

        if (_urbanLayout != null)
        {
            _exodusScenes = UrbanPropPlacer.PlaceProps(_urbanLayout, _ground, _propStaging, _usedCells, LandSeed);
            LoadProfiler.Mark("décors de la ville");
            await YieldFrame();
        }
        await BuildPropOcclusionAsync();
        await YieldFrame();
        LoadProfiler.Mark("tronçons de décors");

        onProgress?.Invoke("Atmosphère...");
        if (_config.LoreElementsEnabled)
            SpawnLoreElements();
        // L'atmosphère suit le joueur de terre en terre : créée une fois, elle relit le biome sous ses pieds.
        if (_biomeAtmosphere == null)
            InitBiomeAtmosphere();
        await YieldFrame();

        // Libérer les refs temporaires
        _terrain = null;
        _urbanLayout = null;
        _roadTiles = null;
        _tileMapper.SetRoadTiles(null);
        _wildFieldsLayout = null;
        _pathCells = _pathNetwork?.Cells;
        _pathNetwork = null;

        IsWorldReady = true;
        LoadProfiler.Mark("atmosphère");
        GD.Print($"[WorldSetup] Terre {_land.Index} générée, graine {LandSeed} (run {Seed})");
    }

    /// <summary>
    /// Vide la carte en cours (plan 51, changement de terre) : nœuds de carte, décors, lieux, coffres et objets au sol,
    /// créatures rendues au pool, tuiles, obstacles. Le joueur et les systèmes qui portent la run ne sont pas touchés ;
    /// leur remise à zéro suit le signal de changement de terre.
    /// </summary>
    public async Task ClearWorldAsync()
    {
        IsWorldReady = false;
        Node enemies = GetNode("EnemyContainer");
        foreach (Node child in enemies.GetChildren())
        {
            if (child is Enemy enemy)
                enemy.Withdraw();
        }

        BeginLoadSlice();
        foreach (Node node in _landNodes)
        {
            if (IsInstanceValid(node))
                node.QueueFree();
        }
        _landNodes.Clear();
        // Décors par tronçons, lieux et objets au sol un par un : la libération s'étale sur plusieurs images.
        foreach (Node container in new[] { GetNode("PropContainer"), _poiContainer })
        {
            foreach (Node child in container.GetChildren())
            {
                child.QueueFree();
                if (LoadSliceSpent())
                    await YieldLoadSlice();
            }
        }
        _ground.Clear();
        _roadOverlay?.Clear();
        ObstacleField.Clear();
        _usedCells.Clear();
        _generator = null;

        // Une image libère ce qui a été mis en file : les index statiques se vident à la sortie de chaque lieu.
        await YieldFrame();
        GD.Print($"[WorldSetup] Terre {_land.Index} vidée");
    }

    /// <summary>Rend la main au moteur pour une image, puis s'arrête si la scène a été quittée entre-temps.</summary>
    private async Task YieldFrame()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        LoadGuard.EnsureAlive(this);
    }

    public override void _ExitTree()
    {
        RunRandom.End();
        if (IsWorldReady)
            return;
        // Scène quittée pendant le chargement (plan 26 Q4) : les décors en attente sont hors de l'arbre, sa destruction
        // ne les atteint pas ; une génération encore en cours voit son éventuelle exception consignée.
        if (IsInstanceValid(_propStaging))
            _propStaging.Free();
        _propStaging = null;
        if (_generation != null && !_generation.IsCompleted)
            _generation.ContinueWith(task => GD.PushWarning($"[WorldSetup] Génération finie après la sortie de scène : {task.Exception?.GetBaseException().Message}"),
                TaskContinuationOptions.OnlyOnFaulted);
    }

    // Les boucles longues du chargement rendent la main au moteur quand leur tranche de temps est épuisée, et non
    // toutes les N cellules : l'écran de chargement reste animé sans payer une frame d'attente par petit lot.
    private const ulong LoadSliceUsec = 12_000;
    private ulong _loadSliceStart;

    private void BeginLoadSlice() => _loadSliceStart = Time.GetTicksUsec();

    private bool LoadSliceSpent() => Time.GetTicksUsec() - _loadSliceStart >= LoadSliceUsec;

    private async Task YieldLoadSlice()
    {
        await YieldFrame();
        BeginLoadSlice();
    }

    private List<BiomeData> LoadAvailableBiomes()
    {
        List<BiomeData> biomes = new();

        if (_config.AvailableBiomes != null && _config.AvailableBiomes.Count > 0)
        {
            // Biomes connus : WorldGenConfig refuse un identifiant absent du catalogue.
            foreach (string biomeId in _config.AvailableBiomes)
                biomes.Add(BiomeDataLoader.Get(biomeId));
        }
        else
        {
            biomes = BiomeDataLoader.GetAll();
        }

        return biomes;
    }

    /// <summary>
    /// Fond noir derrière le TileMap : le vide, l'Effacé.
    /// Au-delà de la carte et dans les trous de décomposition, c'est le néant.
    /// </summary>
    private void CreateVoidBackground()
    {
        ColorRect voidBg = new();
        float worldSize = Mathf.Max(_config.MapRadius, _config.MapRadiusY) * 64f * 4f;
        voidBg.Size = new Vector2(worldSize, worldSize);
        voidBg.Position = new Vector2(-worldSize * 0.5f, -worldSize * 0.5f);
        voidBg.Color = new Color(0f, 0f, 0f, 1f);
        voidBg.ZIndex = -100;
        AddChild(voidBg);
        MoveChild(voidBg, 0);
        _landNodes.Add(voidBg);
    }

    /// <summary>Pose des tuiles du sol, en rendant la main au moteur par tranches de temps.</summary>
    private async Task ApplyTerrainAsync(TerrainType[,] terrain, UrbanLayout urbanLayout, Action<string> onProgress)
    {
        _ground.Clear();
        _roadOverlay?.Clear();

        int radius = _config.MapRadius;
        int radiusY = _config.MapRadiusY;
        int totalCells = (radius * 2 + 1) * (radiusY * 2 + 1);
        float fadeStart = radius - _config.EdgeFadeWidth;
        bool hasDissolution = _tileMapper.HasDissolutionTiles;

        int count = 0;
        BeginLoadSlice();

        for (int gx = 0; gx < radius * 2 + 1; gx++)
        {
            for (int gy = 0; gy < radiusY * 2 + 1; gy++)
            {
                int x = gx - radius;
                int y = gy - radiusY;

                if (!_generator.IsWithinBounds(x, y))
                    continue;

                Vector2I cell = new(x, y);
                float dist = _generator.EllipseDistance(x, y);

                if (_generator.IsErased(x, y))
                {
                    if (hasDissolution && dist <= radius)
                    {
                        float decay = Mathf.Clamp((dist - fadeStart) / _config.EdgeFadeWidth, 0f, 1f);
                        int dissId = _tileMapper.GetDissolutionSourceId(decay);
                        if (dissId >= 0)
                        {
                            _tileMapper.SetCell(_ground, cell, dissId);
                        }
                    }
                    count++;
                    continue;
                }

                TerrainType terrainType = terrain[gx, gy];
                int biomeIndex = _generator.GetBiomeIndex(x, y);
                UrbanCellType urbanCellType = UrbanCellType.None;
                if (urbanLayout != null)
                    urbanCellType = urbanLayout.CellGrid[gx, gy];

                int sourceId = _tileMapper.GetSourceId(biomeIndex, terrainType, x, y, urbanCellType, _generator);
                if (sourceId >= 0)
                    _tileMapper.SetCell(_ground, cell, sourceId);

                count++;
                if ((count & 255) == 0 && LoadSliceSpent())
                {
                    int pct = (int)(count * 100f / totalCells);
                    onProgress?.Invoke($"Terrain... {pct}%");
                    await YieldLoadSlice();
                }
            }
        }

        await ApplyRoadOverlayAsync(onProgress);
    }

    /// <summary>Chemins dessinés sur la couche des routes, juste avant elle : une rue recouvre le chemin qui la rejoint.</summary>
    private void AddPaths()
    {
        if (_pathNetwork == null || _pathNetwork.Strokes.Count == 0)
            return;
        Node2D paths = PathMeshes.Build(_pathNetwork);
        AddChild(paths);
        _landNodes.Add(paths);
        MoveChild(paths, _roadOverlay != null ? _roadOverlay.GetIndex() : _ground.GetIndex() + 1);
    }

    private void InitializeFog()
    {
        _fogOfWar = GetNodeOrNull<FogOfWar>("FogOfWar");
        if (_fogOfWar == null)
        {
            _fogOfWar = new FogOfWar();
            _fogOfWar.Name = "FogOfWar";
            AddChild(_fogOfWar);
        }

        _fogOfWar.Initialize(
            _ground,
            _config.FogRevealRadius,
            _config.FogInitialClearRadius
        );
    }

    private void SpawnPois()
    {
        _poiManager = new PoiManager();
        AddChild(_poiManager);
        _landNodes.Add(_poiManager);

        Spawn.EnemyPool enemyPool = GetNodeOrNull<Spawn.EnemyPool>("EnemyPool");
        Node enemyContainer = GetNodeOrNull("EnemyContainer");

        _poiManager.SpawnPois(
            _generator, _ground, _poiContainer, _usedCells, _config.PoiMinDistanceBetween,
            enemyPool, enemyContainer);
    }

    private void SpawnSites()
    {
        SitePlacer placer = new(_generator, _ground, _usedCells, LandSeed, _urbanLayout, _wildFieldsLayout, ChestDataLoader.LoadPlacement())
        {
            CountScale = _land.CountScale,
            MemorialBand = _land.MemorialBand,
        };
        // Les Mémoriaux des pièces d'abord : coffres et Failles s'en écartent comme des autres lieux.
        List<Vector2> memorialSlots = new();
        foreach (PlacedPiece piece in _pieces)
        {
            if (piece.MemorialPosition is not Vector2 slot)
                continue;
            placer.Claim(slot);
            memorialSlots.Add(slot);
        }
        ChestSpawner.SpawnChests(placer, _poiContainer);
        LandmarkSpawner.SpawnLandmarks(placer, _poiContainer, memorialSlots);
    }

    private async Task BuildPropOcclusionAsync()
    {
        Node2D propContainer = GetNode<Node2D>("PropContainer");
        Node2D decals = new() { Name = "GroundDecals", ZIndex = -1 };
        AddChild(decals);
        _landNodes.Add(decals);
        // Hors de l'arbre comme PropStaging : libéré même si la construction échoue (plan 26 Q4).
        Node2D decalStaging = new();
        try
        {
            SeparateGroundDecals(_propStaging, decalStaging);
            LoadProfiler.Mark("décalques séparés");
            await YieldFrame();
            Node2D player = GetNodeOrNull<Node2D>("Player");
            PropOcclusion occlusion = new() { Name = "PropOcclusion" };
            AddChild(occlusion);
            _landNodes.Add(occlusion);
            occlusion.Build(_propStaging, player);
            LoadProfiler.Mark("index d'occlusion construit");
            await YieldFrame();

            // En dernier : les décors, rangés en tronçons hors de l'arbre, y entrent tronçon par tronçon (voir PropChunks).
            PropChunks chunks = new() { Name = "PropChunks" };
            AddChild(chunks);
            _landNodes.Add(chunks);
            chunks.Build(player?.GetNodeOrNull<Camera2D>("Camera"), (_propStaging, propContainer), (decalStaging, decals));
            _propStaging.Free();
            _propStaging = null;
            LoadProfiler.Mark("décors rangés en tronçons");
            BeginLoadSlice();
            while (chunks.AttachPending(LoadSliceSpent))
                await YieldLoadSlice();
        }
        finally
        {
            decalStaging.Free();
        }
    }

    /// <summary>
    /// Les décalques au sol (débris, fleurs, flaques) passent dans un conteneur non trié :
    /// le tri en Y de la scène ne porte plus que sur les décors qui ont une hauteur.
    /// </summary>
    private static void SeparateGroundDecals(Node2D propContainer, Node2D decals)
    {
        int moved = 0;
        foreach (Node child in propContainer.GetChildren())
        {
            if (child is not EnvironmentProp { ZIndex: -1 } prop)
                continue;
            prop.ZIndex = 0;
            // Hors de l'arbre et conteneurs à l'origine : déplacer le nœud garde sa position, sans sortie ni entrée.
            propContainer.RemoveChild(prop);
            decals.AddChild(prop);
            moved++;
        }
        GD.Print($"[WorldSetup] Décalques au sol hors tri : {moved} / {moved + propContainer.GetChildCount()}");
    }

    /// <summary>Décors des biomes, placeur après placeur ; le moteur reprend la main entre chacun et par tranches de temps.</summary>
    private async Task SpawnEnvironmentPropsAsync(UrbanLayout urbanLayout)
    {
        // Conteneur final, trié en Y avec le joueur, les ennemis et les POI : un décor masque ce qui passe derrière lui.
        if (GetNodeOrNull<Node2D>("PropContainer") == null)
            AddChild(new Node2D { Name = "PropContainer", YSortEnabled = true });
        // Les placeurs posent les décors hors de l'arbre : 10 000 entrées dans la scène coûtent cher, et le découpage en
        // tronçons les déplacerait une seconde fois. Ils y entrent une seule fois, déjà rangés (BuildPropOcclusion).
        _propStaging = new Node2D { Name = "PropStaging" };
        Node2D propContainer = _propStaging;

        _propSpawner = new PropSpawner { Name = "PropSpawner" };
        AddChild(_propSpawner);
        _landNodes.Add(_propSpawner);
        LandPieceComposer.Place(_pieces, propContainer, LandSeed);
        LoadProfiler.Mark("décors des pièces");
        await YieldFrame();
        SiteComposer.Place(_farms, _farmConfig.Site, propContainer, LandSeed);
        LoadProfiler.Mark("décors des fermes");
        await YieldFrame();
        SiteComposer.Place(_quarrySites, _quarryPlan, propContainer, LandSeed);
        HashSet<Vector2I> blockedCells = BuildEnvironmentPropBlockedCells(urbanLayout);
        LoadProfiler.Mark("décors des carrières");
        await YieldFrame();
        WildFieldsComposer.PlaceParcelProps(_wildFieldsLayout, _farmConfig, _usedCells, blockedCells, _ground, propContainer, LandSeed);
        await YieldFrame();
        _storyScenes = WildFieldsComposer.PlaceScenes(_generator, _farmConfig, _usedCells, blockedCells, _ground, propContainer, LandSeed);
        LoadProfiler.Mark("décors des champs");
        await YieldFrame();
        JunctionPropPlacer.Place(_generator, _config.JunctionProps, _usedCells, blockedCells, _ground, propContainer, LandSeed);
        ChannelBridgePlacer.Place(_generator, _pathNetwork, PropSpawner.LoadRhythms(), propContainer);
        LoadProfiler.Mark("décors des carrefours et des passerelles");
        await YieldLoadSlice();
        await _propSpawner.SpawnPropsAsync(_generator, _terrain, _ground, propContainer, _usedCells, LandSeed, LoadSliceSpent,
            YieldLoadSlice, blockedCells, _wildFieldsLayout, _rhythmZones);
    }

    private HashSet<Vector2I> BuildEnvironmentPropBlockedCells(UrbanLayout urbanLayout)
    {
        HashSet<Vector2I> blocked = new();

        if (urbanLayout != null)
        {
            for (int gx = 0; gx < urbanLayout.CellGrid.GetLength(0); gx++)
            {
                for (int gy = 0; gy < urbanLayout.CellGrid.GetLength(1); gy++)
                {
                    if (urbanLayout.CellGrid[gx, gy] == UrbanCellType.None)
                        continue;

                    blocked.Add(new Vector2I(gx - urbanLayout.MapRadius, gy - urbanLayout.MapRadiusY));
                }
            }
        }

        if (_pathNetwork != null)
            blocked.UnionWith(_pathNetwork.Cells);

        return blocked.Count > 0 ? blocked : null;
    }

    private void EnsureRoadOverlayLayer()
    {
        _roadOverlay = GetNodeOrNull<TileMapLayer>("RoadOverlay");
        if (_roadOverlay == null)
        {
            _roadOverlay = new TileMapLayer
            {
                Name = "RoadOverlay",
                ZIndex = -9,
                TileSet = _ground.TileSet
            };
            AddChild(_roadOverlay);
        }
        else
        {
            _roadOverlay.TileSet = _ground.TileSet;
        }

        MoveChild(_roadOverlay, _ground.GetIndex() + 1);
    }

    private async Task ApplyRoadOverlayAsync(Action<string> onProgress)
    {
        if (_roadOverlay == null)
            return;

        _roadOverlay.Clear();
        if (_roadTiles == null)
            return;

        int urbanIndex = _tileMapper.GetBiomeIndex("urban_ruins");
        if (urbanIndex < 0)
            return;

        int count = 0;
        int total = _roadTiles.Cells.Count;
        BeginLoadSlice();

        foreach (Vector2I roadCell in _roadTiles.Cells.Keys)
        {
            if (_tileMapper.TryGetUrbanRoadOverlaySourceId(urbanIndex, roadCell.X, roadCell.Y, out int sourceId) && sourceId >= 0)
                _tileMapper.SetCell(_roadOverlay, roadCell, sourceId);

            count++;
            if ((count & 255) == 0 && LoadSliceSpent())
            {
                int pct = total <= 0 ? 100 : (int)(count * 100f / total);
                onProgress?.Invoke($"Routes... {pct}%");
                await YieldLoadSlice();
            }
        }
    }

    /// <summary>
    /// Place les éléments de lore (props interactifs/décoratifs) sur la map.
    /// Appelé après SpawnEnvironmentProps, utilise le même système de cells occupées.
    /// </summary>
    private void SpawnLoreElements()
    {
        // Placement tiré de la seed, comme le reste de la carte (plan 26 Q8c).
        _loreRng = RunRandom.Create("lore_elements");
        Node2D loreContainer = new() { Name = "LoreContainer" };
        AddChild(loreContainer);
        _landNodes.Add(loreContainer);

        // Placer après PropContainer pour Z-order correct
        Node propContainer = GetNodeOrNull("PropContainer");
        if (propContainer != null)
            MoveChild(loreContainer, propContainer.GetIndex() + 1);

        int loreCount = 0;

        // --- Pas Interrompus : 2-4 par map, tous biomes ---
        int footstepCount = _loreRng.RandiRange(2, 5);
        for (int i = 0; i < footstepCount; i++)
        {
            Vector2I cell = PickLoreCell(10);
            if (cell.X == int.MinValue) continue;

            InterruptedFootsteps footsteps = new() { GlobalPosition = _ground.MapToLocal(cell) };
            loreContainer.AddChild(footsteps);
            _usedCells.Add(cell);
            loreCount++;
        }

        // --- Étagères Vides : 2-3, uniquement en zone urbaine ---
        int shelfCount = _loreRng.RandiRange(2, 4);
        for (int i = 0; i < shelfCount; i++)
        {
            Vector2I cell = PickLoreCellInBiome("urban_ruins", 8);
            if (cell.X == int.MinValue) continue;

            EmptyShelves shelves = new() { GlobalPosition = _ground.MapToLocal(cell) };
            loreContainer.AddChild(shelves);
            _usedCells.Add(cell);
            loreCount++;
        }

        // --- Balançoire Fantôme : 0-1, rare, en forêt ---
        if (_loreRng.Randf() < 0.6f)
        {
            Vector2I cell = PickLoreCellInBiome("forest_reclaimed", 15);
            if (cell.X != int.MinValue)
            {
                GhostSwing swing = new() { GlobalPosition = _ground.MapToLocal(cell) };
                loreContainer.AddChild(swing);
                _usedCells.Add(cell);
                loreCount++;
            }
        }

        // --- Racines-Lettres : 3-5, en forêt ou bordure forêt/urbain ---
        int rootCount = _loreRng.RandiRange(3, 6);
        for (int i = 0; i < rootCount; i++)
        {
            Vector2I cell = PickLoreCellInBiome("forest_reclaimed", 8);
            if (cell.X == int.MinValue) continue;

            RootLetters roots = new() { GlobalPosition = _ground.MapToLocal(cell) };
            loreContainer.AddChild(roots);
            _usedCells.Add(cell);
            loreCount++;
        }

        // --- Panneaux Avalés : 2-3, tous biomes sauf marais ---
        int signCount = _loreRng.RandiRange(2, 4);
        for (int i = 0; i < signCount; i++)
        {
            Vector2I cell = PickLoreCell(10);
            if (cell.X == int.MinValue) continue;

            // Vérifier qu'on n'est pas en marais
            BiomeData biome = _generator.GetBiome(cell.X, cell.Y);
            if (biome != null && biome.Id == "swamp")
                continue;

            SwallowedSign sign = new() { GlobalPosition = _ground.MapToLocal(cell) };
            loreContainer.AddChild(sign);
            _usedCells.Add(cell);
            loreCount++;
        }

        // --- Carillons Fantômes : 2-3, en cluster (proches les uns des autres) ---
        int chimeCount = _loreRng.RandiRange(2, 4);
        Vector2I chimeCenter = PickLoreCell(15);
        if (chimeCenter.X != int.MinValue)
        {
            for (int i = 0; i < chimeCount; i++)
            {
                // Cluster : chaque carillon à 3-6 tiles du centre
                int offsetX = _loreRng.RandiRange(-5, 6);
                int offsetY = _loreRng.RandiRange(-5, 6);
                Vector2I cell = new(chimeCenter.X + offsetX, chimeCenter.Y + offsetY);

                if (!IsValidLoreCell(cell))
                    continue;

                GhostChime chime = new() { GlobalPosition = _ground.MapToLocal(cell) };
                loreContainer.AddChild(chime);
                _usedCells.Add(cell);
                loreCount++;
            }
        }

        // --- Champignons Bioluminescents : 3-5, en forêt (sous-bois/ombre) ---
        int shroomCount = _loreRng.RandiRange(3, 6);
        for (int i = 0; i < shroomCount; i++)
        {
            Vector2I cell = PickLoreCellInBiome("forest_reclaimed", 8);
            if (cell.X == int.MinValue) continue;

            BioluminescentMushrooms shrooms = new() { GlobalPosition = _ground.MapToLocal(cell) };
            loreContainer.AddChild(shrooms);
            _usedCells.Add(cell);
            loreCount++;
        }

        // --- Graffiti Survivant : 3-5, en zone urbaine ---
        int graffitiCount = _loreRng.RandiRange(3, 6);
        for (int i = 0; i < graffitiCount; i++)
        {
            Vector2I cell = PickLoreCellInBiome("urban_ruins", 8);
            if (cell.X == int.MinValue) continue;

            SurvivingGraffiti graffiti = new() { GlobalPosition = _ground.MapToLocal(cell) };
            loreContainer.AddChild(graffiti);
            _usedCells.Add(cell);
            loreCount++;
        }

        // --- Horloge Folle : 1, en zone urbaine (rare et marquante) ---
        {
            Vector2I cell = PickLoreCellInBiome("urban_ruins", 12);
            if (cell.X != int.MinValue)
            {
                CrazyClock clock = new() { GlobalPosition = _ground.MapToLocal(cell) };
                loreContainer.AddChild(clock);
                _usedCells.Add(cell);
                loreCount++;
            }
        }

        // --- Porte Sans Mur : 1-2, tous biomes sauf eau/marais ---
        int doorCount = _loreRng.RandiRange(1, 3);
        for (int i = 0; i < doorCount; i++)
        {
            Vector2I cell = PickLoreCell(10);
            if (cell.X == int.MinValue) continue;

            BiomeData doorBiome = _generator.GetBiome(cell.X, cell.Y);
            if (doorBiome != null && doorBiome.Id == "swamp")
                continue;

            DoorWithoutWall door = new() { GlobalPosition = _ground.MapToLocal(cell) };
            loreContainer.AddChild(door);
            _usedCells.Add(cell);
            loreCount++;
        }

        // --- Miroir d'Eau : 1-2, en marais ---
        int mirrorCount = _loreRng.RandiRange(1, 3);
        for (int i = 0; i < mirrorCount; i++)
        {
            Vector2I cell = PickLoreCellInBiome("swamp", 10);
            if (cell.X == int.MinValue)
            {
                // Fallback : essayer près de l'eau dans n'importe quel biome
                cell = PickLoreCell(10);
                if (cell.X == int.MinValue) continue;
            }

            WaterMirror mirror = new() { GlobalPosition = _ground.MapToLocal(cell) };
            loreContainer.AddChild(mirror);
            _usedCells.Add(cell);
            loreCount++;
        }

        GD.Print($"[WorldSetup] Spawned {loreCount} lore elements");
    }

    /// <summary>Choisit une cellule valide pour un élément de lore (hors eau, void, spawn, cells occupées).</summary>
    private Vector2I PickLoreCell(int minDistFromSpawn)
    {
        int safeRadius = _config.MapRadius - _config.EdgeFadeWidth;
        int safeRadiusY = _config.MapRadiusY - _config.EdgeFadeWidth;

        for (int attempt = 0; attempt < 40; attempt++)
        {
            int x = _loreRng.RandiRange(-safeRadius + 1, safeRadius);
            int y = _loreRng.RandiRange(-safeRadiusY + 1, safeRadiusY);
            Vector2I cell = new(x, y);

            if (!IsValidLoreCell(cell))
                continue;
            if (Mathf.Abs(x) <= minDistFromSpawn && Mathf.Abs(y) <= minDistFromSpawn)
                continue;

            return cell;
        }
        return new Vector2I(int.MinValue, int.MinValue);
    }

    /// <summary>Choisit une cellule valide dans un biome spécifique.</summary>
    private Vector2I PickLoreCellInBiome(string biomeId, int minDistFromSpawn)
    {
        int safeRadius = _config.MapRadius - _config.EdgeFadeWidth;
        int safeRadiusY = _config.MapRadiusY - _config.EdgeFadeWidth;

        for (int attempt = 0; attempt < 60; attempt++)
        {
            int x = _loreRng.RandiRange(-safeRadius + 1, safeRadius);
            int y = _loreRng.RandiRange(-safeRadiusY + 1, safeRadiusY);
            Vector2I cell = new(x, y);

            if (!IsValidLoreCell(cell))
                continue;
            if (Mathf.Abs(x) <= minDistFromSpawn && Mathf.Abs(y) <= minDistFromSpawn)
                continue;

            BiomeData biome = _generator.GetBiome(x, y);
            if (biome == null || biome.Id != biomeId)
                continue;

            return cell;
        }
        return new Vector2I(int.MinValue, int.MinValue);
    }

    private bool IsValidLoreCell(Vector2I cell)
    {
        if (_generator.EllipseDistance(cell.X, cell.Y) > _config.MapRadius - _config.EdgeFadeWidth)
            return false;
        if (_usedCells.Contains(cell))
            return false;
        if (!_generator.IsWithinBounds(cell.X, cell.Y))
            return false;
        if (_generator.IsErased(cell.X, cell.Y))
            return false;
        if (_generator.GetTerrain(cell.X, cell.Y) == TerrainType.Water)
            return false;

        return true;
    }

    private void InitBiomeAtmosphere()
    {
        _biomeAtmosphere = new BiomeAtmosphere { Name = "BiomeAtmosphere" };
        AddChild(_biomeAtmosphere);
        AddChild(new WeatherDirector { Name = "WeatherDirector" });
        AddChild(new FaunaDirector { Name = "FaunaDirector" });
    }

}
