using Godot;
using Vestiges.Combat;
using Vestiges.Core;

namespace Vestiges.World;

/// <summary>Les objets du monde sont lus au moment de l’interaction ; aucun bonus transitoire à reconstituer au rééquipement.</summary>
public static class WorldItemEffects
{
    public static bool IsWorldStat(string stat) => stat is "free_chest" or "chest_choices" or "item_lifetime" or "chest_xp_pull";
    private static float Total(Player player, string id, string parameter)
    {
        ActivePassiveSouvenir item = player?.FindObject(id);
        return item != null && item.Data.Parameters.TryGetValue(parameter, out float value) ? item.Copies * value : 0f;
    }
    private static float Limit(Player player, string id, string parameter)
    {
        ActivePassiveSouvenir item = player?.FindObject(id);
        return item != null && item.Data.Parameters.TryGetValue(parameter, out float value) ? value : 0f;
    }
    public static float FreeChestChance(Player player) => Mathf.Min(Total(player, "pied_de_biche", "chance_per_copy"), Limit(player, "pied_de_biche", "max_chance"));
    public static int ExtraChoices(Player player) => Mathf.RoundToInt(Total(player, "carnet_dadresses", "extra_choices_per_copy"));
    public static float ExtraItemSeconds(Player player) => Total(player, "ticket_de_vestiaire", "extra_seconds_per_copy");
    public static void AttractChestXp(Player player, Vector2 chestPosition)
    {
        float radius = Total(player, "sonnette", "radius_per_copy");
        if (radius > 0f) XpOrb.AttractExisting(chestPosition, radius, player);
    }
}
