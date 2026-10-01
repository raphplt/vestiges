using System.Collections.Generic;
using Godot;

namespace Vestiges.UI;

/// <summary>Une carte de l'écran de choix : bandeau (rareté ou nature), titre, lignes colorées, prix éventuel.</summary>
public sealed class ChoiceCard
{
    public string Tag = "";
    public Color Frame = ChoiceStyle.NeutralBorder;
    /// <summary>Rang de rareté (0 Commun à 4 Légendaire) : épaisseur du cadre.</summary>
    public int Rank = -1;
    public string Title = "";
    public readonly List<(string Text, Color Color)> Lines = new();
    public Texture2D Icon;
    /// <summary>Prix affiché à droite du bandeau (« 30 Essence »), ou rien.</summary>
    public string Price;
    public bool Enabled = true;
}
