using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Le vide comme arme (plan 52 E4), allumé avec l'Élan du joueur. La percussion du dash projette la créature ; une
/// créature ordinaire projetée dans le Néant y tombe et compte comme éliminée ; en marchant, toutes longent le bord au
/// lieu d'y entrer. Élan éteint, rien ne change : les créatures ignorent le Néant.
/// </summary>
public partial class Enemy
{
    // Chute : la créature glisse vers le vide en s'enfonçant, rapetisse et s'efface ; lisible au milieu d'une foule.
    private const float VoidFallSeconds = 0.6f;
    private const float VoidFallSlide = 52f;
    private const float VoidFallDrop = 22f;
    private const float VoidFallScale = 0.1f;
    // L'XP d'une chute rejaillit côté terre, à cette distance du bord.
    private const float VoidFallOrbBack = 30f;

    private bool _fallingIntoVoid;
    private Vector2 _voidFallDirection;

    /// <summary>
    /// Projection par la percussion du dash : remplace le recul en cours, sans le plafond des armes. Boss, mini-boss et
    /// parties de boss ne bougent pas ; une variante agrandie recule d'autant moins qu'elle est grande. Vrai si poussée.
    /// </summary>
    public bool Percuss(Vector2 direction, float distance)
    {
        if (_isDying || !IsActive || _isBurrowed || _boss != null || _tier is EnemyTier.Miniboss or EnemyTier.Boss
            || direction == Vector2.Zero)
            return false;
        Vector2 push = direction.Normalized();
        _knockVelocity = push * (distance * KnockbackDecay / Mathf.Max(1f, Scale.X));
        CombatPools.Instance?.EmitKnockbackDust(GlobalPosition, push);
        return true;
    }

    /// <summary>Seule une créature ordinaire tombe : élites, Souverains, boss et créatures terrées s'arrêtent au bord.</summary>
    private bool CanFallIntoVoid => _tier == EnemyTier.Normal && !_mods.IsVariant && _boss == null && !_isBurrowed;

    /// <summary>
    /// Le pas mène du sol au Néant, le vide étant une arme. Une créature déjà dans le vide (née au bord) en sort
    /// librement.
    /// </summary>
    private bool EntersVoid(Vector2 next) =>
        _player != null && _player.VoidIsWeapon && _player.IsVoidAt(next) && !_player.IsVoidAt(GlobalPosition);

    /// <summary>
    /// Pas complet : chute si la créature est projetée vers le vide assez vite (seule la part du recul dans le sens du
    /// pas compte : un recul qui l'éloigne du bord ne la fait pas tomber), sinon elle longe le bord. Vrai si elle tombe.
    /// </summary>
    private bool ResolveVoid(ref Vector2 next)
    {
        if (!EntersVoid(next))
            return false;
        Vector2 step = next - GlobalPosition;
        if (CanFallIntoVoid && step != Vector2.Zero
            && _knockVelocity.Dot(step.Normalized()) > _player.Elan.Config.FallMinKnockSpeed)
        {
            FallIntoVoid(next);
            return true;
        }
        KeepOffVoid(ref next);
        return false;
    }

    /// <summary>Pas simplifié (créature loin du joueur, sans recul) : il longe aussi le bord du vide.</summary>
    private void MoveSimplified(Vector2 step)
    {
        Vector2 next = GlobalPosition + step;
        if (EntersVoid(next))
            KeepOffVoid(ref next);
        GlobalPosition = next;
    }

    /// <summary>Garde l'axe du pas qui reste sur le sol ; sinon la créature s'arrête au bord.</summary>
    private void KeepOffVoid(ref Vector2 next)
    {
        Vector2 alongX = new(next.X, GlobalPosition.Y);
        Vector2 alongY = new(GlobalPosition.X, next.Y);
        next = !_player.IsVoidAt(alongX) ? alongX : !_player.IsVoidAt(alongY) ? alongY : GlobalPosition;
    }

    /// <summary>Point d'où rejaillit une orbe d'XP après une chute : côté terre, jamais au-dessus du vide.</summary>
    private Vector2 VoidFallOrbPosition(Vector2 jitter) => GlobalPosition - _voidFallDirection * VoidFallOrbBack + jitter * 0.5f;

    /// <summary>
    /// Mort par le vide, comptée au joueur : élimination, score et XP comme un coup fatal. Le corps reste au bord, d'où
    /// l'XP rejaillit vers la terre ; seule son image part dans le vide.
    /// </summary>
    private void FallIntoVoid(Vector2 into)
    {
        _fallingIntoVoid = true;
        _voidFallDirection = (into - GlobalPosition).Normalized();
        _lastHitDirection = -_voidFallDirection;
        float hp = _currentHp;
        DamageResult result = DamageResult.Resolve(Life, new AttackContext(_player.GetInstanceId(), null, 0, DamageKind.Void), hp, hp, 0f);
        LoseHp(hp);
        _eventBus.PublishEnemyDamage(result);
        Die(result);
        _eventBus.PublishCreatureFellIntoVoid(GlobalPosition);
    }

    /// <summary>Image de la chute, à la place de la dissolution : la créature glisse dans le vide, rapetisse et s'efface.</summary>
    private void PlayVoidFall()
    {
        PlayNearbyAudio(_player.Elan.Config.FallAudio);
        Tween tween = CreateTween();
        tween.SetParallel();
        tween.TweenProperty(this, "global_position", GlobalPosition + _voidFallDirection * VoidFallSlide + new Vector2(0f, VoidFallDrop),
            VoidFallSeconds).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tween.TweenProperty(this, "scale", Scale * VoidFallScale, VoidFallSeconds).SetEase(Tween.EaseType.In);
        tween.TweenProperty(this, "modulate:a", 0f, VoidFallSeconds);
        tween.Chain().TweenCallback(Callable.From(OnDeathComplete));
    }
}
