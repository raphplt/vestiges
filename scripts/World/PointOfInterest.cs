using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Point d'intérêt explorable sur la map.
/// Le joueur s'approche et interagit pour fouiller, récupérer du loot, ou déclencher un événement.
/// Chaque type a une forme visuelle distincte : building, crate, ruin, chest_platform, figure, rift.
/// </summary>
public partial class PointOfInterest : StaticBody2D
{
    private string _poiId;
    private PoiData _data;
    private bool _isExplored;
    private bool _guardsDefeated;
    private int _guardCount;
    private Polygon2D _visual;
    private Polygon2D _outline;
    private Polygon2D _indicator;
    private Polygon2D _guardRing;
    private Color _originalColor;
    private EventBus _eventBus;

    public bool IsExplored => _isExplored;
    public string PoiId => _poiId;
    public string PoiType => _data?.Type ?? "";
    public string InteractionType => _data?.InteractionType ?? "";
    public float SearchTime => _data?.SearchTime ?? 1f;
    public string LootTableId => _data?.LootTableId ?? "";
    public int LootRolls => _data?.LootRolls ?? 2;
    public int ScorePoints => _data?.ScorePoints ?? 0;

    public bool CanInteract
    {
        get
        {
            if (_isExplored)
                return false;
            if (_data?.InteractionType == "guarded" && !_guardsDefeated)
                return false;
            return true;
        }
    }

    public override void _Ready()
    {
        _visual = GetNode<Polygon2D>("Visual");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        AddToGroup("pois");
    }

    public void Initialize(PoiData data)
    {
        _poiId = data.Id;
        _data = data;
        _originalColor = data.Color;

        float s = data.Size * 10f;
        Vector2[] shape = BuildShape(data.Shape, s);
        _visual.Polygon = shape;
        _visual.Color = data.Color;

        CreateOutline(shape, data.OutlineColor);
        CreateIndicator(s);

        // Les POI gardés sans gardes actifs sont directement accessibles
        if (data.EnemyGuards.Count == 0)
        {
            _guardsDefeated = true;
        }
        else
        {
            _guardCount = data.EnemyGuards.Count;
            CreateGuardRing(s);
        }
    }

    /// <summary>Marque le POI comme exploré. Émet le signal PoiExplored.</summary>
    public void Explore()
    {
        if (_isExplored)
            return;

        _isExplored = true;

        PlayExploreEffect();

        // Feedback visuel : couleur atténuée, indicateur masqué (après le flash)
        Tween fadeTween = CreateTween();
        fadeTween.TweenProperty(_visual, "color", new Color(_originalColor, 0.4f), 0.3f).SetDelay(0.15f);
        if (_outline != null)
            fadeTween.Parallel().TweenProperty(_outline, "color", new Color(_outline.Color, 0.3f), 0.3f).SetDelay(0.15f);
        if (_indicator != null)
            _indicator.Visible = false;

        _eventBus?.EmitSignal(EventBus.SignalName.PoiExplored, _poiId, _data?.Type ?? "");
    }

    private void PlayExploreEffect()
    {
        // Flash blanc
        Color prevColor = _visual.Color;
        _visual.Color = Colors.White;
        Tween flashTween = CreateTween();
        flashTween.TweenProperty(_visual, "color", prevColor, 0.2f).SetDelay(0.06f);

        // Scale bounce
        Tween scaleTween = CreateTween();
        scaleTween.TweenProperty(this, "scale", Vector2.One * 1.2f, 0.08f);
        scaleTween.TweenProperty(this, "scale", Vector2.One * 0.9f, 0.1f);
        scaleTween.TweenProperty(this, "scale", Vector2.One, 0.08f);

        // Particules dorées qui s'envolent
        SpawnExploreParticles();
    }

    private void SpawnExploreParticles()
    {
        int count = 8;
        Color particleColor = new(0.9f, 0.8f, 0.4f);

        for (int i = 0; i < count; i++)
        {
            Polygon2D particle = new();
            float ps = (float)GD.RandRange(2f, 4.5f);
            particle.Polygon = new Vector2[]
            {
                new(-ps, 0), new(0, -ps * 0.6f), new(ps, 0), new(0, ps * 0.6f)
            };

            float hueShift = (float)GD.RandRange(-0.06f, 0.06f);
            particle.Color = new Color(
                Mathf.Clamp(particleColor.R + hueShift, 0, 1),
                Mathf.Clamp(particleColor.G + hueShift, 0, 1),
                Mathf.Clamp(particleColor.B + hueShift * 0.5f, 0, 1)
            );

            particle.GlobalPosition = GlobalPosition;
            GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, particle);

            float angle = (float)GD.RandRange(0, Mathf.Tau);
            float dist = (float)GD.RandRange(25f, 55f);
            Vector2 target = GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist
                + new Vector2(0, -15f);

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
                t.TweenProperty(p, "global_position", target, 0.5f)
                    .SetTrans(Tween.TransitionType.Quad)
                    .SetEase(Tween.EaseType.Out);
                t.TweenProperty(p, "modulate:a", 0f, 0.5f)
                    .SetDelay(0.2f);
                t.TweenProperty(p, "scale", Vector2.One * 0.2f, 0.5f);
                t.Chain().TweenCallback(cleanup);
            };
        }
    }

    /// <summary>Appelé par un garde quand il meurt. Quand tous les gardes sont éliminés, le POI devient accessible.</summary>
    public void OnGuardKilled()
    {
        _guardCount--;
        if (_guardCount <= 0)
        {
            _guardsDefeated = true;
            _guardCount = 0;

            // Feedback : anneau rouge disparaît, flash vert
            if (_guardRing != null)
            {
                Tween ringTween = CreateTween();
                ringTween.TweenProperty(_guardRing, "modulate:a", 0f, 0.4f);
                ringTween.TweenCallback(Callable.From(() => { _guardRing.QueueFree(); _guardRing = null; }));
            }

            FlashColor(new Color(0.3f, 1f, 0.4f));
        }
    }

    /// <summary>Initialise le nombre de gardes attendus (appelé par PoiManager).</summary>
    public void SetGuardCount(int count)
    {
        _guardCount = count;
        if (count == 0)
            _guardsDefeated = true;
    }

    /// <summary>Anneau rouge pulsant autour des POI gardés.</summary>
    private void CreateGuardRing(float size)
    {
        _guardRing = new Polygon2D();
        float r = size * 0.8f;
        int segments = 16;
        Vector2[] ring = new Vector2[segments];
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.Tau * i / segments;
            ring[i] = new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r * 0.5f);
        }
        _guardRing.Polygon = ring;
        _guardRing.Color = new Color(0.9f, 0.2f, 0.15f, 0.35f);
        _guardRing.ZIndex = -1;
        _visual.AddChild(_guardRing);

        Tween tween = CreateTween();
        tween.SetLoops();
        tween.TweenProperty(_guardRing, "modulate:a", 0.4f, 1.0f)
            .SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(_guardRing, "modulate:a", 1f, 1.0f)
            .SetTrans(Tween.TransitionType.Sine);
    }

    private void FlashColor(Color flashColor)
    {
        Color prev = _visual.Color;
        _visual.Color = flashColor;
        Tween tween = CreateTween();
        tween.TweenProperty(_visual, "color", prev, 0.5f).SetDelay(0.2f);
    }

    private static Vector2[] BuildShape(string shape, float s)
    {
        return shape switch
        {
            // Bâtiment : rectangle large
            "building" => new Vector2[]
            {
                new(-s * 0.6f, -s * 0.4f),
                new(s * 0.6f, -s * 0.4f),
                new(s * 0.6f, s * 0.4f),
                new(-s * 0.6f, s * 0.4f)
            },
            // Caisse : petit carré
            "crate" => new Vector2[]
            {
                new(-s * 0.35f, -s * 0.35f),
                new(s * 0.35f, -s * 0.35f),
                new(s * 0.35f, s * 0.35f),
                new(-s * 0.35f, s * 0.35f)
            },
            // Ruine : forme irrégulière
            "ruin" => new Vector2[]
            {
                new(-s * 0.4f, -s * 0.5f),
                new(s * 0.2f, -s * 0.55f),
                new(s * 0.55f, -s * 0.15f),
                new(s * 0.4f, s * 0.35f),
                new(-s * 0.1f, s * 0.5f),
                new(-s * 0.5f, s * 0.2f)
            },
            // Plateforme de coffre : trapèze
            "chest_platform" => new Vector2[]
            {
                new(-s * 0.5f, -s * 0.3f),
                new(s * 0.5f, -s * 0.3f),
                new(s * 0.4f, s * 0.3f),
                new(-s * 0.4f, s * 0.3f)
            },
            // Figure humanoïde simplifiée
            "figure" => new Vector2[]
            {
                new(0, -s * 0.6f),
                new(s * 0.25f, -s * 0.3f),
                new(s * 0.2f, s * 0.1f),
                new(s * 0.35f, s * 0.5f),
                new(s * 0.1f, s * 0.5f),
                new(0, s * 0.2f),
                new(-s * 0.1f, s * 0.5f),
                new(-s * 0.35f, s * 0.5f),
                new(-s * 0.2f, s * 0.1f),
                new(-s * 0.25f, -s * 0.3f)
            },
            // Faille : losange allongé
            "rift" => new Vector2[]
            {
                new(0, -s * 0.7f),
                new(s * 0.3f, 0),
                new(0, s * 0.7f),
                new(-s * 0.3f, 0)
            },
            // Sanctuaire : bâtiment intact, symétrique, avec toit pointu
            "sanctuary" => new Vector2[]
            {
                new(0, -s * 0.7f),
                new(s * 0.35f, -s * 0.35f),
                new(s * 0.5f, -s * 0.35f),
                new(s * 0.5f, s * 0.4f),
                new(-s * 0.5f, s * 0.4f),
                new(-s * 0.5f, -s * 0.35f),
                new(-s * 0.35f, -s * 0.35f)
            },
            // Fallback : diamant isométrique
            _ => new Vector2[] { new(-s, 0), new(0, -s * 0.5f), new(s, 0), new(0, s * 0.5f) }
        };
    }

    private void CreateOutline(Vector2[] shape, Color outlineColor)
    {
        _outline = new Polygon2D();
        float outlineThickness = 2.5f;

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

    /// <summary>Petit indicateur pulsant au-dessus du POI non exploré.</summary>
    private void CreateIndicator(float size)
    {
        _indicator = new Polygon2D();
        float s = size * 0.15f;
        _indicator.Polygon = new Vector2[]
        {
            new(0, -s),
            new(s * 0.6f, 0),
            new(0, s),
            new(-s * 0.6f, 0)
        };
        _indicator.Color = new Color(1f, 0.9f, 0.4f, 0.8f);
        _indicator.Position = new Vector2(0, -size * 0.6f);
        _visual.AddChild(_indicator);

        AnimateIndicator();
    }

    private void AnimateIndicator()
    {
        if (_indicator == null)
            return;

        Tween tween = CreateTween();
        tween.SetLoops();
        tween.TweenProperty(_indicator, "modulate:a", 0.3f, 0.8f)
            .SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(_indicator, "modulate:a", 1f, 0.8f)
            .SetTrans(Tween.TransitionType.Sine);
    }
}
