using Godot;

namespace Vestiges.Combat;

/// <summary>Paramètres d'une gerbe d'étincelles. Vitesses en texels par seconde.</summary>
public struct SparkBurst
{
    public FxFamily Family;
    public FxOwner Owner;
    public int Count;
    /// <summary>Direction moyenne (normalisée) ; zéro pour une gerbe dans toutes les directions.</summary>
    public Vector2 Direction;
    public float Spread;
    public float SpeedMin;
    public float SpeedMax;
    public float LifeMin;
    public float LifeMax;
    /// <summary>Éclats projetés en l'air qui retombent et rebondissent au sol (vue iso), sinon étincelles filantes.</summary>
    public bool Ballistic;
    /// <summary>Côté en texels : 1, ou 2 pour les éclats marquants.</summary>
    public int Size;
}
