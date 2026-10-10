using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Spawn;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>Banc rendu de Main : aucune modification des données ou du gameplay de production.</summary>
public partial class MovementDenseBenchmark : Node
{
    private int _enemyCount = 120;
    // Audit du 28 septembre : quantifier le coût de l'observateur, sans changer le combat.
    private int _auditObserverPeriod = 1;
    private int _auditObserverSamples;
    private ulong _auditObserverUsec;
    private const ulong Seed = 221092026;
    private Enemy[] _enemies;
    private readonly double[] _frames = new double[200000];
    private readonly double[] _activationFrames = new double[200000];
    private Player _player;
    // Centre du combat : un terrain sans décor bloquant, pour que la carte générée ne change pas ce qui est mesuré.
    private Vector2 _arena;
    private FrameSplit _split;
    private WorldSetup _world;
    private bool _ready;
    private bool _finished;
    private bool _dash;
    // --churn : PV normaux, chaque créature morte est remplacée ; mesure morts, XP, effets et recyclage du pool.
    private bool _churn;
    private int _respawns;
    private EnemyPool _pool;
    private Node _container;
    private readonly RandomNumberGenerator _churnRng = new() { Seed = Seed };
    private string _output;
    private double _duration;
    private double _warmup;
    private double _physicsTime;
    private ulong _start;
    private ulong _previous;
    private int _samples;
    private int _activationSamples;
    private int _activations;
    private bool _activationPending;
    private bool _previousDash;
    private double _lastDashRequest = -10;
    private int _minLiving = int.MaxValue;
    private int _maxLiving;
    private int _minFullAi = int.MaxValue;
    private double _processSum;
    private double _physicsSum;
    private double _renderCpuSum;
    private double _renderGpuSum;
    private double _drawCallsSum;
    private double _objectsSum;
    private double _primitivesSum;
    private double _collisionPairsSum;
    private double _activeBodiesSum;
    private double _nodeCountSum;
    private long _managedStart;
    private long _allocatedStart;
    // Nœuds ajoutés à l'arbre pendant la mesure : coût des effets créés puis libérés (plan 02 J0).
    private long _nodesAdded;
    private readonly Dictionary<string, int> _nodesAddedByName = new();
    // Nœuds vus pour la première fois : un nœud recyclé qui rentre dans l'arbre n'est pas une création.
    private readonly HashSet<ulong> _seenNodes = new();
    private readonly Dictionary<string, int> _nodesCreatedByName = new();
    private int _nodesCreated;
    private long _rssStart;
    private double _nativeStart;
    private Label _label;
    private Vector2I _requestedSize;
    private Vector2I _renderSize;
    private Vector2 _previousPosition;
    private double _distance;
    private double _dashDistance;

    public override async void _Ready()
    {
        try
        {
            ProcessMode = ProcessModeEnum.Always;
            ProcessPhysicsPriority = 100;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            string[] args = OS.GetCmdlineUserArgs();
            _dash = Array.IndexOf(args, "--dash") >= 0;
            _churn = Array.IndexOf(args, "--churn") >= 0;
            _output = Argument(args, "--output", "/tmp/vestiges-dense");
            _duration = double.Parse(Argument(args, "--seconds", "20"), CultureInfo.InvariantCulture);
            _warmup = double.Parse(Argument(args, "--warmup", "5"), CultureInfo.InvariantCulture);
            Vector2I size = new(int.Parse(Argument(args, "--width", "1280")), int.Parse(Argument(args, "--height", "720")));
            _requestedSize = size;
            _enemyCount = int.Parse(Argument(args, "--enemies", "120"), CultureInfo.InvariantCulture);
            _enemies = new Enemy[_enemyCount];
            _auditObserverPeriod = Math.Max(1, int.Parse(Argument(args, "--audit-observer-period", "1"), CultureInfo.InvariantCulture));
            // Temps de rendu CPU/GPU du viewport : décompose la frame au-delà du temps mural (investigation perf).
            RenderingServer.ViewportSetMeasureRenderTime(GetViewport().GetViewportRid(), true);
            GetWindow().Mode = Window.ModeEnum.Windowed;
            GetWindow().ContentScaleSize = size;
            GetWindow().ContentScaleFactor = 1;
            GetWindow().Size = size;
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
            Engine.MaxFps = 0;
            GD.Seed(Seed);
            GameManager manager = GetNode<GameManager>("/root/GameManager");
            manager.RunSeed = Seed;
            manager.SelectedCharacterId = "traqueur";
            GetTree().CurrentScene = null;
            _world = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<WorldSetup>();
            GetTree().Root.AddChild(_world);
            GetTree().CurrentScene = _world;
            ulong timeout = Time.GetTicksMsec() + 120000;
            while (!_world.IsWorldReady || GetTree().Paused || manager.CurrentState != GameManager.GameState.Run)
            {
                if (Time.GetTicksMsec() > timeout)
                    throw new InvalidOperationException("Initialisation Main supérieure à 120 s.");
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
            // Zoom fixe : le recul de caméra en foule (plan 02 J5) changerait la surface rendue d'une version à l'autre.
            Node crowdZoom = _world.GetNodeOrNull("CrowdZoom");
            if (crowdZoom != null)
                crowdZoom.ProcessMode = ProcessModeEnum.Disabled;
            _world.GetNode("ErasureManager").ProcessMode = ProcessModeEnum.Disabled;
            _world.GetNode("CrisisManager").ProcessMode = ProcessModeEnum.Disabled;
            // Main signale prêt avant la fin des 200 lots de brouillard différés (versions antérieures au retrait
            // de la couche de brouillard). Attendre leur fin exclut ce chargement du coût du combat mesuré.
            FogOfWar fog = _world.GetNode<FogOfWar>("FogOfWar");
            FieldInfo fogInitializing = typeof(FogOfWar).GetField("_initPhase", BindingFlags.Instance | BindingFlags.NonPublic);
            while (fogInitializing != null && (bool)fogInitializing.GetValue(fog)!)
            {
                if (Time.GetTicksMsec() > timeout)
                    throw new InvalidOperationException("Préparation FogOfWar supérieure à 120 s.");
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            // Éliminer les gardes issus du placement procédural pour fixer exactement la population.
            foreach (Node node in GetTree().GetNodesInGroup("enemies"))
                node.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            _player = _world.GetNode<Player>("Player");
            _player.IsGodMode = true;
            _player.IsAIControlled = true;
            _arena = Argument(args, "--arena", "open") == "origin" ? Vector2.Zero : FindOpenArena();
            _player.GlobalPosition = _arena;
            // Expérience d'attribution : décors masqués (rendu seul, la physique des décors reste en place).
            if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--hide-props") >= 0)
            {
                _world.GetNode<CanvasItem>("PropContainer").Visible = false;
                _world.GetNodeOrNull<CanvasItem>("GroundDecals")?.Set(CanvasItem.PropertyName.Visible, false);
            }
            EnemyPool pool = _world.GetNode<EnemyPool>("EnemyPool");
            Node container = _world.GetNode("EnemyContainer");
            _pool = pool;
            _container = container;
            // --orbs N : orbes d'XP semées loin du joueur, hors de portée d'attraction (butin laissé derrière soi en nomade).
            int orbs = int.Parse(Argument(args, "--orbs", "0"), CultureInfo.InvariantCulture);
            for (int i = 0; i < orbs; i++)
                CombatPools.Instance.SpawnXpOrb(_arena + Vector2.FromAngle(i * 2.3999632f) * (1200f + 1800f * ((i * 0.618034f) % 1f)), 1f);
            // --weapons a,b : armes ajoutées au personnage (builds à effets de zone, morts en rafale avec --churn).
            foreach (string weapon in Argument(args, "--weapons", "").Split(',', StringSplitOptions.RemoveEmptyEntries))
                _player.AddWeapon(WeaponDataLoader.Get(weapon));
            // --ascend arme:ascension,… : arme portée au niveau max puis montée (statuts d'ascension, plan 27 V1c).
            foreach (string entry in Argument(args, "--ascend", "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = entry.Split(':');
                foreach (WeaponInstance held in _player.WeaponSlots)
                {
                    if (held.Id != parts[0])
                        continue;
                    while (held.CanLevelUp)
                        held.ApplyUpgrade(Array.Empty<StatGain>());
                    _player.AscendWeapon(held.Id, parts[1]);
                }
            }
            // --boss-part R : une partie de boss de rayon R posée hors du combat (plan 07 B1c) ; elle élargit la marge
            // de recherche de tous les tirs et de la mêlée pour le reste de la run.
            float bossPartRadius = float.Parse(Argument(args, "--boss-part", "0"), CultureInfo.InvariantCulture);
            if (bossPartRadius > 0f)
            {
                Enemy part = pool.Get();
                part.Position = _arena + new Vector2(1500f, 0f);
                container.AddChild(part);
                part.Initialize(EnemyDataLoader.Get(EnemyGrammar.BossPartId), 1f, 1f);
                new BossHealth("Banc", 0f).AddPart(part, bossPartRadius, 1_000_000f);
            }
            GD.Seed(Seed);
            for (int i = 0; i < _enemyCount; i++)
            {
                Enemy enemy = pool.Get();
                float angle = i * 2.3999632f;
                float radius = 40f + 100f * Mathf.Sqrt((i + 1f) / _enemyCount);
                enemy.Position = _arena + Vector2.FromAngle(angle) * radius;
                container.AddChild(enemy);
                // Les HP renforcés conservent les 120 IA, attaques, collisions et impacts pendant l'essai.
                // Même proportion qu'à 120 : cinq Ombres pour un Cracheur.
                enemy.Initialize(EnemyDataLoader.Get(i % 6 == 5 ? "fading_spitter" : "shade"), _churn ? 1f : 10000f, 1f);
                _enemies[i] = enemy;
            }
            CanvasLayer layer = new() { Layer = 100 };
            AddChild(layer);
            _label = new Label { Position = new Vector2(16, 88), Text = $"BENCH Main / {_dash} dash / 120 ennemis / {size.X}x{size.Y}" };
            _label.AddThemeFontSizeOverride("font_size", 20);
            _label.AddThemeColorOverride("font_shadow_color", Colors.Black);
            layer.AddChild(_label);
            GetWindow().Mode = Window.ModeEnum.Windowed;
            GetWindow().Size = size;
            GD.Print("[MovementDenseBenchmark] Préparation terminée ; attente de la première image rendue.");
            await WaitForRenderedFrame();
            using (Image initial = GetViewport().GetTexture().GetImage())
                _renderSize = initial.GetSize();
            _start = _previous = Time.GetTicksUsec();
            _ready = true;
            _split = FrameSplit.Attach(GetTree().Root);
            GD.Print($"[MovementDenseBenchmark] Chauffe puis mesure ; image {_renderSize}.");
        }
        catch (Exception ex)
        {
            GD.PushError($"[MovementDenseBenchmark] {ex}");
            GetTree().Quit(1);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_ready || _finished)
            return;
        _physicsTime += delta;
        float distance = _player.GlobalPosition.DistanceTo(_previousPosition);
        _previousPosition = _player.GlobalPosition;
        if ((Time.GetTicksUsec() - _start) / 1e6 >= _warmup)
        {
            _distance += distance;
            if (_player.Mobility.IsDashStep)
                _dashDistance += distance;
        }
        // Même cible orbitale et vitesse de base ; le dash modifie réellement le trajet et les contacts.
        Vector2 target = _arena + Vector2.FromAngle((float)(_physicsTime * 0.8)) * 70f;
        _player.AIInputOverride = ((target - _player.GlobalPosition) / 20f).LimitLength();
        if (_player.Mobility.StartedThisStep)
        {
            _activationPending = true;
            if ((Time.GetTicksUsec() - _start) / 1e6 >= _warmup)
                _activations++;
        }
        if (_dash && _physicsTime - _lastDashRequest >= 2.65)
        {
            _player.Mobility.Request();
            _lastDashRequest = _physicsTime;
        }
    }

    public override void _Process(double delta)
    {
        if (!_ready || _finished)
            return;
        ulong now = Time.GetTicksUsec();
        double elapsed = (now - _start) / 1e6;
        double frameMs = (now - _previous) / 1000.0;
        _previous = now;
        if (_churn)
        {
            ReplaceDeadEnemies(elapsed >= _warmup);
            PickLevelUp();
        }
        if (elapsed < _warmup)
        {
            _activationPending = false;
            return;
        }
        if (_samples == 0)
        {
            _split.Reset();
            _managedStart = GC.GetTotalMemory(false);
            _allocatedStart = GC.GetTotalAllocatedBytes();
            // Nœuds déjà existants au début de la mesure, arbre et réserve détachée du pool comprises.
            SeedSeenNodes(GetTree().Root);
            if (typeof(EnemyPool).GetField("_available", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(_pool)
                    is IEnumerable<Enemy> reserve)
            {
                foreach (Enemy pooled in reserve)
                    SeedSeenNodes(pooled);
            }
            GetTree().NodeAdded += node =>
            {
                _nodesAdded++;
                // Nom de la racine d'effet (script C# ou classe native) : repère les sources à recycler.
                string name = node.GetScript().Obj is Script script ? script.ResourcePath.GetFile() : node.GetClass();
                _nodesAddedByName[name] = _nodesAddedByName.GetValueOrDefault(name) + 1;
                if (_seenNodes.Add(node.GetInstanceId()))
                {
                    _nodesCreated++;
                    _nodesCreatedByName[name] = _nodesCreatedByName.GetValueOrDefault(name) + 1;
                }
            };
            _rssStart = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
            _nativeStart = Performance.GetMonitor(Performance.Monitor.MemoryStatic);
        }
        if (_samples >= _frames.Length)
        {
            GD.PushError("[MovementDenseBenchmark] Capacité d'échantillons dépassée.");
            GetTree().Quit(1);
            return;
        }
        _frames[_samples++] = frameMs;
        if (_activationPending || _previousDash || _player.Mobility.IsDashing)
            _activationFrames[_activationSamples++] = frameMs;
        _activationPending = false;
        _previousDash = _player.Mobility.IsDashing;
        _processSum += Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000;
        _physicsSum += Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000;
        Rid viewport = GetViewport().GetViewportRid();
        _renderCpuSum += RenderingServer.ViewportGetMeasuredRenderTimeCpu(viewport) + RenderingServer.GetFrameSetupTimeCpu();
        _renderGpuSum += RenderingServer.ViewportGetMeasuredRenderTimeGpu(viewport);
        _drawCallsSum += Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
        _objectsSum += Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame);
        _primitivesSum += Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame);
        _collisionPairsSum += Performance.GetMonitor(Performance.Monitor.Physics2DCollisionPairs);
        _activeBodiesSum += Performance.GetMonitor(Performance.Monitor.Physics2DActiveObjects);
        _nodeCountSum += Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);
        if ((_samples - 1) % _auditObserverPeriod == 0 || elapsed >= _warmup + _duration)
        {
            ulong observerStart = Time.GetTicksUsec();
            int living = 0;
            int fullAi = 0;
            foreach (Enemy enemy in _enemies)
            {
                if (IsInstanceValid(enemy) && enemy.IsActive && !enemy.IsDying && enemy.HpRatio > 0)
                {
                    living++;
                    if (enemy.GlobalPosition.DistanceSquaredTo(_player.GlobalPosition) <= 600f * 600f)
                        fullAi++;
                }
            }
            _minLiving = Math.Min(_minLiving, living);
            _maxLiving = Math.Max(_maxLiving, living);
            _minFullAi = Math.Min(_minFullAi, fullAi);
            _auditObserverUsec += Time.GetTicksUsec() - observerStart;
            _auditObserverSamples++;
        }
        if (elapsed >= _warmup + _duration)
        {
            _finished = true;
            Finish();
        }
    }

    private static int CountXpOrbs()
    {
        int count = 0;
        foreach (Node child in CombatPools.Instance.GetChildren())
        {
            if (child is XpOrb { Visible: true })
                count++;
        }
        return count;
    }

    /// <summary>
    /// Libère la run et fait passer le ramasse-miettes avant de quitter : sinon le crash de fermeture des builds de
    /// debug interrompt la série. Autonome (sans GameExit) pour compiler aussi dans les worktrees de base de bench_ab.
    /// </summary>
    private async Task QuitCleanly(int exitCode)
    {
        _world.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GetTree().Quit(exitCode);
    }

    private void SeedSeenNodes(Node root)
    {
        _seenNodes.Add(root.GetInstanceId());
        foreach (Node child in root.GetChildren(true))
            SeedSeenNodes(child);
    }

    /// <summary>Une créature rendue au pool est remplacée sur un anneau autour du joueur, même espèce, même proportion.</summary>
    private void ReplaceDeadEnemies(bool counted)
    {
        for (int i = 0; i < _enemies.Length; i++)
        {
            Enemy enemy = _enemies[i];
            if (IsInstanceValid(enemy) && enemy.IsActive)
                continue;
            Enemy fresh = _pool.Get();
            fresh.Position = _player.GlobalPosition + Vector2.FromAngle(_churnRng.RandfRange(0f, Mathf.Tau)) * _churnRng.RandfRange(220f, 320f);
            _container.AddChild(fresh);
            fresh.Initialize(EnemyDataLoader.Get(i % 6 == 5 ? "fading_spitter" : "shade"), 1f, 1f);
            _enemies[i] = fresh;
            if (counted)
                _respawns++;
        }
    }

    /// <summary>
    /// Premier point, en spirale depuis l'origine, sans décor bloquant dans le rayon de la foule. Depuis la carte agrandie
    /// du 30 septembre, l'origine tombe dans un immeuble : les créatures y forçaient contre un mur (plan 29 §1.1).
    /// </summary>
    private Vector2 FindOpenArena()
    {
        const uint PropLayer = 4;
        PhysicsDirectSpaceState2D space = _player.GetWorld2D().DirectSpaceState;
        using CircleShape2D circle = new() { Radius = 420f };
        using PhysicsShapeQueryParameters2D query = new() { Shape = circle, CollisionMask = PropLayer, CollideWithAreas = false };
        for (int ring = 0; ring <= 40; ring++)
        {
            int steps = Math.Max(1, ring * 6);
            for (int step = 0; step < steps; step++)
            {
                Vector2 candidate = Vector2.FromAngle(Mathf.Tau * step / steps) * ring * 256f;
                query.Transform = new Transform2D(0f, candidate);
                if (space.IntersectShape(query, 1).Count == 0)
                    return candidate;
            }
        }
        throw new InvalidOperationException("Aucun terrain dégagé près de l'origine.");
    }

    /// <summary>Les morts font monter de niveau : la première carte est prise aussitôt, la run ne reste pas en pause.</summary>
    private void PickLevelUp()
    {
        if (!GetTree().Paused)
            return;
        Node screen = _world.GetNode("LevelUpScreen");
        if (screen.GetType().GetField("_fragmentManager", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(screen)
                is not Vestiges.Progression.FragmentManager fragments || !fragments.IsChoiceActive || fragments.PendingChoices.Count == 0)
            return;
        screen.GetType().GetMethod("OnCardChosen", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(screen, new object[] { fragments.PendingChoices[0] });
    }

    private async void Finish()
    {
        try
        {
            long managedEnd = GC.GetTotalMemory(false);
            long allocated = GC.GetTotalAllocatedBytes() - _allocatedStart;
            using System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
            long rssEnd = process.WorkingSet64;
            long rssPeak = process.PeakWorkingSet64;
            double nativeEnd = Performance.GetMonitor(Performance.Monitor.MemoryStatic);
            // En --churn, une créature ne revient qu'après son animation de mort : une rafale creuse la population.
            int livingFloor = _churn ? _enemyCount / 3 : _enemyCount * 5 / 6;
            bool valid = _minLiving >= livingFloor && _minFullAi >= livingFloor && (!_dash || _activations >= 5)
                && !GetTree().Paused && DisplayServer.GetName() != "headless"
                && GetWindow().Size == _requestedSize && _renderSize == _requestedSize
                && _distance > 100 && (!_dash || _dashDistance > 50);
            var result = new
            {
                observer_period_frames = _auditObserverPeriod, observer_samples = _auditObserverSamples,
                observer_total_usec = _auditObserverUsec,
                valid, dash = _dash, seed = Seed, warmup_seconds = _warmup, requested_seconds = _duration,
                measured_seconds = _frames.Take(_samples).Sum() / 1000,
                resolution = new[] { GetWindow().Size.X, GetWindow().Size.Y },
                viewport = new[] { _renderSize.X, _renderSize.Y },
                renderer = RenderingServer.GetCurrentRenderingMethod(), display = DisplayServer.GetName(),
                vsync = DisplayServer.WindowGetVsyncMode().ToString(), max_fps = Engine.MaxFps,
                cpu = OS.GetProcessorName(), cpu_threads = OS.GetProcessorCount(),
                gpu = RenderingServer.GetVideoAdapterName(), vendor = RenderingServer.GetVideoAdapterVendor(),
                engine = Engine.GetVersionInfo()["string"].AsString(),
                arena = new[] { _arena.X, _arena.Y },
                frame_split_ms = _split.Summary(),
                living_min = _minLiving, living_max = _maxLiving, full_ai_range_min = _minFullAi,
                activations = _activations, frames = Stats(_frames, _samples), dash_window_frames = Stats(_activationFrames, _activationSamples),
                traveled_pixels = _distance, dash_traveled_pixels = _dashDistance,
                // Noms historiques du premier banc : moniteurs Godot, pas un profil CPU isolé.
                process_cpu_mean_ms = _processSum / _samples, physics_cpu_mean_ms = _physicsSum / _samples,
                enemies = _enemyCount, churn = _churn, xp_orbs = CountXpOrbs(), respawns = _respawns,
                respawns_per_second = _respawns / (_frames.Take(_samples).Sum() / 1000),
                render_cpu_mean_ms = _renderCpuSum / _samples, render_gpu_mean_ms = _renderGpuSum / _samples,
                draw_calls_mean = _drawCallsSum / _samples, rendered_objects_mean = _objectsSum / _samples,
                primitives_mean = _primitivesSum / _samples, collision_pairs_mean = _collisionPairsSum / _samples,
                active_bodies_mean = _activeBodiesSum / _samples, node_count_mean = _nodeCountSum / _samples,
                managed_start_bytes = _managedStart, managed_end_bytes = managedEnd, allocated_bytes = allocated,
                nodes_added = _nodesAdded,
                nodes_created = _nodesCreated,
                nodes_created_by_type = _nodesCreatedByName.OrderByDescending(pair => pair.Value).Take(12).ToDictionary(pair => pair.Key, pair => pair.Value),
                nodes_added_by_type = _nodesAddedByName.OrderByDescending(pair => pair.Value).Take(12).ToDictionary(pair => pair.Key, pair => pair.Value), nodes_added_per_second = _nodesAdded / (_frames.Take(_samples).Sum() / 1000),
                fx_dropped = new
                {
                    sparks = FxBudget.DroppedCount(FxBudgetKind.Sparks), shapes = FxBudget.DroppedCount(FxBudgetKind.Shapes),
                    deaths = FxBudget.DroppedCount(FxBudgetKind.Deaths), numbers = FxBudget.DroppedCount(FxBudgetKind.Numbers),
                },
                native_start_bytes = _nativeStart, native_end_bytes = nativeEnd,
                rss_start_bytes = _rssStart, rss_end_bytes = rssEnd, rss_process_peak_bytes = rssPeak,
                fixture = "Main réelle ; profil dev temporaire sans souvenir équipé, Steam désactivé ; combat centré sur le premier terrain sans décor bloquant dans 420 px (arena ; --arena origin pour l'ancien point (0,0)) ; Ombres et Cracheurs à 5 pour 1 (120 par défaut, --enemies), HP x10000 ; traqueur invincible, arme initiale active ; spawn naturel, Effacement et crises figés ; cible orbitale commune, trajectoires réelles différentes avec dash ; code courant dans les deux cas."
            };
            Directory.CreateDirectory(Path.GetDirectoryName(_output)!);
            File.WriteAllText(_output + ".json", JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            using (StreamWriter csv = new(_output + ".frames.csv"))
            {
                csv.WriteLine("frame,wall_ms");
                for (int i = 0; i < _samples; i++)
                    csv.WriteLine($"{i},{_frames[i].ToString("F4", CultureInfo.InvariantCulture)}");
            }
            _label.Text = $"BENCH Main | {(_dash ? "DASH" : "SANS DASH")} | vivants {_minLiving}–{_maxLiving} | {_activations} dashs | {_samples} frames";
            // Le readback GPU et l'écriture PNG sont exclus de toutes les mesures.
            await WaitForRenderedFrame();
            using Image screenshot = GetViewport().GetTexture().GetImage();
            Error saved = screenshot.SavePng(_output + ".png");
            if (saved != Error.Ok)
                throw new IOException($"Capture : {saved}");
            GD.Print($"[MovementDenseBenchmark] RESULT valid={valid} output={_output}");
            await QuitCleanly(valid ? 0 : 1);
        }
        catch (Exception ex)
        {
            GD.PushError($"[MovementDenseBenchmark] {ex}");
            GetTree().Quit(1);
        }
    }

    private static object Stats(double[] source, int count)
    {
        if (count == 0)
            return null;
        double[] sorted = source.Take(count).Order().ToArray();
        double mean = sorted.Average();
        return new { count, mean_ms = mean, fps = 1000 / mean,
            p95_ms = sorted[Math.Clamp((int)Math.Ceiling(count * 0.95) - 1, 0, count - 1)],
            p99_ms = sorted[Math.Clamp((int)Math.Ceiling(count * 0.99) - 1, 0, count - 1)],
            max_ms = sorted[^1], over_16_67_percent = 100.0 * sorted.Count(v => v > 1000.0 / 60) / count };
    }

    private async Task WaitForRenderedFrame()
    {
        // Séparer l'attente du rendu du readback pour éviter une reprise réentrante ;
        // les blocages observés étaient avant mesure, sans cause moteur établie.
        int previous = Engine.GetFramesDrawn();
        ulong deadline = Time.GetTicksMsec() + 5000;
        do
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (Time.GetTicksMsec() > deadline)
                throw new TimeoutException("Aucune nouvelle image rendue depuis 5 s.");
        }
        while (Engine.GetFramesDrawn() <= previous);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static string Argument(string[] args, string name, string fallback)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
    }
}
