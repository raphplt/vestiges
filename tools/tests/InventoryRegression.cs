using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Inventaire du joueur (plan 35), dans un profil neuf et isolé. Lot I1 : réglages contrôlés, Écussons à part des objets,
/// rendement au-delà du seuil (aperçu de carte et valeur réelle identiques), déblocage d'un Écusson par sa quête. Lot I2 :
/// objets en exemplaires (niveau d'effet, palier au doublon), oubli qui retire effet et palier, sac plein, butin « objet ».
/// </summary>
public partial class InventoryRegression : Node
{
    private const string ConfigPath = "res://data/progression/inventory.json";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private int _checks;
    private int _failures;
    private Player _player;

    public override void _Ready()
    {
        try
        {
            Check(!DevelopmentMode.IsEnabled, "profil normal");
            CheckConfig();
            CheckBadgeShelf();
            CheckBadgeUnlock();
            CheckItemCopies();
            CheckForgetting();
            CheckBag();
            CheckChestItem();
            GD.Print($"[InventoryRegression] RESULT failures={_failures} checks={_checks}");
            if (_failures == 0)
                GD.Print("[InventoryRegression] PASS");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError($"[InventoryRegression] FAIL {exception}");
            GetTree().Quit(1);
        }
    }

    private void CheckConfig()
    {
        Check(InventoryConfig.TryLoad(out string error), $"réglages du dépôt acceptés {error}");
        Check(InventoryConfig.BadgeSlots == 4, "quatre Écussons");
        (string, string, string)[] cases =
        {
            ("emplacements nuls", "{\"badge_slots\": 0, \"badge_soft_cap_level\": 30, \"badge_soft_cap_factor\": 0.5}", "badge_slots"),
            ("facteur nul", "{\"badge_slots\": 4, \"badge_soft_cap_level\": 30, \"badge_soft_cap_factor\": 0}", "badge_soft_cap_factor"),
            ("facteur au-delà de 1", "{\"badge_slots\": 4, \"badge_soft_cap_level\": 30, \"badge_soft_cap_factor\": 1.5}", "badge_soft_cap_factor"),
            ("clé inconnue", "{\"badge_slots\": 4, \"badge_soft_cap_level\": 30, \"badge_soft_cap_factor\": 0.5, \"tomes\": 4}", "clé « tomes » inconnue"),
            ("seuil absent", "{\"badge_slots\": 4, \"badge_soft_cap_factor\": 0.5}", "badge_soft_cap_level absent"),
            ("sac vide", Config("\"bag_size\": 0", "\"item_copy_levels\": [10, 15]"), "bag_size"),
            ("exemplaires qui ne montent pas", Config("\"bag_size\": 10", "\"item_copy_levels\": [10, 10]"),
                "item_copy_levels[1] : 10 pas plus haut"),
            ("exemplaires absents", Config("\"bag_size\": 10", "\"item_copy_levels\": []"), "item_copy_levels"),
        };
        foreach ((string label, string json, string expected) in cases)
        {
            string message = InventoryConfig.Apply(json);
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"réglages refusés ({label}) : {message}");
        }
        Check(InventoryConfig.BadgeSlots == 4 && Mathf.IsEqualApprox(InventoryConfig.BadgeStepFactor(31), 0.5f)
            && Mathf.IsEqualApprox(InventoryConfig.BadgeStepFactor(30), 1f), "un fichier refusé ne remplace pas les réglages chargés");
        InventoryConfig.Apply(Godot.FileAccess.GetFileAsString(ConfigPath));
    }

    private void CheckBadgeShelf()
    {
        Setup();
        _player.AddOrUpgradePassive("instinct");
        _player.AddOrUpgradePassive("glacon");
        Check(_player.BadgeSlots.Count == 1 && _player.PassiveSlots.Count == 1 && _player.FindObject("instinct").Data.IsBadge
            && !_player.FindObject("glacon").Data.IsBadge, "un Écusson et un objet vont chacun sur leur étagère");

        // L'aperçu d'une carte et la valeur obtenue suivent le même rendement, de part et d'autre du seuil.
        ActivePassiveSouvenir badge = _player.FindObject("instinct");
        _player.AddOrUpgradePassive("instinct", 28);
        float preview = badge.ValueAfter(0, 1f);
        _player.AddOrUpgradePassive("instinct");
        bool fullStep = Mathf.IsEqualApprox(badge.Value(0), preview) && badge.Level == 30;
        preview = badge.ValueAfter(0, 3f);
        float before = badge.Value(0);
        _player.AddOrUpgradePassive("instinct", 1, 3f);
        Check(fullStep && Mathf.IsEqualApprox(badge.Value(0), preview)
            && Mathf.IsEqualApprox(badge.Value(0) - before, badge.Data.Effects[0].Step * 3f * 0.5f),
            $"niveau 31 : un demi-pas, même valeur annoncée et obtenue ({before:0.###} → {badge.Value(0):0.###})");

        ActivePassiveSouvenir item = _player.FindObject("glacon");
        _player.AddOrUpgradePassive("glacon", 40);
        Check(item.Level == 30 && item.IsMaxLevel, "un objet reste plafonné au niveau 30");
    }

    private void CheckBadgeUnlock()
    {
        QuestDefinition quest = QuestDataLoader.FindUnlocking(UnlockKind.Badge, "oeil_critique");
        Check(quest?.Id == "read_closely" && QuestDataLoader.FindUnlocking(UnlockKind.Object, "oeil_critique") == null,
            "l'Écusson de l'horloger est débloqué par « Lire de près », comme Écusson");
        bool lockedBefore = !MetaSaveManager.IsObjectUnlocked("oeil_critique") && MetaSaveManager.IsObjectUnlocked("memoire_vive");
        MetaSaveManager.ClaimQuest(quest, out _);
        Check(lockedBefore && MetaSaveManager.IsObjectUnlocked("oeil_critique"), "verrouillé en profil neuf, ouvert une fois la quête accomplie");
    }

    private static string Config(string bag, string copies) =>
        $"{{\"badge_slots\": 4, \"badge_soft_cap_level\": 30, \"badge_soft_cap_factor\": 0.5, {bag}, {copies}}}";

    private void CheckItemCopies()
    {
        Setup();
        PassiveSouvenirData chill = PassiveSouvenirDataLoader.Get("glacon");
        Check(InventoryConfig.BagSize == 10 && chill.CopyLevels.Length == 6 && chill.CopyLevels[0] == 10 && chill.CopyLevels[1] == 15,
            "sac de dix ; un objet vaut son niveau 10, son doublon le niveau 15");
        Check(PassiveSouvenirDataLoader.Get("sifflet_d_arbitre").CopyLevels[2] == 3, "le Sifflet garde ses propres exemplaires : un Péril chacun");
        _player.AddItemCopy("glacon");
        ActivePassiveSouvenir item = _player.FindObject("glacon");
        float step = chill.Effects[0].Step;
        Check(item.Copies == 1 && item.Level == 10 && _player.BagCount == 1 && Mathf.IsEqualApprox(_player.ObjectTriggers.ChillChance, step * 10f),
            $"un exemplaire : niveau 10, une place ({_player.ObjectTriggers.ChillChance:0.###} de chance)");
        _player.AddItemCopy("glacon");
        Check(item.Copies == 2 && item.Level == 15 && _player.BagCount == 2 && FreezeSeconds() > 0f,
            "deux exemplaires : niveau 15 et le palier (le double ralentissement fige)");
        for (int i = 0; i < 6; i++)
            _player.AddItemCopy("glacon");
        Check(item.Copies == 6 && item.Level == 25 && _player.BagCount == 6 && !_player.CanTakeItem("glacon"),
            "six exemplaires au plus, niveau 25 ; le septième est refusé");
    }

    private void CheckForgetting()
    {
        Setup();
        _player.AddItemCopy("glacon");
        _player.AddItemCopy("glacon");
        _player.ForgetItemCopy("glacon");
        ActivePassiveSouvenir item = _player.FindObject("glacon");
        Check(item.Copies == 1 && item.Level == 10 && FreezeSeconds() == 0f && _player.BagCount == 1,
            "oublier le doublon : retour au niveau 10, palier retiré");
        _player.ForgetItemCopy("glacon");
        Check(_player.FindObject("glacon") == null && Mathf.IsZeroApprox(_player.ObjectTriggers.ChillChance) && _player.BagCount == 0,
            "oublier le dernier exemplaire : l'objet quitte le sac, son effet aussi");
        Check(!_player.ForgetItemCopy("glacon") && !_player.ForgetItemCopy("instinct"), "rien à oublier : ni objet absent, ni Écusson");

        // Un objet monté puis entièrement oublié ne laisse aucun reste d'arrondi : son effet ne se déclenche plus.
        foreach (string id in new[] { "chewing_gum", "petard_mouille", "semelle_usee" })
        {
            for (int i = 0; i < 6; i++)
                _player.AddItemCopy(id);
            for (int i = 0; i < 6; i++)
                _player.ForgetItemCopy(id);
        }
        bool clean = true;
        foreach (string field in new[] { "_dashTrail", "_killExplosion", "_strideDamage" })
            clean &= (float)typeof(ObjectTriggers).GetField(field, Private).GetValue(_player.ObjectTriggers) == 0f;
        Check(clean && !_player.ObjectTriggers.IsProcessing() && _player.BagCount == 0,
            "six exemplaires pris puis oubliés : effets exactement à zéro, plus d'horloge par image");

        float speed = _player.AttackSpeedMultiplier;
        _player.AddItemCopy("tabouret_de_camping");
        _player.AddItemCopy("tabouret_de_camping");
        float stillArmor = (float)typeof(ObjectStances).GetField("_stillArmor", Private).GetValue(_player.ObjectStances);
        _player.ForgetItemCopy("tabouret_de_camping");
        _player.ForgetItemCopy("tabouret_de_camping");
        Check(stillArmor > 0f && (float)typeof(ObjectStances).GetField("_stillArmor", Private).GetValue(_player.ObjectStances) == 0f
            && Mathf.IsEqualApprox(_player.AttackSpeedMultiplier, speed), "un objet d'état oublié rend son palier et sa cadence");
    }

    private void CheckBag()
    {
        Setup();
        string[] ids = { "glacon", "allumette_humide", "de_a_coudre", "semelle_usee", "chewing_gum" };
        foreach (string id in ids)
        {
            _player.AddItemCopy(id);
            _player.AddItemCopy(id);
        }
        Check(_player.BagCount == 10 && !_player.HasBagRoom && !_player.AddItemCopy("thermometre") && !_player.AddItemCopy("glacon"),
            "sac plein : ni objet neuf, ni doublon");
        _player.ForgetItemCopy("chewing_gum");
        Check(_player.AddItemCopy("thermometre") && _player.BagCount == 10, "une place oubliée se reprend");
        _player.SetBagCapacity(13);
        Check(_player.HasBagRoom && _player.AddItemCopy("glacon"), "les Poches agrandissent le sac");
    }

    private void CheckChestItem()
    {
        Setup();
        List<LootResolver.LootResult> loot = new() { new LootResolver.LootResult { Type = "object", ItemId = "random_item", Amount = 1, FallbackEssence = 6 } };
        List<ResolvedLoot> resolved = LootRewards.Resolve(loot, _player);
        PassiveSouvenirData item = PassiveSouvenirDataLoader.Get(resolved[0].ItemId);
        Check(resolved[0].Type == "object" && item != null && !item.IsBadge && MetaSaveManager.IsObjectUnlocked(item.Id),
            $"coffre : un objet débloqué ({resolved[0].ItemId})");
        LootRewards.Apply(resolved[0], _player, GetNode<EventBus>("/root/EventBus"));
        Check(_player.FindObject(item.Id)?.Copies == 1, "l'objet du coffre entre dans le sac");
        _player.SetBagCapacity(1);
        resolved = LootRewards.Resolve(loot, _player);
        Check(resolved[0].Type == "essence" && resolved[0].Amount == 6, "sac plein : le coffre donne son Essence de secours");
    }

    private float FreezeSeconds() =>
        (float)typeof(ObjectTriggers).GetField("_freezeSeconds", Private).GetValue(_player.ObjectTriggers);

    private void Setup()
    {
        if (_player != null)
        {
            RemoveChild(_player);
            _player.QueueFree();
        }
        _player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(_player);
        _player.InitializeCharacter(CharacterDataLoader.Get("vagabond"));
        _player.IsAIControlled = true;
        _player.SetPhysicsProcess(false);
    }

    private void Check(bool condition, string label)
    {
        _checks++;
        if (condition)
            GD.Print($"[InventoryRegression] OK {label}");
        else
        {
            _failures++;
            GD.PushError($"[InventoryRegression] FAIL {label}");
        }
    }
}
