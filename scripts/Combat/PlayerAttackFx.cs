using Godot;
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

    private readonly Node2D _owner;
    private readonly AnimatedSprite2D _sprite;
    private Tween _squashTween;
    private ulong _hitFrame;
    private int _hitBurstsThisFrame;
    private ulong _timeFieldFrame;
    private PixelFx _cone;

    public PlayerAttackFx(Node2D owner, AnimatedSprite2D sprite)
    {
        _owner = owner;
        _sprite = sprite;
    }

    private static CombatPools Pools => CombatPools.Instance;

    public static FxFamily FamilyOf(WeaponData weapon)
    {
        return PixelPalette.ParseFamily(weapon?.Fx.Family, PixelPalette.FamilyForDamageType(weapon?.DamageType));
    }

    /// <summary>Coup de mêlée : une forme par frappe (les frappes supplémentaires appellent plusieurs fois).</summary>
    public void PlayMelee(WeaponData weapon, Vector2 direction, float range, float arcAngle)
    {
        if (Pools == null || !CombatFxSettings.PlayerAttackFx)
            return;
        FxFamily family = FamilyOf(weapon);
        Vector2 torso = _owner.GlobalPosition + new Vector2(0f, -TorsoHeight);
        float radius = Mathf.Clamp(range * 0.7f, 22f, 64f);
        float halfArc = Mathf.DegToRad(Mathf.Clamp(arcAngle, 40f, 200f) * 0.5f);

        switch (weapon?.Fx.Style)
        {
            case "cleave":
                PlayArc(torso, direction, radius * 1.1f, radius * 0.55f, halfArc * 1.15f, family, 0.26f);
                EmitDebris(torso + direction * radius * 0.8f, direction, family, 5);
                break;
            case "smash":
                PlayArc(torso, direction, radius * 0.85f, radius * 0.5f, halfArc, family, 0.18f);
                PlaySmashImpact(_owner.GlobalPosition + direction * radius * 0.75f, family, radius);
                break;
            case "thrust":
                PlayThrust(torso, direction, Mathf.Clamp(range * 0.95f, 26f, 80f), family);
                break;
            case "whirl":
                PlayArc(torso, direction, radius, radius * 0.3f, Mathf.Pi, family, 0.28f, squash: 1.6f);
                break;
            case "bell":
                PlayBell(family, radius);
                break;
            case "clock":
                PlayArc(torso, direction, radius, radius * 0.42f, halfArc, family, 0.22f);
                PlayGroundRing(_owner.GlobalPosition, family, radius * 0.8f, 3f, 0.32f);
                break;
            case "void":
                PlayArc(torso, direction, radius * 1.05f, radius * 0.5f, halfArc, FxFamily.Void, 0.24f);
                EmitSparks(torso + direction * radius * 0.7f, direction, FxFamily.Void, 6, 0.9f, 60f, 130f, 2);
                break;
            default:
                PlayArc(torso, direction, radius, radius * 0.42f, halfArc, family, 0.22f);
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

    /// <summary>Cône entretenu de la Dernière Émission : bandes d'onde qui s'éloignent, ouverture croissante.</summary>
    public void UpdateCone(WeaponData weapon, Vector2 direction, float range, float halfAngle)
    {
        if (Pools == null)
            return;
        if (_cone == null || !_cone.Visible)
        {
            PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Cone, FamilyOf(weapon), range, 1f, 0.5f);
            spec.Angle = direction.Angle();
            spec.ArcHalf = halfAngle;
            spec.FillDensity = 0.25f;
            spec.Steps = 8;
            spec.FadeTail = 0f;
            spec.Loop = true;
            spec.ZIndex = -1;
            _cone = Pools.PlayFx(_owner.GlobalPosition, spec, FxOwner.Player, _owner);
            return;
        }
        _cone.Aim(direction.Angle(), halfAngle);
    }

    public void StopCone()
    {
        _cone?.Stop();
        _cone = null;
    }

    /// <summary>Visuel d'un orbe de la Boîte à Musique : note de laiton qui gravite à hauteur de buste.</summary>
    public static Sprite2D CreateOrbitalVisual()
    {
        ProjectileSprites.SpriteSet set = ProjectileSprites.Get("note");
        return new Sprite2D
        {
            Name = "Visual",
            Texture = set?.Get(0, 0),
            Position = new Vector2(0f, -TorsoHeight),
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

    /// <summary>Réaction d'attaque du personnage : écrasement d'un à deux pixels vers la cible.</summary>
    public void PlayRecoil(bool isMelee)
    {
        if (_sprite == null || !_sprite.Visible)
            return;
        _squashTween?.Kill();
        _sprite.Scale = isMelee ? new Vector2(1.08f, 0.93f) : new Vector2(0.95f, 1.04f);
        _squashTween = _sprite.CreateTween();
        _squashTween.TweenProperty(_sprite, "scale", Vector2.One, isMelee ? 0.1f : 0.08f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
    }

    /// <summary>Gerbe d'impact dans le sens du coup, dorée et plus fournie au critique.</summary>
    public void PlayHit(WeaponData weapon, Vector2 enemyPosition, bool isCrit)
    {
        if (Pools == null)
            return;
        ulong frame = Engine.GetProcessFrames();
        if (frame != _hitFrame)
        {
            _hitFrame = frame;
            _hitBurstsThisFrame = 0;
        }
        if (++_hitBurstsThisFrame > MaxHitBurstsPerFrame)
            return;

        Vector2 direction = (enemyPosition - _owner.GlobalPosition).Normalized();
        Vector2 point = enemyPosition + new Vector2(0f, -TorsoHeight) - direction * 4f;
        if (isCrit)
        {
            EmitSparks(point, direction, FxFamily.Crit, 9, 1.3f, 90f, 190f, 2);
            PlayFlash(point, FxFamily.Crit, 7f);
        }
        else
        {
            EmitSparks(point, direction, FamilyOf(weapon), 4, 1.0f, 70f, 150f, 1);
        }
    }

    /// <summary>Écho des Gantelets : anneau bref à l'endroit de la frappe répétée.</summary>
    public void PlayEcho(Vector2 position)
    {
        PlayGroundRing(position, FxFamily.Hybrid, 18f, 2f, 0.25f);
    }

    /// <summary>Champ de ralentissement de l'Aiguille de l'Horloge, posé au sol.</summary>
    public void PlayTimeField(Vector2 position, float radius, float duration)
    {
        // Un champ par attaque : les cibles touchées ensemble partagent le même.
        ulong frame = Engine.GetProcessFrames();
        if (Pools == null || frame == _timeFieldFrame)
            return;
        _timeFieldFrame = frame;
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Zone, FxFamily.Hybrid, radius, 1f, duration);
        spec.Squash = 2f;
        spec.FillDensity = 0f;
        spec.ProgressFill = false;
        spec.Steps = 8;
        spec.ZIndex = -1;
        Pools.PlayFx(position, spec, FxOwner.Player);
    }

    /// <summary>Chaîne des Noms : rayon brisé entre deux cibles.</summary>
    public void PlayChain(Vector2 from, Vector2 to, WeaponData weapon)
    {
        if (Pools == null)
            return;
        Vector2 offset = new(0f, -TorsoHeight);
        Vector2 delta = to - from;
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Beam, FamilyOf(weapon), delta.Length() * 0.5f, 3f, 0.22f);
        spec.Angle = delta.Angle();
        spec.Steps = 5;
        spec.ZIndex = 2;
        Pools.PlayFx((from + to) * 0.5f + offset, spec, FxOwner.Player);
    }

    private void PlayArc(Vector2 center, Vector2 direction, float radius, float thickness, float halfArc,
                         FxFamily family, float duration, float squash = 1.35f)
    {
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Arc, family, radius, thickness, duration);
        spec.Angle = GroundAngle(direction, squash);
        spec.ArcHalf = halfArc;
        spec.Squash = squash;
        spec.Steps = 7;
        spec.FadeTail = 0.2f;
        Pools.PlayFx(center, spec, FxOwner.Player, _owner);
    }

    private void PlayThrust(Vector2 origin, Vector2 direction, float length, FxFamily family)
    {
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Thrust, family, length, 7f, 0.16f);
        spec.Angle = direction.Angle();
        spec.Steps = 5;
        spec.FadeTail = 0.2f;
        Pools.PlayFx(origin, spec, FxOwner.Player, _owner);
        EmitSparks(origin + direction * length, direction, family, 3, 0.5f, 80f, 140f, 1);
    }

    private void PlaySmashImpact(Vector2 ground, FxFamily family, float radius)
    {
        PlayGroundRing(ground, family, radius * 0.75f, 4f, 0.3f);
        EmitDebris(ground, Vector2.Zero, family, 9);
        ScreenShake.Instance?.ShakeLight();
    }

    private void PlayBell(FxFamily family, float radius)
    {
        Vector2 ground = _owner.GlobalPosition;
        PlayGroundRing(ground, family, radius, 3f, 0.3f);
        PlayGroundRing(ground, family, radius * 1.35f, 2f, 0.45f);
        EmitSparks(ground + new Vector2(0f, -TorsoHeight), Vector2.Zero, family, 8, 0f, 50f, 110f, 1);
    }

    private static void PlayGroundRing(Vector2 ground, FxFamily family, float radius, float thickness, float duration)
    {
        if (Pools == null)
            return;
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Ring, family, radius, thickness, duration);
        spec.Squash = 2f;
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
