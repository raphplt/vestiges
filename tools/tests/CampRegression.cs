using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Meta;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Le Campement (plan 36, lot M1) dans un profil neuf et isolé. Sans argument : catalogue et fichiers refusés, achat,
/// refus faute de Vestiges, remboursement, rechargement, normalisation d'un profil abîmé, totaux et application au
/// joueur. <c>--run fresh</c> et <c>--run full</c> chargent une vraie run (Main) : deux emplacements d'armes pour un
/// profil neuf, quatre et toutes les stats avec le Campement plein.
/// </summary>
public partial class CampRegression : Node
{
    private const string CatalogPath = "res://data/progression/camp_upgrades.json";
    private const int FullCampPrice = 98_450;

    private int _checks;
    private int _failures;

    public override async void _Ready()
    {
        try
        {
            string run = Argument("--run");
            Check(!DevelopmentMode.IsEnabled, "profil normal");
            if (run == null)
            {
                CheckCatalog();
                CheckRefusals();
                CheckTransactions();
                CheckNormalization();
                CheckApply();
            }
            else
                await CheckRun(run == "full");
            GD.Print($"[CampRegression] RESULT failures={_failures} checks={_checks}");
            if (_failures == 0)
                GD.Print("[CampRegression] PASS");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError($"[CampRegression] FAIL {exception}");
            GetTree().Quit(1);
        }
    }

    private void CheckCatalog()
    {
        Check(CampUpgradeDataLoader.TryLoad(out string error), $"catalogue du dépôt accepté {error}");
        IReadOnlyList<CampUpgradeData> all = CampUpgradeDataLoader.All;
        int total = 0;
        Dictionary<CampBranch, int> branches = new() { [CampBranch.Capacities] = 0, [CampBranch.Stats] = 0, [CampBranch.Comfort] = 0 };
        foreach (CampUpgradeData upgrade in all)
        {
            branches[upgrade.Branch]++;
            foreach (int price in upgrade.Prices)
                total += price;
        }
        Check(all.Count == 14, $"14 améliorations ({all.Count})");
        Check(branches[CampBranch.Capacities] == 4 && branches[CampBranch.Stats] == 9 && branches[CampBranch.Comfort] == 1,
            $"4 capacités, 9 stats, 1 confort ({branches[CampBranch.Capacities]}, {branches[CampBranch.Stats]}, {branches[CampBranch.Comfort]})");
        Check(total == FullCampPrice, $"Campement plein : {FullCampPrice} Vestiges ({total})");
        Check(CampUpgradeDataLoader.BaseWeaponSlots == 2, "deux emplacements d'armes au départ");
        CampUpgradeData slots = CampUpgradeDataLoader.Get("camp_weapon_slots");
        Check(slots != null && slots.MaxRank == 2 && slots.PriceOfRank(1) == 400 && slots.PriceOfRank(2) == 2500,
            "Râtelier : 2 rangs, 400 puis 2 500");
        CampUpgradeData hp = CampUpgradeDataLoader.Get("camp_max_hp");
        Check(hp != null && hp.Effect == CampEffect.Stat && hp.IsMultiplicative && Mathf.IsEqualApprox(hp.Step, 0.05f) && hp.Percent,
            "PV max : +5 % par rang, multiplicatif");
        Check(TranslationServer.Translate(slots?.NameKey ?? "") != slots?.NameKey, "noms traduits");
    }

    private void CheckRefusals()
    {
        string valid = Godot.FileAccess.GetFileAsString(CatalogPath);
        (string, Action<JsonObject>, string)[] cases =
        {
            ("effet inconnu", root => Upgrade(root, 1)["effect"] = "jetpack", "effect : « jetpack » inconnu"),
            ("rayon inconnu", root => Upgrade(root, 1)["branch"] = "cuisine", "branch : « cuisine » inconnu"),
            ("stat non lue", root => Upgrade(root, 4)["stat"] = "peril", "n'est pas lu par le joueur"),
            ("modificateur non lu", root => Upgrade(root, 5)["modifier_type"] = "additive", "n'est pas lu par le joueur"),
            ("stat sur une capacité", root => Upgrade(root, 1)["stat"] = "damage", "ne valent que pour l'effet stat"),
            ("cinq emplacements", root => ((JsonArray)Upgrade(root, 0)["prices"]!).Add(9000), "dépasse 4"),
            ("prix décroissant", root => ((JsonArray)Upgrade(root, 1)["prices"]!)[2] = 10, "inférieur au rang précédent"),
            ("prix nul", root => ((JsonArray)Upgrade(root, 1)["prices"]!)[0] = 0, "prices[0]"),
            ("pas entier pour une capacité", root => Upgrade(root, 1)["step"] = 0.5, "step"),
            ("clé inconnue", root => Upgrade(root, 1)["cost"] = 5, "clé « cost » inconnue"),
            ("id en double", root => Upgrade(root, 3)["id"] = "camp_rerolls", "identifiant en double"),
            ("traduction absente", root => Upgrade(root, 2)["name_key"] = "CAMP_NOWHERE", "absente des traductions"),
            ("base hors bornes", root => root["base_weapon_slots"] = 5, "base_weapon_slots"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            JsonObject root = JsonNode.Parse(valid)!.AsObject();
            mutate(root);
            string message = CampUpgradeDataLoader.Apply(root.ToJsonString());
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"Campement refusé ({label}) : {message}");
        }
        Check(CampUpgradeDataLoader.All.Count == 14 && CampUpgradeDataLoader.Get("camp_rerolls")?.MaxRank == 3,
            "un fichier refusé ne remplace pas le catalogue chargé");
    }

    private void CheckTransactions()
    {
        Check(MetaSaveManager.GetVestiges() == 0 && MetaSaveManager.GetCampRanks().Count == 0, "profil neuf : 0 Vestige, rien d'acheté");
        Check(MetaSaveManager.BuyCampRank("camp_weapon_slots", out _) == CampTransaction.NotEnoughVestiges
            && MetaSaveManager.GetCampRank("camp_weapon_slots") == 0, "sans Vestiges, l'achat est refusé et rien ne change");
        Check(MetaSaveManager.BuyCampRank("camp_jetpack", out _) == CampTransaction.Unknown, "amélioration inconnue refusée");

        MetaSaveManager.AddVestiges(1000);
        Check(MetaSaveManager.BuyCampRank("camp_weapon_slots", out SaveFile.WriteResult saved) == CampTransaction.Done && saved.Succeeded,
            "achat du 3e emplacement, écrit");
        Check(MetaSaveManager.GetVestiges() == 600 && MetaSaveManager.GetCampRank("camp_weapon_slots") == 1,
            $"400 Vestiges dépensés, rang 1 ({MetaSaveManager.GetVestiges()})");
        Check(MetaSaveManager.BuyCampRank("camp_weapon_slots", out _) == CampTransaction.NotEnoughVestiges
            && MetaSaveManager.GetVestiges() == 600, "4e emplacement à 2 500 refusé avec 600 Vestiges");

        ReloadProfile();
        Check(MetaSaveManager.GetVestiges() == 600 && MetaSaveManager.GetCampRank("camp_weapon_slots") == 1,
            "rang et solde identiques après rechargement");

        Check(MetaSaveManager.RefundCampRank("camp_weapon_slots", out saved) == CampTransaction.Done && saved.Succeeded
            && MetaSaveManager.GetVestiges() == 1000 && MetaSaveManager.GetCampRank("camp_weapon_slots") == 0,
            "remboursement gratuit : 1 000 Vestiges, rang 0");
        Check(MetaSaveManager.RefundCampRank("camp_weapon_slots", out _) == CampTransaction.NothingToRefund,
            "rien à rembourser au rang 0");

        MetaSaveManager.AddVestiges(10_000);
        for (int i = 0; i < 3; i++)
            MetaSaveManager.BuyCampRank("camp_rerolls", out _);
        Check(MetaSaveManager.GetCampRank("camp_rerolls") == 3 && MetaSaveManager.GetVestiges() == 11_000 - 3100,
            $"relances au rang 3 pour 3 100 ({MetaSaveManager.GetVestiges()})");
        Check(MetaSaveManager.BuyCampRank("camp_rerolls", out _) == CampTransaction.MaxRank
            && MetaSaveManager.GetVestiges() == 7900, "rang max : achat refusé, rien dépensé");
        MetaSaveManager.RefundCampRank("camp_rerolls", out _);
        Check(MetaSaveManager.GetCampRank("camp_rerolls") == 2 && MetaSaveManager.GetVestiges() == 9900,
            "un rang rendu : son prix (2 000) revient");
        MetaSaveManager.BuyCampRank("camp_damage", out _);
        Check(MetaSaveManager.GetCampRefundAmount() == 1350,
            "la confirmation peut annoncer les 1 350 Vestiges réellement versés, sans modifier le solde");
        int refunded = MetaSaveManager.RefundAllCamp(out saved);
        Check(refunded == 1100 + 250 && saved.Succeeded && MetaSaveManager.GetVestiges() == 11_000
            && MetaSaveManager.GetCampRanks().Count == 0, $"tout rembourser rend 1 350 et vide le Campement ({refunded})");
        ReloadProfile();
        Check(MetaSaveManager.GetVestiges() == 11_000 && MetaSaveManager.GetCampRanks().Count == 0, "Campement vide après rechargement");
    }

    /// <summary>Profil abîmé à la main : amélioration retirée du jeu, rang trop haut, montant versé négatif, rang nul.</summary>
    private void CheckNormalization()
    {
        string path = DevelopmentMode.GetSavePath("meta_save.json");
        JsonObject root = JsonNode.Parse(Godot.FileAccess.GetFileAsString(path))!.AsObject();
        root["vestiges"] = 100;
        root["camp_ranks"] = new JsonObject { ["camp_jetpack"] = 2, ["camp_rerolls"] = 9, ["camp_armor"] = 0, ["camp_luck"] = 1 };
        root["camp_paid"] = new JsonObject { ["camp_jetpack"] = 500, ["camp_rerolls"] = 3100, ["camp_armor"] = 250, ["camp_luck"] = -40 };
        using (Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write))
            file.StoreString(root.ToJsonString());
        ReloadProfile();
        Dictionary<string, int> ranks = MetaSaveManager.GetCampRanks();
        Check(!ranks.ContainsKey("camp_jetpack") && !ranks.ContainsKey("camp_armor"),
            "amélioration inconnue et rang nul retirés");
        Check(MetaSaveManager.GetVestiges() == 100 + 500 + 250, $"leurs Vestiges rendus ({MetaSaveManager.GetVestiges()})");
        Check(ranks.GetValueOrDefault("camp_rerolls") == 3, $"rang borné au maximum ({ranks.GetValueOrDefault("camp_rerolls")})");
        int vestiges = MetaSaveManager.GetVestiges();
        MetaSaveManager.RefundCampRank("camp_luck", out _);
        Check(MetaSaveManager.GetVestiges() == vestiges && MetaSaveManager.GetCampRank("camp_luck") == 0,
            "un montant versé négatif ne rend rien");
        MetaSaveManager.RefundAllCamp(out _);
        Check(MetaSaveManager.GetVestiges() == vestiges + 3100, "le rang borné rend tout ce qui avait été versé");
    }

    private void CheckApply()
    {
        CampUpgrades.Totals fresh = CampUpgrades.Compute(new Dictionary<string, int>(), false);
        Check(fresh == new CampUpgrades.Totals(2, 10, 0, 0, 0), $"profil neuf : 2 emplacements, sac de 10, rien d'autre ({fresh})");
        Dictionary<string, int> full = FullRanks();
        CampUpgrades.Totals totals = CampUpgrades.Compute(full, false);
        Check(totals == new CampUpgrades.Totals(4, 25, 3, 3, 100), $"Campement plein : 4 emplacements, sac de 25, +3, +3, +100 Essence ({totals})");
        Check(CampUpgrades.Compute(new Dictionary<string, int>(), true).WeaponSlots == 4, "mode dev : quatre emplacements");
        Check(CampUpgrades.Compute(new Dictionary<string, int> { ["camp_weapon_slots"] = 7 }, false).WeaponSlots == 4,
            "un rang trop haut ne dépasse pas quatre emplacements");

        Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(player);
        player.InitializeCharacter(CharacterDataLoader.Get("vagabond"));
        Check(player.WeaponSlotLimit == Player.MaxWeaponSlots, "joueur construit hors run : quatre emplacements");
        FragmentManager fragments = new();
        AddChild(fragments);
        EssenceTracker essence = new();
        AddChild(essence);
        int rerolls = fragments.RerollsRemaining, banishes = fragments.BanishesRemaining;
        float maxHp = player.EffectiveMaxHp;
        CampUpgrades.Apply(player, fragments, essence, new Dictionary<string, int>(), false);
        Check(player.WeaponSlotLimit == 2 && player.BagCapacity == 10 && fragments.RerollsRemaining == rerolls && essence.CurrentEssence == 0
            && Mathf.IsEqualApprox(player.EffectiveMaxHp, maxHp) && Mathf.IsEqualApprox(player.DamageMultiplier, 1f),
            "profil neuf appliqué : deux emplacements, rien d'autre");
        int weapons = 0;
        foreach (WeaponData weapon in WeaponDataLoader.GetAll())
            if (player.AddWeapon(weapon))
                weapons++;
        Check(player.WeaponSlots.Count == 2 && weapons == 1 && !player.HasFreeWeaponSlot,
            $"deux armes au plus ({player.WeaponSlots.Count})");
        player.QueueFree();

        player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(player);
        player.InitializeCharacter(CharacterDataLoader.Get("vagabond"));
        maxHp = player.EffectiveMaxHp;
        float regen = player.BonusRegenRate, armor = player.Armor, luck = player.LuckBonus;
        CampUpgrades.Apply(player, fragments, essence, full, false);
        Check(player.WeaponSlotLimit == 4 && player.BagCapacity == 25, $"Campement plein : quatre emplacements, sac de 25 ({player.BagCapacity})");
        Check(fragments.RerollsRemaining == rerolls + 3 && fragments.BanishesRemaining == banishes + 3,
            $"relances +3, bannissements +3 ({fragments.RerollsRemaining}, {fragments.BanishesRemaining})");
        Check(essence.CurrentEssence == 100, $"100 Essence au départ ({essence.CurrentEssence})");
        Check(Mathf.IsEqualApprox(player.EffectiveMaxHp, maxHp * 1.25f), $"PV max ×1,25 ({maxHp} → {player.EffectiveMaxHp})");
        Check(Mathf.IsEqualApprox(player.CurrentHp, player.EffectiveMaxHp), "PV courants remplis d'autant");
        Check(Mathf.IsEqualApprox(player.DamageMultiplier, 1.2f) && Mathf.IsEqualApprox(player.AttackSpeedMultiplier, 1.15f)
            && Mathf.IsEqualApprox(player.SpeedMultiplier, 1.1f), "dégâts ×1,2, cadence ×1,15, vitesse ×1,1, sans composer");
        Check(Mathf.IsEqualApprox(player.BonusRegenRate, regen + 0.5f) && Mathf.IsEqualApprox(player.Armor, armor + 5f)
            && Mathf.IsEqualApprox(player.LuckBonus, luck + 0.15f), "régénération +0,5, armure +5, Chance +0,15");
        Check(Mathf.IsEqualApprox(player.XpMagnetMultiplier, 1.5f) && Mathf.IsEqualApprox(player.XpGainMultiplier, 1.2f),
            "attraction ×1,5, XP ×1,2");
        player.QueueFree();
        fragments.QueueFree();
        essence.QueueFree();
    }

    /// <summary>Une vraie run : le profil neuf porte deux armes au plus ; le Campement plein, quatre.</summary>
    private async Task CheckRun(bool fullCamp)
    {
        if (fullCamp)
        {
            MetaSaveManager.AddVestiges(FullCampPrice);
            foreach (CampUpgradeData upgrade in CampUpgradeDataLoader.All)
                while (MetaSaveManager.BuyCampRank(upgrade.Id, out _) == CampTransaction.Done) { }
            Check(MetaSaveManager.GetVestiges() == 0, $"Campement plein pour {FullCampPrice} Vestiges ({MetaSaveManager.GetVestiges()} restants)");
        }
        // La racine finit d'ajouter ses enfants avant de recevoir la scène de run.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GameManager manager = GetNode<GameManager>("/root/GameManager");
        manager.RunSeed = 42;
        manager.SelectedCharacterId = "vagabond";
        GetTree().CurrentScene = null;
        WorldSetup world = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<WorldSetup>();
        GetTree().Root.AddChild(world);
        GetTree().CurrentScene = world;
        ulong timeout = Time.GetTicksMsec() + 120000;
        while (!world.IsWorldReady || GetTree().Paused || manager.CurrentState != GameManager.GameState.Run)
        {
            if (Time.GetTicksMsec() > timeout)
                throw new InvalidOperationException("Initialisation Main supérieure à 120 s.");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        Player player = world.GetNode<Player>("Player");
        FragmentManager fragments = world.GetNode<FragmentManager>("FragmentManager");
        EssenceTracker essence = world.GetNode<EssenceTracker>("EssenceTracker");
        int expectedSlots = fullCamp ? 4 : 2;
        Check(player.WeaponSlotLimit == expectedSlots, $"run : {expectedSlots} emplacements d'armes ({player.WeaponSlotLimit})");
        Check(fragments.RerollsRemaining == (fullCamp ? 6 : 3), $"run : relances ({fragments.RerollsRemaining})");
        Check(fragments.BanishesRemaining == PerilDataLoader.BanishFree + (fullCamp ? 3 : 0), $"run : bannissements ({fragments.BanishesRemaining})");
        Check(essence.CurrentEssence >= (fullCamp ? 100 : 0), $"run : Essence de départ ({essence.CurrentEssence})");
        Check(Mathf.IsEqualApprox(player.DamageMultiplier, fullCamp ? 1.2f : 1f), $"run : dégâts ×{player.DamageMultiplier}");
        foreach (WeaponData weapon in WeaponDataLoader.GetAll())
            if (MetaSaveManager.IsWeaponUnlocked(weapon.Id))
                player.AddWeapon(weapon);
        Check(player.WeaponSlots.Count == expectedSlots, $"run : {expectedSlots} armes portées au plus ({player.WeaponSlots.Count})");
        world.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static Dictionary<string, int> FullRanks()
    {
        Dictionary<string, int> ranks = new();
        foreach (CampUpgradeData upgrade in CampUpgradeDataLoader.All)
            ranks[upgrade.Id] = upgrade.MaxRank;
        return ranks;
    }

    private static void ReloadProfile()
    {
        typeof(MetaSaveManager).GetMethod("ReloadProfile", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(null, null);
        MetaSaveManager.Load();
    }

    private static JsonObject Upgrade(JsonObject root, int index) => (JsonObject)((JsonArray)root["upgrades"]!)[index]!;

    private static string Argument(string name)
    {
        string[] args = OS.GetCmdlineUserArgs();
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private void Check(bool condition, string label)
    {
        _checks++;
        if (condition)
            GD.Print($"[CampRegression] OK {label}");
        else
        {
            _failures++;
            GD.PushError($"[CampRegression] FAIL {label}");
        }
    }
}
