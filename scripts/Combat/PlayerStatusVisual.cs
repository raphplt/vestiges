using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Ce qui ralentit le joueur se voit sur lui (plan 27 V3b) : la toile de la Tisseuse tisse des fils sur son sprite
/// (même canal que les fêlures des créatures) et allume une icône près de sa jauge ; l'Effacement qui le ralentit le
/// fait pâlir (la toile reste prioritaire) et lui fait laisser une poussière pâle quand il marche. Le matériau et la jauge ne sont écrits qu'au changement d'état.
/// Classe simple possédée par le joueur.
/// </summary>
public sealed class PlayerStatusVisual
{
    private static readonly StringName StatusTintParam = "status_tint";
    private static readonly StringName CrackColorParam = "crack_color";
    private static readonly StringName CrackSpacingParam = "crack_spacing";
    private static readonly Vector2 FeetOffset = new(0f, -2f);

    private ShaderMaterial _material;
    private System.Action<bool> _showWebIcon;
    private bool _webShown;
    private bool _erasureShown;
    private float _dustTimer;

    /// <param name="showWebIcon">Allume ou éteint l'icône de toile près de la jauge.</param>
    public void Attach(ShaderMaterial material, System.Action<bool> showWebIcon)
    {
        _material = material;
        _showWebIcon = showWebIcon;
        _webShown = false;
        _erasureShown = false;
    }

    public void Update(float delta, bool webbed, bool erasureSlowed, bool moving, Vector2 position)
    {
        PlayerFeedbackConfig config = PlayerFeedbackConfig.Get();
        if (config == null)
            return;
        if (webbed != _webShown || erasureSlowed != _erasureShown)
        {
            if (webbed != _webShown)
                _showWebIcon?.Invoke(webbed);
            _webShown = webbed;
            _erasureShown = erasureSlowed;
            if (_material != null)
            {
                _material.SetShaderParameter(StatusTintParam, webbed ? config.WebTint : erasureSlowed ? config.ErasureTint : Colors.Transparent);
                _material.SetShaderParameter(CrackColorParam, config.WebThreadColor);
                _material.SetShaderParameter(CrackSpacingParam, webbed ? config.WebThreadSpacingPx : 0);
            }
        }

        if (!erasureSlowed || !moving)
        {
            _dustTimer = 0f;
            return;
        }
        _dustTimer -= delta;
        if (_dustTimer > 0f || CombatPools.Instance == null)
            return;
        _dustTimer = config.ErasureInterval;
        // Monde et non attaque : soumis au seul réglage « Particules ».
        CombatPools.Instance.Sparks.Emit(position + FeetOffset, new SparkBurst
        {
            Family = config.ErasureFamily,
            Owner = FxOwner.World,
            Count = 4,
            Direction = Vector2.Up,
            Spread = 2.4f,
            SpeedMin = 3f,
            SpeedMax = 9f,
            LifeMin = 0.6f,
            LifeMax = 0.9f,
            Size = 2,
        });
    }
}
