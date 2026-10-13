using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.UI;

namespace Vestiges.World;

/// <summary>
/// Oubli à la Faille (plan 44) : contre de l'Essence, une arme, un Écusson ou une pile d'objets quitte la run et libère
/// sa place. Seul endroit du jeu où l'on se défait de ce qu'on a pris. Une arme ou un Écusson oublié laisse une trace :
/// le prochain arrivé sur cette étagère repart d'une part de ses niveaux, en améliorations communes. Le prix monte à
/// chaque oubli de la run. Réglages : <c>rift.forget</c> de data/world/landmarks.json.
/// </summary>
public partial class RiftForgetting : Node
{
    private readonly RiftConfig _config = LandmarkDataLoader.Rift;
    // Flux à part : les gains des traces ne décalent ni les offres des Failles ni les level-ups.
    private readonly RandomNumberGenerator _rng = RunRandom.Create("rift_traces");
    // Niveaux visés par les traces en attente, dans l'ordre des oublis.
    private readonly Queue<int> _weaponTraces = new();
    private readonly Queue<int> _badgeTraces = new();
    private ChoiceScreen _choices;
    private EssenceTracker _essence;
    private EventBus _eventBus;
    private GroupCache _groups;
    private int _forgotten;

    public void Setup(ChoiceScreen choices, EssenceTracker essence)
    {
        _choices = choices;
        _essence = essence;
    }

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _groups = GetNodeOrNull<GroupCache>("/root/GroupCache");
        _eventBus.WeaponEquipped += OnWeaponEquipped;
        _eventBus.PassiveSouvenirAdded += OnPassiveAdded;
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;
        _eventBus.WeaponEquipped -= OnWeaponEquipped;
        _eventBus.PassiveSouvenirAdded -= OnPassiveAdded;
    }

    /// <summary>Prix du prochain oubli.</summary>
    public int Cost => Mathf.RoundToInt(_config.ForgetCost * (1f + _config.ForgetCostGrowth * _forgotten));

    public int PendingWeaponTraces => _weaponTraces.Count;
    public int PendingBadgeTraces => _badgeTraces.Count;

    /// <summary>Quelque chose à oublier : une arme hors la dernière, un Écusson ou un objet.</summary>
    public static bool HasForgettable(Player player) =>
        player.WeaponSlots.Count > 1 || player.BadgeSlots.Count > 0 || player.PassiveSlots.Count > 0;

    /// <summary>Un oubli possible maintenant : quelque chose à oublier, et l'Essence pour le payer.</summary>
    public bool CanForget(Player player) => CanAfford() && HasForgettable(player);

    /// <summary>Niveau de départ que laisse un oubli au niveau <paramref name="level"/>.</summary>
    public int TraceLevel(int level) => Mathf.Max(1, Mathf.FloorToInt(level * _config.ForgetTraceRatio));

    /// <summary>Carte « Oublier » de l'écran de la Faille.</summary>
    public ChoiceCard BuildEntryCard(Player player)
    {
        ChoiceCard card = new()
        {
            Tag = Tr("RIFT_FORGET_TAG"),
            Frame = RarityPalette.Main("rift"),
            Title = Tr("RIFT_FORGET_CARD_TITLE"),
            Price = string.Format(Tr("MEMORIAL_PRICE"), Cost),
            Enabled = CanForget(player),
        };
        card.Lines.Add((Tr("RIFT_FORGET_CARD_LINE"), ChoiceStyle.TextColor));
        card.Lines.Add((string.Format(Tr("RIFT_FORGET_TRACE_RULE"), Mathf.RoundToInt(_config.ForgetTraceRatio * 100f)), ChoiceStyle.TextDim));
        return card;
    }

    /// <summary>
    /// Liste de ce que porte le joueur : armes, Écussons, puis piles du sac. Choisir paie et oublie ;
    /// <paramref name="onBack"/> suit dans tous les cas, pour revenir aux offres de la Faille.
    /// </summary>
    public void Open(Player player, Action onBack)
    {
        int cost = Cost;
        bool affordable = CanAfford();
        List<ChoiceCard> cards = new();
        List<Func<bool>> actions = new();

        foreach (WeaponInstance weapon in player.WeaponSlots)
        {
            WeaponInstance target = weapon;
            bool last = player.WeaponSlots.Count <= 1;
            ChoiceCard card = new()
            {
                Tag = string.Format(Tr("RIFT_FORGET_WEAPON_TAG"), weapon.Level),
                Frame = RarityPalette.Main("rift"),
                Title = weapon.Name,
                Icon = LoadIcon(weapon.Sprite),
                Price = string.Format(Tr("MEMORIAL_PRICE"), cost),
                Enabled = affordable && !last,
            };
            if (last)
                card.Lines.Add((Tr("RIFT_FORGET_LAST_WEAPON"), ChoiceStyle.TextDim));
            else
                AddTraceLine(card, "RIFT_FORGET_WEAPON_TRACE", weapon.Level);
            string deactivated = PerkSpecializationOffers.DeactivatedByForget(player, weapon);
            if (!last && deactivated.Length > 0)
                card.Lines.Add((string.Format(Tr("RIFT_FORGET_PERK_WARNING"), deactivated), ChoiceStyle.LossColor));
            cards.Add(card);
            actions.Add(() => ForgetWeapon(player, target));
        }

        foreach (ActivePassiveSouvenir badge in player.BadgeSlots)
        {
            string id = badge.Id;
            int level = badge.Level;
            ChoiceCard card = new()
            {
                Tag = string.Format(Tr("RIFT_FORGET_BADGE_TAG"), badge.Level),
                Frame = RarityPalette.Main("rift"),
                Title = badge.Data.Name,
                Icon = LoadIcon(badge.Data.Icon),
                Price = string.Format(Tr("MEMORIAL_PRICE"), cost),
                Enabled = affordable,
            };
            AddTraceLine(card, "RIFT_FORGET_BADGE_TRACE", badge.Level);
            cards.Add(card);
            actions.Add(() => ForgetBadge(player, id, level));
        }

        foreach (ActivePassiveSouvenir item in ItemSort.Order(player.PassiveSlots, item => item.Data.Rarity, item => item.Data.Name))
        {
            string id = item.Id;
            ChoiceCard card = new() { Title = $"{item.Data.Name}  {ItemPresentation.CopiesText(item.Data, item.Copies)}" };
            ItemPresentation.ConfigureCard(card, item.Data);
            card.Tag = string.Format(Tr("RIFT_FORGET_ITEM_TAG"), ItemPresentation.RarityText(item.Data));
            card.Price = string.Format(Tr("MEMORIAL_PRICE"), cost);
            card.Enabled = affordable;
            card.Lines.Add((ItemPresentation.Describe(item.Data, item.Copies), ChoiceStyle.TextColor));
            card.Lines.Add((Tr("RIFT_FORGET_ITEM_LINE"), ChoiceStyle.TextDim));
            cards.Add(card);
            actions.Add(() => player.ForgetItemStack(id));
        }

        _choices.Open(Tr("RIFT_FORGET_TITLE"), Tr("RIFT_FORGET_SUBTITLE"), cards, Tr("RIFT_FORGET_BACK"), choice =>
        {
            if (choice >= 0 && choice < actions.Count && GodotObject.IsInstanceValid(player) && _essence.TrySpend(cost))
            {
                if (actions[choice]())
                {
                    _forgotten++;
                    AudioManager.PlayUI("sfx_rift_wake");
                }
                else
                {
                    _essence.AddEssence(cost);
                }
            }
            onBack?.Invoke();
        }, PixelBackdrop.RiftTint, false);
    }

    private bool CanAfford() => (_essence?.CurrentEssence ?? 0) >= Cost;

    private void AddTraceLine(ChoiceCard card, string key, int level)
    {
        int trace = TraceLevel(level);
        if (trace > 1)
            card.Lines.Add((string.Format(Tr(key), trace), ChoiceStyle.GainColor));
        else
            card.Lines.Add((Tr("RIFT_FORGET_NO_TRACE"), ChoiceStyle.TextDim));
    }

    private bool ForgetWeapon(Player player, WeaponInstance weapon)
    {
        int slot = -1;
        for (int i = 0; i < player.WeaponSlots.Count; i++)
            if (player.WeaponSlots[i] == weapon)
                slot = i;
        if (slot < 0 || player.WeaponSlots.Count <= 1 || player.RemoveWeapon(slot) == null)
            return false;
        int trace = TraceLevel(weapon.Level);
        if (trace > 1)
            _weaponTraces.Enqueue(trace);
        return true;
    }

    private bool ForgetBadge(Player player, string id, int level)
    {
        if (!player.ForgetBadge(id))
            return false;
        int trace = TraceLevel(level);
        if (trace > 1)
            _badgeTraces.Enqueue(trace);
        return true;
    }

    /// <summary>
    /// Une arme arrive : la plus ancienne trace d'arme lui donne ses niveaux, en améliorations communes. Les montées se
    /// font pendant l'émission de WeaponEquipped : les abonnés suivants voient déjà l'arme à son niveau de trace.
    /// </summary>
    private void OnWeaponEquipped(string weaponId, int slotIndex)
    {
        if (_weaponTraces.Count == 0 || _groups?.GetPlayer() is not Player player)
            return;
        WeaponInstance weapon = player.GetWeaponInstance(slotIndex);
        if (weapon == null || weapon.Id != weaponId)
            return;
        int target = _weaponTraces.Dequeue();
        UpgradeRarity common = UpgradeRoller.Rarities[0];
        while (weapon.Level < target && weapon.CanLevelUp)
            if (!player.UpgradeWeapon(weaponId, UpgradeRoller.RollWeaponGains(weapon, common, _rng), common.Id))
                break;
        player.ShowPopup(string.Format(Tr("RIFT_TRACE_POPUP"), weapon.Name, weapon.Level), RarityPalette.Main("rift"));
    }

    /// <summary>
    /// Un Écusson arrive : la plus ancienne trace d'Écusson lui donne ses niveaux, en cartes communes. Appelé pendant
    /// l'ajout : les cartes en plus d'une offre à plusieurs cartes s'ajoutent ensuite à la trace.
    /// </summary>
    private void OnPassiveAdded(string passiveId, int slotIndex)
    {
        if (_badgeTraces.Count == 0 || PassiveSouvenirDataLoader.Get(passiveId) is not { IsBadge: true } data
            || _groups?.GetPlayer() is not Player player)
            return;
        int target = _badgeTraces.Dequeue();
        int current = player.GetPassiveLevel(passiveId);
        if (target > current)
            player.AddOrUpgradePassive(passiveId, target - current);
        player.ShowPopup(string.Format(Tr("RIFT_TRACE_POPUP"), data.Name, player.GetPassiveLevel(passiveId)), RarityPalette.Main("rift"));
    }

    private static Texture2D LoadIcon(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        string resPath = path.StartsWith("res://") ? path : $"res://{path}";
        return ResourceLoader.Exists(resPath) ? GD.Load<Texture2D>(resPath) : null;
    }
}
