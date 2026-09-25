using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Rend semi-transparents les décors hauts derrière lesquels passe le joueur.
/// Index spatial construit une fois (les décors sont statiques) : à chaque tick,
/// seuls les décors de la case du joueur sont testés, quel que soit le nombre total.
/// </summary>
public partial class PropOcclusion : Node
{
    private const float BucketSize = 256f;
    private const float OccludedAlpha = 0.4f;
    // Marge horizontale : le joueur doit vraiment chevaucher la silhouette pour la faire pâlir.
    private const float EdgeInset = 4f;

    private readonly List<EnvironmentProp> _props = new();
    private readonly List<Rect2> _rects = new();
    private readonly Dictionary<Vector2I, List<int>> _buckets = new();
    private readonly List<int> _faded = new();
    private readonly List<int> _nextFaded = new();
    private Node2D _player;

    public int OccluderCount => _props.Count;

    /// <summary>Indexe les décors hauts d'un conteneur. À appeler une fois, après tous les placeurs.</summary>
    public void Build(Node container, Node2D player)
    {
        _player = player;
        float minHeight = PropRules.Current.OccluderMinHeight;
        foreach (Node child in container.GetChildren())
        {
            if (child is not EnvironmentProp prop)
                continue;
            if (!prop.HasCanopy && prop.Footprint.VisibleHeight < minHeight)
                continue;

            int index = _props.Count;
            Rect2 rect = prop.VisibleWorldRect();
            _props.Add(prop);
            _rects.Add(rect);
            Vector2I from = BucketOf(rect.Position);
            Vector2I to = BucketOf(rect.End);
            for (int bx = from.X; bx <= to.X; bx++)
            {
                for (int by = from.Y; by <= to.Y; by++)
                {
                    Vector2I key = new(bx, by);
                    if (!_buckets.TryGetValue(key, out List<int> bucket))
                        _buckets[key] = bucket = new List<int>();
                    bucket.Add(index);
                }
            }
        }
        GD.Print($"[PropOcclusion] {_props.Count} décors occultants indexés dans {_buckets.Count} cases");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player == null || _props.Count == 0)
            return;

        Vector2 feet = _player.GlobalPosition;
        _nextFaded.Clear();
        if (_buckets.TryGetValue(BucketOf(feet), out List<int> bucket))
        {
            foreach (int index in bucket)
            {
                Rect2 rect = _rects[index];
                bool behind = feet.Y < _props[index].GlobalPosition.Y
                    && feet.Y > rect.Position.Y
                    && feet.X > rect.Position.X + EdgeInset
                    && feet.X < rect.End.X - EdgeInset;
                if (behind)
                    _nextFaded.Add(index);
            }
        }

        foreach (int index in _faded)
        {
            if (!_nextFaded.Contains(index))
                _props[index].SetOverallTransparency(1f);
        }
        foreach (int index in _nextFaded)
        {
            if (!_faded.Contains(index))
                _props[index].SetOverallTransparency(OccludedAlpha);
        }
        _faded.Clear();
        _faded.AddRange(_nextFaded);
    }

    private static Vector2I BucketOf(Vector2 point)
    {
        return new Vector2I(Mathf.FloorToInt(point.X / BucketSize), Mathf.FloorToInt(point.Y / BucketSize));
    }
}
