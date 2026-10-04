using System;
using System.Diagnostics;
using Godot;
#if TOOLS
using Vestiges.Infrastructure;
#endif

namespace Vestiges.World;

/// <summary>
/// Garde du chargement de la run (plan 26 Q4) : après chaque attente, le chargement vérifie que sa scène existe encore,
/// et s'arrête sans rien toucher si elle a été quittée. Porte aussi les pannes injectées des bancs.
/// </summary>
public static class LoadGuard
{
#if TOOLS
    /// <summary>Étape où un banc fait échouer le chargement ; sans effet hors session de banc.</summary>
    internal static string FaultStep { get; set; }
#endif

    /// <summary>Lève <see cref="LoadAbandonedException"/> si <paramref name="owner"/> a quitté l'arbre ou a été libéré.</summary>
    public static void EnsureAlive(Node owner)
    {
        if (!IsAlive(owner))
            throw new LoadAbandonedException();
    }

    public static bool IsAlive(Node owner) => GodotObject.IsInstanceValid(owner) && owner.IsInsideTree();

    /// <summary>Panne injectée par un banc à l'étape <paramref name="step"/> ; l'appel disparaît des exports.</summary>
    [Conditional("TOOLS")]
    public static void InjectFault(string step)
    {
#if TOOLS
        if (DevelopmentMode.IsTestSession && FaultStep == step)
            throw new InvalidOperationException($"panne injectée à l'étape « {step} »");
#endif
    }
}
