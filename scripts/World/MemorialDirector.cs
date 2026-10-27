using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Combat.Abilities;
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
    // Les créatures d'une vague montent juste derrière le bord du cercle ; leur annonce au sol est un petit disque.
    private const float WaveEdgePx = 26f;
    private const float WaveMarkPx = 20f;
    // Longer le bord ne fait pas grincer l'alerte de sortie à chaque pas.
    private const float LeaveAudioCooldown = 1.5f;
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
    private GatherCountdown _countdown;
    private float _gatherRemaining;
    private int _collected;
    private float _lossTimer = LossCheckInterval;
    private int _collapsed;
    // Cercle à tenir des terres (plan 51 P2b).
    private Memorial _holding;
    private MemorialCircle _circle;
    private float _held;
    private bool _inside;
    private float _leaveAudioIn;
    // Vagues du cercle (plan 57 C4) : la prochaine à monter, ses points au bord, le temps d'annonce qui reste (négatif au repos).
    private int _nextWave;
    private float _waveIn = -1f;
    private Vector2[] _wavePoints;
    private GroundTelegraph[] _waveMarks;
    private Spawn.SpawnManager _spawner;

    /// <summary>Mémoriaux ravivés sur la terre en cours : les lumières du gardien (P3).</summary>
    public int LitThisLand { get; private set; }

#if TOOLS
    /// <summary>Banc des terres : allume des lumières sans raviver de Mémorial, dont le réveil se joue en temps réel.</summary>
    internal void SetLitForTests(int lit)
    {
        DevelopmentMode.RequireTestAccess();
        LitThisLand = lit;
    }
#endif

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
        _eventBus.LandChanging += OnLandChanging;
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;
        _eventBus.MemorialInteracted -= OnMemorialInteracted;
        _eventBus.OubliEffectChanged -= OnOubliEffectChanged;
        _eventBus.LandChanging -= OnLandChanging;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (_gathering != null)
            UpdateGathering(dt);
        if (_holding != null)
            UpdateHolding(dt);

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
        if (memorial.State == Memorial.MemorialState.Dormant && _world.Land?.IsLand == true)
            StartHolding(memorial);
        else if (memorial.State == Memorial.MemorialState.Dormant)
            StartGathering(memorial);
        else if (memorial.State == Memorial.MemorialState.Awake)
            OpenServices(memorial, null);
    }

    // ==============================
    // Éclats
    // ==============================

    /// <summary>
    /// Changement de terre (plan 51) : une collecte d'éclats en cours s'arrête sans rien rendre ; ses éclats et son
    /// Mémorial partent avec la terre (conteneur des lieux vidé).
    /// </summary>
    private void OnLandChanging(int toIndex)
    {
        _gathering = null;
        _countdown?.End();
        _shards.Clear();
        _collected = 0;
        _gatherRemaining = 0f;
        // Le cercle part avec les lieux de la terre ; la foule retrouve sa densité.
        if (_holding != null && _spawner != null)
            _spawner.EncounterDensityMultiplier = 1f;
        _holding = null;
        _circle = null;
        FreeWaveMarks();
        LitThisLand = 0;
    }

    // ==============================
    // Cercle à tenir (terres, plan 51 P2b)
    // ==============================

    /// <summary>
    /// Le cercle apparaît autour du Mémorial ; la foule se resserre tant qu'on le tient. Les marques au sol des vagues
    /// naissent à la première, sur le monde et non sur les lieux : la terre qui part les emporte aussi (<see cref="FreeWaveMarks"/>).
    /// </summary>
    private void StartHolding(Memorial memorial)
    {
        if (_gathering != null)
            EndGathering(false);
        if (_holding != null)
            EndHolding(false);
        _holding = memorial;
        _held = 0f;
        _inside = true;
        _leaveAudioIn = 0f;
        _nextWave = 0;
        _waveIn = -1f;
        memorial.SetState(Memorial.MemorialState.Gathering);
        _circle = new MemorialCircle { Name = "MemorialCircle", GlobalPosition = memorial.GlobalPosition };
        _circle.Setup(_config.CircleRadiusPx, _config.CircleCloseShare, RarityPalette.Colors("memorial").Light);
        _world.GetNode<Node2D>("PoiContainer").AddChild(_circle);
        _spawner ??= _world.GetNode<Spawn.SpawnManager>("SpawnManager");
        _spawner.EncounterDensityMultiplier = _config.CircleCrowdDensity;
        AudioManager.PlayUI("sfx_chest_opening");
    }

    /// <summary>
    /// Dans le cercle, le temps tenu monte ; dehors, il redescend et l'anneau rougit ; trop loin, la tentative s'arrête.
    /// Aux parts tenues réglées, une vague monte du bord ; au dernier tiers, le cercle se resserre (plan 57 C4).
    /// </summary>
    private void UpdateHolding(float dt)
    {
        float distance = Iso.ToGround(_player.GlobalPosition - _holding.GlobalPosition).Length();
        if (distance > _config.CircleAbandonPx)
        {
            EndHolding(false);
            return;
        }
        float radius = CircleRadius(_held / _config.CircleSeconds);
        bool inside = distance <= radius;
        _leaveAudioIn -= dt;
        if (_inside && !inside && _leaveAudioIn <= 0f)
        {
            AudioManager.PlayUI(_config.CircleLeaveAudio);
            _leaveAudioIn = LeaveAudioCooldown;
        }
        _inside = inside;
        _held = inside ? _held + dt : Mathf.Max(0f, _held - dt * _config.CircleLeaveDecay);
        float progress = _held / _config.CircleSeconds;
        UpdateWave(dt, progress, radius);
        _circle.SetState(progress, radius, inside, Mathf.CeilToInt(_config.CircleSeconds - _held));
        if (_held >= _config.CircleSeconds)
            EndHolding(true);
    }

    /// <summary>Rayon du cercle : entier jusqu'au resserrement, puis il se referme jusqu'à sa part finale.</summary>
    private float CircleRadius(float progress)
    {
        float closing = Mathf.Clamp((progress - _config.CircleCloseFrom) / (1f - _config.CircleCloseFrom), 0f, 1f);
        return _config.CircleRadiusPx * Mathf.Lerp(1f, _config.CircleCloseShare, closing);
    }

    /// <summary>Une vague s'annonce au sol tout autour du bord, puis ses créatures, celles du biome, y montent.</summary>
    private void UpdateWave(float dt, float progress, float radius)
    {
        if (_waveIn >= 0f)
        {
            _waveIn -= dt;
            if (_waveIn < 0f)
                RaiseWave();
            return;
        }
        if (_nextWave >= _config.CircleWaveAt.Length || progress < _config.CircleWaveAt[_nextWave])
            return;
        _nextWave++;
        _waveIn = _config.CircleWaveWarning;
        if (_waveMarks == null)
        {
            _wavePoints = new Vector2[_config.CircleWaveCount];
            _waveMarks = new GroundTelegraph[_config.CircleWaveCount];
            for (int i = 0; i < _waveMarks.Length; i++)
            {
                _waveMarks[i] = new GroundTelegraph { Name = $"MemorialWaveMarker{i}" };
                _world.AddChild(_waveMarks[i]);
            }
        }
        float turn = _rng.Randf() * Mathf.Tau;
        for (int i = 0; i < _wavePoints.Length; i++)
        {
            Vector2 ground = Vector2.FromAngle(turn + Mathf.Tau * i / _wavePoints.Length) * (radius + WaveEdgePx);
            _wavePoints[i] = _holding.GlobalPosition + Iso.ToScreen(ground);
            _waveMarks[i].ShowCircle(_wavePoints[i], WaveMarkPx, FxFamily.Hostile);
        }
        AudioManager.PlayAt(_config.CircleWaveAudio, _holding.GlobalPosition, 0.05f, -4f);
    }

    private void RaiseWave()
    {
        _waveIn = -1f;
        int raised = 0;
        for (int i = 0; i < _wavePoints.Length; i++)
        {
            _waveMarks[i].HideMarker();
            Vector2 point = _wavePoints[i];
            if (!_spawner.IsSpawnablePosition(point) || _player.IsVoidAt(point))
                continue;
            string id = _spawner.PickLocalEnemyId(point);
            if (id != null && _spawner.SpawnEventEnemy(id, point) != null)
                raised++;
        }
        GD.Print($"[MemorialDirector] Vague du cercle n°{_nextWave} : {raised} créature(s)");
    }

    private void EndHolding(bool success)
    {
        Memorial memorial = _holding;
        _holding = null;
        _circle?.QueueFree();
        _circle = null;
        FreeWaveMarks();
        _spawner.EncounterDensityMultiplier = 1f;
        if (!success)
        {
            memorial.SetState(Memorial.MemorialState.Dormant);
            return;
        }
        Burst(memorial.GlobalPosition);
        Revive(memorial, _config.Shards);
    }

    /// <summary>
    /// Le Mémorial ravivé lâche une onde : les créatures à portée perdent une part de leurs PV et sont repoussées, comme par
    /// la percussion, jusque dans le vide s'il est là. Les gardiens, mini-boss et lourds ne bougent pas.
    /// </summary>
    private void Burst(Vector2 center)
    {
        float reach = _config.CircleBurstRadiusPx;
        int hit = 0;
        using (CrowdQuery crowd = CrowdIndex.Near(center, reach))
        {
            foreach (Node node in crowd.Targets)
            {
                if (node is not Enemy { IsActive: true, IsDying: false } enemy || !IsInstanceValid(enemy)
                    || enemy.Tier == EnemyTier.Guardian || Iso.GroundDistanceSquared(center, enemy.GlobalPosition) > reach * reach)
                    continue;
                Vector2 away = Iso.ToGround(enemy.GlobalPosition - center);
                enemy.Percuss(Iso.ToScreen(away == Vector2.Zero ? Vector2.Right : away.Normalized()), _config.CircleBurstPushPx);
                enemy.TakeDamage(enemy.MaxHp * _config.CircleBurstDamageShare);
                hit++;
            }
        }
        PixelFxSpec wave = PixelFxSpec.Of(PixelFxShape.Zone, FxFamily.Brass, reach, 1f, 0.35f);
        wave.Matter = ZoneMatter.Burst;
        wave.Squash = Iso.GroundSquash;
        wave.Steps = 5;
        wave.FadeTail = 0.5f;
        wave.ZIndex = -1;
        CombatPools.Instance?.PlayFx(center, wave, FxOwner.Player);
        ScreenShake.Instance?.ShakeMedium();
        AudioManager.PlayAt(_config.CircleBurstAudio, center, 0.05f, -2f);
        GD.Print($"[MemorialDirector] Onde du Mémorial ravivé : {hit} créature(s) touchée(s)");
    }

    private void FreeWaveMarks()
    {
        if (_waveMarks == null)
            return;
        foreach (GroundTelegraph mark in _waveMarks)
        {
            if (IsInstanceValid(mark))
                mark.QueueFree();
        }
        _waveMarks = null;
        _waveIn = -1f;
    }

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
        Countdown().Begin(_gatherRemaining);
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
        _countdown.Show(_collected, _collected + _shards.Count, _gatherRemaining);
    }

    /// <summary>Compte à rebours qui suit le joueur, créé au premier ramassage.</summary>
    private GatherCountdown Countdown()
    {
        if (_countdown != null)
            return _countdown;
        _countdown = new GatherCountdown { Name = "GatherCountdown" };
        _countdown.Setup(_player, RarityPalette.Colors("memorial").Light, _config.ShardUrgentTime);
        AddChild(_countdown);
        return _countdown;
    }

    private void EndGathering(bool success)
    {
        Memorial memorial = _gathering;
        _gathering = null;
        _countdown?.End();
        foreach (MemoryShard shard in _shards)
            shard.QueueFree();
        _shards.Clear();
        if (!success)
        {
            memorial.SetState(Memorial.MemorialState.Dormant);
            return;
        }
        Revive(memorial, _collected);
    }

    /// <summary>Réveil animé du Mémorial, autant d'éclats montés que <paramref name="collected"/>, puis ses bénédictions.</summary>
    private void Revive(Memorial memorial, int collected)
    {
        if (_reveal == null)
        {
            Awaken(memorial, true);
            return;
        }
        Texture2D shardTexture = GD.Load<Texture2D>($"res://{_config.SpriteShard}");
        _reveal.Play(memorial.GlobalPosition + RevivalFocus, RarityPalette.Colors("memorial").Light, RevivalSec,
            progress =>
            {
                if (memorial.ShowRevival(progress, collected, shardTexture) && memorial.State != Memorial.MemorialState.Awake)
                {
                    memorial.SetState(Memorial.MemorialState.Awake);
                    AudioManager.PlayUI("sfx_memorial_wake");
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
        {
            memorial.SetState(Memorial.MemorialState.Awake);
            AudioManager.PlayUI("sfx_memorial_wake");
        }
        EmitSparks(memorial.GlobalPosition + new Vector2(0f, -20f), 22);
        if (_world.Land?.IsLand == true)
            LitThisLand++;
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
            if (memorial == _holding)
                EndHolding(false);
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
        return UpgradeRoller.BumpSteps(_player.LuckBonus, phase);
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
