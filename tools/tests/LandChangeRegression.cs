using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Progression;
using Vestiges.Score;
using Vestiges.UI;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Changement de terre (plan 51, P1) sur la vraie scène Main, terres allumées (--lands) : trois terres enchaînées par le
/// vrai passage, foule et orbe d'XP posées avant chacun. Rien de la terre quittée ne reste (lieux, coffres, obstacles,
/// créatures), la terre suivante est peuplée et d'un autre biome, l'état du joueur et l'horloge ne bougent pas.
/// </summary>
public partial class LandChangeRegression : Node
{
    private const ulong Seed = 51;
    private const float OrbXp = 7f;
    private WorldSetup _world;
    private Player _player;
    private int _failures;
    private int _memorials;
    private int _chests;

    public override async void _Ready()
    {
        try
        {
            ProcessMode = ProcessModeEnum.Always;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GameManager manager = GetNode<GameManager>("/root/GameManager");
            manager.RunSeed = Seed;
            GetTree().CurrentScene = null;
            _world = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<WorldSetup>();
            GetTree().Root.AddChild(_world);
            GetTree().CurrentScene = _world;
            while (!_world.IsWorldReady || GetTree().Paused || manager.CurrentState != GameManager.GameState.Run)
                await Frame();
            _player = _world.GetNode<Player>("Player");
            _player.SurviveFatalHitsForTests = true;
            CheckFrontRules();
            CheckFrontOnLand();
            // Comptes de la terre 0 avant que le cercle n'y fasse tomber le reste : chaque terre doit en avoir autant.
            _memorials = Memorial.All.Count;
            _chests = Chest.Closed.Count;
            await CheckMemorialCircle();
            await CheckVoidDrain();
            await CheckGuardian();
            await CheckLands();
            GD.Print($"[LandChangeRegression] RESULT failures={_failures}");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError($"[LandChangeRegression] {exception}");
            GetTree().Quit(2);
        }
    }

    /// <summary>Lot P2a, règles du front seul : ordre et heure des états, cœur intact, présence, stabilisation, ancre.</summary>
    private void CheckFrontRules()
    {
        LandFrontConfig config = LandFrontConfig.Load(out string error);
        Check(config != null, $"front : réglages valides ({error})");
        if (config == null)
            return;
        Check(LandFrontConfig.Parse(FileAccess.GetFileAsString("res://data/scaling/erasure.json")
            .Replace("\"heart_reached_seconds\": 720", "\"heart_reached_seconds\": 30"), out string order) == null && order.Contains("suivre"),
            $"front : cœur atteint avant la première chute refusé ({order})");
        LandFrontConfig quiet = new()
        {
            FirstFallSeconds = config.FirstFallSeconds, HeartReachedSeconds = config.HeartReachedSeconds,
            WarningSeconds = config.WarningSeconds, HeartRadius = config.HeartRadius, NoiseSeconds = 0f,
            NoiseFrequency = config.NoiseFrequency,
        };
        LandFront front = new(quiet, 1);
        Vector2I heart = new(0, 0), middle = new(1, 0), rim = new(2, 0);
        front.AddCell(heart, 0f);
        // À mi-chemin en surface entre le cœur et le bord : sa chute tombe à mi-chemin entre la première et le cœur atteint.
        front.AddCell(middle, Mathf.Sqrt((1f + quiet.HeartRadius * quiet.HeartRadius) / 2f));
        front.AddCell(rim, 1f);
        float rimFall = quiet.FirstFallSeconds;
        float third = quiet.WarningSeconds / 4f;
        front.Advance(rimFall - quiet.WarningSeconds * 0.5f + 0.1f);
        Check(Phase(front.MemoryAt(rim)) == ErasureManager.ErasureZonePhase.Frayed && Phase(front.MemoryAt(middle)) == ErasureManager.ErasureZonePhase.Anchored,
            $"front : le bord s'effiloche avant le milieu ({front.MemoryAt(rim):F2}, {front.MemoryAt(middle):F2})");
        front.Delay(rim, third);
        Check(Phase(front.MemoryAt(rim)) == ErasureManager.ErasureZonePhase.Fragile, $"front : la présence repousse la chute ({front.MemoryAt(rim):F2})");
        front.Advance(quiet.WarningSeconds);
        Check(front.MemoryAt(rim) == 0f && Phase(front.MemoryAt(rim)) == ErasureManager.ErasureZonePhase.Void,
            "front : le bord tombe dans le Néant après son préavis");
        float middleFall = (quiet.FirstFallSeconds + quiet.HeartReachedSeconds) / 2f;
        front.Advance(middleFall - front.Time - 1f);
        front.Restore(middle, 0.1f);
        Check(front.MemoryAt(middle) > 0f, "front : un lieu ravivé n'avance jamais la chute");
        front.Anchor(middle);
        front.Advance(quiet.HeartReachedSeconds * 3f);
        Check(front.MemoryAt(middle) == 1f && front.MemoryAt(heart) == 1f, "front : zone ancrée et cœur ne tombent jamais");
        Check(Mathf.IsEqualApprox(front.StandingShare(), 2f / 3f), $"front : terre restante {front.StandingShare():F2} (cœur et ancre)");
    }

    /// <summary>Lot P2a sur la vraie terre : toutes ses cellules au front, cœur sûr, bords déjà pâles à l'arrivée.</summary>
    private void CheckFrontOnLand()
    {
        ErasureManager erasure = _world.GetNode<ErasureManager>("ErasureManager");
        LandFront front = erasure.Front;
        Check(front != null && front.CellCount > 1000, $"front de la terre 0 : {front?.CellCount ?? 0} cellules");
        if (front == null)
            return;
        int pale = 0;
        foreach (Vector2I cell in front.Cells)
        {
            if (front.MemoryAt(cell) <= 0.75f)
                pale++;
        }
        Check(erasure.GetMemoryAt(Vector2.Zero) == 1f && pale > 0, $"front de la terre 0 : cœur intact, {pale} cellules du bord déjà fragiles");
        Check(Mathf.IsEqualApprox(front.StandingShare(), 1f), "front de la terre 0 : toute la terre debout à l'arrivée");
    }

    /// <summary>
    /// Lot P2b : sur une terre, raviver un Mémorial, c'est tenir son cercle. S'éloigner l'abandonne ; le tenir jusqu'au
    /// bout le ravive, allume une lumière et ancre sa zone, qui ne tombe plus même quand tout le reste est tombé.
    /// </summary>
    private async Task CheckMemorialCircle()
    {
        MemorialConfig config = Vestiges.Infrastructure.LandmarkDataLoader.Memorial;
        MemorialDirector director = _world.GetNode<MemorialDirector>("MemorialDirector");
        Vestiges.Spawn.SpawnManager spawner = _world.GetNode<Vestiges.Spawn.SpawnManager>("SpawnManager");
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        Memorial memorial = Memorial.All[0];
        _player.IsGodMode = true;
        // Sans apparitions ni créatures : pas d'XP, donc pas d'écran de niveau qui figerait le maintien.
        spawner.ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node child in _world.GetNode("EnemyContainer").GetChildren())
        {
            if (child is Enemy enemy)
                enemy.Withdraw();
        }

        _player.GlobalPosition = memorial.GlobalPosition;
        bus.EmitSignal(EventBus.SignalName.MemorialInteracted, memorial);
        Check(memorial.State == Memorial.MemorialState.Gathering && _world.GetNode("PoiContainer").GetNodeOrNull("MemorialCircle") != null
            && Mathf.IsEqualApprox(spawner.EncounterDensityMultiplier, config.CircleCrowdDensity),
            $"cercle : il apparaît, la foule se resserre (×{spawner.EncounterDensityMultiplier})");
        await Frames(60);
        _player.GlobalPosition = memorial.GlobalPosition + new Vector2(config.CircleAbandonPx + 50f, 0f);
        await Frames(2);
        Check(memorial.State == Memorial.MemorialState.Dormant && spawner.EncounterDensityMultiplier == 1f,
            "cercle : trop loin, la tentative s'arrête et la foule se relâche");

        _player.GlobalPosition = memorial.GlobalPosition;
        bus.EmitSignal(EventBus.SignalName.MemorialInteracted, memorial);
        await Frames(Mathf.CeilToInt(config.CircleSeconds * Engine.PhysicsTicksPerSecond) + 10);
        ChoiceScreen choices = _world.GetNode<ChoiceScreen>("ChoiceScreen");
        // Le réveil animé se déroule en temps réel, même quand le banc accélère le jeu : l'attendre à l'horloge.
        ulong deadline = Time.GetTicksMsec() + 15_000;
        while (!choices.IsOpen && Time.GetTicksMsec() < deadline)
            await Frame();
        for (int attempt = 0; attempt < 10 && choices.IsOpen; attempt++)
        {
            choices.Activate(choices.FirstEnabled());
            await Frame();
        }
        Check(memorial.State == Memorial.MemorialState.Awake && director.LitThisLand == 1 && !GetTree().Paused,
            $"cercle tenu {config.CircleSeconds} s : Mémorial ravivé, {director.LitThisLand} lumière, bénédiction choisie");

        LandFront front = _world.GetNode<ErasureManager>("ErasureManager").Front;
        // Avant que la terre ne soit réduite à son cœur, avec la marge des contrôles suivants : au-delà, le gardien se
        // lèverait seul.
        front.Advance(front.Config.HeartReachedSeconds - 30f - front.Time);
        Vector2I memorialCell = new(Mathf.FloorToInt(memorial.GlobalPosition.X / 128f), Mathf.FloorToInt(memorial.GlobalPosition.Y / 128f));
        Check(front.MemoryAt(memorialCell) == 1f && front.StandingShare() < 0.2f,
            $"cercle : la zone du Mémorial est ancrée quand le reste est tombé (terre restante {front.StandingShare():P0})");
        _player.GlobalPosition = Vector2.Zero;
        _player.IsGodMode = false;
        spawner.ProcessMode = ProcessModeEnum.Inherit;
    }

    /// <summary>
    /// Lot P2d : le Néant d'une terre prend l'Essence d'abord, les PV ensuite. Après le contrôle du cercle, tout le sol
    /// hors du cœur et de l'ancre est tombé : le joueur y est posé.
    /// </summary>
    private async Task CheckVoidDrain()
    {
        EssenceTracker essence = _world.GetNode<EssenceTracker>("EssenceTracker");
        ErasureManager erasure = _world.GetNode<ErasureManager>("ErasureManager");
        LandFrontConfig config = erasure.Front.Config;
        essence.AddEssence(40);
        _player.Heal(_player.EffectiveMaxHp);
        // Hors du cœur et des zones ancrées, qui dépendent de la place des Mémoriaux.
        Vector2 inVoid = new(3000f, 0f);
        for (int i = 0; i < 16 && erasure.GetZonePhaseAt(inVoid) != ErasureManager.ErasureZonePhase.Void; i++)
            inVoid = Vector2.FromAngle(Mathf.Tau * i / 16f) * 3000f;
        _player.GlobalPosition = inVoid;
        int essenceBefore = essence.CurrentEssence;
        float hpBefore = _player.CurrentHp;
        await Frames(120);
        Check(erasure.GetZonePhaseAt(_player.GlobalPosition) == ErasureManager.ErasureZonePhase.Void
            && essence.CurrentEssence < essenceBefore && _player.CurrentHp == hpBefore,
            $"vide : l'Essence paie d'abord ({essenceBefore} → {essence.CurrentEssence}, {config.VoidEssencePerSecond}/s), PV intacts");
        essence.TrySpend(essence.CurrentEssence);
        await Frames(120);
        Check(_player.CurrentHp < hpBefore, $"vide : sans Essence, les PV paient ({hpBefore} → {_player.CurrentHp:F1})");
        _player.GlobalPosition = Vector2.Zero;
        _player.Heal(_player.EffectiveMaxHp);
        await Frames(2);
    }

    /// <summary>
    /// Lot P3 : le repaire au cœur ne s'ouvre qu'avec assez de lumières ; le réveil effondre le sol non ancré et fige le
    /// front ; le gardien vaincu laisse son butin (un coffre de plus par lumière en trop), puis la terre suivante se
    /// recoud avec un rang de Péril de plus. Sur cette terre, attendre le cœur atteint lève le gardien seul, enragé.
    /// </summary>
    private async Task CheckGuardian()
    {
        GuardianConfig config = GuardianConfig.Load(out string error);
        GuardianConfig invalid = GuardianConfig.Parse(FileAccess.GetFileAsString("res://data/world/guardian.json")
            .Replace("\"chest_epic\"", "\"coffre_inconnu\""), out string unknown);
        Check(config != null && invalid == null && unknown != null, $"gardien : réglages valides, coffre inconnu refusé ({error}{unknown})");
        GuardianDirector director = _world.GetNode<GuardianDirector>("GuardianDirector");
        MemorialDirector memorials = _world.GetNode<MemorialDirector>("MemorialDirector");
        LandTransition transition = _world.GetNode<LandTransition>("LandTransition");
        ErasureManager erasure = _world.GetNode<ErasureManager>("ErasureManager");
        PerilManager peril = _world.GetNode<PerilManager>("PerilManager");
        GuardianLair lair = _world.GetNode("PoiContainer").GetNodeOrNull<GuardianLair>("GuardianLair");
        Check(lair != null && !_world.IsWaterAt(lair.GlobalPosition) && _world.GetBiomeAt(lair.GlobalPosition) != null,
            $"gardien : repaire sur du sol praticable ({lair?.GlobalPosition})");
        Check(lair != null && !lair.CanInteract && !director.IsFighting,
            $"gardien : endormi au cœur, repaire fermé avec {memorials.LitThisLand} lumière(s) sur {config.WakeLights}");

        Label objective = (Label)_world.FindChild("LandObjective", true, false);
        string collecting = objective?.Text;
        Check(objective is { Visible: true } && !string.IsNullOrEmpty(collecting), $"objectif de la terre affiché : « {collecting} »");
        memorials.SetLitForTests(config.WakeLights + 1);
        await Frame();
        await Frame();
        Check(lair.CanInteract, "gardien : toutes les lumières allumées, le repaire s'ouvre");
        Check(objective != null && objective.Text != collecting, $"objectif : « {objective?.Text} »");
        float standingBefore = erasure.Front.StandingShare();
        int chestsBefore = Chest.Closed.Count;
        lair.Interact(_player);
        Enemy guardian = director.Guardian;
        Check(director.IsFighting && !director.Enraged && guardian is { IsActive: true } && guardian.VariantId == "champion",
            $"gardien : réveillé ({guardian?.DisplayName}, {guardian?.MaxHp:F0} PV)");
        Check(erasure.FrontFrozen && erasure.Front.StandingShare() < standingBefore,
            $"gardien : le sol non ancré s'effondre, le front se fige ({standingBefore:P0} → {erasure.Front.StandingShare():P0})");

        // Plan 51 §16 G1 : ses coups, ses phases, et la brèche qui s'ouvre au cœur puis se referme.
        guardian.TakeDamage(guardian.MaxHp * 0.5f);
        await Frames(3);
        Check(director.Moves != null && director.Moves.Phase == 1, $"gardien : à mi-PV, deuxième phase ({director.Moves?.Phase + 1})");
        Vector2 strike = new(0f, -120f);
        erasure.BreakGround(strike, 110f, 2f);
        Check(erasure.GetZonePhaseAt(strike) == ErasureManager.ErasureZonePhase.Void, "gardien : sa frappe ouvre le vide, même au cœur");
        _world.GetNode<RunClock>("RunClock").Advance(3.0);
        await Frames(45);
        Check(erasure.GetMemoryAt(strike) == 1f, $"gardien : la brèche se referme ({erasure.GetMemoryAt(strike):F2})");

        int perilBefore = peril.Peril;
        guardian.TakeDamage(guardian.MaxHp * 10f);
        for (int frame = 0; frame < 30 && !director.IsDefeated; frame++)
            await Frame();
        await Frame();
        // Son coffre, un de plus pour la lumière en trop, et celui que tout Souverain lâche à sa mort.
        Check(director.IsDefeated && Chest.Closed.Count >= chestsBefore + 2,
            $"gardien vaincu : son coffre et un de plus pour la lumière en trop ({Chest.Closed.Count - chestsBefore} coffres)");
        int landBefore = transition.LandIndex;
        for (int frame = 0; frame < 600 && transition.LandIndex == landBefore; frame++)
        {
            await Frame();
            if (GetTree().Paused && !transition.IsChanging)
                await SettleChoices();
        }
        while (transition.IsChanging)
            await Frame();
        await SettleChoices();
        Check(transition.LandIndex == landBefore + 1 && peril.Peril == perilBefore + config.RankPeril && !director.IsFighting,
            $"gardien vaincu : la terre {transition.LandIndex} se recoud, Péril {perilBefore} → {peril.Peril}");

        // Pire cas de l'effondrement : une terre encore entière, toutes ses cellules tombent dans la même image.
        LandFront front = erasure.Front;
        float standing = front.StandingShare();
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        erasure.CollapseUnanchored();
        watch.Stop();
        Check(front.StandingShare() < standing && watch.Elapsed.TotalMilliseconds < 16.0,
            $"effondrement d'une terre entière : {front.CellCount} cellules, {standing:P0} → {front.StandingShare():P0} en {watch.Elapsed.TotalMilliseconds:F1} ms");
        front.Advance(front.Config.HeartReachedSeconds);
        await Frames(45);
        Check(director.IsFighting && director.Enraged && erasure.FrontFrozen,
            $"gardien : la terre réduite à son cœur, il se lève seul, enragé ({director.Guardian?.DisplayName})");
        _player.GlobalPosition = Vector2.Zero;
    }

    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++)
            await Frame();
    }

    private static ErasureManager.ErasureZonePhase Phase(float memory) => memory <= 0f ? ErasureManager.ErasureZonePhase.Void
        : memory <= 0.25f ? ErasureManager.ErasureZonePhase.Erased : memory <= 0.5f ? ErasureManager.ErasureZonePhase.Frayed
        : memory <= 0.75f ? ErasureManager.ErasureZonePhase.Fragile : ErasureManager.ErasureZonePhase.Anchored;

    private async Task CheckLands()
    {
        LandTransition transition = _world.GetNode<LandTransition>("LandTransition");
        int start = transition.LandIndex;
        HashSet<string> biomes = new() { _world.Land.BiomeIds[0] };
        Check(_world.Land.BiomeIds.Count == 1 && _world.Generator.MapRadiusX == _world.Land.MapRadius,
            $"terre {start} : un biome ({_world.Land.BiomeIds[0]}), rayon {_world.Generator.MapRadiusX}");
        int memorials = _memorials;
        int chests = _chests;
        Check(GroundHoles() == 0, $"terre {start} : sol sans trou ({GroundHoles()})");
        Check(memorials > 0 && chests > 0 && ObstacleField.Count > 0,
            $"terre {start} peuplée : {memorials} Mémoriaux, {chests} coffres, {ObstacleField.Count} obstacles");

        // Jusqu'à la terre du dernier gardien ; la victoire et la terre du late game se vérifient à part.
        int lastGuardianLand = GuardianConfig.Load(out _).LandsToWin - 1;
        for (int land = start + 1; land <= lastGuardianLand; land++)
        {
            Node enemies = _world.GetNode("EnemyContainer");
            PlaceCrowd(enemies);
            CombatPools.Instance.SpawnXpOrb(_player.GlobalPosition + new Vector2(1500f, 0f), OrbXp);
            await Frame();
            await Frame();
            Check(CountActive(enemies) >= 6, $"terre {land - 1} : foule posée avant le passage ({CountActive(enemies)})");

            Snapshot before = Snapshot.Of(_world, _player);
            float gained = 0f;
            void OnXp(float amount) => gained += amount;
            EventBus bus = GetNode<EventBus>("/root/EventBus");
            bus.XpGained += OnXp;
            await transition.GoToLandAsync(land);
            bus.XpGained -= OnXp;
            Snapshot after = Snapshot.Of(_world, _player);
            // Les orbes ramassées peuvent faire monter de niveau : l'écran de choix prend la pause, le bot choisit.
            await SettleChoices();

            string biome = _world.Land.BiomeIds[0];
            Check(transition.LandIndex == land && _world.Land.Index == land && biomes.Add(biome),
                $"terre {land} : nouveau biome ({biome}), graine {_world.LandSeed}");
            Check(_world.IsWorldReady && _world.Generator.MapRadiusX == _world.Land.MapRadius && !GetTree().Paused && !transition.IsChanging,
                $"terre {land} : construite (rayon {_world.Generator.MapRadiusX}), jeu repris");
            Check(GroundHoles() == 0, $"terre {land} : sol sans trou ({GroundHoles()})");
            LandFront front = _world.GetNode<ErasureManager>("ErasureManager").Front;
            Check(front != null && front.Time == 0f && Mathf.IsEqualApprox(front.StandingShare(), 1f),
                $"terre {land} : front neuf ({front?.CellCount ?? 0} cellules)");
            // Les coffres peuvent naître en jeu (élites, Résurgences) : au moins ceux de la terre, aucun Mémorial de trop.
            Check(Memorial.All.Count == memorials && Chest.Closed.Count >= chests && ObstacleField.Count > 0,
                $"terre {land} : seuls ses lieux comptent ({Memorial.All.Count} Mémoriaux, {Chest.Closed.Count} coffres, {ObstacleField.Count} obstacles)");
            Check(CountActive(enemies) == 0, $"terre {land} : aucune créature de la terre quittée ({CountActive(enemies)})");
            // L'orbe posée, plus celles des créatures tuées juste avant le passage.
            Check(gained >= OrbXp, $"terre {land} : les orbes restées au sol sont revenues ({gained} XP)");
            Check(_player.GlobalPosition == Vector2.Zero, $"terre {land} : joueur au départ ({_player.GlobalPosition})");
            int rankPeril = GuardianConfig.Load(out _)?.RankPeril ?? 0;
            Check(after.SameRunAs(before, rankPeril), $"terre {land} : run intacte, Péril +{rankPeril} ({before} → {after})");

            // Quelques secondes de jeu sur la terre neuve : apparitions, minimap, Effacement tournent sans erreur.
            _player.IsAIControlled = true;
            for (int frame = 0; frame < 240; frame++)
            {
                _player.AIInputOverride = Vector2.FromAngle(frame * 0.02f);
                await Frame();
                if (GetTree().Paused)
                    await SettleChoices();
            }
            _player.IsAIControlled = false;
            Check(_world.GetNode<RunClock>("RunClock").Seconds > after.Clock, $"terre {land} : l'horloge repart après le passage");
        }

        await CheckVictory(biomes);

        // Assez d'XP au sol pour monter de niveau : l'écran de choix garde la pause, la fin du passage ne la lève pas.
        CombatPools.Instance.SpawnXpOrb(_player.GlobalPosition + new Vector2(1500f, 0f), 5000f);
        await Frame();
        await Frame();
        // Par la commande de dev, comme depuis l'atelier de test (F4).
        Vestiges.Development.DevelopmentCommandResult command =
            _world.GetNode<Vestiges.Development.DevelopmentRunTools>("DevelopmentRunTools").Execute("land next");
        Check(command.Success && transition.IsChanging, $"commande « land next » : {command.Message}");
        while (transition.IsChanging)
            await Frame();
        Check(transition.LandIndex == lastGuardianLand + 2, $"commande « land next » : terre {transition.LandIndex}");
        bool levelUpShown = false;
        for (int frame = 0; frame < 30 && !levelUpShown; frame++)
        {
            await Frame();
            levelUpShown = GetTree().Paused && _world.GetNode<CanvasLayer>("LevelUpScreen").Visible;
        }
        Check(levelUpShown, $"terre {transition.LandIndex} : la montée de niveau des orbes ramassées garde la pause sous son écran");
    }

    /// <summary>
    /// Plan 51 §16, L1 et L2 : le dernier gardien vaincu gagne la run (boss battu, endgame), puis la terre du late game
    /// n'a ni repaire ni passage ; un écho de gardien s'y lève, durcit la run, laisse un coffre, et le suivant attend.
    /// </summary>
    private async Task CheckVictory(HashSet<string> biomes)
    {
        GuardianConfig config = GuardianConfig.Load(out _);
        GuardianDirector director = _world.GetNode<GuardianDirector>("GuardianDirector");
        LandTransition transition = _world.GetNode<LandTransition>("LandTransition");
        PerilManager peril = _world.GetNode<PerilManager>("PerilManager");
        GameManager manager = GetNode<GameManager>("/root/GameManager");
        Label objective = (Label)_world.FindChild("LandObjective", true, false);
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        int victories = 0;
        void OnGuardian(int land, bool victory) => victories += victory ? 1 : 0;
        bus.GuardianDefeated += OnGuardian;

        _world.GetNode<MemorialDirector>("MemorialDirector").SetLitForTests(config.WakeLights);
        await Frame();
        await Frame();
        GuardianLair lair = _world.GetNode("PoiContainer").GetNodeOrNull<GuardianLair>("GuardianLair");
        lair?.Interact(_player);
        Enemy guardian = director.Guardian;
        Check(guardian != null && !director.IsLateGame, $"terre {transition.LandIndex} : le dernier gardien est réveillé ({guardian?.DisplayName})");
        guardian?.TakeDamage(guardian.MaxHp * 10f);
        for (int frame = 0; frame < 30 && !director.IsDefeated; frame++)
            await Frame();
        await Frame();
        string won = objective?.Text;
        Check(victories == 1 && manager.CurrentRunPhase == GameManager.RunPhase.Endgame
            && _world.GetNode<Vestiges.Events.EndgameManager>("EndgameManager").IsBossDefeated,
            $"dernier gardien vaincu : run gagnée, phase {manager.CurrentRunPhase}, « {won} »");

        int lastLand = transition.LandIndex;
        int perilBefore = peril.Peril;
        for (int frame = 0; frame < 600 && transition.LandIndex == lastLand; frame++)
        {
            await Frame();
            if (GetTree().Paused && !transition.IsChanging)
                await SettleChoices();
        }
        while (transition.IsChanging)
            await Frame();
        await SettleChoices();
        await Frame();
        Check(director.IsLateGame && _world.GetNode("PoiContainer").GetNodeOrNull("GuardianLair") == null && biomes.Add(_world.Land.BiomeIds[0])
            && peril.Peril == perilBefore + config.RankPeril,
            $"terre {transition.LandIndex}, celle du late game : biome neuf ({_world.Land.BiomeIds[0]}), pas de repaire, Péril {perilBefore} → {peril.Peril}");
        string holding = objective?.Text;
        Check(!string.IsNullOrEmpty(holding) && holding != won, $"terre du late game : objectif « {holding} »");

        int chestsBefore = Chest.Closed.Count;
        perilBefore = peril.Peril;
        typeof(GuardianDirector).GetField("_echoIn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(director, 0f);
        await Frame();
        await Frame();
        Enemy echo = director.Guardian;
        Check(director.IsFighting && echo is { IsActive: true } && peril.Peril == perilBefore + config.EchoPeril && objective?.Text != holding,
            $"écho de gardien levé : {echo?.DisplayName}, {echo?.MaxHp:F0} PV, Péril {perilBefore} → {peril.Peril}, « {objective?.Text} »");
        echo?.TakeDamage(echo.MaxHp * 10f);
        for (int frame = 0; frame < 30 && director.IsFighting; frame++)
            await Frame();
        await Frame();
        Check(!director.IsFighting && director.EchoesDefeated == 1 && Chest.Closed.Count > chestsBefore && victories == 1,
            $"écho vaincu : {director.EchoesDefeated} écho, {Chest.Closed.Count - chestsBefore} coffre(s), « {objective?.Text} »");

        LandFront front = _world.GetNode<ErasureManager>("ErasureManager").Front;
        front.Advance(front.Config.HeartReachedSeconds);
        await Frames(45);
        Check(!director.IsFighting && front.StandingShare() > 0f && transition.LandIndex == lastLand + 1,
            $"terre du late game réduite à son cœur : on y reste ({front.StandingShare():P0} debout), aucun gardien ne se lève");
        bus.GuardianDefeated -= OnGuardian;
        _player.GlobalPosition = Vector2.Zero;
    }

    /// <summary>
    /// Ce que le passage ne doit jamais toucher : PV, armes, Essence, éliminations, horloge. Le niveau peut monter avec
    /// les orbes ramassées au passage ; le Péril monte d'un rang par terre franchie (gardien, P3).
    /// </summary>
    private readonly record struct Snapshot(int Level, float Hp, int Weapons, int Essence, int Peril, int Kills, float Clock)
    {
        public static Snapshot Of(WorldSetup world, Player player) => new(
            player.GetNode<PlayerProgression>("PlayerProgression").CurrentLevel, player.CurrentHp, player.WeaponSlots.Count,
            world.GetNode<EssenceTracker>("EssenceTracker").CurrentEssence, world.GetNode<PerilManager>("PerilManager").Peril,
            world.GetNode<ScoreManager>("ScoreManager").TotalKills, world.GetNode<RunClock>("RunClock").Seconds);

        public bool SameRunAs(Snapshot before, int rankPeril) => Level >= before.Level && Hp == before.Hp
            && Weapons == before.Weapons && Essence == before.Essence && Peril == before.Peril + rankPeril
            && Kills == before.Kills && Clock == before.Clock;
    }

    /// <summary>Cellules de sol de la carte sans tuile : un terrain que le biome ne sait pas dessiner laisse un trou.</summary>
    private int GroundHoles()
    {
        WorldGenerator generator = _world.Generator;
        TileMapLayer ground = _world.GetNode<TileMapLayer>("Ground");
        int holes = 0;
        for (int x = -generator.MapRadiusX; x <= generator.MapRadiusX; x++)
        {
            for (int y = -generator.MapRadiusY; y <= generator.MapRadiusY; y++)
            {
                if (generator.IsWithinBounds(x, y) && !generator.IsErased(x, y) && ground.GetCellSourceId(new Vector2I(x, y)) < 0)
                    holes++;
            }
        }
        return holes;
    }

    private void PlaceCrowd(Node container)
    {
        Vestiges.Spawn.EnemyPool pool = _world.GetNode<Vestiges.Spawn.EnemyPool>("EnemyPool");
        for (int i = 0; i < 6; i++)
        {
            Enemy enemy = pool.Get();
            enemy.Position = _player.GlobalPosition + Vector2.FromAngle(i) * 300f;
            container.AddChild(enemy);
            enemy.Initialize(Vestiges.Infrastructure.EnemyDataLoader.Get("shade"), 1f, 1f);
        }
    }

    private static int CountActive(Node container)
    {
        int active = 0;
        foreach (Node child in container.GetChildren())
        {
            // Une créature déjà en train de mourir finit sa mort, puis revient au pool.
            if (child is Enemy { IsActive: true, IsDying: false })
                active++;
        }
        return active;
    }

    /// <summary>Choisit comme un joueur pressé : première carte d'un écran de niveau ou de choix, jusqu'à la reprise.</summary>
    private async Task SettleChoices()
    {
        ChoiceScreen choices = _world.GetNode<ChoiceScreen>("ChoiceScreen");
        Node levelUp = _world.GetNode("LevelUpScreen");
        System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        for (int attempt = 0; attempt < 200 && GetTree().Paused; attempt++)
        {
            if (choices.IsOpen)
                choices.Activate(choices.FirstEnabled());
            else if (levelUp.GetType().GetField("_fragmentManager", flags)?.GetValue(levelUp) is FragmentManager fragments
                && fragments.IsChoiceActive && fragments.PendingChoices.Count > 0)
                levelUp.GetType().GetMethod("OnCardChosen", flags).Invoke(levelUp, new object[] { fragments.PendingChoices[0] });
            await Frame();
        }
    }

    private SignalAwaiter Frame() => ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

    private void Check(bool passed, string message)
    {
        GD.Print($"[LandChangeRegression] {(passed ? "PASS" : "FAIL")} {message}");
        if (!passed)
            _failures++;
    }
}
