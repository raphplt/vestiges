using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Meta;
using Vestiges.Progression;

namespace Vestiges.World;

/// <summary>
/// Butin concret d'un coffre ou d'un POI. Les tirages « au hasard » (objet à monter, Souvenir) sont résolus
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

    /// <summary>
    /// Tirages concrets. Les butins sans équivalent V2 (ressources, malédictions) sont ignorés ; un Souvenir
    /// quand tous sont retrouvés, ou des niveaux d'objet sans objet à monter, deviennent de l'Essence.
    /// </summary>
    public static List<ResolvedLoot> Resolve(List<LootResolver.LootResult> loots, Player player, string source = "generic", float luck = 0f)
    {
        List<ResolvedLoot> resolved = new();
        // Niveaux déjà promis par ce même butin : deux tirages sur un objet ne dépassent pas son maximum.
        Dictionary<string, int> reserved = new();
        // Exemplaires d'objets déjà promis : le sac et le maximum de chaque objet comptent avec eux.
        Dictionary<string, int> promisedItems = new();
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
                case "object":
                    PassiveSouvenirData item = PickUnlockedItem(player, RunRandom.Loot, promisedItems, source, luck);
                    if (item == null)
                        break;
                    promisedItems[item.Id] = promisedItems.GetValueOrDefault(item.Id) + 1;
                    // Un exemplaire par tirage ; le sac plein sera géré au ramassage, sans remplacer l'objet.
                    resolved.Add(Object(item));
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

    /// <summary>Applique un butin résolu. Les coffres et les lieux ne donnent pas d'armes (DECISIONS §40).</summary>
    public static void Apply(in ResolvedLoot loot, Player player, EventBus eventBus, Vector2? dropPosition = null)
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
            case "object":
                // Le trop-plein attend au sol, y compris si le sac s'est rempli après le tirage.
                if (player.AddItemCopy(loot.ItemId))
                    eventBus.EmitSignal(EventBus.SignalName.LootReceived, loot.Type, loot.ItemId, loot.Amount);
                else
                    eventBus.EmitSignal(EventBus.SignalName.ItemDropRequested, loot.ItemId, dropPosition ?? player.GlobalPosition);
                break;
            default:
                eventBus.EmitSignal(EventBus.SignalName.LootReceived, loot.Type, loot.ItemId, loot.Amount);
                break;
        }
    }

    /// <summary>
    /// Écusson porté qui peut encore monter, niveaux déjà promis compris, au hasard ; null si aucun (les
    /// anciens lieux peuvent encore offrir des niveaux d’Écussons, contrairement aux coffres).
    /// </summary>
    private static ActivePassiveSouvenir PickOwnedObject(Player player, Dictionary<string, int> reserved)
    {
        if (player == null)
            return null;
        List<ActivePassiveSouvenir> candidates = new();
        foreach (ActivePassiveSouvenir badge in player.BadgeSlots)
            if (badge.Level + reserved.GetValueOrDefault(badge.Id) < badge.Data.MaxLevel)
                candidates.Add(badge);

        return candidates.Count > 0 ? candidates[(int)(RunRandom.Loot.Randi() % candidates.Count)] : null;
    }

    /// <summary>
    /// Objet débloqué au hasard que le joueur peut encore cumuler, exemplaires promis compris (plan 35) ; null si aucun.
    /// La place dans le sac n'est pas exigée : un objet au sol attend qu'une place se libère au Puits.
    /// </summary>
    public static PassiveSouvenirData PickUnlockedItem(Player player, RandomNumberGenerator rng, Dictionary<string, int> promised = null,
        string source = "generic", float luck = 0f) => ItemLootPool.For(player).Pick(source, rng, luck, promised);

    public static ResolvedLoot Object(PassiveSouvenirData item) => new("object", item.Id, 1,
        string.Format(TranslationServer.Translate("CHEST_LOOT_OBJECT"), item.Name), RarityPalette.Main(item.Rarity));

    private static ResolvedLoot Essence(int amount) =>
        new("essence", "essence", amount, Format("CHEST_LOOT_ESSENCE", amount), EssenceColor);

    /// <summary>
    /// Une arme débloquée au hasard (Wagonnet), ou null s'il n'y en a aucune. Avec <paramref name="holder"/>,
    /// les armes qu'il porte sont écartées : son ramassage les refuserait.
    /// </summary>
    public static WeaponData PickRandomWeapon(Player holder = null)
    {
        List<WeaponData> candidates = new();
        foreach (WeaponData weapon in WeaponDataLoader.GetAll())
        {
            if (MetaSaveManager.IsWeaponUnlocked(weapon.Id) && !Holds(holder, weapon.Id))
                candidates.Add(weapon);
        }
        return candidates.Count > 0 ? candidates[(int)(RunRandom.Loot.Randi() % candidates.Count)] : null;
    }

    private static bool Holds(Player holder, string weaponId)
    {
        if (holder == null)
            return false;
        foreach (WeaponInstance weapon in holder.WeaponSlots)
            if (weapon.Id == weaponId)
                return true;
        return false;
    }

    private static string Format(string key, object value) => string.Format(TranslationServer.Translate(key), value);
}
