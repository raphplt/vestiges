using Godot;
using Vestiges.Combat;

namespace Vestiges.Tests;

/// <summary>Orientation visuelle : huit secteurs avec hystérésis, repli historique sur quatre diagonales.</summary>
public partial class MovementRegression
{
    private void RunFacingChecks()
    {
        CharacterFacing facing = new();
        facing.Reset(eightWay: true);
        (Vector2 input, CharacterFacing.Direction expected)[] cases =
        {
            (Vector2.Right, CharacterFacing.Direction.E), (new Vector2(1, 1), CharacterFacing.Direction.SE),
            (Vector2.Down, CharacterFacing.Direction.S), (new Vector2(-1, 1), CharacterFacing.Direction.SW),
            (Vector2.Left, CharacterFacing.Direction.W), (new Vector2(-1, -1), CharacterFacing.Direction.NW),
            (Vector2.Up, CharacterFacing.Direction.N), (new Vector2(1, -1), CharacterFacing.Direction.NE),
        };
        foreach ((Vector2 input, CharacterFacing.Direction expected) in cases)
            Check(facing.Update(input.Normalized()) == expected, $"8 directions : {input} → {expected}");

        facing.Update(Vector2.Right);
        // 25° au-dessous de l'axe : au-delà de la demi-largeur (22,5°) mais dans l'hystérésis.
        Check(facing.Update(Vector2.FromAngle(Mathf.DegToRad(25f))) == CharacterFacing.Direction.E,
            "8 directions : hystérésis près d'une frontière");
        Check(facing.Update(Vector2.FromAngle(Mathf.DegToRad(35f))) == CharacterFacing.Direction.SE,
            "8 directions : changement franc au-delà de l'hystérésis");

        facing.Reset(eightWay: false);
        facing.Update(new Vector2(1, -1).Normalized());
        Check(facing.Update(Vector2.Right) == CharacterFacing.Direction.NE,
            "4 diagonales : un axe pur conserve le côté de la dernière pose");
        Check(facing.Update(Vector2.Left) == CharacterFacing.Direction.NW, "4 diagonales : demi-tour horizontal");
    }
}
