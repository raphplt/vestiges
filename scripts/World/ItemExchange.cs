using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.UI;

namespace Vestiges.World;

/// <summary>
/// Écran d'échange quand le sac est plein (plan 35 §9.3) : le nouvel objet en tête, puis chaque autre objet du sac ;
/// choisir une carte oublie un exemplaire de cet objet et prend le nouveau, « Laisser au sol » ne change rien. Passe par
/// l'écran de choix commun, en pause ; l'oubli ne coûte rien (§110).
/// </summary>
public partial class ItemExchange : Node
{
    private ChoiceScreen _choices;

    public void Setup(ChoiceScreen choices) => _choices = choices;

    /// <summary>
    /// Ouvre l'échange ; <paramref name="take"/> prend le nouvel objet une fois la place faite et rend faux s'il n'a pas pu
    /// entrer : l'exemplaire oublié revient alors. <paramref name="onCancel"/> suit « Laisser au sol ». Rien ne s'ouvre pour
    /// un objet déjà à son maximum d'exemplaires : aucun oubli ne le ferait entrer.
    /// </summary>
    public bool Open(Player player, PassiveSouvenirData incoming, Func<bool> take, Action onCancel = null)
    {
        if (_choices == null || _choices.IsOpen || player == null || incoming == null || player.IsItemAtMax(incoming.Id))
            return false;
        List<ChoiceCard> cards = new();
        List<string> forgettable = new();
        foreach (ActivePassiveSouvenir item in player.PassiveSlots)
        {
            // Oublier le nouvel objet pour le reprendre serait un échange nul.
            if (item.Id == incoming.Id)
                continue;
            ChoiceCard card = new()
            {
                Tag = Tr("ITEM_EXCHANGE_FORGET"),
                Title = $"{item.Data.Name}  ×{item.Copies}",
                Icon = LoadIcon(item.Data.Icon),
            };
            ItemPresentation.ConfigureCard(card, item.Data);
            card.Tag = Tr("ITEM_EXCHANGE_FORGET") + " · " + ItemPresentation.RarityText(item.Data);
            card.Lines.Add(($"Actuel : {ItemPresentation.Describe(item.Data, item.Copies)}", ChoiceStyle.TextColor));
            card.Lines.Add(($"Après oubli : {ItemPresentation.Describe(item.Data, item.Copies - 1)}", ChoiceStyle.TextDim));
            cards.Add(card);
            forgettable.Add(item.Id);
        }
        string subtitle = string.Format(Tr("ITEM_EXCHANGE_SUBTITLE"), incoming.Name + " · " + ItemPresentation.RarityText(incoming), ItemPresentation.Describe(incoming, (player.FindObject(incoming.Id)?.Copies ?? 0) + 1));
        _choices.Open(string.Format(Tr("ITEM_EXCHANGE_TITLE"), player.BagCount, player.BagCapacity), subtitle, cards,
            Tr("ITEM_EXCHANGE_LEAVE"), choice =>
            {
                if (choice < 0 || choice >= forgettable.Count || !IsInstanceValid(player))
                {
                    onCancel?.Invoke();
                    return;
                }
                string forgotten = forgettable[choice];
                if (player.ForgetItemCopy(forgotten) && !take())
                    player.AddItemCopy(forgotten);
            });
        return true;
    }

    private static Texture2D LoadIcon(string path) =>
        !string.IsNullOrEmpty(path) && ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
}
