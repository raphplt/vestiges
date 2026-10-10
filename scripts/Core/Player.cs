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

#if TOOLS
    private bool _isAIControlled;
    private bool _isGodMode;
    // Les entrées de banc ne font pas partie du contrôleur distribué.
    public bool IsAIControlled
    {
        get => _isAIControlled;
        set { DevelopmentMode.RequireTestAccess(); _isAIControlled = value; }
    }
    public Vector2 AIInputOverride { get; set; }
    public bool IsGodMode
    {
        get => _isGodMode;
        set { DevelopmentMode.RequireTestAccess(); _isGodMode = value; }
    }
#endif

    public PlayerMobility Mobility { get; private set; }
    private MobilityFeedback _mobilityFeedback;
    private bool _mobilityRequiresRelease;
    private ErasureManager _erasureManager;

    private GameManager _gameManager;

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
    private int _temperCharges;
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
    private readonly System.Collections.Generic.List<OrbitContacts> _orbitalContacts = new();
    private readonly System.Collections.Generic.List<Enemy> _orbitalEntered = new();
    private const float OrbitalHitRadius = 8f;
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
    private int _coneWaves;
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
    /// <summary>Chiffre des dégâts reçus : au-dessus de la tête du personnage.</summary>
    private static readonly Vector2 ReceivedNumberOffset = new(0f, -34f);
    private static readonly Color ShieldFlashColor = new(0.7f, 0.85f, 1f);

    /// <summary>Rayon du corps au sol : les tirs ennemis, qui n'ont plus de zone physique, le testent (plan 29).</summary>
    public float BodyRadius { get; private set; } = 12f;

    public override void _Ready()
    {
        _currentHp = MaxHp;
        BodyRadius = GetNode<CollisionShape2D>("CollisionShape2D").Shape is CircleShape2D body ? body.Radius : BodyRadius;
        _visual = GetNode<Polygon2D>("Visual");
        _sprite = GetNode<AnimatedSprite2D>("Sprite");
        _attackFx = new PlayerAttackFx(this, _sprite);
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
                _statusVisual.Attach(_spriteMaterial, webbed => WebbedChanged?.Invoke(webbed));

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
        float weaponAtkSpd = instance.GetStat("attack_speed");
        timer.WaitTime = 1.0f / Mathf.Max(0.05f, AttackSpeed * weaponAtkSpd * _attackSpeedMultiplier);
        timer.Autostart = true;
        timer.Timeout += () => OnWeaponAttackTimeout(capturedIndex);
        AddChild(timer);
        _weaponTimers.Add(timer);

        // Une orbitale n'attend pas le premier tic de son minuteur (20 s pour la Boîte à musique) : ses orbes
        // apparaissent dès qu'elle est portée.
        if (instance.AttackPattern == AttackPatternKind.Orbital)
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
        // Le Scalpel repris plus tard repart de zéro coup.
        _weaponHitCounters.Remove(removed.Id);
        if (_isConeActive && removed.SpecialEffect?.Kind == SpecialEffectKind.SustainedCone)
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

    /// <summary>
    /// Applique une amélioration tirée au level-up, à l'Atelier ou à la Faille : ses gains, et un niveau de plus. Une
    /// amélioration trempée (plus de stats que sa rareté n'en donne) consomme une charge de Trempe.
    /// </summary>
    public bool UpgradeWeapon(string weaponId, IReadOnlyList<StatGain> gains, string rarityId = null)
    {
        // Le niveau n'existe qu'à un endroit, l'instance d'arme : level-up, Atelier et badge le lisent tous.
        WeaponInstance weapon = FindWeaponSlot(weaponId, out int slot);
        if (weapon == null || !weapon.ApplyUpgrade(gains, rarityId))
            return false;
        if (_temperCharges > 0 && rarityId != null && gains.Count > UpgradeRoller.Get(rarityId).WeaponStats)
            _temperCharges--;

        RefreshAttackSpeed();
        if (weapon == _orbitalWeapon)
            SetupOrbitalWeapon(weapon);
        _eventBus?.EmitSignal(EventBus.SignalName.WeaponUpgraded, weaponId, slot, "all", weapon.Level);
        _eventBus?.EmitSignal(EventBus.SignalName.WeaponInventoryChanged);
        GD.Print($"[Player] Weapon level: {weaponId} → {weapon.Level}/{weapon.MaxLevel}");
        return true;
    }

    /// <summary>Améliorations d'armes à venir qui toucheront une stat de plus (Trempe de l'Atelier, plan 22 C2).</summary>
    public int TemperCharges => _temperCharges;

    public void GrantTemper(int upgrades) => _temperCharges += upgrades;

    /// <summary>Retrempe : remplace les gains de la dernière amélioration de l'arme, sans changer son niveau.</summary>
    public bool RetemperWeapon(string weaponId, IReadOnlyList<StatGain> gains, string rarityId)
    {
        WeaponInstance weapon = FindWeaponSlot(weaponId, out int slot);
        if (weapon == null || !weapon.Retemper(gains, rarityId))
            return false;
        RefreshAttackSpeed();
        if (weapon == _orbitalWeapon)
            SetupOrbitalWeapon(weapon);
        _eventBus?.EmitSignal(EventBus.SignalName.WeaponUpgraded, weaponId, slot, "all", weapon.Level);
        _eventBus?.EmitSignal(EventBus.SignalName.WeaponInventoryChanged);
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
        _attackFx.Tick(dt);
        bool inputAllowed = _gameManager.CurrentState == GameManager.GameState.Run;
        Vector2 inputDir = inputAllowed ? Input.GetVector("move_left", "move_right", "move_up", "move_down") : Vector2.Zero;
#if TOOLS
        if (inputAllowed && IsAIControlled)
            inputDir = AIInputOverride.LimitLength();
#endif

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
        ProcessFootsteps(dt, dashMovement ? 0f : movementRate);
        ApplyRegen(dt);
        StepLifesteal(dt);
        StepDefense(dt);
        ProcessSlowDecay(dt);
        _statusVisual.Update(dt, _slowTimer > 0f, _erasurePenalty.Speed < 1f, Velocity.LengthSquared() > 4f, GlobalPosition);
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
            bool acceptsInput = !_mobilityRequiresRelease;
#if TOOLS
            acceptsInput &= !IsAIControlled;
#endif
            if (acceptsInput)
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

#if TOOLS
    /// <summary>Entrée de simulation ; reprend le comportement de l'action d'interaction.</summary>
    public void AITriggerInteract()
    {
        DevelopmentMode.RequireTestAccess();
        if (_isDead || !IsAIControlled || Mobility.IsDashing || _gameManager.CurrentState != GameManager.GameState.Run || GetTree().Paused) return;
        if (_isExploringPoi || _interaction.IsActive)
        {
            CancelPoiExplore();
            _interaction.Cancel();
        }
        else if (TryStartPoiExplore()) { }
        else if (_interaction.TryStart()) { }
    }
#endif

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
            case "peril":
                if (modifierType == "additive") AddObjectPeril(value);
                break;
        }
    }

    /// <summary>
    /// Appelé par les projectiles à l'impact : effets du coup. <paramref name="rawDamage"/> est le coup avant sa
    /// résolution sur la cible ; à défaut, le coup résolu en tient lieu.
    /// </summary>
    public void OnProjectileHit(Enemy enemy, float damage, bool isCrit, WeaponInstance source, AttackContext context = default,
        float? rawDamage = null)
    {
        OnAttackHit(enemy, damage, rawDamage ?? damage, isCrit, source, context: context);
    }

    /// <summary>
    /// Effets d'un coup porté par <paramref name="source"/> : ceux de l'arme (effet au contact, recul, effet spécial)
    /// restent ceux de l'arme qui a frappé. <paramref name="damage"/> est le coup résolu sur cette cible,
    /// <paramref name="rawDamage"/> le coup d'avant, dont partent les coups secondaires de l'arme.
    /// </summary>
    private void OnAttackHit(Enemy enemy, float damage, float rawDamage, bool isCrit, WeaponInstance source, int triggerCount = 1,
        bool showImpact = true, AttackContext context = default)
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
            switch (ohe.Kind)
            {
                case OnHitEffectKind.Bleed:
                    // La référence du saignement est le coup qui le pose : celle d'un cône couvre toute sa durée.
                    enemy.ApplyBleed(ohe.Damage, StatusDuration(ohe.Duration), context with { ReferenceDamage = damage });
                    break;
                case OnHitEffectKind.Slow:
                    enemy.ApplySlow(ohe.Value, StatusDuration(ohe.Duration), context);
                    break;
                case OnHitEffectKind.Disorient:
                    enemy.ApplyDisorient(StatusDuration(ohe.Duration), context);
                    break;
                case OnHitEffectKind.Freeze:
                    enemy.Freeze(StatusDuration(ohe.Duration));
                    break;
            }
        }

        // Objets à l'impact : après l'effet de l'arme (un ralentissement de la Cloche compte pour le Glaçon), seulement
        // sur un coup direct, et au rythme des impacts visibles pour un cône continu.
        if (_objectTriggers != null && showImpact && context.Kind == DamageKind.DirectWeapon)
            _objectTriggers.OnWeaponImpact(enemy, ComputeBaseAttackDamage(source), source, triggerCount, context, isCrit, damage);

        // --- Weapon knockback ---
        float knockback = source?.GetStat("knockback") ?? 0f;
        if (knockback > 0f)
        {
            Vector2 knockDir = (enemy.GlobalPosition - GlobalPosition).Normalized();
            enemy.ApplyKnockback(knockDir, knockback);
        }

        // --- Weapon special effects ---
        WeaponSpecialEffect se = source?.SpecialEffect;
        if (se != null)
            ProcessWeaponSpecialOnHit(se, enemy, damage, rawDamage, source, context);
    }

    /// <summary>
    /// Coup secondaire d'une arme (écho des Gants, forme des Craies) : un coup de cette arme sur sa propre cible
    /// (DECISIONS §70). Résolu sur cette cible, il compte pour le vol de vie, le relevé de l'arme et les objets à
    /// l'impact ; jamais critique, il ne relance ni l'effet au contact ni l'effet spécial de l'arme.
    /// </summary>
    /// <param name="origin">Centre de l'écho ou de la forme : la gerbe à la couleur de l'arme en part (plan 27 V2b).</param>
    private void SecondaryHit(Enemy enemy, float rawDamage, WeaponInstance source, AttackContext context, Vector2 origin)
    {
        if (_isDead)
            return;
        float damage = ResolveHitDamage(enemy, rawDamage, false);
        AttackContext secondary = context.As(DamageKind.SecondaryWeapon);
        enemy.TakeDamage(damage, source: secondary);
        _attackFx.PlayHit(PlayerAttackFx.FamilyOf(source?.Base), enemy.GlobalPosition, false, origin);
        if (source != null)
        {
            _weaponLedger.AddDamage(source.Id, damage);
            if (enemy.ClaimKillCredit())
                _weaponLedger.AddKill(source.Id);
        }
        _lifestealPending += damage * _lifesteal;
        _objectTriggers?.OnWeaponImpact(enemy, ComputeBaseAttackDamage(source), source, 1, secondary, dealt: damage);
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

    private void ProcessWeaponSpecialOnHit(WeaponSpecialEffect se, Enemy enemy, float damage, float rawDamage, WeaponInstance source,
        AttackContext context)
    {
        switch (se.Kind)
        {
            case SpecialEffectKind.HealEveryNHits:
            {
                string weaponId = source.Id;
                _weaponHitCounters.TryGetValue(weaponId, out int count);
                count++;
                if (count >= (int)se.Get(SpecialEffectParam.HitsPerHeal))
                {
                    Heal(se.Get(SpecialEffectParam.HealAmount));
                    count = 0;
                }
                _weaponHitCounters[weaponId] = count;
                break;
            }
            case SpecialEffectKind.InstantDisintegrate:
            {
                if (enemy.IsDying)
                {
                    // Pas de particules, l'ennemi disparaît instantanément
                    enemy.Scale = Vector2.Zero;
                    enemy.Modulate = new Color(1f, 1f, 1f, 0f);
                }
                break;
            }
            case SpecialEffectKind.DelayedEcho:
            {
                float delay = se.Get(SpecialEffectParam.EchoDelay);
                float echoRatio = se.Get(SpecialEffectParam.EchoDamagePercent);
                float echoDamage = damage * echoRatio;
                float echoRaw = rawDamage * echoRatio;
                Vector2 echoPos = enemy.GlobalPosition;
                float echoRadius = ZoneScale(se.Get(SpecialEffectParam.EchoRadius));
                // Enchaînement (ascension des Gants de boxe) : l'écho repart plusieurs fois, à intervalles égaux.
                int echoCount = (int)se.Get(SpecialEffectParam.EchoCount);
                for (int echo = 1; echo <= echoCount; echo++)
                {
                    GetTree().CreateTimer(delay * echo).Timeout += () =>
                    {
                        // Réapplique les dégâts à la position d'origine (AoE fantôme)
                        using CrowdQuery crowd = CrowdIndex.Near(echoPos, echoRadius);
                        foreach (Node node in crowd.Targets)
                        {
                            if (node is Enemy { IsActive: true, IsDying: false } e && IsInstanceValid(e))
                            {
                                if (e.GlobalPosition.DistanceTo(echoPos) < echoRadius)
                                    SecondaryHit(e, echoRaw, source, context, echoPos);
                            }
                        }
                        _attackFx.PlayEcho(source?.Base, echoPos, echoRadius);
                        if (_objectMilestones?.HasZoneEcho == true)
                            _objectMilestones.QueueCircleEcho(echoPos, echoRadius, echoDamage, source, context);
                    };
                }
                break;
            }
            case SpecialEffectKind.GroundFire:
            {
                // Géré au moment de l'impact du projectile, pas ici
                break;
            }
            case SpecialEffectKind.LocalTimeSlow:
            {
                float radius = ZoneScale(se.Get(SpecialEffectParam.SlowRadius));
                float factor = se.Get(SpecialEffectParam.SlowFactor);
                float duration = StatusDuration(se.Get(SpecialEffectParam.SlowDuration));
                // Arrêt sur image (ascension du Chronomètre) : le champ fige au lieu de ralentir.
                float freeze = StatusDuration(se.Get(SpecialEffectParam.FreezeSeconds));
                Vector2 impactPos = enemy.GlobalPosition;
                // La cible frappée d'abord : elle est au centre du champ, qu'elle soit encore recensée ou non.
                ApplyTimeField(enemy, freeze, factor, duration, context);
                using CrowdQuery crowd = CrowdIndex.Near(impactPos, radius);
                foreach (Node node in crowd.Targets)
                {
                    if (node is Enemy { IsActive: true, IsDying: false } e && e != enemy && IsInstanceValid(e) && e.GlobalPosition.DistanceTo(impactPos) < radius)
                        ApplyTimeField(e, freeze, factor, duration, context);
                }
                SpawnTimeSlowVisual(impactPos, radius, duration);
                break;
            }
            case SpecialEffectKind.RandomShape:
            {
                float aoeRadius = ZoneScale(se.Get(SpecialEffectParam.ShapeRadius));
                float shapeRatio = se.Get(SpecialEffectParam.ShapeDamageRatio);
                float shapeDamage = damage * shapeRatio;
                float shapeRaw = rawDamage * shapeRatio;
                Vector2 impactPos = enemy.GlobalPosition;
                using CrowdQuery crowd = CrowdIndex.Near(impactPos, aoeRadius);
                foreach (Node node in crowd.Targets)
                {
                    if (node is Enemy { IsActive: true, IsDying: false } e && IsInstanceValid(e) && e != enemy)
                    {
                        if (e.GlobalPosition.DistanceTo(impactPos) < aoeRadius)
                            SecondaryHit(e, shapeRaw, source, context, impactPos);
                    }
                }
                Combat.CombatPools.Instance?.ShowChalkShape(impactPos, aoeRadius);
                if (_objectMilestones?.HasZoneEcho == true)
                    _objectMilestones.QueueCircleEcho(impactPos, aoeRadius, shapeDamage, source, context);
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
        int orbitalCount = Mathf.Max(1, Mathf.FloorToInt(weapon.GetStat("orbital_count")));
        // Recréées aussi quand la taille change : contact et note suivent la stat de taille.
        if (_orbitalWeapon == weapon && _orbitalProjectiles.Count == orbitalCount && Mathf.IsEqualApprox(_orbitalSize, _aoeMultiplier))
            return;

        ClearOrbitals();
        _orbitalWeapon = weapon;
        _orbitalSize = _aoeMultiplier;
        for (int i = 0; i < orbitalCount; i++)
        {
            Node2D orb = new() { Name = $"OrbitalOrb_{i}" };
            orb.AddChild(PlayerAttackFx.CreateOrbitalVisual(WeaponSizeScale));
            AddChild(orb);
            _orbitalProjectiles.Add(orb);
            _orbitalContacts.Add(new OrbitContacts());
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
        _orbitalContacts.Clear();
        _orbitalWeapon = null;
    }

    private void ProcessOrbitalWeapons(float delta)
    {
        if (_orbitalProjectiles.Count == 0 || _orbitalWeapon == null)
            return;

        float orbitalSpeed = _orbitalWeapon.GetStat("orbital_speed");
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
            PlayerAttackFx.AnimateOrbitalVisual(orb.GetChild<Sprite2D>(0), _orbitalAngle, i);
            // Les créatures n'ont plus de corps : l'orbe frappe celles qui entrent dans son disque (plan 29 B).
            _orbitalContacts[i].Step(GlobalPosition + orb.Position, ZoneScale(OrbitalHitRadius), _orbitalEntered);
            foreach (Enemy enemy in _orbitalEntered)
                OrbitalHit(enemy);
        }
    }

    private void OrbitalHit(Enemy enemy)
    {
        // Une frappe précédente du même tick a pu tuer la créature ou retirer l'arme.
        if (_orbitalWeapon == null || !enemy.IsActive || enemy.IsDying || enemy.IsBurrowed)
            return;
        PlayWeaponSound(_orbitalWeapon);
        float rawDamage = ComputeBaseAttackDamage(_orbitalWeapon);
        float damage = ResolveHitDamage(enemy, rawDamage, false);
        AttackContext context = BeginAttack(_orbitalWeapon, damage);
        enemy.TakeDamage(damage, source: context);
        OnAttackHit(enemy, damage, rawDamage, false, _orbitalWeapon, context: context);
    }

    // --- Sustained Cone Attack ---

    private void ActivateSustainedCone(WeaponSpecialEffect effect)
    {
        _isConeActive = true;
        _coneDuration = effect.Get(SpecialEffectParam.ConeDuration);
        _coneAttackTimer = _coneDuration;
        _coneDamageRampPerSec = effect.Get(SpecialEffectParam.ConeDamageRamp);
        _coneAngleStart = Mathf.Min(180f, ZoneScale(GetWeaponStat("cone_angle_start")));
        _coneAngleEnd = Mathf.Min(180f, ZoneScale(GetWeaponStat("cone_angle_end")));
        _coneRange = GetEffectiveWeaponRange();
        // Chaque onde en plus repasse à pleins dégâts, comme les frappes d'une onde circulaire en mêlée.
        _coneWaves = RollOwnCount() + RollBonusProjectiles(_equippedWeapon);
        _coneBaseDamage = ComputeBaseAttackDamage() * _coneWaves;
        _coneWeapon = _equippedWeapon;
        _coneContext = BeginAttack(_coneWeapon, _coneBaseDamage * _coneDuration * (1f + _coneDamageRampPerSec * _coneDuration * 0.5f));

        // Un contact radio au départ : le maintien du cône ne doit pas créer un fond continu.
        PlayAttackSound();
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
        // Rayon écran = portée au sol : la distance au sol n'est jamais plus courte (Iso.ToGround).
        using CrowdQuery crowd = CrowdIndex.Near(GlobalPosition, _coneRange + Enemy.LargestPartRadius);
        foreach (Node node in crowd.Targets)
        {
            if (node is not Enemy enemy || !IsInstanceValid(enemy) || !enemy.IsActive || enemy.IsDying)
                continue;

            // Cône posé au sol : portée et ouverture mesurées au sol, comme l'éventail dessiné.
            Vector2 toEnemy = Iso.ToGround(enemy.GlobalPosition - GlobalPosition);
            float dist = toEnemy.Length();
            if (dist - enemy.StrikeMargin > _coneRange || dist <= 0.001f)
                continue;

            Vector2 dirToEnemy = toEnemy / dist;
            if (groundFacing.Dot(dirToEnemy) < dotThreshold)
                continue;

            float hitDamage = ResolveHitDamage(enemy, damage, false);
            bool showImpact = enemy.TakeContinuousDamage(hitDamage, delta, _coneContext);
            OnAttackHit(enemy, hitDamage, damage, false, _coneWeapon, showImpact: showImpact, context: _coneContext);
        }
    }

    private void UpdateConeVisual(float elapsed)
    {
        float progress = Mathf.Clamp(elapsed / _coneDuration, 0f, 1f);
        float currentAngleDeg = Mathf.Lerp(_coneAngleStart, _coneAngleEnd, progress);
        _attackFx.UpdateCone(_coneWeapon?.Base, _facingDirection, _coneRange, Mathf.DegToRad(currentAngleDeg * 0.5f), _coneWaves);
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
        int chainTargets = Mathf.Max(1, FractionalCount.Roll(GetWeaponStat("chain_targets"), RunRandom.Combat.Randf()));
        float chainRange = GetWeaponStat("chain_range");
        float chainFalloff = GetWeaponStat("chain_damage_falloff");

        // Premier hit : ennemi le plus proche (melee)
        System.Collections.Generic.List<Enemy> enemies = FindEnemiesInArc(range, 360f);
        if (enemies.Count == 0)
            return;

        Enemy firstTarget = enemies[0];
        float baseDamage = ComputeBaseAttackDamage();
        Vector2 attackDir = (firstTarget.GlobalPosition - GlobalPosition).Normalized();
        PlayAttackFeedback(isMelee: true, attackDir);

        bool isCrit = _critChance > 0f && RunRandom.Combat.Randf() < _critChance;
        float currentDamage = isCrit ? baseDamage * _critMultiplier : baseDamage;
        AttackContext context = BeginAttack(_equippedWeapon, currentDamage);
        float firstDamage = ResolveHitDamage(firstTarget, currentDamage, isCrit);
        firstTarget.TakeDamage(firstDamage, isCrit, source: context);
        OnAttackHit(firstTarget, firstDamage, currentDamage, isCrit, _equippedWeapon, context: context);
        // Le premier maillon part du joueur (plan 27 V2c) : on voit d'où vient la chaîne.
        SpawnChainVisual(GlobalPosition, firstTarget.GlobalPosition);

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
            OnAttackHit(nextTarget, linkDamage, currentDamage, false, _equippedWeapon, context: context);
            current = nextTarget;
        }
    }

    private Enemy FindNearestEnemyExcluding(Vector2 from, float maxRange, HashSet<ulong> excludeIds)
    {
        using CrowdQuery crowd = CrowdIndex.Near(from, maxRange);
        Enemy nearest = null;
        float nearestDist = maxRange;

        foreach (Node node in crowd.Targets)
        {
            if (node is Enemy { IsActive: true, IsDying: false } enemy && IsInstanceValid(enemy))
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
        _defenseFeedback.Healed(GlobalPosition, result.HpRestored);
        return result;
    }

    /// <param name="from">Position de ce qui frappe, quand elle est connue : la vignette de blessure se tourne vers elle.</param>
    public PlayerDamageResult TakeDamage(float damage, Vector2? from = null)
    {
#if TOOLS
        if (IsGodMode)
            return default;
#endif
        if (_currentHp <= 0 || Mobility.IsInvulnerable || _defense.IsInvulnerable)
            return default;

        if (_objectMilestones != null && _objectMilestones.Ignores(damage, EffectiveMaxHp))
        {
            ShowIgnoredHit();
            return default;
        }

        float armor = _armor * (_objectMilestones?.ArmorMultiplier(Mobility) ?? 1f);
        PlayerDefense.Outcome outcome = _defense.Absorb(damage, armor);
        Vector2 direction = from is Vector2 source && source != GlobalPosition ? (source - GlobalPosition).Normalized() : Vector2.Zero;
        _defenseFeedback.ArmorAbsorbed(GlobalPosition, damage, outcome.Reduced, direction);

        if (outcome.ShieldAbsorbed)
        {
            EmitShield();
            Flash(ShieldFlashColor, outcome.ShieldBroke);
            if (outcome.ShieldBroke)
            {
                _defenseFeedback.ShieldBroken(GlobalPosition);
                _objectMilestones?.OnShieldBroken();
            }
            PlayerDamageResult shieldResult = new(GetInstanceId(), PlayerDamageKind.Combat, _currentHp, 0f, false, true, true);
            _eventBus.PublishPlayerDamage(shieldResult);
            return shieldResult;
        }

        return LoseHp(outcome.HpDamage, PlayerDamageKind.Combat, direction);
    }

    /// <summary>Le Néant consume : ni bouclier, ni armure, ni invulnérabilité ne l'arrêtent.</summary>
    public PlayerDamageResult TakeErasureDamage(float damage)
    {
#if TOOLS
        if (IsGodMode)
            return default;
#endif
        if (_currentHp <= 0)
            return default;
        return LoseHp(damage, PlayerDamageKind.Erasure);
    }

    private PlayerDamageResult LoseHp(float damage, PlayerDamageKind kind, Vector2 fromDirection = default)
    {
        float before = _currentHp;
        _currentHp -= damage;
        PlayerDamageResult result = new(GetInstanceId(), kind, before,
            Mathf.Clamp(damage, 0f, before), _currentHp <= 0f, false, true) { FromDirection = fromDirection };
        // Le Néant consume par tranches (toutes les 0,5 s) : son retour est la vignette pâle (HurtVignette), pas le
        // paquet d'une blessure, qui secouait l'écran deux fois par seconde et bloquait le dash (plan 27 V3d).
        if (kind != PlayerDamageKind.Erasure)
        {
            Mobility.Hurt(Mobility.Config.HurtRecoverySeconds);
            ShowHurt(result.HpLost);
            if (_hasSprite)
                _hurtAnimTimer = Mobility.Config.HurtRecoverySeconds;
        }

        _eventBus.EmitSignal(EventBus.SignalName.PlayerDamaged, _currentHp, EffectiveMaxHp);

#if TOOLS
        if (result.Fatal && SurviveFatalHitsForTests)
            _currentHp = EffectiveMaxHp;
        else
#endif
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
        // Bouclier de nouveau plein : un éclat bref le dit (plan 27 V3c).
        bool shieldFull = _defense.MaxShield > 0f && _defense.Shield >= _defense.MaxShield;
        if (shieldFull && !_shieldWasFull)
            _defenseFeedback.ShieldFull(GlobalPosition);
        _shieldWasFull = shieldFull;

        // Clignotement pendant l'invulnérabilité qui suit un coup : la fenêtre se lit sans interface.
        bool hidden = _defense.IsInvulnerable && Mathf.PosMod(_defense.InvulnerableTimer, 0.12f) < 0.06f;
        if (hidden == _blinkHidden || !_hasSprite)
            return;
        _blinkHidden = hidden;
        _sprite.Modulate = hidden ? new Color(1f, 1f, 1f, 0.4f) : Colors.White;
    }

#if TOOLS
    /// <summary>
    /// Mesures de survie (RunObservation --mortal) : un coup fatal remet les PV au maximum au lieu de finir la run ; le
    /// résultat publié reste fatal, pour être compté.
    /// </summary>
    private bool _surviveFatalHitsForTests;
    internal bool SurviveFatalHitsForTests
    {
        get => _surviveFatalHitsForTests;
        set { DevelopmentMode.RequireTestAccess(); _surviveFatalHitsForTests = value; }
    }

    /// <summary>Bancs de régression : les coups reçus se mesurent sur les PV, sans bouclier ni invulnérabilité.</summary>
    internal void DisableDefenseForTests()
    {
        DevelopmentMode.RequireTestAccess();
        _defense.Disable();
        EmitShield();
    }
#endif

    private void EmitShield() =>
        _eventBus?.EmitSignal(EventBus.SignalName.PlayerShieldChanged, _defense.Shield, _defense.MaxShield);

    // --- Debuffs ---

    private float _slowTimer;
    private float _slowFactor = 1f;
    private readonly PlayerStatusVisual _statusVisual = new();
    private readonly PlayerDefenseFeedback _defenseFeedback = new();
    private bool _shieldWasFull = true;

    /// <summary>La toile de la Tisseuse ralentit le joueur, ou cesse de le ralentir (icône de la jauge, plan 27 V3b).</summary>
    public event System.Action<bool> WebbedChanged;

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
        HealingResult healed = Heal(Mathf.Min(_lifestealPending, cap));
        _lifestealPending = 0f;
        // À vie pleine, rien n'est rendu : l'icône de la Paille ne s'élève pas pour rien (plan 27 V3c).
        if (healed.HpRestored > 0f)
            ObjectProcs?.Show("lifesteal");
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

    /// <summary>
    /// Blessure (plan 27 V3a) : éclair rouge propre au joueur, secousse qui grandit avec la part des PV perdus, chiffre
    /// des dégâts reçus (§72). Réglages refusés : éclair clair et secousse moyenne d'avant.
    /// </summary>
    private void ShowHurt(float lost)
    {
        PlayerFeedbackConfig feedback = PlayerFeedbackConfig.Get();
        if (feedback == null)
        {
            Flash(HurtFlashColor, true);
            return;
        }
        Flash(feedback.HurtFlashColor, false, feedback.HurtFlashSeconds);
        float share = lost / Mathf.Max(EffectiveMaxHp, 1f);
        Combat.ScreenShake.Instance?.AddTrauma(Mathf.Min(1f, feedback.HurtShakeTrauma + feedback.HurtShakeTraumaPerMaxHp * share));
        if (lost >= 0.5f)
            Combat.CombatPools.Instance?.ShowReceivedNumber(GlobalPosition + ReceivedNumberOffset, lost, feedback.NumberColor);
    }

    private void Flash(Color color, bool shake, float seconds = 0.2f)
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
                1.0f, 0.0f, seconds
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

        AttackPatternKind pattern = _equippedWeapon.AttackPattern;

        // Sustained cone : effet spécial persistant (ex: last_broadcast)
        WeaponSpecialEffect specialEffect = _equippedWeapon.SpecialEffect;
        if (specialEffect?.Kind == SpecialEffectKind.SustainedCone)
        {
            if (!_isConeActive)
                ActivateSustainedCone(specialEffect);
            return;
        }

        // Orbital : pas d'attaque par timer, géré dans _PhysicsProcess
        if (pattern == AttackPatternKind.Orbital)
        {
            SetupOrbitalWeapon(_equippedWeapon);
            return;
        }

        PerformDiscreteAttack(_equippedWeapon.Category, pattern);
        _objectMilestones?.CountAttack(_equippedWeapon);
    }

    /// <summary>Attaque ponctuelle de l'arme active : chaîne, mêlée ou tir (ni cône continu ni orbite).</summary>
    private void PerformDiscreteAttack(WeaponCategory category, AttackPatternKind pattern)
    {
        // Chain melee : attaque spéciale avec rebond
        if (pattern == AttackPatternKind.Chain)
            PerformChainAttack();
        else if (category == WeaponCategory.Melee)
            PerformMeleeAttack(pattern);
        else
            PerformRangedAttack(pattern);
    }

    /// <summary>
    /// Nombre propre de l'arme active : tirs, frappes de mêlée ou ondes du cône (plan 21 G6a). Stat entière fractionnaire
    /// (plan 23 R4) : 1,5, c'est un coup et une chance sur deux d'un second.
    /// </summary>
    private int RollOwnCount() => Mathf.Max(1, FractionalCount.Roll(GetWeaponStat("projectile_count"), RunRandom.Combat.Randf()));

    private void PerformRangedAttack(AttackPatternKind pattern)
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
        float projectileSpeed = GetWeaponStat("projectile_speed");
        int totalPierce = FractionalCount.Roll(GetWeaponStat("projectile_pierce") + _projectilePierce, RunRandom.Combat.Randf());
        Vector2 baseDirection = (targets[0].GlobalPosition - GlobalPosition).Normalized();
        PlayAttackFeedback(isMelee: false, baseDirection);
        float spreadAngle = GetWeaponStat("spread_angle");

        if (pattern == AttackPatternKind.Burst)
        {
            // Salve : les projectiles en plus élargissent l'éventail, ou partent chacun vers leur cible au palier.
            SpawnBurstProjectiles(spreadExtras ? ownCount : totalProjectiles, spreadAngle, baseDirection, baseDamage, projectileSpeed, totalPierce);
            if (spreadExtras)
                for (int i = 0; i < extraCount; i++)
                    SpawnAimedProjectile(targets[(ownCount + i) % targets.Count], false, 0f, baseDamage, projectileSpeed, range, totalPierce,
                        launchDelay: VolleyDelay(ownCount + i, targets.Count, totalProjectiles));
            return;
        }

        float homingStrength = GetWeaponStat("homing_strength");
        bool isHoming = pattern == AttackPatternKind.Homing || homingStrength > 0f;
        int aimedCount = spreadExtras ? totalProjectiles : ownCount;
        for (int i = 0; i < totalProjectiles; i++)
        {
            bool fanned = i >= ownCount && !spreadExtras;
            // Sans le palier du Papier carbone, un projectile en plus part en éventail autour du tir principal.
            float offset = fanned ? ExtraFanOffset(i - ownCount, extraCount, spreadAngle) : 0f;
            Node2D target = fanned ? targets[0] : targets[i % targets.Count];
            float launchDelay = fanned ? 0f : VolleyDelay(i, targets.Count, aimedCount);
            Projectile proj = SpawnAimedProjectile(target, isHoming, homingStrength, baseDamage, projectileSpeed, range, totalPierce, offset,
                launchDelay);

            // Ground fire : configurer le projectile pour spawner une zone au sol à l'impact
            WeaponSpecialEffect se = _equippedWeapon?.SpecialEffect;
            if (proj != null && se?.Kind == SpecialEffectKind.GroundFire)
                proj.SetGroundFire(se.Get(SpecialEffectParam.GroundDamage), StatusDuration(se.Get(SpecialEffectParam.GroundDuration)),
                    ZoneScale(se.Get(SpecialEffectParam.GroundRadius)), StatusDuration(se.Get(SpecialEffectParam.GroundBurnSeconds)));
        }
    }

    /// <summary>Un tir vers <paramref name="target"/>, dévié de <paramref name="offsetDegrees"/>, guidé si l'arme l'est.</summary>
    private Projectile SpawnAimedProjectile(Node2D target, bool isHoming, float homingStrength, float baseDamage, float speed,
        float range, int pierce, float offsetDegrees = 0f, float launchDelay = 0f)
    {
        Vector2 direction = (target.GlobalPosition - GlobalPosition).Normalized().Rotated(Mathf.DegToRad(offsetDegrees));
        bool isCrit = _critChance > 0f && RunRandom.Combat.Randf() < _critChance;
        Projectile proj = SpawnProjectile(direction, ProjectileDamage(baseDamage, isCrit), speed, range, pierce, isCrit, launchDelay);
        if (proj != null && launchDelay > 0f && offsetDegrees == 0f)
            proj.AimAtDeparture(target);
        if (proj != null && isHoming)
            proj.SetHoming(homingStrength > 0f ? homingStrength : 0.8f, target);
        return proj;
    }

    /// <summary>
    /// Rafale (plan 21 G6g, DECISIONS §59) : les tirs visés se répartissent sur les cibles ; celui qui retombe sur une
    /// cible déjà visée part un écart plus tard à chaque tour, pour que chaque projectile se voie au lieu de se superposer.
    /// </summary>
    private static float VolleyDelay(int shotIndex, int targetCount, int shotCount)
    {
        int round = shotIndex / Mathf.Max(1, targetCount);
        if (round == 0)
            return 0f;
        int lastRound = (shotCount - 1) / Mathf.Max(1, targetCount);
        WeaponVisualConfig config = WeaponVisualConfig.Load();
        return round * Mathf.Min(config.VolleyInterval, config.VolleyMaxSpan / lastRound);
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

    private void PerformMeleeAttack(AttackPatternKind pattern)
    {
        // Onde circulaire : son rayon est une zone. Arc : la portée donne l'allonge, la zone l'ouverture.
        float range = pattern == AttackPatternKind.Circular ? ZoneScale(GetEffectiveWeaponRange()) : GetEffectiveWeaponRange();
        float arcAngle = pattern switch
        {
            AttackPatternKind.Circular => 360f,
            AttackPatternKind.Linear => 60f,
            _ => Mathf.Min(360f, ZoneScale(GetWeaponStat("arc_angle")))
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
            ? Mathf.Clamp(GetWeaponStat("spread_angle"), 0f, 120f)
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

            bool hasCrit = clampedCritChance > 0f && RunRandom.Combat.Randf() < GetCombinedProcChance(clampedCritChance, hitCount);
            float rawDamage = baseDamage * hitCount * critDamageFactor;
            float totalDamage = ResolveHitDamage(enemy, rawDamage, hasCrit);
            AttackContext hitContext = context with { ReferenceDamage = totalDamage };
            enemy.TakeDamage(totalDamage, hasCrit, source: hitContext);
            OnAttackHit(enemy, totalDamage, rawDamage, hasCrit, _equippedWeapon, triggerCount: hitCount, context: hitContext);
        }
        if (_objectMilestones?.HasZoneEcho == true)
            QueueMeleeEchoes(attackDirection, range, arcAngle, strikeCount, startOffset, step, baseDamage * critDamageFactor, context);
    }

    private void SpawnMeleeSlashVisuals(Vector2 baseDirection, float range, float arcAngle, int strikeCount, float spreadAngle)
    {
        const int maxSlashVisuals = 7;
        int visualCount = Mathf.Min(strikeCount, maxSlashVisuals);
        // Onde en cercle (plan 21 G6g) : tourner l'onde ne la distingue pas ; chaque frappe en plus repart un peu après.
        if (arcAngle >= 359f)
        {
            SpawnSlashEffect(baseDirection, range, arcAngle);
            float interval = WeaponVisualConfig.Load().WaveInterval;
            for (int visualIndex = 1; visualIndex < visualCount; visualIndex++)
                _attackFx.PlayMeleeLater(_equippedWeapon?.Base, baseDirection, range, arcAngle, WeaponSizeScale, visualIndex * interval);
            return;
        }
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
            bool isCrit = _critChance > 0f && RunRandom.Combat.Randf() < _critChance;
            SpawnProjectile(direction, ProjectileDamage(baseDamage, isCrit), projectileSpeed, range, pierce, isCrit);
        }
    }

    private float ProjectileDamage(float baseDamage, bool isCrit) => isCrit ? baseDamage * _critMultiplier : baseDamage;

    private Projectile SpawnProjectile(Vector2 direction, float damage, float speed, float range, int pierce, bool isCrit,
        float launchDelay = 0f)
    {
        Projectile projectile = CombatPools.Instance?.TakePlayerProjectile();
        projectile?.Launch(GlobalPosition, direction, damage, speed, Mathf.Clamp(range / Mathf.Max(speed, 1f), 0.2f, 4f),
            pierce, isCrit, this, _equippedWeapon?.Base, _equippedWeapon, context: _launchContext with { ReferenceDamage = damage },
            pierceDamageRamp: PierceDamageRamp, sizeScale: WeaponSizeScale, launchDelay: launchDelay);
        return projectile;
    }

    private System.Collections.Generic.List<Node2D> FindNearestEnemies(int count, float maxRange)
    {
        using CrowdQuery crowd = CrowdIndex.Near(GlobalPosition, maxRange);
        System.Collections.Generic.List<(Node2D enemy, float dist)> inRange = new();

        foreach (Node node in crowd.Targets)
        {
            // Une cible qui n'est pas une créature (l'Indicible) reste admise.
            if (node is not Node2D enemy || enemy is Enemy { IsActive: false } or Enemy { IsDying: true })
                continue;
            float dist = GlobalPosition.DistanceTo(enemy.GlobalPosition);
            if (dist < maxRange)
                inRange.Add((enemy, dist));
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
        using CrowdQuery crowd = CrowdIndex.Near(GlobalPosition, maxRange + Enemy.LargestPartRadius);
        System.Collections.Generic.List<(Enemy enemy, float dist, Vector2 dir)> candidates = new();

        foreach (Node node in crowd.Targets)
        {
            if (node is not Enemy { IsActive: true, IsDying: false } enemy)
                continue;

            Vector2 toEnemy = enemy.GlobalPosition - GlobalPosition;
            float dist = toEnemy.Length();
            if (dist - enemy.StrikeMargin > maxRange || dist <= 0.001f)
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
        float weaponDamage = weapon?.GetStat("damage") ?? AttackDamage;
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
        float weaponRange = weapon?.GetStat("range") ?? AttackRange;
        return weaponRange * PersonalRangeFactor(weapon) * _attackRangeMultiplier;
    }

    /// <summary>
    /// Portée propre au personnage, rapportée à 300. Elle allonge la mêlée mais ne la raccourcit plus : à ×0,83, la
    /// Forgeuse frappait à 46 px avec le Parcmètre, au contact des créatures (plan 24 lot L3).
    /// </summary>
    private float PersonalRangeFactor(WeaponInstance weapon)
    {
        float factor = AttackRange / 300f;
        return weapon?.Category == WeaponCategory.Melee ? Mathf.Max(1f, factor) : factor;
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
        "attack_speed" => AttackSpeed * weapon.GetStat("attack_speed") * _attackSpeedMultiplier,
        "range" => weapon.AttackPattern == AttackPatternKind.Circular ? ZoneScale(GetEffectiveWeaponRange(weapon)) : GetEffectiveWeaponRange(weapon),
        "arc_angle" => Mathf.Min(360f, ZoneScale(weapon.GetStat("arc_angle"))),
        "cone_angle_end" => Mathf.Min(180f, ZoneScale(weapon.GetStat("cone_angle_end"))),
        "projectile_pierce" => weapon.GetStat("projectile_pierce") + _projectilePierce,
        _ => weapon.GetStat(key),
    };

    private float GetWeaponStat(string key) => _equippedWeapon?.GetStat(key) ?? WeaponContract.StatDefault(key);

    private void PlayAttackSound()
    {
        PlayWeaponSound(_equippedWeapon);
    }

    private static void PlayWeaponSound(WeaponInstance weapon)
    {
        string attackAudio = weapon?.Base.AttackAudio;
        if (!string.IsNullOrEmpty(attackAudio))
            Infrastructure.AudioManager.Play(attackAudio, 0.04f);
    }

    private void PlayAttackFeedback(bool isMelee, Vector2 direction)
    {
        PlayAttackSound();
        if (_visual == null)
            return;

        _facingDirection = direction.Normalized();
        _attackFx.PlayRecoil(isMelee);
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
            float weaponAtkSpd = weapon.GetStat("attack_speed");
            float attacksPerSecond = AttackSpeed * weaponAtkSpd * mult;
            _weaponTimers[i].WaitTime = 1.0f / Mathf.Max(0.05f, attacksPerSecond);
        }
    }

}
