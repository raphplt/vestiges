using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Filtre daltonien plein écran. CanvasLayer au dessus de tout.
/// Modes : Off, Protanopia, Deuteranopia, Tritanopia.
/// Persiste le choix en user://settings.cfg.
/// </summary>
public partial class ColorBlindFilter : CanvasLayer
{
	public enum Mode { Off = 0, Protanopia = 1, Deuteranopia = 2, Tritanopia = 3 }

	public static ColorBlindFilter Instance { get; private set; }

	private ColorRect _rect;
	private ShaderMaterial _material;
	private Mode _currentMode = Mode.Off;

	private const string SettingsPath = "user://settings.cfg";

	public Mode CurrentMode => _currentMode;

	public override void _Ready()
	{
		Instance = this;
		Layer = 100;
		ProcessMode = ProcessModeEnum.Always;

		Shader shader = GD.Load<Shader>("res://assets/shaders/colorblind.gdshader");
		_material = new ShaderMaterial { Shader = shader };

		_rect = new ColorRect();
		_rect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_rect.MouseFilter = Control.MouseFilterEnum.Ignore;
		_rect.Material = _material;
		_rect.Visible = false;
		AddChild(_rect);

		LoadSettings();
	}

	public void SetMode(Mode mode)
	{
		_currentMode = mode;
		_material.SetShaderParameter("mode", (int)mode);
		_rect.Visible = mode != Mode.Off;
		SaveSettings();
		GD.Print($"[ColorBlindFilter] Mode: {mode}");
	}

	/// <summary>Cycle vers le mode suivant. Retourne le nouveau mode.</summary>
	public Mode CycleMode()
	{
		Mode next = (Mode)(((int)_currentMode + 1) % 4);
		SetMode(next);
		return next;
	}

	public static string ModeLabel(Mode mode)
	{
		return mode switch
		{
			Mode.Protanopia => TranslationServer.Translate("UI_COLORBLIND_PROTANOPIA"),
			Mode.Deuteranopia => TranslationServer.Translate("UI_COLORBLIND_DEUTERANOPIA"),
			Mode.Tritanopia => TranslationServer.Translate("UI_COLORBLIND_TRITANOPIA"),
			_ => TranslationServer.Translate("UI_COLORBLIND_OFF"),
		};
	}

	private void LoadSettings()
	{
		ConfigFile cfg = new();
		if (cfg.Load(SettingsPath) != Error.Ok)
			return;
		int mode = cfg.GetValue("accessibility", "colorblind_mode", 0).AsInt32();
		SetMode((Mode)Mathf.Clamp(mode, 0, 3));
	}

	private void SaveSettings()
	{
		ConfigFile cfg = new();
		cfg.Load(SettingsPath);
		cfg.SetValue("accessibility", "colorblind_mode", (int)_currentMode);
		cfg.Save(SettingsPath);
	}
}
