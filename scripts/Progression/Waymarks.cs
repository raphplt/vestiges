using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Repères (plan 22 §3 B, plan 23 R9) : le premier usage de chaque type de lieu dans la run donne un peu de Chance.
/// Douze types : les petits lieux, coffre, Mémorial éveillé, Faille. Enfant du joueur, état de run.
/// </summary>
public partial class Waymarks : Node
{
    private static readonly Color WaymarkColor = new(0.95f, 0.84f, 0.5f);

    private readonly HashSet<string> _found = new();
    private WaymarkConfig _config;
    private EventBus _eventBus;
    private Player _player;

    public int Found => _found.Count;
    public int Total => _config?.NameKeys.Count ?? 0;

    public override void _Ready()
    {
        _config = WaymarkDataLoader.Load();
        _player = GetParent<Player>();
        if (!_config.Enabled)
            return;
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.SmallPlaceUsed += OnSmallPlaceUsed;
        _eventBus.ChestOpened += OnChestOpened;
        _eventBus.MemorialAwakened += OnMemorialAwakened;
        _eventBus.RiftInteracted += OnRiftInteracted;
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;
        _eventBus.SmallPlaceUsed -= OnSmallPlaceUsed;
        _eventBus.ChestOpened -= OnChestOpened;
        _eventBus.MemorialAwakened -= OnMemorialAwakened;
        _eventBus.RiftInteracted -= OnRiftInteracted;
    }

    private void OnSmallPlaceUsed(string placeId, Vector2 position) => Mark(placeId);
    private void OnChestOpened(string chestId, string rarity, Vector2 position) => Mark("chest");
    private void OnMemorialAwakened(Vector2 position) => Mark("memorial");
    private void OnRiftInteracted(Node2D rift) => Mark("rift");

    private void Mark(string type)
    {
        if (_player == null || _player.IsDead || !_config.NameKeys.TryGetValue(type, out string nameKey) || !_found.Add(type))
            return;
        _player.ApplyPerkModifier("luck", _config.LuckPerType, "additive");
        string text = string.Format(TranslationServer.Translate("WAYMARK_FOUND"), TranslationServer.Translate(nameKey),
            StatCatalog.FormatBonus("luck", _config.LuckPerType, false));
        _player.ShowPopup(text, WaymarkColor);
    }
}
