using Godot;
using Vestiges.Core;

namespace Vestiges.UI;

/// <summary>
/// Quêtes de run repliées (plan 24 A2) : un sceau de cire par quête, sous la plaque du score, sans texte. L'anneau de
/// huit crans autour du sceau se remplit avec la progression ; une avancée fait pulser le sceau ; une quête remplie le
/// brise en éclats dorés, il reste doré, et une ligne courte passe dessous. Le détail est dans la pause, ou en
/// maintenant « show_quests ». Dessiné en unités du HUD (deux pixels à 1080p), en attendant les sceaux du plan 25 (S6).
/// Ne se redessine que pendant une animation ou à un changement.
/// </summary>
public partial class RunQuestSeals : Control
{
    public const int MaxSeals = 3;
    private const int SealSize = 16;
    private const float Spacing = 6f;
    private const int RingSteps = 8;
    private const float PulseSec = 0.35f;
    private const float ShatterSec = 0.7f;
    private const float ToastSec = 2.6f;
    private const int Shards = 12;

    private static readonly Color[] WaxColors =
    {
        new(0.66f, 0.23f, 0.23f),
        new(0.23f, 0.54f, 0.52f),
        new(0.69f, 0.47f, 0.18f),
    };
    private static readonly Color Gold = new(0.95f, 0.80f, 0.40f);
    private static readonly Color RingOff = new(0f, 0f, 0f, 0.55f);
    private static readonly Color Outline = new(0.04f, 0.03f, 0.05f, 0.9f);

    private readonly ImageTexture[] _wax = new ImageTexture[MaxSeals];
    private ImageTexture _goldWax;
    private readonly float[] _progress = new float[MaxSeals];
    private readonly bool[] _completed = new bool[MaxSeals];
    private readonly bool[] _used = new bool[MaxSeals];
    private readonly float[] _pulse = new float[MaxSeals];
    private readonly float[] _shatter = new float[MaxSeals];
    private EventBus _eventBus;
    private Label _toast;
    private float _toastAge = float.MaxValue;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        TextureFilter = TextureFilterEnum.Nearest;
        for (int i = 0; i < MaxSeals; i++)
            _wax[i] = MakeWax(WaxColors[i]);
        _goldWax = MakeWax(Gold);

        _toast = new Label { MouseFilter = MouseFilterEnum.Ignore, HorizontalAlignment = HorizontalAlignment.Right, Modulate = Colors.Transparent };
        _toast.AddThemeFontSizeOverride("font_size", 10);
        _toast.AddThemeColorOverride("font_color", Gold);
        _toast.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        _toast.AddThemeConstantOverride("outline_size", 3);
        _toast.Position = new Vector2(-220f, SealSize + 4f);
        _toast.Size = new Vector2(220f, 14f);
        AddChild(_toast);

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.RunQuestUpdated += OnQuestUpdated;
    }

    /// <summary>Pause pendant que la touche du détail est tenue : les sceaux reviennent, le détail se replie.</summary>
    public override void _Notification(int what)
    {
        if (what == NotificationPaused)
            Visible = QuestDisplaySettings.Current == QuestDisplaySettings.Mode.Folded;
    }

    public override void _ExitTree()
    {
        _eventBus.RunQuestUpdated -= OnQuestUpdated;
    }

    private void OnQuestUpdated(int index, string name, float progress, bool completed)
    {
        if (index < 0 || index >= MaxSeals)
            return;
        // Une avancée qui allume un cran de plus fait pulser le sceau ; une quête de durée ne pulse pas à chaque seconde.
        bool advanced = _used[index] && Mathf.FloorToInt(progress * RingSteps) > Mathf.FloorToInt(_progress[index] * RingSteps);
        bool finished = completed && !_completed[index];
        _used[index] = true;
        _progress[index] = progress;
        _completed[index] = completed;
        if (finished)
        {
            _shatter[index] = ShatterSec;
            _toast.Text = string.Format(Tr("QUEST_DONE_TOAST"), name);
            _toastAge = 0f;
        }
        else if (advanced)
        {
            _pulse[index] = PulseSec;
        }
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        Visible = QuestDisplaySettings.Current == QuestDisplaySettings.Mode.Folded && !Input.IsActionPressed("show_quests");
        float dt = (float)delta;
        bool animating = false;
        for (int i = 0; i < MaxSeals; i++)
        {
            if (_pulse[i] > 0f || _shatter[i] > 0f)
                animating = true;
            _pulse[i] = Mathf.Max(0f, _pulse[i] - dt);
            _shatter[i] = Mathf.Max(0f, _shatter[i] - dt);
        }
        if (_toastAge < ToastSec)
        {
            _toastAge += dt;
            float fade = Mathf.Clamp((_toastAge - ToastSec + 0.5f) / 0.5f, 0f, 1f);
            _toast.Modulate = new Color(1f, 1f, 1f, Mathf.Min(1f, _toastAge * 5f) * (1f - fade));
        }
        if (animating)
            QueueRedraw();
    }

    public override void _Draw()
    {
        int count = 0;
        for (int i = 0; i < MaxSeals; i++)
            if (_used[i])
                count++;
        // Alignés à droite sur l'origine du nœud, du dernier au premier.
        float x = -count * (SealSize + Spacing) + Spacing;
        for (int i = 0; i < MaxSeals; i++)
        {
            if (!_used[i])
                continue;
            DrawSeal(i, new Vector2(Mathf.Floor(x), 0f));
            x += SealSize + Spacing;
        }
    }

    private void DrawSeal(int index, Vector2 origin)
    {
        bool done = _completed[index];
        float shatter = _shatter[index];
        Vector2 center = origin + new Vector2(SealSize / 2f, SealSize / 2f);

        // Anneau de huit crans, autour du sceau.
        int lit = done ? RingSteps : Mathf.FloorToInt(_progress[index] * RingSteps + 0.0001f);
        for (int k = 0; k < RingSteps; k++)
        {
            float angle = -Mathf.Pi / 2f + Mathf.Tau * k / RingSteps;
            Vector2 p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (SealSize / 2f + 1f);
            Rect2 notch = new(new Vector2(Mathf.Floor(p.X) - 1f, Mathf.Floor(p.Y) - 1f), new Vector2(2f, 2f));
            DrawRect(notch.Grow(0.5f), Outline);
            DrawRect(notch, k < lit ? (done ? Gold : WaxColors[index].Lightened(0.35f)) : RingOff);
        }

        // Pendant le bris, le sceau a disparu : seuls les éclats volent.
        if (shatter > 0f)
        {
            float t = 1f - shatter / ShatterSec;
            for (int s = 0; s < Shards; s++)
            {
                float angle = Mathf.Tau * s / Shards + index;
                float distance = 2f + t * (10f + (s % 3) * 3f);
                Vector2 p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance + new Vector2(0f, t * t * 6f);
                DrawRect(new Rect2(new Vector2(Mathf.Floor(p.X), Mathf.Floor(p.Y)), new Vector2(s % 2 == 0 ? 2f : 1f, 1f)),
                    (s % 3 == 0 ? Colors.White : Gold) with { A = 1f - t });
            }
            if (t < 0.6f)
                return;
        }

        Color tint = _pulse[index] > 0f ? Colors.White.Lerp(new Color(1.6f, 1.6f, 1.6f), _pulse[index] / PulseSec) : Colors.White;
        DrawTexture(done ? _goldWax : _wax[index], origin, tint);
        if (done)
        {
            // Coche gravée dans la cire dorée.
            Color mark = new(0.45f, 0.30f, 0.08f);
            DrawRect(new Rect2(origin + new Vector2(5f, 8f), new Vector2(1f, 1f)), mark);
            DrawRect(new Rect2(origin + new Vector2(6f, 9f), new Vector2(1f, 1f)), mark);
            DrawRect(new Rect2(origin + new Vector2(7f, 8f), new Vector2(1f, 1f)), mark);
            DrawRect(new Rect2(origin + new Vector2(8f, 7f), new Vector2(1f, 1f)), mark);
            DrawRect(new Rect2(origin + new Vector2(9f, 6f), new Vector2(1f, 1f)), mark);
            DrawRect(new Rect2(origin + new Vector2(10f, 5f), new Vector2(1f, 1f)), mark);
        }
    }

    /// <summary>Sceau de cire 16 × 16 en pixels : disque à trois tons éclairé en haut à gauche, bavures, empreinte.</summary>
    private static ImageTexture MakeWax(Color wax)
    {
        Image image = Image.CreateEmpty(SealSize, SealSize, false, Image.Format.Rgba8);
        Color light = wax.Lightened(0.3f);
        Color dark = wax.Darkened(0.35f);
        Color edge = wax.Darkened(0.65f);
        Vector2 center = new(7.5f, 7.5f);
        for (int y = 0; y < SealSize; y++)
        {
            for (int x = 0; x < SealSize; x++)
            {
                Vector2 p = new(x, y);
                float d = p.DistanceTo(center);
                // Bavures : le bord ondule selon l'angle.
                float angle = Mathf.Atan2(y - center.Y, x - center.X);
                float radius = 6.2f + 0.8f * Mathf.Sin(angle * 5f + 0.7f);
                if (d > radius + 0.5f)
                    continue;
                Color c = d > radius - 0.6f ? edge : (x + y < 12 ? light : (x + y > 18 ? dark : wax));
                // Empreinte au centre : un anneau creux.
                float inner = p.DistanceTo(center);
                if (inner > 2.2f && inner < 3.4f)
                    c = dark;
                image.SetPixel(x, y, c);
            }
        }
        return ImageTexture.CreateFromImage(image);
    }
}
