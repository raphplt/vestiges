using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

namespace Vestiges.Tests;

/// <summary>
/// Captures de l'écran d'accueil rendu : état initial, puis une capture par action rejouée.
/// --output DIR, --actions ui_right,ui_accept (actions d'input envoyées une à une, capture après chacune).
/// --record N : après la dernière action, enregistre N frames d'affilée en vignettes (départ en run, chargement),
/// avec le temps écoulé et la scène courante de chaque frame.
/// --collection-focus ID : arrivée depuis le bilan, « Voir dans la Collection » sur l'arme ID.
/// --meta-fixture TEXTE : contenu déposé dans la sauvegarde méta avant l'accueil (avis de sauvegarde, plan 26 Q2b).
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
            string focus = Argument(args, "--collection-focus", "");
            if (focus.Length > 0)
                GetNode<Vestiges.Core.GameManager>("/root/GameManager").CollectionFocusWeaponId = focus;

            string fixture = Argument(args, "--meta-fixture", "");
            if (fixture.Length > 0)
            {
                System.IO.File.WriteAllText(ProjectSettings.GlobalizePath(Vestiges.Infrastructure.DevelopmentMode.GetSavePath("meta_save.json")), fixture);
                Vestiges.Infrastructure.MetaSaveManager.ReloadProfile();
            }

            GetTree().CurrentScene = null;
            Node hub = GD.Load<PackedScene>("res://scenes/Hub.tscn").Instantiate();
            GetTree().Root.AddChild(hub);
            GetTree().CurrentScene = hub;
            await Frames(150);
            string tab = Argument(args, "--collection-tab", "");
            if (tab.Length > 0)
            {
                typeof(Vestiges.UI.HubCollectionPanel).GetField("_tab", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, tab);
                hub.GetType().GetMethod("OpenCollection", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(hub, null);
                await Frames(30);
                Vestiges.UI.HubCollectionPanel collection = (Vestiges.UI.HubCollectionPanel)hub.GetType()
                    .GetField("_collectionPanel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(hub);
                GridContainer grid = (GridContainer)typeof(Vestiges.UI.HubCollectionPanel)
                    .GetField("_grid", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(collection);
                int index = int.Parse(Argument(args, "--collection-index", "0"), CultureInfo.InvariantCulture);
                ((Control)grid.GetChild(Mathf.Clamp(index, 0, grid.GetChildCount() - 1))).GrabFocus();
                await Frames(20);
            }
            Capture("hub-00");

            string actions = Argument(args, "--actions", "");
            string[] steps = actions.Length > 0 ? actions.Split(',') : Array.Empty<string>();
            for (int index = 0; index < steps.Length; index++)
            {
                Press(steps[index]);
                await Frames(45);
                Capture($"hub-{index + 1:00}-{steps[index]}");
            }
            int record = int.Parse(Argument(args, "--record", "0"), CultureInfo.InvariantCulture);
            if (record > 0)
                await Record(record);
            await Vestiges.Core.GameExit.QuitAsync(GetTree());
        }
        catch (Exception exception)
        {
            GD.PushError($"[HubCapture] {exception}");
            GetTree().Quit(1);
        }
    }

    /// <summary>Une vignette par frame : ce que le joueur voit pendant la transition et le chargement.</summary>
    private async Task Record(int frames)
    {
        ulong start = Time.GetTicksMsec();
        for (int frame = 0; frame < frames; frame++)
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using Image image = GetViewport().GetTexture().GetImage();
            image.Resize(480, 270, Image.Interpolation.Bilinear);
            image.SavePng($"{_output}/rec-{frame:0000}.png");
            string scene = GetTree().CurrentScene?.Name ?? "-";
            GD.Print($"[HubCapture] frame {frame} t={Time.GetTicksMsec() - start} ms scene={scene}");
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
