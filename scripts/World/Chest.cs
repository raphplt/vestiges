using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Coffre du monde : sprite du pipeline procédural posé à son pivot, ombre de contact, colonne de lumière
/// à la couleur de sa rareté. Les coffres fermés sont tenus dans un registre, pour que l'invite, les repères
/// de bord d'écran et l'interaction les trouvent sans recherche par groupe.
/// </summary>
public partial class Chest : StaticBody2D
{
    private static readonly List<Chest> _closed = new();

    private ChestData _chestData;
    private bool _isOpened;
    private Sprite2D _sprite;
    private LightColumn _column;
    private Texture2D _openTexture;
    private EventBus _eventBus;

    /// <summary>Coffres fermés présents dans la scène.</summary>
    public static IReadOnlyList<Chest> Closed => _closed;

    public bool IsOpened => _isOpened;
    public float OpenTime => _chestData?.OpenTime ?? 0.5f;
    public string ChestId => _chestData?.Id ?? "";
    public string Rarity => _chestData?.Rarity ?? "common";
    public string LootTableId => _chestData?.LootTableId ?? "";
    public int LootRolls => _chestData?.LootRolls ?? 1;
    public int ScorePoints => _chestData?.ScorePoints ?? 25;
    public float ColumnHeight => _chestData?.ColumnHeight ?? 0f;

    public bool CanOpen => !_isOpened && _chestData != null;

    /// <summary>Point au-dessus du sprite, pour l'invite et la jauge d'ouverture.</summary>
    public Vector2 TopPosition => GlobalPosition + new Vector2(0f, _sprite != null ? _sprite.Offset.Y - 3f : -24f);

    public override void _EnterTree()
    {
        if (!_isOpened)
            _closed.Add(this);
    }

    public override void _ExitTree()
    {
        _closed.Remove(this);
    }

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        AddToGroup("chests");
    }

    public void Initialize(ChestData data)
    {
        _chestData = data;

        _column = new LightColumn { Name = "LightColumn" };
        AddChild(_column);
        _column.Configure(RarityPalette.Colors(data.Rarity), data.ColumnHeight, data.ColumnCore);

        Texture2D closed = LoadTexture(data.SpriteClosed);
        _openTexture = LoadTexture(data.SpriteOpen);
        _sprite = new Sprite2D { Name = "Sprite", TextureFilter = CanvasItem.TextureFilterEnum.Nearest, Centered = false };
        AddChild(_sprite);
        ShowTexture(closed);
        if (closed != null && PropManifest.TryGet(closed, out PropManifest.Entry entry) && entry.Footprint.Length >= 3)
        {
            Sprite2D shadow = PropShadow.Create(entry.Footprint);
            AddChild(shadow);
            MoveChild(shadow, 0);
        }
    }

    /// <summary>Ouvre le coffre : sprite ouvert, colonne éteinte, gerbe d'éclats. Retourne le butin tiré.</summary>
    public List<LootResolver.LootResult> Open()
    {
        if (_isOpened)
            return new();

        _isOpened = true;
        _closed.Remove(this);
        if (_openTexture != null)
            ShowTexture(_openTexture);
        _column.Visible = false;

        PlayOpenAnimation();
        CombatPools.Instance?.EmitSparks(GlobalPosition + new Vector2(0f, -10f), new SparkBurst
        {
            Family = PixelPalette.ParseFamily(_chestData.FxFamily, FxFamily.Silk),
            Owner = FxOwner.Player,
            Count = 14,
            Direction = Vector2.Up,
            Spread = 2.6f,
            SpeedMin = 60f,
            SpeedMax = 150f,
            LifeMin = 0.35f,
            LifeMax = 0.7f,
            Ballistic = true,
            Size = 2,
        });

        _eventBus?.EmitSignal(EventBus.SignalName.ChestOpened, _chestData.Id, _chestData.Rarity, GlobalPosition);
        return LootResolver.Roll(LootTableId, LootRolls);
    }

    /// <summary>Pose le sprite à son point au sol (manifeste), sinon au bas de l'image.</summary>
    private void ShowTexture(Texture2D texture)
    {
        if (texture == null)
            return;
        _sprite.Texture = texture;
        Vector2 pivot = PropManifest.TryGet(texture, out PropManifest.Entry entry)
            ? entry.Pivot
            : new Vector2(texture.GetWidth() * 0.5f, texture.GetHeight());
        _sprite.Offset = -pivot.Round();
    }

    private static Texture2D LoadTexture(string path)
    {
        string resPath = path.StartsWith("res://") ? path : $"res://{path}";
        Texture2D texture = ResourceLoader.Exists(resPath) ? GD.Load<Texture2D>(resPath) : null;
        if (texture == null)
            GD.PushError($"[Chest] Sprite introuvable : {resPath}");
        return texture;
    }

    private void PlayOpenAnimation()
    {
        _sprite.Modulate = new Color(4f, 4f, 4f, 1f);
        Tween flash = CreateTween();
        flash.TweenProperty(_sprite, "modulate", Colors.White, 0.15f).SetDelay(0.05f);

        Tween bounce = CreateTween();
        bounce.TweenProperty(_sprite, "scale", new Vector2(1.2f, 0.85f), 0.07f);
        bounce.TweenProperty(_sprite, "scale", new Vector2(0.9f, 1.15f), 0.09f);
        bounce.TweenProperty(_sprite, "scale", Vector2.One, 0.08f);
    }
}
