namespace Vestiges.World;

/// <summary>Règles de blocage et de tri des décors (world_gen.json, bloc props).</summary>
public struct PropRules
{
    /// <summary>En dessous de cette hauteur visible, un décor ne bloque jamais.</summary>
    public float MinBlockingHeight;
    /// <summary>En dessous de ce nombre de pixels opaques, un décor ne bloque jamais.</summary>
    public int MinBlockingPixels;
    /// <summary>Losange de collision rapporté à la base visible : &lt; 1 pardonne les frôlements.</summary>
    public float FootprintScale;
    /// <summary>Un décor non bloquant plus bas que ceci est un décalque au sol, dessiné sous les entités.</summary>
    public float GroundDecalMaxHeight;
    /// <summary>Hauteur visible à partir de laquelle un décor devient transparent quand le joueur passe derrière.</summary>
    public float OccluderMinHeight;

    public static PropRules Default => new()
    {
        MinBlockingHeight = 18f,
        MinBlockingPixels = 150,
        FootprintScale = 0.85f,
        GroundDecalMaxHeight = 12f,
        OccluderMinHeight = 24f,
    };

    public static PropRules Current { get; set; } = Default;
}
