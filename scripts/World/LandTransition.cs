using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.UI;

namespace Vestiges.World;

/// <summary>
/// Passage d'une terre à la suivante (plan 51, lot P1). Jeu en pause : chacun lâche la terre quittée au signal
/// <c>LandChanging</c>, la carte est vidée puis reconstruite à sa place, les détails posés sur les décors (petits lieux,
/// cabines, fausses lumières) sont refaits, le joueur repart du départ, puis <c>LandChanged</c>. Le joueur et tout ce
/// qui porte la run restent intacts. Possède aussi la pose de ces détails au premier chargement.
/// </summary>
public partial class LandTransition : Node
{
    private WorldSetup _world;
    private Player _player;
    private SmallPlaceDirector _smallPlaces;
    private ErasureManager _erasure;
    private CanvasModulate _canvas;
    private EventBus _eventBus;
    private AbandonedVoicesDirector _voices;
    private FalseLightDirector _falseLights;

    /// <summary>Rang de la terre en cours, 0 au départ de la run.</summary>
    public int LandIndex { get; private set; }

    public bool IsChanging { get; private set; }

    public void Setup(WorldSetup world, Player player, SmallPlaceDirector smallPlaces, ErasureManager erasure, CanvasModulate canvas)
    {
        _world = world;
        _player = player;
        _smallPlaces = smallPlaces;
        _erasure = erasure;
        _canvas = canvas;
    }

    public override void _Ready()
    {
        // Le passage se déroule jeu en pause, comme le chargement.
        ProcessMode = ProcessModeEnum.Always;
        _eventBus = GetNode<EventBus>("/root/EventBus");
    }

    /// <summary>Petits lieux, cabines qui sonnent et fausses lumières, sur les décors de la terre déjà posés.</summary>
    public void PlaceLandDetails()
    {
        _smallPlaces.PlacePlaces(_world.GetNode("PropContainer"), _world.GetNode<Node2D>("PoiContainer"));
        // Cabines qui sonnent (plan 39 C1) : après les petits lieux, dont les cabines.
        _voices = new AbandonedVoicesDirector { Name = "AbandonedVoicesDirector" };
        _voices.Setup(_smallPlaces, _player);
        _world.AddChild(_voices);
        // Fausses lumières (plan 39 B4a) : sur les lampes des décors déjà posés.
        _falseLights = new FalseLightDirector { Name = "FalseLightDirector" };
        _falseLights.Setup(_world.GetNode("PropContainer"), _erasure, _canvas);
        _world.AddChild(_falseLights);
    }

    /// <summary>Quitte la terre en cours pour celle de rang <paramref name="index"/>.</summary>
    public async Task GoToLandAsync(int index)
    {
        if (IsChanging)
            return;
        IsChanging = true;
        SceneTree tree = GetTree();
        bool wasPaused = tree.Paused;
        tree.Paused = true;
        ulong started = Time.GetTicksMsec();
        // L'écran de chargement couvre la construction ; la recousure suit (ErasureManager).
        GameLoadingOverlay overlay = new() { Name = "LandLoadingOverlay" };
        _world.AddChild(overlay);
        try
        {
            _eventBus.EmitSignal(EventBus.SignalName.LandChanging, index);
            // Nommés comme au premier chargement : libérés tout de suite, pour que les suivants reprennent leur nom.
            _voices?.Free();
            _falseLights?.Free();
            await _world.ClearWorldAsync();
            await _world.BuildLandAsync(index, overlay.SetProgress);
            _smallPlaces.ResetLand(_world.LandSeed);
            PlaceLandDetails();
            _player.ArriveOnLand(Vector2.Zero);
            LandIndex = index;
            _eventBus.EmitSignal(EventBus.SignalName.LandChanged, index);
            GD.Print($"[LandTransition] Terre {index} ({string.Join(",", _world.Land.BiomeIds)}) en {Time.GetTicksMsec() - started} ms");
        }
        finally
        {
            // La terre attend sous le voile, dans le Néant, puis se recoud quand il s'est effacé (§16 R1).
            overlay.FadeOut(_erasure.ReleaseStitch);
            tree.Paused = wasPaused;
            IsChanging = false;
        }
        // Après la reprise : si les orbes de la terre quittée font monter de niveau, l'écran de choix prend la pause à
        // son tour, au lieu d'être relancé par la fin du passage.
        XpOrb.CollectAll(_player);
    }
}
