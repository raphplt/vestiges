using Godot;
using Vestiges.Core;

namespace Vestiges.UI;

/// <summary>
/// Compteur discret de la terre (plan 57 C3), posé sur la minimap : une icône par Mémorial à raviver, pâle puis dorée, et
/// celle du repaire, éteinte tant qu'il manque des lumières, qui s'allume et bat quand le gardien attend. Il remplace la
/// ligne d'objectif permanente, qui prenait le haut de l'écran. Caché hors des terres, au passage et sur la terre du late
/// game. Le saut d'une icône qui s'allume et le battement du repaire font seuls tourner <c>_Process</c>. Coordonnées en
/// unités de référence 960×540.
/// </summary>
public partial class LandMemorialPips : Control
{
    private const float IconScale = 2f;
    private const float Gap = 3f;
    private const float LairGap = 7f;
    private const float RadarGap = 4f;
    private const float PopSeconds = 0.6f;
    private const float PopScale = 0.8f;
    private const float PulseSeconds = 1f;
    private const float PulseRadius = 6f;
    private static readonly Color DormantColor = new(0.72f, 0.7f, 0.8f, 0.75f);
    private static readonly Color LitColor = new(0.98f, 0.82f, 0.45f);
    private static readonly Color LairAsleepColor = new(0.86f, 0.55f, 0.95f, 0.4f);
    private static readonly Color LairColor = new(0.92f, 0.68f, 1f);
    private static readonly Color Ink = new(0.04f, 0.04f, 0.08f, 0.85f);

    private EventBus _eventBus;
    private Texture2D _icon;
    private int _lights;
    private int _needed;
    private int _stage = -1;
    // Secondes restantes du saut de la dernière icône allumée.
    private float _pop;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _icon = GD.Load<Texture2D>("res://assets/ui/hud/plan25/minimap_memorial.png");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.LandObjectiveChanged += OnObjective;
        _eventBus.LandChanging += OnLandChanging;
        Visible = false;
        SetProcess(false);
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.LandObjectiveChanged -= OnObjective;
            _eventBus.LandChanging -= OnLandChanging;
        }
    }

    private void OnObjective(int land, int lights, int needed, int stage)
    {
        int lit = Mathf.Min(lights, needed);
        if (stage <= 1 && lit > Mathf.Min(_lights, _needed) && _stage >= 0)
            _pop = PopSeconds;
        _lights = lights;
        _needed = needed;
        _stage = stage;
        // Mémoriaux à raviver, gardien prêt, combat : le compteur reste ; passage et late game n'en ont pas.
        Visible = needed > 0 && stage <= 3;
        SetProcess(Visible && (_pop > 0f || stage == 1));
        QueueRedraw();
    }

    private void OnLandChanging(int toIndex)
    {
        _stage = -1;
        _pop = 0f;
        Visible = false;
        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        _pop = Mathf.Max(0f, _pop - (float)delta);
        if (_pop <= 0f && _stage != 1)
            SetProcess(false);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_icon == null || _needed <= 0)
            return;
        Vector2 iconSize = _icon.GetSize() * IconScale;
        float width = (_needed + 1) * iconSize.X + (_needed - 1) * Gap + LairGap;
        Rect2 radar = Minimap.RadarRect(Size);
        Vector2 origin = new(Mathf.Round(radar.GetCenter().X - width / 2f), radar.Position.Y - RadarGap - iconSize.Y);
        int lit = Mathf.Min(_lights, _needed);
        for (int i = 0; i < _needed; i++)
        {
            float pop = i == lit - 1 ? _pop / PopSeconds * PopScale : 0f;
            DrawIcon(origin + new Vector2(i * (iconSize.X + Gap), 0f), iconSize, i < lit ? LitColor : DormantColor, pop);
        }
        Vector2 lairAt = origin + new Vector2(_needed * (iconSize.X + Gap) - Gap + LairGap, 0f);
        bool ready = _stage >= 1;
        if (_stage == 1)
        {
            float pulse = (float)(Time.GetTicksMsec() / 1000.0 % PulseSeconds) / PulseSeconds;
            DrawArc(lairAt + iconSize / 2f, iconSize.X / 2f + PulseRadius * pulse, 0f, Mathf.Tau, 20,
                LairColor with { A = 1f - pulse }, 1f);
        }
        DrawIcon(lairAt, iconSize, ready ? LairColor : LairAsleepColor, 0f);
    }

    private void DrawIcon(Vector2 at, Vector2 size, Color color, float pop)
    {
        Vector2 grown = size * (1f + pop);
        Vector2 corner = (at - (grown - size) / 2f).Round();
        DrawRect(new Rect2(corner - Vector2.One, grown + Vector2.One * 2f), Ink with { A = Ink.A * color.A });
        DrawTextureRect(_icon, new Rect2(corner, grown), false, color);
    }
}
