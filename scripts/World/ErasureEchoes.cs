using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Échos de l'oubli (plan 16 O6) : de loin en loin, dans une zone Fragile ou Effilochée proche du joueur,
/// la silhouette pâle d'un habitant réapparaît, immobile ou de passage, puis s'efface. Si le joueur s'en approche,
/// elle se dissout en laissant un murmure. Sans collision ni effet de jeu : un souvenir, pas un événement.
/// Un seul écho à la fois, un seul sprite réutilisé ; réglages dans <c>echoes</c> de data/scaling/erasure.json.
/// </summary>
public partial class ErasureEchoes : Node2D
{
    private enum State { Hidden, Appearing, Present, Fading, Dissolving }

    private const int SpawnAttempts = 8;
    private const float RetryDelaySec = 3f;
    private const float WalkCheckSec = 0.5f;
    // Cadre des sprites de personnages (32×48) : pieds à (16, 36), comme au camp du Hub.
    private static readonly Vector2 FrameFeet = new(16f, 36f);
    private static readonly StringName PresenceParam = "presence";
    private static readonly StringName DissolveParam = "dissolve";

    private EchoConfig _config;
    private ErasureManager _erasure;
    private WorldSetup _world;
    private Player _player;
    private readonly RandomNumberGenerator _rng = new();
    private readonly List<SpriteFrames> _frames = new();

    private AnimatedSprite2D _sprite;
    private ShaderMaterial _material;
    private Label _whisper;
    private State _state = State.Hidden;
    private float _cooldown;
    private float _age;
    private float _stateTime;
    private float _whisperTime = -1f;
    private float _walkCheck;
    private Vector2 _whisperOrigin;
    private Vector2 _walk;

    public override void _Ready()
    {
        _config = EchoConfig.Load();
        if (!_config.Enabled)
        {
            SetProcess(false);
            return;
        }

        foreach (string id in _config.Characters)
        {
            CharacterData data = CharacterDataLoader.Get(id);
            SpriteFrames frames = data == null ? null : CharacterSpriteLoader.LoadOrGet(data.Id, data.SpriteFolder);
            if (frames != null && CharacterSpriteLoader.HasEightDirections(frames))
                _frames.Add(frames);
        }
        if (_frames.Count == 0)
        {
            GD.PushWarning("[ErasureEchoes] aucun personnage utilisable : échos désactivés.");
            SetProcess(false);
            return;
        }

        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/echo.gdshader") };
        _sprite = new AnimatedSprite2D
        {
            Centered = false,
            Offset = -FrameFeet,
            TextureFilter = TextureFilterEnum.Nearest,
            Material = _material,
            Visible = false,
        };
        AddChild(_sprite);

        _whisper = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            ZIndex = 30,
            ZAsRelative = false,
            Visible = false,
        };
        _whisper.AddThemeFontSizeOverride("font_size", 9);
        _whisper.AddThemeColorOverride("font_color", new Color(0.86f, 0.9f, 0.95f));
        _whisper.AddThemeColorOverride("font_outline_color", new Color(0.12f, 0.14f, 0.2f, 0.85f));
        _whisper.AddThemeConstantOverride("outline_size", 3);
        AddChild(_whisper);

        _rng.Randomize();
        _cooldown = _config.FirstDelaySec;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        UpdateWhisper(dt);

        if (_state == State.Hidden)
        {
            _cooldown -= dt;
            if (_cooldown <= 0f)
                _cooldown = TrySpawn() ? 0f : RetryDelaySec;
            return;
        }

        _age += dt;
        _stateTime += dt;
        // Le nœud entier se déplace : Main le trie en Y avec les entités.
        Position += _walk * dt;
        _walkCheck -= dt;
        if (_walk != Vector2.Zero && _walkCheck <= 0f)
        {
            _walkCheck = WalkCheckSec;
            StopIfLeavingOblivion();
        }

        if (_state != State.Dissolving && ResolvePlayer()
            && Iso.GroundDistanceSquared(_player.GlobalPosition, GlobalPosition) < _config.DissolveDistancePx * _config.DissolveDistancePx)
        {
            _walk = Vector2.Zero;
            Enter(State.Dissolving);
            ShowWhisper();
        }

        switch (_state)
        {
            case State.Appearing:
                _material.SetShaderParameter(PresenceParam, Mathf.Clamp(_stateTime / _config.FadeSec, 0f, 1f));
                if (_stateTime >= _config.FadeSec)
                    Enter(State.Present);
                break;
            case State.Present:
                if (_age >= _config.LifetimeSec)
                    Enter(State.Fading);
                break;
            case State.Fading:
                _material.SetShaderParameter(PresenceParam, 1f - Mathf.Clamp(_stateTime / _config.FadeSec, 0f, 1f));
                if (_stateTime >= _config.FadeSec)
                    Vanish();
                break;
            case State.Dissolving:
                _material.SetShaderParameter(DissolveParam, Mathf.Clamp(_stateTime / _config.DissolveSec, 0f, 1f));
                if (_stateTime >= _config.DissolveSec)
                    Vanish();
                break;
        }
    }

    /// <summary>Fait apparaître un écho tout de suite, sans attendre le délai (captures de vérification).</summary>
    public bool SpawnNow() => _config is { Enabled: true } && _frames.Count > 0 && _state == State.Hidden && TrySpawn();

    /// <summary>Un écho de passage s'arrête avant de quitter l'oubli ou d'entrer dans l'eau.</summary>
    private void StopIfLeavingOblivion()
    {
        Vector2 ahead = GlobalPosition + _walk * WalkCheckSec * 2f;
        float memory = _erasure.GetMemoryAt(ahead);
        if (memory > _config.MaxMemory || memory < _config.MinMemory || (_world != null && _world.IsWaterAt(ahead)))
            _walk = Vector2.Zero;
    }

    private bool TrySpawn()
    {
        if (!ResolvePlayer())
            return false;
        _erasure ??= GetParent().GetNodeOrNull<ErasureManager>("ErasureManager");
        _world ??= GetParent() as WorldSetup;
        if (_erasure == null)
            return false;

        for (int attempt = 0; attempt < SpawnAttempts; attempt++)
        {
            float angle = _rng.RandfRange(0f, Mathf.Tau);
            float distance = _rng.RandfRange(_config.MinDistancePx, _config.MaxDistancePx);
            Vector2 point = _player.GlobalPosition + Iso.ToScreen(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance);
            float memory = _erasure.GetMemoryAt(point);
            if (memory < _config.MinMemory || memory > _config.MaxMemory)
                continue;
            if (_world != null && _world.IsWaterAt(point))
                continue;
            Show(point);
            return true;
        }
        return false;
    }

    private void Show(Vector2 point)
    {
        GlobalPosition = point;
        _sprite.SpriteFrames = _frames[_rng.RandiRange(0, _frames.Count - 1)];

        bool walking = _rng.Randf() < _config.WalkChance;
        float heading = _rng.RandfRange(0f, Mathf.Tau);
        Vector2 direction = new(Mathf.Cos(heading), Mathf.Sin(heading));
        _walk = walking ? Iso.ToScreen(direction) * _config.WalkSpeedPx : Vector2.Zero;
        string facing = CharacterFacing.DirectionNames[Mathf.PosMod(Mathf.RoundToInt(Iso.ToScreen(direction).Angle() / (Mathf.Pi / 4f)), 8)];
        _sprite.Play($"{facing}_{(walking ? "walk" : "idle")}");
        _sprite.Frame = _rng.RandiRange(0, 3);

        _material.SetShaderParameter(PresenceParam, 0f);
        _material.SetShaderParameter(DissolveParam, 0f);
        _sprite.Visible = true;
        _age = 0f;
        _walkCheck = 0f;
        Enter(State.Appearing);
    }

    private void Vanish()
    {
        _sprite.Visible = false;
        _sprite.Stop();
        _state = State.Hidden;
        _cooldown = _rng.RandfRange(_config.IntervalMinSec, _config.IntervalMaxSec);
    }

    private void Enter(State state)
    {
        _state = state;
        _stateTime = 0f;
    }

    private void ShowWhisper()
    {
        if (_config.WhisperCount <= 0)
            return;
        int index = _rng.RandiRange(1, _config.WhisperCount);
        _whisper.Text = Tr($"ECHO_WHISPER_{index:00}");
        _whisper.ResetSize();
        _whisperOrigin = new Vector2(-_whisper.Size.X * 0.5f, -FrameFeet.Y - 14f);
        _whisper.Position = _whisperOrigin;
        _whisper.Modulate = Colors.White;
        _whisper.Visible = true;
        _whisperTime = 0f;
    }

    private void UpdateWhisper(float dt)
    {
        if (_whisperTime < 0f)
            return;
        _whisperTime += dt;
        float t = _whisperTime / _config.WhisperSec;
        _whisper.Position = _whisperOrigin + new Vector2(0f, -_config.WhisperRisePx * t);
        _whisper.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp((1f - t) * 2f, 0f, 1f));
        if (t >= 1f)
        {
            _whisper.Visible = false;
            _whisperTime = -1f;
        }
    }

    private bool ResolvePlayer()
    {
        if (_player != null && IsInstanceValid(_player))
            return true;
        _player = GetParent().GetNodeOrNull<Player>("Player");
        return _player != null;
    }
}
