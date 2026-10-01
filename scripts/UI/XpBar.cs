using Godot;

namespace Vestiges.UI;

/// <summary>
/// Barre d'XP sur toute la largeur, au bas de l'écran (DECISIONS §40). Dessinée en unités du HUD, qui valent deux pixels
/// à 1080p : chaque trait tombe sur la grille du pixel art. Rail creusé à liseré doré et embouts, remplissage biseauté
/// en trois tons dont le bas est tramé, crans tous les dixièmes et repères dorés aux quarts, reflet qui parcourt le
/// remplissage, tête lumineuse et étincelles au bout. Le remplissage rattrape sa cible en douceur ; une orbe le fait
/// briller, un niveau le fait éclater en blanc avant de repartir de zéro. Aucune allocation par frame.
/// </summary>
public partial class XpBar : Control
{
    public const float BarHeight = 9f;

    private const float CapWidth = 4f;
    private const float FillSpeed = 3.5f;
    private const float ShimmerPeriod = 2.6f;
    private const float ShimmerWidth = 14f;
    private const int Notches = 10;
    private const int Sparks = 4;

    private static readonly Color Track = new(0.03f, 0.035f, 0.06f, 0.94f);
    private static readonly Color TrackInner = new(0.07f, 0.08f, 0.13f, 0.94f);
    private static readonly Color Rim = new(0.86f, 0.69f, 0.30f, 0.9f);
    private static readonly Color RimDark = new(0.33f, 0.24f, 0.09f, 0.9f);
    private static readonly Color CapLight = new(0.95f, 0.80f, 0.42f);
    private static readonly Color CapDark = new(0.45f, 0.33f, 0.12f);
    private static readonly Color FillLight = new(0.72f, 0.96f, 0.91f);
    private static readonly Color FillMid = new(0.37f, 0.77f, 0.77f);
    private static readonly Color FillDark = new(0.20f, 0.50f, 0.58f);
    private static readonly Color Dither = new(0.13f, 0.36f, 0.45f);
    private static readonly Color Notch = new(0f, 0f, 0f, 0.3f);
    private static readonly Color Quarter = new(0.95f, 0.80f, 0.42f, 0.85f);
    private static readonly Color Shimmer = new(1f, 1f, 1f, 0.28f);
    private static readonly Color Head = new(0.96f, 1f, 0.98f);
    private static readonly Color HeadGlow = new(0.72f, 0.96f, 0.91f, 0.35f);

    private float _target;
    private float _shown;
    private float _shimmer;
    private float _time;
    private float _pulse;
    private float _flash;
    private ImageTexture _dither;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
        TextureRepeat = TextureRepeatEnum.Enabled;
        AnchorLeft = 0f;
        AnchorRight = 1f;
        AnchorTop = 1f;
        AnchorBottom = 1f;
        OffsetLeft = 0f;
        OffsetRight = 0f;
        OffsetTop = -BarHeight;
        OffsetBottom = 0f;

        // Trame en damier de deux tons, posée en mosaïque sous le remplissage : le dégradé reste en pixels francs.
        Image checker = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        checker.SetPixel(0, 0, FillDark);
        checker.SetPixel(1, 1, FillDark);
        checker.SetPixel(1, 0, Dither);
        checker.SetPixel(0, 1, Dither);
        _dither = ImageTexture.CreateFromImage(checker);
    }

    /// <summary>Nouvelle part de l'XP du niveau ; <paramref name="gained"/> fait briller la barre.</summary>
    public void SetRatio(float ratio, bool gained)
    {
        _target = Mathf.Clamp(ratio, 0f, 1f);
        if (gained)
            _pulse = 1f;
    }

    /// <summary>Niveau gagné : éclat blanc, puis la barre repart de zéro.</summary>
    public void Flash()
    {
        _flash = 1f;
        _shown = 0f;
        _target = 0f;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _shown = _shown < _target ? Mathf.Min(_target, _shown + Mathf.Max(0.002f, (_target - _shown) * FillSpeed * dt)) : _target;
        _time += dt;
        _shimmer = (_shimmer + dt / ShimmerPeriod) % 1f;
        _pulse = Mathf.Max(0f, _pulse - dt * 4f);
        _flash = Mathf.Max(0f, _flash - dt * 2.5f);
        QueueRedraw();
    }

    public override void _Draw()
    {
        float width = Mathf.Floor(Size.X);
        float height = BarHeight;
        float left = CapWidth;
        float inner = width - CapWidth * 2f;

        // Rail : liseré doré, ombre, fond creusé.
        DrawRect(new Rect2(0f, 0f, width, height), Track);
        DrawRect(new Rect2(0f, 0f, width, 1f), Rim);
        DrawRect(new Rect2(0f, 1f, width, 1f), RimDark);
        DrawRect(new Rect2(left, 2f, inner, height - 2f), TrackInner);

        float fill = Mathf.Floor(inner * _shown);
        if (fill >= 1f)
        {
            float glow = Mathf.Max(_pulse * 0.35f, _flash);
            DrawRect(new Rect2(left, 2f, fill, 1f), FillLight.Lerp(Colors.White, glow));
            DrawRect(new Rect2(left, 3f, fill, 2f), FillMid.Lerp(Colors.White, glow));
            DrawTextureRect(_dither, new Rect2(left, 5f, fill, height - 5f), true, Colors.White.Lerp(new Color(3f, 3f, 3f), glow * 0.5f));

            // Reflet qui traverse le remplissage, en biais d'un pixel par rangée.
            float shimmerX = Mathf.Floor(_shimmer * (fill + ShimmerWidth * 2f) - ShimmerWidth);
            for (int row = 0; row < 3; row++)
            {
                float start = Mathf.Max(0f, shimmerX - row);
                float end = Mathf.Min(fill, shimmerX + ShimmerWidth - row);
                if (end > start)
                    DrawRect(new Rect2(left + start, 2f + row, end - start, 1f), Shimmer);
            }

            // Tête : halo, colonne claire, étincelles qui clignotent au-dessus du bout.
            float headX = left + Mathf.Max(0f, fill - 2f);
            float glowStart = Mathf.Max(left, headX - 4f);
            DrawRect(new Rect2(glowStart, 2f, Mathf.Min(10f, left + inner - glowStart), height - 2f), HeadGlow);
            DrawRect(new Rect2(headX, 2f, 2f, height - 2f), Head.Lerp(Colors.White, _pulse));
            for (int i = 0; i < Sparks; i++)
            {
                float phase = Mathf.PosMod(_time * (1.3f + i * 0.37f) + i * 0.61f, 1f);
                if (phase > 0.55f)
                    continue;
                float x = headX - 2f - Mathf.Floor(phase * 12f) + i * 2f;
                float y = 2f + ((i * 3 + (int)(_time * 9f)) % (int)(height - 3f));
                if (x > left && x < left + fill)
                    DrawRect(new Rect2(x, y, 1f, 1f), new Color(1f, 1f, 1f, 1f - phase * 1.6f));
            }
        }

        // Crans tous les dixièmes, repères dorés aux quarts sur le liseré.
        for (int i = 1; i < Notches; i++)
            DrawRect(new Rect2(left + Mathf.Floor(inner * i / Notches), 3f, 1f, height - 3f), Notch);
        for (int i = 1; i < 4; i++)
            DrawRect(new Rect2(left + Mathf.Floor(inner * i / 4f) - 1f, 0f, 3f, 2f), Quarter);

        // Embouts dorés en biseau.
        DrawCap(0f);
        DrawCap(width - CapWidth);

        if (_flash > 0f)
            DrawRect(new Rect2(0f, 0f, width, height), new Color(1f, 1f, 1f, _flash * 0.5f));
    }

    private void DrawCap(float x)
    {
        DrawRect(new Rect2(x, 0f, CapWidth, BarHeight), CapDark);
        DrawRect(new Rect2(x + 1f, 1f, CapWidth - 2f, BarHeight - 2f), CapLight);
        DrawRect(new Rect2(x + 1f, 1f, 1f, BarHeight - 2f), Colors.White with { A = 0.5f });
    }
}
