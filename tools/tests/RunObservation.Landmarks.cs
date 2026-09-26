using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Vestiges.Progression;
using Vestiges.UI;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
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
        await Frames(90);

        List<Memorial> memorials = new(Memorial.All);
        Vector2 spawn = _player.GlobalPosition;
        memorials.Sort((a, b) => a.GlobalPosition.DistanceSquaredTo(spawn).CompareTo(b.GlobalPosition.DistanceSquaredTo(spawn)));
        TileMapLayer ground = _world.GetNode<TileMapLayer>("Ground");
        List<string> bands = new();
        foreach (Memorial memorial in memorials)
        {
            Vector2I cell = ground.LocalToMap(ground.ToLocal(memorial.GlobalPosition));
            bands.Add((Mathf.Sqrt(cell.X * cell.X + cell.Y * cell.Y) / _world.Generator.MapRadius).ToString("F2", CultureInfo.InvariantCulture));
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

        target.Interact(_player);
        await Frames(30);
        SaveFrame("memorial-2-gathering");

        float damageBefore = _player.DamageMultiplier;
        float hpBefore = _player.EffectiveMaxHp;
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
        bool blessingsShown = choices.IsOpen;
        SaveFrame("memorial-3-blessings");
        choices.Activate(0);
        await Frames(10);

        _world.GetNode<EssenceTracker>("EssenceTracker").AddEssence(120);
        _player.GlobalPosition = target.GlobalPosition + new Vector2(-44f, 8f);
        target.Interact(_player);
        await Frames(10);
        SaveFrame("memorial-4-services");
        int levelBefore = _player.EquippedWeapon.Level;
        choices.Activate(0);
        await Frames(10);
        SaveFrame("memorial-5-services-after");
        int levelAfter = _player.EquippedWeapon.Level;
        choices.Activate(int.MaxValue);
        await Frames(40);
        SaveCrop("memorial-6-awake", target.GlobalPosition + new Vector2(0f, -50f), new Vector2(240f, 135f));

        GD.Print(string.Create(CultureInfo.InvariantCulture,
            $"[RunObservation] RESULT memorial count={memorials.Count} bands={string.Join(",", bands)} shards={shards.Count} state={target.State} blessings_shown={blessingsShown} damage={damageBefore:F2}->{_player.DamageMultiplier:F2} max_hp={hpBefore:F0}->{_player.EffectiveMaxHp:F0} weapon_level={levelBefore}->{levelAfter}"));
    }
}
