using Godot;
using Vestiges.Infrastructure;
using Vestiges.UI;

namespace Vestiges.World;

/// <summary>
/// Compte à rebours du ramassage des éclats d'un Mémorial (DECISIONS §154) : il suit le joueur, puisque les éclats
/// l'éloignent du Mémorial et de son propre compteur, et bat un tic-tac à chaque seconde. Dans les dernières secondes,
/// le tic-tac presse (deux par seconde) et le texte rougit.
/// </summary>
public partial class GatherCountdown : Node2D
{
    private const float HeadOffsetPx = -46f;
    private const float UrgentTickSeconds = 0.5f;
    private static readonly Color UrgentColor = new(0.95f, 0.38f, 0.3f);

    private Label _label;
    private Node2D _player;
    private Color _calmColor;
    private float _urgentTime;
    private float _nextTick;

    public void Setup(Node2D player, Color calmColor, float urgentTime)
    {
        _player = player;
        _calmColor = calmColor;
        _urgentTime = urgentTime;
    }

    public override void _Ready()
    {
        // Texte à lire au-dessus des entités (couches dans AGENTS.md).
        ZIndex = 20;
        ZAsRelative = false;
        _label = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(-70f, HeadOffsetPx),
            Size = new Vector2(140f, 12f),
            TextureFilter = TextureFilterEnum.Nearest,
        };
        UITheme.SetCompactText(_label, 8);
        AddChild(_label);
        Visible = false;
        SetProcess(false);
    }

    /// <summary>Début du ramassage : le premier tic tombe à la prochaine seconde entière.</summary>
    public void Begin(float remaining)
    {
        _nextTick = Mathf.Ceil(remaining) - 1f;
        Visible = true;
        SetProcess(true);
    }

    public void Show(int collected, int total, float remaining)
    {
        bool urgent = remaining <= _urgentTime;
        _label.Text = string.Format(Tr("MEMORIAL_GATHER_STATUS"), collected, total, Mathf.CeilToInt(remaining));
        _label.AddThemeColorOverride("font_color", urgent ? UrgentColor : _calmColor);
        if (remaining > _nextTick)
            return;
        AudioManager.PlayUI(urgent ? "sfx_compte_tic_presse" : "sfx_compte_tic");
        _nextTick -= remaining - UrgentTickSeconds <= _urgentTime ? UrgentTickSeconds : 1f;
    }

    public void End()
    {
        Visible = false;
        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        if (IsInstanceValid(_player))
            GlobalPosition = _player.GlobalPosition;
    }
}
