using System;
using Godot;
using Vestiges.Combat;
using Vestiges.Combat.Abilities;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Coups du gardien de terre (plan 51 §16 G1, charte §11.4). Trois coups de référence annoncés, dont chaque gardien
/// prend ceux de sa fiche (plan 57 G0) : une charge sur une ligne au sol, à esquiver de côté ; une frappe qui ouvre le
/// vide sous un disque annoncé ; une onde qui part de lui, à traverser d'un dash. Trois phases selon ses PV, chacune plus
/// rapide ; l'onde n'arrive qu'à la deuxième, et à la dernière la frappe tombe en trois disques. Un gardien ajoute son
/// coup à lui : pour le Grand Cerf (plan 57 G1), des charges en chaîne, chacune annoncée et visée sur le joueur, dont la
/// dernière finit par une frappe qui ouvre le vide. Un coup à la fois, pour que chaque annonce reste lisible.
/// </summary>
public sealed class GuardianMoves : IEnemyAbility
{
    private enum Move { None, ChargeWindup, Charge, Recovery, Slam, WaveWindup, Wave }

    private readonly Enemy _owner;
    private readonly GuardianMovesConfig _config;
    private readonly GuardianMoveSet _set;
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
    private float _chainCooldown;
    // Charges qui restent après celle en cours ; la chaîne finit par la frappe.
    private int _chainLeft;
    private bool _inChain;
    private float _windup;
    private float _chargeDistance;
    private float _chargeSeconds;
    private Vector2 _chargeEnd;
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
    public GuardianMoves(Enemy owner, GuardianMovesConfig config, GuardianMoveSet set, Node marks, Action<Vector2, float, float> breakGround)
    {
        _owner = owner;
        _config = config;
        _set = set;
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
        _chainCooldown = config.ChainFirstDelay;
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
                _chargeMark.SetProgress(1f - _timer / _windup);
                if (_inChain && _chainLeft == 0)
                    _slamMarks[0].SetProgress(1f - _timer / _windup);
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
        // La chaîne ne compte son délai qu'à partir de sa phase : la première arrive peu après qu'il l'atteint.
        bool chainReady = Has(GuardianMoveSet.ChainCharge) && _phase >= _config.ChainFromPhase;
        if (chainReady)
            _chainCooldown -= delta;
        if (owner.IsAnotherAbilityActive(this) || owner.IsDisoriented)
            return false;
        // L'onde n'arrive qu'à partir de la deuxième phase.
        if (Has(GuardianMoveSet.Wave) && _phase >= 1 && _waveCooldown <= 0f && distToPlayer <= _config.WaveRadius)
        {
            StartWaveWindup();
            _waveCooldown = _config.WaveCooldown * pace;
            return true;
        }
        if (chainReady && _chainCooldown <= 0f && distToPlayer <= _config.ChainTriggerRange)
        {
            StartChain(player);
            _chainCooldown = _config.ChainCooldown * pace;
            return true;
        }
        if (Has(GuardianMoveSet.Slam) && _slamCooldown <= 0f && distToPlayer <= _config.SlamCastRange)
        {
            StartSlam(player);
            _slamCooldown = _config.SlamCooldown * pace;
            return true;
        }
        if (Has(GuardianMoveSet.Charge) && _chargeCooldown <= 0f && distToPlayer <= _config.ChargeTriggerRange && distToPlayer >= _config.ChargeMinRange)
        {
            _inChain = false;
            StartChargeWindup(player, _config.ChargeWindup, _config.ChargeDistance, _config.ChargeSeconds);
            _chargeCooldown = _config.ChargeCooldown * pace;
            return true;
        }
        return false;
    }

    private bool Has(GuardianMoveSet move) => (_set & move) != 0;

    public void Cancel()
    {
        if (_detached)
            return;
        if (_move is Move.WaveWindup or Move.ChargeWindup)
            _owner.SetWindupPose(false);
        _owner.SetChargePose(false);
        _inChain = false;
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

    /// <summary>Charges en chaîne : la première s'annonce comme une charge, les suivantes plus vite.</summary>
    private void StartChain(Player player)
    {
        _inChain = true;
        _chainLeft = _config.ChainCount - 1;
        // Le disque de sa fin reprend la première marque de la frappe : un éclat d'impact qui traîne ne doit pas l'éteindre.
        _flashTimer = 0f;
        foreach (GroundTelegraph mark in _slamMarks)
            mark.HideMarker();
        StartChargeWindup(player, _config.ChainWindup, _config.ChainDistance, _config.ChainSeconds);
    }

    /// <summary>
    /// Il s'accroupit en montrant sa ligne, verrouillée au début de l'annonce : un pas de côté l'évite. La dernière
    /// charge d'une chaîne montre aussi le disque où sa frappe ouvrira le vide.
    /// </summary>
    private void StartChargeWindup(Player player, float windup, float distance, float seconds)
    {
        _move = Move.ChargeWindup;
        _timer = windup;
        _windup = windup;
        _chargeDistance = distance;
        _chargeSeconds = seconds;
        _chargeDirection = (player.GlobalPosition - _owner.GlobalPosition).Normalized();
        _chargeEnd = _owner.GlobalPosition + _chargeDirection * distance;
        _owner.Velocity = Vector2.Zero;
        _owner.SetWindupPose(true);
        _owner.SetChargePose(true);
        _owner.FlashWarning(PixelPalette.Ramp(FxFamily.Void).Light, windup);
        _chargeMark.ShowLine(_owner.GlobalPosition, _chargeEnd, _config.ChargeWidth, FxFamily.Void);
        if (_inChain && _chainLeft == 0)
            _slamMarks[0].ShowCircle(_chargeEnd, _config.ChainBreachRadius, FxFamily.Void);
        string audio = _inChain ? _config.ChainWarningAudio : _config.WarningAudio;
        AudioManager.PlayAt(audio, _owner.GlobalPosition, 0.1f, -6f);
    }

    private void StartCharge()
    {
        _move = Move.Charge;
        _timer = _chargeSeconds;
        _chargeHit = false;
        _owner.ReleaseWindup();
        if (!_owner.HasChargeAnim)
            _owner.PlayAttackAnim();
        _chargeMark.HideMarker();
    }

    private void ProcessCharge(Player player, float delta)
    {
        _timer -= delta;
        _owner.Velocity = _chargeDirection * (_chargeDistance / _chargeSeconds) * _owner.SlowFactor;
        if (!_chargeHit && GodotObject.IsInstanceValid(player)
            && _owner.GlobalPosition.DistanceSquaredTo(player.GlobalPosition) <= _config.ChargeHitRange * _config.ChargeHitRange)
        {
            _chargeHit = true;
            _owner.MeleeHitPlayer(player, _owner.Damage * _config.ChargeDamage);
            ScreenShake.Instance?.ShakeHeavy();
        }
        if (_timer > 0f)
            return;
        // Sans joueur à viser, la chaîne finit tout de suite par sa frappe.
        if (_inChain && _chainLeft > 0 && GodotObject.IsInstanceValid(player))
        {
            _chainLeft--;
            StartChargeWindup(player, _config.ChainRewindup, _config.ChainDistance, _config.ChainSeconds);
            return;
        }
        _owner.SetChargePose(false);
        // La récupération qui suit laisse une fenêtre pour riposter.
        _move = Move.Recovery;
        _timer = _config.ChargeRecovery;
        if (!_inChain)
            return;
        _inChain = false;
        _timer = _config.ChainRecovery;
        ChainImpact(player);
    }

    /// <summary>Fin de la chaîne : sa frappe ouvre le vide là où sa dernière charge s'achève, et touche qui s'y trouve.</summary>
    private void ChainImpact(Player player)
    {
        _owner.PlayAttackAnim();
        _slamCount = 1;
        _slamCenters[0] = _chargeEnd;
        _slamMarks[0].SetFlash(1f);
        _flashTimer = _config.ImpactFlashSeconds;
        _breakGround(_chargeEnd, _config.ChainBreachRadius, _config.ChainBreachSeconds);
        float reach = _config.ChainBreachRadius + _config.PlayerHitMargin;
        if (GodotObject.IsInstanceValid(player) && Iso.GroundDistanceSquared(player.GlobalPosition, _chargeEnd) <= reach * reach)
            _owner.HitPlayer(player, _owner.Damage * _config.ChainImpactDamage);
        ScreenShake.Instance?.ShakeHeavy();
        AudioManager.PlayAt(_config.ChainImpactAudio, _chargeEnd, 0.08f, -3f);
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
