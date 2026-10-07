using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Marques des statuts autour du sprite d'une créature (plan 27 V1b) : étoiles qui tournent au-dessus de la tête
/// (désorientée), gouttes qui tombent (saignement), braises qui montent (brûlure), stries derrière elle quand elle
/// avance ralentie. Information de jeu : jamais coupées par le réglage « Effets d'attaque » ni par le budget.
/// Dessinées en poses clés à cadence fixe, sans interpolation ; le dessin n'est refait qu'au changement de pose ou
/// d'état. Nœud enfant de la créature, contre-mis à l'échelle pour rester au pixel sur une variante agrandie.
/// </summary>
public partial class EnemyStatusMarks : Node2D
{
    private const StatusMarks DrawnMarks = StatusMarks.Disoriented | StatusMarks.Bleeding | StatusMarks.Burning | StatusMarks.Slowed;
    private const int PoseCycle = 24;
    /// <summary>Au-dessous, une créature ralentie est considérée à l'arrêt : pas de stries.</summary>
    private const float MovingSpeedSq = 4f;

    private static readonly Dictionary<string, Rect2?> BodyById = new();
    /// <summary>Corps de repli d'une pose sans image lisible.</summary>
    private static readonly Rect2 FallbackBody = new(-6f, -16f, 12f, 16f);

    private Node2D _creature;
    private Rect2 _body;
    private float _clock;
    private int _pose = -1;
    private StatusMarks _marks;
    private bool _moving;
    private Vector2 _trailDirection;
    private int _emberCount;
    private int _seed;

    /// <summary>Milieu du corps, relatif à la créature et à son échelle : les étincelles de brûlure en partent.</summary>
    public Vector2 BodyCenter => _body.GetCenter() * _creature.Scale;

    /// <summary>Corps de la créature (pose de repos), mesuré une fois par espèce ; marques éteintes.</summary>
    public void Configure(string enemyId, AnimatedSprite2D sprite)
    {
        _creature = GetParent<Node2D>();
        _body = MeasureBody(enemyId, sprite) ?? FallbackBody;
        _seed = (int)(_creature.GetInstanceId() % 997);
        Clear();
    }

    public void Clear()
    {
        _marks = StatusMarks.None;
        _pose = -1;
        _clock = 0f;
        Visible = false;
    }

    /// <param name="burnShare">Part des PV max brûlée chaque seconde : nombre de braises.</param>
    public void Tick(float delta, StatusMarks marks, Vector2 velocity, float burnShare)
    {
        StatusVisualConfig config = EnemyStatusVisual.Config();
        StatusMarks allMarks = marks;
        marks &= DrawnMarks;
        bool moving = (marks & StatusMarks.Slowed) != 0 && velocity.LengthSquared() > MovingSpeedSq;
        if (config == null || (marks & ~StatusMarks.Slowed) == 0 && !moving)
        {
            if (Visible)
                Clear();
            return;
        }

        int embers = (marks & StatusMarks.Burning) != 0
            ? Mathf.Clamp(config.EmberMinCount + (int)(burnShare / config.EmberMaxHpSharePerExtra), config.EmberMinCount, config.EmberMaxCount)
            : 0;
        // Figée, la créature garde aussi ses marques sur leur pose.
        if ((allMarks & StatusMarks.Frozen) == 0)
            _clock += delta;
        int pose = (int)(_clock * config.MarkPoseFps) % PoseCycle;
        if (moving)
            _trailDirection = -velocity.Normalized();
        if (pose == _pose && marks == _marks && moving == _moving && embers == _emberCount)
            return;
        _pose = pose;
        _marks = marks;
        _moving = moving;
        _emberCount = embers;
        Scale = Vector2.One / _creature.Scale;
        Visible = true;
        QueueRedraw();
    }

    public override void _Draw()
    {
        StatusVisualConfig config = EnemyStatusVisual.Config();
        if (config == null || _pose < 0)
            return;
        // Corps à l'échelle de la créature, dessiné au pixel dans le repère contre-mis à l'échelle.
        Vector2 scale = _creature.Scale;
        Rect2 body = new(_body.Position * scale, _body.Size * scale);
        if (_moving)
            DrawSlowTrail(body, config.SlowTrailRamp);
        if ((_marks & StatusMarks.Burning) != 0)
            DrawEmbers(body, config.EmberRamp, _emberCount);
        if ((_marks & StatusMarks.Bleeding) != 0)
            DrawDrops(body, config.DropRamp, config.DropCount);
        if ((_marks & StatusMarks.Disoriented) != 0)
            DrawStars(body, config.StarRamp, config.StarCount);
    }

    /// <summary>Étoiles en croix sur une ellipse au-dessus de la tête, un huitième de tour par pose.</summary>
    private void DrawStars(Rect2 body, FxRamp ramp, int count)
    {
        float radiusX = Mathf.Max(6f, body.Size.X * 0.45f);
        Vector2 center = new(body.GetCenter().X, body.Position.Y - 3f);
        for (int star = 0; star < count; star++)
        {
            float angle = (_pose % 8) * Mathf.Tau / 8f + star * Mathf.Tau / count;
            Vector2 at = (center + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusX * 0.35f)).Floor();
            DrawPixel(at + Vector2.Left, ramp.Mid);
            DrawPixel(at + Vector2.Right, ramp.Mid);
            DrawPixel(at + Vector2.Up, ramp.Mid);
            DrawPixel(at + Vector2.Down, ramp.Mid);
            DrawPixel(at, ramp.Light);
        }
    }

    /// <summary>Larmes de sang qui tombent du milieu du corps aux pieds, décalées entre elles, éclaboussure en fin de chute.</summary>
    private void DrawDrops(Rect2 body, FxRamp ramp, int count)
    {
        float fall = Mathf.Max(4f, body.End.Y - body.GetCenter().Y - 3f);
        for (int drop = 0; drop < count; drop++)
        {
            int step = (_pose + drop * 3) % 6;
            float x = Lane(body, drop, count);
            if (step == 5)
            {
                Vector2 splat = new(x - 2f, body.End.Y - 1f);
                DrawPixel(splat, ramp.Mid);
                DrawPixel(splat + new Vector2(3f, 0f), ramp.Mid);
                DrawRect(new Rect2(splat + new Vector2(1f, 1f), new Vector2(2f, 1f)), ramp.Dark);
                continue;
            }
            // Larme : une pointe, puis deux pixels de large, le bas assombri.
            Vector2 at = new(x, Mathf.Floor(body.GetCenter().Y + fall * step / 5f));
            DrawPixel(at, ramp.Mid);
            DrawRect(new Rect2(at + new Vector2(-1f, 1f), new Vector2(2f, 1f)), ramp.Mid);
            DrawPixel(at + new Vector2(-1f, 2f), ramp.Mid);
            DrawPixel(at + new Vector2(0f, 2f), ramp.Dark);
        }
    }

    /// <summary>Braises qui montent du milieu du corps au-dessus de la tête : 2×2 puis un pixel en fin de montée.</summary>
    private void DrawEmbers(Rect2 body, FxRamp ramp, int count)
    {
        float rise = Mathf.Max(6f, body.GetCenter().Y - body.Position.Y + 4f);
        for (int ember = 0; ember < count; ember++)
        {
            int step = (_pose + ember * 2) % 6;
            float x = Lane(body, ember + 1, count + 1) + ((_pose + ember) % 2 == 0 ? 0f : 1f);
            Vector2 at = new(x, Mathf.Floor(body.GetCenter().Y - rise * step / 5f));
            if (step >= 4)
            {
                DrawPixel(at, ramp.Light);
                continue;
            }
            DrawRect(new Rect2(at, new Vector2(2f, 2f)), ramp.Mid);
            DrawPixel(at, ramp.Light);
        }
    }

    /// <summary>Deux stries derrière la créature, qui reculent d'un pixel par pose.</summary>
    private void DrawSlowTrail(Rect2 body, FxRamp ramp)
    {
        Vector2 back = _trailDirection;
        Vector2 side = new(-back.Y, back.X);
        float start = body.Size.X * 0.5f + 2f + _pose % 3;
        for (int line = -1; line <= 1; line += 2)
        {
            Vector2 origin = body.GetCenter() + back * start + side * (line * 3f);
            for (int length = 0; length < 4; length++)
                DrawPixel((origin + back * length).Floor(), length == 0 ? ramp.Light : ramp.Mid);
        }
    }

    /// <summary>Colonne d'une marque dans la largeur du corps, propre à la créature pour qu'une foule ne marche pas au pas.</summary>
    private float Lane(Rect2 body, int index, int count)
    {
        float share = (index + 0.5f + (_seed % 5) * 0.1f) / count;
        return Mathf.Floor(body.Position.X + body.Size.X * (0.2f + 0.6f * (share % 1f)));
    }

    private void DrawPixel(Vector2 at, Color color) => DrawRect(new Rect2(at, Vector2.One), color);

    /// <summary>
    /// Partie opaque de la pose de repos, relative à la créature à l'échelle 1, mesurée une fois par espèce (l'ombre
    /// s'y pose aussi) ; null si la pose n'a pas d'image lisible.
    /// </summary>
    internal static Rect2? MeasureBody(string enemyId, AnimatedSprite2D sprite)
    {
        if (BodyById.TryGetValue(enemyId, out Rect2? cached))
            return cached;
        Rect2? body = null;
        SpriteFrames frames = sprite.SpriteFrames;
        string animation = frames.HasAnimation("SE_idle") ? "SE_idle" : sprite.Animation;
        Texture2D texture = frames.GetFrameCount(animation) > 0 ? frames.GetFrameTexture(animation, 0) : null;
        using Image image = texture?.GetImage();
        if (image != null)
        {
            Rect2I used = image.GetUsedRect();
            // Centré par défaut : le haut de l'image est à -taille/2 du centre, décalé de l'offset du sprite.
            Vector2 topLeft = sprite.Offset - new Vector2(image.GetWidth(), image.GetHeight()) * 0.5f;
            body = new Rect2(topLeft + used.Position, used.Size);
        }
        BodyById[enemyId] = body;
        return body;
    }
}
