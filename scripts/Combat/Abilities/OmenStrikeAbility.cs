using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat.Abilities;

/// <summary>
/// Présage : marque le sol là où le joueur sera après un court délai, extrapolé depuis sa vitesse,
/// puis y fait tomber un effondrement. Punit la fuite en ligne droite ; un changement de direction
/// ou d'allure pendant l'annonce suffit à l'éviter. L'ennemi reste immobile pendant l'incantation.
/// </summary>
public class OmenStrikeAbility : IEnemyAbility
{
    // Plafond partagé : plusieurs Présages ne doivent pas recouvrir tout l'écran de marques.
    // Portée processus : équilibré par Resolve/Cancel ; à rattacher à l'instance de run si le coop en lance plusieurs.
    private static int _activeMarks;

    private readonly GroundTelegraph _marker;
    private readonly Sprite2D _eye;
    private ProjectileSprites.SpriteSet _eyeSet;
    private int _eyeFrame;

    private float _leadSeconds;
    private float _maxLeadDistance;
    private float _delaySeconds;
    private float _radius;
    private float _hitMargin;
    private float _cooldownSeconds;
    private float _castRange;
    private int _maxSimultaneous;
    private float _damageMultiplier;
    private float _flashSeconds;
    private FxFamily _family;
    private string _castAudio;
    private string _impactAudio;

    private bool _isCasting;
    private float _castTimer;
    private float _cooldownTimer;
    private float _flashTimer;
    private Vector2 _impactCenter;

    public bool ReplacesBaseAttack => true;

    public OmenStrikeAbility(Enemy owner)
    {
        _marker = new GroundTelegraph { Name = "OmenMarker" };
        owner.AddChild(_marker);
        _eye = new Sprite2D
        {
            Name = "OmenEye", TopLevel = true, ZAsRelative = false, ZIndex = -1,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest, Visible = false,
        };
        owner.AddChild(_eye);
    }

    public void Configure(EnemyAbilityData data)
    {
        _leadSeconds = data.Number("lead_seconds");
        _maxLeadDistance = data.Number("max_lead_distance");
        _delaySeconds = data.Number("delay_seconds");
        _radius = data.Number("radius");
        _hitMargin = data.Number("player_hit_margin");
        _cooldownSeconds = data.Number("cooldown_seconds");
        _castRange = data.Number("cast_range");
        _maxSimultaneous = (int)data.Number("max_simultaneous");
        _damageMultiplier = data.Number("damage_multiplier");
        _flashSeconds = data.Number("impact_flash_seconds");
        _family = PixelPalette.ParseFamily(data.Text("fx_family"), FxFamily.Hostile);
        _castAudio = data.Text("cast_audio");
        _impactAudio = data.Text("impact_audio");

        Cancel();
        _eyeSet = ProjectileSprites.Get("omen");
        // Décalage initial pour que des Présages apparus ensemble ne frappent pas en rythme.
        _cooldownTimer = _cooldownSeconds * (float)GD.RandRange(0.4, 1.0);
    }

    public bool Process(Enemy owner, Player player, float distToPlayer, float delta)
    {
        UpdateFlash(delta);

        if (_isCasting)
        {
            _castTimer -= delta;
            _marker.SetProgress(1f - _castTimer / _delaySeconds);
            if (_eyeSet != null)
            {
                int frame = Mathf.Clamp(_eyeSet.Frames - 1 - (int)(_castTimer * _eyeSet.Fps), 0, _eyeSet.Frames - 1);
                if (frame != _eyeFrame)
                {
                    _eyeFrame = frame;
                    _eye.Texture = _eyeSet.Get(0, frame);
                }
            }
            if (_castTimer <= 0f)
                Resolve(owner, player);

            owner.Velocity = Vector2.Zero;
            return true;
        }

        _cooldownTimer -= delta;
        if (_cooldownTimer > 0f || distToPlayer > _castRange || owner.IsDisoriented || _activeMarks >= _maxSimultaneous)
            return false;

        BeginCast(owner, player);
        owner.Velocity = Vector2.Zero;
        return true;
    }

    public void Cancel()
    {
        if (_isCasting)
            _activeMarks--;

        _isCasting = false;
        _castTimer = 0f;
        _flashTimer = 0f;
        _marker.HideMarker();
        _eye.Visible = false;
    }

    private void BeginCast(Enemy owner, Player player)
    {
        Vector2 lead = player.Velocity * _leadSeconds;
        if (lead.LengthSquared() > _maxLeadDistance * _maxLeadDistance)
            lead = lead.Normalized() * _maxLeadDistance;

        _impactCenter = player.GlobalPosition + lead;
        _isCasting = true;
        _castTimer = _delaySeconds;
        _activeMarks++;

        _marker.ShowCircle(_impactCenter, _radius, _family);
        if (_eyeSet != null)
        {
            _eye.GlobalPosition = _impactCenter;
            _eye.Texture = _eyeSet.Get(0, 0);
            _eyeFrame = 0;
            _eye.Modulate = Colors.White with { A = CombatFxSettings.EnemyOpacity };
            _eye.Visible = true;
        }
        owner.PlayAttackAnim();
        if (_castAudio.Length > 0)
            AudioManager.Play(_castAudio, 0.08f, -6f);
    }

    private void Resolve(Enemy owner, Player player)
    {
        _isCasting = false;
        _activeMarks--;
        _cooldownTimer = _cooldownSeconds;
        _flashTimer = _flashSeconds;
        _marker.SetProgress(1f);
        _marker.SetFlash(1f);
        _eye.Visible = false;
        CombatPools.Instance?.ShowProjectileImpact(_impactCenter, _eyeSet?.Impact);

        if (_impactAudio.Length > 0)
            AudioManager.Play(_impactAudio, 0.08f, -4f);

        float reach = _radius + _hitMargin;
        // Rayon au sol : la frappe touche exactement l'ellipse annoncée.
        if (GodotObject.IsInstanceValid(player) && Iso.GroundDistanceSquared(player.GlobalPosition, _impactCenter) <= reach * reach)
            owner.HitPlayer(player, owner.Damage * _damageMultiplier);
    }

    private void UpdateFlash(float delta)
    {
        if (_flashTimer <= 0f)
            return;

        _flashTimer -= delta;
        if (_flashTimer <= 0f)
            _marker.HideMarker();
        else
            _marker.SetFlash(_flashTimer / _flashSeconds);
    }
}
