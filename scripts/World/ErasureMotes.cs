using System.Collections.Generic;
using Godot;
using Vestiges.Combat;

namespace Vestiges.World;

/// <summary>
/// Les choses se défont (plan 16 O2) : en zone Effacée, des éclats violets du Néant s'élèvent des décors hauts proches du joueur,
/// comme s'ils s'émiettaient vers le ciel. Lit l'index spatial de PropOcclusion (neuf cases autour du joueur) et
/// émet par PixelSparks : aucun nœud créé. Réglages : motes dans data/scaling/erasure.json.
/// </summary>
public partial class ErasureMotes : Node
{
    private readonly List<Rect2> _nearby = new();
    private readonly RandomNumberGenerator _rng = new();
    private ErasureManager _erasure;
    private PropOcclusion _occlusion;
    private Node2D _player;
    private float _timer;

    private float _interval = 0.15f;
    private float _maxMemory = 0.25f;
    private int _propsPerTick = 2;
    private int _count = 3;
    private float _speedMin = 40f;
    private float _speedMax = 90f;
    private float _lifeMin = 0.8f;
    private float _lifeMax = 1.4f;

    public override void _Ready()
    {
        if (!LoadConfig())
            SetProcess(false);
        _rng.Randomize();
    }

    public override void _Process(double delta)
    {
        _timer -= (float)delta;
        if (_timer > 0f)
            return;
        _timer = _interval;

        if (CombatFxSettings.ParticleLevel == ParticleLevel.Off || CombatPools.Instance == null || !Resolve())
            return;
        _occlusion.CollectNear(_player.GlobalPosition, _nearby);
        if (_nearby.Count == 0)
            return;

        for (int i = 0; i < _propsPerTick; i++)
        {
            Rect2 rect = _nearby[_rng.RandiRange(0, _nearby.Count - 1)];
            Vector2 foot = new(rect.GetCenter().X, rect.End.Y);
            if (_erasure.GetMemoryAt(foot) > _maxMemory)
                continue;
            // Du haut de la silhouette : c'est le sommet qui part en premier.
            Vector2 origin = new(_rng.RandfRange(rect.Position.X, rect.End.X), _rng.RandfRange(rect.Position.Y, rect.Position.Y + rect.Size.Y * 0.6f));
            CombatPools.Instance.EmitSparks(origin, new SparkBurst
            {
                Family = FxFamily.Void,
                Owner = FxOwner.World,
                Count = _count,
                Direction = Vector2.Up,
                Spread = 0.7f,
                SpeedMin = _speedMin,
                SpeedMax = _speedMax,
                LifeMin = _lifeMin,
                LifeMax = _lifeMax,
                Size = 2,
                Decorative = true,
            });
        }
    }

    private bool Resolve()
    {
        if (_player != null && _erasure != null && IsInstanceValid(_occlusion))
            return true;
        Node root = GetParent();
        _erasure ??= root.GetNodeOrNull<ErasureManager>("ErasureManager");
        // L'index d'occlusion est refait à chaque terre (plan 51) : l'ancien est libéré avec la carte.
        if (!IsInstanceValid(_occlusion))
            _occlusion = root.GetNodeOrNull<PropOcclusion>("PropOcclusion");
        _player ??= root.GetNodeOrNull<Node2D>("Player");
        return _player != null && _erasure != null && IsInstanceValid(_occlusion);
    }

    /// <summary>Réglages contrôlés avec tout le fichier par <see cref="ErasureConfig"/> (plan 26 Q7d-2) ; sans section, pas d'éclats.</summary>
    private bool LoadConfig()
    {
        ErasureConfig.MotesConfig motes = ErasureConfig.Load().Motes;
        if (motes == null)
            return false;
        _interval = Mathf.Max(0.05f, motes.IntervalSec);
        _maxMemory = motes.MaxMemory;
        _propsPerTick = motes.PropsPerTick;
        _count = motes.Count;
        _speedMin = motes.SpeedMin;
        _speedMax = motes.SpeedMax;
        _lifeMin = motes.LifeMin;
        _lifeMax = motes.LifeMax;
        return motes.Enabled;
    }
}
