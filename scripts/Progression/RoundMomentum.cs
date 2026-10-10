using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Tournée du Facteur (plan 06 P2c) : l'élan monte tant qu'il avance, la glissade comprise, jusqu'au plein en
/// <see cref="PerkMomentum.RampSeconds"/> ; un arrêt plus long que la tolérance, une touche ou l'eau le remettent à
/// zéro. L'élan passe au joueur par paliers, seul l'écart allant à ApplyPerkModifier, pour ne pas relancer les
/// minuteurs d'armes à chaque frame. Des lettres s'envolent derrière lui, d'autant plus souvent que l'élan est haut :
/// c'est sa jauge.
/// </summary>
public partial class RoundMomentum : Node
{
    private const float MovingSpeedSq = 25f;
    private const int Steps = 20;
    private const float WaterCheckSeconds = 0.1f;
    private const int LetterCount = 10;
    private const float LetterLife = 0.9f;
    private const float LetterThreshold = 0.25f;
    private const float LetterHeight = 22f;
    private const int HitScatter = 3;

    private Player _player;
    private PerkMomentum _config;
    private EventBus _eventBus;
    private float _momentum;
    private float _stillSeconds;
    private int _appliedStep;
    private float _waterClock;
    private bool _onWater;

    private ProjectileSprites.SpriteSet _letterSprites;
    private readonly Sprite2D[] _letters = new Sprite2D[LetterCount];
    private readonly Vector2[] _letterVelocity = new Vector2[LetterCount];
    private readonly float[] _letterAge = new float[LetterCount];
    private int _nextLetter;
    private float _emitClock;

    /// <summary>Élan de 0 à 1.</summary>
    public float Momentum => _momentum;

    /// <summary>Palier appliqué au joueur, de 0 à <see cref="Steps"/>.</summary>
    public int AppliedStep => _appliedStep;

    public void Initialize(Player player, PerkMomentum config)
    {
        _player = player;
        _config = config;
    }

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.PlayerDamageResolved += OnPlayerDamage;
        _letterSprites = ProjectileSprites.Get("letter");
        for (int i = 0; i < LetterCount; i++)
        {
            // Hors du tri en Y des entités : une lettre en l'air passe devant ce qu'elle survole.
            _letters[i] = new Sprite2D
            {
                Visible = false,
                TopLevel = true,
                ZIndex = 5,
                ZAsRelative = false,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            };
            _letterAge[i] = LetterLife;
            AddChild(_letters[i]);
        }
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.PlayerDamageResolved -= OnPlayerDamage;
    }

    public override void _Process(double delta)
    {
        if (_player == null || !IsInstanceValid(_player))
            return;
        float dt = (float)delta;
        Advance(dt);
        UpdateLetters(dt);
    }

    /// <summary>Un pas de l'élan (appelé par _Process, ou directement par un test).</summary>
    public void Advance(float dt)
    {
        _waterClock -= dt;
        if (_waterClock <= 0f)
        {
            _waterClock = WaterCheckSeconds;
            _onWater = _player.IsOnWater();
        }
        bool moving = _player.Velocity.LengthSquared() >= MovingSpeedSq || (_player.Mobility?.IsDashing ?? false);
        if (_player.IsDead || _onWater)
        {
            _momentum = 0f;
        }
        else if (moving)
        {
            _stillSeconds = 0f;
            _momentum = Mathf.Min(1f, _momentum + dt / _config.RampSeconds);
        }
        else
        {
            _stillSeconds += dt;
            if (_stillSeconds > _config.StopGraceSeconds)
                _momentum = 0f;
        }
        ApplyStep();
        EmitWhileMoving(dt, moving);
    }

    private void OnPlayerDamage(PlayerDamageResult result)
    {
        if (_player == null || result.PlayerId != _player.GetInstanceId() || !result.Applied
            || result.Kind != PlayerDamageKind.Combat || (result.HpLost <= 0f && !result.ShieldAbsorbed))
            return;
        // Le courrier s'éparpille : la jauge perdue se voit.
        if (_momentum >= LetterThreshold)
            for (int i = 0; i < HitScatter; i++)
                EmitLetter(Vector2.Zero, i);
        _momentum = 0f;
        ApplyStep();
    }

    private void ApplyStep()
    {
        int step = Mathf.FloorToInt(_momentum * Steps);
        if (step == _appliedStep)
            return;
        float previous = (float)_appliedStep / Steps;
        float next = (float)step / Steps;
        _player.ApplyPerkModifier("speed", (1f + _config.SpeedBonus * next) / (1f + _config.SpeedBonus * previous), "multiplicative");
        _player.ApplyPerkModifier("attack_speed",
            (1f + _config.AttackSpeedBonus * next) / (1f + _config.AttackSpeedBonus * previous), "multiplicative");
        _appliedStep = step;
    }

    private void EmitWhileMoving(float dt, bool moving)
    {
        if (!moving || _momentum < LetterThreshold || _letterSprites == null)
            return;
        _emitClock -= dt;
        if (_emitClock > 0f)
            return;
        _emitClock = Mathf.Lerp(0.5f, 0.12f, (_momentum - LetterThreshold) / (1f - LetterThreshold));
        Vector2 velocity = _player.Velocity;
        EmitLetter(velocity.LengthSquared() > 0.01f ? -velocity.Normalized() : Vector2.Zero, _nextLetter);
    }

    /// <summary>Une lettre part du sac, vers l'arrière et vers le haut, et s'efface ; le pool tourne en boucle.</summary>
    private void EmitLetter(Vector2 backward, int variant)
    {
        if (_letterSprites == null)
            return;
        int index = _nextLetter;
        _nextLetter = (_nextLetter + 1) % LetterCount;
        // Écart latéral alterné plutôt qu'aléatoire : un effet de jauge ne consomme pas le tirage du combat.
        float side = (variant % 3 - 1) * 14f;
        Vector2 across = backward == Vector2.Zero ? Vector2.Right : backward.Orthogonal();
        _letterVelocity[index] = backward * 30f + across * side + new Vector2(0f, -24f);
        _letterAge[index] = 0f;
        Sprite2D letter = _letters[index];
        letter.GlobalPosition = _player.GlobalPosition + backward * 6f + new Vector2(0f, -LetterHeight);
        letter.Texture = _letterSprites.Get(0, variant);
        letter.Modulate = Colors.White;
        letter.Visible = true;
    }

    private void UpdateLetters(float dt)
    {
        for (int i = 0; i < LetterCount; i++)
        {
            if (_letterAge[i] >= LetterLife)
                continue;
            _letterAge[i] += dt;
            Sprite2D letter = _letters[i];
            if (_letterAge[i] >= LetterLife)
            {
                letter.Visible = false;
                continue;
            }
            letter.GlobalPosition += _letterVelocity[i] * dt;
            letter.Texture = _letterSprites.Get(0, _letterSprites.FrameAt(_letterAge[i] + i * 0.1f));
            float fade = Mathf.Clamp((LetterLife - _letterAge[i]) / (LetterLife * 0.4f), 0f, 1f);
            letter.Modulate = new Color(1f, 1f, 1f, fade);
        }
    }
}
