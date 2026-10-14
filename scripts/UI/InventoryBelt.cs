using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Ceinture du level-up au fil (plan 04 §11) : ce que l'on porte, en poches de cuir sous les fragments. Armes,
/// Écussons, sac (sur deux rangs au-delà de douze places) et Réminiscences. Le fragment survolé allume la poche qu'il
/// touche ; une nouveauté montre son icône en fantôme dans la première poche libre.
/// </summary>
public partial class InventoryBelt : PanelContainer
{
    private const float LargePocket = 80f;
    private const float SmallPocket = 48f;
    private const int BagSingleRow = 12;
    private static readonly Color LabelColor = new("c4aa82");

    private readonly List<BeltPocket> _weapons = new();
    private readonly List<BeltPocket> _badges = new();
    private readonly List<BeltPocket> _bag = new();
    private readonly List<BeltPocket> _reminiscences = new();
    private readonly List<BeltPocket> _all = new();
    private HBoxContainer _groups;

    public InventoryBelt()
    {
        StyleBoxTexture style = PaperArt.BeltStyle();
        style.ContentMarginLeft = style.ContentMarginRight = 36;
        style.ContentMarginTop = 14;
        style.ContentMarginBottom = 18;
        AddThemeStyleboxOverride("panel", style);
        MouseFilter = MouseFilterEnum.Ignore;
        _groups = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        _groups.AddThemeConstantOverride("separation", 24);
        AddChild(_groups);
    }

    /// <summary>Remplit les poches d'après le joueur : reconstruit à chaque offre, un choix précédent a pu tout changer.</summary>
    public void Build(Player player, int reminiscenceSlots)
    {
        MenuSkin.Clear(_groups);
        _weapons.Clear();
        _badges.Clear();
        _bag.Clear();
        _reminiscences.Clear();
        _all.Clear();
        if (player == null)
            return;

        GridContainer weapons = Group($"{Tr("INVENTORY_WEAPONS")}  {player.WeaponSlots.Count}/{player.WeaponSlotLimit}", player.WeaponSlotLimit);
        for (int i = 0; i < player.WeaponSlotLimit; i++)
        {
            BeltPocket pocket = AddPocket(weapons, _weapons, LargePocket, false);
            if (i < player.WeaponSlots.Count)
            {
                WeaponInstance weapon = player.WeaponSlots[i];
                pocket.Fill(weapon.Id, LoadTexture(weapon.Sprite), null, weapon.Level.ToString());
            }
        }

        GridContainer badges = Group($"{Tr("INVENTORY_BADGES")}  {player.BadgeSlots.Count}/{InventoryConfig.BadgeSlots}", InventoryConfig.BadgeSlots);
        for (int i = 0; i < InventoryConfig.BadgeSlots; i++)
        {
            BeltPocket pocket = AddPocket(badges, _badges, LargePocket, false);
            if (i < player.BadgeSlots.Count)
            {
                ActivePassiveSouvenir badge = player.BadgeSlots[i];
                pocket.Fill(badge.Id, LoadTexture(badge.Data.Icon), badge.Data.Rarity, badge.Level.ToString());
            }
        }

        int bagColumns = player.BagCapacity <= BagSingleRow ? player.BagCapacity : (player.BagCapacity + 1) / 2;
        GridContainer bag = Group($"{Tr("INVENTORY_BAG")}  {player.BagCount}/{player.BagCapacity}", bagColumns);
        for (int i = 0; i < player.BagCapacity; i++)
        {
            BeltPocket pocket = AddPocket(bag, _bag, SmallPocket, false);
            if (i < player.PassiveSlots.Count)
            {
                ActivePassiveSouvenir passive = player.PassiveSlots[i];
                pocket.Fill(passive.Id, LoadTexture(passive.Data.Icon), passive.Data.Rarity, passive.Copies > 1 ? $"×{passive.Copies}" : "");
            }
        }

        if (reminiscenceSlots <= 0)
            return;
        GridContainer reminiscences = Group($"{Tr("INVENTORY_REMINISCENCES")}  {player.Specializations.Count}/{reminiscenceSlots}", reminiscenceSlots);
        for (int i = 0; i < reminiscenceSlots; i++)
        {
            BeltPocket pocket = AddPocket(reminiscences, _reminiscences, 64f, true);
            if (i < player.Specializations.Count)
            {
                PerkSpecializationData perk = player.Specializations[i];
                pocket.Fill(perk.Id, LoadTexture(perk.Icon), null, "");
            }
        }
    }

    /// <summary>Allume la poche que touche l'option ; <c>null</c> éteint tout.</summary>
    public void Highlight(FragmentOption option, Texture2D icon)
    {
        foreach (BeltPocket pocket in _all)
            pocket.SetHot(false, null);
        if (option == null)
            return;
        switch (option.Type)
        {
            case "weapon_upgrade" or FragmentOption.SurplusType or FragmentOption.AscensionType:
                Find(_weapons, option.Id)?.SetHot(true, null);
                break;
            case "weapon_new":
                FirstFree(_weapons)?.SetHot(true, icon);
                break;
            case "passive_upgrade":
                (Find(_badges, option.Id) ?? Find(_bag, option.Id))?.SetHot(true, null);
                break;
            case "passive_new":
                bool badge = PassiveSouvenirDataLoader.Get(option.Id)?.IsBadge == true;
                (Find(badge ? _badges : _bag, option.Id) ?? FirstFree(badge ? _badges : _bag))?.SetHot(true, icon);
                break;
            case PerkSpecializationOffers.OptionType:
                FirstFree(_reminiscences)?.SetHot(true, icon);
                break;
        }
    }

    private GridContainer Group(string title, int columns)
    {
        // Deux rivets de cuivre séparent les groupes de poches.
        if (_groups.GetChildCount() > 0)
        {
            VBoxContainer rivets = new() { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
            rivets.AddThemeConstantOverride("separation", 44);
            for (int i = 0; i < 2; i++)
                rivets.AddChild(new TextureRect { Texture = PaperArt.Rivet(), TextureFilter = TextureFilterEnum.Nearest, MouseFilter = MouseFilterEnum.Ignore });
            _groups.AddChild(rivets);
        }
        VBoxContainer group = new() { MouseFilter = MouseFilterEnum.Ignore };
        group.AddThemeConstantOverride("separation", 6);
        _groups.AddChild(group);
        group.AddChild(UITheme.MakeLabel(title.ToUpper(), TextRole.Small, LabelColor));
        GridContainer grid = new() { Columns = Mathf.Max(1, columns), MouseFilter = MouseFilterEnum.Ignore };
        grid.AddThemeConstantOverride("h_separation", 8);
        grid.AddThemeConstantOverride("v_separation", 8);
        group.AddChild(grid);
        return grid;
    }

    private BeltPocket AddPocket(GridContainer grid, List<BeltPocket> section, float size, bool round)
    {
        BeltPocket pocket = new(size, round);
        grid.AddChild(pocket);
        section.Add(pocket);
        _all.Add(pocket);
        return pocket;
    }

    private static BeltPocket Find(List<BeltPocket> section, string id)
    {
        foreach (BeltPocket pocket in section)
            if (pocket.Id == id)
                return pocket;
        return null;
    }

    private static BeltPocket FirstFree(List<BeltPocket> section)
    {
        foreach (BeltPocket pocket in section)
            if (pocket.Id == null)
                return pocket;
        return null;
    }

    private static Texture2D LoadTexture(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        string resPath = path.StartsWith("res://") ? path : $"res://{path}";
        return ResourceLoader.Exists(resPath) ? GD.Load<Texture2D>(resPath) : null;
    }
}
