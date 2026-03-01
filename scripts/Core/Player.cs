using System.Collections.Generic;
using System.Linq;
using Godot;
using Vestiges.Base;
using Vestiges.Combat;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Core;

/// <summary>
/// Instance runtime d'un Souvenir Passif équipé. Niveau 1 à MaxLevel.
/// </summary>
public class ActivePassiveSouvenir
{
	public string Id { get; }
	public PassiveSouvenirData Data { get; }
	public int Level { get; private set; }
	public bool IsMaxLevel => Level >= Data.MaxLevel;

	public ActivePassiveSouvenir(PassiveSouvenirData data)
	{
		Data = data;
		Id = data.Id;
		Level = 1;
	}

	/// <summary>Retourne le modifier pour le niveau actuel.</summary>
	public float GetCurrentModifier()
	{
		if (Data.PerLevel == null || Data.PerLevel.Length == 0)
			return 0f;
		int idx = Mathf.Clamp(Level - 1, 0, Data.PerLevel.Length - 1);
		return Data.PerLevel[idx];
	}

	/// <summary>Retourne le modifier du niveau précédent (pour calculer le delta).</summary>
	public float GetPreviousModifier()
	{
		if (Level <= 1 || Data.PerLevel == null || Data.PerLevel.Length == 0)
			return 0f;
		int idx = Mathf.Clamp(Level - 2, 0, Data.PerLevel.Length - 1);
		return Data.PerLevel[idx];
	}

	public bool Upgrade()
	{
		if (IsMaxLevel)
			return false;
		Level++;
		return true;
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

    // AI Control (set by AIController in simulation mode)
    public bool IsAIControlled;
    public Vector2 AIInputOverride;

    private GameManager _gameManager;

    private string _characterId;
    private float _currentHp;
    private bool _isDead;
    private WeaponInstance _equippedWeapon; // Active weapon context for attack helpers
    private PackedScene _projectileScene;
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

    // Weapon "Fragment level" — how many times re-selected at level-up (distinct from per-stat upgrades)
    public const int MaxWeaponFragmentLevel = 8;
    private readonly Dictionary<string, int> _weaponFragmentLevels = new();

    // Weapon visual
    private Node2D _weaponPivot;
    private Polygon2D _weaponVisual;
    private Vector2 _facingDirection = new(1f, 0f);

    // Perk stat modifiers
    private float _damageMultiplier = 1f;
    private float _speedMultiplier = 1f;
    private float _attackSpeedMultiplier = 1f;
    private float _bonusMaxHp;
    private int _extraProjectiles;
    private float _aoeMultiplier = 1f;
    private float _harvestSpeedMultiplier = 1f;
    private float _structureHpMultiplier = 1f;
    private float _craftSpeedMultiplier = 1f;
    private float _attackRangeMultiplier = 1f;
    private float _bonusRegenRate;
    private float _armor;
    private float _critChance;
    private float _critMultiplier = 2f;
    private int _projectilePierce;
    private float _xpMagnetMultiplier = 1f;
    private float _repairSpeedMultiplier = 1f;

    // Complex perk effects
    private float _vampirismPercent;
    private float _berserkerThreshold;
    private float _berserkerDamageMult = 1f;
    private float _thornsPercent;
    private float _executionThreshold;
    private float _dodgeChance;
    private bool _secondWindAvailable;
    private float _secondWindHealPercent;
    private int _harvestBonus;
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
    private WeaponData _orbitalWeapon;
    private float _orbitalAngle;

    // Sustained cone attack
    private float _coneAttackTimer;
    private float _coneDuration;
    private float _coneDamageRampPerSec;
    private float _coneAngleStart;
    private float _coneAngleEnd;
    private float _coneRange;
    private float _coneBaseDamage;
    private bool _isConeActive;
    private Node2D _coneVisual;
    private Polygon2D _conePolygon;

    // Coût en essence pour armes Tier 4+
    private float _essenceDamagePenalty = 1f;

    // Harvest system
    private ResourceNode _harvestTarget;
    private float _harvestProgress;
    private bool _isHarvesting;
    private ProgressBar _harvestBar;
    private Inventory _inventory;

    // POI interaction
    private PointOfInterest _poiTarget;
    private float _poiProgress;
    private bool _isExploringPoi;

    // Chest interaction
    private Chest _chestTarget;
    private float _chestProgress;
    private bool _isOpeningChest;

    public float CurrentHp => _currentHp;
    public float EffectiveMaxHp => MaxHp + _bonusMaxHp;
    public float EffectiveAttackRange => AttackRange * _attackRangeMultiplier;
    public float StructureHpMultiplier => _structureHpMultiplier;
    public float CraftSpeedMultiplier => _craftSpeedMultiplier;
    public float RepairSpeedMultiplier => _repairSpeedMultiplier;
    public int ProjectilePierce => _projectilePierce;
    public float XpMagnetMultiplier => _xpMagnetMultiplier;
    public float CritChance => _critChance;
    public float CritMultiplier => _critMultiplier;
    public bool IsDead => _isDead;
    public bool IsHarvesting => _isHarvesting;
    public string CharacterId => _characterId;
    public WeaponInstance EquippedWeapon => _weaponSlots.Count > 0 ? _weaponSlots[0] : null;
    public IReadOnlyList<WeaponInstance> WeaponSlots => _weaponSlots;
    public IReadOnlyList<ActivePassiveSouvenir> PassiveSlots => _passiveSlots;

    public override void _Ready()
    {
        _currentHp = MaxHp;
        _visual = GetNode<Polygon2D>("Visual");
        _originalColor = _visual.Color;

        AddToGroup("player");

        _projectileScene = GD.Load<PackedScene>("res://scenes/combat/Projectile.tscn");

        CreateHarvestBar();

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EnemyKilled += OnEnemyKilled;

        CreateWeaponVisual();

        _gameManager = GetNode<GameManager>("/root/GameManager");
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.EnemyKilled -= OnEnemyKilled;
    }

    public void InitializeCharacter(CharacterData data)
    {
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

        UpdateWeaponVisual();

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
        if (weapon == null || _weaponSlots.Count >= MaxWeaponSlots)
            return false;

        foreach (WeaponInstance existing in _weaponSlots)
        {
            if (existing.Id == weapon.Id)
                return false;
        }

        WeaponInstance instance = new(weapon);
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

        _eventBus?.EmitSignal(EventBus.SignalName.WeaponEquipped, weapon.Id, capturedIndex);
        _eventBus?.EmitSignal(EventBus.SignalName.WeaponInventoryChanged);

        InitWeaponFragmentLevel(weapon.Id);
        UpdateWeaponVisual();
        GD.Print($"[Player] Weapon added [{capturedIndex}]: {weapon.Name} ({weapon.Id})");
        return true;
    }

    /// <summary>
    /// Retire une arme du slot et retourne la WeaponData de base pour spawn un pickup.
    /// </summary>
    public WeaponData RemoveWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _weaponSlots.Count)
            return null;

        WeaponInstance removed = _weaponSlots[slotIndex];
        _weaponSlots.RemoveAt(slotIndex);

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

        UpdateWeaponVisual();
        UpdateAttackSpeed();

        GD.Print($"[Player] Weapon removed [{slotIndex}]: {removed.Name} ({removed.Id})");
        return removed.Base;
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

    /// <summary>
    /// Ajoute un Souvenir Passif ou upgrade un existant.
    /// Retourne true si ajouté/upgradé, false si slots pleins ou déjà max.
    /// </summary>
    public bool AddOrUpgradePassive(string passiveId)
    {
        PassiveSouvenirData data = PassiveSouvenirDataLoader.Get(passiveId);
        if (data == null)
            return false;

        // Upgrade existant ?
        foreach (ActivePassiveSouvenir existing in _passiveSlots)
        {
            if (existing.Id == passiveId)
            {
                if (existing.IsMaxLevel)
                    return false;

                float prevMod = existing.GetCurrentModifier();
                existing.Upgrade();
                float newMod = existing.GetCurrentModifier();

                ApplyPassiveModifierDelta(data, prevMod, newMod);

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

        ApplyPassiveModifier(data, passive.GetCurrentModifier());

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

    /// <summary>Retourne les IDs des passifs au niveau max.</summary>
    public List<string> GetMaxedPassiveIds()
    {
        List<string> result = new();
        foreach (ActivePassiveSouvenir p in _passiveSlots)
        {
            if (p.IsMaxLevel)
                result.Add(p.Id);
        }
        return result;
    }

    /// <summary>
    /// Retire un passif (utilisé par la fusion : le passif est absorbé par le Vestige).
    /// </summary>
    public bool RemovePassive(string passiveId)
    {
        for (int i = 0; i < _passiveSlots.Count; i++)
        {
            if (_passiveSlots[i].Id == passiveId)
            {
                _passiveSlots.RemoveAt(i);
                _eventBus?.EmitSignal(EventBus.SignalName.PassiveSouvenirSlotsChanged);
                GD.Print($"[Player] Passive removed: {passiveId}");
                return true;
            }
        }
        return false;
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

    /// <summary>
    /// Upgrade le niveau fragment d'une arme (quand le joueur re-sélectionne l'arme au level-up).
    /// Monte le niveau global de l'arme — toutes les stats scalent automatiquement via GetStat().
    /// </summary>
    public bool UpgradeWeaponFragmentLevel(string weaponId)
    {
        int current = _weaponFragmentLevels.GetValueOrDefault(weaponId, 0);
        if (current >= MaxWeaponFragmentLevel)
            return false;

        _weaponFragmentLevels[weaponId] = current + 1;

        for (int i = 0; i < _weaponSlots.Count; i++)
        {
            if (_weaponSlots[i].Id == weaponId)
            {
                _weaponSlots[i].LevelUp();
                RefreshAttackSpeed();
                _eventBus?.EmitSignal(EventBus.SignalName.WeaponUpgraded, weaponId, i, "all", current + 1);
                break;
            }
        }

        GD.Print($"[Player] Weapon fragment level: {weaponId} → {current + 1}/{MaxWeaponFragmentLevel}");
        return true;
    }

    public int GetWeaponFragmentLevel(string weaponId)
    {
        return _weaponFragmentLevels.GetValueOrDefault(weaponId, 0);
    }

    public bool IsWeaponFragmentMaxed(string weaponId)
    {
        return _weaponFragmentLevels.GetValueOrDefault(weaponId, 0) >= MaxWeaponFragmentLevel;
    }

    /// <summary>Retourne les IDs des armes au niveau fragment max.</summary>
    public List<string> GetMaxedWeaponIds()
    {
        List<string> result = new();
        foreach (KeyValuePair<string, int> kv in _weaponFragmentLevels)
        {
            if (kv.Value >= MaxWeaponFragmentLevel)
                result.Add(kv.Key);
        }
        return result;
    }

    /// <summary>Initialise le fragment level à 1 quand une arme est équipée pour la première fois.</summary>
    private void InitWeaponFragmentLevel(string weaponId)
    {
        if (!_weaponFragmentLevels.ContainsKey(weaponId))
            _weaponFragmentLevels[weaponId] = 1;
    }

    // --- Weapon Visual ---

    private void CreateWeaponVisual()
    {
        _weaponPivot = new Node2D { Name = "WeaponPivot" };
        AddChild(_weaponPivot);

        _weaponVisual = new Polygon2D { Name = "WeaponVisual" };
        _weaponPivot.AddChild(_weaponVisual);

        UpdateWeaponVisual();
    }

    private void UpdateWeaponVisual()
    {
        if (_weaponVisual == null)
            return;

        WeaponInstance primaryWeapon = _weaponSlots.Count > 0 ? _weaponSlots[0] : null;
        if (primaryWeapon == null)
        {
            _weaponVisual.Visible = false;
            return;
        }

        _weaponVisual.Visible = true;
        string weaponType = primaryWeapon.Type?.ToLower() ?? "ranged";

        if (weaponType == "melee")
        {
            // Blade shape: elongated polygon offset from the body
            _weaponVisual.Polygon = new Vector2[]
            {
                new(10f, -2f),
                new(24f, -1.5f),
                new(26f, 0f),
                new(24f, 1.5f),
                new(10f, 2f)
            };
            _weaponVisual.Color = new Color(0.75f, 0.75f, 0.8f, 0.9f);
        }
        else if (weaponType == "ranged")
        {
            // Bow shape: curved arc offset from the body
            _weaponVisual.Polygon = new Vector2[]
            {
                new(10f, -6f),
                new(14f, -4f),
                new(16f, 0f),
                new(14f, 4f),
                new(10f, 6f),
                new(12f, 4f),
                new(13f, 0f),
                new(12f, -4f)
            };
            _weaponVisual.Color = new Color(0.65f, 0.5f, 0.3f, 0.9f);
        }
        else
        {
            // Special weapons: small orb
            _weaponVisual.Polygon = new Vector2[]
            {
                new(12f, -3f),
                new(16f, -2f),
                new(18f, 0f),
                new(16f, 2f),
                new(12f, 3f),
                new(14f, 0f)
            };
            _weaponVisual.Color = new Color(0.45f, 0.85f, 1f, 0.9f);
        }

        // Tint weapon based on damage type
        string damageType = primaryWeapon.DamageType?.ToLower() ?? "physical";
        if (damageType == "essence")
            _weaponVisual.Color = new Color(0.45f, 0.85f, 1f, 0.9f);
        else if (damageType == "hybrid")
            _weaponVisual.Color = new Color(0.85f, 0.65f, 1f, 0.9f);

        UpdateWeaponPivotRotation();
    }

    private void UpdateWeaponPivotRotation()
    {
        if (_weaponPivot == null)
            return;

        _weaponPivot.Rotation = _facingDirection.Angle();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDead)
            return;

        float dt = (float)delta;
        bool inputAllowed = _gameManager.CurrentState == GameManager.GameState.Run;
        Vector2 inputDir = IsAIControlled
            ? AIInputOverride
            : inputAllowed ? Input.GetVector("move_left", "move_right", "move_up", "move_down") : Vector2.Zero;

        if (inputDir != Vector2.Zero)
        {
            CancelHarvest();
            CancelPoiExplore();
            CancelChestOpen();
            Vector2 isoDir = CartesianToIsometric(inputDir);
            float terrainSpeedFactor = IsOnWater() ? 0.5f : 1f;
            Velocity = isoDir * (Speed * _speedMultiplier * _slowFactor * terrainSpeedFactor);
            _facingDirection = isoDir.Normalized();
            UpdateWeaponPivotRotation();
        }
        else
        {
            Velocity = Vector2.Zero;
        }

        MoveAndSlide();
        ApplyRegen(dt);
        ProcessSlowDecay(dt);
        ProcessHarvest(dt);
        ProcessPoiExplore(dt);
        ProcessChestOpen(dt);
        ProcessKillSpeedDecay(dt);
        ProcessOrbitalWeapons(dt);
        ProcessSustainedCone(dt);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_isDead || _gameManager.CurrentState != GameManager.GameState.Run)
            return;

        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.J)
        {
            ToggleJournal();
            return;
        }

        if (@event.IsActionPressed("interact"))
        {
            if (_isHarvesting || _isExploringPoi || _isOpeningChest)
            {
                CancelHarvest();
                CancelPoiExplore();
                CancelChestOpen();
            }
            else if (TryStartPoiExplore())
            {
                // POI interaction takes priority
            }
            else if (TryStartChestOpen())
            {
                // Chest interaction
            }
            else if (TryRepairNearbyStructure())
            {
                // Repair took priority
            }
            else
            {
                TryStartHarvest();
            }
        }
    }

    // --- AI Interaction ---

    /// <summary>Programmatic interact trigger for AI simulation. Same logic as the interact input handler.</summary>
    public void AITriggerInteract()
    {
        if (_isDead || !IsAIControlled) return;
        if (_isHarvesting || _isExploringPoi || _isOpeningChest)
        {
            CancelHarvest();
            CancelPoiExplore();
            CancelChestOpen();
        }
        else if (TryStartPoiExplore()) { }
        else if (TryStartChestOpen()) { }
        else if (TryRepairNearbyStructure()) { }
        else { TryStartHarvest(); }
    }

    // --- Journal ---

    private void ToggleJournal()
    {
        UI.JournalScreen journal = GetNodeOrNull<UI.JournalScreen>("/root/Main/JournalScreen");
        journal?.Toggle();
    }

    // --- World Queries ---

    private WorldSetup _worldSetup;

    private void CacheWorldSetup()
    {
        _worldSetup = GetNodeOrNull<WorldSetup>("/root/Main");
    }

    private bool IsOnWater()
    {
        if (_worldSetup == null)
            CacheWorldSetup();
        if (_worldSetup == null)
            return false;

        TileMapLayer ground = _worldSetup.GetNodeOrNull<TileMapLayer>("Ground");
        if (ground == null)
            return false;

        Vector2I cell = ground.LocalToMap(ground.ToLocal(GlobalPosition));
        return _worldSetup.Generator.GetTerrain(cell.X, cell.Y) == TerrainType.Water;
    }

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
            case "harvest_speed":
                if (modifierType == "multiplicative") _harvestSpeedMultiplier *= value;
                break;
            case "structure_hp":
                if (modifierType == "multiplicative") _structureHpMultiplier *= value;
                break;
            case "craft_speed":
                if (modifierType == "multiplicative") _craftSpeedMultiplier *= value;
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
            case "repair_speed":
                if (modifierType == "multiplicative") _repairSpeedMultiplier *= value;
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

    public void AddHarvestBonus(int bonus)
    {
        _harvestBonus += bonus;
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
    public void OnProjectileHit(Enemy enemy, float damage, bool isCrit, bool isRicochet)
    {
        OnAttackHit(enemy, damage, isCrit, isRicochet);
    }

    private void OnAttackHit(Enemy enemy, float damage, bool isCrit, bool isRicochet, int triggerCount = 1)
    {
        if (_isDead)
            return;

        if (!IsInstanceValid(enemy) || enemy.IsQueuedForDeletion())
            return;

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
            SpawnRicochet(enemy, damage, isCrit);

        // --- Weapon on-hit effects ---
        WeaponOnHitEffect ohe = _equippedWeapon?.Base.OnHitEffect;
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
        float knockback = GetWeaponStat("knockback", 0f);
        if (knockback > 0f)
        {
            Vector2 knockDir = (enemy.GlobalPosition - GlobalPosition).Normalized();
            enemy.ApplyKnockback(knockDir, knockback);
        }

        // --- Weapon special effects ---
        WeaponSpecialEffect se = _equippedWeapon?.Base.SpecialEffect;
        if (se != null)
            ProcessWeaponSpecialOnHit(se, enemy, damage);
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

    private void SpawnRicochet(Enemy sourceEnemy, float damage, bool isCrit)
    {
        Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemies");
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
        Projectile projectile = _projectileScene.Instantiate<Projectile>();
        projectile.GlobalPosition = sourceEnemy.GlobalPosition;
        projectile.Speed = GetWeaponStat("projectile_speed", 400f);
        projectile.MaxLifetime = Mathf.Clamp(_ricochetRange / Mathf.Max(projectile.Speed, 1f), 0.2f, 2f);
        projectile.Initialize(direction, damage * 0.75f, 0, isCrit, this, isRicochet: true);

        Polygon2D visual = projectile.GetNodeOrNull<Polygon2D>("Visual");
        if (visual != null)
            visual.Color = GetWeaponProjectileColor();

        GetTree().CurrentScene.AddChild(projectile);
    }

    // --- Weapon Special Effects ---

    private void ProcessWeaponSpecialOnHit(WeaponSpecialEffect se, Enemy enemy, float damage)
    {
        switch (se.Type)
        {
            case "heal_every_n_hits":
            {
                string weaponId = _equippedWeapon.Id;
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
                ulong enemyId = enemy.GetInstanceId();
                GetTree().CreateTimer(delay).Timeout += () =>
                {
                    // Réapplique les dégâts à la position d'origine (AoE fantôme)
                    Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemies");
                    foreach (Node node in enemies)
                    {
                        if (node is Enemy e && IsInstanceValid(e) && !e.IsDying)
                        {
                            if (e.GlobalPosition.DistanceTo(echoPos) < 40f)
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
                float radius = se.Params.TryGetValue("slow_radius", out float r) ? r : 80f;
                float factor = se.Params.TryGetValue("slow_factor", out float f) ? f : 0.3f;
                float duration = se.Params.TryGetValue("slow_duration", out float dur) ? dur : 0.5f;
                Vector2 impactPos = enemy.GlobalPosition;
                Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemies");
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
                float aoeRadius = se.Params.TryGetValue("shape_aoe_on_impact", out float aoe) ? aoe : 50f;
                Vector2 impactPos = enemy.GlobalPosition;
                Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemies");
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
        Polygon2D echo = new();
        int segments = 8;
        Vector2[] points = new Vector2[segments];
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.Tau * i / segments;
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 5f;
        }
        echo.Polygon = points;
        echo.Color = new Color(0.6f, 0.4f, 1f, 0.6f);
        echo.GlobalPosition = position;
        GetTree().CurrentScene.AddChild(echo);

        Tween tween = echo.CreateTween();
        tween.SetParallel();
        tween.TweenProperty(echo, "scale", new Vector2(8f, 8f), 0.25f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(echo, "modulate:a", 0f, 0.25f);
        tween.Chain().TweenCallback(Callable.From(() => echo.QueueFree()));
    }

    private void SpawnTimeSlowVisual(Vector2 position, float radius, float duration)
    {
        Polygon2D zone = new();
        int segments = 16;
        Vector2[] points = new Vector2[segments];
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.Tau * i / segments;
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        zone.Polygon = points;
        zone.Color = new Color(0.3f, 0.5f, 0.9f, 0.2f);
        zone.GlobalPosition = position;
        GetTree().CurrentScene.AddChild(zone);

        Tween tween = zone.CreateTween();
        tween.TweenProperty(zone, "modulate:a", 0f, duration);
        tween.TweenCallback(Callable.From(() => zone.QueueFree()));
    }

    // --- Orbital Weapons ---

    private void SetupOrbitalWeapon(WeaponData weapon)
    {
        if (_orbitalWeapon?.Id == weapon.Id && _orbitalProjectiles.Count > 0)
            return;

        _orbitalWeapon = weapon;
        int orbitalCount = Mathf.Max(1, (int)GetWeaponStat("orbital_count", 3f));
        float orbitalRadius = GetWeaponStat("range", 90f);
        float damage = ComputeBaseAttackDamage();

        // Nettoyage des anciens orbitaux
        foreach (Node2D old in _orbitalProjectiles)
        {
            if (IsInstanceValid(old))
                old.QueueFree();
        }
        _orbitalProjectiles.Clear();

        for (int i = 0; i < orbitalCount; i++)
        {
            Area2D orb = new() { Name = $"OrbitalOrb_{i}" };
            orb.CollisionLayer = 0;
            orb.CollisionMask = 2;

            CollisionShape2D shape = new();
            CircleShape2D circle = new() { Radius = 8f };
            shape.Shape = circle;
            orb.AddChild(shape);

            Polygon2D visual = new();
            Vector2[] points = new Vector2[6];
            for (int j = 0; j < 6; j++)
            {
                float angle = Mathf.Tau * j / 6;
                points[j] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 5f;
            }
            visual.Polygon = points;
            visual.Color = new Color(0.45f, 0.85f, 1f, 0.8f);
            orb.AddChild(visual);

            float capturedDamage = damage;
            orb.BodyEntered += (Node2D body) =>
            {
                if (body is Enemy enemy && !enemy.IsDying && IsInstanceValid(enemy))
                {
                    enemy.TakeDamage(capturedDamage);
                    OnAttackHit(enemy, capturedDamage, false, false);
                }
            };

            AddChild(orb);
            _orbitalProjectiles.Add(orb);
        }
    }

    private void ProcessOrbitalWeapons(float delta)
    {
        if (_orbitalProjectiles.Count == 0 || _orbitalWeapon == null)
            return;

        float orbitalSpeed = GetStatFromWeapon(_orbitalWeapon, "orbital_speed", 180f);
        float orbitalRadius = GetStatFromWeapon(_orbitalWeapon, "range", 90f);
        _orbitalAngle += Mathf.DegToRad(orbitalSpeed) * delta;
        if (_orbitalAngle > Mathf.Tau)
            _orbitalAngle -= Mathf.Tau;

        for (int i = 0; i < _orbitalProjectiles.Count; i++)
        {
            Node2D orb = _orbitalProjectiles[i];
            if (!IsInstanceValid(orb))
                continue;

            float angle = _orbitalAngle + (Mathf.Tau * i / _orbitalProjectiles.Count);
            orb.Position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * orbitalRadius;
        }
    }

    private float GetStatFromWeapon(WeaponData weapon, string key, float defaultVal)
    {
        if (weapon?.Stats != null && weapon.Stats.TryGetValue(key, out float val))
            return val;
        return defaultVal;
    }

    // --- Sustained Cone Attack ---

    private void ActivateSustainedCone(WeaponSpecialEffect effect)
    {
        _isConeActive = true;
        _coneDuration = effect.Params.TryGetValue("duration", out float dur) ? dur : 2f;
        _coneAttackTimer = _coneDuration;
        _coneDamageRampPerSec = effect.Params.TryGetValue("damage_ramp_per_sec", out float ramp) ? ramp : 1.5f;
        _coneAngleStart = GetWeaponStat("cone_angle_start", 15f);
        _coneAngleEnd = GetWeaponStat("cone_angle_end", 60f);
        _coneRange = GetEffectiveWeaponRange();
        _coneBaseDamage = ComputeBaseAttackDamage();

        // Création du visuel en cône (polygon triangulaire)
        if (_coneVisual != null && IsInstanceValid(_coneVisual))
            _coneVisual.QueueFree();

        _coneVisual = new Node2D { Name = "SustainedConeVisual" };
        _conePolygon = new Polygon2D();
        _conePolygon.Color = new Color(0.45f, 0.85f, 1f, 0.25f);
        _coneVisual.AddChild(_conePolygon);
        AddChild(_coneVisual);

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

        // Application des dégâts aux ennemis dans le cône
        Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemies");
        foreach (Node node in enemies)
        {
            if (node is not Enemy enemy || enemy.IsDying || !IsInstanceValid(enemy))
                continue;

            Vector2 toEnemy = enemy.GlobalPosition - GlobalPosition;
            float dist = toEnemy.Length();
            if (dist > _coneRange || dist <= 0.001f)
                continue;

            Vector2 dirToEnemy = toEnemy / dist;
            if (_facingDirection.Dot(dirToEnemy) < dotThreshold)
                continue;

            enemy.TakeDamage(damage);
            OnAttackHit(enemy, damage, false, false);
        }
    }

    private void UpdateConeVisual(float elapsed)
    {
        if (_conePolygon == null || !IsInstanceValid(_conePolygon))
            return;

        float progress = Mathf.Clamp(elapsed / _coneDuration, 0f, 1f);
        float currentAngleDeg = Mathf.Lerp(_coneAngleStart, _coneAngleEnd, progress);
        float halfAngleRad = Mathf.DegToRad(currentAngleDeg * 0.5f);

        // Polygone en forme de cône : pointe au joueur, s'élargit vers la portée
        int segments = 12;
        Vector2[] points = new Vector2[segments + 2];
        points[0] = Vector2.Zero; // Pointe du cône (position du joueur)

        float baseAngle = _facingDirection.Angle();
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = baseAngle - halfAngleRad + t * halfAngleRad * 2f;
            points[i + 1] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _coneRange;
        }

        _conePolygon.Polygon = points;

        // Opacité pulsante pour feedback visuel
        float alpha = 0.2f + 0.1f * Mathf.Sin(elapsed * 8f);
        _conePolygon.Color = new Color(0.45f, 0.85f, 1f, alpha);
    }

    private void DeactivateSustainedCone()
    {
        _isConeActive = false;
        _coneAttackTimer = 0f;

        if (_coneVisual != null && IsInstanceValid(_coneVisual))
        {
            _coneVisual.QueueFree();
            _coneVisual = null;
            _conePolygon = null;
        }

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
        OnAttackHit(firstTarget, currentDamage, isCrit, false);

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
            OnAttackHit(nextTarget, currentDamage, false, false);
            current = nextTarget;
        }
    }

    private Enemy FindNearestEnemyExcluding(Vector2 from, float maxRange, HashSet<ulong> excludeIds)
    {
        Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemies");
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
        Line2D chain = new();
        chain.Points = new Vector2[] { from, to };
        chain.Width = 2f;
        chain.DefaultColor = new Color(0.8f, 0.7f, 0.3f, 0.8f);
        GetTree().CurrentScene.AddChild(chain);

        Tween tween = chain.CreateTween();
        tween.TweenProperty(chain, "modulate:a", 0f, 0.2f);
        tween.TweenCallback(Callable.From(() => chain.QueueFree()));
    }

    // --- Health ---

    public void Heal(float amount)
    {
        if (_isDead || amount <= 0)
            return;

        _currentHp = Mathf.Min(_currentHp + amount, EffectiveMaxHp);

        _eventBus.EmitSignal(EventBus.SignalName.PlayerDamaged, _currentHp, EffectiveMaxHp);
    }

    public void TakeDamage(float damage)
    {
        if (_currentHp <= 0)
            return;

        // Dodge check
        if (_dodgeChance > 0f && GD.Randf() < _dodgeChance)
        {
            GD.Print("[Player] Dodged!");
            return;
        }

        float reduced = Mathf.Max(1f, damage - _armor);
        _currentHp -= reduced;
        CancelHarvest();
        HitFlash();

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

        Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemies");
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

    // --- Harvest ---

    private void TryStartHarvest()
    {
        ResourceNode nearest = FindNearestResource();
        if (nearest == null || nearest.IsExhausted)
            return;

        _harvestTarget = nearest;
        _harvestProgress = 0f;
        _isHarvesting = true;
        _harvestBar.Visible = true;
        _harvestBar.MaxValue = nearest.HarvestTime;
        _harvestBar.Value = 0;
    }

    private void ProcessHarvest(float delta)
    {
        if (!_isHarvesting || _harvestTarget == null)
            return;

        if (!IsInstanceValid(_harvestTarget) || _harvestTarget.IsExhausted)
        {
            CancelHarvest();
            return;
        }

        float dist = GlobalPosition.DistanceTo(_harvestTarget.GlobalPosition);
        if (dist > InteractRange * 1.5f)
        {
            CancelHarvest();
            return;
        }

        _harvestProgress += delta * _harvestSpeedMultiplier;
        _harvestBar.Value = _harvestProgress;

        if (_harvestProgress >= _harvestTarget.HarvestTime)
            CompleteHarvest();
    }

    private void CompleteHarvest()
    {
        if (_harvestTarget == null || !IsInstanceValid(_harvestTarget))
        {
            CancelHarvest();
            return;
        }

        int amount = _harvestTarget.Harvest();
        string resourceId = _harvestTarget.ResourceId;

        if (amount > 0)
        {
            int totalAmount = amount + _harvestBonus;
            CacheInventory();
            _inventory?.Add(resourceId, totalAmount);
        }

        _isHarvesting = false;
        _harvestBar.Visible = false;
        _harvestTarget = null;
        _harvestProgress = 0f;
    }

    private void CancelHarvest()
    {
        if (!_isHarvesting)
            return;

        _isHarvesting = false;
        _harvestBar.Visible = false;
        _harvestTarget = null;
        _harvestProgress = 0f;
    }

    private ResourceNode FindNearestResource()
    {
        Godot.Collections.Array<Node> resources = GetTree().GetNodesInGroup("resources");
        ResourceNode nearest = null;
        float nearestDist = InteractRange;

        foreach (Node node in resources)
        {
            if (node is ResourceNode res && !res.IsExhausted)
            {
                float dist = GlobalPosition.DistanceTo(res.GlobalPosition);
                if (dist < nearestDist)
                {
                    nearest = res;
                    nearestDist = dist;
                }
            }
        }

        return nearest;
    }

    private void CreateHarvestBar()
    {
        _harvestBar = new ProgressBar();
        _harvestBar.CustomMinimumSize = new Vector2(40, 5);
        _harvestBar.Position = new Vector2(-20, -25);
        _harvestBar.ShowPercentage = false;
        _harvestBar.Visible = false;

        StyleBoxFlat fillStyle = new();
        fillStyle.BgColor = new Color(0.9f, 0.75f, 0.2f);
        fillStyle.CornerRadiusBottomLeft = 2;
        fillStyle.CornerRadiusBottomRight = 2;
        fillStyle.CornerRadiusTopLeft = 2;
        fillStyle.CornerRadiusTopRight = 2;
        _harvestBar.AddThemeStyleboxOverride("fill", fillStyle);

        StyleBoxFlat bgStyle = new();
        bgStyle.BgColor = new Color(0.1f, 0.1f, 0.1f, 0.7f);
        bgStyle.CornerRadiusBottomLeft = 2;
        bgStyle.CornerRadiusBottomRight = 2;
        bgStyle.CornerRadiusTopLeft = 2;
        bgStyle.CornerRadiusTopRight = 2;
        _harvestBar.AddThemeStyleboxOverride("background", bgStyle);

        AddChild(_harvestBar);
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
        _harvestBar.Visible = true;
        _harvestBar.MaxValue = nearest.SearchTime;
        _harvestBar.Value = 0;
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

        _poiProgress += delta * _harvestSpeedMultiplier;
        _harvestBar.Value = _poiProgress;

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
        _harvestBar.Visible = false;
        _poiTarget = null;
        _poiProgress = 0f;
    }

    private void CancelPoiExplore()
    {
        if (!_isExploringPoi)
            return;

        _isExploringPoi = false;
        _harvestBar.Visible = false;
        _poiTarget = null;
        _poiProgress = 0f;
    }

    private void ApplyPoiLoot(PointOfInterest poi)
    {
        if (string.IsNullOrEmpty(poi.LootTableId))
            return;

        System.Collections.Generic.List<LootResolver.LootResult> loots =
            LootResolver.Roll(poi.LootTableId, poi.LootRolls);
        CacheInventory();

        int lootIndex = 0;
        foreach (LootResolver.LootResult loot in loots)
        {
            string displayName = loot.ItemId;
            Color displayColor = new(0.9f, 0.85f, 0.6f);

            switch (loot.Type)
            {
                case "resource":
                    _inventory?.Add(loot.ItemId, loot.Amount);
                    _eventBus?.EmitSignal(EventBus.SignalName.LootReceived,
                        loot.Type, loot.ItemId, loot.Amount);
                    displayName = $"{loot.ItemId} x{loot.Amount}";
                    break;
                case "xp":
                    _eventBus?.EmitSignal(EventBus.SignalName.XpGained, (float)loot.Amount);
                    displayName = $"+{loot.Amount} XP";
                    displayColor = new Color(0.4f, 0.8f, 1f);
                    break;
                case "perk":
                    string poiPerkName = ResolvePerkLoot(loot.ItemId);
                    displayName = poiPerkName;
                    displayColor = new Color(0.5f, 1f, 0.5f);
                    break;
                case "souvenir":
                    _eventBus?.EmitSignal(EventBus.SignalName.LootReceived,
                        loot.Type, loot.ItemId, loot.Amount);
                    displayName = $"Souvenir: {loot.ItemId}";
                    displayColor = new Color(0.8f, 0.85f, 1f);
                    break;
                default:
                    continue;
            }

            SpawnLootPopup(displayName, displayColor, poi.GlobalPosition, lootIndex);
            lootIndex++;
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

    private bool TryStartChestOpen()
    {
        Chest nearest = FindNearestChest();
        if (nearest == null || !nearest.CanOpen)
            return false;

        if (nearest.OpenTime <= 0f)
        {
            ApplyChestLoot(nearest);
            return true;
        }

        _chestTarget = nearest;
        _chestProgress = 0f;
        _isOpeningChest = true;
        _harvestBar.Visible = true;
        _harvestBar.MaxValue = nearest.OpenTime;
        _harvestBar.Value = 0;
        return true;
    }

    private void ProcessChestOpen(float delta)
    {
        if (!_isOpeningChest || _chestTarget == null)
            return;

        if (!IsInstanceValid(_chestTarget) || _chestTarget.IsOpened)
        {
            CancelChestOpen();
            return;
        }

        float dist = GlobalPosition.DistanceTo(_chestTarget.GlobalPosition);
        if (dist > InteractRange * 1.5f)
        {
            CancelChestOpen();
            return;
        }

        _chestProgress += delta;
        _harvestBar.Value = _chestProgress;

        if (_chestProgress >= _chestTarget.OpenTime)
            CompleteChestOpen();
    }

    private void CompleteChestOpen()
    {
        if (_chestTarget == null || !IsInstanceValid(_chestTarget))
        {
            CancelChestOpen();
            return;
        }

        ApplyChestLoot(_chestTarget);

        _isOpeningChest = false;
        _harvestBar.Visible = false;
        _chestTarget = null;
        _chestProgress = 0f;
    }

    private void ApplyChestLoot(Chest chest)
    {
        System.Collections.Generic.List<LootResolver.LootResult> loots = chest.Open();
        CacheInventory();

        int lootIndex = 0;
        foreach (LootResolver.LootResult loot in loots)
        {
            string displayName = loot.ItemId;
            Color displayColor = new(0.9f, 0.85f, 0.6f);

            switch (loot.Type)
            {
                case "resource":
                    _inventory?.Add(loot.ItemId, loot.Amount);
                    _eventBus?.EmitSignal(EventBus.SignalName.LootReceived,
                        loot.Type, loot.ItemId, loot.Amount);
                    displayName = $"{loot.ItemId} x{loot.Amount}";
                    break;
                case "xp":
                    _eventBus?.EmitSignal(EventBus.SignalName.XpGained, (float)loot.Amount);
                    displayName = $"+{loot.Amount} XP";
                    displayColor = new Color(0.4f, 0.8f, 1f);
                    break;
                case "perk":
                    string chestPerkName = ResolvePerkLoot(loot.ItemId);
                    displayName = chestPerkName;
                    displayColor = new Color(0.5f, 1f, 0.5f);
                    break;
                case "souvenir":
                    _eventBus?.EmitSignal(EventBus.SignalName.LootReceived,
                        loot.Type, loot.ItemId, loot.Amount);
                    displayName = $"Souvenir: {loot.ItemId}";
                    displayColor = new Color(0.8f, 0.85f, 1f);
                    break;
                default:
                    continue;
            }

            SpawnLootPopup(displayName, displayColor, chest.GlobalPosition, lootIndex);
            lootIndex++;
        }
    }


    /// <summary>
    /// Résout un perk loot (random_perk → perk concret), l'applique via LootReceived, et retourne le nom.
    /// </summary>
    private string ResolvePerkLoot(string perkItemId)
    {
        string resolvedId = perkItemId;
        if (resolvedId == "random_perk")
        {
            System.Collections.Generic.List<PerkData> allPerks = PerkDataLoader.GetAll();
            if (allPerks != null && allPerks.Count > 0)
                resolvedId = allPerks[(int)(GD.Randi() % allPerks.Count)].Id;
        }

        _eventBus?.EmitSignal(EventBus.SignalName.LootReceived, "perk", resolvedId, 1);

        PerkData data = PerkDataLoader.Get(resolvedId);
        return data != null ? data.Name : resolvedId;
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
        label.AddThemeFontSizeOverride("font_size", 10);
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

    private void CancelChestOpen()
    {
        if (!_isOpeningChest)
            return;

        _isOpeningChest = false;
        _harvestBar.Visible = false;
        _chestTarget = null;
        _chestProgress = 0f;
    }

    private Chest FindNearestChest()
    {
        Godot.Collections.Array<Node> chests = GetTree().GetNodesInGroup("chests");
        Chest nearest = null;
        float nearestDist = InteractRange;

        foreach (Node node in chests)
        {
            if (node is Chest chest && chest.CanOpen)
            {
                float dist = GlobalPosition.DistanceTo(chest.GlobalPosition);
                if (dist < nearestDist)
                {
                    nearest = chest;
                    nearestDist = dist;
                }
            }
        }

        return nearest;
    }

    private void CacheInventory()
    {
        if (_inventory != null)
            return;

        _inventory = GetNodeOrNull<Inventory>("Inventory");
    }

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
        Velocity = Vector2.Zero;
        foreach (Timer timer in _weaponTimers)
            timer.Stop();
        CancelHarvest();
        DeactivateSustainedCone();
        RemoveFromGroup("player");

        _eventBus.EmitSignal(EventBus.SignalName.EntityDied, this);

        Tween tween = CreateTween();
        tween.SetParallel();
        tween.TweenProperty(_visual, "modulate:a", 0.3f, 0.8f);
        tween.TweenProperty(this, "scale", Vector2.One * 0.5f, 0.8f);
        tween.Chain().TweenCallback(Callable.From(() =>
        {
            GetTree().Paused = true;
        }));
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
    }

    private void OnWeaponAttackTimeout(int slotIndex)
    {
        if (_isDead || slotIndex >= _weaponSlots.Count)
            return;

        _equippedWeapon = _weaponSlots[slotIndex];

        // Coût en essence : consomme si disponible, sinon pénalité de dégâts
        float essenceCost = GetWeaponStat("essence_cost_per_attack", 0f);
        if (essenceCost > 0f)
        {
            CacheInventory();
            int requiredEssence = Mathf.Max(1, Mathf.RoundToInt(essenceCost));
            if (_inventory != null && _inventory.Has("essence", requiredEssence))
            {
                _inventory.Remove("essence", requiredEssence);
                _essenceDamagePenalty = 1f;
            }
            else
            {
                // Pas assez d'essence : dégâts réduits de moitié
                _essenceDamagePenalty = 0.5f;
            }
        }
        else
        {
            _essenceDamagePenalty = 1f;
        }

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
            SetupOrbitalWeapon(_equippedWeapon.Base);
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
                float gRad = se.Params.TryGetValue("ground_radius", out float grad) ? grad : 30f;
                proj.SetGroundFire(gDmg, gDur, gRad);
            }
        }
    }

    private void PerformMeleeAttack(string pattern)
    {
        float range = GetEffectiveWeaponRange();
        float arcAngle = pattern switch
        {
            "circular" => 360f,
            "linear" => 60f,
            _ => GetWeaponStat("arc_angle", 120f)
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
            OnAttackHit(enemy, totalDamage, hasCrit, isRicochet: false, triggerCount: hitCount);
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
        Projectile projectile = _projectileScene.Instantiate<Projectile>();
        projectile.GlobalPosition = GlobalPosition;
        projectile.Speed = speed;
        projectile.MaxLifetime = Mathf.Clamp(range / Mathf.Max(speed, 1f), 0.2f, 4f);
        projectile.Initialize(direction, damage, pierce, isCrit, this);
        projectile.SourceWeapon = _equippedWeapon?.Base;

        Polygon2D visual = projectile.GetNodeOrNull<Polygon2D>("Visual");
        if (visual != null)
            visual.Color = GetWeaponProjectileColor();

        GetTree().CurrentScene.AddChild(projectile);
        return projectile;
    }

    private System.Collections.Generic.List<Node2D> FindNearestEnemies(int count, float maxRange)
    {
        Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemies");
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
        Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemies");
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

    private float ComputeBaseAttackDamage()
    {
        float weaponDamage = GetWeaponStat("damage", AttackDamage);
        float characterDamageFactor = AttackDamage / 10f;
        float damage = weaponDamage * characterDamageFactor * _damageMultiplier;

        if (_berserkerThreshold > 0f && _currentHp / EffectiveMaxHp < _berserkerThreshold)
            damage *= _berserkerDamageMult;

        // Pénalité si manque d'essence (armes Tier 4+)
        damage *= _essenceDamagePenalty;

        return damage;
    }

    private float GetEffectiveWeaponRange()
    {
        float weaponRange = GetWeaponStat("range", AttackRange);
        float characterRangeFactor = AttackRange / 300f;
        return weaponRange * characterRangeFactor * _attackRangeMultiplier * _aoeMultiplier;
    }

    private float GetWeaponStat(string key, float fallback)
    {
        if (_equippedWeapon == null)
            return fallback;

        return _equippedWeapon.GetStat(key, fallback);
    }

    private Color GetWeaponProjectileColor()
    {
        if (_equippedWeapon == null)
            return new Color(1f, 0.85f, 0.2f);

        return _equippedWeapon.DamageType switch
        {
            "essence" => new Color(0.45f, 0.85f, 1f),
            "hybrid" => new Color(0.85f, 0.65f, 1f),
            _ => new Color(1f, 0.85f, 0.2f)
        };
    }

    private void PlayAttackFeedback(bool isMelee, Vector2 direction)
    {
        if (_visual == null)
            return;

        _facingDirection = direction.Normalized();
        UpdateWeaponPivotRotation();

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

        if (!isMelee)
            SpawnMuzzleFlash(direction);
    }

    private void SpawnMuzzleFlash(Vector2 direction)
    {
        Node2D flashRoot = new();
        flashRoot.GlobalPosition = GlobalPosition + direction * 14f;
        flashRoot.Rotation = direction.Angle();

        Polygon2D flash = new();
        flash.Color = new Color(GetWeaponProjectileColor(), 0.85f);
        flash.Polygon = new Vector2[]
        {
            new(-3f, 0f),
            new(8f, -3.5f),
            new(14f, 0f),
            new(8f, 3.5f)
        };
        flashRoot.AddChild(flash);
        GetTree().CurrentScene.AddChild(flashRoot);

        Tween tween = flashRoot.CreateTween();
        tween.SetParallel();
        tween.TweenProperty(flash, "scale", new Vector2(1.5f, 1.2f), 0.08f);
        tween.TweenProperty(flash, "modulate:a", 0f, 0.08f);
        tween.TweenProperty(flashRoot, "rotation", flashRoot.Rotation + 0.2f, 0.08f);
        tween.Chain().TweenCallback(Callable.From(() => flashRoot.QueueFree()));
    }

    private void SpawnSlashEffect(Vector2 direction, float range, float arcAngle)
    {
        Node2D fxRoot = new();
        fxRoot.GlobalPosition = GlobalPosition + direction * 8f;
        fxRoot.Rotation = direction.Angle();

        Polygon2D slash = new();
        slash.Color = new Color(1f, 0.9f, 0.7f, 0.75f);

        if (arcAngle >= 359f)
        {
            int segments = 18;
            float radius = Mathf.Clamp(range * 0.45f, 18f, 80f);
            Vector2[] circle = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float a = Mathf.Tau * i / segments;
                circle[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            }
            slash.Polygon = circle;
        }
        else
        {
            float length = Mathf.Clamp(range * 0.7f, 18f, 90f);
            float width = Mathf.Clamp(length * 0.35f, 8f, 30f);
            slash.Polygon = new Vector2[]
            {
                new(-6f, 0f),
                new(length * 0.3f, -width * 0.5f),
                new(length, 0f),
                new(length * 0.3f, width * 0.5f)
            };
        }

        slash.Scale = new Vector2(0.45f, 0.9f);
        fxRoot.AddChild(slash);
        GetTree().CurrentScene.AddChild(fxRoot);

        float sweep = arcAngle >= 359f ? 0f : Mathf.DegToRad(Mathf.Clamp(arcAngle * 0.35f, 18f, 70f));
        Tween tween = fxRoot.CreateTween();
        tween.SetParallel();
        tween.TweenProperty(slash, "scale", new Vector2(1.2f, 1f), 0.11f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(slash, "modulate:a", 0f, 0.14f);
        if (sweep > 0f)
            tween.TweenProperty(fxRoot, "rotation", fxRoot.Rotation + sweep, 0.11f);
        tween.Chain().TweenCallback(Callable.From(() => fxRoot.QueueFree()));
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

    // --- Repair ---

    private bool TryRepairNearbyStructure()
    {
        Structure target = FindDamagedStructure();
        if (target == null)
            return false;

        float repairAmount = target.HpRatio < 1f ? (1f - target.HpRatio) * 100f : 0f;
        if (repairAmount <= 0f)
            return false;

        CacheInventory();
        if (_inventory == null)
            return false;

        RecipeData recipe = RecipeDataLoader.Get(target.Recipe);
        if (recipe == null || recipe.Ingredients.Count == 0)
            return false;

        RecipeIngredient primaryIngredient = recipe.Ingredients[0];
        if (!_inventory.Has(primaryIngredient.Resource, 1))
            return false;

        _inventory.Remove(primaryIngredient.Resource, 1);
        target.Repair(repairAmount);
        return true;
    }

    private Structure FindDamagedStructure()
    {
        Godot.Collections.Array<Node> structures = GetTree().GetNodesInGroup("structures");
        Structure nearest = null;
        float nearestDist = InteractRange;

        foreach (Node node in structures)
        {
            if (node is Structure structure && !structure.IsDestroyed && structure.HpRatio < 1f)
            {
                float dist = GlobalPosition.DistanceTo(structure.GlobalPosition);
                if (dist < nearestDist)
                {
                    nearest = structure;
                    nearestDist = dist;
                }
            }
        }

        return nearest;
    }

    private static Vector2 CartesianToIsometric(Vector2 cartesian)
    {
        Vector2 iso = new Vector2(
            cartesian.X - cartesian.Y,
            (cartesian.X + cartesian.Y) * 0.5f
        );
        return iso.Normalized();
    }
}
