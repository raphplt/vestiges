using Godot;

namespace Vestiges.UI;

/// <summary>
/// Cadenas d'une case d'arme que le Campement n'a pas encore ouverte (plan 36) : anse et corps en pixels pleins, au
/// grain du HUD. Dessiné une fois ; rien ne se redessine tant que la case ne change pas.
/// </summary>
public partial class HudSlotLock : Control
{
    private static readonly Color Metal = new(0.55f, 0.52f, 0.5f);
    private static readonly Color MetalDark = new(0.24f, 0.22f, 0.24f);
    private static readonly Color Keyhole = new(0.08f, 0.07f, 0.1f);

    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    public override void _Draw()
    {
        // 12 × 14 unités, centré dans la case : anse de 8 de large, corps de 12 × 8 et trou de serrure.
        Vector2 origin = (Size - new Vector2(12f, 14f)) * 0.5f;
        origin = new Vector2(Mathf.Round(origin.X), Mathf.Round(origin.Y));
        DrawRect(new Rect2(origin + new Vector2(2f, 0f), new Vector2(8f, 2f)), MetalDark);
        DrawRect(new Rect2(origin + new Vector2(2f, 2f), new Vector2(2f, 4f)), MetalDark);
        DrawRect(new Rect2(origin + new Vector2(8f, 2f), new Vector2(2f, 4f)), MetalDark);
        DrawRect(new Rect2(origin + new Vector2(0f, 6f), new Vector2(12f, 8f)), MetalDark);
        DrawRect(new Rect2(origin + new Vector2(1f, 7f), new Vector2(10f, 6f)), Metal);
        DrawRect(new Rect2(origin + new Vector2(5f, 8f), new Vector2(2f, 3f)), Keyhole);
    }
}
