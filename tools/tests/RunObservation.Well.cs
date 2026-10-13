using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.UI;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-well : le Puits le plus proche du départ (plan 44), dans le monde puis son écran ; un Écusson oublié, la liste
/// rouverte au nouveau prix, puis la trace sur l'Écusson suivant. RESULT : Puits placés, Écusson oublié, niveau de la trace.
/// </summary>
public partial class RunObservation
{
    private async Task CaptureWell()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Enemy existing && existing.IsActive)
                _world.GetNode<Vestiges.Spawn.EnemyPool>("EnemyPool").Return(existing);
        _player.AIInputOverride = Vector2.Zero;
        ProcessMode = ProcessModeEnum.Always;
        await ToSignal(GetTree().CreateTimer(3.5, processAlways: true), SceneTreeTimer.SignalName.Timeout);

        List<Well> wells = new(Well.All);
        Vector2 spawn = _player.GlobalPosition;
        wells.Sort((a, b) => a.GlobalPosition.DistanceSquaredTo(spawn).CompareTo(b.GlobalPosition.DistanceSquaredTo(spawn)));
        if (wells.Count == 0)
        {
            GD.PushError("[RunObservation] Aucun Puits placé");
            return;
        }
        Well well = wells[0];
        _player.GlobalPosition = well.GlobalPosition + new Vector2(-44f, 8f);
        _camera.ResetSmoothing();
        await Frames(25);
        SaveCrop("well-0-world", well.GlobalPosition + new Vector2(0f, -40f), new Vector2(240f, 135f));
        SaveFrame("well-0-frame");

        // De quoi oublier : une seconde arme, un Écusson monté, deux objets, l'Essence de deux oublis.
        _player.AddWeapon(WeaponDataLoader.Get(_player.GetWeaponFragmentLevel("whip") > 0 ? "music_box" : "whip"));
        _player.AddOrUpgradePassive("instinct", 24);
        _player.AddItemCopy("glacon");
        _player.AddItemCopy("glacon");
        _player.AddItemCopy("thermos");
        _world.GetNode<EssenceTracker>("EssenceTracker").AddEssence(300);
        ChoiceScreen choices = _world.GetNode<ChoiceScreen>("ChoiceScreen");
        well.Interact(_player);
        await ToSignal(GetTree().CreateTimer(2.5, processAlways: true), SceneTreeTimer.SignalName.Timeout);
        bool opened = choices.IsOpen;
        SaveFrame("well-1-screen");
        // Un appui pendant l'entrée ne fait que la terminer : on la finit avant de choisir.
        if (choices.IsEntering)
            choices.Activate(0);
        int badgeLevel = _player.FindObject("instinct")?.Level ?? 0;
        choices.Activate(_player.WeaponSlots.Count);
        await Frames(10);
        bool forgotten = _player.FindObject("instinct") == null;
        SaveFrame("well-2-reopened");
        choices.Activate(int.MaxValue);
        _player.AddOrUpgradePassive("peau_dure");
        int traceLevel = _player.FindObject("peau_dure")?.Level ?? 0;
        await Frames(15);
        SaveFrame("well-3-trace");
        GD.Print(string.Create(CultureInfo.InvariantCulture,
            $"[RunObservation] RESULT well count={wells.Count} opened={opened} badge_forgotten={forgotten} badge_level={badgeLevel} trace_level={traceLevel} waymarks={_player.Waymarks.Found} dossier={_output}"));
    }
}
