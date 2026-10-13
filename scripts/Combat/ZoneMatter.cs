namespace Vestiges.Combat;

/// <summary>
/// Matière d'une zone au sol (pixel_fx.gdshader, forme 3) : décide de son dessin, de son bord et de son animation
/// (plan 25 S10). Les couloirs (forme 7) sont toujours des annonces.
/// </summary>
public enum ZoneMatter
{
    /// <summary>Annonce d'un coup : bord net, sol assombri, front de remplissage.</summary>
    Mark = 0,
    /// <summary>Flaque : lobes, volume, reflet, bulles, gouttes autour.</summary>
    Liquid = 1,
    /// <summary>Feu au sol : sol roussi, flammèches qui montent, braises.</summary>
    Fire = 2,
    /// <summary>Cadran couché : graduations et aiguille qui tourne.</summary>
    Dial = 3,
    /// <summary>Éclat bref : bord hérissé qui s'ouvre, rayons depuis le centre.</summary>
    Burst = 4,
}
