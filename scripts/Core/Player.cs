using Godot;

namespace Vestiges.Core;

public partial class Player : CharacterBody2D
{
    [Export] public float Speed = 200f;

    public override void _PhysicsProcess(double delta)
    {
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_up", "move_down");

        if (inputDir != Vector2.Zero)
        {
            Vector2 isoDir = CartesianToIsometric(inputDir);
            Velocity = isoDir * Speed;
        }
        else
        {
            Velocity = Vector2.Zero;
        }

        MoveAndSlide();
    }

    /// <summary>
    /// Convertit un vecteur d'input cardinal (ZQSD) en direction isométrique écran.
    /// Z → haut-droite, D → bas-droite, S → bas-gauche, Q → haut-gauche.
    /// </summary>
    private static Vector2 CartesianToIsometric(Vector2 cartesian)
    {
        Vector2 iso = new Vector2(
            cartesian.X - cartesian.Y,
            (cartesian.X + cartesian.Y) * 0.5f
        );
        return iso.Normalized();
    }
}
