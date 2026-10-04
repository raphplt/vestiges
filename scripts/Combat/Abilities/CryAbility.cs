using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Spawn;

namespace Vestiges.Combat.Abilities;

/// <summary>
/// Hurleur : s'arrête et gonfle son cri pendant que le sol marque où les renforts vont surgir. Le tuer pendant
/// l'annonce coupe l'appel : c'est la cible à prioriser. Au terme de l'annonce, les renforts apparaissent autour de lui.
/// </summary>
public class CryAbility : IEnemyAbility
{
    private readonly GroundTelegraph _marker;

    private float _cooldownSeconds;
    private float _range;
    private float _windupSeconds;
    private int _reinforcements;
    private string _reinforcementId;
    private float _spawnRadius;
    private float _flashSeconds;
    private FxFamily _family;
    private Color _flashColor;
    private string _cryAudio;

    private bool _isCrying;
    private float _timer;
    private float _cooldownTimer;
    private float _flashTimer;

    public bool ReplacesBaseAttack => false;
    public bool IsActive => _isCrying;

    public CryAbility(Enemy owner)
    {
        _marker = new GroundTelegraph { Name = "CryMarker" };
        owner.AddChild(_marker);
    }

    public void Configure(EnemyAbilityData data)
    {
        _cooldownSeconds = data.Number("cooldown_seconds");
        _range = data.Number("range");
        _windupSeconds = data.Number("windup_seconds");
        _reinforcements = (int)data.Number("reinforcements");
        _reinforcementId = data.Text("reinforcement_id");
        _spawnRadius = data.Number("spawn_radius");
        _flashSeconds = data.Number("impact_flash_seconds");
        _family = PixelPalette.ParseFamily(data.Text("fx_family"), FxFamily.Hostile);
        _flashColor = PixelPalette.Ramp(_family).Light;
        _cryAudio = data.Text("cry_audio");

        Cancel();
        _cooldownTimer = data.TryGetNumber("first_delay", out float firstDelay) ? firstDelay : _cooldownSeconds * 0.5f;
    }

    public bool Process(Enemy owner, Player player, float distToPlayer, float delta)
    {
        UpdateFlash(delta);

        if (_isCrying)
        {
            _timer -= delta;
            owner.Velocity = Vector2.Zero;
            _marker.SetProgress(1f - _timer / _windupSeconds);
            if (_timer <= 0f)
                Resolve(owner);
            return true;
        }

        _cooldownTimer -= delta;
        if (_cooldownTimer > 0f || distToPlayer > _range || owner.IsDisoriented || owner.IsAnotherAbilityActive(this))
            return false;

        _isCrying = true;
        _timer = _windupSeconds;
        owner.Velocity = Vector2.Zero;
        owner.PlayAttackAnim();
        owner.FlashWarning(_flashColor, _windupSeconds);
        _marker.ShowCircle(owner.GlobalPosition, _spawnRadius + 12f, _family);
        if (_cryAudio.Length > 0)
            owner.PlayNearbyAudio(_cryAudio);
        return true;
    }

    public void Cancel()
    {
        _isCrying = false;
        _timer = 0f;
        _flashTimer = 0f;
        _marker.HideMarker();
    }

    private void Resolve(Enemy owner)
    {
        _isCrying = false;
        _cooldownTimer = _cooldownSeconds;
        _flashTimer = _flashSeconds;
        _marker.SetFlash(1f);

        EnemyPool pool = EnemyPool.Instance;
        Node container = owner.GetParent();
        EnemyData data = EnemyDataLoader.Get(_reinforcementId);
        if (pool == null || container == null || data == null)
            return;

        float startAngle = (float)GD.RandRange(0, Mathf.Pi);
        for (int i = 0; i < _reinforcements; i++)
        {
            float angle = startAngle + Mathf.Tau * i / _reinforcements;
            Enemy reinforcement = pool.Get();
            reinforcement.GlobalPosition = owner.GlobalPosition + Iso.ToScreen(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _spawnRadius);
            container.AddChild(reinforcement);
            // Même montée en puissance que le Hurleur, et recensés comme toute apparition.
            reinforcement.Initialize(data, owner.HpScale, owner.DamageScale);
            owner.EmitSpawned(data.Id, owner.HpScale, owner.DamageScale);
        }
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
