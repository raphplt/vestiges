using System.Threading.Tasks;
using Godot;
using Vestiges.UI;

namespace Vestiges.Tests;

/// <summary>
/// Capture de l'écran de chargement (plan 24 B5) : l'overlay seul, nourri des étapes réelles du chargement, photographié
/// à trois moments. Usage : godot --path . res://tools/tests/LoadingCapture.tscn -- --output dossier
/// --capture-size WxH : impose la taille réelle après le démarrage en plein écran du projet.
/// --locale fr|en, --message-keys CLÉ,CLÉ : capture une planche de documents, rotation suspendue.
/// --samples N : capture N messages successifs du vrai tirage, espacés de sept secondes.
/// </summary>
public partial class LoadingCapture : Node
{
	public override async void _Ready()
	{
		string[] args = OS.GetCmdlineUserArgs();
		int index = System.Array.IndexOf(args, "--output");
		string output = index >= 0 && index + 1 < args.Length ? args[index + 1] : "user://loading";
		DirAccess.MakeDirRecursiveAbsolute(output);
		index = System.Array.IndexOf(args, "--capture-size");
		if (index >= 0 && index + 1 < args.Length)
		{
			string[] dimensions = args[index + 1].Split('x');
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			GetWindow().Mode = Window.ModeEnum.Windowed;
			await Seconds(0.1f);
			GetWindow().Size = new Vector2I(int.Parse(dimensions[0]), int.Parse(dimensions[1]));
			await Seconds(0.1f);
		}
		index = System.Array.IndexOf(args, "--locale");
		if (index >= 0 && index + 1 < args.Length)
			TranslationServer.SetLocale(args[index + 1]);
		GameLoadingOverlay overlay = new();
		AddChild(overlay);
		Label message = overlay.GetChild<Control>(0).GetNode<Label>("LoadingMessage");
		string[] steps = { "Préparation des shaders...", "Création du monde...", "Terrain... 30%", "Terrain... 80%", "Routes... 50%", "Décors...", "Préparation des créatures..." };
		for (int i = 0; i < steps.Length; i++)
		{
			overlay.SetProgress(steps[i]);
			await Seconds(0.6f);
			if (i is 1 or 3 or 6)
				GetViewport().GetTexture().GetImage().SavePng($"{output}/loading-{i}.png");
		}
		index = System.Array.IndexOf(args, "--samples");
		int samples = index >= 0 && index + 1 < args.Length ? int.Parse(args[index + 1]) : 0;
		for (int i = 0; i < samples; i++)
		{
			await Seconds(7f);
			CaptureMessage(message, $"{output}/rotation-{i}.png");
		}
		index = System.Array.IndexOf(args, "--message-keys");
		if (index >= 0 && index + 1 < args.Length)
		{
			overlay.SetProcess(false);
			foreach (string key in args[index + 1].Split(','))
			{
				message.Text = Tr(key).ToString().Replace("\\n", "\n", System.StringComparison.Ordinal);
				await Seconds(0.2f);
				CaptureMessage(message, $"{output}/{key}.png");
			}
		}
		GD.Print($"[LoadingCapture] RESULT captures dans {output}");
		GetTree().Quit();
	}

	private async Task Seconds(float seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);

	private void CaptureMessage(Label message, string path)
	{
		GetViewport().GetTexture().GetImage().SavePng(path);
		GD.Print($"[LoadingCapture] image={path} text={message.Text.Replace("\n", " / ")} lines={message.GetLineCount()} rect={message.GetGlobalRect()}");
	}
}
