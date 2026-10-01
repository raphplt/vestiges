using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.UI;

namespace Vestiges.Tests;

/// <summary>
/// Entrée interrompue, relance et validation des écrans Mémorial/Faille, sans fenêtre ; réveil du lieu qui les précède
/// (<see cref="LandmarkReveal"/>) : pause, avancement, passage d'un appui, caméra rendue au joueur.
/// </summary>
public partial class ChoiceScreenRegression : Node
{
    private int _checks;
    private int _failures;
    private ChoiceScreen _screen;
    private readonly List<ChoiceCard> _cards = new()
    {
        new ChoiceCard { Title = "Indisponible", Enabled = false },
        new ChoiceCard { Title = "Bénédiction", Enabled = true },
    };

    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            _screen = new ChoiceScreen();
            AddChild(_screen);
            CheckReopen();
            CheckValidation();
            await CheckPause();
            await CheckReveal();
            GD.Print($"[ChoiceScreenRegression] RESULT checks={_checks} failures={_failures}");
            GetTree().Paused = false;
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError($"[ChoiceScreenRegression] {exception}");
            GetTree().Paused = false;
            GetTree().Quit(2);
        }
    }

    private void CheckReopen()
    {
        int calls = 0;
        _screen.Open("Mémorial", "Avant", _cards, "Sortir", _ => calls++);
        Tween previous = Field<Tween>("_entrance");
        previous.CustomStep(0.1);
        _screen.Open("Services", "Après", _cards, "Sortir", _ => calls++, animate: false);
        Check(!previous.IsRunning(), "l'ancienne entrée est arrêtée à la réouverture");
        Check(Field<Label>("_title").VisibleRatio == 1f, "le titre réouvert est complet");
        Check(Field<Label>("_subtitle").Modulate.A == 1f && Field<HBoxContainer>("_actions").Modulate.A == 1f,
            "sous-titre et sortie réouverts sont visibles");
        Check(Field<ColorRect>("_overlay").Color == ChoiceStyle.OverlayColor,
            "pas d'éclair figé dans la nouvelle offre");
        Check(Mathf.IsEqualApprox(Field<PixelBackdrop>("_backdrop").Modulate.A, 0.75f),
            "le fond réouvert est à son opacité finale");
        Check(Field<VBoxContainer>("_cardsContainer").GetChildCount() == _cards.Count
            && Field<HBoxContainer>("_actions").GetChildCount() == 1, "pas d'anciennes cartes dans le layout de la relance");
        _screen.Activate(1);
        Check(calls == 1 && !_screen.IsOpen && !GetTree().Paused, "la nouvelle offre seule attribue son choix");
    }

    private void CheckValidation()
    {
        foreach (InputEvent input in new InputEvent[]
        {
            new InputEventKey { Keycode = Key.Enter, Pressed = true },
            new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = true },
            new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true },
        })
        {
            int calls = 0;
            _screen.Open("Mémorial", "", _cards, null, _ => calls++);
            _screen._Input(input);
            Check(_screen.IsOpen && calls == 0 && Field<Label>("_title").VisibleRatio == 1f,
                $"{input.GetType().Name} : le premier appui passe l'entrée sans récompense");
            Check(Mathf.IsEqualApprox(Field<PixelBackdrop>("_backdrop").Modulate.A, 0.75f),
                "passer l'entrée termine aussi le fondu du fond");
            _screen.Activate(1);
            _screen.Activate(1);
            Check(calls == 1 && !_screen.IsOpen, "validation suivante unique, même après un second appel");
            input.Dispose();
        }

        int directCalls = 0;
        _screen.Open("Mémorial", "", _cards, null, _ => directCalls++);
        _screen.Activate(1);
        Check(directCalls == 0 && _screen.IsOpen, "validation directe/bot : même garde que l'input");
        _screen.Activate(0);
        Check(directCalls == 0 && _screen.IsOpen, "une carte désactivée ne ferme pas l'offre");
        _screen._Input(new InputEventAction { Action = "ui_cancel", Pressed = true });
        Check(_screen.IsOpen && directCalls == 0, "une bénédiction ne peut pas être annulée");
        _screen.Activate(1);

        int cancelled = 0;
        _screen.Open("Faille", "", _cards, "Refuser", choice => cancelled += choice == -1 ? 1 : 100, animate: false);
        _screen._Input(new InputEventAction { Action = "ui_cancel", Pressed = true });
        Check(cancelled == 1 && !_screen.IsOpen && !GetTree().Paused, "refus de Faille unique, puis reprise");

        int chained = 0;
        _screen.Open("Services", "", _cards, "Sortir", _ =>
        {
            chained++;
            _screen.Open("Services après achat", "", _cards, "Sortir", _ => chained++, animate: false);
        }, animate: false);
        _screen.Activate(1);
        Check(chained == 1 && _screen.IsOpen && GetTree().Paused, "achat : la nouvelle offre garde la pause");
        _screen.Activate(int.MaxValue);
        Check(chained == 2 && !GetTree().Paused, "sortie de l'offre rouverte sans second achat");
    }

    private async Task CheckPause()
    {
        Node2D world = new() { ProcessMode = ProcessModeEnum.Pausable };
        AddChild(world);
        world.CreateTween().TweenProperty(world, "position:x", 100f, 1f);
        _screen.Open("Mémorial", "", _cards, null, _ => { }, animate: false);
        for (int i = 0; i < 8; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(world.Position == Vector2.Zero && GetTree().Paused, "le monde reste arrêté pendant l'offre");
        _screen.Activate(1);
        for (int i = 0; i < 8; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(world.Position.X > 0f && !GetTree().Paused, "le monde reprend après le choix");
        world.QueueFree();
    }

    private async Task CheckReveal()
    {
        Node2D holder = new() { ProcessMode = ProcessModeEnum.Pausable };
        Camera2D camera = new();
        holder.AddChild(camera);
        AddChild(holder);
        LandmarkReveal reveal = new();
        reveal.Setup(camera);
        AddChild(reveal);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        List<float> progress = new();
        List<bool> finished = new();
        reveal.Play(new Vector2(300f, -120f), Colors.Cyan, 0.3f, progress.Add, finished.Add);
        Check(reveal.IsPlaying && GetTree().Paused, "réveil : la run se fige dès le départ");
        reveal._Input(new InputEventAction { Action = "ui_left", Pressed = true });
        Check(reveal.IsPlaying && finished.Count == 0, "réveil : une direction ne le passe pas");
        await RealSeconds(0.5f);
        bool ordered = true;
        for (int i = 1; i < progress.Count; i++)
            ordered &= progress[i] >= progress[i - 1];
        Check(finished.Count == 1 && !finished[0] && progress.Count > 2 && ordered && progress[^1] == 1f,
            "réveil : avancement croissant jusqu'à 1, une seule fin, non passée");
        Check(GetTree().Paused && camera.Offset.DistanceTo(new Vector2(300f, -120f)) < 1f,
            "réveil : la caméra reste sur le lieu, run figée pour l'écran qui suit");

        GetTree().Paused = false;
        await RealSeconds(0.6f);
        Check(camera.Offset == Vector2.Zero && camera.ProcessMode == ProcessModeEnum.Inherit,
            "réveil : la caméra revient au joueur une fois la run relancée");

        progress.Clear();
        finished.Clear();
        reveal.Play(new Vector2(-80f, 40f), Colors.Violet, 1f, progress.Add, finished.Add);
        reveal._Input(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = true });
        Check(!reveal.IsPlaying && finished.Count == 1 && finished[0] && progress[^1] == 1f,
            "réveil : un appui le passe, le lieu atteint son état final");
        await RealSeconds(0.4f);
        Check(finished.Count == 1 && GetTree().Paused, "réveil passé : aucune seconde fin, run toujours figée");
        GetTree().Paused = false;
        reveal.QueueFree();
        holder.QueueFree();
    }

    /// <summary>Le réveil se compte en secondes réelles ; en --fixed-fps, les minuteries du moteur vont plus vite.</summary>
    private async Task RealSeconds(float seconds)
    {
        ulong end = Time.GetTicksMsec() + (ulong)(seconds * 1000f);
        while (Time.GetTicksMsec() < end)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private T Field<T>(string name) => (T)typeof(ChoiceScreen).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_screen)!;

    private void Check(bool condition, string message)
    {
        _checks++;
        if (!condition) _failures++;
        GD.Print($"[ChoiceScreenRegression] {(condition ? "PASS" : "FAIL")} {message}");
    }
}
