using System;
using Godot;
using Vestiges.Core;
using Vestiges.UI;

namespace Vestiges.Tests;

/// <summary>Boote le vrai Hub et confirme sa disponibilité après le nombre d'images demandé.</summary>
public partial class HubSmokeRegression : Node
{
    public override async void _Ready()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            int frames = int.Parse(args[Array.IndexOf(args, "--frames") + 1]);
            for (int frame = 0; frame < frames; frame++)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            HubScreen hub = GetParent<HubScreen>();
            GameManager manager = GetNode<GameManager>("/root/GameManager");
            bool hasUsableMenu = false;
            if (hub.FindChild("Menu", true, false) is VBoxContainer menu)
            {
                foreach (Node node in menu.GetChildren())
                {
                    if (node is HubMenuButton button && button.IsVisibleInTree() && !button.Disabled)
                    {
                        hasUsableMenu = true;
                        break;
                    }
                }
            }
            if (!hub.IsInsideTree() || !hasUsableMenu || GetTree().Paused
                || manager.CurrentState != GameManager.GameState.Hub)
                throw new InvalidOperationException("Le Hub n'est pas disponible après son démarrage.");
            GD.Print($"[HubSmokeRegression] RESULT failures=0 frames={frames}");
            await GameExit.QuitAsync(GetTree(), 0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[HubSmokeRegression] {exception}");
            GetTree().Quit(1);
        }
    }
}
