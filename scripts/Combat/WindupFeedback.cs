using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Annonce vivante d'une attaque de créature (plan 54 G3) : le visuel tremble d'un pixel pendant l'annonce, puis
/// s'étire vers le haut à la détente avant de revenir au repos. Animé par l'ennemi à chaque tick, sans tween ni
/// allocation ; le corps physique ne bouge pas. Un coup reçu (HitFeedback) garde la main sur la position et l'échelle.
/// </summary>
public sealed class WindupFeedback
{
    private const float ShakeStepSec = 0.05f;
    private const float ShakePx = 1f;
    private const float ReleaseSec = 0.14f;
    private static readonly Vector2 ReleaseStretch = new(0.84f, 1.2f);

    private Node2D _target;
    private bool _shaking;
    private float _elapsed;
    private float _release = -1f;

    public bool IsActive => _shaking || _release >= 0f;

    /// <summary>Début de l'annonce : le visuel se met à trembler.</summary>
    public void Begin(Node2D target)
    {
        _target = target;
        _shaking = true;
        _elapsed = 0f;
        _release = -1f;
    }

    /// <summary>Détente de l'attaque : fin du tremblement, étirement bref puis retour élastique.</summary>
    public void Release()
    {
        if (!_shaking || _target == null)
            return;
        _shaking = false;
        _release = 0f;
    }

    /// <summary>Annulation, mort ou retour au pool : le visuel revient au repos sans s'étirer.</summary>
    public void Stop()
    {
        bool wasActive = IsActive;
        _shaking = false;
        _release = -1f;
        if (_target == null || !wasActive)
            return;
        _target.Position = Vector2.Zero;
        _target.Scale = Vector2.One;
    }

    /// <param name="hitActive">Un coup reçu anime déjà le visuel : il reste prioritaire, l'annonce attend.</param>
    public void Tick(float delta, bool hitActive)
    {
        if (_target == null)
            return;
        if (_shaking)
        {
            _elapsed += delta;
            if (!hitActive)
                _target.Position = new Vector2((int)(_elapsed / ShakeStepSec) % 2 == 0 ? ShakePx : -ShakePx, 0f);
            return;
        }
        if (_release < 0f)
            return;
        _release += delta;
        float t = Mathf.Clamp(_release / ReleaseSec, 0f, 1f);
        if (!hitActive)
        {
            _target.Position = Vector2.Zero;
            _target.Scale = ReleaseStretch.Lerp(Vector2.One, 1f - (1f - t) * (1f - t));
        }
        if (t >= 1f)
        {
            _release = -1f;
            if (!hitActive)
                _target.Scale = Vector2.One;
        }
    }
}
