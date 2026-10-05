using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Cible suivie dans la durée (tir guidé, tir de rafale en attente), pour une seule vie (plan 26 Q8a) : une créature
/// rendue au pool reste un objet Godot valide et peut revenir comme une autre créature. Une cible du groupe qui n'est
/// pas une créature (l'Indicible) reste suivie tant que son nœud vit.
/// </summary>
public readonly struct TargetLock
{
    private readonly Node2D _node;
    private readonly EnemyLife _life;

    private TargetLock(Node2D node, EnemyLife life)
    {
        _node = node;
        _life = life;
    }

    /// <summary>Une cible a été verrouillée, qu'elle soit encore valide ou non.</summary>
    public bool IsSet => _node != null;

    public static TargetLock On(Node2D target) =>
        target is Enemy enemy ? new TargetLock(enemy, enemy.Life) : new TargetLock(target, default);

    /// <summary>La cible verrouillée si elle est toujours la même vie, active et pas mourante.</summary>
    public bool TryGet(out Node2D target)
    {
        target = null;
        if (_node == null || !GodotObject.IsInstanceValid(_node) || _node.IsQueuedForDeletion())
            return false;
        if (_node is Enemy enemy && (!enemy.IsActive || enemy.IsDying || enemy.Life != _life))
            return false;
        target = _node;
        return true;
    }
}
