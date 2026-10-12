using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.World;

/// <summary>
/// Coffre du monde : sprite du pipeline procédural posé à son pivot, ombre de contact, colonne de lumière
/// à la couleur de sa rareté. Les coffres fermés sont tenus dans un registre, pour que les repères de bord d'écran
/// les trouvent sans recherche par groupe ; l'interaction passe par <see cref="Interactables"/>.
/// </summary>
public partial class Chest : StaticBody2D, IInteractable
{
    private static readonly List<Chest> _closed = new();

    private ChestData _chestData;
    private bool _isOpened;
    private int? _quotedCost;
    private bool _freeAttempted;
    private bool _freeOpening;
    private bool _purchasing;
    private string _pricePrompt;
    private bool _promptAffordable, _promptFree;
    public int QuotedCost => _quotedCost ?? 0;
    public bool FreeOpening => _freeOpening;
    public ChestRewardSession Rewards { get; private set; }
    private Sprite2D _sprite;
    private LightColumn _column;
    private Texture2D _openTexture;
    private EventBus _eventBus;
    // Oublis (plan 17 lot 3D) : colonne raccourcie (repères), rangs de rareté perdus (trésor).
    private float _signalFactor = 1f;
    private int _downgrades;

    private GroupCache _groups;

    // Frémissement à l'approche (plan 02 J6) : le coffre fermé tremble de plus en plus quand le joueur s'en approche.
    private const float QuiverRange = 110f;
    private const float QuiverMaxRadians = 0.06f;
    private const float QuiverCheckInterval = 0.1f;
    private float _quiverCheck;
    private float _quiverAmount;
    private float _quiverTime;

    /// <summary>Coffres fermés présents dans la scène.</summary>
    public static IReadOnlyList<Chest> Closed => _closed;

    public bool IsOpened => _isOpened;
    public float OpenTime => _chestData?.OpenTime ?? 0.5f;
    public string ChestId => _chestData?.Id ?? "";
    public string Rarity => _chestData?.Rarity ?? "common";
    public string LootTableId => _chestData?.LootTableId ?? "";
    public int LootRolls => _chestData?.LootRolls ?? 1;
    public float ColumnHeight => _chestData?.ColumnHeight ?? 0f;

    public bool CanOpen => !_isOpened && _chestData != null;

    /// <summary>Point au-dessus du sprite, pour l'invite et la jauge d'ouverture.</summary>
    public Vector2 TopPosition => GlobalPosition + new Vector2(0f, _sprite != null ? _sprite.Offset.Y - 3f : -24f);

    public bool CanInteract => CanOpen;
    public Vector2 InteractPosition => GlobalPosition;
    public Vector2 PromptPosition => TopPosition;
    public string PromptVerbKey => "CHEST_OPEN_PROMPT";
    public float HoldTime => OpenTime;
    public Color GaugeColor => RarityPalette.Main(Rarity);

    /// <summary>Le butin passe par l'écran de butin : <see cref="WorldInteraction"/> ouvre le coffre lui-même.</summary>
    public void Interact(Player player) { }

    /// <summary>Devis annoncé une fois ; s’approcher ne tire rien et ne débite rien.</summary>
    public int Quote(float elapsedSeconds) => _quotedCost ??= ItemLootConfig.ChestCost(Rarity, elapsedSeconds);

    public string PricePrompt(Player player, EssenceTracker essence, float elapsedSeconds)
    {
        int cost = Quote(elapsedSeconds);
        bool affordable = (essence?.CurrentEssence ?? 0) >= cost;
        if (_pricePrompt == null || affordable != _promptAffordable || _promptFree != _freeOpening)
        {
            _promptAffordable = affordable;
            _promptFree = _freeOpening;
            _pricePrompt = _freeOpening ? "Ouvrir — gratuit" : $"Ouvrir — {cost} Essence" + (!affordable ? " · fonds insuffisants" : "");
        }
        return _pricePrompt;
    }

    /// <summary>Première intention explicite : jet unique mémorisé, même si les fonds manquent. Aucun paiement avant la fin de l’interaction.</summary>
    public bool CanPurchase(Player player, EssenceTracker essence, float elapsedSeconds, RandomNumberGenerator rng, out string failure)
        => CanPurchase(player, essence, elapsedSeconds, rng, ItemLootPool.For(player), out failure);

    private bool CanPurchase(Player player, EssenceTracker essence, float elapsedSeconds, RandomNumberGenerator rng, ItemLootPool pool, out string failure)
    {
        failure = "";
        if (!CanOpen) { failure = "Coffre déjà ouvert"; return false; }
        if (!pool.CanSupply(LootRolls)) { failure = "Aucun objet disponible"; return false; }
        Quote(elapsedSeconds);
        if (!_freeAttempted)
        {
            _freeAttempted = true;
            float chance = WorldItemEffects.FreeChestChance(player);
            _freeOpening = chance > 0f && rng.Randf() < chance;
        }
        if (!_freeOpening && (essence?.CurrentEssence ?? 0) < QuotedCost)
        { failure = $"Il manque {QuotedCost - (essence?.CurrentEssence ?? 0)} Essence"; return false; }
        return true;
    }

    /// <summary>Une seule transaction. Les choix éventuels appartiennent désormais au coffre payé, jamais à un écran éphémère.</summary>
    public bool TryPurchase(Player player, EssenceTracker essence, float elapsedSeconds, RandomNumberGenerator rng, out string failure)
        => TryPurchase(player, essence, elapsedSeconds, rng, ItemLootPool.For(player), out failure);

    internal bool TryPurchase(Player player, EssenceTracker essence, float elapsedSeconds, RandomNumberGenerator rng, ItemLootPool pool, out string failure)
    {
        if (_purchasing) { failure = "Ouverture déjà en cours"; return false; }
        if (!CanPurchase(player, essence, elapsedSeconds, rng, pool, out failure)) return false;
        _purchasing = true;
        int paid = _freeOpening ? 0 : QuotedCost;
        bool debited = false;
        try
        {
            ChestRewardSession session = new(pool, "chest_" + Rarity, player.LuckBonus, LootRolls, WorldItemEffects.ExtraChoices(player), rng);
            if (session.Propose().Count == 0) { failure = "Aucun objet disponible"; return false; }
            if (paid > 0 && (essence == null || !essence.TrySpend(paid))) { failure = "Essence insuffisante"; return false; }
            debited = paid > 0;
            Rewards = session;
            CommitOpen();
            return true;
        }
        catch
        {
            if (debited && !_isOpened) essence.AddEssence(paid);
            throw;
        }
        finally { _purchasing = false; }
    }

    public override void _EnterTree()
    {
        if (_isOpened)
            return;
        _closed.Add(this);
        Interactables.Register(this);
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.OubliEffectChanged -= OnOubliEffectChanged;
        _closed.Remove(this);
        Interactables.Unregister(this);
    }

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _groups = GetNode<GroupCache>("/root/GroupCache");
        _eventBus.OubliEffectChanged += OnOubliEffectChanged;
        AddToGroup("chests");
    }

    private void OnOubliEffectChanged(string effect, float total)
    {
        if (_chestData == null || _isOpened)
            return;
        if (effect == "chest_signals")
        {
            _signalFactor = Mathf.Max(0f, 1f - total);
            ConfigureColumn();
        }
        else if (effect == "chest_rarity_down")
        {
            for (int target = Mathf.RoundToInt(total); _downgrades < target; _downgrades++)
                Downgrade();
        }
    }

    /// <summary>Le coffre perd un rang de rareté : données, sprites et colonne du rang en dessous.</summary>
    private void Downgrade()
    {
        ChestData lower = string.IsNullOrEmpty(_chestData.DowngradeTo) ? null : ChestDataLoader.Get(_chestData.DowngradeTo);
        if (lower == null)
            return;
        _chestData = lower;
        _openTexture = LoadTexture(lower.SpriteOpen);
        ShowTexture(LoadTexture(lower.SpriteClosed));
        ConfigureColumn();
    }

    private void ConfigureColumn()
    {
        _column.Visible = _signalFactor > 0f;
        if (_column.Visible)
            _column.Configure(RarityPalette.Colors(_chestData.Rarity), _chestData.ColumnHeight * _signalFactor, _chestData.ColumnCore);
    }

    public override void _Process(double delta)
    {
        if (_isOpened || _sprite == null)
            return;
        float dt = (float)delta;
        _quiverCheck -= dt;
        if (_quiverCheck <= 0f)
        {
            _quiverCheck = QuiverCheckInterval;
            _quiverAmount = 0f;
            if (_groups.GetPlayer() is Node2D player)
            {
                float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
                _quiverAmount = Mathf.Clamp(1f - distance / QuiverRange, 0f, 1f);
            }
        }

        if (_quiverAmount <= 0f)
        {
            if (_sprite.Rotation != 0f)
                _sprite.Rotation = 0f;
            return;
        }
        // Secousses brèves par saccades, pas une oscillation régulière : quelque chose bouge dedans.
        // Le sprite est décalé de son pivot au sol : il pivote sur sa base.
        _quiverTime += dt;
        float burst = Mathf.Max(0f, Mathf.Sin(_quiverTime * 3.1f));
        _sprite.Rotation = Mathf.Sin(_quiverTime * 41f) * QuiverMaxRadians * _quiverAmount * burst;
    }

    public void Initialize(ChestData data)
    {
        _chestData = data;
        // Un coffre posé après un Oubli des repères (butin d'événement, d'élite) naît avec sa colonne déjà raccourcie.
        float signalsForgotten = Progression.PerilManager.Current?.EffectTotal("chest_signals") ?? 0f;
        _signalFactor = Mathf.Max(0f, 1f - signalsForgotten);

        _column = new LightColumn { Name = "LightColumn" };
        AddChild(_column);
        ConfigureColumn();

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
    private void CommitOpen()
    {
        if (_isOpened)
            return;

        _isOpened = true;
        _sprite.Rotation = 0f;
        _closed.Remove(this);
        Interactables.Unregister(this);
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
