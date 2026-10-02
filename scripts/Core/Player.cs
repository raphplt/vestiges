using System.Collections.Generic;
using System.Linq;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.Core;

public partial class Player : CharacterBody2D
{
    [Export] public float Speed = 200f;
    [Export] public float AttackDamage = 10f;
    [Export] public float AttackSpeed = 1.0f;
    [Export] public float AttackRange = 300f;
    [Export] public float MaxHp = 100f;
    [Export] public float BaseRegenRate = 0.5f;
    [Export] public float InteractRange = 60f;

    // Entrée de simulation dans les axes écran, amplitude analogique bornée à 1.
    public bool IsAIControlled;
    public Vector2 AIInputOverride;
    
    // Debug
    public bool IsGodMode { get; set; } = false;

    public PlayerMobility Mobility { get; private set; }
    private MobilityFeedback _mobilityFeedback;
    private bool _mobilityRequiresRelease;
    private ErasureManager _erasureManager;

    private GameManager _gameManager;
    private GroupCache _groupCache;

    private string _characterId;
    private float _currentHp;
    private bool _isDead;
    private WeaponInstance _equippedWeapon; // Active weapon context for attack helpers
    private Polygon2D _visual;
    private Color _originalColor;
    private EventBus _eventBus;
    private Tween _attackFeedbackTween;

    // Weapon inventory (max 4 weapons, all auto-attack in parallel)
    public const int MaxWeaponSlots = 4;
    private readonly List<WeaponInstance> _weaponSlots = new();
    private readonly List<Timer> _weaponTimers = new();

    private readonly WeaponLedger _weaponLedger = new();

    private Vector2 _facingDirection = new(1f, 0f);

    // Sprite animé (remplace Polygon2D quand sprite_folder est défini)
    private AnimatedSprite2D _sprite;
    private PlayerAttackFx _attackFx;
    private HeldWeapon _heldWeapon;
    private bool _hasSprite;
    private enum SpriteAction { Idle, Walk, Hurt, Death, Dash }
    private static readonly string[] SpriteActionNames = { "idle", "walk", "hurt", "death", "dash" };
    private readonly CharacterFacing _facing = new();
    private StringName _currentAnimName;
    // Noms précalculés [direction, action] : aucune chaîne construite par tick.
    private static readonly StringName[,] SpriteAnimations = BuildSpriteAnimations();
    private const float MovementSpeedEpsilon = 0.1f;
    private const float SpriteFeetBelowOrigin = 10f;
    private float _hurtAnimTimer;

    // Shader VFX unifié (outline + hit flash + dissolve)
    private static Shader _entityShader;
    private ShaderMaterial _spriteMaterial;

    // Perk stat modifiers
    private float _damageMultiplier = 1f;
    private float _speedMultiplier = 1f;
    // Ce que l'oubli coûte là où se tient le joueur (plan 16 O4), à part des bonus pour rester réversible.
    private ErasureEffects.Effect _erasurePenalty = ErasureEffects.Effect.None;
    private float _attackSpeedMultiplier = 1f;
    private float _bonusMaxHp;
    private float _aoeMultiplier = 1f;
    private float _attackRangeMultiplier = 1f;
    private float _bonusRegenRate;
    private float _lifesteal;
    private float _lifestealPending;
    private float _lifestealTimer;
    private float _xpGainMultiplier = 1f;
    private float _armor;
    private PlayerDefense _defense;
    private bool _blinkHidden;
    private float _critChance;
    private float _critMultiplier = 2f;
    private float _projectilePierce;
    private float _xpMagnetMultiplier = 1f;
    private float _luckBonus;

    // Weapon special effect tracking (per-weapon hit counters)
    private readonly System.Collections.Generic.Dictionary<string, int> _weaponHitCounters = new();

    // Orbital weapon system
    private readonly System.Collections.Generic.List<Node2D> _orbitalProjectiles = new();
    private float _orbitalSize = 1f;
    private WeaponInstance _orbitalWeapon;
    private float _orbitalAngle;

    // Sustained cone attack
    private float _coneAttackTimer;
    private float _coneDuration;
    private float _coneDamageRampPerSec;
    private float _coneAngleStart;
    private float _coneAngleEnd;
    private float _coneRange;
    private float _coneBaseDamage;
    private WeaponInstance _coneWeapon;
    private bool _isConeActive;

    // Jauge de fouille des POI (les lieux du monde ont la leur, dans WorldInteraction)
    private InteractionGauge _interactionGauge;

    // Footsteps
    private float _footstepTimer;
    private const float FootstepInterval = 0.55f;

    // POI interaction
    private PointOfInterest _poiTarget;
    private float _poiProgress;
    private bool _isExploringPoi;

    private WorldInteraction _interaction;

    public float CurrentHp => _currentHp;
    public float EffectiveMaxHp => MaxHp + _bonusMaxHp;
    public float EffectiveAttackRange => AttackRange * _attackRangeMultiplier;
    public float AttackRangeMultiplier => _attackRangeMultiplier;
    // V2: StructureHpMultiplier, CraftSpeedMultiplier, RepairSpeedMultiplier retires
    /// <summary>Perforation ajoutée aux tirs (Reflet brisé), en fraction : la partie décimale est une chance par tir.</summary>
    public float ProjectilePierce => _projectilePierce;
    public float XpMagnetMultiplier => _xpMagnetMultiplier;
    /// <summary>Bonus d'XP du build, appliqué une fois au gain (plan 21 §4, Photo de classe).</summary>
    public float XpGainMultiplier => _xpGainMultiplier;
    public float CritChance => _critChance;
    public float CritMultiplier => _critMultiplier;
    public bool IsDead => _isDead;
    public string CharacterId => _characterId;
    public WeaponInstance EquippedWeapon => _weaponSlots.Count > 0 ? _weaponSlots[0] : null;
    public IReadOnlyList<WeaponInstance> WeaponSlots => _weaponSlots;

    // Stat getters pour le menu pause
    public float DamageMultiplier => _damageMultiplier;
    public float SpeedMultiplier => _speedMultiplier;
    public float AttackSpeedMultiplier => _attackSpeedMultiplier;
    public float Armor => _armor;
    public float ArmorReduction => _defense?.ArmorReduction(_armor) ?? 0f;
    public float Shield => _defense?.Shield ?? 0f;
    public float MaxShield => _defense?.MaxShield ?? 0f;
    public float BonusRegenRate => _bonusRegenRate;
    public float Lifesteal => _lifesteal;
    public float AoeMultiplier => _aoeMultiplier;
    public float LuckBonus => _luckBonus;

    private const float ShadowWidth = 22f;
    // Blessure : éclair blanc chaud ; coup encaissé par le bouclier : éclair bleu pâle, sans secousse tant qu'il tient.
    private static readonly Color HurtFlashColor = new(1f, 0.92f, 0.88f);
    private static readonly Color ShieldFlashColor = new(0.7f, 0.85f, 1f);

    public override void _Ready()
    {
        _currentHp = MaxHp;
        _visual = GetNode<Polygon2D>("Visual");
        _sprite = GetNode<AnimatedSprite2D>("Sprite");
        _attackFx = new PlayerAttackFx(this, _sprite);
        _heldWeapon = new HeldWeapon(_sprite);
        AddChild(GroundShadow.Create(ShadowWidth));
        _originalColor = _visual.Color;

        AddToGroup("player");

        _entityShader ??= GD.Load<Shader>("res://assets/shaders/entity.gdshader");

        _interactionGauge = new InteractionGauge { Name = "InteractionGauge", Position = new Vector2(0f, -44f) };
        AddChild(_interactionGauge);
        _interaction = new WorldInteraction { Name = "WorldInteraction" };
        AddChild(_interaction);
        _interaction.Setup(this);
        Waymarks = new Waymarks { Name = "Waymarks" };
        AddChild(Waymarks);
        Mobility = new PlayerMobility(MobilityConfig.Load());
        _defense = new PlayerDefense(DefenseConfig.Load());
        _mobilityFeedback = new MobilityFeedback { Name = "MobilityFeedback" };
        AddChild(_mobilityFeedback);
        CacheWorldSetup();

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.GameStateChanged += OnMovementGameStateChanged;
        _eventBus.PlayerErasurePhaseChanged += OnErasurePhaseChanged;
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _groupCache = GetNode<GroupCache>("/root/GroupCache");
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.PlayerErasurePhaseChanged -= OnErasurePhaseChanged;
            _eventBus.GameStateChanged -= OnMovementGameStateChanged;
        }
    }

    public void InitializeCharacter(CharacterData data)
    {
        // Le bootstrap crée les services du monde après _Ready, avant cette initialisation.
        // Refaire la liaison ici couvre aussi une nouvelle scène lorsque l'état est déjà Run.
        CacheWorldSetup();
        WeaponDataLoader.Load();

        _characterId = data.Id;

        Speed = data.BaseStats.Speed;
        AttackDamage = data.BaseStats.AttackDamage;
        AttackSpeed = data.BaseStats.AttackSpeed;
        AttackRange = data.BaseStats.AttackRange;
        MaxHp = data.BaseStats.MaxHp;
        BaseRegenRate = data.BaseStats.RegenRate;
        InteractRange = data.BaseStats.InteractRange;

        _currentHp = MaxHp;
        // Le HUD est relié au joueur avant cette initialisation : sans cet envoi, il garde les PV par défaut.
        _eventBus?.EmitSignal(EventBus.SignalName.PlayerDamaged, _currentHp, EffectiveMaxHp);
        _defense.SetBaseShield(data.BaseStats.Shield);
        EmitShield();
        EquipStartingWeapon(data.StartingWeaponId);
        UpdateAttackSpeed();

        _visual.Color = data.VisualColor;
        _originalColor = data.VisualColor;

        // Sprite animé : remplace le Polygon2D si sprite_folder est défini
        if (!string.IsNullOrEmpty(data.SpriteFolder))
        {
            SpriteFrames frames = CharacterSpriteLoader.LoadOrGet(data.Id, data.SpriteFolder);
            if (frames != null)
            {
                _sprite.SpriteFrames = frames;
                // Pieds légèrement sous le centre de collision, comme les anciens sprites centrés.
                _sprite.Offset = new Vector2(0f, SpriteFeetBelowOrigin - data.SpriteFeetOffset);
                _sprite.Visible = true;
                _sprite.SelfModulate = Colors.White;
                _visual.Visible = false;
                _hasSprite = true;
                _facing.Reset(CharacterSpriteLoader.HasEightDirections(frames));
                _currentAnimName = default;
                _hurtAnimTimer = 0f;

                _spriteMaterial = new ShaderMaterial { Shader = _entityShader };
                _spriteMaterial.SetShaderParameter("outline_enabled", true);
                // Sel-out : version sombre de la couleur du personnage
                Color oc = data.VisualColor;
                _spriteMaterial.SetShaderParameter("outline_color",
                    new Color(oc.R * 0.3f + 0.05f, oc.G * 0.3f + 0.05f, oc.B * 0.3f + 0.05f, 0.9f));
                _sprite.Material = _spriteMaterial;

                PlaySpriteAnim("SE_idle");
            }
        }

        GD.Print($"[Player] Initialized as {data.Name} (HP:{MaxHp}, ATK:{AttackDamage}, SPD:{Speed})");
    }

    private void EquipStartingWeapon(string requestedWeaponId)
    {
        WeaponData weapon = null;
        if (!string.IsNullOrEmpty(requestedWeaponId))
            weapon = WeaponDataLoader.Get(requestedWeaponId);

        weapon ??= WeaponDataLoader.GetDefaultForCharacter(_characterId);
        weapon ??= WeaponDataLoader.Get("makeshift_bow");

        if (weapon == null)
        {
            GD.PushError($"[Player] No weapon found for {_characterId}");
            return;
        }

        AddWeapon(weapon);
    }

    public bool AddWeapon(WeaponData weapon)
    {
        if (weapon == null)
            return false;

        return AddWeapon(new WeaponInstance(weapon));
    }

    public bool AddWeapon(WeaponInstance instance)
    {
        if (instance == null || _weaponSlots.Count >= MaxWeaponSlots)
            return false;

        foreach (WeaponInstance existing in _weaponSlots)
        {
            if (existing.Id == instance.Id)
                return false;
        }

        _weaponSlots.Add(instance);
        _equippedWeapon = instance;

        int capturedIndex = _weaponSlots.Count - 1;
        Timer timer = new();
        float weaponAtkSpd = instance.GetStat("attack_speed", 1f);
        timer.WaitTime = 1.0f / Mathf.Max(0.05f, AttackSpeed * weaponAtkSpd * _attackSpeedMultiplier);
        timer.Autostart = true;
        timer.Timeout += () => OnWeaponAttackTimeout(capturedIndex);
        AddChild(timer);
        _weaponTimers.Add(timer);

        // Une orbitale n'attend pas le premier tic de son minuteur (20 s pour la Boîte à musique) : ses orbes
        // apparaissent dès qu'elle est portée.
        if (instance.AttackPattern?.ToLower() == "orbital")
            SetupOrbitalWeapon(instance);

        _eventBus?.EmitSignal(EventBus.SignalName.WeaponEquipped, instance.Id, capturedIndex);
        _eventBus?.EmitSignal(EventBus.SignalName.WeaponInventoryChanged);

        GD.Print($"[Player] Weapon added [{capturedIndex}]: {instance.Name}");
        return true;
    }

    /// <summary>
    /// Retire une arme du slot et retourne la WeaponData de base pour spawn un pickup.
    /// </summary>
    public WeaponInstance RemoveWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _weaponSlots.Count)
            return null;

        WeaponInstance removed = _weaponSlots[slotIndex];
        _weaponSlots.RemoveAt(slotIndex);
        if (_isConeActive && removed.SpecialEffect?.Type == "sustained_cone")
            DeactivateSustainedCone();
        if (removed == _orbitalWeapon)
            ClearOrbitals();

        if (slotIndex < _weaponTimers.Count)
        {
            Timer timer = _weaponTimers[slotIndex];
            timer.Stop();
            timer.QueueFree();
            _weaponTimers.RemoveAt(slotIndex);
        }

        RebindWeaponTimers();

        _eventBus?.EmitSignal(EventBus.SignalName.WeaponDropped, removed.Id);
        _eventBus?.EmitSignal(EventBus.SignalName.WeaponInventoryChanged);

        UpdateAttackSpeed();
        if (_weaponSlots.Count > 0)
            _equippedWeapon = _weaponSlots[0];
        else
            _equippedWeapon = null;

        GD.Print($"[Player] Weapon removed [{slotIndex}]: {removed.Name}");
        return removed;
    }

    /// <summary>Réattache les callbacks des timers après suppression d'un slot.</summary>
    private void RebindWeaponTimers()
    {
        for (int i = 0; i < _weaponTimers.Count; i++)
        {
            Timer timer = _weaponTimers[i];
            foreach (Godot.Collections.Dictionary connection in timer.GetSignalConnectionList("timeout"))
            {
                timer.Disconnect("timeout", (Callable)connection["callable"]);
            }
            int capturedIndex = i;
            timer.Timeout += () => OnWeaponAttackTimeout(capturedIndex);
        }
    }

    public WeaponInstance GetWeaponInstance(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _weaponSlots.Count)
            return null;
        return _weaponSlots[slotIndex];
    }

    /// <summary>Recalcule les timers d'attaque (appelé après upgrade d'attack_speed).</summary>
    public void RefreshAttackSpeed()
    {
        UpdateAttackSpeed();
    }

    // --- Weapon Fragment Levels (level-up re-selection) ---

    /// <summary>Applique une amélioration tirée au level-up, au Mémorial ou à la Faille : ses gains, et un niveau de plus.</summary>
    public bool UpgradeWeapon(string weaponId, IReadOnlyList<StatGain> gains)
    {
        // Le niveau n'existe qu'à un endroit, l'instance d'arme : level-up, Mémorial et badge le lisent tous.
        WeaponInstance weapon = FindWeaponSlot(weaponId, out int slot);
        if (weapon == null || !weapon.ApplyUpgrade(gains))
            return false;

        RefreshAttackSpeed();
        if (weapon == _orbitalWeapon)
            SetupOrbitalWeapon(weapon);
        _eventBus?.EmitSignal(EventBus.SignalName.WeaponUpgraded, weaponId, slot, "all", weapon.Level);
        _eventBus?.EmitSignal(EventBus.SignalName.WeaponInventoryChanged);
        GD.Print($"[Player] Weapon level: {weaponId} → {weapon.Level}/{weapon.MaxLevel}");
        return true;
    }

    /// <summary>Dégâts portés par une arme depuis le début de la run.</summary>
    public float GetDamageDealt(string weaponId) => _weaponLedger.DamageOf(weaponId);
    public int GetKills(string weaponId) => _weaponLedger.KillsOf(weaponId);

    public int GetWeaponFragmentLevel(string weaponId) => FindWeaponSlot(weaponId, out _)?.Level ?? 0;

    public bool IsWeaponFragmentMaxed(string weaponId)
    {
        WeaponInstance weapon = FindWeaponSlot(weaponId, out _);
        return weapon != null && !weapon.CanLevelUp;
    }

    private WeaponInstance FindWeaponSlot(string weaponId, out int slot)
    {
        for (slot = 0; slot < _weaponSlots.Count; slot++)
        {
            if (_weaponSlots[slot].Id == weaponId)
                return _weaponSlots[slot];
        }
        slot = -1;
        return null;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDead)
            return;

        float dt = (float)delta;
        bool inputAllowed = _gameManager.CurrentState == GameManager.GameState.Run;
        Vector2 inputDir = inputAllowed
            ? (IsAIControlled ? AIInputOverride.LimitLength() : Input.GetVector("move_left", "move_right", "move_up", "move_down"))
            : Vector2.Zero;

        if (_mobilityRequiresRelease && !Input.IsActionPressed("mobility"))
            _mobilityRequiresRelease = false;
        float terrainFactor = (IsOnWater() ? Mobility.Config.WaterSpeedFactor : 1f) * _slowFactor;
        Velocity = Mobility.Step(dt, inputDir, Speed * _speedMultiplier * _erasurePenalty.Speed, terrainFactor, inputAllowed);
        if (inputDir != Vector2.Zero || Mobility.IsDashStep)
        {
            CancelPoiExplore();
            _interaction.Cancel();
            if (inputDir != Vector2.Zero)
                _facingDirection = inputDir.Normalized();
        }

        Vector2 previousPosition = GlobalPosition;
        bool dashMovement = Mobility.IsDashStep;
        if (dashMovement)
        {
            Vector2 requested = Velocity * dt;
            Vector2 permitted = ClampMobilityTravel(previousPosition, requested);
            if (!permitted.IsEqualApprox(requested))
                Mobility.StopDash();
            Velocity = permitted / dt;
        }
        MoveAndSlide();
        Vector2 movementVelocity = GetRealVelocity();
        if (dashMovement)
        {
            Vector2 traveled = GlobalPosition - previousPosition;
            // MoveAndSlide peut dévier le trajet : le point final reste soumis au Néant.
            if (!ClampMobilityTravel(previousPosition, traveled).IsEqualApprox(traveled))
            {
                GlobalPosition = previousPosition;
                Velocity = Vector2.Zero;
                movementVelocity = Vector2.Zero;
                Mobility.StopDash();
            }
            if (GetSlideCollisionCount() > 0)
                Mobility.StopDash();
        }
        else if (!IsOnWorldGround(GlobalPosition))
        {
            movementVelocity = KeepOnWorldGround(previousPosition, dt);
        }
        Mobility.FinishMovement(movementVelocity);
        _mobilityFeedback.UpdateFeedback(dt, Mobility, GlobalPosition, movementVelocity.LengthSquared() > 0.01f);
        float movementSpeed = movementVelocity.Length();
        float movementRate = movementSpeed > MovementSpeedEpsilon && Speed > 0f ? movementSpeed / Speed : 0f;
        UpdateSpriteAnimation(dt, movementVelocity, movementRate);
        if (_hasSprite)
            _heldWeapon.Update(dt, EquippedWeapon?.Base, _facing.Current);
        ProcessFootsteps(dt, dashMovement ? 0f : movementRate);
        ApplyRegen(dt);
        StepLifesteal(dt);
        StepDefense(dt);
        ProcessSlowDecay(dt);
        ProcessPoiExplore(dt);
        _interaction.Step(dt, !_isExploringPoi && !Mobility.IsDashing);
        ProcessOrbitalWeapons(dt);
        ProcessSustainedCone(dt);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_isDead || _gameManager.CurrentState != GameManager.GameState.Run || GetTree().Paused)
            return;

        if (@event.IsActionPressed("mobility") && !@event.IsEcho())
        {
            if (!_mobilityRequiresRelease && !IsAIControlled)
                Mobility.Request();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event.IsActionReleased("mobility"))
            _mobilityRequiresRelease = false;

        if (@event.IsActionPressed("journal"))
        {
            ToggleJournal();
            return;
        }

        if (@event.IsActionPressed("interact"))
        {
            if (Mobility.IsDashing)
                return;
            if (_isExploringPoi || _interaction.IsActive)
            {
                CancelPoiExplore();
                _interaction.Cancel();
            }
            else if (TryStartPoiExplore())
            {
                // POI interaction takes priority
            }
            else if (_interaction.TryStart())
            {
                // Chest interaction
            }
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPaused || what == NotificationUnpaused || what == NotificationApplicationFocusOut)
            SuspendMovement();
    }

    private void OnMovementGameStateChanged(string oldState, string newState)
    {
        if (newState == nameof(GameManager.GameState.Run))
            CacheWorldSetup();
        SuspendMovement();
    }

    private void SuspendMovement()
    {
        Mobility?.Suspend();
        _mobilityFeedback?.Suspend();
        _mobilityRequiresRelease = true;
        Velocity = Vector2.Zero;
        CancelPoiExplore();
        _interaction.Cancel();
    }

    /// <summary>Le bord du monde (cellules hors carte ou dissoutes) ne se traverse pas, même en marchant.</summary>
    private bool IsOnWorldGround(Vector2 position)
    {
        if (_worldSetup?.Generator == null || _groundLayer == null)
            return true;
        Vector2I cell = _groundLayer.LocalToMap(_groundLayer.ToLocal(position));
        return _worldSetup.Generator.IsWithinBounds(cell.X, cell.Y) && !_worldSetup.Generator.IsErased(cell.X, cell.Y);
    }

    /// <summary>Ramène le joueur sur le sol en gardant l'axe encore valide : il glisse le long du bord au lieu de s'y coller.</summary>
    private Vector2 KeepOnWorldGround(Vector2 previousPosition, float delta)
    {
        Vector2 attempted = GlobalPosition;
        Vector2 alongX = new(attempted.X, previousPosition.Y);
        Vector2 alongY = new(previousPosition.X, attempted.Y);
        if (IsOnWorldGround(alongX))
            GlobalPosition = alongX;
        else if (IsOnWorldGround(alongY))
            GlobalPosition = alongY;
        else
            GlobalPosition = previousPosition;
        Vector2 kept = GlobalPosition - previousPosition;
        Velocity = Vector2.Zero;
        return delta > 0f ? kept / delta : Vector2.Zero;
    }

    private Vector2 ClampMobilityTravel(Vector2 origin, Vector2 displacement)
    {
        // Le dash n'accorde aucun franchissement du vide, même sans collision physique.
        // Échantillonnage borné à 512 px/tick pour conserver un coût maximal prévisible.
        Vector2 bounded = displacement.LimitLength(512f);
        int steps = Mathf.Max(1, Mathf.CeilToInt(bounded.Length() / 2f));
        Vector2 lastValid = Vector2.Zero;
        for (int i = 0; i <= steps; i++)
        {
            Vector2 offset = bounded * ((float)i / steps);
            Vector2 position = origin + offset;
            if (_erasureManager?.GetZonePhaseAt(position) == ErasureManager.ErasureZonePhase.Void)
                break;
            if (_worldSetup?.Generator != null && _groundLayer != null)
            {
                Vector2I cell = _groundLayer.LocalToMap(_groundLayer.ToLocal(position));
                if (!_worldSetup.Generator.IsWithinBounds(cell.X, cell.Y) || _worldSetup.Generator.IsErased(cell.X, cell.Y))
                    break;
            }
            lastValid = offset;
        }
        return lastValid;
    }

    // --- AI Interaction ---

    /// <summary>Programmatic interact trigger for AI simulation. Same logic as the interact input handler.</summary>
    public void AITriggerInteract()
    {
        if (_isDead || !IsAIControlled || Mobility.IsDashing || _gameManager.CurrentState != GameManager.GameState.Run || GetTree().Paused) return;
        if (_isExploringPoi || _interaction.IsActive)
        {
            CancelPoiExplore();
            _interaction.Cancel();
        }
        else if (TryStartPoiExplore()) { }
        else if (_interaction.TryStart()) { }
    }

    // --- Journal ---

    private void ToggleJournal()
    {
        UI.JournalScreen journal = GetNodeOrNull<UI.JournalScreen>("/root/Main/JournalScreen");
        journal?.Toggle();
    }

    // --- World Queries ---

    private WorldSetup _worldSetup;
    private TileMapLayer _groundLayer;

    private void CacheWorldSetup()
    {
        _worldSetup = GetNodeOrNull<WorldSetup>("/root/Main");
        _erasureManager = GetNodeOrNull<ErasureManager>("/root/Main/ErasureManager");
        if (_worldSetup != null)
            _groundLayer = _worldSetup.GetNodeOrNull<TileMapLayer>("Ground");
    }

    private TerrainType GetCurrentTerrain()
    {
        if (_worldSetup?.Generator == null || _groundLayer == null)
            return TerrainType.Grass;
        Vector2I cell = _groundLayer.LocalToMap(_groundLayer.ToLocal(GlobalPosition));
        return _worldSetup.Generator.GetTerrain(cell.X, cell.Y);
    }

    private bool IsOnWater() => GetCurrentTerrain() == TerrainType.Water;

    // --- Stat Modifiers ---

    public void ApplyPerkModifier(string stat, float value, string modifierType)
    {
        switch (stat)
        {
            case "damage":
                if (modifierType == "multiplicative") _damageMultiplier *= value;
                break;
            case "speed":
                if (modifierType == "multiplicative") _speedMultiplier *= value;
                break;
            case "max_hp":
                if (modifierType == "additive")
                {
                    _bonusMaxHp += value;
                    _currentHp += value;
                }
                else if (modifierType == "multiplicative")
                {
                    float oldMax = EffectiveMaxHp;
                    MaxHp *= value;
                    float newMax = EffectiveMaxHp;
                    _currentHp = Mathf.Max(1f, _currentHp + (newMax - oldMax));
                }
                break;
            case "attack_speed":
                if (modifierType == "multiplicative")
                {
                    _attackSpeedMultiplier *= value;
                    UpdateAttackSpeed();
                }
                break;
            case "aoe_radius":
                if (modifierType == "multiplicative") _aoeMultiplier *= value;
                break;
            case "attack_range":
                if (modifierType == "multiplicative") _attackRangeMultiplier *= value;
                break;
            case "regen_rate":
                if (modifierType == "additive") _bonusRegenRate += value;
                break;
            case "lifesteal":
                if (modifierType == "additive") _lifesteal += value;
                break;
            case "armor":
                if (modifierType == "additive") _armor += value;
                break;
            case "shield":
                if (modifierType == "additive")
                {
                    _defense.AddShield(value);
                    EmitShield();
                }
                break;
            case "crit_chance":
                if (modifierType == "additive") _critChance += value;
                break;
            case "crit_multiplier":
                if (modifierType == "additive") _critMultiplier += value;
                break;
            case "projectile_pierce":
                if (modifierType == "additive") _projectilePierce += value;
                break;
            case "xp_magnet_radius":
                if (modifierType == "multiplicative") _xpMagnetMultiplier *= value;
                break;
            // V2: repair_speed retire
            case "luck":
                if (modifierType == "additive") _luckBonus += value;
                break;
            case "xp_gain":
                if (modifierType == "multiplicative") _xpGainMultiplier *= value;
                break;
            case "dash_recharge":
                if (modifierType == "multiplicative") Mobility.RechargeMultiplier *= value;
                break;
            case "projectile_bonus":
                if (modifierType == "additive") _bonusProjectiles += value;
                break;
            case "status_duration":
                if (modifierType == "multiplicative") _statusDurationMultiplier *= value;
                break;
        }
    }

    /// <summary>Appelé par les projectiles à l'impact : effets du coup.</summary>
    public void OnProjectileHit(Enemy enemy, float damage, bool isCrit, WeaponInstance source, AttackContext context = default)
    {
        OnAttackHit(enemy, damage, isCrit, source, context: context);
    }

    /// <summary>
    /// Effets d'un coup porté par <paramref name="source"/> : ceux de l'arme (effet au contact, recul, effet spécial)
    /// restent ceux de l'arme qui a frappé.
    /// </summary>
    private void OnAttackHit(Enemy enemy, float damage, bool isCrit, WeaponInstance source, int triggerCount = 1, bool showImpact = true, AttackContext context = default)
    {
        if (_isDead)
            return;

        if (!IsInstanceValid(enemy) || enemy.IsQueuedForDeletion())
            return;

        if (showImpact)
            _attackFx.PlayHit(source?.Base, enemy.GlobalPosition, isCrit);
        if (source != null)
            _weaponLedger.AddDamage(source.Id, damage);
        _lifestealPending += damage * _lifesteal;
        if (context.OwnerId == 0)
            context = BeginAttack(source, damage);

        if (source != null && enemy.ClaimKillCredit())
            _weaponLedger.AddKill(source.Id);

        // --- Weapon on-hit effects ---
        WeaponOnHitEffect ohe = source?.OnHitEffect;
        if (ohe != null)
        {
            switch (ohe.Type)
            {
                case "dot":
                    enemy.ApplyBleed(ohe.Damage, StatusDuration(ohe.Duration), context);
                    break;
                case "slow":
                    enemy.ApplySlow(ohe.Value, StatusDuration(ohe.Duration), context);
                    break;
                case "disorient":
                    enemy.ApplyDisorient(StatusDuration(ohe.Duration), context);
                    break;
                case "freeze":
                    enemy.Freeze(StatusDuration(ohe.Duration));
                    break;
            }
        }

        // Objets à l'impact : après l'effet de l'arme (un ralentissement de la Cloche compte pour le Glaçon), seulement
        // sur un coup direct, et au rythme des impacts visibles pour un cône continu.
        if (_objectTriggers != null && showImpact && context.Kind == DamageKind.DirectWeapon)
            _objectTriggers.OnWeaponImpact(enemy, ComputeBaseAttackDamage(source), source, triggerCount, context, isCrit, damage);

        // --- Weapon knockback ---
        float knockback = source?.GetStat("knockback", 0f) ?? 0f;
        if (knockback > 0f)
        {
            Vector2 knockDir = (enemy.GlobalPosition - GlobalPosition).Normalized();
            enemy.ApplyKnockback(knockDir, knockback);
        }

        // --- Weapon special effects ---
        WeaponSpecialEffect se = source?.SpecialEffect;
        if (se != null)
            ProcessWeaponSpecialOnHit(se, enemy, damage, source, context);
    }

    private float GetCombinedProcChance(float perHitChance, int triggerCount)
    {
        float clampedChance = Mathf.Clamp(perHitChance, 0f, 1f);
        if (clampedChance <= 0f || triggerCount <= 0)
            return 0f;
        if (clampedChance >= 1f)
            return 1f;

        return 1f - Mathf.Pow(1f - clampedChance, triggerCount);
    }

    // --- Weapon Special Effects ---

    private void ProcessWeaponSpecialOnHit(WeaponSpecialEffect se, Enemy enemy, float damage, WeaponInstance source, AttackContext context)
    {
        switch (se.Type)
        {
            case "heal_every_n_hits":
            {
                string weaponId = source.Id;
                _weaponHitCounters.TryGetValue(weaponId, out int count);
                count++;
                int n = se.Params.TryGetValue("n", out float nVal) ? Mathf.Max(1, (int)nVal) : 5;
                if (count >= n)
                {
                    float healAmount = se.Params.TryGetValue("heal_amount", out float h) ? h : 4f;
                    Heal(healAmount);
                    count = 0;
                }
                _weaponHitCounters[weaponId] = count;
                break;
            }
            case "instant_disintegrate":
            {
                if (enemy.IsDying)
                {
                    // Pas de particules, l'ennemi disparaît instantanément
                    enemy.Scale = Vector2.Zero;
                    enemy.Modulate = new Color(1f, 1f, 1f, 0f);
                }
                break;
            }
            case "delayed_echo":
            {
                float delay = se.Params.TryGetValue("echo_delay", out float d) ? d : 0.3f;
                float echoPct = se.Params.TryGetValue("echo_damage_percent", out float p) ? p : 0.6f;
                float echoDamage = damage * echoPct;
                Vector2 echoPos = enemy.GlobalPosition;
                float echoRadius = ZoneScale(se.Params.TryGetValue("echo_radius", out float er) ? er : 40f);
                // Enchaînement (ascension des Gants de boxe) : l'écho repart plusieurs fois, à intervalles égaux.
                int echoCount = se.Params.TryGetValue("echo_count", out float ec) ? Mathf.Max(1, (int)ec) : 1;
                for (int echo = 1; echo <= echoCount; echo++)
                {
                    GetTree().CreateTimer(delay * echo).Timeout += () =>
                    {
                        // Réapplique les dégâts à la position d'origine (AoE fantôme)
                        Godot.Collections.Array<Node> enemies = _groupCache.GetEnemies();
                        foreach (Node node in enemies)
                        {
                            if (node is Enemy e && IsInstanceValid(e) && !e.IsDying)
                            {
                                if (e.GlobalPosition.DistanceTo(echoPos) < echoRadius)
                                    e.TakeDamage(echoDamage, source: context.As(DamageKind.SecondaryWeapon));
                            }
                        }
                        SpawnEchoVisual(echoPos);
                        if (_objectMilestones?.HasZoneEcho == true)
                            _objectMilestones.QueueCircleEcho(echoPos, echoRadius, echoDamage, source, context);
                    };
                }
                break;
            }
            case "ground_fire":
            {
                // Géré au moment de l'impact du projectile, pas ici
                break;
            }
            case "local_time_slow":
            {
                float radius = ZoneScale(se.Params.TryGetValue("slow_radius", out float r) ? r : 80f);
                float factor = se.Params.TryGetValue("slow_factor", out float f) ? f : 0.3f;
                float duration = StatusDuration(se.Params.TryGetValue("slow_duration", out float dur) ? dur : 0.5f);
                // Arrêt sur image (ascension du Chronomètre) : le champ fige au lieu de ralentir.
                float freeze = se.Params.TryGetValue("freeze_seconds", out float fz) ? StatusDuration(fz) : 0f;
                Vector2 impactPos = enemy.GlobalPosition;
                // La cible frappée d'abord : elle est au centre du champ, même absente du cache des ennemis de la frame.
                ApplyTimeField(enemy, freeze, factor, duration, context);
                Godot.Collections.Array<Node> enemies = _groupCache.GetEnemies();
                foreach (Node node in enemies)
                {
                    if (node is Enemy e && e != enemy && IsInstanceValid(e) && !e.IsDying && e.GlobalPosition.DistanceTo(impactPos) < radius)
                        ApplyTimeField(e, freeze, factor, duration, context);
                }
                SpawnTimeSlowVisual(impactPos, radius, duration);
                break;
            }
            case "random_shape":
            {
                float aoeRadius = ZoneScale(se.Params.TryGetValue("shape_aoe_on_impact", out float aoe) ? aoe : 50f);
                Vector2 impactPos = enemy.GlobalPosition;
                Godot.Collections.Array<Node> enemies = _groupCache.GetEnemies();
                foreach (Node node in enemies)
                {
                    if (node is Enemy e && IsInstanceValid(e) && !e.IsDying && e != enemy)
                    {
                        if (e.GlobalPosition.DistanceTo(impactPos) < aoeRadius)
                            e.TakeDamage(damage * 0.5f, source: context.As(DamageKind.SecondaryWeapon));
                    }
                }
                if (_objectMilestones?.HasZoneEcho == true)
                    _objectMilestones.QueueCircleEcho(impactPos, aoeRadius, damage * 0.5f, source, context);
                break;
            }
        }
    }

    /// <summary>Champ du Chronomètre sur une créature : fige (Arrêt sur image) ou ralentit.</summary>
    private static void ApplyTimeField(Enemy enemy, float freeze, float factor, float duration, AttackContext context)
    {
        if (freeze > 0f)
            enemy.Freeze(freeze);
        else
            enemy.ApplySlow(factor, duration, context);
    }

    private void SpawnEchoVisual(Vector2 position)
    {
        _attackFx.PlayEcho(position);
    }

    private void SpawnTimeSlowVisual(Vector2 position, float radius, float duration)
    {
        _attackFx.PlayTimeField(position, radius, duration);
    }

    // --- Orbital Weapons ---

    /// <summary>
    /// Crée les orbes d'une arme orbitale dès qu'elle est portée, et les recrée quand leur nombre change
    /// (montée de niveau). Les dégâts se calculent à l'impact, avec le niveau courant de l'arme.
    /// </summary>
    private void SetupOrbitalWeapon(WeaponInstance weapon)
    {
        // Une orbe ne peut pas apparaître une attaque sur deux : seule la partie entière compte (plan 23 R4).
        int orbitalCount = Mathf.Max(1, Mathf.FloorToInt(weapon.GetStat("orbital_count", 3f)));
        // Recréées aussi quand la taille change : contact et note suivent la stat de taille.
        if (_orbitalWeapon == weapon && _orbitalProjectiles.Count == orbitalCount && Mathf.IsEqualApprox(_orbitalSize, _aoeMultiplier))
            return;

        ClearOrbitals();
        _orbitalWeapon = weapon;
        _orbitalSize = _aoeMultiplier;
        for (int i = 0; i < orbitalCount; i++)
        {
            Area2D orb = new() { Name = $"OrbitalOrb_{i}" };
            orb.CollisionLayer = 0;
            orb.CollisionMask = 2;

            CollisionShape2D shape = new();
            CircleShape2D circle = new() { Radius = ZoneScale(8f) };
            shape.Shape = circle;
            orb.AddChild(shape);
            orb.AddChild(PlayerAttackFx.CreateOrbitalVisual(WeaponSizeScale));

            orb.BodyEntered += (Node2D body) =>
            {
                if (body is Enemy enemy && !enemy.IsDying && IsInstanceValid(enemy) && _orbitalWeapon != null)
                {
                    float damage = ResolveHitDamage(enemy, ComputeBaseAttackDamage(_orbitalWeapon), false);
                    AttackContext context = BeginAttack(_orbitalWeapon, damage);
                    enemy.TakeDamage(damage, source: context);
                    OnAttackHit(enemy, damage, false, _orbitalWeapon, context: context);
                }
            };

            AddChild(orb);
            _orbitalProjectiles.Add(orb);
        }
    }

    private void ClearOrbitals()
    {
        foreach (Node2D old in _orbitalProjectiles)
        {
            if (IsInstanceValid(old))
                old.QueueFree();
        }
        _orbitalProjectiles.Clear();
        _orbitalWeapon = null;
    }

    private void ProcessOrbitalWeapons(float delta)
    {
        if (_orbitalProjectiles.Count == 0 || _orbitalWeapon == null)
            return;

        float orbitalSpeed = _orbitalWeapon.GetStat("orbital_speed", 180f);
        // Le rayon d'orbite est une portée : il suit les bonus de portée, comme l'allonge des coups.
        float orbitalRadius = GetEffectiveWeaponRange(_orbitalWeapon) * OrbitPulse(_orbitalWeapon, delta);
        _orbitalAngle += Mathf.DegToRad(orbitalSpeed) * delta;
        if (_orbitalAngle > Mathf.Tau)
            _orbitalAngle -= Mathf.Tau;

        for (int i = 0; i < _orbitalProjectiles.Count; i++)
        {
            Node2D orb = _orbitalProjectiles[i];
            if (!IsInstanceValid(orb))
                continue;

            float angle = _orbitalAngle + (Mathf.Tau * i / _orbitalProjectiles.Count);
            // Orbite couchée au sol : une ellipse deux fois plus large que haute à l'écran.
            orb.Position = Iso.ToScreen(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * orbitalRadius);
            PlayerAttackFx.AnimateOrbitalVisual(orb.GetChild<Sprite2D>(1), _orbitalAngle, i);
        }
    }

    // --- Sustained Cone Attack ---

    private void ActivateSustainedCone(WeaponSpecialEffect effect)
    {
        _isConeActive = true;
        _coneDuration = effect.Params.TryGetValue("duration", out float dur) ? dur : 2f;
        _coneAttackTimer = _coneDuration;
        _coneDamageRampPerSec = effect.Params.TryGetValue("damage_ramp_per_sec", out float ramp) ? ramp : 1.5f;
        _coneAngleStart = Mathf.Min(180f, ZoneScale(GetWeaponStat("cone_angle_start", 15f)));
        _coneAngleEnd = Mathf.Min(180f, ZoneScale(GetWeaponStat("cone_angle_end", 60f)));
        _coneRange = GetEffectiveWeaponRange();
        // Chaque onde en plus repasse à pleins dégâts, comme les frappes d'une onde circulaire en mêlée.
        _coneBaseDamage = ComputeBaseAttackDamage() * (RollOwnCount() + RollBonusProjectiles(_equippedWeapon));
        _coneWeapon = _equippedWeapon;
        _coneContext = BeginAttack(_coneWeapon, _coneBaseDamage * _coneDuration * (1f + _coneDamageRampPerSec * _coneDuration * 0.5f));

        UpdateConeVisual(0f);

        GD.Print("[Player] Sustained cone activé");
    }

    private void ProcessSustainedCone(float delta)
    {
        if (!_isConeActive)
            return;

        _coneAttackTimer -= delta;
        float elapsed = _coneDuration - _coneAttackTimer;

        if (_coneAttackTimer <= 0f)
        {
            DeactivateSustainedCone();
            return;
        }

        // Mise à jour du visuel (angle s'élargit avec le temps)
        UpdateConeVisual(elapsed);

        // Calcul de l'angle courant interpolé entre début et fin
        float progress = Mathf.Clamp(elapsed / _coneDuration, 0f, 1f);
        float currentAngleDeg = Mathf.Lerp(_coneAngleStart, _coneAngleEnd, progress);
        float halfAngleRad = Mathf.DegToRad(currentAngleDeg * 0.5f);
        float dotThreshold = Mathf.Cos(halfAngleRad);

        // Dégâts croissants au fil du temps
        float damage = _coneBaseDamage * (1f + elapsed * _coneDamageRampPerSec) * delta;

        // Application des dégâts aux ennemis dans le cône (uniquement visibles)
        Vector2 groundFacing = Iso.ToGround(_facingDirection).Normalized();
        Godot.Collections.Array<Node> enemies = _groupCache.GetEnemies();
        foreach (Node node in enemies)
        {
            if (node is not Enemy enemy || !IsInstanceValid(enemy) || !enemy.IsActive || enemy.IsDying)
                continue;

            // Cône posé au sol : portée et ouverture mesurées au sol, comme l'éventail dessiné.
            Vector2 toEnemy = Iso.ToGround(enemy.GlobalPosition - GlobalPosition);
            float dist = toEnemy.Length();
            if (dist > _coneRange || dist <= 0.001f)
                continue;

            Vector2 dirToEnemy = toEnemy / dist;
            if (groundFacing.Dot(dirToEnemy) < dotThreshold)
                continue;

            float hitDamage = ResolveHitDamage(enemy, damage, false);
            bool showImpact = enemy.TakeContinuousDamage(hitDamage, delta, _coneContext);
            OnAttackHit(enemy, hitDamage, false, _coneWeapon, showImpact: showImpact, context: _coneContext);
        }
    }

    private void UpdateConeVisual(float elapsed)
    {
        float progress = Mathf.Clamp(elapsed / _coneDuration, 0f, 1f);
        float currentAngleDeg = Mathf.Lerp(_coneAngleStart, _coneAngleEnd, progress);
        _attackFx.UpdateCone(_coneWeapon?.Base, _facingDirection, _coneRange, Mathf.DegToRad(currentAngleDeg * 0.5f));
    }

    private void DeactivateSustainedCone()
    {
        _isConeActive = false;
        _coneAttackTimer = 0f;
        _coneWeapon = null;

        _attackFx.StopCone();

        GD.Print("[Player] Sustained cone désactivé");
    }

    // --- Chain Attack ---

    private void PerformChainAttack()
    {
        float range = GetEffectiveWeaponRange();
        // Stat entière fractionnaire (plan 23 R4) : la décimale est une chance, à chaque attaque, d'un saut de plus.
        int chainTargets = Mathf.Max(1, FractionalCount.Roll(GetWeaponStat("chain_targets", 2f), GD.Randf()));
        float chainRange = GetWeaponStat("chain_range", 100f);
        float chainFalloff = GetWeaponStat("chain_damage_falloff", 0.8f);

        // Premier hit : ennemi le plus proche (melee)
        System.Collections.Generic.List<Enemy> enemies = FindEnemiesInArc(range, 360f);
        if (enemies.Count == 0)
            return;

        Enemy firstTarget = enemies[0];
        float baseDamage = ComputeBaseAttackDamage();
        Vector2 attackDir = (firstTarget.GlobalPosition - GlobalPosition).Normalized();
        PlayAttackFeedback(isMelee: true, attackDir);

        bool isCrit = _critChance > 0f && GD.Randf() < _critChance;
        float currentDamage = isCrit ? baseDamage * _critMultiplier : baseDamage;
        AttackContext context = BeginAttack(_equippedWeapon, currentDamage);
        float firstDamage = ResolveHitDamage(firstTarget, currentDamage, isCrit);
        firstTarget.TakeDamage(firstDamage, isCrit, source: context);
        OnAttackHit(firstTarget, firstDamage, isCrit, _equippedWeapon, context: context);

        // Chain vers les ennemis adjacents
        HashSet<ulong> hitIds = new() { firstTarget.GetInstanceId() };
        Enemy current = firstTarget;

        for (int chain = 0; chain < chainTargets; chain++)
        {
            currentDamage *= chainFalloff;
            Enemy nextTarget = FindNearestEnemyExcluding(current.GlobalPosition, chainRange, hitIds);
            if (nextTarget == null)
                break;

            hitIds.Add(nextTarget.GetInstanceId());
            SpawnChainVisual(current.GlobalPosition, nextTarget.GlobalPosition);
            float linkDamage = ResolveHitDamage(nextTarget, currentDamage, false);
            nextTarget.TakeDamage(linkDamage, source: context);
            OnAttackHit(nextTarget, linkDamage, false, _equippedWeapon, context: context);
            current = nextTarget;
        }
    }

    private Enemy FindNearestEnemyExcluding(Vector2 from, float maxRange, HashSet<ulong> excludeIds)
    {
        Godot.Collections.Array<Node> enemies = _groupCache.GetEnemies();
        Enemy nearest = null;
        float nearestDist = maxRange;

        foreach (Node node in enemies)
        {
            if (node is Enemy enemy && IsInstanceValid(enemy) && !enemy.IsDying)
            {
                if (excludeIds.Contains(enemy.GetInstanceId()))
                    continue;

                float dist = from.DistanceTo(enemy.GlobalPosition);
                if (dist < nearestDist)
                {
                    nearest = enemy;
                    nearestDist = dist;
                }
            }
        }
        return nearest;
    }

    private void SpawnChainVisual(Vector2 from, Vector2 to)
    {
        _attackFx.PlayChain(from, to, _equippedWeapon?.Base);
    }

    // --- Health ---

    public HealingResult Heal(float amount, HealingKind kind = HealingKind.Normal)
    {
        if (_isDead || amount <= 0f)
            return default;

        HealingResult result = HealingResult.Resolve(GetInstanceId(), kind, amount, _currentHp, EffectiveMaxHp);
        _currentHp += result.HpRestored;
        _eventBus.EmitSignal(EventBus.SignalName.PlayerDamaged, _currentHp, EffectiveMaxHp);
        _eventBus.PublishPlayerHealing(result);
        return result;
    }

    public PlayerDamageResult TakeDamage(float damage)
    {
        if (_currentHp <= 0 || IsGodMode || Mobility.IsInvulnerable || _defense.IsInvulnerable)
            return default;

        if (_objectMilestones != null && _objectMilestones.Ignores(damage, EffectiveMaxHp))
        {
            ShowIgnoredHit();
            return default;
        }

        float armor = _armor * (_objectMilestones?.ArmorMultiplier(Mobility) ?? 1f);
        PlayerDefense.Outcome outcome = _defense.Absorb(damage, armor);

        if (outcome.ShieldAbsorbed)
        {
            EmitShield();
            Flash(ShieldFlashColor, outcome.ShieldBroke);
            if (outcome.ShieldBroke)
                _objectMilestones?.OnShieldBroken();
            PlayerDamageResult shieldResult = new(GetInstanceId(), PlayerDamageKind.Combat, _currentHp, 0f, false, true, true);
            _eventBus.PublishPlayerDamage(shieldResult);
            return shieldResult;
        }

        return LoseHp(outcome.HpDamage, PlayerDamageKind.Combat);
    }

    /// <summary>Le Néant consume : ni bouclier, ni armure, ni invulnérabilité ne l'arrêtent.</summary>
    public PlayerDamageResult TakeErasureDamage(float damage)
    {
        if (_currentHp <= 0 || IsGodMode)
            return default;
        return LoseHp(damage, PlayerDamageKind.Erasure);
    }

    private PlayerDamageResult LoseHp(float damage, PlayerDamageKind kind)
    {
        float before = _currentHp;
        _currentHp -= damage;
        PlayerDamageResult result = new(GetInstanceId(), kind, before,
            Mathf.Clamp(damage, 0f, before), _currentHp <= 0f, false, true);
        Mobility.Hurt(Mobility.Config.HurtRecoverySeconds);
        Flash(HurtFlashColor, true);
        if (_hasSprite)
            _hurtAnimTimer = Mobility.Config.HurtRecoverySeconds;

        _eventBus.EmitSignal(EventBus.SignalName.PlayerDamaged, _currentHp, EffectiveMaxHp);

        if (result.Fatal)
        {
            _currentHp = 0;
            Die();
        }
        _eventBus.PublishPlayerDamage(result);
        return result;
    }

    private void StepDefense(float dt)
    {
        if (_defense.Step(dt))
            EmitShield();

        // Clignotement pendant l'invulnérabilité qui suit un coup : la fenêtre se lit sans interface.
        bool hidden = _defense.IsInvulnerable && Mathf.PosMod(_defense.InvulnerableTimer, 0.12f) < 0.06f;
        if (hidden == _blinkHidden || !_hasSprite)
            return;
        _blinkHidden = hidden;
        _sprite.Modulate = hidden ? new Color(1f, 1f, 1f, 0.4f) : Colors.White;
    }

    /// <summary>Bancs de régression : les coups reçus se mesurent sur les PV, sans bouclier ni invulnérabilité.</summary>
    internal void DisableDefenseForTests()
    {
        _defense.Disable();
        EmitShield();
    }

    private void EmitShield() =>
        _eventBus?.EmitSignal(EventBus.SignalName.PlayerShieldChanged, _defense.Shield, _defense.MaxShield);

    // --- Debuffs ---

    private float _slowTimer;
    private float _slowFactor = 1f;

    public void ApplySlow(float factor, float duration)
    {
        _slowFactor = factor;
        _slowTimer = duration;
    }

    private void ProcessSlowDecay(float delta)
    {
        if (_slowTimer <= 0f)
            return;

        _slowTimer -= delta;
        if (_slowTimer <= 0f)
        {
            _slowFactor = 1f;
            _slowTimer = 0f;
        }
    }

    private void OnErasurePhaseChanged(int phase)
    {
        _erasurePenalty = ErasureEffects.For((ErasureManager.ErasureZonePhase)phase);
    }

    /// <summary>Multiplie le speed multiplier courant (pour buffs événementiels temporaires).</summary>
    public void ApplySpeedMultiplier(float factor)
    {
        _speedMultiplier *= factor;
    }

    private void ProcessFootsteps(float delta, float movementRate)
    {
        if (movementRate <= 0f)
        {
            _footstepTimer = 0f;
            return;
        }

        _footstepTimer -= delta * movementRate;
        if (_footstepTimer > 0f)
            return;

        _footstepTimer = FootstepInterval;

        TerrainType terrain = GetCurrentTerrain();
        Combat.FootstepFx.Emit(GlobalPosition, terrain, Velocity.Normalized());
        string biomeFootstep = terrain != TerrainType.Water
            ? _worldSetup?.GetBiomeAt(GlobalPosition)?.FootstepAudio
            : null;
        string key = biomeFootstep ?? (terrain switch
        {
            TerrainType.Water    => "sfx_pas_eau",
            TerrainType.Concrete => "sfx_pas_beton",
            TerrainType.Forest   => "sfx_pas_bois",
            _                    => "sfx_pas_herbe",
        });
        Infrastructure.AudioManager.Play(key, 0.05f, -4f);
    }

    // --- POI Exploration ---

    private bool TryStartPoiExplore()
    {
        PointOfInterest nearest = FindNearestPoi();
        if (nearest == null)
            return false;
        if (!nearest.CanInteract)
        {
            Infrastructure.AudioManager.Play("sfx_interaction_unavailable", 0f);
            return false;
        }

        // Les POI sans temps de recherche sont explorés instantanément
        if (nearest.SearchTime <= 0f)
        {
            ApplyPoiLoot(nearest);
            nearest.Explore();
            _eventBus?.EmitSignal(EventBus.SignalName.PoiDiscovered,
                nearest.PoiId, nearest.PoiType, nearest.GlobalPosition);
            return true;
        }

        _poiTarget = nearest;
        _poiProgress = 0f;
        _isExploringPoi = true;
        Infrastructure.AudioManager.Play("sfx_poi_search", 0.03f);
        _interactionGauge.Begin(new Color("D4A843"));
        return true;
    }

    private void ProcessPoiExplore(float delta)
    {
        if (!_isExploringPoi || _poiTarget == null)
            return;

        if (!IsInstanceValid(_poiTarget) || _poiTarget.IsExplored)
        {
            CancelPoiExplore();
            return;
        }

        float dist = GlobalPosition.DistanceTo(_poiTarget.GlobalPosition);
        if (dist > InteractRange * 2f)
        {
            CancelPoiExplore();
            return;
        }

        _poiProgress += delta;
        _interactionGauge.SetRatio(_poiProgress / _poiTarget.SearchTime);

        if (_poiProgress >= _poiTarget.SearchTime)
            CompletePoiExplore();
    }

    private void CompletePoiExplore()
    {
        if (_poiTarget == null || !IsInstanceValid(_poiTarget))
        {
            CancelPoiExplore();
            return;
        }

        ApplyPoiLoot(_poiTarget);
        _poiTarget.Explore();
        _eventBus?.EmitSignal(EventBus.SignalName.PoiDiscovered,
            _poiTarget.PoiId, _poiTarget.PoiType, _poiTarget.GlobalPosition);

        _isExploringPoi = false;
        _interactionGauge.End();
        _poiTarget = null;
        _poiProgress = 0f;
    }

    private void CancelPoiExplore()
    {
        if (!_isExploringPoi)
            return;

        _isExploringPoi = false;
        _interactionGauge.End();
        _poiTarget = null;
        _poiProgress = 0f;
    }

    private void ApplyPoiLoot(PointOfInterest poi)
    {
        if (string.IsNullOrEmpty(poi.LootTableId))
            return;

        List<ResolvedLoot> loots = LootRewards.Resolve(LootResolver.Roll(poi.LootTableId, poi.LootRolls), this);
        for (int i = 0; i < loots.Count; i++)
        {
            LootRewards.Apply(loots[i], this, _eventBus);
            SpawnLootPopup(loots[i].Label, loots[i].Color, poi.GlobalPosition, i);
        }
    }

    private PointOfInterest FindNearestPoi()
    {
        Godot.Collections.Array<Node> pois = GetTree().GetNodesInGroup("pois");
        PointOfInterest nearest = null;
        float nearestDist = InteractRange * 1.5f;

        foreach (Node node in pois)
        {
            if (node is PointOfInterest poi && poi.CanInteract)
            {
                float dist = GlobalPosition.DistanceTo(poi.GlobalPosition);
                if (dist < nearestDist)
                {
                    nearest = poi;
                    nearestDist = dist;
                }
            }
        }

        return nearest;
    }

    // --- Chest Opening ---

    /// <summary>Écran de roulette des coffres.</summary>
    public void ConfigureLoot(UI.ChestLootScreen lootScreen) => _interaction.Configure(lootScreen);

    /// <summary>
    /// Texte flottant montrant le loot obtenu, empilé verticalement, centré sur le point. Au-dessus des entités (couche
    /// des chiffres de dégâts) : la scène est triée en Y, un décor ou un coffre le couvrirait.
    /// </summary>
    private void SpawnLootPopup(string text, Color color, Vector2 worldPos, int stackIndex, bool plusSign = true)
    {
        const float width = 240f;
        Label label = new()
        {
            Text = plusSign ? $"+ {text}" : text,
            HorizontalAlignment = HorizontalAlignment.Center,
            GlobalPosition = worldPos + new Vector2(-width / 2f, -25 - stackIndex * 16),
            ZIndex = 30,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", 12);
        label.Size = new Vector2(width, 16);

        GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, label);

        Vector2 startPos = label.GlobalPosition;
        Callable cleanup = Callable.From(() =>
        {
            if (IsInstanceValid(label))
                label.QueueFree();
        });

        SceneTreeTimer timer = GetTree().CreateTimer(0f);
        timer.Timeout += () =>
        {
            if (!IsInstanceValid(label))
                return;
            Tween tween = label.CreateTween();
            tween.SetParallel();
            tween.TweenProperty(label, "global_position", startPos + new Vector2(0, -35), 1.5f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
            tween.TweenProperty(label, "modulate:a", 0f, 1.5f)
                .SetDelay(0.6f);
            tween.Chain().TweenCallback(cleanup);
        };
    }

    // V2: CacheInventory retire — EssenceTracker remplacera

    // --- Combat ---

    private void Die()
    {
        _isDead = true;
        Mobility.Die();
        _mobilityFeedback.Suspend();
        _mobilityFeedback.Visible = false;
        CancelPoiExplore();
        _interaction.Cancel();
        Velocity = Vector2.Zero;
        foreach (Timer timer in _weaponTimers)
            timer.Stop();
        DeactivateSustainedCone();
        RemoveFromGroup("player");

        if (_hasSprite)
        {
            _sprite.SpeedScale = 1f;
            PlaySpriteAnim(SpriteAnimations[(int)_facing.Current, (int)SpriteAction.Death]);
            _heldWeapon.Hide();
        }

        _eventBus.EmitSignal(EventBus.SignalName.EntityDied, this);

        if (_hasSprite && _spriteMaterial != null)
            _spriteMaterial.SetShaderParameter("outline_enabled", false);
    }

    /// <summary>
    /// Avancement de la dissolution du joueur mort, de 0 à 1. La séquence de mort (DeathSequence) le pilote en temps
    /// réel, en phase avec ses éclats et le départ de l'Effacement.
    /// </summary>
    public void SetDeathDissolve(float amount)
    {
        if (_hasSprite && _spriteMaterial != null)
        {
            _spriteMaterial.SetShaderParameter("dissolve_amount", amount);
            return;
        }
        // Repli Polygon2D
        _visual.Modulate = new Color(_visual.Modulate, Mathf.Lerp(1f, 0.3f, amount));
        Scale = Vector2.One * Mathf.Lerp(1f, 0.5f, amount);
    }

    /// <summary>
    /// Vol de vie (plan 21 G6b) : la part des dégâts accumulée depuis le dernier intervalle revient en un seul soin,
    /// plafonné en PV max par seconde ; l'excédent au-delà du plafond est perdu.
    /// </summary>
    private void StepLifesteal(float delta)
    {
        _lifestealTimer += delta;
        if (_lifestealTimer < _defense.Config.LifestealTickSeconds)
            return;
        float window = _lifestealTimer;
        _lifestealTimer = 0f;
        if (_lifestealPending <= 0f)
            return;
        float capMultiplier = _objectMilestones?.LifestealCapMultiplier(_currentHp / EffectiveMaxHp) ?? 1f;
        float cap = EffectiveMaxHp * _defense.Config.LifestealMaxHpPerSecond * capMultiplier * window;
        Heal(Mathf.Min(_lifestealPending, cap));
        _lifestealPending = 0f;
    }

    private void ApplyRegen(float delta)
    {
        float amount = (BaseRegenRate + _bonusRegenRate) * (_objectMilestones?.RegenMultiplier ?? 1f) * delta;
        if (_isDead || amount <= 0f)
            return;
        HealingResult result = HealingResult.Resolve(GetInstanceId(), HealingKind.Regeneration, amount, _currentHp, EffectiveMaxHp);
        _currentHp += result.HpRestored;
        // À pleine vie, seul le contrat de soin reçoit l'excédent ; aucun signal UI supplémentaire par frame.
        if (result.HpRestored > 0f)
            _eventBus.EmitSignal(EventBus.SignalName.PlayerDamaged, _currentHp, EffectiveMaxHp);
        _eventBus.PublishPlayerHealing(result);
    }

    private void Flash(Color color, bool shake)
    {
        _visual.Color = color;
        Tween tween = CreateTween();
        tween.TweenProperty(_visual, "color", _originalColor, 0.2f)
            .SetDelay(0.05f);

        if (_hasSprite && _spriteMaterial != null)
        {
            _spriteMaterial.SetShaderParameter("flash_color", color);
            _spriteMaterial.SetShaderParameter("flash_amount", 1.0f);
            tween.Parallel().TweenMethod(
                Callable.From((float v) => _spriteMaterial.SetShaderParameter("flash_amount", v)),
                1.0f, 0.0f, 0.2f
            ).SetDelay(0.05f);
        }

        if (shake)
            Combat.ScreenShake.Instance?.ShakeMedium();
    }

    // --- Sprite Animation ---

    private static StringName[,] BuildSpriteAnimations()
    {
        string[] directions = CharacterFacing.DirectionNames;
        StringName[,] animations = new StringName[directions.Length, SpriteActionNames.Length];
        for (int direction = 0; direction < directions.Length; direction++)
            for (int action = 0; action < SpriteActionNames.Length; action++)
                animations[direction, action] = $"{directions[direction]}_{SpriteActionNames[action]}";
        return animations;
    }

    private void UpdateSpriteAnimation(float delta, Vector2 movementVelocity, float movementRate)
    {
        if (!_hasSprite)
            return;

        _hurtAnimTimer = Mathf.Max(_hurtAnimTimer - delta, 0f);

        // Le mouvement réellement parcouru pilote la pose, indépendamment de la visée des armes.
        if (movementRate > 0f)
            _facing.Update(movementVelocity.Normalized());

        // Action : death > hurt > dash > walk > idle
        SpriteAction action;
        if (_isDead)
            action = SpriteAction.Death;
        else if (_hurtAnimTimer > 0f)
            action = SpriteAction.Hurt;
        else if (Mobility.IsDashStep && movementRate > 0f)
            action = SpriteAction.Dash;
        else if (movementRate > 0f)
            action = SpriteAction.Walk;
        else
            action = SpriteAction.Idle;

        StringName animation = SpriteAnimations[(int)_facing.Current, (int)action];
        if (action == SpriteAction.Dash && !_sprite.SpriteFrames.HasAnimation(animation))
        {
            action = SpriteAction.Walk;
            animation = SpriteAnimations[(int)_facing.Current, (int)action];
        }
        _sprite.SpeedScale = action == SpriteAction.Walk ? movementRate : 1f;
        if (action == SpriteAction.Dash)
        {
            float frames = _sprite.SpriteFrames.GetFrameCount(animation);
            float fps = (float)_sprite.SpriteFrames.GetAnimationSpeed(animation);
            _sprite.SpeedScale = frames / (fps * Mobility.Config.DurationSeconds);
        }
        PlaySpriteAnim(animation);
    }

    private void PlaySpriteAnim(StringName animName)
    {
        if (animName == _currentAnimName)
            return;

        if (_sprite.SpriteFrames != null && _sprite.SpriteFrames.HasAnimation(animName))
        {
            _sprite.Play(animName);
            _currentAnimName = animName;
        }
    }

    private void OnWeaponAttackTimeout(int slotIndex)
    {
        if (_isDead || slotIndex >= _weaponSlots.Count)
            return;

        _equippedWeapon = _weaponSlots[slotIndex];

        string type = _equippedWeapon.Type?.ToLower() ?? "ranged";
        string pattern = _equippedWeapon.AttackPattern?.ToLower() ?? "linear";

        // Sustained cone : effet spécial persistant (ex: last_broadcast)
        WeaponSpecialEffect specialEffect = _equippedWeapon.SpecialEffect;
        if (specialEffect != null && specialEffect.Type == "sustained_cone")
        {
            if (!_isConeActive)
                ActivateSustainedCone(specialEffect);
            return;
        }

        // Orbital : pas d'attaque par timer, géré dans _PhysicsProcess
        if (pattern == "orbital")
        {
            SetupOrbitalWeapon(_equippedWeapon);
            return;
        }

        PerformDiscreteAttack(type, pattern);
        _objectMilestones?.CountAttack(_equippedWeapon);
    }

    /// <summary>Attaque ponctuelle de l'arme active : chaîne, mêlée ou tir (ni cône continu ni orbite).</summary>
    private void PerformDiscreteAttack(string type, string pattern)
    {
        // Chain melee : attaque spéciale avec rebond
        if (pattern == "chain")
            PerformChainAttack();
        else if (type == "melee")
            PerformMeleeAttack(pattern);
        else
            PerformRangedAttack(pattern);
    }

    /// <summary>
    /// Nombre propre de l'arme active : tirs, frappes de mêlée ou ondes du cône (plan 21 G6a). Stat entière fractionnaire
    /// (plan 23 R4) : 1,5, c'est un coup et une chance sur deux d'un second.
    /// </summary>
    private int RollOwnCount() => Mathf.Max(1, FractionalCount.Roll(GetWeaponStat("projectile_count", 1f), GD.Randf()));

    private void PerformRangedAttack(string pattern)
    {
        int ownCount = RollOwnCount();
        int extraCount = RollBonusProjectiles(_equippedWeapon);
        int totalProjectiles = ownCount + extraCount;
        bool spreadExtras = ExtraProjectilesSpread;
        float range = GetEffectiveWeaponRange();
        System.Collections.Generic.List<Node2D> targets = FindNearestEnemies(spreadExtras ? totalProjectiles : ownCount, range);
        if (targets.Count == 0)
            return;

        float baseDamage = ComputeBaseAttackDamage();
        _launchContext = BeginAttack(_equippedWeapon, baseDamage);
        float projectileSpeed = GetWeaponStat("projectile_speed", 400f);
        int totalPierce = FractionalCount.Roll(GetWeaponStat("projectile_pierce", 0f) + _projectilePierce, GD.Randf());
        Vector2 baseDirection = (targets[0].GlobalPosition - GlobalPosition).Normalized();
        PlayAttackFeedback(isMelee: false, baseDirection);
        float spreadAngle = GetWeaponStat("spread_angle", 20f);

        if (pattern == "burst")
        {
            // Salve : les projectiles en plus élargissent l'éventail, ou partent chacun vers leur cible au palier.
            SpawnBurstProjectiles(spreadExtras ? ownCount : totalProjectiles, spreadAngle, baseDirection, baseDamage, projectileSpeed, totalPierce);
            if (spreadExtras)
                for (int i = 0; i < extraCount; i++)
                    SpawnAimedProjectile(targets[(ownCount + i) % targets.Count], false, 0f, baseDamage, projectileSpeed, range, totalPierce);
            return;
        }

        float homingStrength = GetWeaponStat("homing_strength", 0f);
        bool isHoming = pattern == "homing" || homingStrength > 0f;
        for (int i = 0; i < totalProjectiles; i++)
        {
            bool extra = i >= ownCount;
            // Sans le palier du Papier carbone, un projectile en plus part en éventail autour du tir principal.
            float offset = extra && !spreadExtras ? ExtraFanOffset(i - ownCount, extraCount, spreadAngle) : 0f;
            Node2D target = extra && !spreadExtras ? targets[0] : targets[i % targets.Count];
            Projectile proj = SpawnAimedProjectile(target, isHoming, homingStrength, baseDamage, projectileSpeed, range, totalPierce, offset);

            // Ground fire : configurer le projectile pour spawner une zone au sol à l'impact
            if (proj != null && _equippedWeapon?.SpecialEffect?.Type == "ground_fire")
            {
                WeaponSpecialEffect se = _equippedWeapon.SpecialEffect;
                float gDmg = se.Params.TryGetValue("ground_damage", out float gd) ? gd : 5f;
                float gDur = StatusDuration(se.Params.TryGetValue("ground_duration", out float gdur) ? gdur : 2f);
                float gRad = ZoneScale(se.Params.TryGetValue("ground_radius", out float grad) ? grad : 30f);
                proj.SetGroundFire(gDmg, gDur, gRad);
            }
        }
    }

    /// <summary>Un tir vers <paramref name="target"/>, dévié de <paramref name="offsetDegrees"/>, guidé si l'arme l'est.</summary>
    private Projectile SpawnAimedProjectile(Node2D target, bool isHoming, float homingStrength, float baseDamage, float speed,
        float range, int pierce, float offsetDegrees = 0f)
    {
        Vector2 direction = (target.GlobalPosition - GlobalPosition).Normalized().Rotated(Mathf.DegToRad(offsetDegrees));
        bool isCrit = _critChance > 0f && GD.Randf() < _critChance;
        Projectile proj = SpawnProjectile(direction, ProjectileDamage(baseDamage, isCrit), speed, range, pierce, isCrit);
        if (proj != null && isHoming)
            proj.SetHoming(homingStrength > 0f ? homingStrength : 0.8f, target);
        return proj;
    }

    /// <summary>
    /// Écart du <paramref name="index"/>-ième projectile en plus, de part et d'autre du tir principal : ±1, ±2… pas,
    /// le pas partageant l'angle d'éventail de l'arme entre les projectiles en plus.
    /// </summary>
    private static float ExtraFanOffset(int index, int count, float spreadAngle)
    {
        float step = spreadAngle / Mathf.Max(2, count);
        return step * (index / 2 + 1) * (index % 2 == 0 ? 1f : -1f);
    }

    private void PerformMeleeAttack(string pattern)
    {
        // Onde circulaire : son rayon est une zone. Arc : la portée donne l'allonge, la zone l'ouverture.
        float range = pattern == "circular" ? ZoneScale(GetEffectiveWeaponRange()) : GetEffectiveWeaponRange();
        float arcAngle = pattern switch
        {
            "circular" => 360f,
            "linear" => 60f,
            _ => Mathf.Min(360f, ZoneScale(GetWeaponStat("arc_angle", 120f)))
        };

        System.Collections.Generic.List<Enemy> enemies = FindEnemiesInArc(range, arcAngle);
        if (enemies.Count == 0)
            return;

        Vector2 attackDirection = (enemies[0].GlobalPosition - GlobalPosition).Normalized();
        PlayAttackFeedback(isMelee: true, attackDirection);

        float baseDamage = ComputeBaseAttackDamage();
        AttackContext context = BeginAttack(_equippedWeapon, baseDamage);
        int strikeCount = RollOwnCount() + RollBonusProjectiles(_equippedWeapon);
        float spreadAngle = strikeCount > 1
            ? Mathf.Clamp(GetWeaponStat("spread_angle", 20f), 0f, 120f)
            : 0f;
        float startOffset = strikeCount == 1 ? 0f : -spreadAngle * 0.5f;
        float step = strikeCount <= 1 || spreadAngle <= 0.001f ? 0f : spreadAngle / (strikeCount - 1);
        bool fullCircle = arcAngle >= 359f;
        float arcHalf = arcAngle * 0.5f;

        SpawnMeleeSlashVisuals(attackDirection, range, arcAngle, strikeCount, spreadAngle);

        float coverageArc = fullCircle ? 360f : Mathf.Clamp(arcAngle + spreadAngle, arcAngle, 360f);
        System.Collections.Generic.List<Enemy> strikeCandidates = FindEnemiesInArc(range, coverageArc, attackDirection);
        if (strikeCandidates.Count == 0)
            return;

        float clampedCritChance = Mathf.Clamp(_critChance, 0f, 1f);
        float critDamageFactor = 1f + clampedCritChance * Mathf.Max(0f, _critMultiplier - 1f);

        foreach (Enemy enemy in strikeCandidates)
        {
            if (!IsInstanceValid(enemy) || enemy.IsDying)
                continue;

            int firstHitIndex;
            int lastHitIndex;

            if (fullCircle)
            {
                firstHitIndex = 0;
                lastHitIndex = strikeCount - 1;
            }
            else
            {
                Vector2 toEnemy = enemy.GlobalPosition - GlobalPosition;
                if (toEnemy.LengthSquared() <= 0.0001f)
                    continue;

                float enemyOffset = Mathf.RadToDeg(attackDirection.AngleTo(toEnemy.Normalized()));
                if (step <= 0.0001f)
                {
                    if (Mathf.Abs(enemyOffset - startOffset) > arcHalf)
                        continue;

                    firstHitIndex = 0;
                    lastHitIndex = strikeCount - 1;
                }
                else
                {
                    float hitMin = (enemyOffset - arcHalf - startOffset) / step;
                    float hitMax = (enemyOffset + arcHalf - startOffset) / step;
                    firstHitIndex = Mathf.Clamp(Mathf.CeilToInt(hitMin), 0, strikeCount - 1);
                    lastHitIndex = Mathf.Clamp(Mathf.FloorToInt(hitMax), 0, strikeCount - 1);
                    if (lastHitIndex < firstHitIndex)
                        continue;
                }
            }

            int hitCount = lastHitIndex - firstHitIndex + 1;
            if (hitCount <= 0)
                continue;

            bool hasCrit = clampedCritChance > 0f && GD.Randf() < GetCombinedProcChance(clampedCritChance, hitCount);
            float totalDamage = ResolveHitDamage(enemy, baseDamage * hitCount * critDamageFactor, hasCrit);
            AttackContext hitContext = context with { ReferenceDamage = totalDamage };
            enemy.TakeDamage(totalDamage, hasCrit, source: hitContext);
            OnAttackHit(enemy, totalDamage, hasCrit, _equippedWeapon, triggerCount: hitCount, context: hitContext);
        }
        if (_objectMilestones?.HasZoneEcho == true)
            QueueMeleeEchoes(attackDirection, range, arcAngle, strikeCount, startOffset, step, baseDamage * critDamageFactor, context);
    }

    private void SpawnMeleeSlashVisuals(Vector2 baseDirection, float range, float arcAngle, int strikeCount, float spreadAngle)
    {
        const int maxSlashVisuals = 7;
        int visualCount = Mathf.Min(strikeCount, maxSlashVisuals);
        float visualStart = visualCount == 1 ? 0f : -spreadAngle * 0.5f;
        float visualStep = visualCount == 1 ? 0f : spreadAngle / (visualCount - 1);

        for (int visualIndex = 0; visualIndex < visualCount; visualIndex++)
        {
            float angleOffset = visualStart + (visualStep * visualIndex);
            Vector2 visualDirection = baseDirection.Rotated(Mathf.DegToRad(angleOffset)).Normalized();
            SpawnSlashEffect(visualDirection, range, arcAngle);
        }
    }

    /// <summary>Salve en éventail de <paramref name="count"/> projectiles pleins.</summary>
    private void SpawnBurstProjectiles(int count, float spreadAngle, Vector2 baseDirection, float baseDamage, float projectileSpeed, int pierce)
    {
        float range = GetEffectiveWeaponRange();
        float start = count <= 1 ? 0f : -spreadAngle * 0.5f;
        float step = count <= 1 ? 0f : spreadAngle / (count - 1);
        for (int i = 0; i < count; i++)
        {
            Vector2 direction = baseDirection.Rotated(Mathf.DegToRad(start + step * i)).Normalized();
            bool isCrit = _critChance > 0f && GD.Randf() < _critChance;
            SpawnProjectile(direction, ProjectileDamage(baseDamage, isCrit), projectileSpeed, range, pierce, isCrit);
        }
    }

    private float ProjectileDamage(float baseDamage, bool isCrit) => isCrit ? baseDamage * _critMultiplier : baseDamage;

    private Projectile SpawnProjectile(Vector2 direction, float damage, float speed, float range, int pierce, bool isCrit)
    {
        Projectile projectile = CombatPools.Instance?.TakePlayerProjectile();
        projectile?.Launch(GlobalPosition, direction, damage, speed, Mathf.Clamp(range / Mathf.Max(speed, 1f), 0.2f, 4f),
            pierce, isCrit, this, _equippedWeapon?.Base, _equippedWeapon, context: _launchContext with { ReferenceDamage = damage },
            pierceDamageRamp: PierceDamageRamp, sizeScale: WeaponSizeScale);
        return projectile;
    }

    private System.Collections.Generic.List<Node2D> FindNearestEnemies(int count, float maxRange)
    {
        Godot.Collections.Array<Node> enemies = _groupCache.GetEnemies();
        System.Collections.Generic.List<(Node2D enemy, float dist)> inRange = new();

        foreach (Node node in enemies)
        {
            if (node is Node2D enemy)
            {
                float dist = GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (dist < maxRange)
                    inRange.Add((enemy, dist));
            }
        }

        inRange.Sort((a, b) => a.dist.CompareTo(b.dist));
        PromotePriorityTarget(inRange, static candidate => candidate.enemy);

        System.Collections.Generic.List<Node2D> result = new();
        int limit = System.Math.Min(count, inRange.Count);
        for (int i = 0; i < limit; i++)
            result.Add(inRange[i].enemy);

        return result;
    }

    private System.Collections.Generic.List<Enemy> FindEnemiesInArc(float maxRange, float arcAngle, Vector2? forwardOverride = null)
    {
        Godot.Collections.Array<Node> enemies = _groupCache.GetEnemies();
        System.Collections.Generic.List<(Enemy enemy, float dist, Vector2 dir)> candidates = new();

        foreach (Node node in enemies)
        {
            if (node is not Enemy enemy || enemy.IsDying)
                continue;

            Vector2 toEnemy = enemy.GlobalPosition - GlobalPosition;
            float dist = toEnemy.Length();
            if (dist > maxRange || dist <= 0.001f)
                continue;

            candidates.Add((enemy, dist, toEnemy / dist));
        }

        if (candidates.Count == 0)
            return new System.Collections.Generic.List<Enemy>();

        Vector2 forward;
        if (forwardOverride.HasValue && forwardOverride.Value.LengthSquared() > 0.0001f)
        {
            forward = forwardOverride.Value.Normalized();
        }
        else
        {
            candidates.Sort((a, b) => a.dist.CompareTo(b.dist));
            PromotePriorityTarget(candidates, static candidate => candidate.enemy);
            forward = candidates[0].dir;
        }

        bool fullCircle = arcAngle >= 359f;
        float dotThreshold = fullCircle ? -1f : Mathf.Cos(Mathf.DegToRad(arcAngle * 0.5f));

        System.Collections.Generic.List<Enemy> result = new();
        foreach ((Enemy enemy, float _, Vector2 dir) in candidates)
        {
            if (fullCircle || forward.Dot(dir) >= dotThreshold)
                result.Add(enemy);
        }

        return result;
    }

    /// <summary>Dégâts de l'attaque qui part : ceux de l'arme active, et la charge de la Semelle usée s'il y en a une.</summary>
    private float ComputeBaseAttackDamage() => ComputeBaseAttackDamage(_equippedWeapon) * (_objectTriggers?.ConsumeStride() ?? 1f);

    private float ComputeBaseAttackDamage(WeaponInstance weapon)
    {
        float weaponDamage = weapon?.GetStat("damage", AttackDamage) ?? AttackDamage;
        float characterDamageFactor = AttackDamage / 10f;
        float damage = weaponDamage * characterDamageFactor * _damageMultiplier * _erasurePenalty.Damage;
        return damage;
    }

    private float GetEffectiveWeaponRange() => GetEffectiveWeaponRange(_equippedWeapon);

    /// <summary>
    /// Portée : allonge d'un coup, distance d'un tir (plan 05 §4). La zone ne l'allonge pas ; elle agrandit les
    /// arcs, ondes, cônes, feux et explosions (<see cref="ZoneScale"/>).
    /// </summary>
    private float GetEffectiveWeaponRange(WeaponInstance weapon)
    {
        float weaponRange = weapon?.GetStat("range", AttackRange) ?? AttackRange;
        return weaponRange * PersonalRangeFactor(weapon) * _attackRangeMultiplier;
    }

    /// <summary>
    /// Portée propre au personnage, rapportée à 300. Elle allonge la mêlée mais ne la raccourcit plus : à ×0,83, la
    /// Forgeuse frappait à 46 px avec le Parcmètre, au contact des créatures (plan 24 lot L3).
    /// </summary>
    private float PersonalRangeFactor(WeaponInstance weapon)
    {
        float factor = AttackRange / 300f;
        return weapon?.Base.Type == "melee" ? Mathf.Max(1f, factor) : factor;
    }

    /// <summary>Taille d'une zone d'effet (rayon, angle) après les bonus de zone du joueur.</summary>
    private float ZoneScale(float value) => value * _aoeMultiplier;

    /// <summary>Échelle des visuels d'arme selon la stat de taille, plafonnée pour la lisibilité (plan 21 G6e).</summary>
    private float WeaponSizeScale => WeaponVisualConfig.Load().ScaleFor(_aoeMultiplier);

    /// <summary>
    /// Valeur effective d'une stat d'arme, telle que le combat l'applique (arme × niveau × personnage × bonus) :
    /// c'est elle que montrent le level-up et la pause, jamais la base des données.
    /// </summary>
    public float GetWeaponStatForDisplay(WeaponInstance weapon, string key) => key switch
    {
        "damage" => ComputeBaseAttackDamage(weapon),
        "attack_speed" => AttackSpeed * weapon.GetStat("attack_speed", 1f) * _attackSpeedMultiplier,
        "range" => weapon.AttackPattern == "circular" ? ZoneScale(GetEffectiveWeaponRange(weapon)) : GetEffectiveWeaponRange(weapon),
        "arc_angle" => Mathf.Min(360f, ZoneScale(weapon.GetStat("arc_angle", 120f))),
        "cone_angle_end" => Mathf.Min(180f, ZoneScale(weapon.GetStat("cone_angle_end", 60f))),
        "projectile_pierce" => weapon.GetStat("projectile_pierce", 0f) + _projectilePierce,
        _ => weapon.GetStat(key, 0f),
    };

    private float GetWeaponStat(string key, float fallback)
    {
        if (_equippedWeapon == null)
            return fallback;

        return _equippedWeapon.GetStat(key, fallback);
    }

    private void PlayAttackFeedback(bool isMelee, Vector2 direction)
    {
        string attackAudio = _equippedWeapon?.Base.AttackAudio;
        if (!string.IsNullOrEmpty(attackAudio))
            Infrastructure.AudioManager.Play(attackAudio, 0.04f);
        if (_visual == null)
            return;

        _facingDirection = direction.Normalized();
        _attackFx.PlayRecoil(isMelee);
        _heldWeapon.OnAttack(_equippedWeapon?.Base, direction);
        if (!isMelee)
            SpawnMuzzleFlash(direction);
        // Personnage sans sprite : le polygone de repli garde sa réaction propre.
        if (_hasSprite)
            return;
        if (_attackFeedbackTween != null && _attackFeedbackTween.IsValid())
            _attackFeedbackTween.Kill();

        _visual.Scale = isMelee ? new Vector2(1.18f, 0.86f) : new Vector2(1.1f, 0.92f);
        _visual.Color = isMelee ? new Color(1f, 0.86f, 0.68f) : new Color(1f, 0.98f, 0.8f);

        _attackFeedbackTween = CreateTween();
        _attackFeedbackTween.SetParallel();
        _attackFeedbackTween.TweenProperty(_visual, "scale", Vector2.One, isMelee ? 0.11f : 0.09f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        _attackFeedbackTween.TweenProperty(_visual, "color", _originalColor, isMelee ? 0.12f : 0.1f);
    }

    private void SpawnMuzzleFlash(Vector2 direction)
    {
        _attackFx.PlayMuzzle(_equippedWeapon?.Base, direction);
    }

    private void SpawnSlashEffect(Vector2 direction, float range, float arcAngle)
    {
        _attackFx.PlayMelee(_equippedWeapon?.Base, direction, range, arcAngle, WeaponSizeScale);
    }

    private void UpdateAttackSpeed()
    {
        float mult = _attackSpeedMultiplier;
        for (int i = 0; i < _weaponSlots.Count && i < _weaponTimers.Count; i++)
        {
            WeaponInstance weapon = _weaponSlots[i];
            float weaponAtkSpd = weapon.GetStat("attack_speed", 1f);
            float attacksPerSecond = AttackSpeed * weaponAtkSpd * mult;
            _weaponTimers[i].WaitTime = 1.0f / Mathf.Max(0.05f, attacksPerSecond);
        }
    }

}
