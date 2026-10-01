namespace Vestiges.Infrastructure;

/// <summary>Intention musicale résolue par <see cref="MusicDirector"/>, de la plus faible priorité à la plus forte en run.</summary>
public enum MusicIntent
{
    None,
    Hub,
    Exploration,
    Combat,
    Calm,
    LateGame,
    Endgame,
    Warning,
    Resurgence,
    Death,
}
