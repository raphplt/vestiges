using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Progression;

/// <summary>
/// Pression de la Scaphandrière : gain offensif événementiel, multiplicatif après les malus de phase.
/// Les deux chevrons près du casque montrent l'intensité, sans polling ni allocation par image.
/// </summary>
public partial class ErasurePressure : Node2D
{
    private Player _player;
    private PerkPressure _config;
    private GameManager _manager;
    private EventBus _events;
    public float DamageMultiplier { get; private set; } = 1f;
    public int Intensity { get; private set; }

    public void Initialize(Player player, PerkPressure config)
    {
        _player = player;
        _config = config;
    }

    public override void _Ready()
    {
        SetProcess(false);
        _manager = GetNode<GameManager>("/root/GameManager");
        _events = GetNode<EventBus>("/root/EventBus");
        _player.SynchronizeErasurePhase();
        _player.ErasurePhaseChanged += OnPhaseChanged;
        _events.GameStateChanged += OnGameStateChanged;
        _events.PlayerDamageResolved += OnDamage;
        Position = new Vector2(0f, -42f);
        ZIndex = 20;
        Refresh();
    }

    public override void _ExitTree()
    {
        if (_player != null && IsInstanceValid(_player))
        {
            _player.ErasurePhaseChanged -= OnPhaseChanged;
            _player.PressureDamageMultiplier = 1f;
        }
        if (_events != null && IsInstanceValid(_events))
        {
            _events.GameStateChanged -= OnGameStateChanged;
            _events.PlayerDamageResolved -= OnDamage;
        }
        DamageMultiplier = 1f;
        Intensity = 0;
        Visible = false;
    }

    private void OnPhaseChanged(ErasureManager.ErasureZonePhase phase) => Refresh();
    private void OnGameStateChanged(string oldState, string newState) => Refresh();
    private void OnDamage(PlayerDamageResult result)
    {
        if (result.PlayerId == _player.GetInstanceId() && _player.IsDead)
            Refresh();
    }

    private void Refresh()
    {
        bool active = !_player.IsDead && _manager.CurrentState == GameManager.GameState.Run;
        int intensity = !active ? 0 : _player.ErasurePhase switch
        {
            ErasureManager.ErasureZonePhase.Frayed => 1,
            ErasureManager.ErasureZonePhase.Erased => 2,
            _ => 0,
        };
        DamageMultiplier = intensity == 1 ? 1f + _config.FrayedBonus : intensity == 2 ? 1f + _config.ErasedBonus : 1f;
        _player.PressureDamageMultiplier = DamageMultiplier;
        Visible = intensity > 0;
        if (intensity == Intensity)
            return;
        Intensity = intensity;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Color color = Intensity == 2 ? new Color(0.9f, 0.75f, 0.4f) : new Color(0.57f, 0.83f, 0.86f);
        for (int i = 0; i < Intensity; i++)
        {
            float y = -i * 4f;
            Color outline = new(0.06f, 0.08f, 0.1f);
            DrawLine(new Vector2(-4f, y + 2f), new Vector2(0f, y), outline, 3f);
            DrawLine(new Vector2(0f, y), new Vector2(4f, y + 2f), outline, 3f);
            DrawLine(new Vector2(-4f, y + 2f), new Vector2(0f, y), color, 1f);
            DrawLine(new Vector2(0f, y), new Vector2(4f, y + 2f), color, 1f);
        }
    }
}
