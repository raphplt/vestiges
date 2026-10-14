using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Double de l'Éveillée (plan 06 P3b) : au départ d'une mobilité, une rémanence reste à l'endroit quitté pendant
/// <see cref="PerkEcho.Seconds"/> et répète la prochaine attaque, depuis là, à <see cref="PerkEcho.DamageRatio"/> des dégâts.
/// Le joueur attaque ; ce module arme la rémanence, la montre (sa silhouette figée, translucide) et la retire.
/// </summary>
public partial class RemanenceEcho : Node
{
    private const float FadeAfterAttack = 0.25f;
    private static readonly Color GhostTint = new(0.55f, 1f, 1f, 0.6f);

    private Player _player;
    private PerkEcho _config;
    private AnimatedSprite2D _playerSprite;
    private Sprite2D _ghost;
    private Vector2 _previousPosition;
    private bool _wasMoving;
    private int _dashesSeen;
    private float _remaining;
    private float _fade;

    public bool IsShown => _ghost != null && _ghost.Visible;

    public void Initialize(Player player, PerkEcho config)
    {
        _player = player;
        _config = config;
        ProcessMode = ProcessModeEnum.Pausable;
    }

    public override void _Ready()
    {
        if (_player == null || !IsInstanceValid(_player) || _config == null
            || !float.IsFinite(_config.Seconds) || _config.Seconds <= 0f
            || !float.IsFinite(_config.DamageRatio) || _config.DamageRatio <= 0f)
        {
            SetProcess(false);
            return;
        }
        _playerSprite = _player.GetNodeOrNull<AnimatedSprite2D>("Sprite");
        _ghost = new Sprite2D
        {
            Visible = false,
            TopLevel = true,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        };
        AddChild(_ghost);
        _previousPosition = _player.GlobalPosition;
        _dashesSeen = _player.Mobility?.DashesStarted ?? 0;
        _player.AttackEchoed += OnAttackEchoed;
    }

    public override void _ExitTree()
    {
        if (_player != null && IsInstanceValid(_player))
        {
            _player.AttackEchoed -= OnAttackEchoed;
            _player.ResetAttackEcho();
        }
    }

    public override void _Process(double delta)
    {
        if (_player == null || !IsInstanceValid(_player))
            return;
        Advance((float)delta);
    }

    /// <summary>Un pas du module (appelé par _Process, ou directement par un test).</summary>
    public void Advance(float dt)
    {
        if (_ghost == null || _player == null || !IsInstanceValid(_player))
            return;
        if (_player.IsDead)
        {
            _player.ResetAttackEcho();
            _remaining = 0f;
            _fade = 0f;
            _ghost.Visible = false;
            return;
        }
        if (GetTree().Paused || !float.IsFinite(dt) || dt <= 0f)
            return;
        PlayerMobility mobility = _player.Mobility;
        bool moving = mobility?.IsDashing ?? false;
        // Un saut (Plan de métro) se termine dans son image : seul le compte des dashs le montre.
        bool started = mobility != null && mobility.DashesStarted != _dashesSeen;
        if ((moving && !_wasMoving || started) && !_player.IsDead)
            Arm(_previousPosition);
        _wasMoving = moving;
        _dashesSeen = mobility?.DashesStarted ?? 0;
        if (!moving)
            _previousPosition = _player.GlobalPosition;

        if (_remaining > 0f)
        {
            _remaining -= dt;
            if (_remaining <= 0f)
            {
                _player.DisarmAttackEcho();
                _ghost.Visible = false;
                return;
            }
            // Elle s'efface à mesure que l'occasion passe.
            _ghost.Modulate = GhostTint with { A = GhostTint.A * Mathf.Clamp(_remaining / _config.Seconds + 0.3f, 0f, 1f) };
        }
        else if (_fade > 0f)
        {
            _fade -= dt;
            _ghost.Modulate = GhostTint with { A = GhostTint.A * Mathf.Clamp(_fade / FadeAfterAttack, 0f, 1f) };
            if (_fade <= 0f)
                _ghost.Visible = false;
        }
    }

    private void Arm(Vector2 origin)
    {
        _player.ArmAttackEcho(origin, _config.DamageRatio);
        _remaining = _config.Seconds;
        _fade = 0f;
        ShowGhost(origin);
    }

    /// <summary>La silhouette de l'Éveillée telle qu'elle était au départ, figée là où elle était.</summary>
    private void ShowGhost(Vector2 origin)
    {
        if (_playerSprite?.SpriteFrames == null)
            return;
        _ghost.Texture = _playerSprite.SpriteFrames.GetFrameTexture(_playerSprite.Animation, _playerSprite.Frame);
        _ghost.Offset = _playerSprite.Offset;
        _ghost.FlipH = _playerSprite.FlipH;
        _ghost.Scale = _playerSprite.GlobalScale;
        _ghost.GlobalPosition = origin + _playerSprite.Position;
        _ghost.Modulate = GhostTint;
        _ghost.Visible = true;
    }

    private void OnAttackEchoed(Vector2 origin)
    {
        _remaining = 0f;
        _fade = FadeAfterAttack;
        CombatPools.Instance?.EmitSparks(origin + new Vector2(0f, -Iso.FlightHeight), new SparkBurst
        {
            Family = FxFamily.Essence,
            Owner = FxOwner.Player,
            Count = 6,
            Spread = Mathf.Tau,
            SpeedMin = 20f,
            SpeedMax = 50f,
            LifeMin = 0.15f,
            LifeMax = 0.3f,
            Size = 1,
        });
    }
}
