using System;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Collecte la jeune génération tous les quelques Mo alloués. Le budget gen0 par défaut de .NET suit
/// le cache du processeur (des dizaines de Mo) : une collecte toutes les une à deux minutes, qui
/// promeut 10 à 20 Mo d'un coup et fige l'image 25 à 65 ms. Des collectes rapprochées promeuvent
/// peu et restent sous 15 ms (plan 29). Le réglage natif (GCgen0size) ne se lit que dans une
/// variable d'environnement, d'où ce déclenchement en code. Un seul nœud : son rappel C# par image est négligeable.
/// </summary>
public partial class GcPacer : Node
{
    private const long BudgetBytes = 4L * 1024 * 1024;

    private long _allocatedAtCollection;
    private int _collections;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Reset();
    }

    public override void _Process(double delta)
    {
        if (GC.CollectionCount(0) != _collections)
        {
            Reset();
            return;
        }
        if (GC.GetTotalAllocatedBytes() - _allocatedAtCollection < BudgetBytes)
            return;
        GC.Collect(0, GCCollectionMode.Forced, blocking: true);
        Reset();
    }

    private void Reset()
    {
        _allocatedAtCollection = GC.GetTotalAllocatedBytes();
        _collections = GC.CollectionCount(0);
    }
}
