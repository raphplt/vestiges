using Godot;

namespace Vestiges.UI;

/// <summary>Un point par rang possible ; seuls les rangs achetés se remplissent, sans traitement par frame.</summary>
public partial class CampRankPips : Control
{
    private const float DotSize = 16f;
    private const float Spacing = 8f;
    public int Rank { get; private set; }
    public int MaxRank { get; private set; }

    public CampRankPips()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
    }

    public void SetRank(int rank, int maxRank)
    {
        int count = Mathf.Max(0, maxRank);
        int filled = Mathf.Clamp(rank, 0, count);
        if (count == MaxRank && filled == Rank) return;
        Rank = filled;
        MaxRank = count;
        CustomMinimumSize = new Vector2(count * (DotSize + Spacing) - (count > 0 ? Spacing : 0f), DotSize);
        QueueRedraw();
    }

    public override void _Draw()
    {
        for (int i = 0; i < MaxRank; i++)
        {
            Vector2 at = new(i * (DotSize + Spacing), 0f);
            DrawDot(at, UITheme.TextDim.Darkened(0.25f));
            Color fill = i < Rank ? UITheme.GoldBright : UITheme.BgDark.Lightened(0.15f);
            DrawDot(at + Vector2.One * 2f, fill, DotSize - 4f);
        }
    }

    private void DrawDot(Vector2 at, Color color, float size = DotSize)
    {
        // Octogone sur la grille de deux pixels : lisible même après l'échelle du menu en 720p.
        DrawRect(new Rect2(at + new Vector2(4f, 0f), new Vector2(size - 8f, size)), color);
        DrawRect(new Rect2(at + new Vector2(2f, 2f), new Vector2(size - 4f, size - 4f)), color);
        DrawRect(new Rect2(at + new Vector2(0f, 4f), new Vector2(size, size - 8f)), color);
    }
}
