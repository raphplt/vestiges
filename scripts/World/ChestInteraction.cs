using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.UI;

namespace Vestiges.World;

/// <summary>
/// Ouverture des coffres par le joueur : invite au-dessus du coffre fermé le plus proche à portée, jauge
/// pendant l'ouverture (immobile), puis butin résolu, montré par l'écran de butin et appliqué à sa fermeture.
/// </summary>
public partial class ChestInteraction : Node
{
    private Player _player;
    private EventBus _eventBus;
    private ChestLootScreen _lootScreen;
    private PerkManager _perks;
    private InteractionPrompt _prompt;
    private InteractionGauge _gauge;
    private Chest _target;
    private float _progress;

    public bool IsOpening => _target != null;

    public void Setup(Player player)
    {
        _player = player;
        _eventBus = player.GetNode<EventBus>("/root/EventBus");
        _prompt = new InteractionPrompt { Name = "ChestPrompt" };
        AddChild(_prompt);
        _gauge = new InteractionGauge { Name = "ChestGauge", TopLevel = true };
        AddChild(_gauge);
    }

    public void Configure(ChestLootScreen lootScreen, PerkManager perks)
    {
        _lootScreen = lootScreen;
        _perks = perks;
    }

    /// <summary>Appelé à chaque pas physique du joueur : jauge et invite.</summary>
    public void Step(float delta, bool canInteract)
    {
        if (_target != null)
        {
            if (!IsInstanceValid(_target) || !_target.CanOpen
                || _player.GlobalPosition.DistanceTo(_target.GlobalPosition) > _player.InteractRange * 1.5f)
            {
                Cancel();
            }
            else
            {
                _progress += delta;
                _gauge.SetRatio(_progress / _target.OpenTime);
                if (_progress >= _target.OpenTime)
                    Complete();
            }
        }

        Chest nearest = _target == null && canInteract ? FindNearest() : null;
        if (nearest != null)
            _prompt.ShowAt(nearest.TopPosition);
        else
            _prompt.HidePrompt();
    }

    public bool TryStart()
    {
        Chest nearest = FindNearest();
        if (nearest == null)
            return false;

        _target = nearest;
        _progress = 0f;
        if (nearest.OpenTime <= 0f)
        {
            Complete();
            return true;
        }
        _gauge.GlobalPosition = nearest.TopPosition.Round() - new Vector2(0f, 6f);
        _gauge.Begin(RarityPalette.Main(nearest.Rarity));
        _prompt.HidePrompt();
        return true;
    }

    public void Cancel()
    {
        _target = null;
        _progress = 0f;
        _gauge.End();
    }

    private void Complete()
    {
        Chest chest = _target;
        Cancel();
        List<ResolvedLoot> loots = LootRewards.Resolve(chest.Open(), _perks);
        Vector2 position = chest.GlobalPosition;
        if (_lootScreen != null && loots.Count > 0)
            _lootScreen.ShowLoot(loots, chest.Rarity, () => ApplyAll(loots, position));
        else
            ApplyAll(loots, position);
    }

    private void ApplyAll(List<ResolvedLoot> loots, Vector2 position)
    {
        foreach (ResolvedLoot loot in loots)
            LootRewards.Apply(loot, _player, _eventBus, position);
    }

    private Chest FindNearest()
    {
        Chest nearest = null;
        float nearestDistance = _player.InteractRange;
        IReadOnlyList<Chest> chests = Chest.Closed;
        for (int i = 0; i < chests.Count; i++)
        {
            Chest chest = chests[i];
            if (!chest.CanOpen)
                continue;
            float distance = _player.GlobalPosition.DistanceTo(chest.GlobalPosition);
            if (distance < nearestDistance)
            {
                nearest = chest;
                nearestDistance = distance;
            }
        }
        return nearest;
    }
}
