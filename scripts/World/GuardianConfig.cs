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
    public float HpCapFloorRatio { get; init; }
    public float FightCrowdDensity { get; init; }
    public int FightCrowdCap { get; init; }
    public float TargetBias { get; init; }
    public float ExtraLightHp { get; init; }
    public string ExtraLightChest { get; init; }
    public string Chest { get; init; }
    public float RageHp { get; init; }
    public float RageDamage { get; init; }
    public int Escorts { get; init; }
    public float PassageDelaySeconds { get; init; }
    public int RankPeril { get; init; }

    /// <summary>Gardiens à vaincre pour gagner la run ; la terre suivante est celle du late game, sans fin.</summary>
    public int LandsToWin { get; init; }
    public float EchoFirstSeconds { get; init; }
    public float EchoIntervalSeconds { get; init; }
    public float EchoHpMultiplier { get; init; }
    public int EchoPeril { get; init; }
    public int EchoEscorts { get; init; }
    public string EchoChest { get; init; }
    public float EchoDistance { get; init; }
    public string RiseAudio { get; init; }
    public string VictoryAudio { get; init; }

    /// <summary>Son du carton d'arrivée d'une terre (plan 56 B3).</summary>
    public string ArrivalAudio { get; init; }
    public GuardianMovesConfig Moves { get; init; }

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
            reader.AllowOnly(root, "gardien", "_doc", "wake_lights", "lair_offset_px", "hp_multiplier", "min_hp", "min_hp_rank_growth", "hp_cap_floor_ratio", "fight_crowd_density", "fight_crowd_cap", "target_bias", "extra_light_hp",
                "extra_light_chest", "chest", "rage_hp", "rage_damage", "escorts", "passage_delay_seconds", "rank_peril", "lands_to_win", "rise_audio", "victory_audio", "arrival_audio", "late_game", "moves");
            JsonElement lateGame = reader.Section(root, "late_game");
            reader.AllowOnly(lateGame, "late_game", "_doc", "echo_first_seconds", "echo_interval_seconds", "echo_hp_multiplier", "echo_peril",
                "echo_escorts", "echo_chest", "echo_distance");
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
                HpCapFloorRatio = reader.Number(root, "hp_cap_floor_ratio"),
                FightCrowdDensity = reader.Positive(root, "fight_crowd_density"),
                FightCrowdCap = reader.Integer(root, "fight_crowd_cap", 1, 500),
                TargetBias = reader.NonNegative(root, "target_bias"),
                ExtraLightHp = reader.NonNegative(root, "extra_light_hp"),
                ExtraLightChest = reader.OneOf(root, "extra_light_chest", chests),
                Chest = reader.OneOf(root, "chest", chests),
                RageHp = reader.Positive(root, "rage_hp"),
                RageDamage = reader.Positive(root, "rage_damage"),
                Escorts = reader.Integer(root, "escorts", 0, 50),
                PassageDelaySeconds = reader.NonNegative(root, "passage_delay_seconds"),
                RankPeril = reader.Integer(root, "rank_peril", 0, 50),
                LandsToWin = reader.Integer(root, "lands_to_win", 1, 20),
                EchoFirstSeconds = reader.NonNegative(lateGame, "echo_first_seconds"),
                EchoIntervalSeconds = reader.Positive(lateGame, "echo_interval_seconds"),
                EchoHpMultiplier = reader.Positive(lateGame, "echo_hp_multiplier"),
                EchoPeril = reader.Integer(lateGame, "echo_peril", 0, 50),
                EchoEscorts = reader.Integer(lateGame, "echo_escorts", 0, 50),
                EchoChest = reader.OneOf(lateGame, "echo_chest", chests),
                EchoDistance = reader.Positive(lateGame, "echo_distance"),
                RiseAudio = reader.Text(root, "rise_audio"),
                VictoryAudio = reader.Text(root, "victory_audio"),
                ArrivalAudio = reader.Text(root, "arrival_audio"),
                Moves = GuardianMovesConfig.Parse(reader, root),
            };
            if (reader.Error == null && config.HpCapFloorRatio < 1f)
                reader.Fail("hp_cap_floor_ratio : 1 ou plus attendu (plafond au-dessus du plancher)");
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
