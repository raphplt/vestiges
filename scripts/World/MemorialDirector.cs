using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.UI;

namespace Vestiges.World;

/// <summary>
/// Mène les Mémoriaux (plan 17 lot 3B) : rassemblement des éclats, bénédictions au réveil, services contre de
/// l'Essence, perte dans le Néant. Réglages : section <c>memorial</c> de data/world/landmarks.json.
/// </summary>
public partial class MemorialDirector : Node
{
    private const float LossCheckInterval = 1f;
    private const float ShardSpacingPx = 90f;
    private const uint PropCollisionLayer = 4;
    private const string ServiceHeal = "heal";
    private const string ServiceLift = "lift";
    private const string ServiceReroll = "reroll";
    private const float RevivalSec = 0.8f;
    // Point de la stèle où se fond la lumière, au-dessus de son pied.
    private static readonly Vector2 RevivalFocus = new(0f, -24f);

    private readonly MemorialConfig _config = LandmarkDataLoader.Memorial;
    private readonly RandomNumberGenerator _rng = new();
    private readonly List<MemoryShard> _shards = new();
    private ChoiceScreen _choices;
    private LandmarkReveal _reveal;
    private EssenceTracker _essence;
    private ErasureManager _erasure;
    private PerilManager _peril;
    private WorldSetup _world;
    private EventBus _eventBus;
    private Player _player;
    private Memorial _gathering;
    private float _gatherRemaining;
    private int _collected;
    private float _lossTimer = LossCheckInterval;
    private int _collapsed;

    public void Setup(ChoiceScreen choices, LandmarkReveal reveal, EssenceTracker essence, ErasureManager erasure, PerilManager peril)
    {
        _choices = choices;
        _reveal = reveal;
        _essence = essence;
        _erasure = erasure;
        _peril = peril;
    }

    public override void _Ready()
    {
        _rng.Seed = RunRandom.SeedFor("memorials");
        _world = GetParent<WorldSetup>();
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.MemorialInteracted += OnMemorialInteracted;
        _eventBus.OubliEffectChanged += OnOubliEffectChanged;
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;
        _eventBus.MemorialInteracted -= OnMemorialInteracted;
        _eventBus.OubliEffectChanged -= OnOubliEffectChanged;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (_gathering != null)
            UpdateGathering(dt);

        _lossTimer -= dt;
        if (_lossTimer <= 0f)
        {
            _lossTimer = LossCheckInterval;
            CheckLost();
        }
    }

    private void OnMemorialInteracted(Node2D node)
    {
        if (node is not Memorial memorial || !CachePlayer())
            return;
        if (memorial.State == Memorial.MemorialState.Dormant)
            StartGathering(memorial);
        else if (memorial.State == Memorial.MemorialState.Awake)
            OpenServices(memorial, null);
    }

    // ==============================
    // Éclats
    // ==============================

    private void StartGathering(Memorial memorial)
    {
        if (_gathering != null)
            EndGathering(false);

        Texture2D texture = GD.Load<Texture2D>($"res://{_config.SpriteShard}");
        Node2D container = _world.GetNode<Node2D>("PoiContainer");
        List<Vector2> placed = new();
        for (int i = 0; i < _config.Shards; i++)
        {
            if (!TryFindShardPosition(memorial.GlobalPosition, placed, out Vector2 position))
                continue;
            placed.Add(position);
            MemoryShard shard = new() { Name = $"MemoryShard{i + 1}", GlobalPosition = position };
            shard.Initialize(texture);
            container.AddChild(shard);
            _shards.Add(shard);
        }

        _gathering = memorial;
        _gatherRemaining = _config.ShardTime;
        _collected = 0;
        memorial.SetState(Memorial.MemorialState.Gathering);
        UpdateStatus();
        AudioManager.PlayUI("sfx_chest_opening");
    }

    /// <summary>Un point libre autour du Mémorial : ni eau, ni Néant, ni décor bloquant, écarté des autres éclats.</summary>
    private bool TryFindShardPosition(Vector2 center, List<Vector2> placed, out Vector2 position)
    {
        PhysicsDirectSpaceState2D space = _world.GetWorld2D().DirectSpaceState;
        PhysicsShapeQueryParameters2D query = new()
        {
            Shape = new CircleShape2D { Radius = 12f },
            CollisionMask = PropCollisionLayer,
        };
        for (int attempt = 0; attempt < 40; attempt++)
        {
            float angle = _rng.RandfRange(0f, Mathf.Tau);
            float distance = _rng.RandfRange(_config.ShardDistanceMin, _config.ShardDistanceMax);
            position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            if (_world.IsWaterAt(position) || _erasure?.GetZonePhaseAt(position) == ErasureManager.ErasureZonePhase.Void)
                continue;
            bool crowded = false;
            foreach (Vector2 other in placed)
                crowded |= other.DistanceTo(position) < ShardSpacingPx;
            if (crowded)
                continue;
            query.Transform = new Transform2D(0f, position);
            if (space.IntersectShape(query, 1).Count > 0)
                continue;
            return true;
        }
        position = default;
        return false;
    }

    private void UpdateGathering(float dt)
    {
        _gatherRemaining -= dt;
        for (int i = _shards.Count - 1; i >= 0; i--)
        {
            MemoryShard shard = _shards[i];
            if (_player.GlobalPosition.DistanceTo(shard.GlobalPosition) > _config.ShardPickupPx)
                continue;
            EmitSparks(shard.GlobalPosition, 8);
            shard.QueueFree();
            _shards.RemoveAt(i);
            _collected++;
            AudioManager.PlayUI("sfx_menu_confirmer");
        }

        if (_shards.Count == 0)
            EndGathering(_collected > 0);
        else if (_gatherRemaining <= 0f)
            EndGathering(false);
        else
            UpdateStatus();
    }

    private void UpdateStatus()
    {
        _gathering.ShowStatus(string.Format(Tr("MEMORIAL_GATHER_STATUS"), _collected, _collected + _shards.Count,
            Mathf.CeilToInt(_gatherRemaining)));
    }

    private void EndGathering(bool success)
    {
        Memorial memorial = _gathering;
        _gathering = null;
        foreach (MemoryShard shard in _shards)
            shard.QueueFree();
        _shards.Clear();
        if (!success)
        {
            memorial.SetState(Memorial.MemorialState.Dormant);
            return;
        }

        if (_reveal == null)
        {
            Awaken(memorial, true);
            return;
        }
        Texture2D shardTexture = GD.Load<Texture2D>($"res://{_config.SpriteShard}");
        int collected = _collected;
        _reveal.Play(memorial.GlobalPosition + RevivalFocus, RarityPalette.Colors("memorial").Light, RevivalSec,
            progress =>
            {
                if (memorial.ShowRevival(progress, collected, shardTexture) && memorial.State != Memorial.MemorialState.Awake)
                {
                    memorial.SetState(Memorial.MemorialState.Awake);
                    AudioManager.PlayUI("sfx_souvenir_trouve");
                }
            },
            skipped =>
            {
                memorial.EndRevival();
                Awaken(memorial, !skipped);
            });
    }

    /// <summary>Le Mémorial est ravivé : il le dit au monde et offre ses bénédictions.</summary>
    private void Awaken(Memorial memorial, bool animate)
    {
        if (memorial.State != Memorial.MemorialState.Awake)
            memorial.SetState(Memorial.MemorialState.Awake);
        EmitSparks(memorial.GlobalPosition + new Vector2(0f, -20f), 22);
        _eventBus.EmitSignal(EventBus.SignalName.MemorialAwakened, memorial.GlobalPosition);
        OpenBlessings(memorial, animate);
    }

    // ==============================
    // Bénédictions
    // ==============================

    private void OpenBlessings(Memorial memorial, bool animate = true)
    {
        List<StatEffectData> pool = new(BlessingDataLoader.All);
        List<StatModifier> modifiers = new();
        List<ChoiceCard> cards = new();
        float bump = BumpSteps(memorial.GlobalPosition);
        for (int i = 0; i < _config.BlessingChoices && pool.Count > 0; i++)
        {
            StatEffectData blessing = pool[_rng.RandiRange(0, pool.Count - 1)];
            pool.Remove(blessing);
            UpgradeRarity rarity = UpgradeRoller.RollRarityAtLeast(bump, _config.BlessingMinRarity, _rng);
            StatModifier modifier = StatModifier.Scaled(blessing.Stat, blessing.ModifierType, blessing.Amount, rarity.PassiveGain);
            modifiers.Add(modifier);
            ChoiceCard card = RarityCard(rarity, Tr(blessing.NameKey));
            card.Lines.Add((modifier.Describe(), ChoiceStyle.GainColor));
            cards.Add(card);
        }

        // Relance payante : trois nouvelles offres, plus chère à chaque usage au même Mémorial.
        int essence = _essence?.CurrentEssence ?? 0;
        int rerollCost = Price(_config.BlessingRerollCost, memorial.ServiceUses(ServiceReroll));
        ChoiceCard reroll = new()
        {
            Tag = Tr("MEMORIAL_REROLL_TAG").ToUpper(),
            Frame = RarityPalette.Main("memorial"),
            Title = Tr("MEMORIAL_REROLL_TITLE"),
            Price = string.Format(Tr("MEMORIAL_PRICE"), rerollCost),
            Enabled = essence >= rerollCost,
        };
        reroll.Lines.Add((string.Format(Tr("MEMORIAL_REROLL_LINE"), modifiers.Count), ChoiceStyle.TextColor));
        cards.Add(reroll);

        _choices.Open(Tr("MEMORIAL_AWAKE_TITLE"), Tr("MEMORIAL_AWAKE_SUBTITLE"), cards, null, choice =>
        {
            if (choice == modifiers.Count)
                RerollBlessings(memorial, rerollCost);
            else if (choice >= 0 && CachePlayer())
                modifiers[choice].ApplyTo(_player);
        }, PixelBackdrop.MemorialTint, animate);
    }

    /// <summary>Relance : les nouvelles bénédictions s'ouvrent sans rejouer l'entrée.</summary>
    private void RerollBlessings(Memorial memorial, int cost)
    {
        if (_essence == null || !_essence.TrySpend(cost))
        {
            OpenBlessings(memorial, false);
            return;
        }
        memorial.RecordServiceUse(ServiceReroll);
        OpenBlessings(memorial, false);
    }

    // ==============================
    // Services
    // ==============================

    private void OpenServices(Memorial memorial, string lastResult)
    {
        List<ChoiceCard> cards = new();
        List<System.Action> actions = new();
        int essence = _essence?.CurrentEssence ?? 0;

        int healCost = Price(_config.HealCost, memorial.ServiceUses(ServiceHeal));
        ChoiceCard heal = new()
        {
            Tag = Tr("MEMORIAL_HEAL_TAG").ToUpper(),
            Frame = RarityPalette.Main("memorial"),
            Title = Tr("MEMORIAL_HEAL_TITLE"),
            Price = string.Format(Tr("MEMORIAL_PRICE"), healCost),
            Enabled = essence >= healCost && _player.CurrentHp < _player.EffectiveMaxHp - 0.1f,
        };
        heal.Lines.Add((string.Format(Tr("MEMORIAL_HEAL_LINE"), Mathf.RoundToInt(_config.HealPercent * 100f)), ChoiceStyle.GainColor));
        cards.Add(heal);
        actions.Add(() => Heal(memorial, healCost));

        // Lever un Oubli (lot 3C) : le malus s'efface, le Péril gagné à la Faille reste.
        int liftCost = Price(_config.LiftOubliCost, memorial.ServiceUses(ServiceLift));
        foreach (ActiveOubli oubli in _peril.Oublis)
        {
            if (oubli.Data.Permanent)
                continue;
            ChoiceCard card = new()
            {
                Tag = Tr("MEMORIAL_LIFT_TAG").ToUpper(),
                Frame = RarityPalette.Main("rift"),
                Title = string.Format(Tr("MEMORIAL_LIFT_TITLE"), Tr(oubli.Data.NameKey)),
                Price = string.Format(Tr("MEMORIAL_PRICE"), liftCost),
                Enabled = essence >= liftCost,
            };
            card.Lines.Add((oubli.Data.Describe(), ChoiceStyle.LossColor));
            cards.Add(card);
            ActiveOubli target = oubli;
            actions.Add(() => LiftOubli(memorial, target, liftCost));
        }

        string subtitle = string.Format(Tr("MEMORIAL_ESSENCE"), essence);
        if (!string.IsNullOrEmpty(lastResult))
            subtitle = $"{lastResult}   ·   {subtitle}";
        // Rouvert après chaque achat : l'entrée ne se joue qu'à la première ouverture.
        _choices.Open(Tr("MEMORIAL_SERVICES_TITLE"), subtitle, cards, Tr("MEMORIAL_LEAVE"), choice =>
        {
            if (choice >= 0 && CachePlayer())
                actions[choice]();
        }, PixelBackdrop.MemorialTint, string.IsNullOrEmpty(lastResult));
    }

    private void Heal(Memorial memorial, int cost)
    {
        if (!_essence.TrySpend(cost))
            return;
        _player.Heal(_player.EffectiveMaxHp * _config.HealPercent);
        memorial.RecordServiceUse(ServiceHeal);
        OpenServices(memorial, Tr("MEMORIAL_HEALED"));
    }

    private void LiftOubli(Memorial memorial, ActiveOubli oubli, int cost)
    {
        if (!_essence.TrySpend(cost))
            return;
        if (!_peril.LiftOubli(oubli))
        {
            _essence.AddEssence(cost);
            OpenServices(memorial, null);
            return;
        }
        memorial.RecordServiceUse(ServiceLift);
        OpenServices(memorial, string.Format(Tr("MEMORIAL_LIFTED"), Tr(oubli.Data.NameKey)));
    }

    private int Price(int baseCost, int uses) => Mathf.RoundToInt(baseCost * (1f + _config.CostGrowth * uses));

    // ==============================
    // Néant
    // ==============================

    /// <summary>
    /// Oubli des lieux (plan 17 lot 3D) : à chaque Oubli pris, le Mémorial endormi le plus proche du joueur
    /// s'effondre, comme englouti par le Néant.
    /// </summary>
    private void OnOubliEffectChanged(string effect, float total)
    {
        if (effect != "collapse_memorial" || !CachePlayer())
            return;
        for (int target = Mathf.RoundToInt(total); _collapsed < target; _collapsed++)
        {
            Memorial nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (Memorial memorial in Memorial.All)
            {
                float distance = memorial.GlobalPosition.DistanceTo(_player.GlobalPosition);
                if (memorial.State == Memorial.MemorialState.Dormant && distance < nearestDistance)
                {
                    nearest = memorial;
                    nearestDistance = distance;
                }
            }
            if (nearest == null)
                return;
            nearest.SetState(Memorial.MemorialState.Lost);
            EmitSparks(nearest.GlobalPosition + new Vector2(0f, -20f), 16);
        }
    }

    private void CheckLost()
    {
        if (_erasure == null)
            return;
        IReadOnlyList<Memorial> all = Memorial.All;
        for (int i = 0; i < all.Count; i++)
        {
            Memorial memorial = all[i];
            if (memorial.State == Memorial.MemorialState.Lost
                || _erasure.GetZonePhaseAt(memorial.GlobalPosition) != ErasureManager.ErasureZonePhase.Void)
                continue;
            if (memorial == _gathering)
                EndGathering(false);
            memorial.SetState(Memorial.MemorialState.Lost);
        }
    }

    // ==============================
    // Outils
    // ==============================

    /// <summary>Crans de montée de rareté : Chance du joueur, oubli de la zone du Mémorial, Péril.</summary>
    private float BumpSteps(Vector2 position)
    {
        ErasureManager.ErasureZonePhase phase = _erasure?.GetZonePhaseAt(position) ?? ErasureManager.ErasureZonePhase.Anchored;
        return UpgradeRoller.BumpSteps(_player.LuckBonus, phase, _peril.Peril);
    }

    private static ChoiceCard RarityCard(UpgradeRarity rarity, string title) => new()
    {
        Tag = RarityPalette.DisplayName(rarity.Id).ToUpper(),
        Frame = RarityPalette.Main(rarity.Id),
        Rank = rarity.Rank,
        Title = title,
    };

    private static Texture2D LoadIcon(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        string resPath = path.StartsWith("res://") ? path : $"res://{path}";
        return ResourceLoader.Exists(resPath) ? GD.Load<Texture2D>(resPath) : null;
    }

    private static void EmitSparks(Vector2 position, int count)
    {
        CombatPools.Instance?.EmitSparks(position, new SparkBurst
        {
            Family = FxFamily.Silk,
            Owner = FxOwner.Player,
            Count = count,
            Direction = Vector2.Up,
            Spread = 2.6f,
            SpeedMin = 50f,
            SpeedMax = 130f,
            LifeMin = 0.35f,
            LifeMax = 0.7f,
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
