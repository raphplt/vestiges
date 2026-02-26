using Godot;
using Vestiges.Base;
using Vestiges.Combat;
using Vestiges.Infrastructure;
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

    private string _characterId;
    private float _currentHp;
    private bool _isDead;
    private WeaponData _equippedWeapon;
    private PackedScene _projectileScene;
    private Timer _attackTimer;
    private Polygon2D _visual;
    private Color _originalColor;
    private EventBus _eventBus;
    private Tween _attackFeedbackTween;

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
    public WeaponData EquippedWeapon => _equippedWeapon;

    public override void _Ready()
    {
        _currentHp = MaxHp;
        _visual = GetNode<Polygon2D>("Visual");
        _originalColor = _visual.Color;

        AddToGroup("player");

        _projectileScene = GD.Load<PackedScene>("res://scenes/combat/Projectile.tscn");

        _attackTimer = new Timer();
        _attackTimer.WaitTime = 1.0 / AttackSpeed;
        _attackTimer.Autostart = true;
        _attackTimer.Timeout += OnAttackTimerTimeout;
        AddChild(_attackTimer);

        CreateHarvestBar();

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EnemyKilled += OnEnemyKilled;
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

        _equippedWeapon = weapon;
        GD.Print($"[Player] Equipped weapon: {_equippedWeapon.Name} ({_equippedWeapon.Id})");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDead)
            return;

        float dt = (float)delta;
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_up", "move_down");

        if (inputDir != Vector2.Zero)
        {
            CancelHarvest();
            CancelPoiExplore();
            CancelChestOpen();
            Vector2 isoDir = CartesianToIsometric(inputDir);
            float terrainSpeedFactor = IsOnWater() ? 0.5f : 1f;
            Velocity = isoDir * (Speed * _speedMultiplier * terrainSpeedFactor);
        }
        else
        {
            Velocity = Vector2.Zero;
        }

        MoveAndSlide();
        ClampToWorldBounds();
        ApplyRegen(dt);
        ProcessHarvest(dt);
        ProcessPoiExplore(dt);
        ProcessChestOpen(dt);
        ProcessKillSpeedDecay(dt);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_isDead)
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

    // --- Journal ---

    private void ToggleJournal()
    {
        UI.JournalScreen journal = GetNodeOrNull<UI.JournalScreen>("/root/Main/JournalScreen");
        journal?.Toggle();
    }

    // --- World Bounds ---

    private WorldSetup _worldSetup;
    private float _worldBoundsRadius = -1f;

    /// <summary>
    /// Empêche le joueur de sortir du monde circulaire.
    /// La réalité s'arrête au-delà : barrière invisible de sécurité en plus des tiles d'eau.
    /// </summary>
    private void ClampToWorldBounds()
    {
        if (_worldBoundsRadius < 0f)
            CacheWorldBounds();

        if (_worldBoundsRadius <= 0f)
            return;

        float dist = GlobalPosition.Length();
        if (dist > _worldBoundsRadius)
            GlobalPosition = GlobalPosition.Normalized() * _worldBoundsRadius;
    }

    private void CacheWorldBounds()
    {
        _worldSetup = GetNodeOrNull<WorldSetup>("/root/Main");
        if (_worldSetup?.Generator == null)
            return;

        int mapRadius = _worldSetup.Generator.MapRadius;
        _worldBoundsRadius = (mapRadius - 3) * 32f;
    }

    private bool IsOnWater()
    {
        if (_worldSetup == null)
            CacheWorldBounds();
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

        foreach (LootResolver.LootResult loot in loots)
        {
            switch (loot.Type)
            {
                case "resource":
                    _inventory?.Add(loot.ItemId, loot.Amount);
                    _eventBus?.EmitSignal(EventBus.SignalName.LootReceived,
                        loot.Type, loot.ItemId, loot.Amount);
                    break;
                case "xp":
                    _eventBus?.EmitSignal(EventBus.SignalName.XpGained, (float)loot.Amount);
                    break;
                case "perk":
                    _eventBus?.EmitSignal(EventBus.SignalName.LootReceived,
                        loot.Type, loot.ItemId, loot.Amount);
                    break;
                case "souvenir":
                    _eventBus?.EmitSignal(EventBus.SignalName.LootReceived,
                        loot.Type, loot.ItemId, loot.Amount);
                    break;
            }
        }
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
        _attackTimer.Stop();
        CancelHarvest();
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

    private void OnAttackTimerTimeout()
    {
        if (_isDead)
            return;

        if (_equippedWeapon == null)
            return;

        string type = _equippedWeapon.Type?.ToLower() ?? "ranged";
        string pattern = _equippedWeapon.AttackPattern?.ToLower() ?? "linear";

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

        for (int i = 0; i < totalProjectiles; i++)
        {
            Node2D target = targets[i % targets.Count];
            Vector2 direction = (target.GlobalPosition - GlobalPosition).Normalized();
            bool isCrit = _critChance > 0f && GD.Randf() < _critChance;
            float effectiveDamage = isCrit ? baseDamage * _critMultiplier : baseDamage;
            SpawnProjectile(direction, effectiveDamage, projectileSpeed, range, totalPierce, isCrit);
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

    private void SpawnProjectile(Vector2 direction, float damage, float speed, float range, int pierce, bool isCrit)
    {
        Projectile projectile = _projectileScene.Instantiate<Projectile>();
        projectile.GlobalPosition = GlobalPosition;
        projectile.Speed = speed;
        projectile.MaxLifetime = Mathf.Clamp(range / Mathf.Max(speed, 1f), 0.2f, 4f);
        projectile.Initialize(direction, damage, pierce, isCrit, this);

        Polygon2D visual = projectile.GetNodeOrNull<Polygon2D>("Visual");
        if (visual != null)
            visual.Color = GetWeaponProjectileColor();

        GetTree().CurrentScene.AddChild(projectile);
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
        if (_equippedWeapon == null || !_equippedWeapon.Stats.TryGetValue(key, out float value))
            return fallback;

        return value;
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

        float weaponAttackSpeed = GetWeaponStat("attack_speed", 1f);
        float attacksPerSecond = AttackSpeed * weaponAttackSpeed * mult;
        _attackTimer.WaitTime = 1.0f / Mathf.Max(0.05f, attacksPerSecond);
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
