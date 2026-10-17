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
/// Prototype : le gardien est un Souverain de la terre, renforcé.
/// </summary>
public partial class GuardianDirector : Node
{
    private enum Phase { Sleeping, Fighting, Defeated, Passage }

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
    private BossBarFeed _bar;
    private Phase _phase;
    private int _lights;
    private float _passageIn;
    private (int Land, int Lights, int Stage) _published = (-1, -1, -1);

    /// <summary>Le gardien de la terre en cours s'est-il levé de lui-même (terre réduite à son cœur) ?</summary>
    public bool Enraged { get; private set; }

    public bool IsFighting => _phase == Phase.Fighting;

    public bool IsDefeated => _phase == Phase.Defeated;

    public Enemy Guardian => _guardian;

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
                break;
            case Phase.Fighting:
                UpdateFight();
                break;
            case Phase.Defeated:
                _passageIn -= (float)delta;
                if (_passageIn <= 0f && !_transition.IsChanging)
                    StartPassage();
                break;
        }
    }

    /// <summary>L'objectif de la terre, publié seulement quand il change.</summary>
    private void PublishObjective()
    {
        int lights = _phase == Phase.Sleeping ? _memorials.LitThisLand : _lights;
        int stage = _phase switch
        {
            Phase.Sleeping => lights >= _config.WakeLights ? 1 : 0,
            Phase.Fighting => Enraged ? 3 : 2,
            _ => 4,
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
        _lair.Setup(this, _config.WakeLights);
        _world.GetNode<Node2D>("PoiContainer").AddChild(_lair);
    }

    private void Wake(bool enraged)
    {
        _phase = Phase.Fighting;
        Enraged = enraged;
        _lights = _memorials.LitThisLand;
        _lair.Awaken();
        _erasure.CollapseUnanchored();
        _erasure.FrontFrozen = true;
        ScreenShake.Instance?.ShakeMedium();
        AudioManager.PlayUI("sfx_barrier_rise");

        Vector2 position = _lair.GlobalPosition;
        string enemyId = _spawner.PickLocalEnemyId(position);
        _guardian = _spawner.SpawnEventEnemy(enemyId, position, "champion");
        if (_guardian == null)
        {
            GD.PushError("[GuardianDirector] Gardien impossible à poser : passage direct à la terre suivante");
            Defeat(position);
            return;
        }
        float extra = Mathf.Max(0, _lights - _config.WakeLights) * _config.ExtraLightHp;
        _guardian.Empower(_config.HpMultiplier * (1f + extra) * (enraged ? _config.RageHp : 1f), enraged ? _config.RageDamage : 1f);
        _guardian.Modifiers.IsEventBound = true;
        for (int i = 0; i < _config.Escorts; i++)
        {
            Vector2 escort = position + Vector2.FromAngle(Mathf.Tau * i / Mathf.Max(1, _config.Escorts)) * 90f;
            if (_spawner.IsSpawnablePosition(escort))
                _spawner.SpawnEventEnemy(enemyId, escort);
        }
        _bar = new BossBarFeed(_eventBus, _guardian.DisplayName, _guardian.MaxHp);
        GD.Print($"[GuardianDirector] Gardien de la terre {_transition.LandIndex} réveillé ({(enraged ? "enragé, terre réduite à son cœur" : $"{_lights} lumière(s)")}) : {_guardian.DisplayName}, {_guardian.MaxHp:F0} PV");
    }

    private void UpdateFight()
    {
        if (_guardian.IsDying)
        {
            Defeat(_guardian.GlobalPosition);
            return;
        }
        // Retiré sans mourir (rendu au pool par un autre système) : la terre ne doit pas rester bloquée.
        if (!_guardian.IsActive)
        {
            Defeat(_lair.GlobalPosition);
            return;
        }
        _bar.Update(_guardian.CurrentHp, _guardian.MaxHp);
    }

    /// <summary>Le gardien tombe : son coffre, un de plus par lumière au-delà du compte, puis le passage.</summary>
    private void Defeat(Vector2 position)
    {
        _phase = Phase.Defeated;
        _bar?.End(true);
        _bar = null;
        _guardian = null;
        DropChest(_config.Chest, position);
        int extra = Mathf.Max(0, _lights - _config.WakeLights);
        for (int i = 0; i < extra; i++)
            DropChest(_config.ExtraLightChest, position + Vector2.FromAngle(Mathf.Tau * (i + 1) / (extra + 1)) * 48f);
        _passageIn = _config.PassageDelaySeconds;
        GD.Print($"[GuardianDirector] Gardien de la terre {_transition.LandIndex} vaincu");
    }

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

    private void OnLandChanging(int toIndex)
    {
        _bar?.End(false);
        _bar = null;
        _guardian = null;
        _lair = null;
    }

    /// <summary>Terre suivante : un gardien neuf l'attend, et la run se durcit d'un rang.</summary>
    private void OnLandChanged(int index)
    {
        if (index > 0)
            _peril.AddPeril(_config.RankPeril);
        PlaceLair();
    }
}
