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
        foreach (StatLine stat in StatLines(player))
        {
            AddLine(container, stat.Label, stat.Value, null, role);
            Label detail = MakeLabel(stat.Detail, TextRole.Caption, UITheme.TextVeryDim);
            detail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            container.AddChild(detail);
        }
        if (player.ObjectStances is { } stances)
            AddLine(container, "Dégâts conditionnels actifs", StatCatalog.FormatBonus("damage", stances.DamageMultiplier, true), null, role);
        if (player.Waymarks is { Total: > 0 } waymarks)
            AddLine(container, Tr("STAT_WAYMARKS"), $"{waymarks.Found} / {waymarks.Total}", null, role);
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

    /// <summary>Les dix-neuf statistiques globales, y compris les zéros ; valeurs de base et contributions lisibles.</summary>
    public readonly record struct StatLine(string Id, string Label, string Value, string Detail);

    public static List<StatLine> StatLines(Player player)
    {
        List<StatLine> rows = new(19);
        string Number(float value) => value.ToString("0.##", French);
        string Fraction(float value) => Number(value * 100f) + " %";
        string BaseBonus(string initial, string gain) => $"{Tr("STAT_BASE")} : {initial} · {Tr("STAT_BUILD")} : {gain}";
        void Add(string id, string key, string value, string detail) => rows.Add(new(id, Tr(key), value, detail));
        void Multiplier(string id, string key, float multiplier) => Add(id, key, Fraction(multiplier),
            BaseBonus("100 %", StatCatalog.FormatBonus(id, multiplier, true)));
        void Flat(string id, string key, float value, float initial, bool percentage = false)
        {
            string current = percentage ? Fraction(value) : Number(value);
            string basis = percentage ? Fraction(initial) : Number(initial);
            string gain = percentage ? StatCatalog.FormatBonus("crit_chance", value - initial, false)
                : (value - initial >= 0f ? "+" : "") + Number(value - initial);
            Add(id, key, current, BaseBonus(basis, gain));
        }
        CharacterStats initial = CharacterDataLoader.Get(player.CharacterId)?.BaseStats;
        Add("max_hp", "STAT_MAX_HP", $"{Number(player.CurrentHp)} / {Number(player.EffectiveMaxHp)}",
            BaseBonus(Number(player.MaxHp), $"+{Number(player.EffectiveMaxHp - player.MaxHp)}"));
        float regenFactor = player.ObjectMilestones?.RegenMultiplier ?? 1f;
        Add("regen_rate", "STAT_REGEN", $"{Number((player.BaseRegenRate + player.BonusRegenRate) * regenFactor * 60f)} PV/min",
            BaseBonus($"{Number(player.BaseRegenRate * 60f)} PV/min", $"+{Number(player.BonusRegenRate * 60f)} PV/min") + $" · Effets actifs ×{Number(regenFactor)} ; suspendue après blessure.");
        DefenseConfig defense = DefenseConfig.Load();
        float stealCapFactor = player.ObjectMilestones?.LifestealCapMultiplier(player.CurrentHp / player.EffectiveMaxHp) ?? 1f;
        Add("lifesteal", "STAT_LIFESTEAL", Fraction(player.Lifesteal), BaseBonus("0 %", Fraction(player.Lifesteal))
            + $" · Plafond commun : {Number(player.EffectiveMaxHp * defense.LifestealMaxHpPerSecond * stealCapFactor)} PV/s ({Fraction(defense.LifestealMaxHpPerSecond * stealCapFactor)} PV max/s).");
        Add("shield", "STAT_SHIELD", $"{Number(player.Shield)} / {Number(player.MaxShield)}",
            BaseBonus(Number(initial?.Shield ?? 0f), $"+{Number(player.MaxShield - (initial?.Shield ?? 0f))}"));
        float armorFactor = player.ObjectMilestones?.ArmorMultiplier(player.Mobility) ?? 1f;
        float armor = player.Armor * armorFactor;
        float reduction = armor <= 0f ? 0f : Mathf.Min(defense.ArmorMaxReduction, armor / (armor + defense.ArmorHalfValue));
        Add("armor", "STAT_ARMOR", $"{Number(armor)} (−{Fraction(reduction)} dégâts)",
            BaseBonus("0", $"+{Number(player.Armor)}") + $" · Effets actifs ×{Number(armorFactor)}.");
        Add("speed", "STAT_SPEED", $"{Number(player.Speed * player.SpeedMultiplier)} px/s",
            BaseBonus($"{Number(player.Speed)} px/s", StatCatalog.FormatBonus("speed", player.SpeedMultiplier, true)));
        Add("crit_chance", "STAT_CRIT", Fraction(Mathf.Clamp(player.CritChance, 0f, 1f)),
            BaseBonus("0 %", Fraction(player.CritChance)) + " · Chance plafonnée à 100 %.");
        Add("crit_multiplier", "STAT_CRIT_MULTIPLIER", $"×{Number(player.CritMultiplier)}",
            BaseBonus("×2", StatCatalog.FormatBonus("crit_multiplier", player.CritMultiplier - 2f, false)));
        Multiplier("damage", "STAT_DAMAGE", player.DamageMultiplier);
        Multiplier("attack_speed", "STAT_ATTACK_SPEED", player.AttackSpeedMultiplier);
        Multiplier("attack_range", "STAT_RANGE", player.AttackRangeMultiplier);
        Multiplier("aoe_radius", "STAT_AOE", player.AoeMultiplier);
        Multiplier("status_duration", "STAT_STATUS_DURATION", player.StatusDurationMultiplier);
        Multiplier("xp_magnet_radius", "STAT_XP_RANGE", player.XpMagnetMultiplier);
        Multiplier("xp_gain", "STAT_XP_GAIN", player.XpGainMultiplier);
        Flat("luck", "STAT_LUCK", player.LuckBonus, 0f, true);
        Flat("projectile_bonus", "STAT_PROJECTILE_BONUS", player.BonusProjectiles, 0f);
        Flat("projectile_pierce", "STAT_PIERCE", player.ProjectilePierce, 0f);
        Add("dash_recharge", "STAT_DASH_RECHARGE", $"{Number(player.Mobility.CooldownSeconds)} s",
            BaseBonus($"{Number(player.Mobility.Config.CooldownSeconds)} s", StatCatalog.FormatBonus("dash_recharge", player.Mobility.RechargeMultiplier, true)));
        return rows;
    }

    /// <summary>Péril : le niveau, puis ce qu'il coûte et ce qu'il rapporte (le score seul : le reste vient des éliminations).</summary>
    private static void AddPerilLines(VBoxContainer container, int peril, TextRole role)
    {
        AddLine(container, Tr("STAT_PERIL"), peril.ToString(), peril > 0 ? PerilColor : null, role);
        if (peril == 0)
            return;
        AddLine(container, "  " + Tr("PERIL_CREATURES"),
            $"{Bonus(PerilDataLoader.EnemyCountMultiplier(peril))} · PV {Bonus(PerilDataLoader.EnemyHpMultiplier(peril))} · {Tr("PERIL_DAMAGE")} {Bonus(PerilDataLoader.EnemyDamageMultiplier(peril))}",
            UITheme.TextDim, role);
        AddLine(container, "  " + Tr("PERIL_REWARDS"), $"score {Bonus(PerilDataLoader.ScoreMultiplier(peril))}", UITheme.TextDim, role);
    }

    /// <summary>
    /// Inventaire compact : armes (niveau et voie), objets (niveau), Réminiscences, chaque section avec ses
    /// emplacements occupés.
    /// </summary>
    public static void AddCompactInventory(VBoxContainer container, Player player, int reminiscenceSlots)
    {
        AddSectionTitle(container, $"{Tr("INVENTORY_WEAPONS")}  {player.WeaponSlots.Count}/{player.WeaponSlotLimit}");
        foreach (WeaponInstance weapon in player.WeaponSlots)
        {
            string name = weapon.Ascension != null ? $"{weapon.Name} · {weapon.Ascension.Name}" : weapon.Name;
            AddCompactRow(container, weapon.Sprite, name, string.Format(Tr("INVENTORY_LEVEL"), weapon.Level));
        }
        AddSectionTitle(container, $"{Tr("INVENTORY_BADGES")}  {player.BadgeSlots.Count}/{InventoryConfig.BadgeSlots}");
        foreach (ActivePassiveSouvenir badge in player.BadgeSlots)
            AddCompactRow(container, badge.Data.IconSmall, badge.Data.Name, string.Format(Tr("INVENTORY_LEVEL"), badge.Level));
        AddSectionTitle(container, $"{Tr("INVENTORY_BAG")}  {player.BagCount}/{player.BagCapacity}");
        foreach (ActivePassiveSouvenir passive in player.PassiveSlots)
            AddCompactRow(container, passive.Data.IconSmall, $"{passive.Data.Name} · {ItemPresentation.RarityText(passive.Data)}", $"×{passive.Copies}");
        string slots = reminiscenceSlots > 0 ? $"  {player.Specializations.Count}/{reminiscenceSlots}" : "";
        AddSectionTitle(container, Tr("INVENTORY_REMINISCENCES") + slots);
        foreach (PerkSpecializationData perk in player.Specializations)
            AddCompactRow(container, perk.IconSmall, perk.Name, "");
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
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(100, 0);
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
        row.AddThemeConstantOverride("separation", 16);
        Label name = MakeLabel(label, role, color ?? StatLabelColor);
        name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        name.AutowrapMode = TextServer.AutowrapMode.Word;
        name.CustomMinimumSize = new Vector2(100f, 0f);
        row.AddChild(name);
        Label amount = MakeLabel(value, role, StatValueColor, HorizontalAlignment.Right);
        amount.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        // Une contrepartie longue peut se replier, sans repousser le nom de la stat hors de sa colonne.
        float valueWidth = UITheme.BodyFont.GetStringSize(value, HorizontalAlignment.Left, -1, UITheme.FontSize(role)).X;
        amount.CustomMinimumSize = new Vector2(Mathf.Min(Mathf.Ceil(valueWidth), 220f), 0f);
        amount.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        row.AddChild(amount);
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
