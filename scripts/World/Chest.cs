using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Coffre récupérable dans le monde. 4 raretés : common, rare, epic, lore.
/// Le joueur interagit pour ouvrir avec une barre de progression.
/// Utilise des sprites pixel art quand disponibles, sinon rendu procédural.
/// </summary>
public partial class Chest : StaticBody2D
{
    private ChestData _chestData;
    private bool _isOpened;
    private Polygon2D _visual;
    private Polygon2D _outline;
    private Sprite2D _sprite;
    private InteractableAura _interactionAura;
    private Texture2D _closedTexture;
    private Texture2D _openTexture;
    private bool _usesSprite;
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
        _visual = GetNodeOrNull<Polygon2D>("Visual");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        AddToGroup("chests");
    }

    public void Initialize(ChestData data)
    {
        _chestData = data;
        _originalColor = data.Color;

        if (TryLoadSprites(data))
        {
            _usesSprite = true;
            if (_visual != null)
                _visual.Visible = false;
        }
        else
        {
            _usesSprite = false;
            float s = data.Size;
            Vector2[] shape = BuildChestShape(s);
            _visual.Polygon = shape;
            _visual.Color = data.Color;
            CreateOutline(shape, data.OutlineColor);
        }

        CreateInteractionAura(data);
    }

    private bool TryLoadSprites(ChestData data)
    {
        if (string.IsNullOrEmpty(data.SpriteClosed))
            return false;

        string closedPath = data.SpriteClosed.StartsWith("res://") ? data.SpriteClosed : $"res://{data.SpriteClosed}";
        if (!ResourceLoader.Exists(closedPath))
        {
            GD.PushWarning($"[Chest] Sprite not found: {closedPath}, using polygon fallback");
            return false;
        }

        _closedTexture = GD.Load<Texture2D>(closedPath);
        if (_closedTexture == null)
            return false;

        // Charger la texture ouverte (optionnelle)
        if (!string.IsNullOrEmpty(data.SpriteOpen))
        {
            string openPath = data.SpriteOpen.StartsWith("res://") ? data.SpriteOpen : $"res://{data.SpriteOpen}";
            if (ResourceLoader.Exists(openPath))
                _openTexture = GD.Load<Texture2D>(openPath);
        }

        _sprite = new Sprite2D
        {
            Texture = _closedTexture,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Offset = new Vector2(0, -_closedTexture.GetHeight() * 0.5f + 2)
        };
        AddChild(_sprite);

        return true;
    }

    /// <summary>Ouvre le coffre. Émet ChestOpened. Retourne la liste des loots.</summary>
    public System.Collections.Generic.List<LootResolver.LootResult> Open()
    {
        if (_isOpened)
            return new();

        _isOpened = true;

        if (_usesSprite && _sprite != null)
        {
            // Swap vers le sprite ouvert
            if (_openTexture != null)
            {
                _sprite.Texture = _openTexture;
                _sprite.Offset = new Vector2(0, -_openTexture.GetHeight() * 0.5f + 2);
            }
            else
            {
                _sprite.Modulate = new Color(1f, 1f, 1f, 0.4f);
            }
        }
        else
        {
            _visual.Color = new Color(_originalColor, 0.3f);
            if (_outline != null)
                _outline.Color = new Color(_outline.Color, 0.2f);
        }

        _interactionAura?.SetActive(false);

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

    private void CreateInteractionAura(ChestData data)
    {
        _interactionAura?.QueueFree();
        _interactionAura = new InteractableAura();
        AddChild(_interactionAura);

        Color baseColor = data.Rarity switch
        {
            "rare" => Color.FromHtml("#5A7A9A"),
            "epic" => Color.FromHtml("#D4A843"),
            "lore" => Color.FromHtml("#C4D0D4"),
            _ => Color.FromHtml("#7A5C42")
        };

        Color accentColor = data.Rarity switch
        {
            "rare" => Color.FromHtml("#8B6BAE"),
            "epic" => Color.FromHtml("#F0C030"),
            "lore" => Color.FromHtml("#F0E0C0"),
            _ => Color.FromHtml("#C49B3E")
        };

        _interactionAura.Configure(
            baseColor,
            accentColor,
            radius: Mathf.Max(12f, data.Size * 1.15f),
            height: Mathf.Max(12f, data.Size * 1.05f),
            withMote: data.Rarity != "common",
            pulseSpeed: data.Rarity switch
            {
                "epic" => 1.2f,
                "rare" => 0.95f,
                "lore" => 0.8f,
                _ => 0.75f
            },
            baseAlpha: data.Rarity switch
            {
                "epic" => 0.18f,
                "rare" => 0.14f,
                "lore" => 0.15f,
                _ => 0.1f
            },
            pulseAlpha: data.Rarity switch
            {
                "epic" => 0.08f,
                "rare" => 0.06f,
                "lore" => 0.06f,
                _ => 0.04f
            },
            crownAlpha: data.Rarity switch
            {
                "epic" => 0.12f,
                "rare" => 0.1f,
                "lore" => 0.12f,
                _ => 0.06f
            },
            crownPulseAlpha: data.Rarity switch
            {
                "epic" => 0.07f,
                "rare" => 0.05f,
                "lore" => 0.06f,
                _ => 0.03f
            });
    }

    private void PlayOpenAnimation()
    {
        if (_usesSprite && _sprite != null)
        {
            _sprite.Modulate = new Color(10f, 10f, 10f, 1f);
            Tween flashTween = CreateTween();
            flashTween.TweenProperty(_sprite, "modulate", Colors.White, 0.15f)
                .SetDelay(0.05f);
        }
        else
        {
            Color prevColor = _visual.Color;
            _visual.Color = Colors.White;
            Tween tween = CreateTween();
            tween.TweenProperty(_visual, "color", prevColor, 0.15f).SetDelay(0.05f);
        }

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
