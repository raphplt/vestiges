using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Écran de chargement (plan 24 B5) : le personnage choisi marche sur une bande de sol en gros pixels. Devant lui, le
/// sol se dessine au rythme du chargement ; derrière, il s'efface en pixels violets. Les cinq sols natifs
/// défilent avec la progression, à la même échelle entière que le personnage ; aucune allocation par frame.
/// </summary>
public partial class LoadingWalkStrip : Control
{
    private const int SlicePixels = 4;
    private const float AheadPixels = 28f;
    private const float ErasePixels = 90f;
    private const float CharacterScale = 3f;
    private const float FollowSpeed = 2.5f;

    private static readonly Color Erased = new(0.42f, 0.31f, 0.63f);

    private float _target;
    private float _shown;
    private float _time;
    private AnimatedSprite2D _walker;
    private float _feet = 48f;
    private Texture2D[] _grounds;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ProcessMode = ProcessModeEnum.Always;
        TextureFilter = TextureFilterEnum.Nearest;
        _grounds = ScreenArt.Grounds;
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
        float top = Mathf.Floor(Size.Y * 0.5f / CharacterScale) * CharacterScale;
        _walker.Position = new Vector2(Mathf.Floor(WalkerX() / CharacterScale) * CharacterScale, top - _feet);
        QueueRedraw();
    }

    private float WalkerX() => Mathf.Lerp(Size.X * 0.12f, Size.X * 0.88f, _shown);

    public override void _Draw()
    {
        if (_grounds == null)
            return;
        Texture2D ground = _grounds[Mathf.Min((int)(_shown * _grounds.Length), _grounds.Length - 1)];
        float top = Mathf.Floor(Size.Y * 0.5f / CharacterScale) * CharacterScale;
        float walker = WalkerX() / CharacterScale;
        int frontier = Mathf.Min(Mathf.FloorToInt(walker + AheadPixels), Mathf.CeilToInt(Size.X / CharacterScale));
        float erasedBehind = walker - ErasePixels;
        // Colonnes natives de quatre pixels : répétition exacte du motif, sans interpolation ni texture temporaire.
        int scroll = (int)(_time * 4f) * SlicePixels;
        for (int x = 0; x < frontier; x += SlicePixels)
        {
            float behind = (erasedBehind - x) / 24f;
            float noise = Hash(x, 0);
            if (behind > noise)
            {
                if (behind < noise + 0.35f)
                    DrawRect(new Rect2(x * CharacterScale, top + (int)(noise * 16) * CharacterScale, CharacterScale, CharacterScale), Erased);
                continue;
            }
            int width = Mathf.Min(SlicePixels, frontier - x);
            Rect2 source = new((x + scroll) % ground.GetWidth(), 0, width, ground.GetHeight());
            Rect2 target = new(x * CharacterScale, top, width * CharacterScale, ground.GetHeight() * CharacterScale);
            Color tint = behind > 0 ? Erased.Lerp(Colors.White, 0.5f) : Colors.White;
            DrawTextureRectRegion(ground, target, source, tint);
        }
    }

    private static float Hash(int x, int y)
    {
        uint h = (uint)(x * 374761393 + y * 668265263);
        h = (h ^ (h >> 13)) * 1274126177u;
        return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
    }
}
