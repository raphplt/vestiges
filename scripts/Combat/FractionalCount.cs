using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Stat entière fractionnaire (plan 21 §3, plan 23) : la partie entière s'applique toujours, la partie décimale est
/// la chance d'en avoir une de plus à cette attaque. 2,5 projectiles, c'est 2 projectiles et une chance sur deux d'un
/// troisième.
/// </summary>
public static class FractionalCount
{
    /// <summary>Compte tiré pour une attaque ; <paramref name="roll"/> est un tirage uniforme dans [0, 1[.</summary>
    public static int Roll(float value, float roll)
    {
        if (value <= 0f)
            return 0;
        int whole = Mathf.FloorToInt(value);
        return roll < value - whole ? whole + 1 : whole;
    }
}
