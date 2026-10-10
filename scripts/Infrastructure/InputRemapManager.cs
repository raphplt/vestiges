using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Gestionnaire de remapping des touches. Autoload.
/// Supporte clavier + manette. Persiste en user://input_bindings.cfg ce que le joueur a changé (plan 26 Q8b) :
/// remapper remplace la touche ou le bouton principal d'une action, sans toucher aux secondaires (flèches, stick).
/// </summary>
public partial class InputRemapManager : Node
{
	public static InputRemapManager Instance { get; private set; }

	private const string SettingsPath = "user://input_bindings.cfg";
	private const string MetaSection = "meta";
	private const int FormatVersion = 2;

	/// <summary>Actions remappables avec leur description et leur binding par défaut.</summary>
	public static readonly ActionDef[] RemappableActions = new[]
	{
		new ActionDef("move_up", "UI_MOVE_UP", Key.W, JoyButton.DpadUp),
		new ActionDef("move_down", "UI_MOVE_DOWN", Key.S, JoyButton.DpadDown),
		new ActionDef("move_left", "UI_MOVE_LEFT", Key.A, JoyButton.DpadLeft),
		new ActionDef("move_right", "UI_MOVE_RIGHT", Key.D, JoyButton.DpadRight),
		new ActionDef("interact", "UI_INTERACT", Key.E, JoyButton.A),
		new ActionDef("mobility", "UI_MOBILITY", Key.Space, JoyButton.X),
		new ActionDef("journal", "UI_JOURNAL", Key.J, JoyButton.Back),
		new ActionDef("show_map", "UI_SHOW_MAP", Key.M, JoyButton.RightShoulder),
	};

	/// <summary>true si une manette est connectée.</summary>
	public bool GamepadConnected { get; private set; }

	/// <summary>Liaisons par défaut de chaque action, relevées au démarrage avant toute préférence.</summary>
	private readonly Dictionary<string, InputEvent[]> _defaults = new();

	public override void _EnterTree()
	{
		Instance = this;
		Input.JoyConnectionChanged += OnJoyConnectionChanged;
		GamepadConnected = Input.GetConnectedJoypads().Count > 0;
	}

	public override void _Ready()
	{
		EnsureGamepadDefaults();
		foreach (ActionDef def in RemappableActions)
			_defaults[def.Action] = new List<InputEvent>(InputMap.ActionGetEvents(def.Action)).ToArray();
		LoadBindings();
	}

	public override void _ExitTree()
	{
		Input.JoyConnectionChanged -= OnJoyConnectionChanged;
		Instance = null;
	}

	/// <summary>Remplace la touche principale d'une action, garde ses touches secondaires, et l'enregistre.</summary>
	public void RemapKey(string action, Key newKey)
	{
		ApplyPrimaryKey(action, newKey);
		SaveBindings();
		GD.Print($"[InputRemap] {action} → {OS.GetKeycodeString(newKey)}");
	}

	/// <summary>Remplace le bouton principal d'une action, garde ses axes et autres boutons, et l'enregistre.</summary>
	public void RemapJoyButton(string action, JoyButton newButton)
	{
		ApplyPrimaryJoyButton(action, newButton);
		SaveBindings();
		GD.Print($"[InputRemap] {action} → Joy {newButton}");
	}

	/// <summary>Remet toutes les liaisons par défaut, secondaires comprises, et oublie les préférences.</summary>
	public void ResetToDefaults()
	{
		foreach (ActionDef def in RemappableActions)
		{
			InputMap.ActionEraseEvents(def.Action);
			foreach (InputEvent ev in _defaults[def.Action])
				InputMap.ActionAddEvent(def.Action, ev);
		}

		if (FileAccess.FileExists(SettingsPath))
			DirAccess.RemoveAbsolute(SettingsPath);

		GD.Print("[InputRemap] Reset to defaults");
	}

	/// <summary>La touche principale est la première de l'action ; elle seule est remplacée, à la même place.</summary>
	private static void ApplyPrimaryKey(string action, Key newKey)
	{
		ReplacePrimary<InputEventKey>(action, new InputEventKey { PhysicalKeycode = newKey });
	}

	private static void ApplyPrimaryJoyButton(string action, JoyButton newButton)
	{
		ReplacePrimary<InputEventJoypadButton>(action, new InputEventJoypadButton { ButtonIndex = newButton });
	}

	private static void ReplacePrimary<T>(string action, InputEvent replacement) where T : InputEvent
	{
		Godot.Collections.Array<InputEvent> events = InputMap.ActionGetEvents(action);
		InputMap.ActionEraseEvents(action);
		bool replaced = false;
		foreach (InputEvent ev in events)
		{
			if (!replaced && ev is T)
			{
				InputMap.ActionAddEvent(action, replacement);
				replaced = true;
			}
			else
			{
				InputMap.ActionAddEvent(action, ev);
			}
		}
		if (!replaced)
			InputMap.ActionAddEvent(action, replacement);
	}

	/// <summary>Retourne le nom lisible du binding clavier actuel d'une action.</summary>
	public static string GetKeyName(string action)
	{
		foreach (InputEvent ev in InputMap.ActionGetEvents(action))
		{
			if (ev is InputEventKey key)
				return OS.GetKeycodeString(KeyOf(key));
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

	/// <summary>N'écrit que les liaisons principales que le joueur a changées.</summary>
	private void SaveBindings()
	{
		ConfigFile cfg = new();
		cfg.SetValue(MetaSection, "version", FormatVersion);
		foreach (ActionDef def in RemappableActions)
		{
			Key key = PrimaryKey(InputMap.ActionGetEvents(def.Action));
			if (key != Key.None && key != PrimaryKey(_defaults[def.Action]))
				cfg.SetValue(def.Action, "key", (long)key);
			JoyButton button = PrimaryJoyButton(InputMap.ActionGetEvents(def.Action));
			if (button != JoyButton.Invalid && button != PrimaryJoyButton(_defaults[def.Action]))
				cfg.SetValue(def.Action, "joy_button", (long)button);
		}
		if (cfg.Save(SettingsPath) != Error.Ok)
			GD.PushWarning($"[InputRemap] Préférences de touches non enregistrées ({SettingsPath})");
	}

	/// <summary>Applique les préférences sans rien réécrire.</summary>
	private void LoadBindings()
	{
		ConfigFile cfg = new();
		if (cfg.Load(SettingsPath) != Error.Ok)
			return;
		bool legacy = cfg.GetValue(MetaSection, "version", 0).AsInt32() < FormatVersion;

		foreach (ActionDef def in RemappableActions)
		{
			if (cfg.HasSectionKey(def.Action, "key"))
			{
				Key key = (Key)cfg.GetValue(def.Action, "key").AsInt64();
				// Ancien format : la dernière touche de l'action était écrite, même sans changement ; une touche
				// secondaire par défaut (la flèche) n'est donc pas un choix du joueur.
				if (!(legacy && IsDefaultSecondaryKey(def.Action, key)))
					ApplyPrimaryKey(def.Action, key);
			}
			if (cfg.HasSectionKey(def.Action, "joy_button"))
				ApplyPrimaryJoyButton(def.Action, (JoyButton)cfg.GetValue(def.Action, "joy_button").AsInt64());
		}

		GD.Print("[InputRemap] Bindings loaded");
	}

	private bool IsDefaultSecondaryKey(string action, Key key)
	{
		bool first = true;
		foreach (InputEvent ev in _defaults[action])
		{
			if (ev is not InputEventKey keyEvent)
				continue;
			if (!first && KeyOf(keyEvent) == key)
				return true;
			first = false;
		}
		return false;
	}

	private static Key PrimaryKey(IEnumerable<InputEvent> events)
	{
		foreach (InputEvent ev in events)
		{
			if (ev is InputEventKey key)
				return KeyOf(key);
		}
		return Key.None;
	}

	private static JoyButton PrimaryJoyButton(IEnumerable<InputEvent> events)
	{
		foreach (InputEvent ev in events)
		{
			if (ev is InputEventJoypadButton joy)
				return joy.ButtonIndex;
		}
		return JoyButton.Invalid;
	}

	private static Key KeyOf(InputEventKey key) => key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;

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
