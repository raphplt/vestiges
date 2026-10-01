using System.Collections.Generic;
using System.Globalization;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Lignes communes à la pause et au level-up (plan 23 R2) : la fiche des stats du joueur et l'inventaire compact
/// (armes, objets, Réminiscences avec leurs niveaux). Un seul endroit décide de ce qu'une stat affiche.
/// </summary>
public static class PlayerSheet
{
    public static readonly Color StatLabelColor = new(0.62f, 0.60f, 0.54f);
    public static readonly Color StatValueColor = new(0.9f, 0.86f, 0.78f);
    public static readonly Color StatBonusColor = new(0.42f, 0.73f, 0.45f);
    public static readonly Color PerilColor = new(0.85f, 0.38f, 0.42f);
    public static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    /// <summary>
    /// Toutes les stats du joueur ; les multiplicateurs se lisent en pourcentage de bonus. Essence et Péril ne
    /// s'affichent que si leurs systèmes sont fournis ; <paramref name="withOublis"/> ajoute le détail des Oublis.
    /// </summary>
    public static void AddStatLines(VBoxContainer container, Player player, EssenceTracker essence, PerilManager peril,
        bool withOublis, TextRole role = TextRole.Body)
    {
        AddLine(container, Tr("STAT_MAX_HP"), $"{player.CurrentHp:F0} / {player.EffectiveMaxHp:F0}", null, role);
        AddLine(container, Tr("STAT_REGEN"), $"{(player.BaseRegenRate + player.BonusRegenRate).ToString("0.0", French)} PV/s", null, role);
        // Plus de bouclier de départ (plan 23 R1) : la ligne n'apparaît qu'avec un objet qui en donne.
        if (player.MaxShield > 0f)
            AddLine(container, Tr("STAT_SHIELD"), $"{player.Shield:F0} / {player.MaxShield:F0}", null, role);
        AddLine(container, Tr("STAT_ARMOR"), $"{player.Armor:F0}  (−{Percent(player.ArmorReduction)})", null, role);
        // Valeurs plutôt que multiplicateurs (plan 24 D1) : un « — » se lisait comme une stat absente.
        AddLine(container, Tr("STAT_SPEED"), WithBonus($"{player.Speed * player.SpeedMultiplier:F0}", player.SpeedMultiplier), null, role);
        AddLine(container, Tr("STAT_CRIT"), $"{Percent(player.CritChance)}  ×{player.CritMultiplier.ToString("0.0", French)}", null, role);
        // Les dégâts montent par arme (cartes d'arme) : les bonus globaux ne s'affichent que s'ils existent. La portée
        // propre au personnage fait partie de ses armes ; seule la portée gagnée en run s'affiche ici.
        AddBonusLine(container, Tr("STAT_RANGE"), player.AttackRangeMultiplier, role);
        AddBonusLine(container, Tr("STAT_DAMAGE"), player.DamageMultiplier, role);
        AddBonusLine(container, Tr("STAT_ATTACK_SPEED"), player.AttackSpeedMultiplier, role);
        AddBonusLine(container, Tr("STAT_AOE"), player.AoeMultiplier, role);
        AddBonusLine(container, Tr("STAT_STATUS_DURATION"), player.StatusDurationMultiplier, role);
        AddBonusLine(container, Tr("STAT_XP_RANGE"), player.XpMagnetMultiplier, role);
        AddLine(container, Tr("STAT_LUCK"), Percent(player.LuckBonus), null, role);
        if (player.Waymarks is { Total: > 0 } waymarks)
            AddLine(container, Tr("STAT_WAYMARKS"), $"{waymarks.Found} / {waymarks.Total}", null, role);
        if (player.BonusProjectiles > 0f)
            AddLine(container, Tr("STAT_PROJECTILE_BONUS"), $"+{StatCatalog.CountText(player.BonusProjectiles)}", null, role);
        if (player.ProjectilePierce > 0f)
            AddLine(container, Tr("STAT_PIERCE"), $"+{StatCatalog.CountText(player.ProjectilePierce)}", null, role);
        if (essence != null)
            AddLine(container, "Essence", essence.CurrentEssence.ToString(), null, role);
        if (peril == null)
            return;
        AddPerilLines(container, peril.Peril, role);
        if (!withOublis)
            return;
        foreach (ActiveOubli oubli in peril.Oublis)
        {
            AddLine(container, "  " + Tr(oubli.Data.NameKey), oubli.Data.Permanent ? Tr("OUBLI_PERMANENT") : "", PerilColor, role);
            Label effect = MakeLabel("    " + oubli.Data.Describe(), TextRole.Caption, UITheme.TextVeryDim);
            effect.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            container.AddChild(effect);
        }
    }

    /// <summary>Ligne d'un bonus global en pourcentage, omise tant qu'il vaut 0.</summary>
    private static void AddBonusLine(VBoxContainer container, string label, float multiplier, TextRole role)
    {
        if (Mathf.RoundToInt((multiplier - 1f) * 100f) != 0)
            AddLine(container, label, Bonus(multiplier), null, role);
    }

    /// <summary>Valeur suivie de son bonus entre parenthèses, s'il y en a un.</summary>
    private static string WithBonus(string value, float multiplier) =>
        Mathf.RoundToInt((multiplier - 1f) * 100f) == 0 ? value : $"{value}  ({Bonus(multiplier)})";

    /// <summary>Péril : le niveau, puis ce qu'il coûte et ce qu'il rapporte.</summary>
    private static void AddPerilLines(VBoxContainer container, int peril, TextRole role)
    {
        AddLine(container, Tr("STAT_PERIL"), peril.ToString(), peril > 0 ? PerilColor : null, role);
        if (peril == 0)
            return;
        AddLine(container, "  " + Tr("PERIL_CREATURES"),
            $"{Bonus(PerilDataLoader.EnemyCountMultiplier(peril))} · PV {Bonus(PerilDataLoader.EnemyHpMultiplier(peril))}", UITheme.TextDim, role);
        AddLine(container, "  " + Tr("PERIL_REWARDS"),
            $"XP {Bonus(PerilDataLoader.XpMultiplier(peril))} · score {Bonus(PerilDataLoader.ScoreMultiplier(peril))}", UITheme.TextDim, role);
    }

    /// <summary>
    /// Inventaire compact : armes (niveau et voie), objets (niveau), Réminiscences, chaque section avec ses
    /// emplacements occupés.
    /// </summary>
    public static void AddCompactInventory(VBoxContainer container, Player player, int reminiscenceSlots)
    {
        AddSectionTitle(container, $"{Tr("INVENTORY_WEAPONS")}  {player.WeaponSlots.Count}/{Player.MaxWeaponSlots}");
        foreach (WeaponInstance weapon in player.WeaponSlots)
        {
            string name = weapon.Ascension != null ? $"{weapon.Name} · {weapon.Ascension.Name}" : weapon.Name;
            AddCompactRow(container, weapon.Sprite, name, string.Format(Tr("INVENTORY_LEVEL"), weapon.Level));
        }
        AddSectionTitle(container, $"{Tr("INVENTORY_OBJECTS")}  {player.PassiveSlots.Count}/{Player.MaxPassiveSlots}");
        foreach (ActivePassiveSouvenir passive in player.PassiveSlots)
            AddCompactRow(container, PerkIconResolver.GetPassiveStatIconPath(passive.Data.Stat), passive.Data.Name,
                string.Format(Tr("INVENTORY_LEVEL"), passive.Level));
        string slots = reminiscenceSlots > 0 ? $"  {player.Specializations.Count}/{reminiscenceSlots}" : "";
        AddSectionTitle(container, Tr("INVENTORY_REMINISCENCES") + slots);
        foreach (PerkSpecializationData perk in player.Specializations)
            AddCompactRow(container, null, perk.Name, "");
        if (player.Specializations.Count == 0)
            container.AddChild(MakeLabel(Tr("INVENTORY_NONE"), TextRole.Small, UITheme.TextVeryDim));
    }

    private static void AddCompactRow(VBoxContainer container, string iconPath, string name, string level)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 8);
        if (iconPath != null)
            row.AddChild(MakeIcon(iconPath, 32));
        Label label = MakeLabel(name, TextRole.Small, StatValueColor);
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        label.ClipText = true;
        row.AddChild(label);
        if (level.Length > 0)
            row.AddChild(MakeLabel(level, TextRole.Small, StatBonusColor, HorizontalAlignment.Right));
        container.AddChild(row);
    }

    public static void AddSectionTitle(VBoxContainer container, string text) =>
        container.AddChild(MakeLabel(text.ToUpper(), TextRole.Caption, UITheme.TextDim));

    public static void AddLine(VBoxContainer container, string label, string value, Color? color = null, TextRole role = TextRole.Body)
    {
        HBoxContainer row = new();
        Label name = MakeLabel(label, role, color ?? StatLabelColor);
        name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(name);
        row.AddChild(MakeLabel(value, role, StatValueColor, HorizontalAlignment.Right));
        container.AddChild(row);
    }

    public static Label MakeLabel(string text, TextRole role, Color color, HorizontalAlignment align = HorizontalAlignment.Left) =>
        UITheme.MakeLabel(text, role, color, TextWeight.Regular, align);

    public static Control MakeIcon(string path, float size = 32f)
    {
        TextureRect icon = new()
        {
            CustomMinimumSize = new Vector2(size, size),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        if (!string.IsNullOrEmpty(path))
        {
            string resPath = path.StartsWith("res://") ? path : $"res://{path}";
            if (ResourceLoader.Exists(resPath))
                icon.Texture = GD.Load<Texture2D>(resPath);
        }
        return icon;
    }

    public static string Percent(float fraction) => $"{Mathf.RoundToInt(fraction * 100f)} %";

    /// <summary>Multiplicateur lu en bonus : ×1,15 devient « +15 % », ×1 devient « — ».</summary>
    public static string Bonus(float multiplier)
    {
        int percent = Mathf.RoundToInt((multiplier - 1f) * 100f);
        return percent == 0 ? "—" : $"{(percent > 0 ? "+" : "")}{percent} %";
    }

    private static string Tr(string key) => TranslationServer.Translate(key);
}
