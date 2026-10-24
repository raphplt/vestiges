using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.UI;

/// <summary>
/// Carton d'arrivée d'une terre (plan 56 B3) : « Terre 2 » en petit, le lieu de Vaulme en grand, un trait qui s'ouvre
/// dessous et un son. Il vient à chaque terre, départ compris, quand le fondu du chargement s'éclaircit, puis s'efface.
/// Écoute <see cref="EventBus.LandObjectiveChanged"/> : une terre nouvelle l'annonce. Le lieu vient du biome de la terre
/// (<c>place_key</c>). Le temps du carton suit la run : il se fige sous un écran de choix. Au repos, ni dessin ni rappel.
/// Coordonnées en unités de référence 960×540, comme le reste du HUD.
/// </summary>
public partial class LandArrivalCard : Control
{
    // Entre la Série et les bandeaux de quête (QuestToast), qui tombent souvent au passage d'une terre.
    private const float RankBaseline = StreakHud.Bottom + 22f;
    private const float PlaceGap = 30f;
    private const float RuleGap = 11f;
    private const int RankSize = 16;
    private const int PlaceSize = 32;
    private const int RankOutline = 4;
    private const int PlaceOutline = 6;
    private const float RuleHalfWidth = 72f;
    private const float RuleDiamond = 3f;

    // Le fondu du chargement dure près de deux secondes : le carton paraît quand le noir s'éclaircit.
    private const float DelaySeconds = 1.2f;
    private const float FadeInSeconds = 0.5f;
    private const float HoldSeconds = 2.6f;
    private const float FadeOutSeconds = 0.9f;
    private const float RisePixels = 6f;
    private const float ArrivalPitch = 0.8f;
    private const float ArrivalVolumeDb = -4f;

    private static readonly Color Outline = new(0.02f, 0.02f, 0.04f, 0.92f);
    private static readonly Color RankColor = new(0x9E / 255f, 0x94 / 255f, 0x94 / 255f);
    private static readonly Color PlaceColor = new(0xE8 / 255f, 0xE0 / 255f, 0xD4 / 255f);
    private static readonly Color RuleColor = new(0xD4 / 255f, 0xA8 / 255f, 0x43 / 255f);

    private EventBus _eventBus;
    private GameManager _gameManager;
    private Font _font;
    private int _landsToWin;
    private string _arrivalAudio;
    private int _announcedLand = -1;
    private string _rankText = "";
    private string _placeText = "";
    private float _rankWidth;
    private float _placeWidth;
    // Temps depuis l'annonce, délai compris ; négatif au repos.
    private float _age = -1f;
    private readonly Vector2[] _diamondOutline = new Vector2[4];
    private readonly Vector2[] _diamondFill = new Vector2[4];

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _font = UITheme.TitleFont;
        GuardianConfig config = GuardianConfig.Load(out string error);
        if (config == null)
        {
            GD.PushError($"[LandArrivalCard] {error} : pas de carton d'arrivée");
            SetProcess(false);
            return;
        }
        _landsToWin = config.LandsToWin;
        _arrivalAudio = config.ArrivalAudio;
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.LandObjectiveChanged += OnObjective;
        SetProcess(false);
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.LandObjectiveChanged -= OnObjective;
    }

    private void OnObjective(int land, int lights, int needed, int stage)
    {
        if (land == _announcedLand)
            return;
        _announcedLand = land;
        _age = 0f;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        float before = _age;
        _age += (float)delta;
        if (before < DelaySeconds && _age >= DelaySeconds)
        {
            // Mort pendant le délai : pas de carton sur l'écran de fin.
            if (_gameManager.CurrentState != GameManager.GameState.Run)
                _age = float.MaxValue;
            else
                Announce(_announcedLand);
        }
        if (_age >= DelaySeconds + FadeInSeconds + HoldSeconds + FadeOutSeconds)
        {
            _age = -1f;
            SetProcess(false);
        }
        QueueRedraw();
    }

    /// <summary>Textes du carton, lus au moment où il paraît : la terre est construite, son biome connu.</summary>
    private void Announce(int land)
    {
        _rankText = land >= _landsToWin ? Tr("LAND_ARRIVAL_LAST") : string.Format(Tr("LAND_ARRIVAL_RANK"), land + 1);
        _placeText = PlaceOf(GetNodeOrNull<WorldSetup>("/root/Main")?.Land);
        _rankWidth = TextWidth(_rankText, RankSize);
        _placeWidth = TextWidth(_placeText, PlaceSize);
        AudioManager.Play(_arrivalAudio, 0f, ArrivalVolumeDb, ArrivalPitch);
        GD.Print($"[LandArrivalCard] {_rankText} · {_placeText}");
    }

    private string PlaceOf(LandSpec land)
    {
        if (land == null || land.BiomeIds.Count == 0)
            return "";
        string key = BiomeDataLoader.Get(land.BiomeIds[0])?.PlaceKey;
        return string.IsNullOrEmpty(key) ? "" : Tr(key);
    }

    public override void _Draw()
    {
        float shown = _age - DelaySeconds;
        if (shown < 0f)
            return;
        float appear = Mathf.Clamp(shown / FadeInSeconds, 0f, 1f);
        float alpha = shown < FadeInSeconds + HoldSeconds
            ? appear
            : 1f - Mathf.Clamp((shown - FadeInSeconds - HoldSeconds) / FadeOutSeconds, 0f, 1f);
        float ease = 1f - (1f - appear) * (1f - appear);
        float center = Size.X / 2f;
        float rise = Mathf.Round((1f - ease) * RisePixels);

        DrawText(_rankText, _rankWidth, RankSize, RankOutline, new Vector2(center, RankBaseline + rise), RankColor with { A = alpha });
        DrawText(_placeText, _placeWidth, PlaceSize, PlaceOutline, new Vector2(center, RankBaseline + PlaceGap + rise),
            PlaceColor with { A = alpha });

        // Le trait s'ouvre du centre, un losange à chaque bout.
        float half = Mathf.Round(RuleHalfWidth * ease);
        float y = RankBaseline + PlaceGap + RuleGap;
        Color rule = RuleColor with { A = alpha * 0.85f };
        DrawRect(new Rect2(center - half, y - 1f, half * 2f, 3f), Outline with { A = Outline.A * alpha });
        DrawRect(new Rect2(center - half, y, half * 2f, 1f), rule);
        DrawDiamond(new Vector2(center - half, y + 0.5f), rule, alpha);
        DrawDiamond(new Vector2(center + half, y + 0.5f), rule, alpha);
    }

    private void DrawDiamond(Vector2 at, Color color, float alpha)
    {
        FillDiamond(_diamondOutline, at, RuleDiamond + 1f);
        DrawColoredPolygon(_diamondOutline, Outline with { A = Outline.A * alpha });
        FillDiamond(_diamondFill, at, RuleDiamond);
        DrawColoredPolygon(_diamondFill, color);
    }

    private static void FillDiamond(Vector2[] points, Vector2 at, float radius)
    {
        points[0] = at + new Vector2(0f, -radius);
        points[1] = at + new Vector2(radius, 0f);
        points[2] = at + new Vector2(0f, radius);
        points[3] = at + new Vector2(-radius, 0f);
    }

    private void DrawText(string text, float width, int size, int outline, Vector2 baseline, Color color)
    {
        if (text.Length == 0)
            return;
        Vector2 position = new(Mathf.Round(baseline.X - width / 2f), baseline.Y);
        DrawStringOutline(_font, position, text, HorizontalAlignment.Left, -1, size, outline, Outline with { A = Outline.A * color.A });
        DrawString(_font, position, text, HorizontalAlignment.Left, -1, size, color);
    }

    private float TextWidth(string text, int size) =>
        text.Length == 0 ? 0f : _font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
}
