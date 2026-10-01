using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Écran de chargement (plan 24 B5) : le personnage choisi marche sur une bande de sol en gros pixels. Devant lui, le
/// sol se dessine au rythme du chargement ; derrière, il s'efface en pixels violets. C'est la barre de progression,
/// sans barre. Dessiné sur la grille de l'écran ; aucune allocation par frame.
/// </summary>
public partial class LoadingWalkStrip : Control
{
    private const float Cell = 8f;
    private const int Rows = 7;
    private const float AheadCells = 10f;
    private const float EraseCells = 34f;
    private const float CharacterScale = 3f;
    private const float FollowSpeed = 2.5f;

    private static readonly Color Grass = new(0.42f, 0.55f, 0.30f);
    private static readonly Color GrassLight = new(0.56f, 0.68f, 0.38f);
    private static readonly Color Dirt = new(0.42f, 0.31f, 0.22f);
    private static readonly Color DirtDark = new(0.30f, 0.21f, 0.16f);
    private static readonly Color Edge = new(0.16f, 0.11f, 0.10f);
    private static readonly Color Sketch = new(0.95f, 0.90f, 0.78f);
    private static readonly Color Erased = new(0.42f, 0.31f, 0.63f);

    private float _target;
    private float _shown;
    private float _time;
    private AnimatedSprite2D _walker;
    private float _feet = 48f;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ProcessMode = ProcessModeEnum.Always;
        _walker = new AnimatedSprite2D { TextureFilter = TextureFilterEnum.Nearest, Scale = Vector2.One * CharacterScale };
        AddChild(_walker);
        string characterId = GetNodeOrNull<Vestiges.Core.GameManager>("/root/GameManager")?.SelectedCharacterId ?? "vagabond";
        CharacterData data = CharacterDataLoader.Get(characterId) ?? CharacterDataLoader.Get("vagabond");
        SpriteFrames frames = data != null ? CharacterSpriteLoader.LoadOrGet(data.Id, data.SpriteFolder) : null;
        if (frames != null)
        {
            _walker.SpriteFrames = frames;
            string animation = frames.HasAnimation("E_walk") ? "E_walk" : "SE_walk";
            if (frames.HasAnimation(animation))
            {
                _walker.Play(animation);
                // Sprite centré : ses pieds sont à une demi-hauteur sous son centre, posés sur l'herbe.
                _feet = frames.GetFrameTexture(animation, 0).GetHeight() * CharacterScale / 2f - CharacterScale * 2f;
            }
        }
    }

    /// <summary>Part du chargement faite, de 0 à 1 ; elle ne recule jamais.</summary>
    public void SetProgress(float progress) => _target = Mathf.Max(_target, Mathf.Clamp(progress, 0f, 1f));

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _time += dt;
        _shown = Mathf.Lerp(_shown, _target, 1f - Mathf.Exp(-FollowSpeed * dt));
        float top = Mathf.Floor(Size.Y * 0.5f / Cell) * Cell;
        _walker.Position = new Vector2(Mathf.Floor(WalkerX() / CharacterScale) * CharacterScale, top - _feet);
        QueueRedraw();
    }

    private float WalkerX() => Mathf.Lerp(Size.X * 0.12f, Size.X * 0.88f, _shown);

    public override void _Draw()
    {
        int columns = Mathf.CeilToInt(Size.X / Cell);
        float top = Mathf.Floor(Size.Y * 0.5f / Cell) * Cell;
        float walker = WalkerX() / Cell;
        float frontier = walker + AheadCells;
        float erasedBehind = walker - EraseCells;
        int sketchColumn = Mathf.FloorToInt(frontier);
        for (int x = 0; x < columns; x++)
        {
            if (x > frontier)
                break;
            // Derrière, au-delà de la marge, le sol est oublié ; à la lisière, il part pixel par pixel.
            float behind = (erasedBehind - x) / 8f;
            for (int y = 0; y < Rows; y++)
            {
                float noise = Hash(x, y);
                if (behind > noise)
                {
                    if (behind < noise + 0.35f && Hash(x + (int)(_time * 4f), y) > 0.6f)
                        DrawRect(new Rect2(x * Cell, top + y * Cell, Cell, Cell), Erased with { A = 0.7f });
                    continue;
                }
                Color color = y == 0 ? (noise > 0.55f ? GrassLight : Grass)
                    : y == 1 ? (noise > 0.4f ? Grass : Dirt)
                    : y == Rows - 1 ? Edge
                    : (x + y) % 2 == 0 && noise > 0.5f ? DirtDark : Dirt;
                // Le sol en train de se dessiner : la dernière colonne scintille.
                if (x == sketchColumn && Hash(y, (int)(_time * 10f)) > 0.4f)
                    color = Sketch;
                DrawRect(new Rect2(x * Cell, top + y * Cell, Cell, Cell), color);
            }
        }
    }

    private static float Hash(int x, int y)
    {
        uint h = (uint)(x * 374761393 + y * 668265263);
        h = (h ^ (h >> 13)) * 1274126177u;
        return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
    }
}
