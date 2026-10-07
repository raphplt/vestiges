using Godot;
using Vestiges.Combat.Abilities;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Annonce d'une créature qui explose à sa mort (affixe Instable, plan 27 V4) : sous un seuil de PV, le rayon exact de
/// sa détonation s'affiche au sol, mesuré au sol comme ses dégâts, et se remplit à mesure que ses PV baissent. Danger :
/// toujours affiché, seule son opacité se règle. Classe simple possédée par la créature ; l'annonce est créée une fois,
/// position et remplissage écrits par paliers.
/// </summary>
public sealed class EnemyExplosionWarning
{
    private GroundTelegraph _telegraph;
    private bool _shown;

    /// <param name="owner">Créature qui porte l'annonce (l'annonce, détachée, ne suit pas son transform).</param>
    public void Tick(Node2D owner, float radius, float hpRatio)
    {
        StatusVisualConfig config = EnemyStatusVisual.Config();
        if (config == null || radius <= 0f || hpRatio > config.ExplosionWarningHpRatio)
        {
            Hide();
            return;
        }
        if (_telegraph == null)
        {
            _telegraph = new GroundTelegraph { Name = "ExplosionWarning" };
            owner.AddChild(_telegraph);
        }
        if (!_shown)
        {
            _telegraph.ShowCircle(owner.GlobalPosition, radius, config.ExplosionWarningFamily);
            _shown = true;
        }
        _telegraph.GlobalPosition = owner.GlobalPosition;
        _telegraph.SetProgress(1f - hpRatio / config.ExplosionWarningHpRatio);
    }

    public void Hide()
    {
        if (!_shown)
            return;
        _shown = false;
        _telegraph.HideMarker();
    }
}
