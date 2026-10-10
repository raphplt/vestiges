using System;
using System.Threading.Tasks;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Tests;

public partial class RunObservation
{
    /// <summary>Rendu des vrais ramassables, avec collecte par leur système.</summary>
    private async Task CaptureIntegratedArt()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        _player.AIInputOverride = Vector2.Zero;
        await ToSignal(GetTree().CreateTimer(3.5), SceneTreeTimer.SignalName.Timeout);
        FieldBonusDirector director = _world.GetNode<FieldBonusDirector>("FieldBonusDirector");
        FieldBonusConfig config = FieldBonusDataLoader.Load();
        Vector2 origin = _player.GlobalPosition;
        foreach (FieldBonusData bonus in config.Bonuses)
        {
            Vector2 position = origin + new Vector2(42, 35);
            director.Spawn(bonus, position);
            await Frames(12);
            SaveFrame("bonus-" + bonus.Id + "-idle");
            _player.GlobalPosition = position;
            await Frames(3);
            if (director.Active.Count != 0)
                throw new InvalidOperationException("Le bonus n'a pas été ramassé : " + bonus.Id);
            SaveFrame("bonus-" + bonus.Id + "-collected");
            await Frames(25);
            _player.GlobalPosition = origin;
        }

        GD.Print("[RunObservation] RESULT integrated_art=True collected=5");
    }
}
