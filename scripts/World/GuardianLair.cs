using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Repaire du gardien au cœur d'une terre (plan 51 P3) : le gardien couché sous une colonne de lumière (plan 57 G1), et
/// une rangée de lumières qui s'allument une à une avec les Mémoriaux ravivés. Une fois toutes allumées, on le touche
/// pour réveiller le gardien.
/// </summary>
public partial class GuardianLair : Node2D, IInteractable
{
    // Stèle de repli d'un gardien encore sans sprite.
    private const string FallbackSpritePath = "res://assets/landmarks/memorial_dormant.png";
    // Vu de profil, couché : le crâne et les bois se lisent mieux que de trois quarts.
    private const string SleepAnimation = "W_death";
    private const float PipSpacing = 10f;
    private static readonly RarityColors LairColors = RarityPalette.Colors("legendary");
    // Assombri et violacé tant qu'il dort profondément ; ses couleurs reviennent quand toutes ses lumières sont allumées.
    private static readonly Color SleepTint = new(0.6f, 0.52f, 0.72f);
    private static readonly Color ReadyTint = new(1.1f, 0.95f, 1.2f);
    private static readonly Color DarkPip = new(0.15f, 0.12f, 0.2f, 0.9f);

    private GuardianDirector _director;
    private Sprite2D _sprite;
    private LightColumn _column;
    private int _needed;
    private int _lit;
    private bool _awake;

    public bool CanInteract => !_awake && _lit >= _needed;
    public Vector2 InteractPosition => GlobalPosition;
    public Vector2 PromptPosition => GlobalPosition + new Vector2(0f, _sprite.Offset.Y * _sprite.Scale.Y - 14f);
    public string PromptVerbKey => "GUARDIAN_WAKE_PROMPT";
    public float HoldTime => 0.6f;
    public Color GaugeColor => LairColors.Main;

    /// <summary>Le corps montré est la dernière image de la chute de <paramref name="guardian"/>, couché.</summary>
    public void Setup(GuardianDirector director, int needed, EnemyData guardian)
    {
        _director = director;
        _needed = needed;
        _column = new LightColumn { Name = "LightColumn" };
        AddChild(_column);
        _column.Configure(LairColors, 220f, 3f, 16f);
        _sprite = SleepingBody(guardian) ?? FallbackStele();
        _sprite.Name = "Sprite";
        _sprite.TextureFilter = TextureFilterEnum.Nearest;
        _sprite.Centered = false;
        _sprite.Modulate = SleepTint;
        AddChild(_sprite);
    }

    private static Sprite2D SleepingBody(EnemyData guardian)
    {
        SpriteFrames frames = guardian?.Visual.SpriteFolder != null ? EnemySpriteLoader.LoadOrGet(guardian.Id, guardian.Visual.SpriteFolder) : null;
        if (frames == null || !frames.HasAnimation(SleepAnimation))
            return null;
        Texture2D texture = frames.GetFrameTexture(SleepAnimation, frames.GetFrameCount(SleepAnimation) - 1);
        // Mêmes pieds que la créature debout : le corps se lève là où il dormait.
        float feet = texture.GetHeight() / 2f + guardian.Visual.SpriteFeetOffset;
        return new Sprite2D { Texture = texture, Offset = new Vector2(-texture.GetWidth() / 2f, -feet) };
    }

    private static Sprite2D FallbackStele()
    {
        Texture2D texture = GD.Load<Texture2D>(FallbackSpritePath);
        return new Sprite2D
        {
            Texture = texture, Offset = new Vector2(-texture.GetWidth() / 2f, -texture.GetHeight()), Scale = Vector2.One * 1.6f,
        };
    }

    /// <summary>Repaire de la terre en cours, montré par la minimap dès l'arrivée ; nul hors des terres.</summary>
    public static GuardianLair Current { get; private set; }

    public override void _EnterTree()
    {
        Current = this;
        Interactables.Register(this);
    }

    public override void _ExitTree()
    {
        if (Current == this)
            Current = null;
        Interactables.Unregister(this);
    }

    public void Interact(Player player) => _director.WakeByPlayer();

    /// <summary>Lumières allumées par les Mémoriaux ravivés de la terre.</summary>
    public void SetLit(int lit)
    {
        if (lit == _lit)
            return;
        _lit = lit;
        _sprite.Modulate = CanInteract ? ReadyTint : SleepTint;
        QueueRedraw();
    }

    /// <summary>Le gardien s'est levé : le repaire s'éteint et ne s'active plus.</summary>
    public void Awaken()
    {
        _awake = true;
        Interactables.Unregister(this);
        Visible = false;
    }

    public override void _Draw()
    {
        float top = _sprite.Offset.Y * _sprite.Scale.Y - 8f;
        float left = -(_needed - 1) * PipSpacing / 2f;
        for (int i = 0; i < _needed; i++)
        {
            Color color = i < _lit ? LairColors.Light : DarkPip;
            DrawRect(new Rect2(left + i * PipSpacing - 2f, top - 2f, 4f, 4f), color);
        }
    }
}
