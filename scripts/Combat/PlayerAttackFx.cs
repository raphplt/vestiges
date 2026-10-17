using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Mise en scène des attaques du joueur (plan 08, effets d'attaque) : forme du coup selon le style
/// de l'arme (`fx.style` dans weapons.json), étincelles à l'impact, écrasement bref du sprite et
/// secousse réservée aux armes lourdes. Tout passe par CombatPools : aucun nœud créé par coup.
/// </summary>
public sealed class PlayerAttackFx
{
    /// <summary>Hauteur du torse au-dessus du point au sol : les coups partent du buste, pas des pieds.</summary>
    private const float TorsoHeight = 11f;
    /// <summary>Plafond de gerbes d'impact par frame : une zone qui touche 80 ennemis n'en dessine que quelques-unes.</summary>
    private const int MaxHitBurstsPerFrame = 10;
    /// <summary>Gerbes des coups secondaires et des objets : plafond à part, pour ne pas priver un coup direct de la sienne.</summary>
    private const int MaxSecondaryBurstsPerFrame = 10;

    private readonly Node2D _owner;
    private readonly AnimatedSprite2D _sprite;
    private Tween _squashTween;
    private ulong _hitFrame;
    private int _hitBurstsThisFrame;
    private int _secondaryBurstsThisFrame;
    private ulong _timeFieldFrame;
    private PixelFx _cone;
    // Ondes en attente d'une frappe circulaire répétée (plan 21 G6g) : liste réutilisée, aucune allocation par coup.
    private readonly List<PendingMelee> _pendingMelee = new();

    /// <summary>Décalage d'élan du sprite lors d'une frappe ou d'un recul de tir (plan 54 G1), résorbé chaque tick.</summary>
    public Vector2 LungeOffset { get; private set; }

    private struct PendingMelee
    {
        public WeaponData Weapon;
        public AttackPatternKind Pattern;
        public Vector2 Direction;
        public float Range;
        public float ArcAngle;
        public float Delay;
        public Vector2? Origin;
    }

    public PlayerAttackFx(Node2D owner, AnimatedSprite2D sprite)
    {
        _owner = owner;
        _sprite = sprite;
    }

    private static CombatPools Pools => CombatPools.Instance;

    /// <summary>Liseré clair et halo des projectiles du joueur, partagé par tous (DECISIONS §53, plan 21 F5).</summary>
    public static ShaderMaterial ProjectileMaterial { get; } =
        new() { Shader = GD.Load<Shader>("res://assets/shaders/player_projectile.gdshader") };

    public static FxFamily FamilyOf(WeaponData weapon)
    {
        return PixelPalette.ParseFamily(weapon?.Fx.Family, PixelPalette.FamilyForDamageType(weapon?.DamageType));
    }

    /// <summary>Coup de mêlée rejoué après <paramref name="delay"/> secondes : ondes successives d'une frappe en cercle.</summary>
    public void PlayMeleeLater(WeaponData weapon, AttackPatternKind pattern, Vector2 direction, float range, float arcAngle, float delay, Vector2? origin = null)
    {
        _pendingMelee.Add(new PendingMelee
        {
            Weapon = weapon,
            Pattern = pattern,
            Direction = direction,
            Range = range,
            ArcAngle = arcAngle,
            Delay = delay,
            Origin = origin,
        });
    }

    /// <summary>Avance les coups en attente et résorbe l'élan d'attaque ; à appeler à chaque frame physique du joueur.</summary>
    public void Tick(float delta)
    {
        if (LungeOffset != Vector2.Zero)
            LungeOffset = LungeOffset.MoveToward(Vector2.Zero, 28f * delta);

        for (int i = _pendingMelee.Count - 1; i >= 0; i--)
        {
            PendingMelee pending = _pendingMelee[i];
            pending.Delay -= delta;
            if (pending.Delay > 0f)
            {
                _pendingMelee[i] = pending;
                continue;
            }
            _pendingMelee.RemoveAt(i);
            PlayMelee(pending.Weapon, pending.Pattern, pending.Direction, pending.Range, pending.ArcAngle, pending.Origin);
        }
    }

    /// <summary>
    /// Coup de mêlée : une forme par frappe (les frappes supplémentaires appellent plusieurs fois). La forme suit
    /// l'attaque : trait pour l'attaque droite, traînée pour l'arc et l'onde, câble pour la Rallonge.
    /// Son bord passe à l'allonge (<paramref name="range"/>) et son ouverture à <paramref name="arcAngle"/>,
    /// sans plafond : la frappe ne touche jamais hors de ce qui est dessiné (<see cref="MeleeStrikeShape"/>).
    /// </summary>
    public void PlayMelee(WeaponData weapon, AttackPatternKind pattern, Vector2 direction, float range, float arcAngle, Vector2? origin = null)
    {
        if (Pools == null || !CombatFxSettings.PlayerAttackFx)
            return;
        FxFamily family = FamilyOf(weapon);
        // Les coups partent du buste et la touche se mesure aux pieds : le buste d'une cible est à la même distance.
        Vector2 ground = origin ?? _owner.GlobalPosition;
        bool followOwner = origin == null;
        Vector2 torso = ground + new Vector2(0f, -TorsoHeight);
        float radius = Mathf.Max(range, 22f);
        float halfArc = Mathf.DegToRad(Mathf.Clamp(arcAngle, 40f, 360f) * 0.5f);
        if (MeleeStrikeShape.IsThrust(pattern))
        {
            PlayThrust(torso, direction, Mathf.Max(range, 26f), family, followOwner);
            return;
        }

        switch (weapon?.Fx.Style)
        {
            case "cleave":
                PlayArc(torso, direction, radius, radius * 0.55f, halfArc * 1.15f, family, 0.26f, followOwner: followOwner);
                EmitDebris(torso + direction * radius * 0.8f, direction, family, 5);
                break;
            case "smash":
                PlayArc(torso, direction, radius, radius * 0.5f, halfArc, family, 0.18f, followOwner: followOwner);
                PlaySmashImpact(ground + direction * radius * 0.75f, family, radius);
                break;
            case "whirl":
                PlayArc(torso, direction, radius, radius * 0.3f, Mathf.Pi, family, 0.28f, followOwner: followOwner);
                break;
            case "cable":
                PlayCable(torso, direction, radius, family, followOwner);
                break;
            case "bell":
                // L'anneau extérieur, 1,35 fois le premier, marque l'allonge.
                PlayBell(family, radius / 1.35f, ground);
                break;
            case "clock":
                PlayArc(torso, direction, radius, radius * 0.42f, halfArc, family, 0.22f, followOwner: followOwner);
                PlayGroundRing(ground, family, radius * 0.8f, 3f, 0.32f);
                break;
            case "void":
                PlayArc(torso, direction, radius, radius * 0.5f, halfArc, FxFamily.Void, 0.24f, followOwner: followOwner);
                EmitSparks(torso + direction * radius * 0.7f, direction, FxFamily.Void, 6, 0.9f, 60f, 130f, 2);
                break;
            default:
                PlayArc(torso, direction, radius, radius * 0.42f, halfArc, family, 0.22f, followOwner: followOwner);
                break;
        }
    }

    /// <summary>Flash de tir : courte poussée lumineuse à la sortie de l'arme.</summary>
    public void PlayMuzzle(WeaponData weapon, Vector2 direction)
    {
        if (Pools == null)
            return;
        FxFamily family = FamilyOf(weapon);
        Vector2 origin = _owner.GlobalPosition + new Vector2(0f, -TorsoHeight) + direction * 5f;
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Thrust, family, 10f, 5f, 0.09f);
        spec.Angle = direction.Angle();
        spec.Steps = 3;
        spec.FadeTail = 0.3f;
        spec.ZIndex = 2;
        Pools.PlayFx(origin, spec, FxOwner.Player, _owner);
        EmitSparks(origin + direction * 8f, direction, family, 2, 0.8f, 60f, 110f, 1);
    }

    /// <summary>
    /// Cône entretenu de la Dernière Émission : bandes d'onde qui s'éloignent, ouverture croissante ; chaque onde en plus
    /// ajoute un front qui le parcourt (<paramref name="waves"/>, plafonné par <see cref="WeaponVisualConfig.ConeMaxFronts"/>).
    /// </summary>
    public void UpdateCone(WeaponData weapon, Vector2 direction, float range, float halfAngle, int waves, Vector2? origin = null)
    {
        if (Pools == null)
            return;
        if (_cone == null || !_cone.Visible)
        {
            PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Cone, FamilyOf(weapon), range, 1f, 0.5f);
            // Éventail posé au sol : angle au sol, forme aplatie par le shader.
            spec.Angle = Iso.ToGround(direction).Angle();
            spec.Squash = Iso.GroundSquash;
            spec.ArcHalf = halfAngle;
            spec.FillDensity = 0.25f;
            spec.Thickness = Mathf.Clamp(waves - 1, 0, WeaponVisualConfig.Load().ConeMaxFronts);
            spec.Steps = 8;
            spec.FadeTail = 0f;
            spec.Loop = true;
            spec.ZIndex = -1;
            _cone = Pools.PlayFx(origin ?? _owner.GlobalPosition, spec, FxOwner.Player, origin == null ? _owner : null);
            return;
        }
        _cone.Aim(Iso.ToGround(direction).Angle(), halfAngle);
    }

    public void StopCone()
    {
        _cone?.Stop();
        _cone = null;
    }

    /// <summary>Visuel d'un orbe de la Boîte à Musique : note de laiton qui gravite à hauteur de buste.</summary>
    public static Sprite2D CreateOrbitalVisual(float sizeScale = 1f)
    {
        ProjectileSprites.SpriteSet set = ProjectileSprites.Get("note");
        return new Sprite2D
        {
            Name = "Visual",
            Texture = set?.Get(0, 0),
            Position = new Vector2(0f, -TorsoHeight),
            Scale = Vector2.One * sizeScale,
            Material = ProjectileMaterial,
            Visible = set != null && CombatFxSettings.PlayerProjectiles,
            Modulate = new Color(1f, 1f, 1f, CombatFxSettings.PlayerOpacity),
        };
    }

    /// <summary>Balancement de la note, deux poses par seconde et par orbe.</summary>
    public static void AnimateOrbitalVisual(Sprite2D visual, float time, int index)
    {
        ProjectileSprites.SpriteSet set = ProjectileSprites.Get("note");
        if (set == null || visual == null)
            return;
        Texture2D texture = set.Get(0, (int)(time * set.Fps + index) % set.Frames);
        if (visual.Texture != texture)
            visual.Texture = texture;
    }

    /// <summary>Réaction d'attaque du personnage : élan orienté vers la cible (mêlée) ou recul (tir) + squash bref.</summary>
    public void PlayRecoil(bool isMelee, Vector2 direction = default)
    {
        if (_sprite == null || !_sprite.Visible)
            return;
        if (direction != Vector2.Zero)
            LungeOffset = isMelee ? direction * 2.6f : -direction * 1.4f;
        _squashTween?.Kill();
        _sprite.Scale = isMelee ? new Vector2(1.09f, 0.92f) : new Vector2(0.95f, 1.05f);
        _squashTween = _sprite.CreateTween();
        _squashTween.TweenProperty(_sprite, "scale", Vector2.One, isMelee ? 0.1f : 0.08f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
    }

    /// <summary>Annule l'élan en cours : mort ou suspension reposent la silhouette à sa place.</summary>
    public void ClearLunge() => LungeOffset = Vector2.Zero;

    /// <summary>Micro-amorti d'inertie à la réception d'un dash ou à l'arrêt franc d'une course (plan 54 G1).</summary>
    public void PlayStepSquash(float horizontal = 1.06f, float vertical = 0.94f, float duration = 0.09f)
    {
        if (_sprite == null || !_sprite.Visible)
            return;
        _squashTween?.Kill();
        _sprite.Scale = new Vector2(horizontal, vertical);
        _squashTween = _sprite.CreateTween();
        _squashTween.TweenProperty(_sprite, "scale", Vector2.One, duration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
    }

    /// <summary>Gerbe d'impact dans le sens du coup, dorée et plus fournie au critique.</summary>
    public void PlayHit(WeaponData weapon, Vector2 enemyPosition, bool isCrit) =>
        PlayHit(FamilyOf(weapon), enemyPosition, isCrit, _owner.GlobalPosition, false);

    /// <summary>
    /// Gerbe d'impact à la couleur d'une arme ou d'un objet, partie de <paramref name="from"/> : le joueur pour un coup,
    /// le centre de la zone pour un écho, une forme ou une explosion (plan 27 V2b).
    /// </summary>
    public void PlayHit(FxFamily family, Vector2 enemyPosition, bool isCrit, Vector2 from) =>
        PlayHit(family, enemyPosition, isCrit, from, true);

    private void PlayHit(FxFamily family, Vector2 enemyPosition, bool isCrit, Vector2 from, bool secondary)
    {
        if (Pools == null || !CombatFxSettings.PlayerAttackFx)
            return;
        ulong frame = Engine.GetProcessFrames();
        if (frame != _hitFrame)
        {
            _hitFrame = frame;
            _hitBurstsThisFrame = 0;
            _secondaryBurstsThisFrame = 0;
        }
        if (secondary ? ++_secondaryBurstsThisFrame > MaxSecondaryBurstsPerFrame : ++_hitBurstsThisFrame > MaxHitBurstsPerFrame)
            return;

        // Chaque coup se voit (DECISIONS §53, plan 21 F6) : éclat en étoile à la couleur de l'arme et gerbe ; un
        // critique ajoute une gerbe large et un anneau au sol. Le budget d'effets (FxBudget) plafonne la foule.
        Vector2 direction = (enemyPosition - from).Normalized();
        if (direction == Vector2.Zero)
            direction = Vector2.Up;
        Vector2 point = enemyPosition + new Vector2(0f, -TorsoHeight) - direction * 4f;
        if (isCrit)
        {
            EmitSparks(point, direction, FxFamily.Crit, 14, 1.5f, 100f, 220f, 2);
            PlayFlash(point, FxFamily.Crit, 10f);
            PlayGroundRing(enemyPosition, FxFamily.Crit, 16f, 2f, 0.2f);
        }
        else
        {
            EmitSparks(point, direction, family, 7, 1.15f, 80f, 180f, 1);
            PlayFlash(point, family, 5f);
        }
    }

    /// <summary>
    /// Écho des Gants : zone tramée, anneau et gerbe à l'endroit de la frappe répétée, au rayon réel du contrôle des
    /// dégâts (couchée au sol, comme la zone touchée), à la couleur de l'arme.
    /// </summary>
    public void PlayEcho(WeaponData weapon, Vector2 position, float radius)
    {
        PlayBurst(position, FamilyOf(weapon), radius);
    }

    /// <summary>Anneau au sol d'un palier d'objet : zone qui refrappe, onde du bouclier cassé.</summary>
    public void PlayObjectRing(Vector2 ground, FxFamily family, float radius)
    {
        PlayGroundRing(ground, family, radius, 2f, 0.3f);
    }

    /// <summary>
    /// Éclat d'un projectile en bout de course (Mètre pliant) : zone tramée du rayon réel, anneau, étincelles.
    /// <paramref name="squash"/> : écrasement au sol de la zone, 1 pour une zone contrôlée en cercle à l'écran.
    /// </summary>
    public void PlayBurst(Vector2 ground, FxFamily family, float radius, float squash = Iso.GroundSquash)
    {
        if (Pools == null)
            return;
        PixelFxSpec zone = PixelFxSpec.Of(PixelFxShape.Zone, family, radius, 1f, 0.25f);
        zone.Matter = ZoneMatter.Burst;
        zone.Squash = squash;
        zone.Steps = 4;
        zone.FadeTail = 0.5f;
        zone.ZIndex = -1;
        Pools.PlayFx(ground, zone, FxOwner.Player);
        PlayGroundRing(ground, family, radius, 2f, 0.22f, squash);
        EmitSparks(ground + new Vector2(0f, -TorsoHeight * 0.5f), Vector2.Zero, family, 5, 0f, 40f, 90f, 1);
    }

    /// <summary>Projectile en bout de course : petite étoile et quatre étincelles qui poursuivent sa route.</summary>
    public void PlayFizzle(Vector2 position, FxFamily family, Vector2 direction)
    {
        if (Pools == null)
            return;
        PlayFlash(position, family, 3f);
        EmitSparks(position, direction, family, 4, 0.9f, 20f, 50f, 1);
    }

    /// <summary>Tache collante du Chewing-gum, flaque rose posée au sol le temps qu'elle ralentit.</summary>
    public void PlayTrail(Vector2 position, float radius, float duration)
    {
        if (Pools == null)
            return;
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Zone, FxFamily.Gum, radius, 1f, duration);
        spec.Matter = ZoneMatter.Liquid;
        // Translucide : les taches d'un dash se chevauchent en traînée sans couvrir le sol.
        spec.FillDensity = 0.4f;
        spec.Squash = Iso.GroundSquash;
        spec.Steps = 4;
        spec.FadeTail = 0.3f;
        spec.ZIndex = -1;
        Pools.PlayFx(position, spec, FxOwner.Player);
    }

    /// <summary>Champ de ralentissement de l'Aiguille de l'Horloge, posé au sol.</summary>
    public void PlayTimeField(Vector2 position, float radius, float duration)
    {
        // Un champ par attaque : les cibles touchées ensemble partagent le même.
        ulong frame = Engine.GetProcessFrames();
        if (Pools == null || frame == _timeFieldFrame)
            return;
        _timeFieldFrame = frame;
        // Cadran couché au sol (plan 25 S10) : le champ se contrôle au sol, comme les autres zones.
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Zone, FxFamily.Hybrid, radius, 1f, duration);
        spec.Matter = ZoneMatter.Dial;
        spec.Squash = Iso.GroundSquash;
        spec.Steps = 12;
        spec.ZIndex = -1;
        Pools.PlayFx(position, spec, FxOwner.Player);
    }

    /// <summary>Chaîne des Noms : rayon brisé entre deux cibles.</summary>
    public void PlayChain(Vector2 from, Vector2 to, WeaponData weapon) => PlayBeam(from, to, FamilyOf(weapon));

    /// <summary>Rayon brisé entre deux cibles, de la famille donnée (chaîne, coup critique qui repart).</summary>
    public void PlayBeam(Vector2 from, Vector2 to, FxFamily family)
    {
        if (Pools == null)
            return;
        Vector2 offset = new(0f, -TorsoHeight);
        Vector2 delta = to - from;
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Beam, family, delta.Length() * 0.5f, 3f, 0.22f);
        spec.Angle = delta.Angle();
        spec.Steps = 5;
        spec.ZIndex = 2;
        Pools.PlayFx((from + to) * 0.5f + offset, spec, FxOwner.Player);
    }

    private void PlayArc(Vector2 center, Vector2 direction, float radius, float thickness, float halfArc,
                         FxFamily family, float duration, float squash = Iso.SlashSquash, bool followOwner = true)
    {
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Arc, family, radius, thickness, duration);
        spec.Angle = GroundAngle(direction, squash);
        spec.ArcHalf = halfArc;
        spec.Squash = squash;
        spec.Steps = 7;
        spec.FadeTail = 0.2f;
        Pools.PlayFx(center, spec, FxOwner.Player, followOwner ? _owner : null);
    }

    private void PlayCable(Vector2 center, Vector2 direction, float radius, FxFamily family, bool followOwner)
    {
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Cable, family, radius, 3f, 0.32f);
        spec.Angle = GroundAngle(direction, Iso.SlashSquash);
        spec.ArcHalf = Mathf.Pi;
        spec.Squash = Iso.SlashSquash;
        spec.Steps = 12;
        spec.FadeTail = 0.15f;
        Pools.PlayFx(center, spec, FxOwner.Player, followOwner ? _owner : null);
    }

    private void PlayThrust(Vector2 origin, Vector2 direction, float length, FxFamily family, bool followOwner = true)
    {
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Thrust, family, length, MeleeStrikeShape.ThrustHalfWidth * 2f, 0.16f);
        spec.Angle = direction.Angle();
        spec.Steps = 5;
        spec.FadeTail = 0.2f;
        Pools.PlayFx(origin, spec, FxOwner.Player, followOwner ? _owner : null);
        EmitSparks(origin + direction * length, direction, family, 3, 0.5f, 80f, 140f, 1);
    }

    private void PlaySmashImpact(Vector2 ground, FxFamily family, float radius)
    {
        PlayGroundRing(ground, family, radius * 0.75f, 4f, 0.3f);
        EmitDebris(ground, Vector2.Zero, family, 9);
        ScreenShake.Instance?.ShakeLight();
    }

    private void PlayBell(FxFamily family, float radius, Vector2 ground)
    {
        // Ondes aplaties comme les traînées : la Cloche touche dans la même ellipse (MeleeStrikeShape).
        PlayGroundRing(ground, family, radius, 3f, 0.3f, Iso.SlashSquash);
        PlayGroundRing(ground, family, radius * 1.35f, 2f, 0.45f, Iso.SlashSquash);
        EmitSparks(ground + new Vector2(0f, -TorsoHeight), Vector2.Zero, family, 8, 0f, 50f, 110f, 1);
    }

    private static void PlayGroundRing(Vector2 ground, FxFamily family, float radius, float thickness, float duration,
        float squash = Iso.GroundSquash)
    {
        if (Pools == null)
            return;
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Ring, family, radius, thickness, duration);
        spec.Squash = squash;
        spec.Steps = 6;
        spec.FadeTail = 0.35f;
        spec.ZIndex = -1;
        Pools.PlayFx(ground, spec, FxOwner.Player);
    }

    private static void PlayFlash(Vector2 point, FxFamily family, float radius)
    {
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Star, family, radius, 1f, 0.12f);
        spec.Steps = 3;
        spec.FadeTail = 0.4f;
        spec.ZIndex = 2;
        Pools.PlayFx(point, spec, FxOwner.Player);
    }

    private static void EmitSparks(Vector2 point, Vector2 direction, FxFamily family, int count, float spread,
                                   float speedMin, float speedMax, int size)
    {
        Pools.EmitSparks(point, new SparkBurst
        {
            Family = family,
            Owner = FxOwner.Player,
            Count = count,
            Direction = direction,
            Spread = spread,
            SpeedMin = speedMin,
            SpeedMax = speedMax,
            LifeMin = 0.12f,
            LifeMax = 0.26f,
            Size = size,
        });
    }

    private static void EmitDebris(Vector2 ground, Vector2 direction, FxFamily family, int count)
    {
        Pools.EmitSparks(ground, new SparkBurst
        {
            Family = family,
            Owner = FxOwner.Player,
            Count = count,
            Direction = direction,
            Spread = 1.4f,
            SpeedMin = 60f,
            SpeedMax = 130f,
            LifeMin = 0.35f,
            LifeMax = 0.55f,
            Ballistic = true,
            Size = 2,
        });
    }

    /// <summary>Angle d'une direction d'écran dans le repère « déplié » d'une forme compressée au sol.</summary>
    private static float GroundAngle(Vector2 direction, float squash)
    {
        return new Vector2(direction.X, direction.Y * squash).Angle();
    }
}
