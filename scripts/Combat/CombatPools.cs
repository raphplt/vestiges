using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Pools des objets de combat fréquents de la run : projectiles du joueur et des ennemis, chiffres de dégâts,
/// effets de forme et étincelles. Trié en Y avec les entités ; une instance par scène de run.
/// </summary>
public partial class CombatPools : Node2D
{
    public static CombatPools Instance { get; private set; }

    private static ChalkShapeConfig _chalkConfig;
    private static bool _chalkConfigTried;

    private const int MaxKnockbackDustPerFrame = 6;
    private ulong _dustFrame;
    private int _dustThisFrame;

    private NodePool<EnemyProjectile> _enemyProjectiles;
    private NodePool<Projectile> _playerProjectiles;
    private NodePool<DamageNumber> _damageNumbers;
    private NodePool<PixelFx> _pixelFx;
    private NodePool<DeathFx> _deathFx;
    private NodePool<ProjectileImpact> _projectileImpacts;
    private NodePool<XpOrb> _xpOrbs;
    private NodePool<ChalkDrawing> _chalkDrawings;
    // Orbes endormies loin du joueur : une ronde toutes les 0,25 s réveille celles dont il se rapproche.
    private const float WakeCheckInterval = 0.25f;
    private const float WakeHysteresis = 100f;
    private readonly List<(XpOrb Orb, Vector2 Position, int Token)> _sleepingOrbs = new();
    private float _wakeTimer;
    private Player _player;
    private GroundFire _groundFire;

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
        _projectileImpacts = new NodePool<ProjectileImpact>(this, () => ProjectileImpact.Create(_projectileImpacts.Return));
        _chalkDrawings = new NodePool<ChalkDrawing>(this, () => ChalkDrawing.Create(_chalkDrawings.Return));
        PackedScene xpOrbScene = GD.Load<PackedScene>("res://scenes/combat/XpOrb.tscn");
        _xpOrbs = new NodePool<XpOrb>(this, () =>
        {
            XpOrb orb = xpOrbScene.Instantiate<XpOrb>();
            orb.SetRelease(_xpOrbs.Return);
            return orb;
        });
        _groundFire = new GroundFire();
        Sparks = new PixelSparks { Name = "PixelSparks" };
        AddChild(Sparks);
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddSleepingOrb(XpOrb orb)
    {
        _sleepingOrbs.Add((orb, orb.GlobalPosition, orb.SleepToken));
    }

    /// <summary>Flaque de feu : dégâts au sol pendant <paramref name="duration"/> secondes, zone tramée qui la montre.</summary>
    public void AddGroundFire(Vector2 position, float damage, float duration, float radius, float burnSeconds, AttackContext source = default)
    {
        _groundFire.Add(position, damage, duration, radius, burnSeconds, source);
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Zone, FxFamily.Fire, radius, 1f, duration);
        spec.Squash = Iso.GroundSquash;
        spec.FillDensity = 0.3f;
        spec.ProgressFill = false;
        spec.Steps = 8;
        spec.FadeTail = 0.4f;
        spec.ZIndex = -1;
        PlayFx(position, spec, FxOwner.Player);
    }

    public override void _Process(double delta)
    {
        _groundFire.Process((float)delta);
        if (_sleepingOrbs.Count == 0)
            return;
        _wakeTimer -= (float)delta;
        if (_wakeTimer > 0f)
            return;
        _wakeTimer = WakeCheckInterval;
        if (_player == null || !IsInstanceValid(_player))
        {
            _player = GetTree().GetFirstNodeInGroup("player") as Player;
            if (_player == null)
                return;
        }
        Vector2 playerPosition = _player.GlobalPosition;
        float wakeRadius = XpOrb.SleepRadius(_player) - WakeHysteresis;
        float wakeRadiusSq = wakeRadius * wakeRadius;
        for (int i = _sleepingOrbs.Count - 1; i >= 0; i--)
        {
            (XpOrb orb, Vector2 position, int token) = _sleepingOrbs[i];
            // Ramassée en dormant, ou réutilisée depuis : l'entrée ne vaut plus rien.
            bool stillAsleep = orb.IsAsleep && orb.SleepToken == token;
            bool trailBound = false;
            if (stillAsleep && position.DistanceSquaredTo(playerPosition) > wakeRadiusSq)
            {
                // Sillage : une orbe endormie dans le couloir récent du joueur se réveille pour le rejoindre.
                trailBound = XpTrail.Any && XpTrail.Covers(position);
                if (!trailBound)
                    continue;
            }
            if (stillAsleep)
                orb.Wake(trailBound);
            int last = _sleepingOrbs.Count - 1;
            _sleepingOrbs[i] = _sleepingOrbs[last];
            _sleepingOrbs.RemoveAt(last);
        }
    }

    public EnemyProjectile TakeEnemyProjectile() => _enemyProjectiles.Take();

    public void ShowProjectileImpact(Vector2 position, ProjectileSprites.SpriteSet sprites)
    {
        if (sprites != null)
            _projectileImpacts.Take().Play(position, sprites);
    }

    public Projectile TakePlayerProjectile() => _playerProjectiles.Take();

    /// <summary>
    /// Lance un chiffre de dégâts ; le rendu permet à la cible d'y additionner ses coups suivants.
    /// Nul quand le budget de la frame est épuisé ; un critique passe toujours.
    /// </summary>
    public DamageNumber ShowDamageNumber(Vector2 position, float damage, bool isCrit, bool isCarried = false)
    {
        if (!isCrit && !isCarried && !FxBudget.TryTake(FxBudgetKind.Numbers))
            return null;
        DamageNumber number = _damageNumbers.Take();
        number.Play(position, damage, isCrit, isCarried);
        return number;
    }

    /// <summary>
    /// Forme des Craies tirée au hasard parmi celles des réglages, à la taille réelle de la zone. Effet d'attaque du
    /// joueur : suit son réglage et le budget des formes.
    /// </summary>
    public void ShowChalkShape(Vector2 center, float radius)
    {
        if (!_chalkConfigTried)
        {
            _chalkConfigTried = true;
            if (!ChalkShapeConfig.TryLoad(out _chalkConfig, out string error))
                GD.PushError($"[CombatPools] {error}");
        }
        if (_chalkConfig == null || !CombatFxSettings.PlayerAttackFx || !FxBudget.TryTake(FxBudgetKind.Shapes))
            return;
        (string shape, FxFamily family) = _chalkConfig.Shapes[(int)(GD.Randi() % (uint)_chalkConfig.Shapes.Count)];
        _chalkDrawings.Take().Play(center, radius, shape, family, _chalkConfig, CombatFxSettings.PlayerOpacity);
    }

    /// <summary>
    /// Recul d'une créature (plan 27 V2d) : un peu de poussière part du sol, à l'opposé du coup. Fioriture d'attaque du
    /// joueur, au budget des étincelles.
    /// </summary>
    public void EmitKnockbackDust(Vector2 feet, Vector2 direction)
    {
        // Une onde repousse toute une foule d'un coup : quelques nuages suffisent, le budget reste aux gerbes d'impact.
        ulong frame = Engine.GetProcessFrames();
        if (frame != _dustFrame)
        {
            _dustFrame = frame;
            _dustThisFrame = 0;
        }
        if (++_dustThisFrame > MaxKnockbackDustPerFrame)
            return;
        Sparks.Emit(feet, new SparkBurst
        {
            Family = FxFamily.Stone,
            Owner = FxOwner.Player,
            Count = 3,
            Direction = -direction,
            Spread = 1.2f,
            SpeedMin = 15f,
            SpeedMax = 35f,
            LifeMin = 0.2f,
            LifeMax = 0.35f,
            Size = 1,
        });
    }

    /// <summary>Dégâts reçus par le joueur (plan 27 V3a) : information, jamais écartée par le budget.</summary>
    public void ShowReceivedNumber(Vector2 position, float damage, Color color)
    {
        _damageNumbers.Take().PlayReceived(position, damage, color);
    }

    /// <summary>Chiffre d'un tic de brûlure ou de saignement (plan 27 V2a), au budget des chiffres.</summary>
    public void ShowTickNumber(Vector2 position, float damage, StatusKind kind)
    {
        if (!FxBudget.TryTake(FxBudgetKind.Numbers))
            return;
        _damageNumbers.Take().PlayTick(position, damage, kind);
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
        float opacity = owner switch
        {
            FxOwner.Player => CombatFxSettings.PlayerOpacity,
            FxOwner.Enemy => CombatFxSettings.EnemyOpacity,
            _ => 1f,
        };
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

    /// <summary>
    /// Explosion d'une créature instable : zone au sol du rayon réel des dégâts (ellipse 2:1, mesurée au sol comme eux),
    /// onde qui s'élargit, éclair et gerbe de braises. Recyclé, comme tous les effets de combat.
    /// </summary>
    public void ShowExplosion(Vector2 position, float radius)
    {
        PixelFxSpec zone = PixelFxSpec.Of(PixelFxShape.Zone, FxFamily.Fire, radius, 1f, 0.35f);
        zone.Squash = Iso.GroundSquash;
        zone.ProgressFill = false;
        zone.FillDensity = 0.5f;
        zone.Steps = 4;
        zone.FadeTail = 0.5f;
        zone.ZIndex = -1;
        PlayFx(position, zone, FxOwner.Enemy);
        PixelFxSpec ring = PixelFxSpec.Of(PixelFxShape.Ring, FxFamily.Fire, radius, 2f, 0.3f);
        ring.Squash = Iso.GroundSquash;
        ring.Steps = 5;
        ring.FadeTail = 0.4f;
        ring.ZIndex = -1;
        PlayFx(position, ring, FxOwner.Enemy);
        PixelFxSpec flash = PixelFxSpec.Of(PixelFxShape.Star, FxFamily.Fire, 12f, 1f, 0.14f);
        flash.Steps = 3;
        flash.FadeTail = 0.4f;
        flash.ZIndex = 2;
        PlayFx(position + new Vector2(0f, -10f), flash, FxOwner.Enemy);
        Sparks.Emit(position + new Vector2(0f, -6f), new SparkBurst
        {
            Family = FxFamily.Fire,
            Owner = FxOwner.Enemy,
            Count = 14,
            Direction = Vector2.Zero,
            Spread = Mathf.Tau,
            SpeedMin = 60f,
            SpeedMax = 140f,
            LifeMin = 0.2f,
            LifeMax = 0.4f,
            Ballistic = true,
            Size = 1,
        });
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
        XpDropped += xpValue;
        Callable.From(() => _xpOrbs.Take().Launch(position, xpValue, hopFrom)).CallDeferred();
    }

    /// <summary>Petite gerbe à la collecte d'une orbe d'XP.</summary>
    public void ShowXpCollect(Vector2 position)
    {
        Sparks.Emit(position, new SparkBurst
        {
            Family = FxFamily.Essence,
            Owner = FxOwner.World,
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

    /// <summary>Orbes d'XP au sol, pas encore ramassées (bancs de mesure).</summary>
    public int XpOrbsOnGround => _xpOrbs.InUse;
    /// <summary>XP lâchée depuis le début de la run, ramassée ou non : la borne d'un joueur qui ramasse tout (mesures).</summary>
    public double XpDropped { get; private set; }

    /// <summary>Objets créés depuis le début de la run, tous pools confondus (bancs de mesure).</summary>
    public int CreatedCount => _enemyProjectiles.Created + _playerProjectiles.Created + _damageNumbers.Created + _pixelFx.Created
        + _deathFx.Created + _xpOrbs.Created + _projectileImpacts.Created;
}
