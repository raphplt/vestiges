using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.UI;

namespace Vestiges.Tests;

/// <summary>Lecture du journal et retour dans Main, par les entrées du moteur.</summary>
public partial class RunObservation
{
    private async Task CaptureJournal()
    {
        ProcessMode = ProcessModeEnum.Always;
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        _player.AIInputOverride = Vector2.Zero;
        await Seconds(3.5);
        JournalScreen journal = _world.GetNode<JournalScreen>("JournalScreen");
        journal.Show();
        await Frames(5);
        SaveFrame("journal-ouverture");
        AssertMenuFits((Control)typeof(JournalScreen).GetField("_root", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(journal), "journal");
        int index = 0;
        foreach (ConstellationData constellation in SouvenirDataLoader.GetAllConstellations())
        {
            typeof(JournalScreen).GetMethod("SelectConstellation", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(journal, new object[] { constellation.Id });
            await Frames(3);
            SaveFrame($"journal-constellation-{index++}");
        }
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.B, Pressed = true });
        await Frames(3);
        if (journal.IsOpen || GetTree().Paused)
            throw new System.InvalidOperationException("Le journal doit reprendre la run avec B.");
        GetViewport().PushInput(new InputEventAction { Action = "journal", Pressed = true });
        await Frames(3);
        if (!journal.IsOpen || !GetTree().Paused)
            throw new System.InvalidOperationException("J doit rouvrir le journal.");
        SaveFrame("journal-reouverture");
        GetViewport().PushInput(new InputEventAction { Action = "journal", Pressed = true });
        await Frames(3);
        if (journal.IsOpen || GetTree().Paused)
            throw new System.InvalidOperationException("J doit refermer le journal sans ouvrir la pause.");
        GD.Print($"[RunObservation] RESULT journal_captured=True constellations={index}");
    }
    private void AssertMenuFits(Control control, string name)
    {
        Rect2 viewport = GetViewport().GetVisibleRect().Grow(2);
        Rect2 rect = control.GetGlobalRect();
        if (!viewport.Encloses(rect))
            throw new System.InvalidOperationException($"{name} dépasse l'écran : {rect}, viewport {viewport}");
        GD.Print($"[RunObservation] MENU_BOUNDS {name} {rect}");
    }

}
