using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.Spawn;

namespace Vestiges.World;

/// <summary>
/// Gardien de chaque terre (plan 51 P3). Il dort au cœur ; les Mémoriaux ravivés allument ses lumières, et dès qu'il y
/// en a assez, on le réveille en touchant son repaire. Si la terre se réduit à son cœur avant, il se lève seul, enragé.
/// Au réveil, tout le sol non ancré s'effondre : l'arène est ce qu'on a sauvé, et le front se fige jusqu'à sa mort.
/// Vaincu, il laisse son butin, puis la terre suivante se recoud ; chaque terre franchie ajoute du Péril.
/// Le dernier gardien à vaincre gagne la run (§16 L1). La terre suivante est celle du late game (§16 L2) : ni repaire ni
/// passage, mais un écho de gardien qui se lève près du joueur à intervalles, plus fort à chaque fois, jusqu'à la mort.
/// Chaque terre a son gardien, une créature à part que nomme son biome (plan 57 G0) ; les échos reprennent ceux que le
/// joueur a vaincus.
/// </summary>
public partial class GuardianDirector : Node
{
    private enum Phase { Sleeping, Fighting, Defeated, Passage, Holding, Echo }

    // Distance au joueur où l'écho se lève : hors de portée immédiate, dans l'écran.
    private const int EchoPlacementTries = 12;

    private WorldSetup _world;
    private ErasureManager _erasure;
    private MemorialDirector _memorials;
    private LandTransition _transition;
    private SpawnManager _spawner;
    private PerilManager _peril;
    private EventBus _eventBus;
    private GuardianConfig _config;
    private GuardianLair _lair;
    private Enemy _guardian;
    private GuardianMoves _moves;
    // Gardien, rang et forces du dernier posé, pour le reposer à l'identique s'il est retiré sans mourir.
    private (string Id, int Rank, float Strength, float Damage) _spawned;
    // Gardiens vaincus dans la run, dans l'ordre : les échos du late game les reprennent chacun son tour.
    private readonly List<string> _beaten = new();
    private BossBarFeed _bar;
    private Phase _phase;
    private int _lights;
    private float _passageIn;
    private float _echoIn;
    private int _echoes;
    private bool _arenaClosed;
    private RandomNumberGenerator _echoRng;
    private (int Land, int Lights, int Stage) _published = (-1, -1, -1);
    // Secondes avant que le gardien se lève seul, publiées pendant l'alerte ; −1 hors alerte.
    private int _rageCountdown = -1;

    /// <summary>Le gardien de la terre en cours s'est-il levé de lui-même (terre réduite à son cœur) ?</summary>
    public bool Enraged { get; private set; }

    public bool IsFighting => _phase is Phase.Fighting or Phase.Echo;

    /// <summary>Terre du late game : après le dernier gardien à vaincre, sans fin.</summary>
    public bool IsLateGame => _transition.LandIndex >= _config.LandsToWin;

    /// <summary>Échos de gardien vaincus sur la terre du late game.</summary>
    public int EchoesDefeated => _echoes;

    public bool IsDefeated => _phase == Phase.Defeated;

    public Enemy Guardian => _guardian;

    /// <summary>Coups du gardien en combat ; nuls hors combat.</summary>
    public GuardianMoves Moves => _moves;

    public void Setup(WorldSetup world, ErasureManager erasure, MemorialDirector memorials, LandTransition transition,
        SpawnManager spawner, PerilManager peril)
    {
        _world = world;
        _erasure = erasure;
        _memorials = memorials;
        _transition = transition;
        _spawner = spawner;
        _peril = peril;
    }

    public override void _Ready()
    {
        _config = GuardianConfig.Load(out string error);
        if (_config == null)
        {
            GD.PushError($"[GuardianDirector] {error} : terres sans gardien");
            SetProcess(false);
            return;
        }
        _echoRng = RunRandom.Create("guardian/echo");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.LandChanging += OnLandChanging;
        _eventBus.LandChanged += OnLandChanged;
        PlaceLair();
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;
        _eventBus.LandChanging -= OnLandChanging;
        _eventBus.LandChanged -= OnLandChanged;
    }

    public override void _Process(double delta)
    {
        PublishObjective();
        switch (_phase)
        {
            case Phase.Sleeping:
                _lair?.SetLit(_memorials.LitThisLand);
                LandFront front = _erasure.Front;
                if (front != null && front.Time >= front.Config.HeartReachedSeconds)
                    Wake(true);
                else
                    UpdateRageCountdown(front);
                break;
            case Phase.Fighting:
                UpdateFight();
                break;
            case Phase.Defeated:
                _passageIn -= (float)delta;
                if (_passageIn <= 0f && !_transition.IsChanging)
                    StartPassage();
                break;
            case Phase.Holding:
                _echoIn -= (float)delta;
                if (_echoIn <= 0f)
                    RaiseEcho();
                break;
            case Phase.Echo:
                UpdateEcho();
                break;
        }
    }

    /// <summary>
    /// Dernière minute avant que le gardien se lève seul (plan 57 C2) : le temps qui reste au rythme de l'oubli du
    /// moment, en secondes de jeu, publié à chaque seconde ; le son marque l'entrée dans l'alerte, une fois par terre.
    /// Entrée, l'alerte ne s'éteint plus avant le réveil : un oubli qui ralentit le front la borne à sa durée, un front
    /// arrêté la fige.
    /// </summary>
    private void UpdateRageCountdown(LandFront front)
    {
        if (front == null)
            return;
        float rate = _erasure.FrontRate;
        if (rate <= 0f)
            return;
        int seconds = Mathf.Max(0, Mathf.CeilToInt((front.Config.HeartReachedSeconds - front.Time) / rate));
        int warning = Mathf.CeilToInt(_config.RageWarningSeconds);
        if (seconds > warning)
        {
            if (_rageCountdown < 0)
                return;
            seconds = warning;
        }
        if (_rageCountdown < 0)
            AudioManager.PlayUI(_config.RageWarningAudio);
        SetRageCountdown(seconds);
    }

    private void SetRageCountdown(int seconds)
    {
        if (seconds == _rageCountdown)
            return;
        _rageCountdown = seconds;
        _eventBus.EmitSignal(EventBus.SignalName.GuardianRageCountdown, seconds);
    }

    /// <summary>L'objectif de la terre, publié seulement quand il change.</summary>
    private void PublishObjective()
    {
        int lights = _phase switch
        {
            Phase.Sleeping => _memorials.LitThisLand,
            Phase.Holding or Phase.Echo => _echoes,
            _ => _lights,
        };
        int stage = _phase switch
        {
            Phase.Sleeping => lights >= _config.WakeLights ? 1 : 0,
            Phase.Fighting => Enraged ? 3 : 2,
            Phase.Holding => 5,
            Phase.Echo => 6,
            _ => _transition.LandIndex == _config.LandsToWin - 1 ? 7 : 4,
        };
        (int, int, int) objective = (_transition.LandIndex, lights, stage);
        if (objective == _published)
            return;
        _published = objective;
        _eventBus.EmitSignal(EventBus.SignalName.LandObjectiveChanged, _transition.LandIndex, lights, _config.WakeLights, stage);
    }

    /// <summary>Passage vers la terre suivante, une seule fois ; jamais derrière l'écran de fin de run.</summary>
    private async void StartPassage()
    {
        _phase = Phase.Passage;
        if (GetNode<GameManager>("/root/GameManager").CurrentState != GameManager.GameState.Run)
            return;
        try
        {
            await _transition.GoToLandAsync(_transition.LandIndex + 1);
        }
        catch (System.Exception exception)
        {
            GD.PushError($"[GuardianDirector] Passage vers la terre suivante impossible : {exception}");
        }
    }

    /// <summary>Le joueur touche le repaire, toutes les lumières allumées.</summary>
    public void WakeByPlayer()
    {
        if (_phase == Phase.Sleeping && _memorials.LitThisLand >= _config.WakeLights)
            Wake(false);
    }

    private void PlaceLair()
    {
        _phase = Phase.Sleeping;
        Enraged = false;
        _guardian = null;
        _lair = new GuardianLair { Name = "GuardianLair", GlobalPosition = _config.LairOffset };
        _lair.Setup(this, _config.WakeLights, EnemyDataLoader.Get(LandGuardian(_config.LairOffset) ?? string.Empty));
        _world.GetNode<Node2D>("PoiContainer").AddChild(_lair);
    }

    private void Wake(bool enraged)
    {
        SetRageCountdown(-1);
        _phase = Phase.Fighting;
        Enraged = enraged;
        _lights = _memorials.LitThisLand;
        _lair.Awaken();
        _erasure.CollapseUnanchored();
        _erasure.FrontFrozen = true;
        int swallowed = SwallowStranded();
        ScreenShake.Instance?.ShakeMedium();
        AudioManager.PlayUI(_config.RiseAudio);

        Vector2 position = _lair.GlobalPosition;
        float extra = Mathf.Max(0, _lights - _config.WakeLights) * _config.ExtraLightHp;
        float strength = (1f + extra) * (enraged ? _config.RageHp : 1f);
        if (!SpawnGuardian(LandGuardian(position), position, _transition.LandIndex, strength, enraged ? _config.RageDamage : 1f, _config.Escorts))
        {
            GD.PushError("[GuardianDirector] Gardien impossible à poser : passage direct à la terre suivante");
            Defeat(position);
            return;
        }
        GD.Print($"[GuardianDirector] Gardien de la terre {_transition.LandIndex} réveillé ({(enraged ? "enragé, terre réduite à son cœur" : $"{_lights} lumière(s)")}) : {_guardian.DisplayName}, {_guardian.MaxHp:F0} PV, {swallowed} créatures emportées par l'effondrement");
    }

    /// <summary>
    /// Le sol qui s'effondre emporte les créatures qui s'y tenaient : retirées sans butin ni élimination, l'arène ne garde
    /// que la foule qui l'avait atteinte.
    /// </summary>
    private int SwallowStranded()
    {
        int swallowed = 0;
        foreach (Node child in _world.GetNode("EnemyContainer").GetChildren())
        {
            // Les créatures d'un événement restent : leur objectif compte sur leur mort.
            if (child is Enemy { IsActive: true, IsDying: false } enemy && !enemy.Modifiers.IsEventBound
                && _erasure.GetZonePhaseAt(enemy.GlobalPosition) == ErasureManager.ErasureZonePhase.Void)
            {
                enemy.Withdraw();
                swallowed++;
            }
        }
        return swallowed;
    }

    /// <summary>
    /// L'arène s'ouvre au plus à fight_crowd_cap créatures (plan 57 C2) : averti, le joueur rejoint le cœur avec sa horde,
    /// qui ferait au gardien un rempart que ses armes ne traversent pas. Les plus éloignées du joueur s'en vont, sans
    /// butin, comme celles du sol qui s'effondre ; le gardien et les créatures d'un événement restent.
    /// </summary>
    private int ThinArena()
    {
        Vector2 center = GetNode<GroupCache>("/root/GroupCache").GetPlayer() is Node2D player ? player.GlobalPosition : _guardian.GlobalPosition;
        List<(float Distance, Enemy Enemy)> crowd = new();
        foreach (Node child in _world.GetNode("EnemyContainer").GetChildren())
        {
            if (child is Enemy { IsActive: true, IsDying: false } enemy && enemy != _guardian && !enemy.Modifiers.IsEventBound)
                crowd.Add((enemy.GlobalPosition.DistanceSquaredTo(center), enemy));
        }
        if (crowd.Count <= _config.FightCrowdCap)
            return 0;
        crowd.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        for (int i = _config.FightCrowdCap; i < crowd.Count; i++)
            crowd[i].Enemy.Withdraw();
        return crowd.Count - _config.FightCrowdCap;
    }

    /// <summary>Le gardien que nomme le biome de cette place, celui de la terre.</summary>
    private string LandGuardian(Vector2 position) => _world.GetBiomeAt(position)?.Guardian;

    /// <summary>
    /// Pose un gardien (ou un écho) : la créature <paramref name="guardianId"/>, sans variante ni affixe, aux PV de sa
    /// fiche × <see cref="GuardianConfig.HpRankGrowth"/> par rang × <paramref name="strength"/>, avec son escorte de
    /// créatures de la terre et sa barre de boss.
    /// </summary>
    private bool SpawnGuardian(string guardianId, Vector2 position, int rank, float strength, float damage, int escorts)
    {
        _spawned = (guardianId, rank, strength, damage);
        EnemyData data = EnemyDataLoader.Get(guardianId);
        _guardian = data?.Guardian != null ? _spawner.SpawnEventEnemy(guardianId, position) : null;
        if (_guardian == null)
            return false;
        // Ni le temps, ni le Péril, ni la difficulté : un combat de une à deux minutes quel que soit le moment du réveil.
        float hp = data.Stats.Hp * Mathf.Pow(_config.HpRankGrowth, rank) * strength;
        _guardian.Empower(hp / Mathf.Max(1f, _guardian.MaxHp), damage);
        _guardian.SetBodyRadius(data.Guardian.BodyRadius);
        _guardian.Modifiers.IsEventBound = true;
        _guardian.SetTargetBias(_config.TargetBias);
        _moves = new GuardianMoves(_guardian, _config.Moves, data.Guardian.Moves, _world, _erasure.BreakGround);
        _guardian.AddAbility(_moves);
        int thinned = ThinArena();
        if (thinned > 0)
            GD.Print($"[GuardianDirector] Arène ramenée à {_config.FightCrowdCap} créatures : {thinned} emportées");
        for (int i = 0; i < escorts; i++)
        {
            Vector2 escort = position + Vector2.FromAngle(Mathf.Tau * i / Mathf.Max(1, escorts)) * 90f;
            string escortId = _spawner.PickLocalEnemyId(escort);
            if (escortId != null && _spawner.IsSpawnablePosition(escort))
                _spawner.SpawnEventEnemy(escortId, escort);
        }
        CloseArena();
        _bar = new BossBarFeed(_eventBus, _guardian.DisplayName, _guardian.MaxHp);
        return true;
    }

    private void UpdateFight()
    {
        if (_guardian.IsDying)
        {
            Defeat(_guardian.GlobalPosition);
            return;
        }
        if (!_guardian.IsActive && !ReplaceLost(_lair.GlobalPosition))
        {
            GD.PushError("[GuardianDirector] Gardien perdu et impossible à reposer : passage sans victoire ni butin");
            _phase = Phase.Defeated;
            _passageIn = _config.PassageDelaySeconds;
            OpenArena();
            return;
        }
        _bar.Update(_guardian.CurrentHp, _guardian.MaxHp);
    }

    /// <summary>
    /// Gardien retiré sans mourir (rendu au pool par un autre système) : ce n'est pas une victoire. Il revient à
    /// <paramref name="position"/>, avec les mêmes PV de départ, sans escorte.
    /// </summary>
    private bool ReplaceLost(Vector2 position)
    {
        EndMoves();
        _bar?.End(false);
        _bar = null;
        GD.PushWarning("[GuardianDirector] Gardien retiré sans mourir : il revient");
        return SpawnGuardian(_spawned.Id, position, _spawned.Rank, _spawned.Strength, _spawned.Damage, 0);
    }

    /// <summary>Le gardien tombe : ses coffres, un de plus par lumière au-delà du compte, puis le passage.</summary>
    private void Defeat(Vector2 position)
    {
        if (_guardian != null && !_beaten.Contains(_spawned.Id))
            _beaten.Add(_spawned.Id);
        if (_guardian != null)
            PlayDeathCry(position);
        _phase = Phase.Defeated;
        EndMoves();
        OpenArena();
        _bar?.End(true);
        _bar = null;
        _guardian = null;
        for (int i = 0; i < _config.Chests.Count; i++)
            DropChest(_config.Chests[i], position + new Vector2(36f * i, 0f));
        int extra = Mathf.Max(0, _lights - _config.WakeLights);
        for (int i = 0; i < extra; i++)
            DropChest(_config.ExtraLightChest, position + Vector2.FromAngle(Mathf.Tau * (i + 1) / (extra + 1)) * 48f);
        _passageIn = _config.PassageDelaySeconds;
        bool victory = _transition.LandIndex == _config.LandsToWin - 1;
        if (victory)
            AudioManager.PlayUI(_config.VictoryAudio);
        _eventBus.EmitSignal(EventBus.SignalName.GuardianDefeated, _transition.LandIndex, victory);
        GD.Print($"[GuardianDirector] Gardien de la terre {_transition.LandIndex} vaincu{(victory ? " : la run est gagnée" : "")}");
    }

    /// <summary>Terre du late game : on tient, et le prochain écho se lève après un répit.</summary>
    private void StartHolding(float delay)
    {
        _phase = Phase.Holding;
        _echoIn = delay;
    }

    /// <summary>
    /// Un écho de gardien se lève près du joueur, au rang suivant le dernier, et la run se durcit : l'un des gardiens
    /// vaincus, chacun son tour ; à défaut (terre atteinte sans victoire, en test), celui de la terre.
    /// </summary>
    private void RaiseEcho()
    {
        Vector2 position = EchoPosition();
        int rank = _config.LandsToWin + _echoes;
        string echoId = _beaten.Count > 0 ? _beaten[_echoes % _beaten.Count] : LandGuardian(position);
        if (!SpawnGuardian(echoId, position, rank, _config.EchoHpMultiplier, 1f, _config.EchoEscorts))
        {
            GD.PushError("[GuardianDirector] Écho de gardien impossible à poser : nouvel essai au prochain intervalle");
            StartHolding(_config.EchoIntervalSeconds);
            return;
        }
        _phase = Phase.Echo;
        _peril.AddPeril(_config.EchoPeril);
        ScreenShake.Instance?.ShakeMedium();
        AudioManager.PlayUI(_config.RiseAudio);
        GD.Print($"[GuardianDirector] Écho de gardien n°{_echoes + 1} (rang {rank}) : {_guardian.DisplayName}, {_guardian.MaxHp:F0} PV");
    }

    /// <summary>Une place debout dans l'écran autour du joueur ; à défaut, le cœur, qui ne tombe jamais.</summary>
    private Vector2 EchoPosition()
    {
        if (GetNode<GroupCache>("/root/GroupCache").GetPlayer() is not Node2D player)
            return _config.LairOffset;
        float start = _echoRng.Randf() * Mathf.Tau;
        for (int i = 0; i < EchoPlacementTries; i++)
        {
            Vector2 candidate = player.GlobalPosition + Vector2.FromAngle(start + Mathf.Tau * i / EchoPlacementTries) * _config.EchoDistance;
            if (_spawner.IsSpawnablePosition(candidate) && _erasure.GetZonePhaseAt(candidate) < ErasureManager.ErasureZonePhase.Erased)
                return candidate;
        }
        return _config.LairOffset;
    }

    private void UpdateEcho()
    {
        if (!_guardian.IsDying && !_guardian.IsActive && !ReplaceLost(EchoPosition()))
        {
            GD.PushError("[GuardianDirector] Écho perdu et impossible à reposer : le suivant viendra à l'heure");
            _guardian = null;
            OpenArena();
            StartHolding(_config.EchoIntervalSeconds);
            return;
        }
        if (_guardian.IsDying)
        {
            Vector2 position = _guardian.GlobalPosition;
            EndMoves();
            OpenArena();
            _bar?.End(true);
            _bar = null;
            _guardian = null;
            _echoes++;
            PlayDeathCry(position);
            DropChest(_config.EchoChest, position);
            _eventBus.EmitSignal(EventBus.SignalName.GuardianDefeated, _transition.LandIndex, false);
            GD.Print($"[GuardianDirector] Écho de gardien n°{_echoes} vaincu");
            StartHolding(_config.EchoIntervalSeconds);
            return;
        }
        _bar.Update(_guardian.CurrentHp, _guardian.MaxHp);
    }

    private void PlayDeathCry(Vector2 position) =>
        AudioManager.PlayAt(EnemyDataLoader.Get(_spawned.Id).Guardian.DeathAudio, position, 0.05f, 0f);

    private void DropChest(string chestId, Vector2 position)
    {
        ChestData data = ChestDataLoader.Get(chestId);
        if (data == null)
            return;
        Chest chest = GD.Load<PackedScene>("res://scenes/world/Chest.tscn").Instantiate<Chest>();
        chest.GlobalPosition = position;
        _world.GetNode<Node2D>("PoiContainer").AddChild(chest);
        chest.Initialize(data);
    }

    /// <summary>
    /// Arène fermée (plan 56 B1) : jusqu'à la mort du gardien, le temps de difficulté s'arrête, rien d'autre ne se
    /// déclenche et la foule est allégée et plafonnée ; les armes automatiques visent le plus proche, une foule qui
    /// grossirait protégerait le gardien sans fin.
    /// </summary>
    private void CloseArena()
    {
        _spawner.EncounterDensityMultiplier = _config.FightCrowdDensity;
        _spawner.EncounterCrowdCap = _config.FightCrowdCap;
        if (_arenaClosed)
            return;
        _arenaClosed = true;
        _eventBus.EmitSignal(EventBus.SignalName.GuardianArenaChanged, true);
    }

    private void OpenArena()
    {
        _spawner.EncounterDensityMultiplier = 1f;
        _spawner.EncounterCrowdCap = 0;
        if (!_arenaClosed)
            return;
        _arenaClosed = false;
        _eventBus.EmitSignal(EventBus.SignalName.GuardianArenaChanged, false);
    }

    /// <summary>Les coups s'arrêtent avec le combat ; leurs annonces au sol disparaissent.</summary>
    private void EndMoves()
    {
        _moves?.Detach();
        _moves = null;
    }

    private void OnLandChanging(int toIndex)
    {
        SetRageCountdown(-1);
        EndMoves();
        OpenArena();
        _bar?.End(false);
        _bar = null;
        _guardian = null;
        _lair = null;
    }

    /// <summary>Terre suivante : un gardien neuf l'attend (ou les échos du late game), et la run se durcit d'un rang.</summary>
    private void OnLandChanged(int index)
    {
        if (index > 0)
            _peril.AddPeril(_config.RankPeril);
        Enraged = false;
        if (IsLateGame)
        {
            _echoes = 0;
            StartHolding(_config.EchoFirstSeconds);
            GD.Print($"[GuardianDirector] Terre {index}, celle du late game : premier écho dans {_config.EchoFirstSeconds:F0} s");
        }
        else
            PlaceLair();
    }
}
