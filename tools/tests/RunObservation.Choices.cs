using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.UI;

namespace Vestiges.Tests;

/// <summary>--capture-choices : reproduit une relance pendant l'entrée dans la vraie scène Main.</summary>
public partial class RunObservation
{
    private async Task CaptureChoiceReopen()
    {
        ProcessMode = ProcessModeEnum.Always;
        _player.AIInputOverride = Vector2.Zero;
        while (_world.GetNodeOrNull("GameLoadingOverlay") != null)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        ChoiceScreen choices = _world.GetNode<ChoiceScreen>("ChoiceScreen");
        List<ChoiceCard> cards = new();
        foreach (StatEffectData blessing in BlessingDataLoader.All)
        {
            ChoiceCard card = new() { Title = Tr(blessing.NameKey), Frame = RarityPalette.Main("memorial"), Tag = Tr("RARITY_UNCOMMON"), Rank = 1 };
            card.Lines.Add((Vestiges.Progression.StatModifier.Scaled(blessing.Stat, blessing.ModifierType, blessing.Amount, 1f).Describe(), ChoiceStyle.GainColor));
            cards.Add(card);
            if (cards.Count == 3) break;
        }
        int selected = 0;
        double savedScale = Engine.TimeScale;
        try
        {
            // Geler les animations pendant la lecture GPU/écriture PNG : même instant avant et après correctif.
            Engine.TimeScale = 0.0001;
            choices.Open(Tr("MEMORIAL_AWAKE_TITLE"), Tr("MEMORIAL_AWAKE_SUBTITLE"), cards, Tr("MEMORIAL_LEAVE"), _ => selected++, PixelBackdrop.MemorialTint);
            Tween entrance = (Tween)typeof(ChoiceScreen).GetField("_entrance", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(choices)!;
            entrance.CustomStep(0.1);
            choices.Open(Tr("MEMORIAL_AWAKE_TITLE"), Tr("MEMORIAL_AWAKE_SUBTITLE"), cards, Tr("MEMORIAL_LEAVE"), _ => selected++, PixelBackdrop.MemorialTint, false);
            await Frames(3);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            SaveFrame("choices-01-reopened");
            choices.Activate(0);

            choices.Open(Tr("MEMORIAL_AWAKE_TITLE"), Tr("MEMORIAL_AWAKE_SUBTITLE"), cards, null, _ => selected++, PixelBackdrop.MemorialTint);
            choices._Input(new InputEventKey { Keycode = Key.Enter, Pressed = true });
            await Frames(3);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            SaveFrame("choices-02-skipped");
            choices.Activate(0);
        }
        finally
        {
            Engine.TimeScale = savedScale;
        }
        GD.Print($"[RunObservation] RESULT choices selected={selected} open={choices.IsOpen} paused={GetTree().Paused}");
    }
}
