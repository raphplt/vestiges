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
    private Color _color;
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
    }

    public void Configure(EnemyAbilityData data)
    {
        _leadSeconds = data.GetNumber("lead_seconds", 1f);
        _maxLeadDistance = data.GetNumber("max_lead_distance", 240f);
        _delaySeconds = Mathf.Max(0.1f, data.GetNumber("delay_seconds", 1f));
        _radius = data.GetNumber("radius", 42f);
        _hitMargin = data.GetNumber("player_hit_margin", 8f);
        _cooldownSeconds = data.GetNumber("cooldown_seconds", 2.8f);
        _castRange = data.GetNumber("cast_range", 320f);
        _maxSimultaneous = Mathf.Max(1, Mathf.RoundToInt(data.GetNumber("max_simultaneous", 3f)));
        _damageMultiplier = data.GetNumber("damage_multiplier", 1f);
        _flashSeconds = data.GetNumber("impact_flash_seconds", 0.15f);
        _color = Color.FromHtml(data.GetText("color", "#B8FF5A"));
        _castAudio = data.GetText("cast_audio", "");
        _impactAudio = data.GetText("impact_audio", "");

        Cancel();
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

        _marker.ShowCircle(_impactCenter, _radius, _color);
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

        if (_impactAudio.Length > 0)
            AudioManager.Play(_impactAudio, 0.08f, -4f);

        float reach = _radius + _hitMargin;
        if (GodotObject.IsInstanceValid(player) && player.GlobalPosition.DistanceSquaredTo(_impactCenter) <= reach * reach)
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
