using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Core;

/// <summary>
/// La Série (plan 52 §15), en trois règles : chaque élimination la fait monter ; un coup reçu la coupe en deux ; sans
/// élimination pendant la fenêtre de son rang, elle s'arrête et s'encaisse en Essence. Ses rangs donnent vitesse,
/// dégâts et Chance, et leur fenêtre raccourcit. État pur, sans node : le joueur la fait avancer à chaque pas physique,
/// applique les bonus du rang et publie ses changements.
/// </summary>
public sealed class PlayerStreak
{
    public ElanConfig Config { get; }

    /// <summary>Éteinte, la Série reste à zéro et ne donne rien.</summary>
    public bool Enabled { get; private set; }

    /// <summary>Éliminations de la Série en cours.</summary>
    public int Count { get; private set; }

    /// <summary>Rang atteint : 0 (aucun) à <see cref="ElanConfig.Ranks"/>.Count.</summary>
    public int Rank { get; private set; }

    /// <summary>Temps restant avant l'encaissement ; nul quand la Série est vide.</summary>
    public float WindowRemaining { get; private set; }

    /// <summary>Meilleure Série depuis l'allumage, et son rang (bilan de fin de run).</summary>
    public int Best { get; private set; }
    public int BestRank { get; private set; }

    /// <summary>Essence encaissée depuis l'allumage (réglage).</summary>
    public int EssenceCashed { get; private set; }

    /// <summary>Dernière source de gain ou de perte, pour l'affichage de réglage du bac à sable.</summary>
    public string LastChange { get; private set; } = "";

    public PlayerStreak(ElanConfig config)
    {
        Config = config;
        Enabled = config.EnabledInRuns;
    }

    public int RankCount => Config.Ranks.Count;

    /// <summary>Fenêtre pleine au rang donné : elle raccourcit à chaque rang.</summary>
    public float WindowAt(int rank) => rank == 0 ? Config.StreakWindowSeconds : Config.Ranks[rank - 1].WindowSeconds;

    /// <summary>Part de la fenêtre qui reste, de 0 à 1.</summary>
    public float WindowFraction => Count > 0 ? Mathf.Clamp(WindowRemaining / WindowAt(Rank), 0f, 1f) : 0f;

    /// <summary>Multiplicateur de vitesse d'un rang (0 : aucun bonus).</summary>
    public float SpeedAt(int rank) => rank == 0 ? 1f : 1f + Config.Ranks[rank - 1].Speed;

    public float DamageAt(int rank) => rank == 0 ? 1f : 1f + Config.Ranks[rank - 1].Damage;

    /// <summary>Chance ajoutée par un rang (0 : aucune).</summary>
    public float LuckAt(int rank) => rank == 0 ? 0f : Config.Ranks[rank - 1].Luck;

    public Color ColorAt(int rank) => rank == 0 ? Config.StreakColor : Config.Ranks[rank - 1].Color;

    /// <summary>Clé de traduction du nom d'un rang ; vide au rang 0.</summary>
    public string NameAt(int rank) => rank == 0 ? "" : Config.Ranks[rank - 1].Name;

    /// <summary>Essence d'une Série encaissée : Série × taux de son rang, rien au rang 0.</summary>
    public int CashOutFor(int count, int rank) => rank == 0 ? 0 : Mathf.FloorToInt(count * Config.Ranks[rank - 1].CashOut);

    public int RankFor(int count)
    {
        int rank = 0;
        while (rank < Config.Ranks.Count && count >= Config.Ranks[rank].From)
            rank++;
        return rank;
    }

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        if (!enabled)
            Reset();
    }

    /// <summary>
    /// La fenêtre s'écoule ; vide, la Série s'arrête. Rend l'Essence encaissée, ou -1 si la Série ne s'est pas
    /// arrêtée à ce pas (0 : arrêtée sous le premier rang, sans rien).
    /// </summary>
    public int Step(float delta)
    {
        if (!Enabled || Count == 0 || delta <= 0f)
            return -1;
        WindowRemaining -= delta;
        if (WindowRemaining > 0f)
            return -1;
        int essence = CashOutFor(Count, Rank);
        EssenceCashed += essence;
        Set(0, "encaissée");
        WindowRemaining = 0f;
        return essence;
    }

    /// <summary>Une élimination, ou une chute dans le vide qui en vaut plusieurs : la Série monte et la fenêtre repart.</summary>
    public void Kill(int weight, string source)
    {
        if (!Enabled || weight <= 0)
            return;
        Set(Count + weight, source);
        WindowRemaining = WindowAt(Rank);
        if (Count > Best)
        {
            Best = Count;
            BestRank = Rank;
        }
    }

    /// <summary>Un geste maîtrisé (esquive parfaite, saut, percussion, coup au gardien) relance la fenêtre sans rien ajouter.</summary>
    public void Refresh(string source)
    {
        if (!Enabled || Count == 0)
            return;
        WindowRemaining = WindowAt(Rank);
        LastChange = source;
    }

    /// <summary>
    /// Un coup qui entame les PV coupe la Série en deux (arrondi inférieur) : les rangs doublant, c'est un rang de moins.
    /// La fenêtre continue de couler ; une Série d'une seule élimination tombe à zéro sans rien encaisser.
    /// </summary>
    public void Hit()
    {
        if (!Enabled || Count == 0)
            return;
        Set(Count / 2, "coup reçu");
        if (Count == 0)
            WindowRemaining = 0f;
    }

    public void Reset()
    {
        Count = 0;
        Rank = 0;
        WindowRemaining = 0f;
        Best = 0;
        BestRank = 0;
        EssenceCashed = 0;
        LastChange = "";
    }

    private void Set(int count, string source)
    {
        Count = Mathf.Max(0, count);
        LastChange = source;
        Rank = RankFor(Count);
    }
}
