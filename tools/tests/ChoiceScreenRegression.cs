using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.UI;

namespace Vestiges.Tests;

/// <summary>Entrée interrompue, relance et validation des écrans Mémorial/Faille, sans fenêtre.</summary>
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

    private T Field<T>(string name) => (T)typeof(ChoiceScreen).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_screen)!;

    private void Check(bool condition, string message)
    {
        _checks++;
        if (!condition) _failures++;
        GD.Print($"[ChoiceScreenRegression] {(condition ? "PASS" : "FAIL")} {message}");
    }
}
