namespace Vestiges.Combat;

/// <summary>
/// Matière d'une zone au sol (pixel_fx.gdshader, forme 3) : décide de son dessin, de son bord et de son animation
/// (plan 25 S10). Les couloirs (forme 7) sont toujours des annonces.
/// </summary>
public enum ZoneMatter
{
    /// <summary>Annonce d'un coup : tirets qui tournent, sol assombri et fissuré, front, ondes qui convergent.</summary>
    Mark = 0,
    /// <summary>Flaque : volume, reflet, ondes concentriques, taches qui dérivent, bulles, gouttes autour.</summary>
    Liquid = 1,
    /// <summary>Feu au sol : couronne de braises, anneau calciné, fissures rougeoyantes, flammèches qui montent.</summary>
    Fire = 2,
    /// <summary>Cadran couché : graduations et aiguille qui tourne.</summary>
    Dial = 3,
    /// <summary>Éclat bref : onde de choc ronde qui s'élargit, rayons depuis le centre, débris projetés.</summary>
    Burst = 4,
}
