using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.Tests;

public partial class RunObservation
{
    /// <summary>Rendu des vrais ramassables et des vrais sceaux, avec collecte et complétion par leurs systèmes.</summary>
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

        QuestManager quests = _world.GetNode<QuestManager>("QuestManager");
        IList active = (IList)typeof(QuestManager).GetField("_activeRunQuests", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(quests);
        object quest = active[0];
        QuestDefinition definition = (QuestDefinition)quest.GetType().GetProperty("Definition").GetValue(quest);
        MethodInfo progress = typeof(QuestManager).GetMethod("SetAbsoluteProgress", BindingFlags.Instance | BindingFlags.NonPublic);
        progress.Invoke(quests, new object[] { definition.ObjectiveType, definition.Target * 0.5f });
        await Frames(20);
        SaveFrame("quest-progress");
        progress.Invoke(quests, new object[] { definition.ObjectiveType, definition.Target });
        while (GetTree().Paused)
        {
            AutoPickLevelUp();
            await Frames(1);
        }
        await Frames(3);
        SaveFrame("quest-break");
        await Frames(50);
        SaveFrame("quest-complete");
        if (!(bool)quest.GetType().GetProperty("Completed").GetValue(quest))
            throw new InvalidOperationException("La quête n'a pas été accomplie.");
        GD.Print("[RunObservation] RESULT integrated_art=True collected=5 quest_completed=True");
    }
}
