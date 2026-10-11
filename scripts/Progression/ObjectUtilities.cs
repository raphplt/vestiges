using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Objets qui changent une règle du joueur plutôt qu'un chiffre (plan 35 I6) : Ticket de consigne (une résurrection par
/// exemplaire), Plan de métro (le dash devient un saut), Trèfle plastifié (une carte de plus au level-up). Simple
/// classe possédée par le joueur : rien ne tourne par image, le joueur et le level-up la lisent quand il le faut.
/// </summary>
public sealed class ObjectUtilities
{
    public const string ReviveStat = "revive";
    public const string DashBlinkStat = "dash_blink";
    public const string ExtraCardsStat = "extra_cards";

    private readonly Player _player;
    private float _revives;
    private float _extraCards;

    public ObjectUtilities(Player player) => _player = player;

    /// <summary>Longueur du saut du Plan de métro ; le joueur la rend à un nouveau module de mobilité.</summary>
    public float BlinkDistance { get; private set; }

    /// <summary>Objet qui porte la résurrection : c'est un de ses exemplaires qui se consume.</summary>
    public string ReviveItemId { get; private set; }
    public float ReviveHpRatio { get; private set; }
    public float ReviveInvulnerabilitySeconds { get; private set; }

    public bool CanRevive => _revives >= 1f && ReviveItemId != null;
    public int ExtraCards => Mathf.FloorToInt(_extraCards + 0.0001f);

    public static bool IsUtilityStat(string stat) => stat is ReviveStat or DashBlinkStat or ExtraCardsStat;

    /// <summary>Réglages d'un objet à règle, lus à son arrivée.</summary>
    public void Configure(PassiveSouvenirData data)
    {
        foreach (PassiveEffectData effect in data.Effects)
        {
            if (effect.Stat != ReviveStat)
                continue;
            ReviveItemId = data.Id;
            ReviveHpRatio = data.Parameters["hp_ratio"];
            ReviveInvulnerabilitySeconds = data.Parameters["invulnerability_seconds"];
        }
    }

    /// <summary>Écart de valeur d'un effet (somme des exemplaires) ; un exemplaire oublié ou consumé le retire.</summary>
    public void Add(string stat, float change)
    {
        switch (stat)
        {
            case ReviveStat: _revives = ObjectTriggers.Settle(_revives + change); break;
            case ExtraCardsStat: _extraCards = ObjectTriggers.Settle(_extraCards + change); break;
            case DashBlinkStat:
                BlinkDistance = ObjectTriggers.Settle(BlinkDistance + change);
                _player.Mobility.BlinkDistance = BlinkDistance;
                break;
        }
    }
}
