using Vestiges.Combat;

namespace Vestiges.Core;

public partial class Player
{
    private ulong _attackSequence;
    private AttackContext _launchContext;
    private AttackContext _coneContext;

    /// <summary>
    /// Une salve, un arc et une chaîne partagent leur lancement. Un cône entier garde le même identifiant ;
    /// sa référence couvre son émission complète, indépendamment du nombre de frames/cibles. Une entrée en
    /// contact orbitale est une attaque autonome. Les répliques et DOT conservent le lancement d'origine.
    /// </summary>
    public AttackContext BeginAttack(WeaponInstance weapon, float referenceDamage,
        DamageKind kind = DamageKind.DirectWeapon) => new(GetInstanceId(), weapon, ++_attackSequence, kind, referenceDamage);
}
