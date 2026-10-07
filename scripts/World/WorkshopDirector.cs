using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.UI;

namespace Vestiges.World;

/// <summary>
/// Mène les Ateliers (plan 22 C2) : Trempe offerte à la première visite, niveau d'arme et Retrempe contre de
/// l'Essence, perte dans le Néant. Réglages : section <c>workshop</c> de data/world/landmarks.json.
/// </summary>
public partial class WorkshopDirector : Node
{
    private const float LossCheckInterval = 1f;
    private const string ServiceWeapon = "weapon";
    private const string ServiceRetemper = "retemper";

    private readonly WorkshopConfig _config = LandmarkDataLoader.Workshop;
    private readonly RandomNumberGenerator _rng = new();
    private ChoiceScreen _choices;
    private EssenceTracker _essence;
    private ErasureManager _erasure;
    private EventBus _eventBus;
    private Player _player;
    private float _lossTimer = LossCheckInterval;

    public void Setup(ChoiceScreen choices, EssenceTracker essence, ErasureManager erasure)
    {
        _choices = choices;
        _essence = essence;
        _erasure = erasure;
    }

    public override void _Ready()
    {
        _rng.Seed = RunRandom.SeedFor("workshops");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.WorkshopInteracted += OnWorkshopInteracted;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.WorkshopInteracted -= OnWorkshopInteracted;
    }

    public override void _Process(double delta)
    {
        _lossTimer -= (float)delta;
        if (_lossTimer > 0f)
            return;
        _lossTimer = LossCheckInterval;
        CheckLost();
    }

    private void OnWorkshopInteracted(Node2D node)
    {
        if (node is not Workshop workshop || workshop.IsLost || !CachePlayer())
            return;
        string welcome = null;
        if (workshop.MarkVisited())
        {
            _player.GrantTemper(_config.TemperUpgrades);
            _eventBus.EmitSignal(EventBus.SignalName.WorkshopVisited, workshop.GlobalPosition);
            welcome = string.Format(Tr("WORKSHOP_TEMPERED"), _config.TemperUpgrades);
        }
        OpenServices(workshop, welcome, true);
    }

    /// <summary>Services de l'Atelier ; rouvert après chaque achat, l'entrée ne se joue qu'à l'arrivée.</summary>
    private void OpenServices(Workshop workshop, string lastResult, bool animate)
    {
        List<ChoiceCard> cards = new();
        List<System.Action> actions = new();
        int essence = _essence?.CurrentEssence ?? 0;

        int weaponCost = Price(_config.WeaponCost, workshop.ServiceUses(ServiceWeapon));
        foreach (WeaponInstance weapon in _player.WeaponSlots)
        {
            ChoiceCard card = new()
            {
                Tag = string.Format(Tr("WORKSHOP_WEAPON_TAG"), RarityPalette.DisplayName(_config.WeaponMinRarity)).ToUpper(),
                Frame = RarityPalette.Main(_config.WeaponMinRarity),
                Rank = RarityArt.Rank(_config.WeaponMinRarity),
                Title = string.Format(Tr("WORKSHOP_WEAPON_TITLE"), weapon.Name),
                Price = string.Format(Tr("MEMORIAL_PRICE"), weaponCost),
                Enabled = essence >= weaponCost && weapon.CanLevelUp,
                Icon = LoadIcon(weapon.Base.Sprite),
            };
            card.Lines.Add((string.Format(Tr("LEVELUP_LEVEL"), weapon.Level, weapon.Level + 1), ChoiceStyle.TextColor));
            cards.Add(card);
            WeaponInstance target = weapon;
            actions.Add(() => UpgradeWeapon(workshop, target, weaponCost));
        }

        // Retrempe : seulement pour une arme dont la dernière amélioration est connue (rareté et gains).
        int retemperCost = Price(_config.RetemperCost, workshop.ServiceUses(ServiceRetemper));
        foreach (WeaponInstance weapon in _player.WeaponSlots)
        {
            if (weapon.LastRarityId == null || weapon.LastGains.Count == 0)
                continue;
            ChoiceCard card = new()
            {
                Tag = string.Format(Tr("WORKSHOP_RETEMPER_TAG"), RarityPalette.DisplayName(weapon.LastRarityId)).ToUpper(),
                Frame = RarityPalette.Main("workshop"),
                Rank = RarityArt.Rank(weapon.LastRarityId),
                Title = string.Format(Tr("WORKSHOP_RETEMPER_TITLE"), weapon.Name),
                Price = string.Format(Tr("MEMORIAL_PRICE"), retemperCost),
                Enabled = essence >= retemperCost,
                Icon = LoadIcon(weapon.Base.Sprite),
            };
            card.Lines.Add((Tr("WORKSHOP_RETEMPER_LINE"), ChoiceStyle.TextColor));
            cards.Add(card);
            WeaponInstance target = weapon;
            actions.Add(() => Retemper(workshop, target, retemperCost));
        }

        string subtitle = string.Format(Tr("MEMORIAL_ESSENCE"), essence);
        if (_player.TemperCharges > 0)
            subtitle = $"{subtitle}   ·   {string.Format(Tr("WORKSHOP_TEMPER_LEFT"), _player.TemperCharges)}";
        if (!string.IsNullOrEmpty(lastResult))
            subtitle = $"{lastResult}   ·   {subtitle}";
        _choices.Open(Tr("WORKSHOP_TITLE"), subtitle, cards, Tr("MEMORIAL_LEAVE"), choice =>
        {
            if (choice >= 0 && CachePlayer())
                actions[choice]();
        }, PixelBackdrop.WorkshopTint, animate);
    }

    private void UpgradeWeapon(Workshop workshop, WeaponInstance weapon, int cost)
    {
        if (!_essence.TrySpend(cost))
            return;
        UpgradeRarity rarity = UpgradeRoller.RollRarityAtLeast(BumpSteps(workshop.GlobalPosition), _config.WeaponMinRarity, _rng);
        List<StatGain> gains = UpgradeRoller.RollWeaponGains(weapon, rarity, _rng, _player.TemperCharges > 0 ? 1 : 0);
        if (!_player.UpgradeWeapon(weapon.Id, gains, rarity.Id))
        {
            _essence.AddEssence(cost);
            OpenServices(workshop, null, false);
            return;
        }
        workshop.RecordServiceUse(ServiceWeapon);
        OpenServices(workshop, $"{weapon.Name} : {RarityPalette.DisplayName(rarity.Id)}", false);
    }

    /// <summary>Retrempe : la dernière amélioration est retirée et remplacée, à rareté égale ou supérieure, même nombre de stats.</summary>
    private void Retemper(Workshop workshop, WeaponInstance weapon, int cost)
    {
        if (!_essence.TrySpend(cost))
            return;
        UpgradeRarity rarity = UpgradeRoller.RollRarityAtLeast(BumpSteps(workshop.GlobalPosition), weapon.LastRarityId, _rng);
        int extra = Mathf.Max(0, weapon.LastGains.Count - rarity.WeaponStats);
        if (!_player.RetemperWeapon(weapon.Id, UpgradeRoller.RollWeaponGains(weapon, rarity, _rng, extra), rarity.Id))
        {
            _essence.AddEssence(cost);
            OpenServices(workshop, null, false);
            return;
        }
        workshop.RecordServiceUse(ServiceRetemper);
        OpenServices(workshop, string.Format(Tr("WORKSHOP_RETEMPERED"), weapon.Name, RarityPalette.DisplayName(rarity.Id)), false);
    }

    private int Price(int baseCost, int uses) => Mathf.RoundToInt(baseCost * (1f + _config.CostGrowth * uses));

    private void CheckLost()
    {
        if (_erasure == null)
            return;
        IReadOnlyList<Workshop> all = Workshop.All;
        for (int i = 0; i < all.Count; i++)
        {
            Workshop workshop = all[i];
            if (!workshop.IsLost && _erasure.GetZonePhaseAt(workshop.GlobalPosition) == ErasureManager.ErasureZonePhase.Void)
                workshop.MarkLost();
        }
    }

    /// <summary>Crans de montée de rareté : Chance du joueur, oubli de la zone de l'Atelier, Péril.</summary>
    private float BumpSteps(Vector2 position)
    {
        ErasureManager.ErasureZonePhase phase = _erasure?.GetZonePhaseAt(position) ?? ErasureManager.ErasureZonePhase.Anchored;
        return UpgradeRoller.BumpSteps(_player.LuckBonus, phase);
    }

    private static Texture2D LoadIcon(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        string resPath = path.StartsWith("res://") ? path : $"res://{path}";
        return ResourceLoader.Exists(resPath) ? GD.Load<Texture2D>(resPath) : null;
    }

    private bool CachePlayer()
    {
        if (_player == null || !IsInstanceValid(_player))
            _player = GetTree().GetFirstNodeInGroup("player") as Player;
        return _player != null;
    }
}
