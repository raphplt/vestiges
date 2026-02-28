namespace Vestiges.Simulation;

/// <summary>
/// Profil de compétence simulé pour l'IA.
/// Détermine comment le bot prend ses décisions : réactivité, survie, kiting, interactions.
/// </summary>
public class AIProfile
{
    public string Name { get; set; }

    // Décision
    public float DecisionInterval { get; set; }
    public float RetreatThreshold { get; set; }
    public bool DefendAtNight { get; set; }
    public bool CanKite { get; set; }

    // Mouvement
    public float RoamChangeDirInterval { get; set; }
    public float RoamMaxRadius { get; set; }

    // Interaction
    public bool InteractsDuringDay { get; set; }

    public static AIProfile Noob() => new()
    {
        Name = "noob",
        DecisionInterval = 0.5f,
        RetreatThreshold = 0.2f,
        DefendAtNight = false,
        CanKite = false,
        RoamChangeDirInterval = 3f,
        RoamMaxRadius = 800f,
        InteractsDuringDay = false
    };

    public static AIProfile Medium() => new()
    {
        Name = "medium",
        DecisionInterval = 0.15f,
        RetreatThreshold = 0.5f,
        DefendAtNight = true,
        CanKite = true,
        RoamChangeDirInterval = 2f,
        RoamMaxRadius = 600f,
        InteractsDuringDay = true
    };

    public static AIProfile Pro() => new()
    {
        Name = "pro",
        DecisionInterval = 0.08f,
        RetreatThreshold = 0.5f,
        DefendAtNight = true,
        CanKite = true,
        RoamChangeDirInterval = 1.5f,
        RoamMaxRadius = 500f,
        InteractsDuringDay = true
    };

    public static AIProfile FromName(string name) => name switch
    {
        "noob" => Noob(),
        "medium" => Medium(),
        "pro" => Pro(),
        _ => Medium()
    };
}
