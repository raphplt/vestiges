using System;
using Godot;
using Vestiges.Combat;
using Vestiges.Combat.Abilities;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Coups du gardien de terre (plan 51 §16 G1, charte §11.4), greffés sur le Souverain qui le porte. Trois coups annoncés :
/// une charge sur une ligne au sol, à esquiver de côté ; une frappe qui ouvre le vide sous un disque annoncé ; une onde
/// qui part de lui, à traverser d'un dash. Trois phases selon ses PV, chacune plus rapide ; à la dernière, la frappe
/// tombe en trois disques. Un coup à la fois, pour que chaque annonce reste lisible.
/// </summary>
public sealed class GuardianMoves : IEnemyAbility
{
    private enum Move { None, ChargeWindup, Charge, Recovery, Slam, WaveWindup, Wave }

    private readonly Enemy _owner;
    private readonly GuardianMovesConfig _config;
    private readonly Action<Vector2, float, float> _breakGround;
    private readonly GroundTelegraph _chargeMark;
    private readonly GroundTelegraph[] _slamMarks;
    private readonly GroundTelegraph _waveMark;
    private readonly Vector2[] _slamCenters;

    private Move _move;
    private int _phase;
    private int _slamCount;
    private float _timer;
    private float _chargeCooldown;
    private float _slamCooldown;
    private float _waveCooldown;
    private float _flashTimer;
    private Vector2 _waveCenter;
    private bool _waveHit;
    private Vector2 _chargeDirection;
    private bool _chargeHit;
    private bool _detached;
    private int _waveStep;

    private const int WaveSteps = 12;

    public bool ReplacesBaseAttack => false;
    public bool IsActive => _move != Move.None;

    /// <summary>Phase du combat, de 0 à 2 : elle monte quand les PV passent sous les seuils.</summary>
    public int Phase => _phase;

    /// <param name="marks">
    /// Parent des annonces au sol. Pas la créature : elle retourne au pool et resservira à une autre espèce.
    /// </param>
    public GuardianMoves(Enemy owner, GuardianMovesConfig config, Node marks, Action<Vector2, float, float> breakGround)
    {
        _owner = owner;
        _config = config;
        _breakGround = breakGround;
        _chargeMark = AddMark(marks, "GuardianChargeMarker");
        _slamMarks = new GroundTelegraph[config.FinalSlamCount];
        _slamCenters = new Vector2[config.FinalSlamCount];
        for (int i = 0; i < _slamMarks.Length; i++)
            _slamMarks[i] = AddMark(marks, $"GuardianSlamMarker{i}");
        _waveMark = AddMark(marks, "GuardianWaveMarker");
        _chargeCooldown = config.ChargeFirstDelay;
        _slamCooldown = config.SlamFirstDelay;
        _waveCooldown = config.WaveCooldown;
    }

    private static GroundTelegraph AddMark(Node parent, string name)
    {
        GroundTelegraph mark = new() { Name = name };
        parent.AddChild(mark);
        return mark;
    }

    /// <summary>Fin du combat (mort, terre quittée) : les coups s'arrêtent et leurs annonces disparaissent.</summary>
    public void Detach()
    {
        if (_detached)
            return;
        Cancel();
        _detached = true;
        _chargeMark.QueueFree();
        foreach (GroundTelegraph mark in _slamMarks)
            mark.QueueFree();
        _waveMark.QueueFree();
    }

    public void Configure(EnemyAbilityData data)
    {
    }

    public bool Process(Enemy owner, Player player, float distToPlayer, float delta)
    {
        if (_detached)
            return false;
        UpdatePhase();
        UpdateFlash(delta);
        float pace = _config.PhaseCooldownScale[_phase];
        switch (_move)
        {
            case Move.ChargeWindup:
                _timer -= delta;
                _chargeMark.SetProgress(1f - _timer / _config.ChargeWindup);
                owner.Velocity = Vector2.Zero;
                if (_timer <= 0f)
                    StartCharge();
                return true;
            case Move.Charge:
                ProcessCharge(player, delta);
                return true;
            case Move.Recovery:
                _timer -= delta;
                owner.Velocity = Vector2.Zero;
                if (_timer <= 0f)
                    _move = Move.None;
                return true;
            case Move.Slam:
                ProcessSlam(player, delta);
                return true;
            case Move.WaveWindup:
                _timer -= delta;
                _waveMark.SetProgress(1f - _timer / _config.WaveWindup);
                owner.Velocity = Vector2.Zero;
                if (_timer <= 0f)
                    StartWave();
                return true;
            case Move.Wave:
                ProcessWave(player, delta);
                return true;
        }

        _chargeCooldown -= delta;
        _slamCooldown -= delta;
        _waveCooldown -= delta;
        if (owner.IsAnotherAbilityActive(this) || owner.IsDisoriented)
            return false;
        // L'onde n'arrive qu'à partir de la deuxième phase.
        if (_phase >= 1 && _waveCooldown <= 0f && distToPlayer <= _config.WaveRadius)
        {
            StartWaveWindup();
            _waveCooldown = _config.WaveCooldown * pace;
            return true;
        }
        if (_slamCooldown <= 0f && distToPlayer <= _config.SlamCastRange)
        {
            StartSlam(player);
            _slamCooldown = _config.SlamCooldown * pace;
            return true;
        }
        if (_chargeCooldown <= 0f && distToPlayer <= _config.ChargeTriggerRange && distToPlayer >= _config.ChargeMinRange)
        {
            StartChargeWindup(player);
            _chargeCooldown = _config.ChargeCooldown * pace;
            return true;
        }
        return false;
    }

    public void Cancel()
    {
        if (_detached)
            return;
        if (_move is Move.WaveWindup or Move.ChargeWindup)
            _owner.SetWindupPose(false);
        _move = Move.None;
        _flashTimer = 0f;
        _chargeMark.HideMarker();
        foreach (GroundTelegraph mark in _slamMarks)
            mark.HideMarker();
        _waveMark.HideMarker();
    }

    private void UpdatePhase()
    {
        float ratio = _owner.CurrentHp / Mathf.Max(1f, _owner.MaxHp);
        int phase = 0;
        while (phase < _config.PhaseThresholds.Length && ratio <= _config.PhaseThresholds[phase])
            phase++;
        if (phase <= _phase)
            return;
        _phase = phase;
        // Changement de phase : il gronde, le sol tremble, et le prochain coup arrive vite.
        _owner.FlashWarning(PixelPalette.Ramp(FxFamily.Void).Light, 0.6f);
        ScreenShake.Instance?.ShakeMedium();
        AudioManager.PlayAt(_config.PhaseAudio, _owner.GlobalPosition, 0.05f, -4f);
        _slamCooldown = Mathf.Min(_slamCooldown, 1.5f);
        GD.Print($"[GuardianMoves] {_owner.DisplayName} passe en phase {_phase + 1}");
    }

    /// <summary>Il s'accroupit en montrant sa ligne, verrouillée au début de l'annonce : un pas de côté l'évite.</summary>
    private void StartChargeWindup(Player player)
    {
        _move = Move.ChargeWindup;
        _timer = _config.ChargeWindup;
        _chargeDirection = (player.GlobalPosition - _owner.GlobalPosition).Normalized();
        _owner.Velocity = Vector2.Zero;
        _owner.SetWindupPose(true);
        _owner.FlashWarning(PixelPalette.Ramp(FxFamily.Void).Light, _config.ChargeWindup);
        _chargeMark.ShowLine(_owner.GlobalPosition, _owner.GlobalPosition + _chargeDirection * _config.ChargeDistance, _config.ChargeWidth, FxFamily.Void);
        AudioManager.PlayAt(_config.WarningAudio, _owner.GlobalPosition, 0.1f, -6f);
    }

    private void StartCharge()
    {
        _move = Move.Charge;
        _timer = _config.ChargeSeconds;
        _chargeHit = false;
        _owner.ReleaseWindup();
        _owner.PlayAttackAnim();
        _chargeMark.HideMarker();
    }

    private void ProcessCharge(Player player, float delta)
    {
        _timer -= delta;
        _owner.Velocity = _chargeDirection * (_config.ChargeDistance / _config.ChargeSeconds) * _owner.SlowFactor;
        if (!_chargeHit && GodotObject.IsInstanceValid(player)
            && _owner.GlobalPosition.DistanceSquaredTo(player.GlobalPosition) <= _config.ChargeHitRange * _config.ChargeHitRange)
        {
            _chargeHit = true;
            _owner.MeleeHitPlayer(player, _owner.Damage * _config.ChargeDamage);
            ScreenShake.Instance?.ShakeHeavy();
        }
        if (_timer > 0f)
            return;
        // La récupération qui suit laisse une fenêtre pour riposter.
        _move = Move.Recovery;
        _timer = _config.ChargeRecovery;
    }

    private void StartSlam(Player player)
    {
        _move = Move.Slam;
        _timer = _config.SlamDelay;
        _slamCount = _phase >= 2 ? _config.FinalSlamCount : 1;
        Vector2 lead = (player.Velocity * _config.SlamLeadSeconds).LimitLength(_config.SlamRadius * 1.5f);
        Vector2 target = player.GlobalPosition + lead;
        for (int i = 0; i < _slamCount; i++)
        {
            // Les disques en plus encadrent la cible : on sort par le côté qu'ils laissent libre.
            // Au sol, depuis la direction gardien → cible : les disques encadrent la cible sans fermer le côté du gardien.
            Vector2 facing = Iso.ToGround(target - _owner.GlobalPosition).Normalized();
            Vector2 offset = i == 0 ? Vector2.Zero
                : Iso.ToScreen(facing.Rotated(Mathf.Tau * i / _slamCount) * _config.FinalSlamSpread);
            _slamCenters[i] = target + offset;
            _slamMarks[i].ShowCircle(_slamCenters[i], _config.SlamRadius, FxFamily.Void);
        }
        _owner.PlayAttackAnim();
        _owner.FlashWarning(PixelPalette.Ramp(FxFamily.Void).Light, _config.SlamDelay);
        AudioManager.PlayAt(_config.WarningAudio, _owner.GlobalPosition, 0.08f, -6f);
    }

    private void ProcessSlam(Player player, float delta)
    {
        _timer -= delta;
        _owner.Velocity = Vector2.Zero;
        float progress = 1f - _timer / _config.SlamDelay;
        for (int i = 0; i < _slamCount; i++)
            _slamMarks[i].SetProgress(progress);
        if (_timer > 0f)
            return;
        _move = Move.None;
        _flashTimer = _config.ImpactFlashSeconds;
        float reach = _config.SlamRadius + _config.PlayerHitMargin;
        bool hit = false;
        for (int i = 0; i < _slamCount; i++)
        {
            _slamMarks[i].SetFlash(1f);
            _breakGround(_slamCenters[i], _config.SlamRadius, _config.BreachSeconds);
            hit |= GodotObject.IsInstanceValid(player) && Iso.GroundDistanceSquared(player.GlobalPosition, _slamCenters[i]) <= reach * reach;
        }
        if (hit)
            _owner.HitPlayer(player, _owner.Damage * _config.SlamDamage);
        ScreenShake.Instance?.ShakeHeavy();
        AudioManager.PlayAt(_config.SlamAudio, _slamCenters[0], 0.08f, -3f);
    }

    private void StartWaveWindup()
    {
        _move = Move.WaveWindup;
        _timer = _config.WaveWindup;
        _waveCenter = _owner.GlobalPosition;
        _owner.Velocity = Vector2.Zero;
        _owner.SetWindupPose(true);
        _owner.FlashWarning(PixelPalette.Ramp(FxFamily.Void).Light, _config.WaveWindup);
        // La portée de l'onde se montre pendant l'annonce : on sait jusqu'où elle ira.
        _waveMark.ShowCircle(_waveCenter, _config.WaveRadius, FxFamily.Void);
        AudioManager.PlayAt(_config.WarningAudio, _owner.GlobalPosition, 0.08f, -6f);
    }

    private void StartWave()
    {
        _move = Move.Wave;
        _timer = 0f;
        _waveHit = false;
        _waveStep = -1;
        _owner.ReleaseWindup();
        _owner.PlayAttackAnim();
        ScreenShake.Instance?.ShakeLight();
        AudioManager.PlayAt(_config.WaveAudio, _waveCenter, 0.08f, -5f);
    }

    /// <summary>L'anneau grandit de son centre à sa portée ; il touche le joueur au passage, sauf si un dash le traverse.</summary>
    private void ProcessWave(Player player, float delta)
    {
        _timer += delta;
        _owner.Velocity = Vector2.Zero;
        float radius = _config.WaveRadius * Mathf.Clamp(_timer / _config.WaveExpandSeconds, 0f, 1f);
        // L'anneau dessiné avance par paliers, comme les autres annonces : pas une nouvelle forme à chaque image.
        int step = Mathf.CeilToInt(radius / _config.WaveRadius * WaveSteps);
        if (step != _waveStep)
        {
            _waveStep = step;
            _waveMark.ShowRing(_waveCenter, Mathf.Max(_config.WaveRadius * step / WaveSteps, 1f), FxFamily.Void);
        }
        if (!_waveHit && GodotObject.IsInstanceValid(player))
        {
            float distance = Mathf.Sqrt(Iso.GroundDistanceSquared(player.GlobalPosition, _waveCenter));
            if (Mathf.Abs(distance - radius) <= _config.WaveThickness)
            {
                _waveHit = true;
                _owner.HitPlayer(player, _owner.Damage * _config.WaveDamage);
            }
        }
        if (_timer < _config.WaveExpandSeconds)
            return;
        _move = Move.None;
        _waveMark.HideMarker();
    }

    private void UpdateFlash(float delta)
    {
        if (_flashTimer <= 0f)
            return;
        _flashTimer -= delta;
        for (int i = 0; i < _slamCount; i++)
        {
            if (_flashTimer <= 0f)
                _slamMarks[i].HideMarker();
            else
                _slamMarks[i].SetFlash(_flashTimer / _config.ImpactFlashSeconds);
        }
    }
}
