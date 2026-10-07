using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Chiffres des dégâts sur la durée d'une créature (plan 27 V2a, §72) : brûlure et saignement s'additionnent et sortent
/// en un petit chiffre par statut toutes les 0,5 s ; la part fractionnaire attend le chiffre suivant, le reliquat sort à
/// la mort. Classe simple possédée par la créature, sans allocation.
/// </summary>
public sealed class DamageOverTimeNumbers
{
    private float _burn;
    private float _bleed;
    private float _timer;

    public void Add(StatusKind kind, float damage)
    {
        if (kind == StatusKind.Burn)
            _burn += damage;
        else if (kind == StatusKind.Bleed)
            _bleed += damage;
    }

    public void Tick(float delta, Vector2 position)
    {
        if (_burn <= 0f && _bleed <= 0f)
        {
            _timer = 0f;
            return;
        }
        _timer += delta;
        float interval = EnemyStatusVisual.Config()?.DotNumberInterval ?? 0.5f;
        if (_timer < interval)
            return;
        _timer = 0f;
        Flush(position);
    }

    /// <summary>Affiche la part entière de chaque somme ; la part fractionnaire reste pour le chiffre suivant.</summary>
    public void Flush(Vector2 position)
    {
        _burn = Show(StatusKind.Burn, _burn, position);
        _bleed = Show(StatusKind.Bleed, _bleed, position + new Vector2(0f, -8f));
    }

    public void Clear()
    {
        _burn = _bleed = _timer = 0f;
    }

    /// <summary>Tolérance d'arrondi : trente tics de 1/6 font 5, pas 4,9999.</summary>
    private const float Epsilon = 0.001f;

    private static float Show(StatusKind kind, float total, Vector2 position)
    {
        float whole = Mathf.Floor(total + Epsilon);
        if (whole < 1f)
            return total;
        CombatPools.Instance?.ShowTickNumber(position, whole, kind);
        return Mathf.Max(0f, total - whole);
    }
}
