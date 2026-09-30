using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Lieux de la carte pendant --density (plan 22, lot C0) : coffres, Mémoriaux et Failles croisés (entrés dans le cadre)
/// et visités (atteints), micro-événements lancés, Essence gagnée et dépensée.
/// Avec --visit, le bot fait comme un joueur qui ratisse : il se détourne vers le lieu vu le plus proche (700 px), ouvre
/// les coffres, ravive les Mémoriaux puis, s'il en recroise un éveillé, y achète le premier service qu'il peut payer
/// (un achat par passage) ; il n'ouvre pas les Failles, qui changeraient le Péril de la mesure.
/// « Visité » : un lieu encore utile atteint à portée d'interaction, qu'il soit utilisé ou non.
/// </summary>
public partial class RunObservation
{
    private const float PlaceDetourRange = 700f;
    private const float PlaceReachRange = 36f;
    private const double PlaceGiveUpSeconds = 12.0;
    private const double PlaceHoldSeconds = 2.5;

    private bool _visitPlaces;
    private PlaceTracker _placeTracker;

    private async Task CapturePlaces()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        ProcessMode = ProcessModeEnum.Always;
        await Frames(90);
        SmallPlaceDirector director = _world.GetNode<SmallPlaceDirector>("SmallPlaceDirector");
        // Un maintien s'annule dès que le joueur bouge : le bot reste immobile pendant toute la capture.
        _player.AIInputOverride = Vector2.Zero;
        HashSet<string> done = new();
        foreach (SmallPlace place in director.Places)
        {
            if (!done.Add(place.Data.Id))
                continue;
            _player.GlobalPosition = place.GlobalPosition + new Vector2(-30f, 20f);
            _camera.ResetSmoothing();
            await Frames(40);
            SaveFrame($"place-{place.Data.Id}-near");
            _player.AITriggerInteract();
            // Le maintien se compte en temps de jeu : 180 frames couvrent le plus long (1 s) même à 150 images par seconde.
            await Frames(180);
            SaveFrame($"place-{place.Data.Id}-used");
            GD.Print($"[RunObservation] lieu {place.Data.Id} : utilisé={place.Used}");
        }
        GD.Print($"[RunObservation] RESULT places captured={done.Count} dossier={_output}");
    }

    private sealed class PlaceTracker
    {
        private readonly HashSet<ulong> _seen = new();
        private readonly HashSet<ulong> _visited = new();
        private readonly HashSet<ulong> _abandoned = new();
        private readonly Dictionary<ulong, int> _interactions = new();
        private readonly Dictionary<string, int> _seenByKind = new();
        private readonly Dictionary<string, int> _visitedByKind = new();
        private readonly List<(Node2D Node, string Kind)> _places = new();
        private double _firstSeen = -1;
        private Node2D _target;
        private string _targetKind;
        private double _targetSince;
        private double _holdUntil = -1;
        private int _lastEssence;
        private int _frameDelta;
        private ulong _deltaFrame;
        private bool _purchaseAllowed;

        public int Events { get; set; }
        public int EssenceGained { get; private set; }
        public int EssenceSpent { get; private set; }
        /// <summary>Essence gagnée jusqu'ici, gain de la frame en cours compris (colonne du CSV de densité).</summary>
        public int EssenceGainedSoFar => EssenceGained + System.Math.Max(0, _frameDelta);

        public PlaceTracker(int essence) => _lastEssence = essence;

        /// <summary>
        /// Variation de l'Essence, cumulée par frame : un achat raté et remboursé dans la même frame ne compte ni
        /// comme dépense ni comme gain.
        /// </summary>
        public void OnEssence(int amount)
        {
            ulong frame = Engine.GetProcessFrames();
            if (frame != _deltaFrame)
                FlushEssence();
            _deltaFrame = frame;
            _frameDelta += amount - _lastEssence;
            _lastEssence = amount;
        }

        public void FlushEssence()
        {
            if (_frameDelta > 0)
                EssenceGained += _frameDelta;
            else
                EssenceSpent -= _frameDelta;
            _frameDelta = 0;
        }

        /// <summary>--visit : un seul achat par Mémorial visité ; les écrans de services suivants se referment.</summary>
        public bool TakePurchase()
        {
            bool allowed = _purchaseAllowed;
            _purchaseAllowed = false;
            return allowed;
        }

        /// <summary>Lieux entrés dans le cadre ; <paramref name="chests"/> est le groupe déjà lu par la mesure des coffres.</summary>
        public void Sample(double t, Rect2 view, Godot.Collections.Array<Node> chests, SmallPlaceDirector smallPlaces)
        {
            _places.Clear();
            if (smallPlaces != null)
                foreach (SmallPlace place in smallPlaces.Places)
                    _places.Add((place, place.Data.Id));
            foreach (Node node in chests)
                if (node is Chest chest)
                    _places.Add((chest, "chest"));
            foreach (Memorial memorial in Memorial.All)
                _places.Add((memorial, "memorial"));
            foreach (Rift rift in Rift.All)
                _places.Add((rift, "rift"));

            foreach ((Node2D node, string kind) in _places)
            {
                ulong id = node.GetInstanceId();
                if (view.HasPoint(node.GlobalPosition) && _seen.Add(id))
                {
                    _seenByKind[kind] = _seenByKind.GetValueOrDefault(kind) + 1;
                    if (_firstSeen < 0)
                        _firstSeen = t;
                }
            }
        }

        /// <summary>
        /// À chaque frame : un lieu encore utile (coffre fermé, Mémorial à raviver ou à servir, Faille ouverte) atteint
        /// à portée d'interaction compte comme visité, que le bot l'utilise ou non.
        /// </summary>
        public void CheckReach(Vector2 player)
        {
            foreach ((Node2D node, string kind) in _places)
                if (GodotObject.IsInstanceValid(node) && Useful(node) && player.DistanceTo(node.GlobalPosition) <= PlaceReachRange)
                    MarkVisited(node, kind);
        }

        private void MarkVisited(Node2D node, string kind)
        {
            if (_visited.Add(node.GetInstanceId()))
                _visitedByKind[kind] = _visitedByKind.GetValueOrDefault(kind) + 1;
        }

        /// <summary>
        /// --visit : oriente le bot vers un lieu vu et encore utile ; vrai tant qu'il doit rester immobile pour tenir
        /// une interaction.
        /// </summary>
        public bool Steer(double t, Player player, ref Vector2 waypoint)
        {
            if (t < _holdUntil)
                return true;
            // Le droit d'achat ne vaut que pendant le maintien au Mémorial, jamais pour un écran ouvert plus tard.
            _purchaseAllowed = false;
            if (_target != null && (!GodotObject.IsInstanceValid(_target) || t - _targetSince > PlaceGiveUpSeconds))
            {
                if (GodotObject.IsInstanceValid(_target))
                    _abandoned.Add(_target.GetInstanceId());
                _target = null;
            }
            if (_target == null)
                PickTarget(t, player.GlobalPosition);
            if (_target == null)
                return false;
            if (player.GlobalPosition.DistanceTo(_target.GlobalPosition) > PlaceReachRange)
            {
                waypoint = _target.GlobalPosition;
                return false;
            }
            MarkVisited(_target, _targetKind);
            ulong id = _target.GetInstanceId();
            // Un Mémorial se ravive d'abord, puis sert une fois éveillé : deux passages au plus.
            bool wakeFirst = _target is Memorial { State: Memorial.MemorialState.Dormant };
            if (_targetKind != "rift" && _target is IInteractable { CanInteract: true })
            {
                player.AIInputOverride = Vector2.Zero;
                player.AITriggerInteract();
                _holdUntil = t + PlaceHoldSeconds;
                _purchaseAllowed = _targetKind == "memorial" && !wakeFirst;
            }
            _interactions[id] = _interactions.GetValueOrDefault(id) + 1;
            if (!wakeFirst || _interactions[id] >= 2)
                _abandoned.Add(id);
            _target = null;
            return _holdUntil > t;
        }

        private void PickTarget(double t, Vector2 from)
        {
            float best = PlaceDetourRange;
            foreach ((Node2D node, string kind) in _places)
            {
                ulong id = node.GetInstanceId();
                if (!_seen.Contains(id) || _abandoned.Contains(id) || !Useful(node))
                    continue;
                float distance = from.DistanceTo(node.GlobalPosition);
                if (distance >= best)
                    continue;
                best = distance;
                _target = node;
                _targetKind = kind;
            }
            _targetSince = t;
        }

        private static bool Useful(Node2D node) => node switch
        {
            Chest chest => !chest.IsOpened,
            Memorial memorial => memorial.CanInteract,
            Rift rift => rift.IsOpen,
            SmallPlace place => place.CanInteract,
            _ => false,
        };

        public void Append(StringBuilder summary, double seconds)
        {
            FlushEssence();
            double minutes = seconds / 60.0;
            int seen = 0, visited = 0;
            foreach (int count in _seenByKind.Values)
                seen += count;
            foreach (int count in _visitedByKind.Values)
                visited += count;
            summary.Append(CultureInfo.InvariantCulture,
                $" places_seen={seen} places_visited={visited} places_seen_per_min={seen / minutes:F2} places_visited_per_min={visited / minutes:F2} first_place_s={_firstSeen:F0}");
            foreach (string kind in new[] { "chest", "memorial", "rift", "well", "crystal_vein", "scarecrow" })
                summary.Append(CultureInfo.InvariantCulture,
                    $" {kind}_seen={_seenByKind.GetValueOrDefault(kind)} {kind}_visited={_visitedByKind.GetValueOrDefault(kind)}");
            summary.Append(CultureInfo.InvariantCulture,
                $" events={Events} events_per_min={Events / minutes:F2} essence_gained={EssenceGained} essence_spent={EssenceSpent} essence_gained_per_min={EssenceGained / minutes:F1}");
        }
    }
}
