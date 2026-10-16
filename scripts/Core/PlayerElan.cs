using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Core;

/// <summary>
/// Élan du joueur (plan 52) : de 0 à 1, nourri par la course jusqu'à un plafond, puis par les gestes maîtrisés ;
/// il baisse à l'arrêt, s'érode au-dessus du plafond de la course et chute d'un palier sous un coup. État pur, sans
/// node : le joueur le fait avancer à chaque pas physique et applique les bonus du palier.
/// </summary>
public sealed class PlayerElan
{
    public ElanConfig Config { get; }

    /// <summary>Éteint, l'Élan reste à zéro et ne donne rien.</summary>
    public bool Enabled { get; private set; }

    public float Value { get; private set; }

    /// <summary>Palier atteint : 0 (aucun) à <see cref="ElanConfig.Tiers"/>.Count.</summary>
    public int Tier { get; private set; }

    /// <summary>Dernière source de gain ou de perte, pour l'affichage de réglage du bac à sable.</summary>
    public string LastChange { get; private set; } = "";

    private float _stillSeconds;

    public PlayerElan(ElanConfig config)
    {
        Config = config;
        Enabled = config.EnabledInRuns;
    }

    /// <summary>Multiplicateur de vitesse d'un palier (0 : aucun bonus).</summary>
    public float SpeedAt(int tier) => tier == 0 ? 1f : 1f + Config.Tiers[tier - 1].Speed;

    public float DamageAt(int tier) => tier == 0 ? 1f : 1f + Config.Tiers[tier - 1].Damage;

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
        if (!enabled)
            Reset();
    }

    /// <param name="speedRatio">
    /// Vitesse réelle rapportée à la vitesse de course du personnage (1 = pleine course). Un dash dépasse 1 et compte
    /// comme course ; l'eau et les ralentissements la font passer sous le seuil : courir freiné ne nourrit pas l'Élan.
    /// </param>
    public void Step(float delta, float speedRatio)
    {
        if (!Enabled || delta <= 0f)
            return;
        if (speedRatio < Config.StillSpeedRatio)
        {
            _stillSeconds += delta;
            if (_stillSeconds > Config.StillGraceSeconds)
                Set(Value - Config.StillDecayPerSecond * delta, "arrêt");
            return;
        }
        _stillSeconds = 0f;
        if (Value > Config.SpeedCap)
            Set(Mathf.Max(Config.SpeedCap, Value - Config.UpkeepDecayPerSecond * delta), LastChange);
        else if (speedRatio >= Config.SpeedMinRatio)
            Set(Mathf.Min(Config.SpeedCap, Value + Config.SpeedGainPerSecond * delta), "course");
    }

    /// <summary>Gain d'un geste maîtrisé ; seul ce chemin dépasse le plafond de la course.</summary>
    public void Gain(float amount, string source)
    {
        if (Enabled && amount > 0f)
            Set(Value + amount, source);
    }

    /// <summary>Un coup qui entame les PV fait perdre un palier.</summary>
    public void Hit()
    {
        if (Enabled)
            Set(Value - Config.HitLoss, "coup reçu");
    }

    public void Reset()
    {
        Value = 0f;
        Tier = 0;
        _stillSeconds = 0f;
        LastChange = "";
    }

    private void Set(float value, string source)
    {
        Value = Mathf.Clamp(value, 0f, 1f);
        LastChange = source;
        int tier = 0;
        // Un seuil atteint à l'arrondi près compte : la course plafonne exactement au premier seuil.
        while (tier < Config.Tiers.Count && Value >= Config.Tiers[tier].From - 0.0001f)
            tier++;
        Tier = tier;
    }
}
