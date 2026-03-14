using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Gestionnaire de remapping des touches. Autoload.
/// Supporte clavier + manette. Persiste les bindings en user://input_bindings.cfg.
/// </summary>
public partial class InputRemapManager : Node
{
	public static InputRemapManager Instance { get; private set; }

	private const string SettingsPath = "user://input_bindings.cfg";

	/// <summary>Actions remappables avec leur description et leur binding par défaut.</summary>
	public static readonly ActionDef[] RemappableActions = new[]
	{
		new ActionDef("move_up", "UI_MOVE_UP", Key.W, JoyButton.DpadUp),
		new ActionDef("move_down", "UI_MOVE_DOWN", Key.S, JoyButton.DpadDown),
		new ActionDef("move_left", "UI_MOVE_LEFT", Key.A, JoyButton.DpadLeft),
		new ActionDef("move_right", "UI_MOVE_RIGHT", Key.D, JoyButton.DpadRight),
		new ActionDef("interact", "UI_INTERACT", Key.E, JoyButton.A),
		new ActionDef("journal", "UI_JOURNAL", Key.J, JoyButton.Back),
	};

	/// <summary>true si une manette est connectée.</summary>
	public bool GamepadConnected { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
		Input.JoyConnectionChanged += OnJoyConnectionChanged;
		GamepadConnected = Input.GetConnectedJoypads().Count > 0;
	}

	public override void _Ready()
	{
		EnsureGamepadDefaults();
		LoadBindings();
	}

	public override void _ExitTree()
	{
		Input.JoyConnectionChanged -= OnJoyConnectionChanged;
		Instance = null;
	}

	/// <summary>Remplace le binding clavier d'une action.</summary>
	public void RemapKey(string action, Key newKey)
	{
		// Supprimer les événements clavier existants
		foreach (InputEvent ev in InputMap.ActionGetEvents(action))
		{
			if (ev is InputEventKey)
				InputMap.ActionEraseEvent(action, ev);
		}

		// Ajouter le nouveau binding
		InputEventKey keyEvent = new() { PhysicalKeycode = newKey };
		InputMap.ActionAddEvent(action, keyEvent);

		SaveBindings();
		GD.Print($"[InputRemap] {action} → {OS.GetKeycodeString((Key)newKey)}");
	}

	/// <summary>Remplace le binding manette (bouton) d'une action.</summary>
	public void RemapJoyButton(string action, JoyButton newButton)
	{
		foreach (InputEvent ev in InputMap.ActionGetEvents(action))
		{
			if (ev is InputEventJoypadButton)
				InputMap.ActionEraseEvent(action, ev);
		}

		InputEventJoypadButton joyEvent = new() { ButtonIndex = newButton };
		InputMap.ActionAddEvent(action, joyEvent);

		SaveBindings();
		GD.Print($"[InputRemap] {action} → Joy {newButton}");
	}

	/// <summary>Réinitialise tous les bindings aux valeurs par défaut.</summary>
	public void ResetToDefaults()
	{
		foreach (ActionDef def in RemappableActions)
		{
			InputMap.ActionEraseEvents(def.Action);

			InputEventKey keyEvent = new() { PhysicalKeycode = def.DefaultKey };
			InputMap.ActionAddEvent(def.Action, keyEvent);

			InputEventJoypadButton joyEvent = new() { ButtonIndex = def.DefaultJoyButton };
			InputMap.ActionAddEvent(def.Action, joyEvent);
		}

		// Ré-ajouter les axes du stick gauche pour le mouvement
		AddStickDefaults();

		// Supprimer le fichier de config pour revenir aux défauts
		if (FileAccess.FileExists(SettingsPath))
			DirAccess.RemoveAbsolute(SettingsPath);

		GD.Print("[InputRemap] Reset to defaults");
	}

	/// <summary>Retourne le nom lisible du binding clavier actuel d'une action.</summary>
	public static string GetKeyName(string action)
	{
		foreach (InputEvent ev in InputMap.ActionGetEvents(action))
		{
			if (ev is InputEventKey key)
			{
				Key code = key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;
				return OS.GetKeycodeString(code);
			}
		}
		return "???";
	}

	/// <summary>Retourne le nom lisible du binding manette actuel d'une action.</summary>
	public static string GetJoyButtonName(string action)
	{
		foreach (InputEvent ev in InputMap.ActionGetEvents(action))
		{
			if (ev is InputEventJoypadButton joy)
				return JoyButtonLabel(joy.ButtonIndex);
		}
		return "???";
	}

	/// <summary>Ajoute les bindings manette par défaut s'ils n'existent pas déjà.</summary>
	private void EnsureGamepadDefaults()
	{
		foreach (ActionDef def in RemappableActions)
		{
			bool hasJoy = false;
			foreach (InputEvent ev in InputMap.ActionGetEvents(def.Action))
			{
				if (ev is InputEventJoypadButton or InputEventJoypadMotion)
				{
					hasJoy = true;
					break;
				}
			}

			if (!hasJoy)
			{
				InputEventJoypadButton joyEvent = new() { ButtonIndex = def.DefaultJoyButton };
				InputMap.ActionAddEvent(def.Action, joyEvent);
			}
		}

		AddStickDefaults();
	}

	/// <summary>Ajoute les axes du stick gauche pour le mouvement.</summary>
	private static void AddStickDefaults()
	{
		AddAxisIfMissing("move_left", JoyAxis.LeftX, -1f);
		AddAxisIfMissing("move_right", JoyAxis.LeftX, 1f);
		AddAxisIfMissing("move_up", JoyAxis.LeftY, -1f);
		AddAxisIfMissing("move_down", JoyAxis.LeftY, 1f);
	}

	private static void AddAxisIfMissing(string action, JoyAxis axis, float direction)
	{
		foreach (InputEvent ev in InputMap.ActionGetEvents(action))
		{
			if (ev is InputEventJoypadMotion motion && motion.Axis == axis)
				return;
		}

		InputEventJoypadMotion axisEvent = new()
		{
			Axis = axis,
			AxisValue = direction
		};
		InputMap.ActionAddEvent(action, axisEvent);
	}

	private void OnJoyConnectionChanged(long device, bool connected)
	{
		GamepadConnected = Input.GetConnectedJoypads().Count > 0;
		GD.Print($"[InputRemap] Gamepad {(connected ? "connected" : "disconnected")}: {Input.GetJoyName((int)device)}");
	}

	// --- Persistence ---

	private void SaveBindings()
	{
		ConfigFile cfg = new();

		foreach (ActionDef def in RemappableActions)
		{
			foreach (InputEvent ev in InputMap.ActionGetEvents(def.Action))
			{
				if (ev is InputEventKey key)
				{
					Key code = key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;
					cfg.SetValue(def.Action, "key", (long)code);
				}
				else if (ev is InputEventJoypadButton joy)
				{
					cfg.SetValue(def.Action, "joy_button", (long)joy.ButtonIndex);
				}
			}
		}

		cfg.Save(SettingsPath);
	}

	private void LoadBindings()
	{
		ConfigFile cfg = new();
		if (cfg.Load(SettingsPath) != Error.Ok)
			return;

		foreach (ActionDef def in RemappableActions)
		{
			if (!cfg.HasSection(def.Action))
				continue;

			if (cfg.HasSectionKey(def.Action, "key"))
			{
				long keyCode = cfg.GetValue(def.Action, "key").AsInt64();
				RemapKey(def.Action, (Key)keyCode);
			}

			if (cfg.HasSectionKey(def.Action, "joy_button"))
			{
				long joyBtn = cfg.GetValue(def.Action, "joy_button").AsInt64();
				RemapJoyButton(def.Action, (JoyButton)joyBtn);
			}
		}

		GD.Print("[InputRemap] Bindings loaded");
	}

	// --- Helpers ---

	public static string JoyButtonLabel(JoyButton button)
	{
		return button switch
		{
			JoyButton.A => "A",
			JoyButton.B => "B",
			JoyButton.X => "X",
			JoyButton.Y => "Y",
			JoyButton.LeftShoulder => "LB",
			JoyButton.RightShoulder => "RB",
			JoyButton.LeftStick => "L3",
			JoyButton.RightStick => "R3",
			JoyButton.Back => "Select",
			JoyButton.Start => "Start",
			JoyButton.DpadUp => "D-Up",
			JoyButton.DpadDown => "D-Down",
			JoyButton.DpadLeft => "D-Left",
			JoyButton.DpadRight => "D-Right",
			_ => button.ToString()
		};
	}

	public record ActionDef(string Action, string TranslationKey, Key DefaultKey, JoyButton DefaultJoyButton);
}
