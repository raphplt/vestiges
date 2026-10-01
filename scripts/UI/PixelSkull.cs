using Godot;

namespace Vestiges.UI;

/// <summary>
/// Petite tête de mort du compteur d'éliminations, dessinée pixel par pixel en unités du HUD (deux pixels à 1080p),
/// en attendant l'icône du plan 25 (S6). Os jauni, orbites sombres, contour noir.
/// </summary>
public partial class PixelSkull : Control
{
    // 7 × 7 : '#' os, 'o' ombre de l'os, '.' orbite ou dents, ' ' vide.
    private static readonly string[] Pattern =
    {
        " ##### ",
        "#######",
        "#..#..#",
        "#..#..#",
        "###.###",
        " #o#o# ",
        " o.o.o ",
    };

    private static readonly Color Bone = new(0.93f, 0.88f, 0.74f);
    private static readonly Color BoneShade = new(0.66f, 0.60f, 0.48f);
    private static readonly Color Hollow = new(0.10f, 0.08f, 0.12f);
    private static readonly Color Outline = new(0f, 0f, 0f, 0.85f);

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = new Vector2(Pattern[0].Length + 2, Pattern.Length + 2);
    }

    public override void _Draw()
    {
        // Contour : chaque pixel plein déborde d'un pixel dans les quatre directions, dessiné d'abord.
        for (int y = 0; y < Pattern.Length; y++)
            for (int x = 0; x < Pattern[y].Length; x++)
                if (Pattern[y][x] != ' ')
                    DrawRect(new Rect2(x, y, 3f, 3f), Outline);
        for (int y = 0; y < Pattern.Length; y++)
        {
            for (int x = 0; x < Pattern[y].Length; x++)
            {
                Color? color = Pattern[y][x] switch
                {
                    '#' => Bone,
                    'o' => BoneShade,
                    '.' => Hollow,
                    _ => null,
                };
                if (color is { } c)
                    DrawRect(new Rect2(x + 1, y + 1, 1f, 1f), c);
            }
        }
    }
}
