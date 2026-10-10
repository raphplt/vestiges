using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Repères (plan 22 §3 B, plan 23 R9, plan 24 D3) : le premier usage de chaque type de lieu dans la run donne un petit
/// gain permanent propre au lieu (une stat, ou une relance, un bannissement gratuit). Treize types : les petits lieux,
/// coffre, Mémorial éveillé, Faille dont on a pris une offre, Atelier visité. Enfant du joueur, état de run.
/// Les signaux ne disent pas quel joueur agit : à revoir pour le coop v2.
/// </summary>
public partial class Waymarks : Node
{
    private static readonly Color WaymarkColor = new(0.95f, 0.84f, 0.5f);

    private readonly HashSet<string> _found = new();
    private WaymarkConfig _config;
    private EventBus _eventBus;
    private Player _player;

    public int Found => _found.Count;
    public int Total => _config is { Enabled: true } ? _config.Rewards.Count : 0;

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
        _eventBus.RiftUsed += OnRiftUsed;
        _eventBus.WorkshopVisited += OnWorkshopVisited;
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;
        _eventBus.SmallPlaceUsed -= OnSmallPlaceUsed;
        _eventBus.ChestOpened -= OnChestOpened;
        _eventBus.MemorialAwakened -= OnMemorialAwakened;
        _eventBus.RiftUsed -= OnRiftUsed;
        _eventBus.WorkshopVisited -= OnWorkshopVisited;
    }

    private void OnSmallPlaceUsed(string placeId, Vector2 position) => Mark(placeId);
    private void OnChestOpened(string chestId, string rarity, Vector2 position) => Mark("chest");
    private void OnMemorialAwakened(Vector2 position) => Mark("memorial");
    private void OnRiftUsed(Vector2 position) => Mark("rift");
    private void OnWorkshopVisited(Vector2 position) => Mark("workshop");

    private void Mark(string type)
    {
        if (_player == null || _player.IsDead || !_config.Rewards.TryGetValue(type, out WaymarkReward reward) || !_found.Add(type))
            return;
        _eventBus.EmitSignal(EventBus.SignalName.WaymarkFound, type);
        string gain;
        if (reward.Stat != null)
        {
            bool multiplicative = reward.ModifierType == "multiplicative";
            float value = multiplicative ? 1f + reward.Amount : reward.Amount;
            _player.ApplyPerkModifier(reward.Stat, value, reward.ModifierType);
            gain = $"{StatCatalog.Name(reward.Stat)} {StatCatalog.FormatBonus(reward.Stat, value, multiplicative)}";
        }
        else
        {
            _eventBus.EmitSignal(EventBus.SignalName.ChoiceTokensGranted, reward.Rerolls, reward.Banishes);
            gain = reward.Rerolls > 0
                ? string.Format(TranslationServer.Translate("WAYMARK_REROLLS"), reward.Rerolls)
                : string.Format(TranslationServer.Translate("WAYMARK_BANISHES"), reward.Banishes);
        }
        _player.ShowPopup(string.Format(TranslationServer.Translate("WAYMARK_FOUND"), TranslationServer.Translate(reward.NameKey), gain), WaymarkColor);
    }
}
