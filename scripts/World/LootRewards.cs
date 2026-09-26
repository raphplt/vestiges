using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Meta;
using Vestiges.Progression;

namespace Vestiges.World;

/// <summary>
/// Butin concret d'un coffre ou d'un POI. Les tirages « au hasard » (perk, arme, Souvenir) sont résolus
/// avant l'affichage, pour que l'écran de butin montre ce que le joueur reçoit vraiment, puis appliqués.
/// </summary>
public readonly struct ResolvedLoot
{
    public readonly string Type;
    public readonly string ItemId;
    public readonly int Amount;
    public readonly string Label;
    public readonly Color Color;

    public ResolvedLoot(string type, string itemId, int amount, string label, Color color)
    {
        Type = type;
        ItemId = itemId;
        Amount = amount;
        Label = label;
        Color = color;
    }
}

public static class LootRewards
{
    private static readonly Color EssenceColor = new("5EC4C4");
    private static readonly Color XpColor = new("8AB8C4");
    private static readonly Color PerkColor = new("6ACA5A");
    private static readonly Color WeaponColor = new("E8E0D4");

    /// <summary>
    /// Tirages concrets. Les butins sans équivalent V2 (ressources, malédictions) sont ignorés ; un Souvenir
    /// quand tous sont retrouvés devient de l'Essence.
    /// </summary>
    public static List<ResolvedLoot> Resolve(List<LootResolver.LootResult> loots, PerkManager perks)
    {
        List<ResolvedLoot> resolved = new();
        foreach (LootResolver.LootResult loot in loots)
        {
            switch (loot.Type)
            {
                case "essence":
                    resolved.Add(Essence(loot.Amount));
                    break;
                case "xp":
                    resolved.Add(new ResolvedLoot("xp", "xp", loot.Amount, Format("CHEST_LOOT_XP", loot.Amount), XpColor));
                    break;
                case "perk":
                    string perkId = loot.ItemId == "random_perk" ? perks?.PickLootPerk() : loot.ItemId;
                    PerkData perk = perkId != null ? PerkDataLoader.Get(perkId) : null;
                    if (perk != null)
                        resolved.Add(new ResolvedLoot("perk", perkId, 1, Format("CHEST_LOOT_PERK", perk.Name), PerkColor));
                    break;
                case "weapon":
                    WeaponData weapon = PickWeapon(loot.ItemId);
                    if (weapon != null)
                        resolved.Add(new ResolvedLoot("weapon", weapon.Id, 1, Format("CHEST_LOOT_WEAPON", weapon.Name), WeaponColor));
                    break;
                case "souvenir":
                    string souvenirId = loot.ItemId == "random_souvenir" ? SouvenirManager.PickRandomUndiscovered() : loot.ItemId;
                    SouvenirData souvenir = souvenirId != null ? SouvenirDataLoader.Get(souvenirId) : null;
                    resolved.Add(souvenir != null
                        ? new ResolvedLoot("souvenir", souvenirId, 1, Format("CHEST_LOOT_SOUVENIR", souvenir.Name), RarityPalette.Main("lore"))
                        : Essence(8));
                    break;
            }
        }
        return resolved;
    }

    /// <summary>Applique un butin résolu. Une arme sans emplacement libre tombe au sol près de <paramref name="position"/>.</summary>
    public static void Apply(in ResolvedLoot loot, Player player, EventBus eventBus, Vector2 position)
    {
        switch (loot.Type)
        {
            case "xp":
                eventBus.EmitSignal(EventBus.SignalName.XpGained, (float)loot.Amount);
                break;
            case "weapon":
                WeaponData data = WeaponDataLoader.Get(loot.ItemId);
                eventBus.EmitSignal(EventBus.SignalName.LootReceived, loot.Type, loot.ItemId, 1);
                if (data == null || player.AddWeapon(data))
                    break;
                WeaponPickup pickup = new();
                pickup.Initialize(new WeaponInstance(data),
                    position + new Vector2((float)GD.RandRange(-18, 18), (float)GD.RandRange(-12, 12)));
                player.GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, pickup);
                break;
            default:
                eventBus.EmitSignal(EventBus.SignalName.LootReceived, loot.Type, loot.ItemId, loot.Amount);
                break;
        }
    }

    private static ResolvedLoot Essence(int amount) =>
        new("essence", "essence", amount, Format("CHEST_LOOT_ESSENCE", amount), EssenceColor);

    private static WeaponData PickWeapon(string itemId)
    {
        if (itemId != "random_weapon")
            return WeaponDataLoader.Get(itemId);

        List<WeaponData> candidates = new();
        foreach (WeaponData weapon in WeaponDataLoader.GetAll())
        {
            if (string.IsNullOrEmpty(weapon.RequiresSouvenir) || MetaSaveManager.HasSouvenir(weapon.RequiresSouvenir))
                candidates.Add(weapon);
        }
        return candidates.Count > 0 ? candidates[(int)(GD.Randi() % candidates.Count)] : null;
    }

    private static string Format(string key, object value) => string.Format(TranslationServer.Translate(key), value);
}
