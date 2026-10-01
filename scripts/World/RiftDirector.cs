using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.UI;

namespace Vestiges.World;

/// <summary>
/// Mène les Failles (plan 17 lot 3C) : offres d'améliorations Épiques ou Légendaires, chacune payée d'un Oubli et
/// d'un point de Péril, toujours refusables ; ouverture de nouvelles Failles là où le monde s'efface.
/// Réglages : section <c>rift</c> de data/world/landmarks.json.
/// </summary>
public partial class RiftDirector : Node
{
    private const uint PropCollisionLayer = 4;

    private readonly RiftConfig _config = LandmarkDataLoader.Rift;
    private readonly RandomNumberGenerator _rng = new();
    private ChoiceScreen _choices;
    private PerilManager _peril;
    private ErasureManager _erasure;
    private WorldSetup _world;
    private EventBus _eventBus;
    private Player _player;
    private float _spawnCooldown;
    private int _spawned;

    public void Setup(ChoiceScreen choices, PerilManager peril, ErasureManager erasure)
    {
        _choices = choices;
        _peril = peril;
        _erasure = erasure;
    }

    public override void _Ready()
    {
        _rng.Randomize();
        _world = GetParent<WorldSetup>();
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.RiftInteracted += OnRiftInteracted;
        _eventBus.ZonePhaseChanged += OnZonePhaseChanged;
        // Pas d'ouverture dans les premières secondes : les Failles du départ suffisent.
        _spawnCooldown = _config.SpawnCooldown;
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;
        _eventBus.RiftInteracted -= OnRiftInteracted;
        _eventBus.ZonePhaseChanged -= OnZonePhaseChanged;
    }

    public override void _Process(double delta)
    {
        if (_spawnCooldown > 0f)
            _spawnCooldown -= (float)delta;
    }

    // ==============================
    // Offres
    // ==============================

    private void OnRiftInteracted(Node2D node)
    {
        if (node is not Rift rift || !rift.IsOpen || !CachePlayer())
            return;

        List<FragmentOption> candidates = new();
        foreach (WeaponInstance weapon in _player.WeaponSlots)
            if (weapon.CanLevelUp)
                candidates.Add(new FragmentOption(weapon.Id, "weapon_upgrade", weapon.Name, 1));
        foreach (ActivePassiveSouvenir passive in _player.PassiveSlots)
            if (!passive.IsMaxLevel)
                candidates.Add(new FragmentOption(passive.Id, "passive_upgrade", passive.Data.Name, 1));
        if (candidates.Count == 0)
            return;

        ErasureManager.ErasureZonePhase phase = _erasure?.GetZonePhaseAt(rift.GlobalPosition) ?? ErasureManager.ErasureZonePhase.Anchored;
        float bump = UpgradeRoller.BumpSteps(_player.LuckBonus, phase, _peril.Peril);
        List<OubliData> oubliPool = new(OubliDataLoader.All);
        List<(FragmentOption Option, OubliData Oubli)> offers = new();
        List<ChoiceCard> cards = new();
        // Peu de cibles (début de run : une arme) : la même revient, avec d'autres gains et un autre Oubli, pour
        // qu'il reste un vrai choix.
        List<FragmentOption> remaining = new(candidates);
        for (int i = 0; i < _config.Offers && oubliPool.Count > 0; i++)
        {
            if (remaining.Count == 0)
                remaining.AddRange(candidates);
            FragmentOption candidate = remaining[_rng.RandiRange(0, remaining.Count - 1)];
            remaining.Remove(candidate);
            OubliData oubli = oubliPool[_rng.RandiRange(0, oubliPool.Count - 1)];
            oubliPool.Remove(oubli);
            UpgradeRarity rarity = UpgradeRoller.RollRarityAtLeast(bump, _config.OfferMinRarity, _rng);
            FragmentOption option = UpgradeRoller.RollGains(candidate, _player, rarity, _rng);
            offers.Add((option, oubli));
            cards.Add(BuildCard(option, oubli));
        }

        _choices.Open(Tr("RIFT_TITLE"), Tr("RIFT_SUBTITLE"), cards, Tr("RIFT_REFUSE"), choice =>
        {
            if (choice < 0 || !CachePlayer() || !offers[choice].Option.ApplyTo(_player))
                return;
            _peril.AddOubli(offers[choice].Oubli);
            _peril.AddPeril(_config.PerilPerOffer);
            rift.Close();
            _eventBus.EmitSignal(EventBus.SignalName.RiftUsed, rift.GlobalPosition);
            EmitSparks(rift.GlobalPosition);
        });
    }

    private ChoiceCard BuildCard(FragmentOption option, OubliData oubli)
    {
        ChoiceCard card = new()
        {
            Tag = $"{ChoiceStyle.RarityGlyph(option.Rarity.Rank)} {RarityPalette.DisplayName(option.Rarity.Id).ToUpper()}".Trim(),
            Frame = RarityPalette.Main(option.Rarity.Id),
            Rank = option.Rarity.Rank,
            Title = option.DisplayName,
            Icon = option.Type == "weapon_upgrade" ? LoadIcon(WeaponDataLoader.Get(option.Id)?.Sprite) : null,
        };
        card.Lines.AddRange(UpgradeText.Describe(option, _player));
        string permanent = oubli.Permanent ? $"  ({Tr("OUBLI_PERMANENT")})" : "";
        card.Lines.Add((string.Format(Tr("RIFT_OUBLI_LINE"), Tr(oubli.NameKey), oubli.Describe()) + permanent, ChoiceStyle.LossColor));
        card.Lines.Add((string.Format(Tr("RIFT_PERIL_LINE"), _config.PerilPerOffer), ChoiceStyle.LossColor));
        return card;
    }

    // ==============================
    // Ouvertures dans les zones oubliées
    // ==============================

    private void OnZonePhaseChanged(int cellX, int cellY, int phase)
    {
        if (phase != (int)ErasureManager.ErasureZonePhase.Erased || _spawnCooldown > 0f || _erasure == null || !CachePlayer())
            return;
        if (_rng.Randf() >= _config.SpawnChance || CountOpen() >= _config.MaxOpen)
            return;

        Vector2 position = _erasure.CellCenterToWorld(new Vector2I(cellX, cellY));
        float distance = position.DistanceTo(_player.GlobalPosition);
        if (distance < _config.SpawnDistanceMin || distance > _config.SpawnDistanceMax || !IsFree(position))
            return;

        Spawn(position);
        _spawnCooldown = _config.SpawnCooldown;
    }

    private void Spawn(Vector2 position)
    {
        Rift rift = new() { Name = $"OpenedRift{++_spawned}", GlobalPosition = position };
        rift.Initialize(_config);
        _world.GetNode<Node2D>("PoiContainer").AddChild(rift);
        GD.Print($"[RiftDirector] Faille ouverte en {position.Round()}");
    }

    private bool IsFree(Vector2 position)
    {
        if (_world.IsWaterAt(position))
            return false;
        IReadOnlyList<Rift> all = Rift.All;
        for (int i = 0; i < all.Count; i++)
            if (all[i].GlobalPosition.DistanceTo(position) < _config.MinSpacingPx)
                return false;
        PhysicsShapeQueryParameters2D query = new()
        {
            Shape = new CircleShape2D { Radius = 24f },
            CollisionMask = PropCollisionLayer,
            Transform = new Transform2D(0f, position),
        };
        return _world.GetWorld2D().DirectSpaceState.IntersectShape(query, 1).Count == 0;
    }

    private static int CountOpen()
    {
        int open = 0;
        IReadOnlyList<Rift> all = Rift.All;
        for (int i = 0; i < all.Count; i++)
            if (all[i].IsOpen)
                open++;
        return open;
    }

    private static Texture2D LoadIcon(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        string resPath = path.StartsWith("res://") ? path : $"res://{path}";
        return ResourceLoader.Exists(resPath) ? GD.Load<Texture2D>(resPath) : null;
    }

    private static void EmitSparks(Vector2 position)
    {
        CombatPools.Instance?.EmitSparks(position + new Vector2(0f, -6f), new SparkBurst
        {
            Family = FxFamily.Void,
            Owner = FxOwner.Player,
            Count = 18,
            Direction = Vector2.Up,
            Spread = 2.6f,
            SpeedMin = 50f,
            SpeedMax = 140f,
            LifeMin = 0.35f,
            LifeMax = 0.8f,
            Ballistic = true,
            Size = 2,
        });
    }

    private bool CachePlayer()
    {
        if (_player == null || !IsInstanceValid(_player))
            _player = GetTree().GetFirstNodeInGroup("player") as Player;
        return _player != null;
    }
}
