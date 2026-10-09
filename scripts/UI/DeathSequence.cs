using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Mort du joueur (plan 02 M1), avant le bilan. Deux temps enchaînés : l'impact (quasi-gel, secousse, sons du monde
/// assourdis) puis l'effacement (ralenti, caméra qui se resserre, le joueur se défait en éclats, puis l'Effacement part
/// de là où il se tenait et décolore l'écran). La créature qui a porté le coup est redessinée au-dessus de l'effacement,
/// cernée de clair, avec ses couleurs ; elle se défait la dernière.
/// Le monde est ensuite figé et <see cref="Finished"/> passe la main au bilan.
/// Tout se compte en secondes réelles : Engine.TimeScale ralentit le monde, pas la séquence.
/// Un appui, après un court délai, la termine ; aucun appui ne passe au monde ni au menu de pause pendant qu'elle joue.
/// </summary>
public partial class DeathSequence : CanvasLayer
{
    [Signal] public delegate void FinishedEventHandler();

    private const float ImpactSec = 0.14f;
    private const float ImpactTimeScale = 0.03f;
    private const float SlowTimeScale = 0.22f;
    private const float SlowRampSec = 0.35f;
    private const float ZoomSec = 2f;
    private const float ZoomFactor = 1.45f;
    private const float ShakeSec = 0.5f;
    private const float ShakePx = 7f;
    private const float MuffleSec = 0.6f;
    private const float WaveStart = 0.7f;
    private const float WaveEnd = 2.3f;
    // Rayon final du front, en demi-diagonales d'écran : les lobes du contour rentrent aussi dans les coins.
    private const float WaveReach = 1.15f;
    // Le joueur se défait en éclats avant que l'Effacement ne parte de sa place.
    private const float DissolveStart = 0.2f;
    private const float DissolveEnd = 1f;
    private const float MotesInterval = 0.07f;
    private const float KillerReleaseStart = 2.35f;
    private const float KillerReleaseSec = 0.45f;
    private const float KillerSearchPx = 900f;
    // Des pieds au centre de la silhouette du joueur, en pixels du monde.
    private const float PlayerLift = 14f;
    // Derrière le front, le monde pâlit encore à la fin, sans jamais disparaître sous le blanc.
    private const float WashStart = 0.45f;
    private const float WashEnd = 0.7f;
    private const float FadeStart = 2.2f;
    private const float EndSec = 2.9f;
    private const float SkipAfter = 0.4f;
    private const float UiFadeSec = 0.35f;
    private static readonly Color KillerOutline = new(0.96f, 0.94f, 0.92f);

    private EventBus _eventBus;
    private Camera2D _camera;
    private Node[] _cameraDrivers = System.Array.Empty<Node>();
    private ColorRect _overlay;
    private ShaderMaterial _material;
    private Player _player;
    private Enemy _killer;
    // Copie du sprite de la créature, dessinée au-dessus de l'effacement ; son matériau est dupliqué.
    private Sprite2D _killerEcho;
    private ShaderMaterial _killerMaterial;
    private string _lastHitBy;
    private Vector2 _baseZoom;
    private double _savedTimeScale = 1.0;
    private ulong _startUsec;
    private float _elapsed;
    private float _nextMote;
    private bool _running;
    private bool _dissolutionPlayed;
    private readonly RandomNumberGenerator _rng = new();

    /// <summary>La séquence prend la caméra du joueur ; les nœuds qui la pilotent d'habitude (secousse, recul) sont suspendus.</summary>
    public void Setup(Camera2D camera, params Node[] cameraDrivers)
    {
        _camera = camera;
        _cameraDrivers = cameraDrivers;
    }

    public override void _Ready()
    {
        Layer = 45;
        ProcessMode = ProcessModeEnum.Always;
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EntityDied += OnEntityDied;
        _eventBus.PlayerHitBy += OnPlayerHitBy;
        _rng.Randomize();

        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/death_erasure.gdshader") };
        // Bloque aussi la souris : sous le bilan, ce voile reste le fond figé du monde.
        _overlay = new ColorRect { Material = _material, MouseFilter = Control.MouseFilterEnum.Stop };
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_overlay);
        _killerEcho = new Sprite2D { Visible = false, TextureFilter = CanvasItem.TextureFilterEnum.Nearest };
        AddChild(_killerEcho);
        Visible = false;
        SetProcess(false);
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.EntityDied -= OnEntityDied;
            _eventBus.PlayerHitBy -= OnPlayerHitBy;
        }
        if (_running)
        {
            Engine.TimeScale = _savedTimeScale;
            AudioManager.SetWorldMuffle(0f);
        }
    }

    /// <summary>
    /// Efface une couche d'interface de run (HUD, quêtes) au début de la séquence : la mort se joue sans interface.
    /// En temps réel, car le monde est au ralenti.
    /// </summary>
    public static void FadeOutLayer(CanvasLayer layer)
    {
        Tween tween = layer.CreateTween().SetParallel().SetIgnoreTimeScale();
        foreach (Node child in layer.GetChildren())
        {
            if (child is CanvasItem item)
                tween.TweenProperty(item, "modulate:a", 0f, UiFadeSec);
        }
    }

    private void OnPlayerHitBy(string enemyId, float damage) => _lastHitBy = enemyId;

    private void OnEntityDied(Node entity)
    {
        if (entity is not Player player || _running || _camera == null)
            return;

        _player = player;
        _killer = FindKiller(player.GlobalPosition);
        PrepareKillerEcho();
        foreach (Node driver in _cameraDrivers)
            driver.SetProcess(false);
        _baseZoom = _camera.Zoom;
        _savedTimeScale = Engine.TimeScale;
        _material.SetShaderParameter("seed", _rng.RandfRange(0f, 100f));
        _startUsec = Time.GetTicksUsec();
        _elapsed = 0f;
        _nextMote = DissolveStart;
        _dissolutionPlayed = false;
        _running = true;
        Visible = true;
        SetProcess(true);
        Advance(0f);
    }

    /// <summary>La créature la plus proche du type qui a frappé en dernier : le coup fatal vient d'elle ou d'un tir voisin.</summary>
    private Enemy FindKiller(Vector2 around)
    {
        if (string.IsNullOrEmpty(_lastHitBy))
            return null;
        Enemy nearest = null;
        float best = KillerSearchPx * KillerSearchPx;
        using CrowdQuery crowd = CrowdIndex.Near(around, KillerSearchPx);
        foreach (Node node in crowd.Targets)
        {
            if (node is not Enemy enemy || !enemy.IsActive || enemy.IsDying || enemy.EnemyId != _lastHitBy)
                continue;
            float distance = enemy.GlobalPosition.DistanceSquaredTo(around);
            if (distance < best)
            {
                best = distance;
                nearest = enemy;
            }
        }
        return nearest;
    }

    private void PrepareKillerEcho()
    {
        _killerMaterial = _killer?.Sprite?.Material?.Duplicate() as ShaderMaterial;
        _killerEcho.Material = _killerMaterial;
        _killerEcho.Visible = _killerMaterial != null;
        if (_killerMaterial == null)
            return;
        _killerMaterial.SetShaderParameter("outline_enabled", true);
        _killerMaterial.SetShaderParameter("outline_color", KillerOutline);
        _killerMaterial.SetShaderParameter("flash_amount", 0f);
        _killerMaterial.SetShaderParameter("dissolve_amount", 0f);
    }

    public override void _Process(double delta)
    {
        if (!_running)
            return;
        _elapsed = (Time.GetTicksUsec() - _startUsec) / 1_000_000f;
        if (_elapsed >= EndSec)
        {
            Finish();
            return;
        }
        Advance(_elapsed);
    }

    public override void _Input(InputEvent @event)
    {
        if (!_running || !@event.IsPressed() || @event.IsEcho())
            return;
        if (@event is not (InputEventKey or InputEventMouseButton or InputEventJoypadButton))
            return;
        GetViewport().SetInputAsHandled();
        if (_elapsed >= SkipAfter)
            Finish();
    }

    private void Advance(float t)
    {
        float slow = t < ImpactSec
            ? ImpactTimeScale
            : Mathf.Lerp(ImpactTimeScale, SlowTimeScale, Mathf.Clamp((t - ImpactSec) / SlowRampSec, 0f, 1f));
        Engine.TimeScale = _savedTimeScale * slow;

        float zoom = 1f - Mathf.Pow(1f - Mathf.Clamp(t / ZoomSec, 0f, 1f), 3f);
        _camera.Zoom = _baseZoom * Mathf.Lerp(1f, ZoomFactor, zoom);
        float shake = t < ShakeSec ? ShakePx * CombatFxSettings.ScreenShake * Mathf.Pow(1f - t / ShakeSec, 2f) : 0f;
        _camera.Offset = new Vector2(Mathf.Sin(t * 97f), Mathf.Cos(t * 83f)) * shake;

        AudioManager.SetWorldMuffle(Mathf.Clamp(t / MuffleSec, 0f, 1f));
        if (!_dissolutionPlayed && t >= WaveStart)
        {
            _dissolutionPlayed = true;
            AudioManager.PlayUI("sfx_monde_dissolution");
        }

        _player.SetDeathDissolve(Mathf.Clamp((t - DissolveStart) / (DissolveEnd - DissolveStart), 0f, 1f));
        EmitMotes(t);
        ApplyShader(t);
    }

    /// <summary>Le joueur se défait en éclats pâles qui montent, émis par la gerbe commune (aucun nœud créé).</summary>
    private void EmitMotes(float t)
    {
        if (CombatPools.Instance == null)
            return;
        while (t >= _nextMote && _nextMote <= DissolveEnd)
        {
            _nextMote += MotesInterval;
            Vector2 origin = _player.GlobalPosition + new Vector2(_rng.RandfRange(-7f, 7f), -_rng.RandfRange(2f, 26f));
            CombatPools.Instance.EmitSparks(origin, new SparkBurst
            {
                Family = FxFamily.Pale,
                Owner = FxOwner.World,
                Count = 4,
                Direction = Vector2.Up,
                Spread = 1.1f,
                SpeedMin = 140f,
                SpeedMax = 260f,
                LifeMin = 0.5f,
                LifeMax = 0.9f,
                Size = 2,
            });
        }
    }

    private void ApplyShader(float t)
    {
        Viewport viewport = GetViewport();
        Vector2 size = viewport.GetVisibleRect().Size;
        Transform2D toWorld = viewport.CanvasTransform.AffineInverse();
        Vector2 viewOrigin = toWorld * Vector2.Zero;
        Vector2 viewSize = toWorld * size - viewOrigin;
        _material.SetShaderParameter("view_origin", viewOrigin);
        _material.SetShaderParameter("view_size", viewSize);
        _material.SetShaderParameter("origin", _player.GlobalPosition + Vector2.Up * PlayerLift);

        float wave = Mathf.Clamp((t - WaveStart) / (WaveEnd - WaveStart), 0f, 1f);
        _material.SetShaderParameter("radius", wave * wave * WaveReach * viewSize.Length() * 0.5f);
        _material.SetShaderParameter("wash_amount", Mathf.Lerp(WashStart, WashEnd, Mathf.Clamp((t - FadeStart) / (EndSec - FadeStart), 0f, 1f)));

        UpdateKillerEcho(t, viewport.CanvasTransform);
    }

    /// <summary>La copie suit l'image courante et la place à l'écran de la créature, puis se dissout.</summary>
    private void UpdateKillerEcho(float t, Transform2D canvas)
    {
        if (_killerMaterial == null)
            return;
        AnimatedSprite2D sprite = IsInstanceValid(_killer) && _killer.IsActive ? _killer.Sprite : null;
        float release = Mathf.Clamp((t - KillerReleaseStart) / KillerReleaseSec, 0f, 1f);
        _killerEcho.Visible = sprite != null && release < 1f;
        if (!_killerEcho.Visible)
            return;
        _killerEcho.Texture = sprite.SpriteFrames.GetFrameTexture(sprite.Animation, sprite.Frame);
        _killerEcho.Offset = sprite.Offset;
        _killerEcho.Centered = sprite.Centered;
        _killerEcho.FlipH = sprite.FlipH;
        _killerEcho.Modulate = sprite.Modulate;
        _killerEcho.Transform = canvas * sprite.GlobalTransform;
        _killerMaterial.SetShaderParameter("dissolve_amount", release);
    }

    private void Finish()
    {
        _running = false;
        SetProcess(false);
        _player.SetDeathDissolve(1f);
        ApplyShader(EndSec);
        Engine.TimeScale = _savedTimeScale;
        _camera.Offset = Vector2.Zero;
        AudioManager.SetWorldMuffle(0f);
        GetTree().Paused = true;
        EmitSignal(SignalName.Finished);
    }
}
