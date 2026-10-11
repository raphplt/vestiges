using System;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// Inventaire du joueur (plan 35), dans un profil neuf et isolé. Lot I1 : réglages contrôlés, Écussons à part des objets,
/// rendement au-delà du seuil (aperçu de carte et valeur réelle identiques), déblocage d'un Écusson par sa quête.
/// </summary>
public partial class InventoryRegression : Node
{
    private const string ConfigPath = "res://data/progression/inventory.json";

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
