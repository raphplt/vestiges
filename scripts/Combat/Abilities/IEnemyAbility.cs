using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat.Abilities;

/// <summary>
/// Capacité composée sur un Enemy, décrite par le bloc "abilities" de son JSON.
/// Une instance est créée une fois par ennemi et reconfigurée à chaque sortie du pool.
/// </summary>
public interface IEnemyAbility
{
    /// <summary>true si l'attaque de base du type (contact ou projectile) est remplacée par la capacité.</summary>
    bool ReplacesBaseAttack { get; }

    void Configure(EnemyAbilityData data);

    /// <summary>Retourne true si la capacité pilote la vélocité de l'ennemi pendant ce tick.</summary>
    bool Process(Enemy owner, Player player, float distToPlayer, float delta);

    /// <summary>Interrompt l'action en cours sans effet (mort, retour au pool, sortie de l'arbre).</summary>
    void Cancel();
}
