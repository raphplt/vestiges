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
    private NodePool<DeathFx> _deathFx;
    private NodePool<XpOrb> _xpOrbs;

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
        _deathFx = new NodePool<DeathFx>(this, () => DeathFx.Create(_deathFx.Return));
        PackedScene xpOrbScene = GD.Load<PackedScene>("res://scenes/combat/XpOrb.tscn");
        _xpOrbs = new NodePool<XpOrb>(this, () =>
        {
            XpOrb orb = xpOrbScene.Instantiate<XpOrb>();
            orb.SetRelease(_xpOrbs.Return);
            return orb;
        });
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

    /// <summary>
    /// Lance un chiffre de dégâts ; le rendu permet à la cible d'y additionner ses coups suivants.
    /// Nul quand le budget de la frame est épuisé ; un critique passe toujours.
    /// </summary>
    public DamageNumber ShowDamageNumber(Vector2 position, float damage, bool isCrit)
    {
        if (!isCrit && !FxBudget.TryTake(FxBudgetKind.Numbers))
            return null;
        DamageNumber number = _damageNumbers.Take();
        number.Play(position, damage, isCrit);
        return number;
    }

    /// <summary>Étoile d'impact au point touché, en trois poses.</summary>
    public void ShowHitFlash(Vector2 position)
    {
        if (!FxBudget.TryTake(FxBudgetKind.Shapes))
            return;
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Star, FxFamily.Physical, 5f, 1f, 0.1f);
        spec.Steps = 3;
        spec.FadeTail = 0.34f;
        spec.ZIndex = 2;
        PlayFx(position, spec, FxOwner.Player);
    }

    /// <summary>Éclat au départ d'un tir ennemi, aux couleurs de son projectile.</summary>
    public void ShowMuzzleFlash(Vector2 position, FxFamily family)
    {
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Star, family, 5f, 1f, 0.1f);
        spec.Steps = 3;
        spec.FadeTail = 0.34f;
        spec.ZIndex = 2;
        PlayFx(position, spec, FxOwner.Enemy);
    }

    /// <summary>Éclaboussure d'un projectile ennemi qui touche : étoile et gouttes de sa couleur.</summary>
    public void ShowEnemyImpact(Vector2 position, Vector2 direction, FxFamily family)
    {
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Star, family, 6f, 1f, 0.12f);
        spec.Steps = 3;
        spec.FadeTail = 0.34f;
        spec.ZIndex = 2;
        PlayFx(position, spec, FxOwner.Enemy);
        Sparks.Emit(position, new SparkBurst
        {
            Family = family,
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

    /// <summary>
    /// Mort d'une créature : éclats sombres qui s'élèvent, nuage de dissolution, flaque irisée
    /// (<paramref name="poolScale"/> ≤ 0 : pas de flaque). Rien quand les particules sont coupées. Au-delà du budget
    /// de la frame, une mort ordinaire perd nuage et flaque (le sprite se dissout toujours) ; une mort
    /// <paramref name="signature"/> (élite, mini-boss) garde tout.
    /// </summary>
    public void ShowDeath(Vector2 position, int shards, float spread, float poolScale, bool signature = false,
                          Vector2 direction = default)
    {
        if (CombatFxSettings.ParticleLevel == ParticleLevel.Off)
            return;
        // Moitié des éclats qui s'élèvent vers le Néant, moitié projetés dans le sens du dernier coup (plan 02 J2).
        int thrown = direction == Vector2.Zero ? 0 : shards / 2;
        if (shards - thrown > 0)
        {
            Sparks.Emit(position + new Vector2(0f, -6f), new SparkBurst
            {
                Family = FxFamily.Void,
                Owner = FxOwner.Enemy,
                Count = shards - thrown,
                Direction = Vector2.Up,
                Spread = spread,
                SpeedMin = 20f,
                SpeedMax = 60f,
                LifeMin = 0.4f,
                LifeMax = 0.7f,
                Size = 1,
                Decorative = !signature,
            });
        }
        if (thrown > 0)
        {
            Sparks.Emit(position + new Vector2(0f, -8f), new SparkBurst
            {
                Family = FxFamily.Void,
                Owner = FxOwner.Enemy,
                Count = thrown,
                Direction = direction,
                Spread = 1.1f,
                SpeedMin = 60f,
                SpeedMax = 140f,
                LifeMin = 0.35f,
                LifeMax = 0.6f,
                Ballistic = true,
                Size = signature ? 2 : 1,
                Decorative = !signature,
            });
        }
        if (signature)
            ShowDeathSignature(position);
        if (signature || FxBudget.TryTake(FxBudgetKind.Deaths))
            _deathFx.Take().Play(position, poolScale, direction);
    }

    /// <summary>Mort d'élite ou de Souverain : onde au sol qui s'élargit et éclair bref sur le corps.</summary>
    private void ShowDeathSignature(Vector2 position)
    {
        PixelFxSpec ring = PixelFxSpec.Of(PixelFxShape.Ring, FxFamily.Void, 46f, 3f, 0.45f);
        ring.Squash = 2f;
        ring.Steps = 7;
        ring.FadeTail = 0.4f;
        ring.ZIndex = -1;
        PlayFx(position, ring, FxOwner.Enemy);
        PixelFxSpec flash = PixelFxSpec.Of(PixelFxShape.Star, FxFamily.Pale, 14f, 1f, 0.16f);
        flash.Steps = 3;
        flash.FadeTail = 0.4f;
        flash.ZIndex = 2;
        PlayFx(position + new Vector2(0f, -12f), flash, FxOwner.Enemy);
    }

    /// <summary>
    /// Pose une orbe d'XP recyclée. Différé : une mort survient souvent pendant un rappel de la physique,
    /// où une zone ne peut pas entrer dans l'arbre ni changer de surveillance.
    /// </summary>
    public void SpawnXpOrb(Vector2 position, float xpValue, Vector2? hopFrom = null)
    {
        Callable.From(() => _xpOrbs.Take().Launch(position, xpValue, hopFrom)).CallDeferred();
    }

    /// <summary>Petite gerbe à la collecte d'une orbe d'XP.</summary>
    public void ShowXpCollect(Vector2 position)
    {
        Sparks.Emit(position, new SparkBurst
        {
            Family = FxFamily.Essence,
            Owner = FxOwner.Enemy,
            Count = 4,
            Direction = Vector2.Up,
            Spread = Mathf.Tau,
            SpeedMin = 25f,
            SpeedMax = 50f,
            LifeMin = 0.2f,
            LifeMax = 0.35f,
            Size = 1,
            Decorative = true,
        });
    }

    /// <summary>Objets créés depuis le début de la run, tous pools confondus (bancs de mesure).</summary>
    public int CreatedCount => _enemyProjectiles.Created + _playerProjectiles.Created + _damageNumbers.Created + _pixelFx.Created
        + _deathFx.Created + _xpOrbs.Created;
}
