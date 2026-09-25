using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Pools des objets de combat fréquents de la run : projectiles ennemis, chiffres de dégâts,
/// flashs d'impact et de tir. Trié en Y avec les entités ; une instance par scène de run.
/// </summary>
public partial class CombatPools : Node2D
{
    public static CombatPools Instance { get; private set; }

    private NodePool<EnemyProjectile> _enemyProjectiles;
    private NodePool<DamageNumber> _damageNumbers;
    private NodePool<FlashSprite> _hitFlashes;
    private NodePool<FlashSprite> _muzzleFlashes;

    public override void _EnterTree()
    {
        Instance = this;
        YSortEnabled = true;
        PackedScene projectileScene = GD.Load<PackedScene>("res://scenes/combat/EnemyProjectile.tscn");
        PackedScene damageNumberScene = GD.Load<PackedScene>("res://scenes/combat/DamageNumber.tscn");
        _enemyProjectiles = new NodePool<EnemyProjectile>(this, () =>
        {
            EnemyProjectile projectile = projectileScene.Instantiate<EnemyProjectile>();
            projectile.SetRelease(_enemyProjectiles.Return);
            return projectile;
        });
        _damageNumbers = new NodePool<DamageNumber>(this, () =>
        {
            DamageNumber number = damageNumberScene.Instantiate<DamageNumber>();
            number.SetRelease(_damageNumbers.Return);
            return number;
        });
        _hitFlashes = new NodePool<FlashSprite>(this, () => FlashSprite.CreateHitFlash(_hitFlashes.Return));
        _muzzleFlashes = new NodePool<FlashSprite>(this, () => FlashSprite.CreateMuzzleFlash(_muzzleFlashes.Return));
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public EnemyProjectile TakeEnemyProjectile() => _enemyProjectiles.Take();

    public void ShowDamageNumber(Vector2 position, float damage, bool isCrit)
    {
        _damageNumbers.Take().Play(position, damage, isCrit);
    }

    public void ShowHitFlash(Vector2 position)
    {
        if (VfxFactory.CurrentParticleLevel != ParticleLevel.Off)
            _hitFlashes.Take().Play(position, 0f);
    }

    public void ShowMuzzleFlash(Vector2 position, float angle)
    {
        _muzzleFlashes.Take().Play(position, angle);
    }

    /// <summary>Objets créés depuis le début de la run, tous pools confondus (bancs de mesure).</summary>
    public int CreatedCount => _enemyProjectiles.Created + _damageNumbers.Created + _hitFlashes.Created + _muzzleFlashes.Created;
}
