using System;
using Godot;
using Vestiges.Core;

namespace Vestiges.UI;

/// <summary>
/// Trajet de l'Essence vers le HUD (plan 02 J3) : à chaque gain localisé, un grain cyan part de l'endroit de la mort et
/// file en courbe vers le compteur, qui pulse à l'arrivée. Le gain est déjà acquis : l'animation ne le porte pas.
/// Un seul nœud dessine tous les grains (tableau fixe, 32 à la fois) ; au-delà, le grain est omis.
/// </summary>
public partial class EssenceFlights : Control
{
    private const int Capacity = 32;
    private const float FlightSec = 0.55f;
    private static readonly Color Core = new(0.62f, 0.93f, 0.93f);
    private static readonly Color Edge = new(0.2f, 0.52f, 0.56f);

    private readonly Vector2[] _from = new Vector2[Capacity];
    private readonly Vector2[] _bend = new Vector2[Capacity];
    private readonly float[] _age = new float[Capacity];
    private readonly RandomNumberGenerator _rng = new();
    private EventBus _eventBus;
    private Func<Vector2> _target;
    private Action _onArrival;
    private int _active;

    /// <summary><paramref name="target"/> : point d'arrivée en coordonnées de ce nœud ; <paramref name="onArrival"/> à chaque grain arrivé.</summary>
    public void Setup(Func<Vector2> target, Action onArrival)
    {
        _target = target;
        _onArrival = onArrival;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        for (int i = 0; i < Capacity; i++)
            _age[i] = -1f;
        _rng.Randomize();
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EssenceGained += OnEssenceGained;
        SetProcess(false);
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.EssenceGained -= OnEssenceGained;
    }

    private void OnEssenceGained(int amount, Vector2 worldPosition)
    {
        if (_target == null || worldPosition == Vector2.Zero)
            return;
        // Monde → écran → repère de ce nœud (le HUD est mis à l'échelle).
        Vector2 screen = GetViewport().GetCanvasTransform() * worldPosition;
        Vector2 local = GetGlobalTransformWithCanvas().AffineInverse() * screen;
        for (int i = 0; i < Capacity; i++)
        {
            if (_age[i] >= 0f)
                continue;
            _from[i] = local;
            // Courbe : le grain s'élève d'abord, décalé au hasard, avant de filer vers le compteur.
            _bend[i] = local + new Vector2(_rng.RandfRange(-40f, 40f), _rng.RandfRange(-70f, -30f));
            _age[i] = 0f;
            _active++;
            SetProcess(true);
            return;
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        for (int i = 0; i < Capacity; i++)
        {
            if (_age[i] < 0f)
                continue;
            _age[i] += dt;
            if (_age[i] < FlightSec)
                continue;
            _age[i] = -1f;
            _active--;
            _onArrival?.Invoke();
        }
        if (_active == 0)
            SetProcess(false);
        QueueRedraw();
    }

    /// <summary>Bézier quadratique départ → courbe → compteur, accélérée vers la fin.</summary>
    private Vector2 PointAt(int index, float t, Vector2 target)
    {
        float eased = t * t;
        Vector2 a = _from[index].Lerp(_bend[index], eased);
        Vector2 b = _bend[index].Lerp(target, eased);
        return a.Lerp(b, eased).Floor();
    }

    public override void _Draw()
    {
        if (_active == 0 || _target == null)
            return;
        Vector2 target = _target();
        for (int i = 0; i < Capacity; i++)
        {
            if (_age[i] < 0f)
                continue;
            float t = _age[i] / FlightSec;
            // Traînée : deux positions un peu en arrière sur la courbe, plus petites.
            Vector2 tail = PointAt(i, Mathf.Max(0f, t - 0.12f), target);
            Vector2 mid = PointAt(i, Mathf.Max(0f, t - 0.06f), target);
            Vector2 head = PointAt(i, t, target);
            DrawRect(new Rect2(tail - Vector2.One, new Vector2(2f, 2f)), Edge);
            DrawRect(new Rect2(mid - Vector2.One, new Vector2(3f, 3f)), Edge);
            DrawRect(new Rect2(head - new Vector2(2f, 2f), new Vector2(5f, 5f)), Edge);
            DrawRect(new Rect2(head - Vector2.One, new Vector2(3f, 3f)), Core);
        }
    }
}
