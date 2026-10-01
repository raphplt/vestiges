using System;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>Ramassable animé, lueur au sol et dissolution violette ; effets et durée restent au directeur.</summary>
public partial class FieldBonus : Node2D
{
    private const float IconLift = 14f;
    private Sprite2D _icon;
    private Sprite2D _glow;
    private FieldBonusArt.Sprites _sprites;
    private Action<FieldBonus> _release;
    private Action<FieldBonus> _despawnStarted;
    private float _age;
    private float _lifetime;
    private float _blink;
    private int _frame;
    private bool _disappearing;

    public FieldBonusData Data { get; private set; }
    public bool Active { get; private set; }

    public void SetRelease(Action<FieldBonus> release, Action<FieldBonus> despawnStarted = null)
    {
        _release = release;
        _despawnStarted = despawnStarted;
    }

    public override void _Ready()
    {
        _glow = new Sprite2D { TextureFilter = TextureFilterEnum.Nearest, ZAsRelative = false, ZIndex = -1 };
        AddChild(_glow);
        _icon = new Sprite2D { TextureFilter = TextureFilterEnum.Nearest, Position = new Vector2(0, -IconLift) };
        AddChild(_icon);
        Visible = false;
        SetProcess(false);
    }

    public void Launch(FieldBonusData data, Vector2 position, float lifetime, float blink)
    {
        Data = data;
        GlobalPosition = position;
        _sprites = FieldBonusArt.Get(data.Sprite);
        _age = 0f;
        _frame = 0;
        _lifetime = lifetime;
        _blink = blink;
        _disappearing = false;
        _icon.Texture = _sprites.Idle[0];
        _glow.Texture = _sprites.Glow;
        _glow.Visible = true;
        Modulate = Colors.White;
        Visible = true;
        Active = true;
        SetProcess(true);
    }

    /// <summary>Inactif dès la collecte ; le pool récupère le nœud après les trois poses de disparition.</summary>
    public void Release()
    {
        if (!Active)
            return;
        Active = false;
        _disappearing = true;
        _age = 0f;
        _frame = 0;
        _icon.Texture = _sprites.Disappear[0];
        _glow.Visible = false;
        Modulate = Colors.White;
        _despawnStarted?.Invoke(this);
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        if (_disappearing)
        {
            int pose = (int)(_age * _sprites.DisappearFps);
            if (pose >= _sprites.Disappear.Length)
            {
                _disappearing = false;
                Visible = false;
                SetProcess(false);
                _release?.Invoke(this);
            }
            else if (pose != _frame)
            {
                _frame = pose;
                _icon.Texture = _sprites.Disappear[pose];
            }
            return;
        }
        if (_age >= _lifetime)
        {
            Release();
            return;
        }
        int frame = (int)(_age * _sprites.IdleFps) % _sprites.Idle.Length;
        if (frame != _frame)
        {
            _frame = frame;
            _icon.Texture = _sprites.Idle[frame];
        }
        float left = _lifetime - _age;
        if (_blink > 0f && left < _blink)
            Modulate = Colors.White with { A = Mathf.Sin(_age * Mathf.Lerp(30f, 10f, left / _blink)) > 0f ? 1f : 0.25f };
    }
}
