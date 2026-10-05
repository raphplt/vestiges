using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Tests;

/// <summary>
/// Relevé des valeurs effectives de la courbe d'XP, du barème du score et du Péril (plan 26 Q7a), à comparer avant et
/// après un changement de lecteur au même commit de base. N'utilise que des accès présents des deux côtés.
/// </summary>
public partial class CatalogValuesProbe : Node
{
    public override void _Ready()
    {
        List<string> lines = new();
        XpCurveConfig curve = XpCurveConfig.Load();
        for (int level = 1; level <= 250; level++)
            lines.Add(Invariant($"xp {level} {curve.CostOf(level):R}"));
        EnemyDataLoader.Load();
        ScoreConfig score = ScoreConfig.Load();
        List<string> ids = new(EnemyDataLoader.GetAllIds()) { "inconnu" };
        ids.Sort(StringComparer.Ordinal);
        foreach (string id in ids)
            lines.Add($"score {id} {score.KillPoints(id)}");
        for (int peril = 0; peril <= 10; peril++)
            lines.Add(Invariant($"peril {peril} {PerilDataLoader.EnemyCountMultiplier(peril):R} {PerilDataLoader.EnemyHpMultiplier(peril):R} {PerilDataLoader.EnemyDamageMultiplier(peril):R} {PerilDataLoader.XpMultiplier(peril):R} {PerilDataLoader.ScoreMultiplier(peril):R} {PerilDataLoader.RaritySteps(peril):R}"));
        lines.Add($"peril max {PerilDataLoader.Max} banish {PerilDataLoader.BanishFree} {PerilDataLoader.BanishPerilDivisor}");
        // Q7b : objets (tous, désactivés compris), raretés d'amélioration, offre de niveau.
        List<string> objectIds = new() { "flamme_interieure", "fragment_deternite" };
        foreach (PassiveSouvenirData item in PassiveSouvenirDataLoader.GetAll())
            objectIds.Add(item.Id);
        objectIds.Sort(StringComparer.Ordinal);
        foreach (string id in objectIds)
        {
            PassiveSouvenirData item = PassiveSouvenirDataLoader.Get(id);
            bool offered = PassiveSouvenirDataLoader.GetAll().Exists(candidate => candidate.Id == id);
            lines.Add(Invariant($"object {id} {item.Name} | {item.Description} | {item.Icon} {item.IconSmall} {item.IconColor} max={item.MaxLevel} survival={item.Survival} weight={item.OfferWeight:R} offered={offered}"));
            foreach (PassiveEffectData effect in item.Effects)
                lines.Add(Invariant($"object {id} effect {effect.Stat} {effect.ModifierType} {effect.Step:R}"));
            List<string> parameters = new(item.Parameters.Keys);
            parameters.Sort(StringComparer.Ordinal);
            foreach (string name in parameters)
                lines.Add(Invariant($"object {id} param {name} {item.Parameters[name]:R}"));
            foreach (ObjectMilestoneData milestone in item.Milestones)
            {
                List<string> names = new(milestone.Parameters.Keys);
                names.Sort(StringComparer.Ordinal);
                List<string> values = names.ConvertAll(name => Invariant($"{name}={milestone.Parameters[name]:R}"));
                lines.Add($"object {id} milestone {milestone.Level} {milestone.Effect} {string.Join(",", values)} | {milestone.Text}");
            }
        }
        foreach (UpgradeRarity rarity in UpgradeRoller.Rarities)
            lines.Add(Invariant($"rarity {rarity.Id} rank={rarity.Rank} weight={rarity.Weight:R} stats={rarity.WeaponStats} gain={rarity.WeaponGain:R} integer={rarity.IntegerGain:R} passive={rarity.PassiveGain:R} bump={rarity.BumpChance:R}"));
        foreach (Vestiges.World.ErasureManager.ErasureZonePhase phase in Enum.GetValues<Vestiges.World.ErasureManager.ErasureZonePhase>())
            lines.Add(Invariant($"rarity steps {phase} luck1={UpgradeRoller.BumpSteps(1f, phase, 0):R} peril3={UpgradeRoller.BumpSteps(0f, phase, 3):R}"));
        LevelUpOfferConfig offer = LevelUpOfferConfig.Load();
        lines.Add(Invariant($"offer upgrade={offer.UpgradeWeight:R} min={offer.MinWeight:R}"));
        // Q7c-1 : Mémoriaux, Ateliers, Failles, bénédictions, Oublis.
        MemorialConfig memorial = LandmarkDataLoader.Memorial;
        lines.Add(Invariant($"memorial {memorial.SpriteDormant} {memorial.SpriteAwake} {memorial.SpriteShard} {Bands(memorial.Placement)} spacing={memorial.MinSpacingPx:R} hold={memorial.HoldTime:R} shards={memorial.Shards} dist={memorial.ShardDistanceMin:R}-{memorial.ShardDistanceMax:R} time={memorial.ShardTime:R} pickup={memorial.ShardPickupPx:R} choices={memorial.BlessingChoices} min={memorial.BlessingMinRarity} heal={memorial.HealCost}/{memorial.HealPercent:R} growth={memorial.CostGrowth:R} lift={memorial.LiftOubliCost} reroll={memorial.BlessingRerollCost}"));
        WorkshopConfig workshop = LandmarkDataLoader.Workshop;
        lines.Add(Invariant($"workshop {workshop.Sprite} {Bands(workshop.Placement)} spacing={workshop.MinSpacingPx:R} weapon={workshop.WeaponCost} min={workshop.WeaponMinRarity} retemper={workshop.RetemperCost} temper={workshop.TemperUpgrades} growth={workshop.CostGrowth:R}"));
        RiftConfig rift = LandmarkDataLoader.Rift;
        lines.Add(Invariant($"rift {rift.SpriteOpen} {rift.SpriteClosed} {Bands(rift.Placement)} spacing={rift.MinSpacingPx:R} hold={rift.HoldTime:R} offers={rift.Offers} min={rift.OfferMinRarity} peril={rift.PerilPerOffer} open={rift.MaxOpen} chance={rift.SpawnChance:R} cooldown={rift.SpawnCooldown:R} dist={rift.SpawnDistanceMin:R}-{rift.SpawnDistanceMax:R}"));
        foreach (StatEffectData blessing in BlessingDataLoader.All)
            lines.Add(Invariant($"blessing {blessing.Id} {blessing.NameKey} {blessing.Stat} {blessing.ModifierType} {blessing.Amount:R}"));
        foreach (OubliData oubli in OubliDataLoader.All)
            lines.Add(Invariant($"oubli {oubli.Id} {oubli.NameKey} {oubli.DescriptionKey} {oubli.Effect} {oubli.Amount:R} permanent={oubli.Permanent}"));
        foreach (string line in lines)
            GD.Print($"[CatalogValuesProbe] {line}");
        GD.Print($"[CatalogValuesProbe] RESULT lines={lines.Count}");
        GetTree().Quit(0);
    }

    private static string Bands(List<LandmarkBand> bands) =>
        string.Join(";", bands.ConvertAll(band => Invariant($"{band.Count}@{band.Min:R}-{band.Max:R}")));

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}
