using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Progression;
using Vestiges.UI;

namespace Vestiges.World;

/// <summary>
/// Interaction du joueur avec les lieux du monde (<see cref="IInteractable"/>) : invite au-dessus du plus proche à
/// portée, jauge pendant l'activation (immobile), puis activation. Un coffre donne son butin, résolu, montré par
/// l'écran de butin et appliqué à sa fermeture ; les autres lieux gèrent eux-mêmes leur activation.
/// </summary>
public partial class WorldInteraction : Node
{
    private Player _player;
    private EventBus _eventBus;
    private ChestLootScreen _lootScreen;
    private InteractionPrompt _prompt;
    private InteractionGauge _gauge;
    private IInteractable _target;
    private float _progress;

    public bool IsActive => _target != null;

    public void Setup(Player player)
    {
        _player = player;
        _eventBus = player.GetNode<EventBus>("/root/EventBus");
        _prompt = new InteractionPrompt { Name = "InteractionPrompt" };
        AddChild(_prompt);
        _gauge = new InteractionGauge { Name = "InteractionGauge", TopLevel = true };
        AddChild(_gauge);
    }

    public void Configure(ChestLootScreen lootScreen) => _lootScreen = lootScreen;

    /// <summary>Appelé à chaque pas physique du joueur : jauge et invite.</summary>
    public void Step(float delta, bool canInteract)
    {
        if (_target != null)
        {
            if (!IsValid(_target) || !_target.CanInteract
                || _player.GlobalPosition.DistanceTo(_target.InteractPosition) > _player.InteractRange * 1.5f)
            {
                Cancel();
            }
            else
            {
                _progress += delta;
                _gauge.SetRatio(_progress / _target.HoldTime);
                if (_progress >= _target.HoldTime)
                    Complete();
            }
        }

        IInteractable nearest = _target == null && canInteract ? FindNearest() : null;
        if (nearest != null)
            _prompt.ShowAt(nearest.PromptPosition, nearest.PromptVerbKey);
        else
            _prompt.HidePrompt();
    }

    public bool TryStart()
    {
        IInteractable nearest = FindNearest();
        if (nearest == null)
            return false;

        _target = nearest;
        _progress = 0f;
        if (nearest.HoldTime <= 0f)
        {
            Complete();
            return true;
        }
        _gauge.GlobalPosition = nearest.PromptPosition.Round() - new Vector2(0f, 6f);
        _gauge.Begin(nearest.GaugeColor);
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
        IInteractable target = _target;
        Cancel();
        if (target is Chest chest)
            OpenChest(chest);
        else
            target.Interact(_player);
    }

    private void OpenChest(Chest chest)
    {
        List<ResolvedLoot> loots = LootRewards.Resolve(chest.Open(), _player);
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

    private static bool IsValid(IInteractable interactable) =>
        interactable is not GodotObject node || GodotObject.IsInstanceValid(node);

    private IInteractable FindNearest()
    {
        IInteractable nearest = null;
        float nearestDistance = _player.InteractRange;
        IReadOnlyList<IInteractable> all = Interactables.All;
        for (int i = 0; i < all.Count; i++)
        {
            IInteractable candidate = all[i];
            if (!candidate.CanInteract)
                continue;
            float distance = _player.GlobalPosition.DistanceTo(candidate.InteractPosition);
            if (distance < nearestDistance)
            {
                nearest = candidate;
                nearestDistance = distance;
            }
        }
        return nearest;
    }
}
