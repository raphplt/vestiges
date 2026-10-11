using System.Threading.Tasks;
using Godot;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-exchange : sac rempli, un objet tombe aux pieds du joueur ; captures de l'objet au sol, de l'invite, puis de
/// l'écran d'échange ouvert (plan 35 I3).
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
}
