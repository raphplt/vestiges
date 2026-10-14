using Vestiges.Combat;
using Vestiges.Progression;
using Vestiges.Infrastructure;

namespace Vestiges.Core;

public partial class Player
{
    public OpeningStrike OpeningStrike { get; internal set; }

    /// <summary>Œil d'aigle reporte le jet natif à l'impact ; les autres personnages et répliques le gardent au départ.</summary>
    private bool RollAttackCrit() => (OpeningStrike == null || _isEchoAttack)
        && _critChance > 0f && RunRandom.Combat.Randf() < _critChance;

    /// <summary>Le tirage se fait à l'impact : un projectile en vol ne sait pas encore si sa cible sera indemne.</summary>
    internal void ResolveOpeningStrike(Enemy enemy, ref float damage, ref bool isCrit, ref AttackContext context)
    {
        if (isCrit || damage <= 0f || OpeningStrike?.Roll(enemy, context) != true)
            return;
        isCrit = true;
        damage *= _critMultiplier;
        // Le bonus des Lunettes s'applique aussi à ce critique, sans réappliquer les autres objets à la cible.
        damage = _objectMilestones?.CritAgainst(enemy, damage, true) ?? damage;
        context = context with { ReferenceDamage = context.ReferenceDamage * _critMultiplier };
    }
}
