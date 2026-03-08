using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Base;

/// <summary>
/// Noeud de ressource récoltable (arbre, rocher, débris, cristal).
/// Le joueur s'approche et interagit pour récolter.
/// Utilise un sprite pixel art quand disponible, sinon fallback sur forme procédurale.
/// </summary>
public partial class ResourceNode : StaticBody2D
{
    private string _resourceId;
    private string _shape;
    private int _amountMin;
    private int _amountMax;
    private float _harvestTime;
    private int _harvestsRemaining;

    private Polygon2D _visual;
    private Polygon2D _outline;
    private Sprite2D _sprite;
    private InteractableAura _interactionAura;
    private Color _originalColor;
    private bool _usesSprite;
    private float _harvestBonusMult = 1f;

    public bool IsExhausted => _harvestsRemaining <= 0;
    public string ResourceId => _resourceId;
    public float HarvestTime => _harvestTime;
    public string HarvestSoundKey => _shape == "tree" ? "sfx_recolte_hache" : "sfx_recolte_pioche";

    public override void _Ready()
    {
        _visual = GetNodeOrNull<Polygon2D>("Visual");
        AddToGroup("resources");

        EventBus eventBus = GetNodeOrNull<EventBus>("/root/EventBus");
        if (eventBus != null)
            eventBus.ResourceBonusChanged += OnResourceBonusChanged;
    }

    public override void _ExitTree()
    {
        EventBus eventBus = GetNodeOrNull<EventBus>("/root/EventBus");
        if (eventBus != null)
            eventBus.ResourceBonusChanged -= OnResourceBonusChanged;
    }

    private void OnResourceBonusChanged(float multiplier)
    {
        _harvestBonusMult = multiplier;
    }

    public void Initialize(ResourceData data)
    {
        _resourceId = data.Id;
        _shape = data.Shape;
        _amountMin = data.AmountMin;
        _amountMax = data.AmountMax;
        _harvestTime = data.HarvestTime;
        _harvestsRemaining = data.Harvests;
        _originalColor = data.Color;

        if (TryLoadSprite(data))
        {
            _usesSprite = true;
            if (_visual != null)
                _visual.Visible = false;
        }
        else
        {
            _usesSprite = false;
            BuildPolygonFallback(data);
        }

        CreateInteractionAura(data);
    }

    private bool TryLoadSprite(ResourceData data)
    {
        if (data.Sprites == null || data.Sprites.Count == 0)
            return false;

        int variantIndex = (int)(GD.Randi() % data.Sprites.Count);
        string spritePath = data.Sprites[variantIndex];
        string resPath = spritePath.StartsWith("res://") ? spritePath : $"res://{spritePath}";

        if (!ResourceLoader.Exists(resPath))
        {
            GD.PushWarning($"[ResourceNode] Sprite not found: {resPath}, using polygon fallback");
            return false;
        }

        Texture2D texture = GD.Load<Texture2D>(resPath);
        if (texture == null)
            return false;

        _sprite = new Sprite2D
        {
            Texture = texture,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            // Ancrer le sprite en bas-centre pour un bon placement au sol
            Offset = new Vector2(0, -texture.GetHeight() * 0.5f + 2)
        };
        AddChild(_sprite);

        return true;
    }

    private void BuildPolygonFallback(ResourceData data)
    {
        if (_visual == null)
            return;

        float s = data.Size;
        Vector2[] shape = BuildShape(data.Shape, s);
        _visual.Polygon = shape;
        _visual.Color = data.Color;
        CreateOutline(shape, data.OutlineColor);
    }

    private static Vector2[] BuildShape(string shape, float s)
    {
        return shape switch
        {
            "tree" => new Vector2[]
            {
                new(0, -s * 0.8f),
                new(s * 0.6f, -s * 0.1f),
                new(s * 0.35f, -s * 0.1f),
                new(s * 0.35f, s * 0.5f),
                new(-s * 0.35f, s * 0.5f),
                new(-s * 0.35f, -s * 0.1f),
                new(-s * 0.6f, -s * 0.1f)
            },
            "rock" => BuildRockShape(s),
            "crystal" => new Vector2[]
            {
                new(0, -s * 0.7f),
                new(s * 0.4f, -s * 0.2f),
                new(s * 0.5f, s * 0.2f),
                new(s * 0.15f, s * 0.5f),
                new(-s * 0.15f, s * 0.5f),
                new(-s * 0.5f, s * 0.2f),
                new(-s * 0.4f, -s * 0.2f)
            },
            _ => new Vector2[] { new(-s, 0), new(0, -s * 0.5f), new(s, 0), new(0, s * 0.5f) }
        };
    }

    private static Vector2[] BuildRockShape(float s)
    {
        return new Vector2[]
        {
            new(-s * 0.3f, -s * 0.45f),
            new(s * 0.25f, -s * 0.5f),
            new(s * 0.55f, -s * 0.2f),
            new(s * 0.5f, s * 0.25f),
            new(s * 0.15f, s * 0.45f),
            new(-s * 0.35f, s * 0.4f),
            new(-s * 0.55f, s * 0.1f),
            new(-s * 0.5f, -s * 0.2f)
        };
    }

    private void CreateOutline(Vector2[] shape, Color outlineColor)
    {
        _outline = new Polygon2D();
        float outlineThickness = 2f;
        Vector2[] outerShape = new Vector2[shape.Length];
        Vector2 center = Vector2.Zero;
        for (int i = 0; i < shape.Length; i++)
            center += shape[i];
        center /= shape.Length;

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

    private void CreateInteractionAura(ResourceData data)
    {
        _interactionAura?.QueueFree();
        _interactionAura = new InteractableAura();
        AddChild(_interactionAura);

        (Color baseColor, Color accentColor, bool withMote, float pulseSpeed) = GetAuraStyle(data);
        float radius = Mathf.Max(10f, data.Size * 0.9f);
        float height = data.Shape switch
        {
            "tree" => Mathf.Max(14f, data.Size * 1.15f),
            "crystal" => Mathf.Max(15f, data.Size * 1.2f),
            _ => Mathf.Max(10f, data.Size * 0.9f)
        };

        _interactionAura.Configure(
            baseColor,
            accentColor,
            radius,
            height,
            withMote,
            pulseSpeed,
            baseAlpha: data.Id == "essence" ? 0.16f : 0.09f,
            pulseAlpha: data.Id == "essence" ? 0.07f : 0.04f,
            crownAlpha: data.Id == "essence" ? 0.14f : 0.05f,
            crownPulseAlpha: data.Id == "essence" ? 0.08f : 0.03f);
    }

    private (Color baseColor, Color accentColor, bool withMote, float pulseSpeed) GetAuraStyle(ResourceData data)
    {
        return data.Id switch
        {
            "wood" => (
                Color.FromHtml("#5A4A38"),
                Color.FromHtml("#8A9A4A"),
                false,
                0.85f),
            "stone" => (
                Color.FromHtml("#6B6161"),
                Color.FromHtml("#E8E0D4"),
                false,
                0.7f),
            "metal" => (
                Color.FromHtml("#5A6A7A"),
                Color.FromHtml("#8AB8C4"),
                false,
                0.9f),
            "fiber" => (
                Color.FromHtml("#2D5A27"),
                Color.FromHtml("#7BC558"),
                false,
                0.95f),
            "essence" => (
                Color.FromHtml("#56A900"),
                Color.FromHtml("#CFFF75"),
                true,
                1.25f),
            _ => (
                new Color(data.Color, 1f),
                new Color(data.OutlineColor, 1f),
                false,
                0.8f)
        };
    }

    /// <summary>Récolte le noeud entièrement en un seul coup. Retourne la quantité totale.</summary>
    public int Harvest()
    {
        if (IsExhausted)
            return 0;

        int totalAmount = 0;
        while (_harvestsRemaining > 0)
        {
            _harvestsRemaining--;
            int baseAmount = (int)GD.RandRange(_amountMin, _amountMax + 1);
            totalAmount += (int)(baseAmount * _harvestBonusMult);
        }

        HarvestFlash();
        Exhaust();

        return totalAmount;
    }

    private void HarvestFlash()
    {
        if (_usesSprite && _sprite != null)
        {
            _sprite.Modulate = Colors.White;
            Tween colorTween = CreateTween();
            colorTween.TweenProperty(_sprite, "modulate", Colors.White, 0.05f);
            colorTween.TweenProperty(_sprite, "modulate", new Color(1, 1, 1, 1), 0.2f);
        }
        else if (_visual != null)
        {
            _visual.Color = Colors.White;
            if (_outline != null)
                _outline.Color = Colors.White;

            Tween tween = CreateTween();
            tween.TweenProperty(_visual, "color", _originalColor, 0.2f)
                .SetDelay(0.05f);
        }

        Tween shake = CreateTween();
        Vector2 basePos = Position;
        shake.TweenProperty(this, "position", basePos + new Vector2(3, 0), 0.05f);
        shake.TweenProperty(this, "position", basePos + new Vector2(-3, 0), 0.05f);
        shake.TweenProperty(this, "position", basePos, 0.05f);

        _interactionAura?.SetActive(false);
    }

    private void Exhaust()
    {
        Tween tween = CreateTween();
        tween.SetParallel();
        tween.TweenProperty(this, "scale", Vector2.One * 0.3f, 0.5f);
        tween.TweenProperty(this, "modulate:a", 0f, 0.5f);
        tween.Chain().TweenCallback(Callable.From(() =>
        {
            RemoveFromGroup("resources");
            QueueFree();
        }));
    }
}
