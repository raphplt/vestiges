using Godot;

namespace Vestiges.World;

/// <summary>
/// Chronomètre du chargement d'une run : une ligne « [Chargement] » par étape, avec sa durée et le temps écoulé
/// depuis l'entrée dans la scène. Sert à mesurer avant d'optimiser.
/// </summary>
public static class LoadProfiler
{
    private static ulong _start;
    private static ulong _last;

    public static void Begin()
    {
        _start = Time.GetTicksUsec();
        _last = _start;
    }

    public static void Mark(string step)
    {
        ulong now = Time.GetTicksUsec();
        GD.Print($"[Chargement] {step} : {(now - _last) / 1000.0:F0} ms (total {(now - _start) / 1000.0:F0} ms)");
        _last = now;
    }
}
