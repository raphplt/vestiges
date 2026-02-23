using Godot;
using Vestiges.Base;
using Vestiges.Combat;
using Vestiges.Infrastructure;

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
    private PackedScene _projectileScene;
    private Timer _attackTimer;
    private Polygon2D _visual;
    private Color _originalColor;

    // Perk modifiers
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

    // Harvest system
    private ResourceNode _harvestTarget;
    private float _harvestProgress;
    private bool _isHarvesting;
    private ProgressBar _harvestBar;
    private Inventory _inventory;

    public float CurrentHp => _currentHp;
    public float EffectiveMaxHp => MaxHp + _bonusMaxHp;
    public float EffectiveAttackRange => AttackRange * _attackRangeMultiplier;
    public float StructureHpMultiplier => _structureHpMultiplier;
    public float CraftSpeedMultiplier => _craftSpeedMultiplier;
    public bool IsDead => _isDead;
    public bool IsHarvesting => _isHarvesting;
    public string CharacterId => _characterId;

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
    }

    public void InitializeCharacter(CharacterData data)
    {
        _characterId = data.Id;

        Speed = data.BaseStats.Speed;
        AttackDamage = data.BaseStats.AttackDamage;
        AttackSpeed = data.BaseStats.AttackSpeed;
        AttackRange = data.BaseStats.AttackRange;
        MaxHp = data.BaseStats.MaxHp;
        BaseRegenRate = data.BaseStats.RegenRate;
        InteractRange = data.BaseStats.InteractRange;

        _currentHp = MaxHp;

        _attackTimer.WaitTime = 1.0 / AttackSpeed;

        _visual.Color = data.VisualColor;
        _originalColor = data.VisualColor;

        GD.Print($"[Player] Initialized as {data.Name} (HP:{MaxHp}, ATK:{AttackDamage}, SPD:{Speed})");
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
            Vector2 isoDir = CartesianToIsometric(inputDir);
            Velocity = isoDir * (Speed * _speedMultiplier);
        }
        else
        {
            Velocity = Vector2.Zero;
        }

        MoveAndSlide();
        ApplyRegen(dt);
        ProcessHarvest(dt);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_isDead)
            return;

        if (@event.IsActionPressed("interact"))
        {
            if (_isHarvesting)
            {
                CancelHarvest();
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
                break;
            case "attack_speed":
                if (modifierType == "multiplicative")
                {
                    _attackSpeedMultiplier *= value;
                    _attackTimer.WaitTime = 1.0 / (AttackSpeed * _attackSpeedMultiplier);
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
        }
    }

    public void Heal(float amount)
    {
        if (_isDead || amount <= 0)
            return;

        _currentHp = Mathf.Min(_currentHp + amount, EffectiveMaxHp);

        EventBus eventBus = GetNode<EventBus>("/root/EventBus");
        eventBus.EmitSignal(EventBus.SignalName.PlayerDamaged, _currentHp, EffectiveMaxHp);
    }

    public void TakeDamage(float damage)
    {
        if (_currentHp <= 0)
            return;

        _currentHp -= damage;
        CancelHarvest();
        HitFlash();

        EventBus eventBus = GetNode<EventBus>("/root/EventBus");
        eventBus.EmitSignal(EventBus.SignalName.PlayerDamaged, _currentHp, EffectiveMaxHp);

        if (_currentHp <= 0)
        {
            _currentHp = 0;
            Die();
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
            CacheInventory();
            _inventory?.Add(resourceId, amount);
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

    private void CacheInventory()
    {
        if (_inventory != null)
            return;

        _inventory = GetNodeOrNull<Inventory>("Inventory");
    }

    // --- Combat ---

    private void Die()
    {
        _isDead = true;
        Velocity = Vector2.Zero;
        _attackTimer.Stop();
        CancelHarvest();
        RemoveFromGroup("player");

        EventBus eventBus = GetNode<EventBus>("/root/EventBus");
        eventBus.EmitSignal(EventBus.SignalName.EntityDied, this);

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

        EventBus eventBus = GetNode<EventBus>("/root/EventBus");
        eventBus.EmitSignal(EventBus.SignalName.PlayerDamaged, _currentHp, EffectiveMaxHp);
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

        int totalProjectiles = 1 + _extraProjectiles;
        System.Collections.Generic.List<Node2D> targets = FindNearestEnemies(totalProjectiles);
        if (targets.Count == 0)
            return;

        float effectiveDamage = AttackDamage * _damageMultiplier;

        for (int i = 0; i < totalProjectiles; i++)
        {
            Node2D target = targets[i % targets.Count];
            Vector2 direction = (target.GlobalPosition - GlobalPosition).Normalized();

            Projectile projectile = _projectileScene.Instantiate<Projectile>();
            projectile.GlobalPosition = GlobalPosition;
            projectile.Initialize(direction, effectiveDamage);
            GetTree().CurrentScene.AddChild(projectile);
        }
    }

    private System.Collections.Generic.List<Node2D> FindNearestEnemies(int count)
    {
        Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemies");
        System.Collections.Generic.List<(Node2D enemy, float dist)> inRange = new();

        float effectiveRange = EffectiveAttackRange;

        foreach (Node node in enemies)
        {
            if (node is Node2D enemy)
            {
                float dist = GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (dist < effectiveRange)
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
