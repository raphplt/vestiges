using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Combat.Abilities;
using Vestiges.Core;

namespace Vestiges.Tests;

/// <summary>
/// Esquive du bot de mesure (plan 42 M0d) : il ne lit que ce qu'un joueur voit à l'écran, les tirs ennemis en vol et
/// les annonces de coup au sol (disques et couloirs). Chaque menace est remarquée ou non (attention), puis seulement
/// après un temps de réaction ; le bot s'écarte alors de la trajectoire ou sort de la zone, et dashe si marcher ne
/// suffit plus. Les réglages sont des ordres de grandeur humains, pas un calage sur un joueur :
/// - débutant : réagit en 0,5 s, remarque une menace sur trois, ne dashe jamais ;
/// - moyen : 0,35 s, deux sur trois, dashe seulement sous 40 % des PV ;
/// - fort : 0,22 s, presque toutes, dashe dès que marcher ne suffit plus.
/// Le bot historique (sans --player-profile) n'esquive pas.
/// </summary>
public partial class RunObservation
{
    private sealed record DodgeSkill(double ReactionSeconds, float Attention, bool DashToDodge, bool DashOnlyWhenLow);

    // Horizon d'un tir : au-delà, sa trajectoire n'est pas encore une menace.
    private const float ProjectileHorizonSeconds = 1.2f;
    // Marge autour du corps : on s'écarte un peu plus que le strict nécessaire.
    private const float DodgeMarginPx = 10f;
    private const float ThreatWeight = 3f;
    // Temps de sortie à pied au-delà duquel un tir ou une zone justifie un dash.
    private const float DashWhenEscapeSeconds = 0.3f;
    private const float LowHpRatio = 0.4f;

    private readonly Dictionary<ulong, (double NoticeAt, bool Noticed)> _threats = new();
    private readonly HashSet<ulong> _threatsPresent = new();
    private readonly List<ulong> _threatsGone = new();
    private int _threatsDodged;

    private DodgeSkill Skill => _profile switch
    {
        BotProfile.Beginner => new DodgeSkill(0.5, 0.35f, false, false),
        BotProfile.Average => new DodgeSkill(0.35, 0.65f, true, true),
        BotProfile.Strong => new DodgeSkill(0.22, 0.95f, true, false),
        _ => null,
    };

    /// <summary>
    /// Direction de marche corrigée par les menaces remarquées ; <paramref name="direction"/> est celle du point de
    /// passage (nulle à l'arrêt). Demande un dash si la menace la plus pressante ne s'évite plus à pied.
    /// </summary>
    private Vector2 DodgeThreats(Vector2 direction, double time, Rect2 view)
    {
        DodgeSkill skill = Skill;
        if (skill == null)
            return direction;
        Vector2 position = _player.GlobalPosition;
        float speed = Mathf.Max(1f, _player.Speed);
        Vector2 away = Vector2.Zero;
        float worstEscape = 0f;
        _threatsPresent.Clear();

        float reach = _player.BodyRadius + DodgeMarginPx;
        foreach (EnemyProjectile projectile in EnemyProjectile.InFlight)
        {
            if (projectile.IsSpent || !view.HasPoint(projectile.GlobalPosition) || !Noticed(projectile.GetInstanceId(), time, skill))
                continue;
            Vector2 toPlayer = position - projectile.GlobalPosition;
            float along = toPlayer.Dot(projectile.Direction);
            float arrival = along / projectile.Speed;
            if (arrival < 0f || arrival > ProjectileHorizonSeconds)
                continue;
            Vector2 offset = toPlayer - projectile.Direction * along;
            float miss = offset.Length();
            float needed = projectile.HitRadius + reach;
            if (miss >= needed)
                continue;
            Vector2 side = miss > 0.5f ? offset / miss : projectile.Direction.Orthogonal();
            // Plus le tir est proche d'arriver, plus il pèse.
            away += side * (1f / (arrival + 0.1f));
            worstEscape = Mathf.Max(worstEscape, (needed - miss) / speed - arrival);
        }

        foreach (GroundTelegraph marker in GroundTelegraph.Shown)
        {
            // Un marqueur réutilisé annonce un nouveau coup à chaque génération : attention et réaction repartent de zéro.
            ulong key = marker.GetInstanceId() ^ ((ulong)marker.Generation << 48);
            if (!marker.IsVisibleInTree() || !view.HasPoint(marker.Origin) || !Noticed(key, time, skill))
                continue;
            // Repère au sol, comme le dessin du marqueur ; la marge du corps y est comptée telle quelle.
            Vector2 ground = Iso.ToGround(position - marker.Origin);
            Vector2 exit;
            float depth;
            if (marker.IsLane)
            {
                Vector2 closest = Geometry2D.GetClosestPointToSegment(ground, Vector2.Zero, marker.GroundEnd);
                Vector2 offset = ground - closest;
                depth = marker.GroundSize * 0.5f + reach - offset.Length();
                exit = offset.Length() > 0.5f ? offset.Normalized() : marker.GroundEnd.Normalized().Orthogonal();
            }
            else
            {
                depth = marker.GroundSize + reach - ground.Length();
                exit = ground.Length() > 0.5f ? ground.Normalized() : Vector2.Right;
            }
            if (depth <= 0f)
                continue;
            away += Iso.ToScreen(exit).Normalized() * 2f;
            worstEscape = Mathf.Max(worstEscape, depth / speed);
        }

        foreach (ulong id in _threats.Keys)
            if (!_threatsPresent.Contains(id))
                _threatsGone.Add(id);
        foreach (ulong id in _threatsGone)
            _threats.Remove(id);
        _threatsGone.Clear();

        if (away == Vector2.Zero)
            return direction;
        bool canDash = skill.DashToDodge && (!skill.DashOnlyWhenLow || _player.CurrentHp < _player.EffectiveMaxHp * LowHpRatio);
        if (canDash && worstEscape > DashWhenEscapeSeconds && _player.Mobility.SecondsSinceDash >= _player.Mobility.Config.CooldownSeconds)
            _player.Mobility.Request();
        _threatsDodged++;
        Vector2 steered = (direction + away.Normalized() * ThreatWeight).Normalized();
        return steered == Vector2.Zero ? away.Normalized() : steered;
    }

    /// <summary>Une menace vue pour la première fois est remarquée ou non (une fois pour toutes), puis après la réaction.</summary>
    private bool Noticed(ulong id, double time, DodgeSkill skill)
    {
        _threatsPresent.Add(id);
        if (!_threats.TryGetValue(id, out (double NoticeAt, bool Noticed) threat))
        {
            threat = (time + skill.ReactionSeconds, _profileRng.Randf() < skill.Attention);
            _threats[id] = threat;
        }
        return threat.Noticed && time >= threat.NoticeAt;
    }
}
