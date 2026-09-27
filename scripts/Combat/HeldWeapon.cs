using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Première arme du joueur portée en main (plan 17 lot 2C) : un essai derrière une option désactivée par défaut.
/// Seule l'arme du premier emplacement est montrée, pour que la main ne passe pas d'une arme à l'autre à chaque tir.
/// Le sprite est enfant du sprite du personnage : il suit son recul et son écrasement, et passe derrière lui
/// quand la main droite est du côté caché.
/// </summary>
public sealed class HeldWeapon
{
    private const float LungeSeconds = 0.12f;
    private const float LungePixels = 3f;
    /// <summary>Prise dans le sprite 16×16 : l'objet est dessiné en diagonale, manche en bas à gauche.</summary>
    private static readonly Vector2 Grip = new(5f, 11f);
    private const float HeldSize = 16f;

    /// <summary>Main droite par orientation (ordre de CharacterFacing.Direction) : position par rapport aux pieds
    /// du personnage, arme retournée vers la gauche, main du côté caché.</summary>
    private static readonly (Vector2 Hand, bool Flip, bool Behind)[] Poses =
    {
        (new Vector2(0f, 1f), false, false),   // E : main droite vers la caméra
        (new Vector2(-5f, 1f), false, false),  // SE
        (new Vector2(-7f, 1f), true, false),   // S : main droite à gauche de l'écran
        (new Vector2(-5f, 0f), true, true),    // SW
        (new Vector2(0f, 0f), true, true),     // W : main droite côté caché
        (new Vector2(5f, 0f), true, true),     // NW
        (new Vector2(7f, 0f), false, false),   // N : main droite à droite de l'écran
        (new Vector2(5f, 1f), false, false),   // NE
    };

    private readonly Sprite2D _sprite;
    private WeaponData _shown;
    private Vector2 _lungeDirection;
    private float _lunge;

    public HeldWeapon(Node2D characterSprite)
    {
        _sprite = new Sprite2D
        {
            Name = "HeldWeapon",
            Centered = false,
            Visible = false,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        };
        characterSprite.AddChild(_sprite);
    }

    /// <summary>Coup porté par une arme : seule l'arme tenue donne un élan à la main.</summary>
    public void OnAttack(WeaponData weapon, Vector2 direction)
    {
        if (weapon == null || weapon != _shown || direction == Vector2.Zero)
            return;
        _lungeDirection = direction.Normalized();
        _lunge = LungeSeconds;
    }

    /// <summary>Mort du personnage : l'arme ne survit pas à sa dissolution.</summary>
    public void Hide() => _sprite.Visible = false;

    public void Update(float delta, WeaponData primary, CharacterFacing.Direction facing)
    {
        if (primary == null || !CombatFxSettings.HeldWeapon)
        {
            _sprite.Visible = false;
            return;
        }

        if (primary != _shown)
        {
            _shown = primary;
            _sprite.Texture = string.IsNullOrEmpty(primary.HeldSprite) ? null : GD.Load<Texture2D>($"res://{primary.HeldSprite}");
            _lunge = 0f;
        }
        if (_sprite.Texture == null)
        {
            _sprite.Visible = false;
            return;
        }

        (Vector2 hand, bool flip, bool behind) = Poses[(int)facing];
        _lunge = Mathf.Max(0f, _lunge - delta);
        // Élan arrondi au pixel : l'arme reste sur la grille du personnage.
        Vector2 lunge = (_lungeDirection * LungePixels * (_lunge / LungeSeconds)).Round();
        _sprite.FlipH = flip;
        _sprite.ShowBehindParent = behind;
        _sprite.Offset = new Vector2(flip ? Grip.X - HeldSize : -Grip.X, -Grip.Y);
        _sprite.Position = hand + lunge;
        _sprite.Visible = true;
    }
}
