using System.Threading.Tasks;
using Godot;
using Vestiges.UI;

namespace Vestiges.Tests;

/// <summary>
/// Capture de l'écran de chargement (plan 24 B5) : l'overlay seul, nourri des étapes réelles du chargement, photographié
/// à trois moments. Usage : godot --path . res://tools/tests/LoadingCapture.tscn -- --output dossier
/// </summary>
public partial class LoadingCapture : Node
{
	public override async void _Ready()
	{
		string[] args = OS.GetCmdlineUserArgs();
		int index = System.Array.IndexOf(args, "--output");
		string output = index >= 0 && index + 1 < args.Length ? args[index + 1] : "user://loading";
		DirAccess.MakeDirRecursiveAbsolute(output);
		GameLoadingOverlay overlay = new();
		AddChild(overlay);
		string[] steps = { "Préparation des shaders...", "Création du monde...", "Terrain... 30%", "Terrain... 80%", "Routes... 50%", "Décors...", "Préparation des créatures..." };
		for (int i = 0; i < steps.Length; i++)
		{
			overlay.SetProgress(steps[i]);
			await Seconds(0.6f);
			if (i is 1 or 3 or 6)
				GetViewport().GetTexture().GetImage().SavePng($"{output}/loading-{i}.png");
		}
		GD.Print($"[LoadingCapture] RESULT captures dans {output}");
		GetTree().Quit();
	}

	private async Task Seconds(float seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
}
