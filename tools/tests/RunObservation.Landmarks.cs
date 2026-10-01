using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Vestiges.Progression;
using Vestiges.UI;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-rift : la Faille la plus proche, son offre, la fiche de pause (Péril, Oubli), puis l'Oubli levé au
/// Mémorial le plus proche. RESULT : Failles placées, Péril et Oublis avant et après.
/// --capture-memorial : le Mémorial le plus proche du départ, parcouru en entier. Endormi (invite), éclats à ramasser,
/// bénédictions au réveil, services avant et après un achat, puis ravivé. Une ligne RESULT donne le nombre de
/// Mémoriaux, leur distance au départ en fraction du rayon et l'effet de la bénédiction prise.
/// </summary>
public partial class RunObservation
{
    private async Task CaptureMemorial()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Vestiges.Combat.Enemy existing && existing.IsActive)
                _world.GetNode<Vestiges.Spawn.EnemyPool>("EnemyPool").Return(existing);
        _player.AIInputOverride = Vector2.Zero;
        ProcessMode = ProcessModeEnum.Always;
        await ToSignal(GetTree().CreateTimer(3.5, processAlways: true), SceneTreeTimer.SignalName.Timeout);

        List<Memorial> memorials = new(Memorial.All);
        Vector2 spawn = _player.GlobalPosition;
        memorials.Sort((a, b) => a.GlobalPosition.DistanceSquaredTo(spawn).CompareTo(b.GlobalPosition.DistanceSquaredTo(spawn)));
        TileMapLayer ground = _world.GetNode<TileMapLayer>("Ground");
        List<string> bands = new();
        foreach (Memorial memorial in memorials)
        {
            Vector2I cell = ground.LocalToMap(ground.ToLocal(memorial.GlobalPosition));
            bands.Add((_world.Generator.EllipseDistance(cell.X, cell.Y) / _world.Generator.MapRadiusX).ToString("F2", CultureInfo.InvariantCulture));
        }
        if (memorials.Count == 0)
        {
            GD.PushError("[RunObservation] Aucun Mémorial placé");
            return;
        }

        Memorial target = memorials[0];
        _player.GlobalPosition = target.GlobalPosition + new Vector2(-44f, 8f);
        _camera.ResetSmoothing();
        await Frames(25);
        SaveCrop("memorial-1-dormant", target.GlobalPosition + new Vector2(0f, -50f), new Vector2(240f, 135f));

        float damageBefore = _player.DamageMultiplier;
        float hpBefore = _player.EffectiveMaxHp;
        int shardCount = await AwakenMemorial(target, "memorial-2-gathering", "memorial-3-blessings");
        ChoiceScreen choices = _world.GetNode<ChoiceScreen>("ChoiceScreen");
        bool blessingsShown = shardCount > 0;

        _world.GetNode<EssenceTracker>("EssenceTracker").AddEssence(120);
        _player.GlobalPosition = target.GlobalPosition + new Vector2(-44f, 8f);
        target.Interact(_player);
        await ToSignal(GetTree().CreateTimer(2.5, processAlways: true), SceneTreeTimer.SignalName.Timeout);
        SaveFrame("memorial-4-services");
        int levelBefore = _player.EquippedWeapon.Level;
        choices.Activate(0);
        await ToSignal(GetTree().CreateTimer(0.6, processAlways: true), SceneTreeTimer.SignalName.Timeout);
        SaveFrame("memorial-5-services-after");
        int levelAfter = _player.EquippedWeapon.Level;
        choices.Activate(int.MaxValue);
        await Frames(40);
        SaveCrop("memorial-6-awake", target.GlobalPosition + new Vector2(0f, -50f), new Vector2(240f, 135f));

        GD.Print(string.Create(CultureInfo.InvariantCulture,
            $"[RunObservation] RESULT memorial count={memorials.Count} bands={string.Join(",", bands)} shards={shardCount} state={target.State} blessings_shown={blessingsShown} damage={damageBefore:F2}->{_player.DamageMultiplier:F2} max_hp={hpBefore:F0}->{_player.EffectiveMaxHp:F0} weapon_level={levelBefore}->{levelAfter}"));
    }

    /// <summary>Ravive un Mémorial comme un joueur : éclats ramassés, première bénédiction prise. Rend le nombre d'éclats.</summary>
    private async Task<int> AwakenMemorial(Memorial memorial, string gatheringFrame, string blessingsFrame)
    {
        _player.GlobalPosition = memorial.GlobalPosition + new Vector2(-44f, 8f);
        memorial.Interact(_player);
        await Frames(30);
        if (gatheringFrame != null)
            SaveFrame(gatheringFrame);
        List<MemoryShard> shards = new();
        foreach (Node child in _world.GetNode("PoiContainer").GetChildren())
            if (child is MemoryShard shard)
                shards.Add(shard);
        foreach (MemoryShard shard in shards)
        {
            _player.GlobalPosition = shard.GlobalPosition;
            await Frames(4);
        }
        await Frames(10);
        ChoiceScreen choices = _world.GetNode<ChoiceScreen>("ChoiceScreen");
        if (!choices.IsOpen)
            return 0;
        if (blessingsFrame != null)
        {
            await ToSignal(GetTree().CreateTimer(2.5, processAlways: true), SceneTreeTimer.SignalName.Timeout);
            SaveFrame(blessingsFrame);
        }
        choices.Activate(0);
        await Frames(10);
        return shards.Count;
    }

    private async Task CaptureRift()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Vestiges.Combat.Enemy existing && existing.IsActive)
                _world.GetNode<Vestiges.Spawn.EnemyPool>("EnemyPool").Return(existing);
        _player.AIInputOverride = Vector2.Zero;
        ProcessMode = ProcessModeEnum.Always;
        await ToSignal(GetTree().CreateTimer(3.5, processAlways: true), SceneTreeTimer.SignalName.Timeout);

        List<Rift> rifts = new(Rift.All);
        Vector2 spawn = _player.GlobalPosition;
        rifts.Sort((a, b) => a.GlobalPosition.DistanceSquaredTo(spawn).CompareTo(b.GlobalPosition.DistanceSquaredTo(spawn)));
        if (rifts.Count == 0)
        {
            GD.PushError("[RunObservation] Aucune Faille placée");
            return;
        }

        PerilManager peril = _world.GetNode<PerilManager>("PerilManager");
        ChoiceScreen choices = _world.GetNode<ChoiceScreen>("ChoiceScreen");
        Rift rift = rifts[0];
        _player.GlobalPosition = rift.GlobalPosition + new Vector2(-44f, 8f);
        _camera.ResetSmoothing();
        await Frames(25);
        SaveCrop("rift-1-open", rift.GlobalPosition + new Vector2(0f, -40f), new Vector2(240f, 135f));

        float speedBefore = _player.SpeedMultiplier;
        float damageBefore = _player.DamageMultiplier;
        rift.Interact(_player);
        await ToSignal(GetTree().CreateTimer(2.5, processAlways: true), SceneTreeTimer.SignalName.Timeout);
        bool offerShown = choices.IsOpen;
        SaveFrame("rift-2-offers");
        choices.Activate(0);
        await Frames(20);
        SaveCrop("rift-3-closed", rift.GlobalPosition + new Vector2(0f, -40f), new Vector2(240f, 135f));
        int perilAfter = peril.Peril;
        int oublisAfter = peril.Oublis.Count;
        string oubli = oublisAfter > 0 ? peril.Oublis[0].Data.Id : "none";

        Node pause = _world.GetNode("PauseMenu");
        pause.GetType().GetMethod("Pause", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(pause, null);
        await Frames(10);
        SaveFrame("rift-4-pause");
        pause.GetType().GetMethod("Resume", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(pause, null);
        await Frames(5);

        List<Memorial> memorials = new(Memorial.All);
        memorials.Sort((a, b) => a.GlobalPosition.DistanceSquaredTo(spawn).CompareTo(b.GlobalPosition.DistanceSquaredTo(spawn)));
        await AwakenMemorial(memorials[0], null, null);
        _world.GetNode<EssenceTracker>("EssenceTracker").AddEssence(200);
        _player.GlobalPosition = memorials[0].GlobalPosition + new Vector2(-44f, 8f);
        memorials[0].Interact(_player);
        await ToSignal(GetTree().CreateTimer(2.5, processAlways: true), SceneTreeTimer.SignalName.Timeout);
        SaveFrame("rift-5-memorial-lift");
        // La levée d'Oubli est la dernière carte des services.
        choices.Activate(_player.WeaponSlots.Count + 1);
        await Frames(10);
        SaveFrame("rift-6-lifted");
        choices.Activate(int.MaxValue);
        await Frames(5);

        GD.Print(string.Create(CultureInfo.InvariantCulture,
            $"[RunObservation] RESULT rift count={rifts.Count} offer_shown={offerShown} peril=0->{perilAfter} oublis=0->{oublisAfter}->{peril.Oublis.Count} oubli={oubli} speed={speedBefore:F2}->{_player.SpeedMultiplier:F2} damage={damageBefore:F2}->{_player.DamageMultiplier:F2}"));
    }
}
