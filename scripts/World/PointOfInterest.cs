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
    private Polygon2D _visual;
    private Polygon2D _outline;
    private Polygon2D _indicator;
    private Color _originalColor;
    private EventBus _eventBus;

    public bool IsExplored => _isExplored;
    public string PoiId => _poiId;
    public string PoiType => _data?.Type ?? "";
    public string InteractionType => _data?.InteractionType ?? "";
    public float SearchTime => _data?.SearchTime ?? 1f;
    public string LootTableId => _data?.LootTableId ?? "";
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
            _guardsDefeated = true;
    }

    /// <summary>Marque le POI comme exploré. Émet le signal PoiExplored.</summary>
    public void Explore()
    {
        if (_isExplored)
            return;

        _isExplored = true;

        // Feedback visuel : couleur atténuée, indicateur masqué
        _visual.Color = new Color(_originalColor, 0.4f);
        if (_outline != null)
            _outline.Color = new Color(_outline.Color, 0.3f);
        if (_indicator != null)
            _indicator.Visible = false;

        _eventBus?.EmitSignal(EventBus.SignalName.PoiExplored, _poiId, _data?.Type ?? "");
    }

    /// <summary>Appelé quand les gardes du POI sont vaincus.</summary>
    public void MarkGuardsDefeated()
    {
        _guardsDefeated = true;
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
