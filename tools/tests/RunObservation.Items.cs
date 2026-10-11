using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.World;

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
        foreach (string id in new[] { "glacon", "allumette_humide", "de_a_coudre", "semelle_usee", "chewing_gum" })
            while (_player.HasBagRoom && _player.AddItemCopy(id)) { }
        await Frames(5);
        SaveFrame("exchange-0-bag");
        ItemPickup pickup = _world.GetNode<ItemDropDirector>("ItemDropDirector").Drop(_player.GlobalPosition + new Vector2(30f, 10f));
        await Frames(40);
        SaveFrame("exchange-1-ground");
        pickup.Interact(_player);
        await Frames(90);
        SaveFrame("exchange-2-screen");
        GD.Print($"[RunObservation] RESULT exchange objet={pickup.ItemId} sac={_player.BagCount}/{_player.BagCapacity} dossier={_output}");
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
