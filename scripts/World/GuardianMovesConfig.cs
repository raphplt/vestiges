using System.Collections.Generic;
using System.Text.Json;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>Coups du gardien de terre (section <c>moves</c> de data/world/guardian.json, plan 51 §16 G1).</summary>
public sealed class GuardianMovesConfig
{
    public float[] PhaseThresholds { get; init; }
    public float[] PhaseCooldownScale { get; init; }
    public string WarningAudio { get; init; }
    public string PhaseAudio { get; init; }
    public float PlayerHitMargin { get; init; }
    public float ImpactFlashSeconds { get; init; }

    public float ChargeFirstDelay { get; init; }
    public float ChargeCooldown { get; init; }
    public float ChargeTriggerRange { get; init; }
    public float ChargeMinRange { get; init; }
    public float ChargeWindup { get; init; }
    public float ChargeDistance { get; init; }
    public float ChargeSeconds { get; init; }
    public float ChargeRecovery { get; init; }
    public float ChargeHitRange { get; init; }
    public float ChargeWidth { get; init; }
    public float ChargeDamage { get; init; }

    public float SlamFirstDelay { get; init; }
    public float SlamCooldown { get; init; }
    public float SlamCastRange { get; init; }
    public float SlamDelay { get; init; }
    public float SlamLeadSeconds { get; init; }
    public float SlamRadius { get; init; }
    public float SlamDamage { get; init; }
    public float BreachSeconds { get; init; }
    public int FinalSlamCount { get; init; }
    public float FinalSlamSpread { get; init; }
    public string SlamAudio { get; init; }

    public float WaveCooldown { get; init; }
    public float WaveWindup { get; init; }
    public float WaveRadius { get; init; }
    public float WaveExpandSeconds { get; init; }
    public float WaveThickness { get; init; }
    public float WaveDamage { get; init; }
    public string WaveAudio { get; init; }

    /// <summary>Charges en chaîne du Grand Cerf (plan 57 G1), à partir de cette phase (0 à 2).</summary>
    public int ChainFromPhase { get; init; }
    public float ChainFirstDelay { get; init; }
    public float ChainCooldown { get; init; }
    public int ChainCount { get; init; }
    public float ChainTriggerRange { get; init; }
    public float ChainWindup { get; init; }
    public float ChainRewindup { get; init; }
    public float ChainDistance { get; init; }
    public float ChainSeconds { get; init; }
    public float ChainRecovery { get; init; }
    public float ChainBreachRadius { get; init; }
    public float ChainBreachSeconds { get; init; }
    public float ChainImpactDamage { get; init; }
    public string ChainWarningAudio { get; init; }
    public string ChainImpactAudio { get; init; }

    /// <summary>Fauchage de l'Épouvantail (plan 57 G2), à partir de cette phase (0 à 2).</summary>
    public int SweepFromPhase { get; init; }
    public float SweepFirstDelay { get; init; }
    public float SweepCooldown { get; init; }
    public float SweepTriggerRange { get; init; }
    public float SweepWindup { get; init; }
    public float SweepRadius { get; init; }
    /// <summary>Demi-ouverture de la brèche, en radians (la donnée est la brèche entière, en degrés).</summary>
    public float SweepGapHalf { get; init; }
    /// <summary>Écart de la brèche au joueur, en radians : elle ne s'ouvre jamais sous ses pieds.</summary>
    public float SweepGapOffset { get; init; }
    public float SweepRecovery { get; init; }
    public float SweepDamage { get; init; }
    public string SweepWarningAudio { get; init; }
    public string SweepImpactAudio { get; init; }

    public static GuardianMovesConfig Parse(JsonConfigReader reader, JsonElement root)
    {
        JsonElement moves = reader.Section(root, "moves");
        reader.AllowOnly(moves, "moves", "_doc", "phase_thresholds", "phase_cooldown_scale", "warning_audio", "phase_audio", "player_hit_margin",
            "impact_flash_seconds", "charge", "slam", "wave", "chain_charge", "sweep");
        JsonElement charge = reader.Section(moves, "charge");
        reader.AllowOnly(charge, "charge", "first_delay", "cooldown_seconds", "trigger_range", "min_range", "windup_seconds", "distance",
            "charge_seconds", "recovery_seconds", "hit_range", "marker_width", "damage_multiplier");
        JsonElement slam = reader.Section(moves, "slam");
        reader.AllowOnly(slam, "slam", "first_delay", "cooldown_seconds", "cast_range", "delay_seconds", "lead_seconds", "radius",
            "damage_multiplier", "breach_seconds", "final_count", "final_spread", "impact_audio");
        JsonElement wave = reader.Section(moves, "wave");
        reader.AllowOnly(wave, "wave", "cooldown_seconds", "windup_seconds", "radius", "expand_seconds", "thickness", "damage_multiplier",
            "impact_audio");
        JsonElement chain = reader.Section(moves, "chain_charge");
        reader.AllowOnly(chain, "chain_charge", "from_phase", "first_delay", "cooldown_seconds", "count", "trigger_range",
            "windup_seconds", "rewindup_seconds", "distance", "charge_seconds", "recovery_seconds", "breach_radius",
            "breach_seconds", "impact_damage_multiplier", "warning_audio", "impact_audio");
        JsonElement sweep = reader.Section(moves, "sweep");
        reader.AllowOnly(sweep, "sweep", "from_phase", "first_delay", "cooldown_seconds", "trigger_range", "windup_seconds",
            "radius", "gap_degrees", "gap_offset_degrees", "recovery_seconds", "damage_multiplier", "warning_audio", "impact_audio");
        float gapDegrees = reader.Positive(sweep, "gap_degrees");
        float gapOffsetDegrees = reader.Positive(sweep, "gap_offset_degrees");
        // La brèche reste une brèche, et ne touche jamais le joueur au moment de l'annonce.
        if (reader.Error == null && (gapDegrees >= 180f || gapOffsetDegrees - gapDegrees / 2f < 15f || gapOffsetDegrees > 180f))
            reader.Fail("moves.sweep : brèche de moins de 180°, décalée du joueur d'au moins sa moitié et 15°, au plus 180°");
        float[] thresholds = Numbers(reader, moves, "phase_thresholds");
        float[] scales = Numbers(reader, moves, "phase_cooldown_scale");
        // Trois phases : l'onde arrive à la deuxième, la frappe multiple à la troisième.
        if (reader.Error == null && (thresholds.Length != 2 || scales.Length != 3))
            reader.Fail("moves : deux seuils de phase_thresholds et trois valeurs de phase_cooldown_scale attendus (trois phases)");
        foreach (float threshold in thresholds)
        {
            if (threshold >= 1f)
                reader.Fail("moves.phase_thresholds : parts de PV dans ]0 ; 1[ attendues");
        }
        for (int i = 1; i < thresholds.Length; i++)
        {
            if (thresholds[i] >= thresholds[i - 1])
                reader.Fail("moves.phase_thresholds : seuils de PV décroissants attendus");
        }
        return new GuardianMovesConfig
        {
            PhaseThresholds = thresholds,
            PhaseCooldownScale = scales,
            WarningAudio = Audio(reader, moves, "warning_audio"),
            PhaseAudio = Audio(reader, moves, "phase_audio"),
            PlayerHitMargin = reader.NonNegative(moves, "player_hit_margin"),
            ImpactFlashSeconds = reader.Positive(moves, "impact_flash_seconds"),
            ChargeFirstDelay = reader.NonNegative(charge, "first_delay"),
            ChargeCooldown = reader.Positive(charge, "cooldown_seconds"),
            ChargeTriggerRange = reader.Positive(charge, "trigger_range"),
            ChargeMinRange = reader.NonNegative(charge, "min_range"),
            ChargeWindup = reader.Positive(charge, "windup_seconds"),
            ChargeDistance = reader.Positive(charge, "distance"),
            ChargeSeconds = reader.Positive(charge, "charge_seconds"),
            ChargeRecovery = reader.NonNegative(charge, "recovery_seconds"),
            ChargeHitRange = reader.Positive(charge, "hit_range"),
            ChargeWidth = reader.Positive(charge, "marker_width"),
            ChargeDamage = reader.Positive(charge, "damage_multiplier"),
            SlamFirstDelay = reader.NonNegative(slam, "first_delay"),
            SlamCooldown = reader.Positive(slam, "cooldown_seconds"),
            SlamCastRange = reader.Positive(slam, "cast_range"),
            SlamDelay = reader.Positive(slam, "delay_seconds"),
            SlamLeadSeconds = reader.NonNegative(slam, "lead_seconds"),
            SlamRadius = reader.Positive(slam, "radius"),
            SlamDamage = reader.Positive(slam, "damage_multiplier"),
            BreachSeconds = reader.Positive(slam, "breach_seconds"),
            FinalSlamCount = reader.Integer(slam, "final_count", 1, 6),
            FinalSlamSpread = reader.NonNegative(slam, "final_spread"),
            SlamAudio = Audio(reader, slam, "impact_audio"),
            WaveCooldown = reader.Positive(wave, "cooldown_seconds"),
            WaveWindup = reader.Positive(wave, "windup_seconds"),
            WaveRadius = reader.Positive(wave, "radius"),
            WaveExpandSeconds = reader.Positive(wave, "expand_seconds"),
            WaveThickness = reader.Positive(wave, "thickness"),
            WaveDamage = reader.Positive(wave, "damage_multiplier"),
            WaveAudio = Audio(reader, wave, "impact_audio"),
            ChainFromPhase = reader.Integer(chain, "from_phase", 0, 2),
            ChainFirstDelay = reader.NonNegative(chain, "first_delay"),
            ChainCooldown = reader.Positive(chain, "cooldown_seconds"),
            ChainCount = reader.Integer(chain, "count", 2, 6),
            ChainTriggerRange = reader.Positive(chain, "trigger_range"),
            ChainWindup = reader.Positive(chain, "windup_seconds"),
            ChainRewindup = reader.Positive(chain, "rewindup_seconds"),
            ChainDistance = reader.Positive(chain, "distance"),
            ChainSeconds = reader.Positive(chain, "charge_seconds"),
            ChainRecovery = reader.NonNegative(chain, "recovery_seconds"),
            ChainBreachRadius = reader.Positive(chain, "breach_radius"),
            ChainBreachSeconds = reader.Positive(chain, "breach_seconds"),
            ChainImpactDamage = reader.Positive(chain, "impact_damage_multiplier"),
            ChainWarningAudio = Audio(reader, chain, "warning_audio"),
            ChainImpactAudio = Audio(reader, chain, "impact_audio"),
            SweepFromPhase = reader.Integer(sweep, "from_phase", 0, 2),
            SweepFirstDelay = reader.NonNegative(sweep, "first_delay"),
            SweepCooldown = reader.Positive(sweep, "cooldown_seconds"),
            SweepTriggerRange = reader.Positive(sweep, "trigger_range"),
            SweepWindup = reader.Positive(sweep, "windup_seconds"),
            SweepRadius = reader.Positive(sweep, "radius"),
            SweepGapHalf = Godot.Mathf.DegToRad(gapDegrees) / 2f,
            SweepGapOffset = Godot.Mathf.DegToRad(gapOffsetDegrees),
            SweepRecovery = reader.NonNegative(sweep, "recovery_seconds"),
            SweepDamage = reader.Positive(sweep, "damage_multiplier"),
            SweepWarningAudio = Audio(reader, sweep, "warning_audio"),
            SweepImpactAudio = Audio(reader, sweep, "impact_audio"),
        };
    }

    private static float[] Numbers(JsonConfigReader reader, JsonElement owner, string key)
    {
        List<JsonElement> items = reader.List(owner, key, 1);
        float[] values = new float[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].ValueKind != JsonValueKind.Number || items[i].GetDouble() <= 0.0)
                reader.Fail($"moves.{key} : nombres positifs attendus");
            else
                values[i] = (float)items[i].GetDouble();
        }
        return values;
    }

    /// <summary>Clé de data/audio/sounds.json ; une clé inconnue se tait, comme pour les capacités des créatures.</summary>
    private static string Audio(JsonConfigReader reader, JsonElement owner, string key) => reader.Text(owner, key);
}
