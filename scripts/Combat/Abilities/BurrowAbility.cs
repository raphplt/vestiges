using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat.Abilities;

/// <summary>
/// Rampant : s'enfouit, file sous terre vers le joueur (invulnérable, sans contact), puis annonce son surgissement
/// par un cercle au sol qui se remplit. Le surgissement frappe la zone annoncée ; s'en écarter pendant l'annonce
/// l'évite. Un court temps de sortie, immobile et vulnérable, suit.
/// </summary>
public class BurrowAbility : IEnemyAbility
{
    private enum Phase { Surface, Burrowed, Warning, Recovery }

    private readonly Enemy _owner;
    private readonly GroundTelegraph _marker;

    private float _surfaceSeconds;
    private float _burrowSeconds;
    private float _warningSeconds;
    private float _recoverySeconds;
    private float _burrowSpeedMultiplier;
    private float _emergeRadius;
    private float _hitMargin;
    private float _damageMultiplier;
    private float _flashSeconds;
    private FxFamily _family;
    private string _burrowAudio;
    private string _emergeAudio;

    private Phase _phase;
    private float _timer;
    private float _flashTimer;

    public bool ReplacesBaseAttack => false;

    public BurrowAbility(Enemy owner)
    {
        _owner = owner;
        _marker = new GroundTelegraph { Name = "BurrowMarker" };
        owner.AddChild(_marker);
    }

    public void Configure(EnemyAbilityData data)
    {
        _surfaceSeconds = Mathf.Max(0.1f, data.GetNumber("surface_seconds", 5f));
        _burrowSeconds = Mathf.Max(0.1f, data.GetNumber("burrow_seconds", 2f));
        _warningSeconds = Mathf.Max(0.1f, data.GetNumber("warning_seconds", 0.6f));
        _recoverySeconds = data.GetNumber("recovery_seconds", 0.4f);
        _burrowSpeedMultiplier = data.GetNumber("burrow_speed_multiplier", 1.3f);
        _emergeRadius = data.GetNumber("emerge_radius", 34f);
        _hitMargin = data.GetNumber("player_hit_margin", 6f);
        _damageMultiplier = data.GetNumber("damage_multiplier", 1.2f);
        _flashSeconds = data.GetNumber("impact_flash_seconds", 0.15f);
        _family = PixelPalette.ParseFamily(data.GetText("fx_family", "rust"), FxFamily.Rust);
        _burrowAudio = data.GetText("burrow_audio", "");
        _emergeAudio = data.GetText("emerge_audio", "");

        Cancel();
    }

    public bool Process(Enemy owner, Player player, float distToPlayer, float delta)
    {
        UpdateFlash(delta);
        _timer -= delta;

        switch (_phase)
        {
            case Phase.Surface:
                if (_timer <= 0f && !owner.IsDisoriented)
                {
                    Burrow(owner);
                    return true;
                }
                return false;

            case Phase.Burrowed:
                Vector2 toPlayer = player.GlobalPosition - owner.GlobalPosition;
                owner.Velocity = toPlayer.LengthSquared() > 1f
                    ? toPlayer.Normalized() * owner.Speed * _burrowSpeedMultiplier * owner.SlowFactor
                    : Vector2.Zero;
                if (_timer <= 0f)
                    StartWarning(owner);
                return true;

            case Phase.Warning:
                owner.Velocity = Vector2.Zero;
                _marker.SetProgress(1f - _timer / _warningSeconds);
                if (_timer <= 0f)
                    Emerge(owner, player);
                return true;

            case Phase.Recovery:
                owner.Velocity = Vector2.Zero;
                if (_timer <= 0f)
                {
                    _phase = Phase.Surface;
                    _timer = _surfaceSeconds;
                }
                return true;
        }
        return false;
    }

    /// <summary>Remonte sans frapper : la créature ne doit jamais rester enfouie hors du traitement complet ni au pool.</summary>
    public void Cancel()
    {
        if (_phase is Phase.Burrowed or Phase.Warning)
            _owner.SetBurrowed(false);
        _phase = Phase.Surface;
        _timer = _surfaceSeconds;
        _flashTimer = 0f;
        _marker.HideMarker();
    }

    private void Burrow(Enemy owner)
    {
        _phase = Phase.Burrowed;
        _timer = _burrowSeconds;
        owner.SetBurrowed(true);
        if (_burrowAudio.Length > 0)
            owner.PlayNearbyAudio(_burrowAudio);
    }

    private void StartWarning(Enemy owner)
    {
        _phase = Phase.Warning;
        _timer = _warningSeconds;
        owner.Velocity = Vector2.Zero;
        _marker.ShowCircle(owner.GlobalPosition, _emergeRadius, _family);
    }

    private void Emerge(Enemy owner, Player player)
    {
        _phase = Phase.Recovery;
        _timer = _recoverySeconds;
        owner.SetBurrowed(false);
        owner.PlayAttackAnim();
        _flashTimer = _flashSeconds;
        _marker.SetFlash(1f);
        if (_emergeAudio.Length > 0)
            owner.PlayNearbyAudio(_emergeAudio);
        // Terre soulevée : toujours en pierre, quelle que soit la couleur de l'annonce.
        CombatPools.Instance?.EmitSparks(owner.GlobalPosition, new SparkBurst
        {
            Family = FxFamily.Stone,
            Owner = FxOwner.Enemy,
            Count = 6,
            Direction = Vector2.Up,
            Spread = 2.2f,
            SpeedMin = 40f,
            SpeedMax = 90f,
            LifeMin = 0.25f,
            LifeMax = 0.45f,
            Ballistic = true,
            Size = 1,
        });

        float reach = _emergeRadius + _hitMargin;
        // Rayon au sol : le surgissement touche exactement l'ellipse annoncée.
        if (GodotObject.IsInstanceValid(player) && Iso.GroundDistanceSquared(player.GlobalPosition, owner.GlobalPosition) <= reach * reach)
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
