using Godot;
using Vestiges.Core;

namespace Vestiges.Tests;

/// <summary>
/// Captures de la run 3D (plan 60, 60.1c et 60.2a), lancée comme depuis le Hub : le personnage de <c>--character</c>
/// marche, saute, use de sa capacité, puis la caméra tourne et plonge, une image à chaque étape ; la pause ; enfin la
/// horde arrivée sur le joueur, qui ne meurt pas, après <c>--horde-seconds</c>, avec les images par seconde de cette
/// attente. Arguments après -- : --capture &lt;dossier&gt; [--character &lt;id&gt;] [--crowd N] [--horde-seconds S].
/// Les entrées passent par les actions du jeu, comme au clavier.
/// </summary>
public partial class Run3DCapture : Node
{
    private string _dir;
    private float _time;
    private int _step;
    private float _hordeSeconds;
    private int _frames;
    private Vestiges.World3D.Run3D _run;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        string[] args = OS.GetCmdlineUserArgs();
        _dir = Argument(args, "--capture", "user://run3d_capture");
        GameManager gameManager = GetNode<GameManager>("/root/GameManager");
        gameManager.SelectedCharacterId = Argument(args, "--character", "facteur");
        gameManager.ChangeState(GameManager.GameState.Run);
        DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
        _hordeSeconds = float.Parse(Argument(args, "--horde-seconds", "20"), System.Globalization.CultureInfo.InvariantCulture);
        _run = GD.Load<PackedScene>(GameManager.RunScenePath).Instantiate<Vestiges.World3D.Run3D>();
        _run.Undying = true;
        _run.CrowdTarget = int.Parse(Argument(args, "--crowd", "-1"), System.Globalization.CultureInfo.InvariantCulture);
        AddChild(_run);
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        switch (_step)
        {
            case 0 when _time > 2.5f:
                Save("depart");
                Input.ActionPress("move_up");
                Next();
                break;
            case 1 when _time > 1.2f:
                Save("marche");
                Input.ActionPress("jump");
                Next();
                break;
            case 2 when _time > 0.25f:
                Input.ActionRelease("jump");
                Save("saut");
                Next();
                break;
            case 3 when _time > 0.8f:
                Input.ActionPress("mobility");
                Next();
                break;
            case 4 when _time > 0.15f:
                Input.ActionRelease("mobility");
                Save("capacite");
                Input.ActionRelease("move_up");
                Turn(new Vector2(600f, 0f));
                Next();
                break;
            case 5 when _time > 0.8f:
                Save("camera_tournee");
                Turn(new Vector2(0f, 220f));
                Next();
                break;
            case 6 when _time > 0.8f:
                Save("camera_plongee");
                Input.ParseInputEvent(new InputEventAction { Action = "ui_cancel", Pressed = true });
                Next();
                break;
            case 7 when _time > 0.6f:
                Save("pause");
                Input.ParseInputEvent(new InputEventAction { Action = "ui_cancel", Pressed = true });
                Next();
                break;
            case 8:
                _frames++;
                if (_time < _hordeSeconds)
                    break;
                Save("horde");
                GD.Print($"[Run3DCapture] Horde : {_run.Crowd.Present} créatures, {_run.Crowd.Kills} éliminations, {_run.Crowd.ContactHits} coups au contact, "
                    + $"{_frames / _time:0} images par seconde sur {_time:0} s");
                GD.Print($"[Run3DCapture] Fini : souris {Input.MouseMode}");
                GetTree().Quit();
                Next();
                break;
        }
    }

    private static void Turn(Vector2 relative) => Input.ParseInputEvent(new InputEventMouseMotion { Relative = relative });

    private void Next()
    {
        _step++;
        _time = 0f;
    }

    private void Save(string name)
    {
        DirAccess.MakeDirRecursiveAbsolute(_dir);
        string path = $"{_dir}/{name}.png";
        GetViewport().GetTexture().GetImage().SavePng(path);
        GD.Print($"[Run3DCapture] {path} (souris {Input.MouseMode})");
    }

    private static string Argument(string[] args, string name, string fallback)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
                return args[i + 1];
        }
        return fallback;
    }
}
