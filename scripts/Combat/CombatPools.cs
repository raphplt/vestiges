using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Pools des objets de combat fréquents de la run : projectiles du joueur et des ennemis, chiffres de dégâts,
/// effets de forme et étincelles. Trié en Y avec les entités ; une instance par scène de run.
/// </summary>
public partial class CombatPools : Node2D
{
    public static CombatPools Instance { get; private set; }

    private NodePool<EnemyProjectile> _enemyProjectiles;
    private NodePool<Projectile> _playerProjectiles;
    private NodePool<DamageNumber> _damageNumbers;
    private NodePool<PixelFx> _pixelFx;

    /// <summary>Étincelles et éclats de combat, tracés par un seul nœud.</summary>
    public PixelSparks Sparks { get; private set; }

    public override void _EnterTree()
    {
        Instance = this;
        YSortEnabled = true;
        PackedScene projectileScene = GD.Load<PackedScene>("res://scenes/combat/EnemyProjectile.tscn");
        PackedScene playerProjectileScene = GD.Load<PackedScene>("res://scenes/combat/Projectile.tscn");
        PackedScene damageNumberScene = GD.Load<PackedScene>("res://scenes/combat/DamageNumber.tscn");
        _enemyProjectiles = new NodePool<EnemyProjectile>(this, () =>
        {
            EnemyProjectile projectile = projectileScene.Instantiate<EnemyProjectile>();
            projectile.SetRelease(_enemyProjectiles.Return);
            return projectile;
        });
        _playerProjectiles = new NodePool<Projectile>(this, () =>
        {
            Projectile projectile = playerProjectileScene.Instantiate<Projectile>();
            projectile.SetRelease(_playerProjectiles.Return);
            return projectile;
        });
        _damageNumbers = new NodePool<DamageNumber>(this, () =>
        {
            DamageNumber number = damageNumberScene.Instantiate<DamageNumber>();
            number.SetRelease(_damageNumbers.Return);
            return number;
        });
        _pixelFx = new NodePool<PixelFx>(this, () => PixelFx.Create(_pixelFx.Return));
        Sparks = new PixelSparks { Name = "PixelSparks" };
        AddChild(Sparks);
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public EnemyProjectile TakeEnemyProjectile() => _enemyProjectiles.Take();

    public Projectile TakePlayerProjectile() => _playerProjectiles.Take();

    public void ShowDamageNumber(Vector2 position, float damage, bool isCrit)
    {
        _damageNumbers.Take().Play(position, damage, isCrit);
    }

    /// <summary>Étoile d'impact au point touché, en trois poses.</summary>
    public void ShowHitFlash(Vector2 position)
    {
        if (CombatFxSettings.ParticleLevel == ParticleLevel.Off)
            return;
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Star, FxFamily.Physical, 5f, 1f, 0.1f);
        spec.Steps = 3;
        spec.FadeTail = 0.34f;
        spec.ZIndex = 2;
        PlayFx(position, spec, FxOwner.Player);
    }

    /// <summary>Éclat vert-acide au départ d'un tir ennemi.</summary>
    public void ShowMuzzleFlash(Vector2 position, float angle)
    {
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Star, FxFamily.Hostile, 5f, 1f, 0.1f);
        spec.Steps = 3;
        spec.FadeTail = 0.34f;
        spec.ZIndex = 2;
        PlayFx(position, spec, FxOwner.Enemy);
    }

    /// <summary>Éclaboussure d'un projectile ennemi qui touche : étoile et gouttes acides.</summary>
    public void ShowEnemyImpact(Vector2 position, Vector2 direction)
    {
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Star, FxFamily.Hostile, 6f, 1f, 0.12f);
        spec.Steps = 3;
        spec.FadeTail = 0.34f;
        spec.ZIndex = 2;
        PlayFx(position, spec, FxOwner.Enemy);
        Sparks.Emit(position, new SparkBurst
        {
            Family = FxFamily.Hostile,
            Owner = FxOwner.Enemy,
            Count = 5,
            Direction = -direction,
            Spread = 2.2f,
            SpeedMin = 40f,
            SpeedMax = 90f,
            LifeMin = 0.25f,
            LifeMax = 0.4f,
            Ballistic = true,
            Size = 1,
        });
    }

    /// <summary>
    /// Effet de forme en pixel art. Ceux du joueur respectent le réglage « Effets d'attaque » ;
    /// ceux des ennemis restent toujours affichés, seule leur opacité se règle.
    /// </summary>
    public PixelFx PlayFx(Vector2 position, in PixelFxSpec spec, FxOwner owner, Node2D follow = null)
    {
        if (owner == FxOwner.Player && !CombatFxSettings.PlayerAttackFx)
            return null;
        float opacity = owner == FxOwner.Player ? CombatFxSettings.PlayerOpacity : CombatFxSettings.EnemyOpacity;
        PixelFx fx = _pixelFx.Take();
        fx.Play(position, spec, opacity, follow);
        return fx;
    }

    public void EmitSparks(Vector2 position, in SparkBurst burst) => Sparks.Emit(position, burst);

    /// <summary>Objets créés depuis le début de la run, tous pools confondus (bancs de mesure).</summary>
    public int CreatedCount => _enemyProjectiles.Created + _playerProjectiles.Created + _damageNumbers.Created + _pixelFx.Created;
}
