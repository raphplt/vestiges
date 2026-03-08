namespace Vestiges.UI;

public static class PerkIconResolver
{
    public static string GetPassiveStatIconPath(string stat)
    {
        return stat switch
        {
            "damage" => "assets/ui/icons/ui_icon_perk_damage_up.png",
            "attack_speed" => "assets/ui/icons/ui_icon_perk_attack_speed_up.png",
            "max_hp" => "assets/ui/icons/ui_icon_perk_hp_up.png",
            "speed" => "assets/ui/icons/ui_icon_perk_speed_up.png",
            "armor" => "assets/ui/icons/ui_icon_perk_armor_up.png",
            "aoe_radius" => "assets/ui/icons/ui_icon_perk_aoe_up.png",
            "projectile_count" => "assets/ui/icons/ui_icon_perk_extra_projectile.png",
            "crit_chance" => "assets/ui/icons/ui_icon_perk_crit_chance.png",
            "regen_rate" => "assets/ui/icons/ui_icon_perk_regen_up.png",
            "attack_range" => "assets/ui/icons/ui_icon_perk_range_up.png",
            "xp_magnet_radius" => "assets/ui/icons/ui_icon_perk_xp_magnet.png",
            "cooldown_reduction" => "assets/ui/icons/ui_icon_perk_channeling.png",
            "projectile_pierce" => "assets/ui/icons/ui_icon_perk_piercing_shot.png",
            _ => "assets/ui/icons/ui_icon_perk_damage_up.png"
        };
    }
}
