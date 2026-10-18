namespace Vestiges.Infrastructure;

/// <summary>
/// Retours de la Série (plan 52 S4, section <c>feedback</c> d'elan.json) : sons du rang gagné (de plus en plus aigu de
/// <see cref="RankPitchStep"/> par rang), du coup reçu et de l'encaissement ; tic de la jauge presque vide, sous
/// <see cref="TickFrom"/> de fenêtre, de plus en plus serré ; secousse au rang gagné ; aura au sol (opacité), contour à la
/// couleur du rang, images rémanentes et musique de combat à partir d'un rang.
/// </summary>
public sealed record StreakFeedbackSettings(
    string RankAudio,
    float RankPitchStep,
    string HitAudio,
    string CashOutAudio,
    string TickAudio,
    string TickUrgentAudio,
    float TickFrom,
    float TickSlowSeconds,
    float TickFastSeconds,
    float RankShake,
    float AuraAlpha,
    int OutlineFromRank,
    int AfterimagesFromRank,
    int CombatMusicFromRank);
