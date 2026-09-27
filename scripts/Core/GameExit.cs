using System;
using System.Threading.Tasks;
using Godot;

namespace Vestiges.Core;

/// <summary>
/// Sortie propre du jeu. Quitter d'un coup laissait des milliers d'enveloppes C# (formes de collision des décors,
/// tweens, styles) vivantes jusqu'à l'arrêt du runtime .NET : leur libération tardive déclenchait un crash à la
/// fermeture dans les builds de debug. La scène est libérée et le ramasse-miettes passe avant de quitter.
/// </summary>
public static class GameExit
{
    private static bool _quitting;

    public static async Task QuitAsync(SceneTree tree, int exitCode = 0)
    {
        // Un second clic sur Quitter ou sur la croix pendant la sortie ne relance rien.
        if (_quitting)
            return;
        _quitting = true;
        try
        {
            tree.CurrentScene?.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        catch (Exception exception)
        {
            // La fenêtre doit se fermer quoi qu'il arrive.
            GD.PushError($"[GameExit] {exception}");
        }
        tree.Quit(exitCode);
    }
}
