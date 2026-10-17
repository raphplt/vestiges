using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>Réglages du gardien de terre (data/world/guardian.json, plan 51 P3), contrôlés en entier.</summary>
public sealed class GuardianConfig
{
    private const string ConfigPath = "res://data/world/guardian.json";

    public int WakeLights { get; init; }
    public Vector2 LairOffset { get; init; }
    public float HpMultiplier { get; init; }
    public float MinHp { get; init; }
    public float MinHpRankGrowth { get; init; }
    public float FightCrowdDensity { get; init; }
    public float ExtraLightHp { get; init; }
    public string ExtraLightChest { get; init; }
    public string Chest { get; init; }
    public float RageHp { get; init; }
    public float RageDamage { get; init; }
    public int Escorts { get; init; }
    public float PassageDelaySeconds { get; init; }
    public int RankPeril { get; init; }

    public static GuardianConfig Load(out string error)
    {
        using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            error = $"{ConfigPath} absent";
            return null;
        }
        return Parse(file.GetAsText(), out error);
    }

    public static GuardianConfig Parse(string json, out string error)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement root = reader.Root;
            reader.AllowOnly(root, "gardien", "_doc", "wake_lights", "lair_offset_px", "hp_multiplier", "min_hp", "min_hp_rank_growth", "fight_crowd_density", "extra_light_hp",
                "extra_light_chest", "chest", "rage_hp", "rage_damage", "escorts", "passage_delay_seconds", "rank_peril");
            List<JsonElement> offset = reader.List(root, "lair_offset_px", 2);
            HashSet<string> chests = new();
            foreach (ChestData chest in ChestDataLoader.GetAll())
                chests.Add(chest.Id);
            GuardianConfig config = new()
            {
                WakeLights = reader.Integer(root, "wake_lights", 1, 20),
                LairOffset = offset.Count == 2 ? new Vector2((float)offset[0].GetDouble(), (float)offset[1].GetDouble()) : Vector2.Zero,
                HpMultiplier = reader.Positive(root, "hp_multiplier"),
                MinHp = reader.NonNegative(root, "min_hp"),
                MinHpRankGrowth = reader.Positive(root, "min_hp_rank_growth"),
                FightCrowdDensity = reader.Positive(root, "fight_crowd_density"),
                ExtraLightHp = reader.NonNegative(root, "extra_light_hp"),
                ExtraLightChest = reader.OneOf(root, "extra_light_chest", chests),
                Chest = reader.OneOf(root, "chest", chests),
                RageHp = reader.Positive(root, "rage_hp"),
                RageDamage = reader.Positive(root, "rage_damage"),
                Escorts = reader.Integer(root, "escorts", 0, 50),
                PassageDelaySeconds = reader.NonNegative(root, "passage_delay_seconds"),
                RankPeril = reader.Integer(root, "rank_peril", 0, 50),
            };
            error = reader.Error;
            return error == null ? config : null;
        }
        catch (JsonException exception)
        {
            error = $"JSON illisible : {exception.Message}";
            return null;
        }
    }
}
