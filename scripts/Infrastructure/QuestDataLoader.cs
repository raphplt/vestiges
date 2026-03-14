using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

public class QuestDefinition
{
    public string Id { get; set; }
    public string Category { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ObjectiveType { get; set; }
    public float Target { get; set; }
    public string RewardType { get; set; }
    public string RewardId { get; set; }
    public int RewardAmount { get; set; }
    public string RewardLabel { get; set; }
}

public static class QuestDataLoader
{
    private static readonly List<QuestDefinition> _all = new();
    private static readonly Dictionary<string, QuestDefinition> _byId = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        FileAccess file = FileAccess.Open("res://data/quests/quests.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[QuestDataLoader] Cannot open quests.json");
            return;
        }

        string jsonText = file.GetAsText();
        file.Close();

        Json json = new();
        if (json.Parse(jsonText) != Error.Ok)
        {
            GD.PushError($"[QuestDataLoader] Parse error: {json.GetErrorMessage()}");
            return;
        }

        Godot.Collections.Array array = json.Data.AsGodotArray();
        foreach (Variant item in array)
        {
            if (item.VariantType != Variant.Type.Dictionary)
                continue;

            Godot.Collections.Dictionary dict = item.AsGodotDictionary();
            QuestDefinition definition = new()
            {
                Id = dict.ContainsKey("id") ? dict["id"].AsString() : "",
                Category = dict.ContainsKey("category") ? dict["category"].AsString() : "run",
                Name = dict.ContainsKey("name") ? dict["name"].AsString() : "",
                Description = dict.ContainsKey("description") ? dict["description"].AsString() : "",
                ObjectiveType = dict.ContainsKey("objective_type") ? dict["objective_type"].AsString() : "",
                Target = dict.ContainsKey("target") ? (float)dict["target"].AsDouble() : 1f,
                RewardType = dict.ContainsKey("reward_type") ? dict["reward_type"].AsString() : "",
                RewardId = dict.ContainsKey("reward_id") ? dict["reward_id"].AsString() : "",
                RewardAmount = dict.ContainsKey("reward_amount") ? (int)dict["reward_amount"].AsDouble() : 0,
                RewardLabel = dict.ContainsKey("reward_label") ? dict["reward_label"].AsString() : ""
            };

            if (string.IsNullOrWhiteSpace(definition.Id))
                continue;

            _all.Add(definition);
            _byId[definition.Id] = definition;
        }

        _loaded = true;
        GD.Print($"[QuestDataLoader] Loaded {_all.Count} quests");
    }

    public static QuestDefinition Get(string questId)
    {
        if (!_loaded)
            Load();

        return string.IsNullOrWhiteSpace(questId) ? null : _byId.GetValueOrDefault(questId);
    }

    public static List<QuestDefinition> GetAll()
    {
        if (!_loaded)
            Load();

        return new List<QuestDefinition>(_all);
    }

    public static List<QuestDefinition> GetByCategory(string category)
    {
        if (!_loaded)
            Load();

        List<QuestDefinition> result = new();
        foreach (QuestDefinition definition in _all)
        {
            if (definition.Category == category)
                result.Add(definition);
        }

        return result;
    }
}
