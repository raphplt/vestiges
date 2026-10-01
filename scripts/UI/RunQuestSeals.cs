using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Quêtes de run repliées (plan 24 A2) : un sceau de cire par quête, sous la plaque du score, sans texte. L'anneau de
/// huit crans autour du sceau se remplit avec la progression ; une avancée fait pulser le sceau ; une quête remplie le
/// brise en éclats dorés, il reste doré, et une ligne courte passe dessous. Le détail est dans la pause, ou en
/// maintenant « show_quests ». Textures du plan 25 (S6), dessinées en unités du HUD (deux pixels à 1080p).
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
    private readonly Texture2D[][] _wax = new Texture2D[MaxSeals][];
    private Texture2D _goldWax;
    private Texture2D[] _break;
    private static readonly Color Gold = new(0.95f, 0.80f, 0.40f);
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
        const string folder = "res://assets/ui/hud/plan25/";
        string[] colors = { "red", "green", "blue" };
        for (int i = 0; i < MaxSeals; i++)
            _wax[i] = SpriteAtlas.Horizontal(folder + "quest_seal_" + colors[i] + ".png", SealSize, SealSize);
        _goldWax = GD.Load<Texture2D>(folder + "quest_seal_complete.png");
        _break = SpriteAtlas.Horizontal(folder + "quest_seal_break.png", SealSize, SealSize);

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
        int step = done ? RingSteps : Mathf.Clamp(Mathf.FloorToInt(_progress[index] * RingSteps + 0.0001f), 0, RingSteps);
        if (_shatter[index] > 0f)
        {
            float t = 1f - _shatter[index] / ShatterSec;
            int frame = Mathf.Min((int)(t * _break.Length), _break.Length - 1);
            DrawTexture(_break[frame], origin);
            if (t < 0.8f)
                return;
        }
        Color tint = _pulse[index] > 0f ? new Color(1f + _pulse[index] / PulseSec * 0.6f, 1f + _pulse[index] / PulseSec * 0.6f, 1f + _pulse[index] / PulseSec * 0.6f) : Colors.White;
        DrawTexture(done ? _goldWax : _wax[index][step], origin, tint);
    }
}
