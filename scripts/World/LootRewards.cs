using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Meta;
using Vestiges.Progression;

namespace Vestiges.World;

/// <summary>
/// Butin concret d'un coffre ou d'un POI. Les tirages « au hasard » (objet à monter, arme, Souvenir) sont résolus
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
    private static readonly Color ObjectColor = new("6ACA5A");
    private static readonly Color WeaponColor = new("E8E0D4");

    /// <summary>
    /// Tirages concrets. Les butins sans équivalent V2 (ressources, malédictions) sont ignorés ; un Souvenir
    /// quand tous sont retrouvés, ou des niveaux d'objet sans objet à monter, deviennent de l'Essence.
    /// </summary>
    public static List<ResolvedLoot> Resolve(List<LootResolver.LootResult> loots, Player player)
    {
        List<ResolvedLoot> resolved = new();
        // Niveaux déjà promis par ce même butin : deux tirages sur un objet ne dépassent pas son maximum.
        Dictionary<string, int> reserved = new();
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
                case "object_level":
                    ActivePassiveSouvenir owned = PickOwnedObject(player, reserved);
                    if (owned == null)
                    {
                        resolved.Add(Essence(loot.FallbackEssence));
                        break;
                    }
                    int already = reserved.GetValueOrDefault(owned.Id);
                    // Un niveau d'objet de coffre vaut une carte commune (plan 23 R3, en attendant R8), borné au niveau maximal.
                    int levels = Mathf.Min(owned.Level + already + loot.Amount, owned.Data.MaxLevel) - owned.Level - already;
                    reserved[owned.Id] = already + levels;
                    resolved.Add(new ResolvedLoot("object_level", owned.Id, levels,
                        string.Format(TranslationServer.Translate("CHEST_LOOT_OBJECT_LEVEL"), owned.Data.Name, levels), ObjectColor));
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
            case "object_level":
                player.AddOrUpgradePassive(loot.ItemId, loot.Amount);
                eventBus.EmitSignal(EventBus.SignalName.LootReceived, loot.Type, loot.ItemId, loot.Amount);
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

    /// <summary>
    /// Objet porté qui peut encore monter, niveaux déjà promis compris, au hasard ; null si aucun (plan 21 §4 : les
    /// coffres montent les objets).
    /// </summary>
    private static ActivePassiveSouvenir PickOwnedObject(Player player, Dictionary<string, int> reserved)
    {
        if (player == null)
            return null;
        List<ActivePassiveSouvenir> candidates = new();
        foreach (ActivePassiveSouvenir passive in player.PassiveSlots)
            if (passive.Level + reserved.GetValueOrDefault(passive.Id) < passive.Data.MaxLevel)
                candidates.Add(passive);
        return candidates.Count > 0 ? candidates[(int)(GD.Randi() % candidates.Count)] : null;
    }

    private static ResolvedLoot Essence(int amount) =>
        new("essence", "essence", amount, Format("CHEST_LOOT_ESSENCE", amount), EssenceColor);

    private static WeaponData PickWeapon(string itemId) =>
        itemId != "random_weapon" ? WeaponDataLoader.Get(itemId) : PickRandomWeapon();

    /// <summary>Une arme débloquée au hasard (coffres, Wagonnet), ou null s'il n'y en a aucune.</summary>
    public static WeaponData PickRandomWeapon()
    {
        List<WeaponData> candidates = new();
        foreach (WeaponData weapon in WeaponDataLoader.GetAll())
        {
            if (MetaSaveManager.IsWeaponUnlocked(weapon))
                candidates.Add(weapon);
        }
        return candidates.Count > 0 ? candidates[(int)(GD.Randi() % candidates.Count)] : null;
    }

    private static string Format(string key, object value) => string.Format(TranslationServer.Translate(key), value);
}
