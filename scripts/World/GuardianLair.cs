using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Repaire du gardien au cœur d'une terre (plan 51 P3) : une masse endormie sous une colonne de lumière, et une rangée
/// de lumières qui s'allument une à une avec les Mémoriaux ravivés. Une fois toutes allumées, on le touche pour
/// réveiller le gardien. Visuel provisoire, en attendant les gardiens dessinés.
/// </summary>
public partial class GuardianLair : Node2D, IInteractable
{
    private const string SpritePath = "res://assets/landmarks/memorial_dormant.png";
    private const float PipSpacing = 10f;
    private static readonly RarityColors LairColors = RarityPalette.Colors("legendary");
    private static readonly Color SleepTint = new(0.45f, 0.35f, 0.55f);
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

    public void Setup(GuardianDirector director, int needed)
    {
        _director = director;
        _needed = needed;
        _column = new LightColumn { Name = "LightColumn" };
        AddChild(_column);
        _column.Configure(LairColors, 220f, 3f, 16f);
        Texture2D texture = GD.Load<Texture2D>(SpritePath);
        _sprite = new Sprite2D
        {
            Name = "Sprite", Texture = texture, TextureFilter = TextureFilterEnum.Nearest, Centered = false,
            Offset = new Vector2(-texture.GetWidth() / 2f, -texture.GetHeight()), Scale = Vector2.One * 1.6f, Modulate = SleepTint,
        };
        AddChild(_sprite);
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
        _sprite.Modulate = CanInteract ? Colors.White : SleepTint;
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
