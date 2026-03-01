using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Coffre récupérable dans le monde. 4 raretés : common, rare, epic, lore.
/// Le joueur interagit pour ouvrir avec une barre de progression.
/// </summary>
public partial class Chest : StaticBody2D
{
    private ChestData _chestData;
    private bool _isOpened;
    private Polygon2D _visual;
    private Polygon2D _outline;
    private Polygon2D _glowEffect;
    private Color _originalColor;
    private EventBus _eventBus;

    public bool IsOpened => _isOpened;
    public float OpenTime => _chestData?.OpenTime ?? 0.5f;
    public string ChestId => _chestData?.Id ?? "";
    public string Rarity => _chestData?.Rarity ?? "common";
    public string LootTableId => _chestData?.LootTableId ?? "";
    public int LootRolls => _chestData?.LootRolls ?? 1;
    public int ScorePoints => _chestData?.ScorePoints ?? 25;

    public bool CanOpen => !_isOpened;

    public override void _Ready()
    {
        _visual = GetNode<Polygon2D>("Visual");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        AddToGroup("chests");
    }

    public void Initialize(ChestData data)
    {
        _chestData = data;
        _originalColor = data.Color;

        float s = data.Size;
        Vector2[] shape = BuildChestShape(s);
        _visual.Polygon = shape;
        _visual.Color = data.Color;

        CreateOutline(shape, data.OutlineColor);
        CreateRarityGlow(data.Rarity, s);
    }

    /// <summary>Ouvre le coffre. Émet ChestOpened. Retourne la liste des loots.</summary>
    public System.Collections.Generic.List<LootResolver.LootResult> Open()
    {
        if (_isOpened)
            return new();

        _isOpened = true;

        // Feedback visuel
        _visual.Color = new Color(_originalColor, 0.3f);
        if (_outline != null)
            _outline.Color = new Color(_outline.Color, 0.2f);
        if (_glowEffect != null)
            _glowEffect.Visible = false;

        PlayOpenAnimation();
        SpawnOpenParticles();

        _eventBus?.EmitSignal(EventBus.SignalName.ChestOpened,
            _chestData?.Id ?? "", _chestData?.Rarity ?? "common", GlobalPosition);

        return LootResolver.Roll(LootTableId, LootRolls);
    }

    private static Vector2[] BuildChestShape(float s)
    {
        return new Vector2[]
        {
            new(-s * 0.5f, -s * 0.35f),
            new(s * 0.5f, -s * 0.35f),
            new(s * 0.5f, s * 0.2f),
            new(s * 0.3f, s * 0.35f),
            new(-s * 0.3f, s * 0.35f),
            new(-s * 0.5f, s * 0.2f)
        };
    }

    private void CreateOutline(Vector2[] shape, Color outlineColor)
    {
        _outline = new Polygon2D();
        float outlineThickness = 2f;

        Vector2 center = Vector2.Zero;
        for (int i = 0; i < shape.Length; i++)
            center += shape[i];
        center /= shape.Length;

        Vector2[] outerShape = new Vector2[shape.Length];
        for (int i = 0; i < shape.Length; i++)
        {
            Vector2 dir = (shape[i] - center).Normalized();
            outerShape[i] = shape[i] + dir * outlineThickness;
        }

        _outline.Polygon = outerShape;
        _outline.Color = outlineColor;
        _outline.ZIndex = -1;
        _visual.AddChild(_outline);
    }

    private void CreateRarityGlow(string rarity, float size)
    {
        if (rarity == "common")
            return;

        Color glowColor = rarity switch
        {
            "rare" => new Color(0.5f, 0.4f, 0.8f, 0.4f),
            "epic" => new Color(0.9f, 0.6f, 0.15f, 0.5f),
            "lore" => new Color(0.8f, 0.85f, 1f, 0.4f),
            _ => new Color(1f, 1f, 1f, 0.3f)
        };

        _glowEffect = new Polygon2D();
        float gs = size * 0.8f;
        _glowEffect.Polygon = new Vector2[]
        {
            new(-gs, 0), new(0, -gs * 0.5f),
            new(gs, 0), new(0, gs * 0.5f)
        };
        _glowEffect.Color = glowColor;
        _glowEffect.ZIndex = -2;
        _visual.AddChild(_glowEffect);

        AnimateGlow();
    }

    private void AnimateGlow()
    {
        if (_glowEffect == null)
            return;

        Tween tween = CreateTween();
        tween.SetLoops();
        tween.TweenProperty(_glowEffect, "modulate:a", 0.4f, 1f)
            .SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(_glowEffect, "modulate:a", 1f, 1f)
            .SetTrans(Tween.TransitionType.Sine);
    }

    private void PlayOpenAnimation()
    {
        // Flash blanc bref
        Color prevColor = _visual.Color;
        _visual.Color = Colors.White;

        Tween tween = CreateTween();
        tween.TweenProperty(_visual, "color", prevColor, 0.15f).SetDelay(0.05f);

        Tween scaleTween = CreateTween();
        scaleTween.TweenProperty(this, "scale", Vector2.One * 1.3f, 0.08f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        scaleTween.TweenProperty(this, "scale", Vector2.One * 0.85f, 0.1f);
        scaleTween.TweenProperty(this, "scale", Vector2.One, 0.08f);
    }

    /// <summary>Burst de particules colorées à l'ouverture, proportionnel à la rareté.</summary>
    private void SpawnOpenParticles()
    {
        string rarity = _chestData?.Rarity ?? "common";
        int count = rarity switch
        {
            "epic" => 12,
            "rare" => 8,
            "lore" => 10,
            _ => 5
        };

        Color particleColor = rarity switch
        {
            "epic" => new Color(0.9f, 0.6f, 0.15f),
            "rare" => new Color(0.5f, 0.4f, 0.8f),
            "lore" => new Color(0.8f, 0.85f, 1f),
            _ => new Color(0.9f, 0.85f, 0.6f)
        };

        for (int i = 0; i < count; i++)
        {
            Polygon2D particle = new();
            float ps = (float)GD.RandRange(2f, 5f);
            particle.Polygon = new Vector2[]
            {
                new(-ps, 0), new(0, -ps * 0.6f), new(ps, 0), new(0, ps * 0.6f)
            };

            float hueShift = (float)GD.RandRange(-0.08f, 0.08f);
            particle.Color = new Color(
                Mathf.Clamp(particleColor.R + hueShift, 0, 1),
                Mathf.Clamp(particleColor.G + hueShift, 0, 1),
                Mathf.Clamp(particleColor.B + hueShift * 0.5f, 0, 1)
            );

            particle.GlobalPosition = GlobalPosition;
            GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, particle);

            float angle = (float)GD.RandRange(0, Mathf.Tau);
            float dist = (float)GD.RandRange(20f, 50f);
            Vector2 target = GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

            // Capture pour la lambda
            Polygon2D p = particle;
            Callable cleanup = Callable.From(() =>
            {
                if (IsInstanceValid(p))
                    p.QueueFree();
            });

            SceneTreeTimer timer = GetTree().CreateTimer(0f);
            timer.Timeout += () =>
            {
                if (!IsInstanceValid(p))
                    return;
                Tween t = p.CreateTween();
                t.SetParallel();
                t.TweenProperty(p, "global_position", target, 0.4f)
                    .SetTrans(Tween.TransitionType.Quad)
                    .SetEase(Tween.EaseType.Out);
                t.TweenProperty(p, "modulate:a", 0f, 0.4f)
                    .SetDelay(0.15f);
                t.TweenProperty(p, "scale", Vector2.One * 0.3f, 0.4f);
                t.Chain().TweenCallback(cleanup);
            };
        }
    }
}
