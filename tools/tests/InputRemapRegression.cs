using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// Remapping sans pertes (plan 26 Q8b), en plusieurs lancements dans un profil temporaire (--phase) : le redémarrage
/// entre deux phases est réel. remap : seule l'interaction change. restart : au redémarrage, WASD, flèches, croix et
/// stick sont là. legacy-write puis legacy-check : un ancien fichier, écrit par le défaut (flèches à la place de WASD)
/// avec un vrai choix sur l'interaction. reset : la réinitialisation rend tout, secondaires comprises. Le lanceur
/// vérifie en plus que la lecture ne réécrit pas le fichier.
/// </summary>
public partial class InputRemapRegression : Node
{
    private const string SettingsPath = "user://input_bindings.cfg";

    private int _checks;
    private string _phase;

    public override void _Ready()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            int index = Array.IndexOf(args, "--phase");
            _phase = index >= 0 ? args[index + 1] : "remap";
            switch (_phase)
            {
                case "remap": Remap(); break;
                case "restart": Restart(); break;
                case "legacy-write": LegacyWrite(); break;
                case "legacy-check": LegacyCheck(); break;
                case "reset": Reset(); break;
                default: throw new InvalidOperationException($"phase inconnue : {_phase}");
            }
            GD.Print($"[InputRemapRegression] RESULT failures=0 checks={_checks} phase={_phase}");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[InputRemapRegression] FAIL {_phase} : {exception}");
            GetTree().Quit(1);
        }
    }

    private void Remap()
    {
        CheckDefaults("move_up", Key.W, Key.Up, JoyButton.DpadUp);
        InputRemapManager.Instance.RemapKey("interact", Key.F);
        Check(Keys("interact") == "F" && JoyButtons("interact") == "A", $"interaction remappée sur F, bouton A gardé ({Keys("interact")})");
        CheckDefaults("move_up", Key.W, Key.Up, JoyButton.DpadUp);
        using ConfigFile saved = new();
        Check(saved.Load(SettingsPath) == Error.Ok && saved.HasSectionKey("interact", "key")
            && !saved.HasSection("move_up") && saved.GetValue("meta", "version", 0).AsInt32() == 2,
            "fichier : seule l'interaction changée est écrite, avec la version");
    }

    private void Restart()
    {
        foreach ((string action, Key primary, Key secondary, JoyButton button) in Movement)
            CheckDefaults(action, primary, secondary, button);
        Check(Keys("interact") == "F", "après redémarrage : interaction sur F, sans E");
    }

    private void LegacyWrite()
    {
        // Ce qu'écrivait l'ancienne sauvegarde après un seul changement (interaction sur F) : la dernière touche
        // et le dernier bouton de chaque action, c'est-à-dire les flèches pour les déplacements.
        using ConfigFile old = new();
        foreach ((string action, _, Key secondary, JoyButton button) in Movement)
        {
            old.SetValue(action, "key", (long)secondary);
            old.SetValue(action, "joy_button", (long)button);
        }
        old.SetValue("interact", "key", (long)Key.F);
        old.SetValue("interact", "joy_button", (long)JoyButton.A);
        Check(old.Save(SettingsPath) == Error.Ok, "ancien fichier écrit");
    }

    private void LegacyCheck()
    {
        foreach ((string action, Key primary, Key secondary, JoyButton button) in Movement)
            CheckDefaults(action, primary, secondary, button);
        Check(Keys("interact") == "F", "ancien fichier : le vrai choix (interaction sur F) est gardé");
    }

    private void Reset()
    {
        InputRemapManager.Instance.RemapKey("move_up", Key.I);
        Check(Keys("move_up") == "I Up", $"déplacement remappé : flèche gardée ({Keys("move_up")})");
        InputRemapManager.Instance.RemapJoyButton("mobility", JoyButton.RightShoulder);
        InputRemapManager.Instance.ResetToDefaults();
        foreach ((string action, Key primary, Key secondary, JoyButton button) in Movement)
            CheckDefaults(action, primary, secondary, button);
        Check(Keys("interact") == "E" && JoyButtons("mobility") == "X", "réinitialisation : interaction sur E, mobilité sur X");
        Check(!FileAccess.FileExists(SettingsPath), "réinitialisation : fichier supprimé");
    }

    private static readonly (string Action, Key Primary, Key Secondary, JoyButton Button)[] Movement =
    {
        ("move_up", Key.W, Key.Up, JoyButton.DpadUp),
        ("move_down", Key.S, Key.Down, JoyButton.DpadDown),
        ("move_left", Key.A, Key.Left, JoyButton.DpadLeft),
        ("move_right", Key.D, Key.Right, JoyButton.DpadRight),
    };

    private void CheckDefaults(string action, Key primary, Key secondary, JoyButton button)
    {
        string expected = $"{OS.GetKeycodeString(primary)} {OS.GetKeycodeString(secondary)}";
        Check(Keys(action) == expected && JoyButtons(action) == InputRemapManager.JoyButtonLabel(button) && HasStick(action),
            $"{action} : {Keys(action)} · {JoyButtons(action)} · stick");
    }

    private static string Keys(string action)
    {
        List<string> keys = new();
        foreach (InputEvent ev in InputMap.ActionGetEvents(action))
        {
            if (ev is InputEventKey key)
                keys.Add(OS.GetKeycodeString(key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode));
        }
        return string.Join(" ", keys);
    }

    private static string JoyButtons(string action)
    {
        List<string> buttons = new();
        foreach (InputEvent ev in InputMap.ActionGetEvents(action))
        {
            if (ev is InputEventJoypadButton joy)
                buttons.Add(InputRemapManager.JoyButtonLabel(joy.ButtonIndex));
        }
        return string.Join(" ", buttons);
    }

    private static bool HasStick(string action)
    {
        foreach (InputEvent ev in InputMap.ActionGetEvents(action))
        {
            if (ev is InputEventJoypadMotion)
                return true;
        }
        return false;
    }

    private void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        _checks++;
        GD.Print($"[InputRemapRegression] OK {message}");
    }
}
