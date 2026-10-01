using Godot;

namespace Vestiges.UI;

/// <summary>Éclat de rareté, reflet légendaire et révélation des rangs gagnés par la Chance.</summary>
public partial class RarityIcon : TextureRect
{
    private Texture2D[] _frames;
    private Texture2D[][] _jumps;
    private float _age;
    private float _delay;
    private int _last = -1;

    public RarityIcon() : this(0) { }

    public RarityIcon(int rank, int size = 24)
    {
        _frames = RarityArt.Icons(rank, size);
        Texture = _frames[0];
        CustomMinimumSize = Vector2.One * size;
        ExpandMode = ExpandModeEnum.IgnoreSize;
        StretchMode = StretchModeEnum.KeepCentered;
        TextureFilter = TextureFilterEnum.Nearest;
        MouseFilter = MouseFilterEnum.Ignore;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        SetProcess(_frames.Length > 1);
    }

    public void RevealFrom(int initialRank, int finalRank, float delay)
    {
        int count = finalRank - initialRank;
        if (count <= 0)
            return;
        // L'éclatement fait 32 px : garder ses pixels natifs plutôt que le réduire à 24 px.
        CustomMinimumSize = new Vector2(32, 32);
        _jumps = new Texture2D[count][];
        for (int i = 0; i < count; i++)
            _jumps[i] = RarityArt.Jump(initialRank + i);
        Texture = RarityArt.Icons(initialRank, 24)[0];
        _age = 0f;
        _delay = delay;
        _last = -1;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree())
            return;
        _age += (float)delta;
        int frame;
        if (_jumps != null)
        {
            if (_age < _delay)
                return;
            frame = (int)((_age - _delay) * RarityArt.Fps);
            int poses = _jumps[0].Length;
            if (frame < poses * _jumps.Length)
            {
                if (frame != _last)
                    Texture = _jumps[frame / poses][frame % poses];
                _last = frame;
                return;
            }
            _jumps = null;
            _last = -1;
            SetProcess(_frames.Length > 1);
        }
        frame = (int)(_age * RarityArt.Fps) % _frames.Length;
        if (frame != _last)
            Texture = _frames[frame];
        _last = frame;
    }
}
