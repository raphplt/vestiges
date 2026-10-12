using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.World;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// --capture-exchange : sac rempli, un objet tombe aux pieds du joueur ; captures de l'objet au sol, de l'invite, puis de
/// l'écran d'échange ouvert (plan 35 I3). Rapport de mesure : niveaux d'Écussons gagnés, dont ceux du fil d'usage (I7).
/// </summary>
public partial class RunObservation
{
    private async Task CaptureExchange()
    {
        await PrepareCloseUpScene();
        foreach (PassiveSouvenirData item in PassiveSouvenirDataLoader.GetAll())
            if (!item.IsBadge && _player.HasBagRoom)
                _player.AddItemCopy(item.Id);
        await Frames(5);
        SaveFrame("exchange-0-bag");
        PassiveSouvenirData absent = PassiveSouvenirDataLoader.GetAll().Find(item => !item.IsBadge && _player.FindObject(item.Id) == null);
        ItemPickup pickup = _world.GetNode<ItemDropDirector>("ItemDropDirector").Drop(_player.GlobalPosition + new Vector2(30f, 10f), absent);
        await Frames(40);
        SaveFrame("exchange-1-ground");
        pickup.Interact(_player);
        // La cadence peut dépasser 60 FPS : attendre la durée de révélation, pas un nombre d'images.
        await ToSignal(GetTree().CreateTimer(2.5, processAlways: true), SceneTreeTimer.SignalName.Timeout);
        await Frames(2);
        SaveFrame("exchange-2-screen");
        GD.Print($"[RunObservation] RESULT exchange objet={pickup.ItemId} sac={_player.BagCount}/{_player.BagCapacity} dossier={_output}");
    }

    /// <summary>Vraie run : dix types et plus de dix copies, doublon ramassé à sac plein, puis vrai coffre payé.</summary>
    private async Task CaptureItemStacks()
    {
        ProcessMode = ProcessModeEnum.Always;
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node enemy in GetTree().GetNodesInGroup("enemies"))
            enemy.QueueFree();
        _player.AIInputOverride = Vector2.Zero;
        _player.SetBagCapacity(InventoryConfig.BagSize);
        (string Id, int Copies)[] stacks =
        {
            ("boite_de_pansements", 8), ("semelle_usee", 7), ("allumette_humide", 12), ("thermos", 4),
            ("flacon_de_pharmacie", 5), ("de_a_coudre", 3), ("petard_mouille", 2),
            ("loupe_de_philateliste", 3), ("ticket_de_consigne", 2), ("trefle_plastifie", 1),
        };
        foreach ((string id, int copies) in stacks)
            for (int i = 0; i < copies; i++)
                if (!_player.AddItemCopy(id))
                    throw new System.InvalidOperationException($"Pile de capture refusée : {id} copie {i + 1}");
        await Frames(5);
        SaveFrame("items-hud-stacks");
        int before = _player.FindObject("allumette_humide").Copies;
        ItemPickup duplicate = _world.GetNode<ItemDropDirector>("ItemDropDirector").Drop(
            _player.GlobalPosition + new Vector2(40, 0), PassiveSouvenirDataLoader.Get("allumette_humide"));
        await Frames(2);
        duplicate.Interact(_player);
        await ToSignal(GetTree().CreateTimer(1, processAlways: true), SceneTreeTimer.SignalName.Timeout);
        if (_player.BagCount != stacks.Length || _player.FindObject("allumette_humide").Copies != before + 1
            || GodotObject.IsInstanceValid(duplicate) || GetTree().Paused)
            throw new System.InvalidOperationException("Le doublon n'a pas rejoint sa pile à sac plein.");
        SaveFrame("items-hud-after-duplicate");
        await CaptureLootScreen(keepCapacity: true, report: false);
        // Les nouveaux types sont déposés par CallDeferred : les rattacher avant de fermer la scène du banc.
        await Frames(3);
        GD.Print($"[RunObservation] RESULT item_stacks types={_player.BagCount}/{_player.BagCapacity} copies={_player.BagCopyCount} duplicate={before}->{before + 1} dossier={_output}");
    }

    /// <summary>
    /// Écussons (plan 35 I7) : niveaux gagnés depuis le niveau 1, dont ceux du fil d'usage, et le détail par Écusson
    /// (niveau atteint/niveaux par l'usage).
    /// </summary>
    private void AppendBadgeUsage(StringBuilder summary)
    {
        int gained = 0;
        List<string> detail = new();
        foreach (ActivePassiveSouvenir badge in _player.BadgeSlots)
        {
            gained += badge.Level - 1;
            detail.Add($"{badge.Id}:{badge.Level}/{_player.BadgeUsage?.UsageLevelsOf(badge.Id) ?? 0}");
        }
        summary.Append(CultureInfo.InvariantCulture,
            $" badge_levels={gained} badge_usage_levels={_player.BadgeUsage?.UsageLevels ?? 0} badges={string.Join(",", detail)}");
        if (_player.BadgeUsage == null)
            return;
        List<string> totals = new();
        foreach (Vestiges.Progression.UsageTrigger trigger in System.Enum.GetValues<Vestiges.Progression.UsageTrigger>())
            totals.Add(string.Create(CultureInfo.InvariantCulture, $"{trigger}:{_player.BadgeUsage.TotalOf(trigger):F0}"));
        summary.Append(" usage_totals=").Append(string.Join(",", totals));
    }
}
