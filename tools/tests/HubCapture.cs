using System;
using System.Globalization;
using System.Threading.Tasks;
using Godot;

namespace Vestiges.Tests;

/// <summary>
/// Captures de l'écran d'accueil rendu : état initial, puis une capture par action rejouée.
/// --output DIR, --actions ui_right,ui_accept (actions d'input envoyées une à une, capture après chacune).
/// </summary>
public partial class HubCapture : Node
{
    private string _output;

    public override async void _Ready()
    {
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            string[] args = OS.GetCmdlineUserArgs();
            _output = Argument(args, "--output", "/tmp/vestiges-hub");
            DirAccess.MakeDirRecursiveAbsolute(_output);

            GetTree().CurrentScene = null;
            Node hub = GD.Load<PackedScene>("res://scenes/Hub.tscn").Instantiate();
            GetTree().Root.AddChild(hub);
            GetTree().CurrentScene = hub;
            await Frames(150);
            Capture("hub-00");

            string actions = Argument(args, "--actions", "");
            string[] steps = actions.Length > 0 ? actions.Split(',') : Array.Empty<string>();
            for (int index = 0; index < steps.Length; index++)
            {
                Press(steps[index]);
                await Frames(45);
                Capture($"hub-{index + 1:00}-{steps[index]}");
            }
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[HubCapture] {exception}");
            GetTree().Quit(1);
        }
    }

    private static void Press(string action)
    {
        InputEventAction down = new() { Action = action, Pressed = true };
        Input.ParseInputEvent(down);
        InputEventAction up = new() { Action = action, Pressed = false };
        Input.ParseInputEvent(up);
    }

    private void Capture(string name)
    {
        using Image image = GetViewport().GetTexture().GetImage();
        string path = $"{_output}/{name}.png";
        image.SavePng(path);
        GD.Print($"[HubCapture] {path}");
    }

    private async Task Frames(int count)
    {
        for (int frame = 0; frame < count; frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static string Argument(string[] args, string name, string fallback)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
    }
}
