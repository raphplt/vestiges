using Godot;
using Vestiges.Combat;

namespace Vestiges.Core;

/// <summary>Bonus du sac : une même valeur alimente le combat et la fiche d'arme.</summary>
public partial class Player
{
    public float WeaponKnockbackMultiplier { get; private set; } = 1f;
    public float ProjectileSpeedMultiplier { get; private set; } = 1f;
    public float OrbitalSpeedMultiplier { get; private set; } = 1f;
    public float ChainRangeMultiplier { get; private set; } = 1f;
    public float HomingStrengthMultiplier { get; private set; } = 1f;
    public float BonusChainTargets { get; private set; }

    private bool ApplyWeaponItemModifier(string stat, float value, string modifierType)
    {
        switch (stat)
        {
            case "knockback" when modifierType == "multiplicative": WeaponKnockbackMultiplier *= value; return true;
            case "projectile_speed" when modifierType == "multiplicative": ProjectileSpeedMultiplier *= value; return true;
            case "orbital_speed" when modifierType == "multiplicative": OrbitalSpeedMultiplier *= value; return true;
            case "chain_range" when modifierType == "multiplicative": ChainRangeMultiplier *= value; return true;
            case "homing_strength" when modifierType == "multiplicative": HomingStrengthMultiplier *= value; return true;
            case "chain_targets" when modifierType == "additive": BonusChainTargets += value; return true;
            default: return false;
        }
    }

    private float GetModifiedWeaponStat(WeaponInstance weapon, string key)
    {
        float value = weapon.GetStat(key);
        // Les valeurs par défaut du contrat ne rendent pas une arme compatible avec rebonds ou guidage.
        bool declared = weapon.Base.Stats.ContainsKey(key) || weapon.Base.Growth.ContainsKey(key);
        return key switch
        {
            "knockback" => value * WeaponKnockbackMultiplier,
            "projectile_speed" => value * ProjectileSpeedMultiplier,
            "orbital_speed" => value * OrbitalSpeedMultiplier,
            "chain_range" when declared => value * ChainRangeMultiplier,
            "homing_strength" when declared => value * HomingStrengthMultiplier,
            "chain_targets" when declared => value + BonusChainTargets,
            _ => value,
        };
    }

    /// <summary>Le Portrait reprend un instant de l'apparence existante, sans produire de nouvel asset.</summary>
    internal void CopyPortraitAppearance(Sprite2D portrait)
    {
        portrait.Texture = _sprite?.SpriteFrames?.GetFrameTexture(_sprite.Animation, _sprite.Frame);
        portrait.Offset = _sprite?.Offset ?? Vector2.Zero;
        portrait.Scale = _sprite?.Scale ?? Vector2.One;
        portrait.FlipH = _sprite?.FlipH ?? false;
        portrait.TextureFilter = TextureFilterEnum.Nearest;
        if (portrait.Material == null && _entityShader != null)
        {
            ShaderMaterial material = new() { Shader = _entityShader };
            material.SetShaderParameter("outline_enabled", true);
            material.SetShaderParameter("outline_color", PixelPalette.Ramp(FxFamily.Pale).Light);
            portrait.Material = material;
        }
    }
}
