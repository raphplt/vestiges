namespace Vestiges.Combat;

/// <summary>Qui produit l'effet : décide du réglage d'affichage et d'opacité appliqué.</summary>
public enum FxOwner
{
    Player,
    Enemy,
    /// <summary>
    /// Monde et progression (montée de niveau, collecte, éclats de l'oubli) : ni attaque ni menace, donc pleine opacité
    /// et soumis au seul réglage « Particules ».
    /// </summary>
    World,
}
