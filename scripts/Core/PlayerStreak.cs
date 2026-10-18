using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Core;

/// <summary>
/// La Série (plan 52 §13) : un combo d'éliminations. Chaque élimination l'augmente et relance sa fenêtre ; la fenêtre
/// écoulée ou un coup reçu la font retomber au début du palier inférieur. Ses paliers donnent vitesse, dégâts et Chance.
/// État pur, sans node : le joueur la fait avancer à chaque pas physique et applique les bonus du palier.
/// </summary>
public sealed class PlayerStreak
{
    public ElanConfig Config { get; }

    /// <summary>Éteinte, la Série reste à zéro et ne donne rien.</summary>
    public bool Enabled { get; private set; }

    /// <summary>Éliminations de la Série en cours.</summary>
    public int Count { get; private set; }

    /// <summary>Palier atteint : 0 (aucun) à <see cref="ElanConfig.Tiers"/>.Count.</summary>
    public int Tier { get; private set; }

    /// <summary>Temps restant avant que la Série ne retombe d'un palier ; nul quand elle est vide.</summary>
    public float WindowRemaining { get; private set; }

    /// <summary>Meilleure Série depuis l'allumage (affichage de réglage du bac à sable).</summary>
    public int Best { get; private set; }

    /// <summary>Dernière source de gain ou de perte, pour l'affichage de réglage du bac à sable.</summary>
    public string LastChange { get; private set; } = "";

    public PlayerStreak(ElanConfig config)
    {
        Config = config;
        Enabled = config.EnabledInRuns;
    }

    /// <summary>Multiplicateur de vitesse d'un palier (0 : aucun bonus).</summary>
    public float SpeedAt(int tier) => tier == 0 ? 1f : 1f + Config.Tiers[tier - 1].Speed;

    public float DamageAt(int tier) => tier == 0 ? 1f : 1f + Config.Tiers[tier - 1].Damage;

    /// <summary>Chance ajoutée par un palier (0 : aucune).</summary>
    public float LuckAt(int tier) => tier == 0 ? 0f : Config.Tiers[tier - 1].Luck;

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        if (!enabled)
            Reset();
    }

    /// <summary>La fenêtre s'écoule ; vide, la Série retombe d'un palier et la fenêtre repart s'il en reste.</summary>
    public void Step(float delta)
    {
        if (!Enabled || Count == 0 || delta <= 0f)
            return;
        WindowRemaining -= delta;
        if (WindowRemaining > 0f)
            return;
        DropTier("fenêtre écoulée");
    }

    /// <summary>Une élimination, ou une chute dans le vide qui en vaut plusieurs : la Série monte et la fenêtre repart.</summary>
    public void Kill(int weight, string source)
    {
        if (!Enabled || weight <= 0)
            return;
        Set(Count + weight, source);
        WindowRemaining = Config.StreakWindowSeconds;
        if (Count > Best)
            Best = Count;
    }

    /// <summary>Un geste maîtrisé (esquive parfaite, saut, percussion) relance la fenêtre sans ajouter d'élimination.</summary>
    public void Refresh(string source)
    {
        if (!Enabled || Count == 0)
            return;
        WindowRemaining = Config.StreakWindowSeconds;
        LastChange = source;
    }

    /// <summary>Un coup qui entame les PV fait perdre un palier.</summary>
    public void Hit()
    {
        if (Enabled && Count > 0)
            DropTier("coup reçu");
    }

    public void Reset()
    {
        Count = 0;
        Tier = 0;
        WindowRemaining = 0f;
        Best = 0;
        LastChange = "";
    }

    /// <summary>Retombe au début du palier inférieur (zéro depuis les deux premiers) ; la fenêtre repart s'il en reste.</summary>
    private void DropTier(string source)
    {
        int floor = Tier >= 2 ? Config.Tiers[Tier - 2].From : 0;
        Set(floor, source);
        WindowRemaining = Count > 0 ? Config.StreakWindowSeconds : 0f;
    }

    private void Set(int count, string source)
    {
        Count = Mathf.Max(0, count);
        LastChange = source;
        int tier = 0;
        while (tier < Config.Tiers.Count && Count >= Config.Tiers[tier].From)
            tier++;
        Tier = tier;
    }
}
