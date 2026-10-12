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
    private ChoiceScreen _choices;
    private EssenceTracker _essence;
    private float _elapsed;
    private readonly RandomNumberGenerator _chestRng = RunRandom.Create("chest_items");
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

    public void Configure(ChestLootScreen lootScreen, EssenceTracker essence, ChoiceScreen choices)
    { _lootScreen = lootScreen; _essence = essence; _choices = choices; }

    /// <summary>Appelé à chaque pas physique du joueur : jauge et invite.</summary>
    public void Step(float delta, bool canInteract)
    {
        _elapsed += delta;
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
            _prompt.ShowAt(nearest.PromptPosition, nearest is Chest chest
                ? chest.PricePrompt(_player, _essence, _elapsed) : nearest.PromptVerbKey);
        else
            _prompt.HidePrompt();
    }

    public bool TryStart()
    {
        IInteractable nearest = FindNearest();
        if (nearest == null)
            return false;

        if (nearest is Chest chest && !chest.CanPurchase(_player, _essence, _elapsed, _chestRng, out string failure))
        {
            _player.ShowPopup(failure, new Color("D4A843"));
            return true;
        }

        _target = nearest;
        _progress = 0f;
        // Masquée avant une activation immédiate aussi : un lieu peut figer le monde aussitôt (réveil, écran).
        _prompt.HidePrompt();
        if (nearest.HoldTime <= 0f)
        {
            Complete();
            return true;
        }
        _gauge.GlobalPosition = nearest.PromptPosition.Round() - new Vector2(0f, 6f);
        _gauge.Begin(nearest.GaugeColor);
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
        if (!chest.TryPurchase(_player, _essence, _elapsed, _chestRng, out string failure))
        { _player.ShowPopup(failure, new Color("D4A843")); return; }
        if (chest.FreeOpening) _player.ShowPopup("Ouverture gratuite", new Color("6ACA5A"));
        ChooseNext(chest);
    }

    private void ChooseNext(Chest chest)
    {
        ChestRewardSession session = chest.Rewards;
        if (session.Complete)
        {
            List<ResolvedLoot> rewards = new(session.Rewards);
            if (_lootScreen != null)
                _lootScreen.ShowLoot(rewards, chest.Rarity, () => ApplyAll(chest), session.Eligible,
                    FormatProbabilities(session.Probabilities), chest.FreeOpening ? "Ouverture gratuite" : $"{chest.QuotedCost} Essence dépensée");
            else ApplyAll(chest);
            return;
        }
        IReadOnlyList<Infrastructure.PassiveSouvenirData> candidates = session.Propose();
        if (candidates.Count == 1 || _choices == null)
        {
            if (session.Choose(0)) ChooseNext(chest);
            return;
        }
        List<ChoiceCard> cards = new();
        foreach (Infrastructure.PassiveSouvenirData candidate in candidates)
        {
            ChoiceCard card = new() { Title = candidate.Name };
            ItemPresentation.ConfigureCard(card, candidate);
            ItemPresentation.AddComparison(card, candidate, _player.FindObject(candidate.Id)?.Copies ?? 0);
            cards.Add(card);
        }
        _choices.Open("Carnet d’adresses", "Choisissez un seul objet · " + FormatProbabilities(session.Probabilities), cards, null,
            index => { if (session.Choose(index)) ChooseNext(chest); });
    }

    private void ApplyAll(Chest chest)
    {
        ChestRewardSession session = chest.Rewards;
        if (!session.MarkApplied()) return;
        for (int i = 0; i < session.Rewards.Count; i++)
            LootRewards.Apply(session.Rewards[i], _player, _eventBus, chest.GlobalPosition + new Vector2(18f * i, 24f));
        WorldItemEffects.AttractChestXp(_player, chest.GlobalPosition);
    }

    internal static string FormatProbabilities(double[] probabilities)
    {
        List<string> values = new();
        for (int i = 0; i < probabilities.Length; i++)
            if (probabilities[i] > 0d) values.Add($"{Infrastructure.RarityPalette.DisplayName(Infrastructure.ItemLootConfig.Rarities[i])} {probabilities[i] * 100d:0.#} %");
        return string.Join(" · ", values);
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
