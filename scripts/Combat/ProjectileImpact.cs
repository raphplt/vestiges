using System;
using Godot;

namespace Vestiges.Combat;

/// <summary>Éclatement au sol en quatre poses, recyclé et sans collision ni effet de gameplay.</summary>
public partial class ProjectileImpact : Sprite2D
{
    private Action<ProjectileImpact> _release;
    private ProjectileSprites.SpriteSet _sprites;
    private float _age;
    private int _frame;
    private bool _playing;

    public static ProjectileImpact Create(Action<ProjectileImpact> release) => new()
    {
        _release = release,
        TextureFilter = TextureFilterEnum.Nearest,
        ZAsRelative = false,
        ZIndex = -1,
        Visible = false,
        ProcessMode = ProcessModeEnum.Disabled,
    };

    public void Play(Vector2 position, ProjectileSprites.SpriteSet sprites)
    {
        _sprites = sprites;
        _age = 0f;
        _frame = 0;
        _playing = true;
        Texture = sprites.Get(0, 0);
        GlobalPosition = position;
        Modulate = Colors.White with { A = CombatFxSettings.EnemyOpacity };
        Visible = true;
        ProcessMode = ProcessModeEnum.Inherit;
    }

    public override void _Process(double delta)
    {
        if (!_playing)
            return;
        _age += (float)delta;
        int frame = (int)(_age * _sprites.Fps);
        if (frame >= _sprites.Frames)
        {
            _playing = false;
            Visible = false;
            ProcessMode = ProcessModeEnum.Disabled;
            _release(this);
        }
        else if (frame != _frame)
        {
            _frame = frame;
            Texture = _sprites.Get(0, frame);
        }
    }
}
