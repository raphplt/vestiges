using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;

namespace Vestiges.Core;

/// <summary>
/// Percussion (plan 52 E4) : à partir d'un rang de la Série, le dash projette les créatures qu'il traverse, vers l'extérieur
/// de son trajet et en avant. Élan allumé, le vide devient une arme : une créature ordinaire projetée dans le Néant y
/// tombe (voir <c>Enemy.Void.cs</c>).
/// </summary>
public partial class Player
{
    // Marge de la recherche dans l'index de foule : la portée est mesurée au sol, plus longue que la distance à l'écran.
    private const float PercussionQueryMargin = 2f;

    // Créatures déjà projetées par le dash en cours : une seule fois chacune.
    private readonly HashSet<ulong> _percussed = new();
    // Un seul son et une seule relance de la Série par dash, même si chaque pas en touche dans une foule.
    private bool _percussionLanded;

    /// <summary>Palier à partir duquel le dash projette ; réglage du fichier, que le bac à sable peut changer.</summary>
    public int PercussionMinRank { get; set; }

    /// <summary>Créatures projetées depuis le début de la run (affichage de réglage du bac à sable).</summary>
    public int Percussions { get; private set; }

    /// <summary>
    /// Le vide est une arme, allumé avec l'Élan : les créatures évitent le Néant en marchant et y tombent quand on les y
    /// pousse. Éteint (runs normales), elles l'ignorent comme avant.
    /// </summary>
    public bool VoidIsWeapon => Streak.Enabled;

    /// <summary>Après le déplacement : projette les créatures que le trajet de ce pas de dash a traversées.</summary>
    private void StepPercussion(Vector2 from, bool dashMovement)
    {
        if (Mobility.StartedThisStep)
        {
            _percussed.Clear();
            _percussionLanded = false;
        }
        if (!dashMovement || !Streak.Enabled || Streak.Rank < PercussionMinRank)
            return;
        Vector2 to = GlobalPosition;
        float length = from.DistanceTo(to);
        if (length <= 0.01f)
            return;
        ElanConfig config = Streak.Config;
        Vector2 direction = (to - from) / length;
        float distance = config.PercussionDistanceAt(Streak.Rank);
        float reach = config.PercussionReachPixels;
        int hits = 0;
        using CrowdQuery crowd = CrowdIndex.Near((from + to) * 0.5f, length * 0.5f + reach * PercussionQueryMargin);
        foreach (Node node in crowd.Targets)
        {
            if (node is not Enemy { IsActive: true, IsDying: false } enemy || !IsInstanceValid(enemy)
                || Iso.GroundDistanceToSegment(enemy.GlobalPosition, from, to) > reach
                || !_percussed.Add(enemy.GetInstanceId()))
                continue;
            if (enemy.Percuss(PushDirection(enemy, from, to, direction, config.PercussionForwardBias), distance))
                hits++;
        }
        if (hits == 0)
            return;
        Percussions += hits;
        if (_percussionLanded)
            return;
        _percussionLanded = true;
        AudioManager.Play(config.PercussionAudio, 0.06f, -5f, 1.1f);
        Streak.Refresh("percussion");
    }

    /// <summary>Vers l'extérieur du trajet, plus une part dans le sens du dash ; une créature pile sur le trajet part d'un côté fixe.</summary>
    private static Vector2 PushDirection(Enemy enemy, Vector2 from, Vector2 to, Vector2 direction, float forwardBias)
    {
        Vector2 segment = to - from;
        float t = Mathf.Clamp((enemy.GlobalPosition - from).Dot(segment) / segment.LengthSquared(), 0f, 1f);
        Vector2 away = enemy.GlobalPosition - (from + segment * t);
        Vector2 side = away.LengthSquared() > 1f
            ? away.Normalized()
            : direction.Orthogonal() * (enemy.GetInstanceId() % 2 == 0 ? 1f : -1f);
        return (side + direction * forwardBias).Normalized();
    }
}
