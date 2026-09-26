using System.Collections.Generic;
using System.Linq;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.Core;

/// <summary>
/// Souvenir passif porté : son niveau et l'effet cumulé de ses améliorations. Une amélioration ajoute l'écart
/// entre deux niveaux de la table, multiplié par le gain de sa rareté (plan 17 lot 1B).
/// </summary>
public class ActivePassiveSouvenir
{
	public string Id { get; }
	public PassiveSouvenirData Data { get; }
	public int Level { get; private set; }
	/// <summary>Effet total appliqué au joueur (multiplicateur ou valeur ajoutée, selon le passif).</summary>
	public float Modifier { get; private set; }
	public bool IsMaxLevel => Level >= Data.MaxLevel;

	public ActivePassiveSouvenir(PassiveSouvenirData data)
	{
		Data = data;
		Id = data.Id;
		Level = 1;
		Modifier = TableValue(1);
	}

	/// <summary>Effet après une amélioration de <paramref name="levels"/> niveaux au gain <paramref name="gain"/>.</summary>
	public float PreviewModifier(float gain, int levels)
	{
		int target = Mathf.Min(Data.MaxLevel, Level + levels);
		return Modifier + (TableValue(target) - TableValue(Level)) * gain;
	}

	public bool Upgrade(float gain, int levels)
	{
		if (IsMaxLevel)
			return false;
		Modifier = PreviewModifier(gain, levels);
		Level = Mathf.Min(Data.MaxLevel, Level + levels);
		return true;
	}

	private float TableValue(int level)
	{
		if (Data.PerLevel == null || Data.PerLevel.Length == 0)
			return 0f;
		return Data.PerLevel[Mathf.Clamp(level - 1, 0, Data.PerLevel.Length - 1)];
	}
}

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

    // Passive Souvenir inventory (max 4, from level-up)
    public const int MaxPassiveSlots = 4;
    private readonly List<ActivePassiveSouvenir> _passiveSlots = new();
    // Dégâts infligés par arme depuis le début de la run (pause, plan 17 lot 1C).
    private readonly Dictionary<string, float> _damageDealtByWeapon = new();

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
    private int _extraProjectiles;
    private float _aoeMultiplier = 1f;
    private float _attackRangeMultiplier = 1f;
    private float _bonusRegenRate;
    private float _armor;
    private float _critChance;
    private float _critMultiplier = 2f;
    private int _projectilePierce;
    private float _xpMagnetMultiplier = 1f;
    private float _luckBonus;

    // Complex perk effects
    private float _vampirismPercent;
    private float _berserkerThreshold;
    private float _berserkerDamageMult = 1f;
    private float _thornsPercent;
    private float _executionThreshold;
    private float _dodgeChance;
    private bool _secondWindAvailable;
    private float _secondWindHealPercent;
    private float _igniteChance;
    private float _igniteDamage;
    private float _igniteDuration;
    private float _ricochetChance;
    private float _ricochetRange = 120f;

    // Kill speed buff
    private float _killSpeedBonusPerKill;
    private float _killSpeedDuration;
    private int _killSpeedMaxStacks;
    private int _killSpeedActiveStacks;
    private float _killSpeedTimer;

    // Weapon special effect tracking (per-weapon hit counters)
    private readonly System.Collections.Generic.Dictionary<string, int> _weaponHitCounters = new();

    // Orbital weapon system
    private readonly System.Collections.Generic.List<Node2D> _orbitalProjectiles = new();
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
    private PerkManager _perkManager;

    public float CurrentHp => _currentHp;
    public float EffectiveMaxHp => MaxHp + _bonusMaxHp;
    public float EffectiveAttackRange => AttackRange * _attackRangeMultiplier;
    public float AttackRangeMultiplier => _attackRangeMultiplier;
    // V2: StructureHpMultiplier, CraftSpeedMultiplier, RepairSpeedMultiplier retires
    public int ProjectilePierce => _projectilePierce;
    public float XpMagnetMultiplier => _xpMagnetMultiplier;
    public float CritChance => _critChance;
    public float CritMultiplier => _critMultiplier;
    public bool IsDead => _isDead;
    public string CharacterId => _characterId;
    public WeaponInstance EquippedWeapon => _weaponSlots.Count > 0 ? _weaponSlots[0] : null;
    public IReadOnlyList<WeaponInstance> WeaponSlots => _weaponSlots;
    public IReadOnlyList<ActivePassiveSouvenir> PassiveSlots => _passiveSlots;

    // Stat getters pour le menu pause
    public float DamageMultiplier => _damageMultiplier;
    public float SpeedMultiplier => _speedMultiplier;
    public float AttackSpeedMultiplier => _attackSpeedMultiplier;
    public float Armor => _armor;
    public float BonusRegenRate => _bonusRegenRate;
    public float AoeMultiplier => _aoeMultiplier;
    public float VampirismPercent => _vampirismPercent;
    public float DodgeChance => _dodgeChance;
    public float ThornsPercent => _thornsPercent;
    public int ExtraProjectiles => _extraProjectiles;
    public float IgniteChance => _igniteChance;
    public float RicochetChance => _ricochetChance;
    public float LuckBonus => _luckBonus;

    private const float ShadowWidth = 22f;

    public override void _Ready()
    {
        _currentHp = MaxHp;
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
        Mobility = new PlayerMobility(MobilityConfig.Load());
        _mobilityFeedback = new MobilityFeedback { Name = "MobilityFeedback" };
        AddChild(_mobilityFeedback);
        CacheWorldSetup();

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EnemyKilled += OnEnemyKilled;
        _eventBus.GameStateChanged += OnMovementGameStateChanged;
        _eventBus.PlayerErasurePhaseChanged += OnErasurePhaseChanged;
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _groupCache = GetNode<GroupCache>("/root/GroupCache");
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.EnemyKilled -= OnEnemyKilled;
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
        if (_isConeActive && removed.Base.SpecialEffect?.Type == "sustained_cone")
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

    // --- Passive Souvenirs ---

    /// <summary>Ajoute un passif, ou l'améliore : écart de la table × <paramref name="gain"/>, sur <paramref name="levels"/> niveaux.</summary>
    public bool AddOrUpgradePassive(string passiveId, float gain, int levels)
    {
        PassiveSouvenirData data = PassiveSouvenirDataLoader.Get(passiveId);
        if (data == null)
            return false;

        foreach (ActivePassiveSouvenir existing in _passiveSlots)
        {
            if (existing.Id == passiveId)
            {
                float prevMod = existing.Modifier;
                if (!existing.Upgrade(gain, levels))
                    return false;

                ApplyPassiveModifierDelta(data, prevMod, existing.Modifier);

                _eventBus?.EmitSignal(EventBus.SignalName.PassiveSouvenirUpgraded, passiveId, existing.Level);
                _eventBus?.EmitSignal(EventBus.SignalName.PassiveSouvenirSlotsChanged);

                GD.Print($"[Player] Passive upgraded: {data.Name} → level {existing.Level}/{data.MaxLevel}");
                return true;
            }
        }

        // Nouveau slot
        if (_passiveSlots.Count >= MaxPassiveSlots)
            return false;

        ActivePassiveSouvenir passive = new(data);
        _passiveSlots.Add(passive);

        ApplyPassiveModifier(data, passive.Modifier);

        int slotIndex = _passiveSlots.Count - 1;
        _eventBus?.EmitSignal(EventBus.SignalName.PassiveSouvenirAdded, passiveId, slotIndex);
        _eventBus?.EmitSignal(EventBus.SignalName.PassiveSouvenirSlotsChanged);

        GD.Print($"[Player] Passive added [{slotIndex}]: {data.Name} (level 1/{data.MaxLevel})");
        return true;
    }

    /// <summary>Vérifie si un passif donné est au max.</summary>
    public bool IsPassiveMaxLevel(string passiveId)
    {
        foreach (ActivePassiveSouvenir p in _passiveSlots)
        {
            if (p.Id == passiveId)
                return p.IsMaxLevel;
        }
        return false;
    }

    /// <summary>Retourne le niveau actuel d'un passif (0 si pas équipé).</summary>
    public int GetPassiveLevel(string passiveId)
    {
        foreach (ActivePassiveSouvenir p in _passiveSlots)
        {
            if (p.Id == passiveId)
                return p.Level;
        }
        return 0;
    }

    private void ApplyPassiveModifier(PassiveSouvenirData data, float value)
    {
        ApplyPerkModifier(data.Stat, value, data.ModifierType);
    }

    private void ApplyPassiveModifierDelta(PassiveSouvenirData data, float oldValue, float newValue)
    {
        if (data.ModifierType == "multiplicative")
        {
            // Undo old multiplier, apply new one
            if (oldValue > 0f)
                ApplyPerkModifier(data.Stat, newValue / oldValue, data.ModifierType);
        }
        else
        {
            // Additive: apply only the delta
            ApplyPerkModifier(data.Stat, newValue - oldValue, data.ModifierType);
        }
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
    public float GetDamageDealt(string weaponId) => _damageDealtByWeapon.GetValueOrDefault(weaponId);

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
        ProcessFootsteps(dt, dashMovement ? 0f : movementRate);
        ApplyRegen(dt);
        ProcessSlowDecay(dt);
        ProcessPoiExplore(dt);
        _interaction.Step(dt, !_isExploringPoi && !Mobility.IsDashing);
        ProcessKillSpeedDecay(dt);
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
            case "projectile_count":
                if (modifierType == "additive") _extraProjectiles += (int)value;
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
            case "armor":
                if (modifierType == "additive") _armor += value;
                break;
            case "crit_chance":
                if (modifierType == "additive") _critChance += value;
                break;
            case "crit_multiplier":
                if (modifierType == "additive") _critMultiplier += value;
                break;
            case "projectile_pierce":
                if (modifierType == "additive") _projectilePierce += (int)value;
                break;
            case "xp_magnet_radius":
                if (modifierType == "multiplicative") _xpMagnetMultiplier *= value;
                break;
            // V2: repair_speed retire
            case "luck":
                if (modifierType == "additive") _luckBonus += value;
                break;
        }
    }

    // --- Complex Perk Effects ---

    public void AddVampirism(float percentPerStack)
    {
        _vampirismPercent += percentPerStack;
    }

    public void AddBerserker(float hpThreshold, float damageMult)
    {
        _berserkerThreshold = hpThreshold;
        _berserkerDamageMult += (damageMult - 1f);
    }

    public void AddThorns(float percentPerStack)
    {
        _thornsPercent += percentPerStack;
    }

    public void AddExecution(float threshold)
    {
        _executionThreshold += threshold;
    }

    public void AddDodge(float chancePerStack)
    {
        _dodgeChance += chancePerStack;
    }

    public void SetSecondWind(float healPercent)
    {
        _secondWindAvailable = true;
        _secondWindHealPercent = healPercent;
    }

    public void AddIgnite(float chance, float damage, float duration)
    {
        _igniteChance += chance;
        _igniteDamage = Mathf.Max(_igniteDamage, damage);
        _igniteDuration = Mathf.Max(_igniteDuration, duration);
    }

    public void AddRicochet(float chance, float range)
    {
        _ricochetChance += chance;
        _ricochetRange = Mathf.Max(_ricochetRange, range);
    }

    public void AddKillSpeed(float bonusPerKill, float duration, int maxStacks)
    {
        _killSpeedBonusPerKill += (bonusPerKill - 1f);
        _killSpeedDuration = Mathf.Max(_killSpeedDuration, duration);
        _killSpeedMaxStacks = System.Math.Max(_killSpeedMaxStacks, maxStacks);
    }

    /// <summary>
    /// Called by projectiles and melee hits. Handles vampirism, ignite, execution, ricochet.
    /// </summary>
    public void OnProjectileHit(Enemy enemy, float damage, bool isCrit, bool isRicochet, WeaponInstance source)
    {
        OnAttackHit(enemy, damage, isCrit, isRicochet, source);
    }

    /// <summary>
    /// Effets d'un coup porté par <paramref name="source"/>. Les effets de perks (vampirisme, embrasement…) sont
    /// globaux ; ceux de l'arme (effet au contact, recul, effet spécial) restent ceux de l'arme qui a frappé.
    /// </summary>
    private void OnAttackHit(Enemy enemy, float damage, bool isCrit, bool isRicochet, WeaponInstance source, int triggerCount = 1)
    {
        if (_isDead)
            return;

        if (!IsInstanceValid(enemy) || enemy.IsQueuedForDeletion())
            return;

        _attackFx.PlayHit(source?.Base, enemy.GlobalPosition, isCrit);
        if (source != null)
            _damageDealtByWeapon[source.Id] = _damageDealtByWeapon.GetValueOrDefault(source.Id) + damage;
        int procRollCount = Mathf.Max(1, triggerCount);

        // Vampirism: heal % of damage dealt
        if (_vampirismPercent > 0f)
            Heal(damage * _vampirismPercent);

        // Ignite: chance to apply DOT
        if (_igniteChance > 0f && GD.Randf() < GetCombinedProcChance(_igniteChance, procRollCount))
            enemy.ApplyIgnite(_igniteDamage, _igniteDuration);

        // Execution: instant kill enemies below HP threshold
        if (_executionThreshold > 0f && !enemy.IsDying && enemy.HpRatio > 0f && enemy.HpRatio < _executionThreshold)
            enemy.Execute();

        // Ricochet: bounce to nearby enemy (only from original projectiles)
        if (!isRicochet && _ricochetChance > 0f && GD.Randf() < GetCombinedProcChance(_ricochetChance, procRollCount))
            SpawnRicochet(enemy, damage, isCrit, source);

        // --- Weapon on-hit effects ---
        WeaponOnHitEffect ohe = source?.Base.OnHitEffect;
        if (ohe != null)
        {
            switch (ohe.Type)
            {
                case "dot":
                    enemy.ApplyBleed(ohe.Damage, ohe.Duration);
                    break;
                case "slow":
                    enemy.ApplySlow(ohe.Value, ohe.Duration);
                    break;
                case "disorient":
                    enemy.ApplyDisorient(ohe.Duration);
                    break;
            }
        }

        // --- Weapon knockback ---
        float knockback = source?.GetStat("knockback", 0f) ?? 0f;
        if (knockback > 0f)
        {
            Vector2 knockDir = (enemy.GlobalPosition - GlobalPosition).Normalized();
            enemy.ApplyKnockback(knockDir, knockback);
        }

        // --- Weapon special effects ---
        WeaponSpecialEffect se = source?.Base.SpecialEffect;
        if (se != null)
            ProcessWeaponSpecialOnHit(se, enemy, damage, source);
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

    private void SpawnRicochet(Enemy sourceEnemy, float damage, bool isCrit, WeaponInstance source)
    {
        Godot.Collections.Array<Node> enemies = _groupCache.GetEnemies();
        Node2D bounceTarget = null;
        float nearestDist = _ricochetRange;

        foreach (Node node in enemies)
        {
            if (node is Node2D candidate && candidate != sourceEnemy && !candidate.IsQueuedForDeletion())
            {
                float dist = sourceEnemy.GlobalPosition.DistanceTo(candidate.GlobalPosition);
                if (dist < nearestDist)
                {
                    bounceTarget = candidate;
                    nearestDist = dist;
                }
            }
        }

        if (bounceTarget == null)
            return;

        Vector2 direction = (bounceTarget.GlobalPosition - sourceEnemy.GlobalPosition).Normalized();
        float speed = source?.GetStat("projectile_speed", 400f) ?? 400f;
        CombatPools.Instance?.TakePlayerProjectile().Launch(sourceEnemy.GlobalPosition, direction, damage * 0.75f, speed,
            Mathf.Clamp(_ricochetRange / Mathf.Max(speed, 1f), 0.2f, 2f), 0, isCrit, this, source?.Base, source, isRicochet: true);
    }

    // --- Weapon Special Effects ---

    private void ProcessWeaponSpecialOnHit(WeaponSpecialEffect se, Enemy enemy, float damage, WeaponInstance source)
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
                ulong enemyId = enemy.GetInstanceId();
                GetTree().CreateTimer(delay).Timeout += () =>
                {
                    // Réapplique les dégâts à la position d'origine (AoE fantôme)
                    Godot.Collections.Array<Node> enemies = _groupCache.GetEnemies();
                    foreach (Node node in enemies)
                    {
                        if (node is Enemy e && IsInstanceValid(e) && !e.IsDying)
                        {
                            if (e.GlobalPosition.DistanceTo(echoPos) < echoRadius)
                                e.TakeDamage(echoDamage);
                        }
                    }
                    SpawnEchoVisual(echoPos);
                };
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
                float duration = se.Params.TryGetValue("slow_duration", out float dur) ? dur : 0.5f;
                Vector2 impactPos = enemy.GlobalPosition;
                Godot.Collections.Array<Node> enemies = _groupCache.GetEnemies();
                foreach (Node node in enemies)
                {
                    if (node is Enemy e && IsInstanceValid(e) && !e.IsDying)
                    {
                        if (e.GlobalPosition.DistanceTo(impactPos) < radius)
                            e.ApplySlow(factor, duration);
                    }
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
                            e.TakeDamage(damage * 0.5f);
                    }
                }
                break;
            }
        }
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
        int orbitalCount = Mathf.Max(1, (int)weapon.GetStat("orbital_count", 3f));
        if (_orbitalWeapon == weapon && _orbitalProjectiles.Count == orbitalCount)
            return;

        ClearOrbitals();
        _orbitalWeapon = weapon;
        for (int i = 0; i < orbitalCount; i++)
        {
            Area2D orb = new() { Name = $"OrbitalOrb_{i}" };
            orb.CollisionLayer = 0;
            orb.CollisionMask = 2;

            CollisionShape2D shape = new();
            CircleShape2D circle = new() { Radius = ZoneScale(8f) };
            shape.Shape = circle;
            orb.AddChild(shape);
            orb.AddChild(PlayerAttackFx.CreateOrbitalVisual());

            orb.BodyEntered += (Node2D body) =>
            {
                if (body is Enemy enemy && !enemy.IsDying && IsInstanceValid(enemy) && _orbitalWeapon != null)
                {
                    float damage = ComputeBaseAttackDamage(_orbitalWeapon);
                    enemy.TakeDamage(damage);
                    OnAttackHit(enemy, damage, false, false, _orbitalWeapon);
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
        float orbitalRadius = GetEffectiveWeaponRange(_orbitalWeapon);
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
        _coneBaseDamage = ComputeBaseAttackDamage();
        _coneWeapon = _equippedWeapon;

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
            if (node is not Enemy enemy || enemy.IsDying || !IsInstanceValid(enemy))
                continue;

            // Cône posé au sol : portée et ouverture mesurées au sol, comme l'éventail dessiné.
            Vector2 toEnemy = Iso.ToGround(enemy.GlobalPosition - GlobalPosition);
            float dist = toEnemy.Length();
            if (dist > _coneRange || dist <= 0.001f)
                continue;

            Vector2 dirToEnemy = toEnemy / dist;
            if (groundFacing.Dot(dirToEnemy) < dotThreshold)
                continue;

            enemy.TakeDamage(damage);
            OnAttackHit(enemy, damage, false, false, _coneWeapon);
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
        float baseDamage = ComputeBaseAttackDamage();
        int chainTargets = Mathf.Max(1, (int)GetWeaponStat("chain_targets", 2f));
        float chainRange = GetWeaponStat("chain_range", 100f);
        float chainFalloff = GetWeaponStat("chain_damage_falloff", 0.8f);

        // Premier hit : ennemi le plus proche (melee)
        System.Collections.Generic.List<Enemy> enemies = FindEnemiesInArc(range, 360f);
        if (enemies.Count == 0)
            return;

        Enemy firstTarget = enemies[0];
        Vector2 attackDir = (firstTarget.GlobalPosition - GlobalPosition).Normalized();
        PlayAttackFeedback(isMelee: true, attackDir);

        bool isCrit = _critChance > 0f && GD.Randf() < _critChance;
        float currentDamage = isCrit ? baseDamage * _critMultiplier : baseDamage;
        firstTarget.TakeDamage(currentDamage, isCrit);
        OnAttackHit(firstTarget, currentDamage, isCrit, false, _equippedWeapon);

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
            nextTarget.TakeDamage(currentDamage);
            OnAttackHit(nextTarget, currentDamage, false, false, _equippedWeapon);
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

    /// <summary>Ramène les PV courants à <paramref name="max"/> au plus, sans dégât ni événement (levée d'un Oubli).</summary>
    public void CapCurrentHp(float max) => _currentHp = Mathf.Min(_currentHp, max);

    public void Heal(float amount)
    {
        if (_isDead || amount <= 0)
            return;

        _currentHp = Mathf.Min(_currentHp + amount, EffectiveMaxHp);

        _eventBus.EmitSignal(EventBus.SignalName.PlayerDamaged, _currentHp, EffectiveMaxHp);
    }

    public void TakeDamage(float damage)
    {
        if (_currentHp <= 0 || IsGodMode || Mobility.IsInvulnerable)
            return;

        // Dodge check
        if (_dodgeChance > 0f && GD.Randf() < _dodgeChance)
        {
            GD.Print("[Player] Dodged!");
            return;
        }

        float reduced = Mathf.Max(1f, damage - _armor);
        _currentHp -= reduced;
        Mobility.Hurt(Mobility.Config.HurtRecoverySeconds);
        HitFlash();
        if (_hasSprite)
            _hurtAnimTimer = Mobility.Config.HurtRecoverySeconds;

        _eventBus.EmitSignal(EventBus.SignalName.PlayerDamaged, _currentHp, EffectiveMaxHp);

        // Thorns: reflect damage to nearest enemy
        if (_thornsPercent > 0f)
            ApplyThorns(reduced);

        if (_currentHp <= 0)
        {
            _currentHp = 0;
            Die();
        }
    }

    private void ApplyThorns(float damageTaken)
    {
        float reflectedDamage = damageTaken * _thornsPercent;
        if (reflectedDamage <= 0f)
            return;

        Godot.Collections.Array<Node> enemies = _groupCache.GetEnemies();
        float nearestDist = 60f;
        Enemy nearestEnemy = null;

        foreach (Node node in enemies)
        {
            if (node is Enemy enemy && !enemy.IsDying)
            {
                float dist = GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (dist < nearestDist)
                {
                    nearestEnemy = enemy;
                    nearestDist = dist;
                }
            }
        }

        nearestEnemy?.TakeDamage(reflectedDamage);
    }

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

        string key = GetCurrentTerrain() switch
        {
            TerrainType.Water    => "sfx_pas_eau",
            TerrainType.Concrete => "sfx_pas_beton",
            _                    => "sfx_pas_herbe",
        };
        Infrastructure.AudioManager.Play(key, 0.05f, -4f);
    }

    // --- POI Exploration ---

    private bool TryStartPoiExplore()
    {
        PointOfInterest nearest = FindNearestPoi();
        if (nearest == null || !nearest.CanInteract)
            return false;

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

        List<ResolvedLoot> loots = LootRewards.Resolve(LootResolver.Roll(poi.LootTableId, poi.LootRolls), _perkManager);
        for (int i = 0; i < loots.Count; i++)
        {
            LootRewards.Apply(loots[i], this, _eventBus, poi.GlobalPosition);
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

    /// <summary>Services du butin : écran de roulette des coffres et tirage des perks.</summary>
    public void ConfigureLoot(UI.ChestLootScreen lootScreen, PerkManager perkManager)
    {
        _perkManager = perkManager;
        _interaction.Configure(lootScreen, perkManager);
    }

    /// <summary>Texte flottant montrant le loot obtenu, empilé verticalement.</summary>
    private void SpawnLootPopup(string text, Color color, Vector2 worldPos, int stackIndex)
    {
        Label label = new()
        {
            Text = $"+ {text}",
            HorizontalAlignment = HorizontalAlignment.Center,
            GlobalPosition = worldPos + new Vector2(-40, -25 - stackIndex * 16)
        };
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", 12);
        label.Size = new Vector2(80, 16);

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
        // Second Wind: revive once per run
        if (_secondWindAvailable)
        {
            _secondWindAvailable = false;
            _currentHp = EffectiveMaxHp * _secondWindHealPercent;
            _eventBus.EmitSignal(EventBus.SignalName.PlayerDamaged, _currentHp, EffectiveMaxHp);
            HitFlash();
            GD.Print($"[Player] Second Wind! Revived at {_currentHp:F0} HP");
            return;
        }

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
        {
            // Dissolution via le shader unifié (pas de swap)
            _spriteMaterial.SetShaderParameter("outline_enabled", false);

            Tween tween = CreateTween();
            tween.TweenMethod(
                Callable.From((float v) => _spriteMaterial.SetShaderParameter("dissolve_amount", v)),
                0.0f, 1.0f, 0.8f
            );
            tween.TweenCallback(Callable.From(() => GetTree().Paused = true));
        }
        else
        {
            // Fallback Polygon2D
            Tween tween = CreateTween();
            tween.SetParallel();
            tween.TweenProperty(_visual, "modulate:a", 0.3f, 0.8f);
            tween.TweenProperty(this, "scale", Vector2.One * 0.5f, 0.8f);
            tween.Chain().TweenCallback(Callable.From(() => GetTree().Paused = true));
        }
    }

    private void ApplyRegen(float delta)
    {
        if (_currentHp >= EffectiveMaxHp)
            return;

        float effectiveRegen = BaseRegenRate + _bonusRegenRate;
        _currentHp = Mathf.Min(_currentHp + effectiveRegen * delta, EffectiveMaxHp);

        _eventBus.EmitSignal(EventBus.SignalName.PlayerDamaged, _currentHp, EffectiveMaxHp);
    }

    private void HitFlash()
    {
        _visual.Color = new Color(1f, 0.3f, 0.3f);
        Tween tween = CreateTween();
        tween.TweenProperty(_visual, "color", _originalColor, 0.2f)
            .SetDelay(0.05f);

        if (_hasSprite && _spriteMaterial != null)
        {
            _spriteMaterial.SetShaderParameter("flash_amount", 1.0f);
            tween.Parallel().TweenMethod(
                Callable.From((float v) => _spriteMaterial.SetShaderParameter("flash_amount", v)),
                1.0f, 0.0f, 0.2f
            ).SetDelay(0.05f);
        }

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
        WeaponSpecialEffect specialEffect = _equippedWeapon.Base.SpecialEffect;
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

        // Chain melee : attaque spéciale avec rebond
        if (pattern == "chain")
        {
            PerformChainAttack();
            return;
        }

        if (type == "melee")
            PerformMeleeAttack(pattern);
        else
            PerformRangedAttack(pattern);
    }

    private void PerformRangedAttack(string pattern)
    {
        int baseProjectileCount = Mathf.Max(1, Mathf.RoundToInt(GetWeaponStat("projectile_count", 1f)));
        int totalProjectiles = Mathf.Max(1, baseProjectileCount + _extraProjectiles);
        float range = GetEffectiveWeaponRange();
        System.Collections.Generic.List<Node2D> targets = FindNearestEnemies(totalProjectiles, range);
        if (targets.Count == 0)
            return;

        float baseDamage = ComputeBaseAttackDamage();
        float projectileSpeed = GetWeaponStat("projectile_speed", 400f);
        int totalPierce = Mathf.Max(0, Mathf.RoundToInt(GetWeaponStat("projectile_pierce", 0f)) + _projectilePierce);
        Vector2 baseDirection = (targets[0].GlobalPosition - GlobalPosition).Normalized();
        PlayAttackFeedback(isMelee: false, baseDirection);

        if (pattern == "burst")
        {
            float spreadAngle = GetWeaponStat("spread_angle", 20f);
            SpawnBurstProjectiles(totalProjectiles, spreadAngle, baseDirection, baseDamage, projectileSpeed, totalPierce);
            return;
        }

        float homingStrength = GetWeaponStat("homing_strength", 0f);
        bool isHoming = pattern == "homing" || homingStrength > 0f;

        for (int i = 0; i < totalProjectiles; i++)
        {
            Node2D target = targets[i % targets.Count];
            Vector2 direction = (target.GlobalPosition - GlobalPosition).Normalized();
            bool isCrit = _critChance > 0f && GD.Randf() < _critChance;
            float effectiveDamage = isCrit ? baseDamage * _critMultiplier : baseDamage;
            Projectile proj = SpawnProjectile(direction, effectiveDamage, projectileSpeed, range, totalPierce, isCrit);

            if (proj != null && isHoming)
                proj.SetHoming(homingStrength > 0f ? homingStrength : 0.8f, target);

            // Ground fire : configurer le projectile pour spawner une zone au sol à l'impact
            if (proj != null && _equippedWeapon?.Base.SpecialEffect?.Type == "ground_fire")
            {
                WeaponSpecialEffect se = _equippedWeapon.Base.SpecialEffect;
                float gDmg = se.Params.TryGetValue("ground_damage", out float gd) ? gd : 5f;
                float gDur = se.Params.TryGetValue("ground_duration", out float gdur) ? gdur : 2f;
                float gRad = ZoneScale(se.Params.TryGetValue("ground_radius", out float grad) ? grad : 30f);
                proj.SetGroundFire(gDmg, gDur, gRad);
            }
        }
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
        int strikeCount = Mathf.Max(1, 1 + _extraProjectiles);
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

            float hitMultiplierSum = SumMeleeStrikeDamageMultipliers(firstHitIndex, lastHitIndex);
            if (hitMultiplierSum <= 0f)
                continue;

            float totalDamage = baseDamage * hitMultiplierSum * critDamageFactor;
            bool hasCrit = clampedCritChance > 0f && GD.Randf() < GetCombinedProcChance(clampedCritChance, hitCount);
            enemy.TakeDamage(totalDamage, hasCrit);
            OnAttackHit(enemy, totalDamage, hasCrit, isRicochet: false, _equippedWeapon, triggerCount: hitCount);
        }
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

    private float SumMeleeStrikeDamageMultipliers(int fromIndex, int toIndex)
    {
        if (toIndex < fromIndex)
            return 0f;

        int start = Mathf.Max(0, fromIndex);
        int end = Mathf.Max(start, toIndex);
        float sum = 0f;

        if (start == 0)
        {
            sum += 1f;
            start = 1;
        }

        if (start > end)
            return sum;

        int linearEnd = Mathf.Min(end, 3);
        if (start <= linearEnd)
        {
            int count = linearEnd - start + 1;
            float sumIndices = (start + linearEnd) * count * 0.5f;
            sum += (0.95f * count) - (0.1f * sumIndices);
            start = linearEnd + 1;
        }

        if (start <= end)
            sum += (end - start + 1) * 0.55f;

        return sum;
    }

    private void SpawnBurstProjectiles(int count, float spreadAngle, Vector2 baseDirection, float baseDamage, float projectileSpeed, int pierce)
    {
        if (count <= 1)
        {
            bool isCrit = _critChance > 0f && GD.Randf() < _critChance;
            float effectiveDamage = isCrit ? baseDamage * _critMultiplier : baseDamage;
            SpawnProjectile(baseDirection, effectiveDamage, projectileSpeed, GetEffectiveWeaponRange(), pierce, isCrit);
            return;
        }

        float start = -spreadAngle * 0.5f;
        float step = spreadAngle / (count - 1);
        for (int i = 0; i < count; i++)
        {
            float angleOffset = start + step * i;
            Vector2 direction = baseDirection.Rotated(Mathf.DegToRad(angleOffset)).Normalized();
            bool isCrit = _critChance > 0f && GD.Randf() < _critChance;
            float effectiveDamage = isCrit ? baseDamage * _critMultiplier : baseDamage;
            SpawnProjectile(direction, effectiveDamage, projectileSpeed, GetEffectiveWeaponRange(), pierce, isCrit);
        }
    }

    private Projectile SpawnProjectile(Vector2 direction, float damage, float speed, float range, int pierce, bool isCrit)
    {
        Projectile projectile = CombatPools.Instance?.TakePlayerProjectile();
        projectile?.Launch(GlobalPosition, direction, damage, speed, Mathf.Clamp(range / Mathf.Max(speed, 1f), 0.2f, 4f),
            pierce, isCrit, this, _equippedWeapon?.Base, _equippedWeapon);
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

    private float ComputeBaseAttackDamage() => ComputeBaseAttackDamage(_equippedWeapon);

    private float ComputeBaseAttackDamage(WeaponInstance weapon)
    {
        float weaponDamage = weapon?.GetStat("damage", AttackDamage) ?? AttackDamage;
        float characterDamageFactor = AttackDamage / 10f;
        float damage = weaponDamage * characterDamageFactor * _damageMultiplier * _erasurePenalty.Damage;

        if (_berserkerThreshold > 0f && _currentHp / EffectiveMaxHp < _berserkerThreshold)
            damage *= _berserkerDamageMult;

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
        return weaponRange * (AttackRange / 300f) * _attackRangeMultiplier;
    }

    /// <summary>Taille d'une zone d'effet (rayon, angle) après les bonus de zone du joueur.</summary>
    private float ZoneScale(float value) => value * _aoeMultiplier;

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
        "projectile_count" => weapon.GetStat("projectile_count", 1f) + _extraProjectiles,
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
        _attackFx.PlayMelee(_equippedWeapon?.Base, direction, range, arcAngle);
    }

    // --- Kill Speed Buff ---

    private void OnEnemyKilled(string enemyId, Vector2 position)
    {
        if (_killSpeedBonusPerKill <= 0f || _isDead)
            return;

        _killSpeedActiveStacks = System.Math.Min(_killSpeedActiveStacks + 1, _killSpeedMaxStacks);
        _killSpeedTimer = _killSpeedDuration;
        UpdateAttackSpeed();
    }

    private void ProcessKillSpeedDecay(float delta)
    {
        if (_killSpeedActiveStacks <= 0)
            return;

        _killSpeedTimer -= delta;
        if (_killSpeedTimer <= 0f)
        {
            _killSpeedActiveStacks = 0;
            _killSpeedTimer = 0f;
            UpdateAttackSpeed();
        }
    }

    private void UpdateAttackSpeed()
    {
        float mult = _attackSpeedMultiplier;
        if (_killSpeedActiveStacks > 0 && _killSpeedBonusPerKill > 0f)
            mult *= 1f + _killSpeedBonusPerKill * _killSpeedActiveStacks;

        for (int i = 0; i < _weaponSlots.Count && i < _weaponTimers.Count; i++)
        {
            WeaponInstance weapon = _weaponSlots[i];
            float weaponAtkSpd = weapon.GetStat("attack_speed", 1f);
            float attacksPerSecond = AttackSpeed * weaponAtkSpd * mult;
            _weaponTimers[i].WaitTime = 1.0f / Mathf.Max(0.05f, attacksPerSecond);
        }
    }

}
