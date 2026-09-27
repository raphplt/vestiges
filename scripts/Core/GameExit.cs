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
    public static async Task QuitAsync(SceneTree tree, int exitCode = 0)
    {
        tree.CurrentScene?.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        tree.Quit(exitCode);
    }
}
