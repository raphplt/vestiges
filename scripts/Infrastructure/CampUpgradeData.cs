namespace Vestiges.Infrastructure;

/// <summary>Rayon du Campement (plan 36 §3) : l'écran les présente dans cet ordre.</summary>
public enum CampBranch
{
    Capacities,
    Stats,
    Comfort,
}

/// <summary>Ce qu'un rang du Campement change au début de la run.</summary>
public enum CampEffect
{
    WeaponSlots,
    Rerolls,
    Banishes,
    StartEssence,
    Stat,
}

/// <summary>Une amélioration du Campement (data/progression/camp_upgrades.json) : un prix par rang, un pas par rang.</summary>
public sealed class CampUpgradeData
{
    public string Id;
    public CampBranch Branch;
    public CampEffect Effect;
    /// <summary>Statistique du joueur et son modificateur, pour <see cref="CampEffect.Stat"/> seulement.</summary>
    public string Stat;
    public string ModifierType;
    public float Step;
    /// <summary>Le pas s'affiche en pourcentage (0,05 → « 5 % »).</summary>
    public bool Percent;
    public int[] Prices;
    public string NameKey;
    public string DescKey;

    public int MaxRank => Prices.Length;

    public bool IsMultiplicative => ModifierType == "multiplicative";

    /// <summary>Prix du rang <paramref name="rank"/> (de 1 à <see cref="MaxRank"/>).</summary>
    public int PriceOfRank(int rank) => Prices[rank - 1];

    /// <summary>Gain total à ce rang : rang × pas. Un multiplicateur vaut 1 + ce gain, sans composer.</summary>
    public float TotalAt(int rank) => Step * rank;
}
