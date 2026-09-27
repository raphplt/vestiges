using Godot;
using Vestiges.Combat;
using Vestiges.Core;

namespace Vestiges.World;

/// <summary>
/// Reflets sur les points d'intérêt (plan 02 J6) : de temps en temps, un point d'intérêt inexploré proche du joueur
/// accroche la lumière d'un éclat bref, pour attirer l'œil sans marqueur d'interface. Un seul éclat par tirage, tiré
/// parmi les points d'intérêt de GroupCache (quelques dizaines) ; rien quand aucun n'est proche.
/// </summary>
public partial class PoiGlints : Node
{
    private const float Interval = 0.8f;
    private const float Range = 520f;

    private readonly RandomNumberGenerator _rng = new();
    private GroupCache _groups;
    private float _timer;

    public override void _Ready()
    {
        _groups = GetNode<GroupCache>("/root/GroupCache");
        _rng.Randomize();
    }

    public override void _Process(double delta)
    {
        _timer -= (float)delta;
        if (_timer > 0f)
            return;
        _timer = Interval;
        if (CombatPools.Instance == null || CombatFxSettings.ParticleLevel == ParticleLevel.Off
            || _groups.GetPlayer() is not Node2D player)
            return;

        // Tirage de réservoir : un point d'intérêt au hasard parmi ceux à portée, sans liste intermédiaire.
        PointOfInterest chosen = null;
        int seen = 0;
        float rangeSq = Range * Range;
        foreach (Node node in _groups.GetPois())
        {
            if (node is not PointOfInterest poi || poi.IsExplored
                || poi.GlobalPosition.DistanceSquaredTo(player.GlobalPosition) > rangeSq)
                continue;
            seen++;
            if (_rng.RandiRange(1, seen) == 1)
                chosen = poi;
        }
        if (chosen == null)
            return;

        Vector2 offset = new(_rng.RandfRange(-8f, 8f), -chosen.VisualHeight * _rng.RandfRange(0.4f, 0.9f));
        PixelFxSpec glint = PixelFxSpec.Of(PixelFxShape.Star, FxFamily.Crit, 6f, 1f, 0.35f);
        glint.Steps = 4;
        glint.FadeTail = 0.5f;
        glint.ZIndex = 3;
        CombatPools.Instance.PlayFx(chosen.GlobalPosition + offset, glint, FxOwner.World);
    }
}
