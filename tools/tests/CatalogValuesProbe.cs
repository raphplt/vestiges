using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        lines.Add(Invariant($"score kills_per_vestige {ScoreConfig.Load().KillsPerVestige:R}"));
        foreach (int peril in new[] { 0, 1, 5, 10, 20, 40 })
            lines.Add(Invariant($"peril {peril} {PerilDataLoader.EnemyCountMultiplier(peril):R} {PerilDataLoader.EnemyHpMultiplier(peril):R} {PerilDataLoader.EnemyDamageMultiplier(peril):R}"));
        lines.Add($"peril ceiling {PerilDataLoader.ActiveEnemiesCeiling} banish {PerilDataLoader.BanishFree} {PerilDataLoader.BanishPerilDivisor}");
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
            lines.Add(Invariant($"rarity steps {phase} luck1={UpgradeRoller.BumpSteps(1f, phase):R} luck0={UpgradeRoller.BumpSteps(0f, phase):R}"));
        LevelUpOfferConfig offer = LevelUpOfferConfig.Load();
        lines.Add(Invariant($"offer upgrade={offer.UpgradeWeight:R} min={offer.MinWeight:R}"));
        // Q7c-1 : Mémoriaux, Ateliers, Failles, bénédictions, Oublis.
        MemorialConfig memorial = LandmarkDataLoader.Memorial;
        lines.Add(Invariant($"memorial {memorial.SpriteDormant} {memorial.SpriteAwake} {memorial.SpriteShard} {Bands(memorial.Placement)} spacing={memorial.MinSpacingPx:R} hold={memorial.HoldTime:R} shards={memorial.Shards} dist={memorial.ShardDistanceMin:R}-{memorial.ShardDistanceMax:R} time={memorial.ShardTime:R} pickup={memorial.ShardPickupPx:R} choices={memorial.BlessingChoices} min={memorial.BlessingMinRarity} heal={memorial.HealCost}/{memorial.HealPercent:R} growth={memorial.CostGrowth:R} lift={memorial.LiftOubliCost} reroll={memorial.BlessingRerollCost}"));
        WorkshopConfig workshop = LandmarkDataLoader.Workshop;
        lines.Add(Invariant($"workshop {workshop.Sprite} {Bands(workshop.Placement)} spacing={workshop.MinSpacingPx:R} weapon={workshop.WeaponCost} min={workshop.WeaponMinRarity} retemper={workshop.RetemperCost} temper={workshop.TemperUpgrades} growth={workshop.CostGrowth:R}"));
        WellConfig well = LandmarkDataLoader.Well;
        lines.Add(Invariant($"well {well.Sprite} {Bands(well.Placement)} spacing={well.MinSpacingPx:R} cost={well.ForgetCost} growth={well.ForgetCostGrowth:R} trace={well.ForgetTraceRatio:R}"));
        RiftConfig rift = LandmarkDataLoader.Rift;
        lines.Add(Invariant($"rift {rift.SpriteOpen} {rift.SpriteClosed} {Bands(rift.Placement)} spacing={rift.MinSpacingPx:R} hold={rift.HoldTime:R} offers={rift.Offers} min={rift.OfferMinRarity} peril={rift.PerilPerOffer} open={rift.MaxOpen} chance={rift.SpawnChance:R} cooldown={rift.SpawnCooldown:R} dist={rift.SpawnDistanceMin:R}-{rift.SpawnDistanceMax:R}"));
        foreach (StatEffectData blessing in BlessingDataLoader.All)
            lines.Add(Invariant($"blessing {blessing.Id} {blessing.NameKey} {blessing.Stat} {blessing.ModifierType} {blessing.Amount:R}"));
        foreach (OubliData oubli in OubliDataLoader.All)
            lines.Add(Invariant($"oubli {oubli.Id} {oubli.NameKey} {oubli.DescriptionKey} {oubli.Effect} {oubli.Amount:R} permanent={oubli.Permanent}"));
        // Q7c-2 : coffres, placement, bonus lâchés.
        List<ChestData> chests = ChestDataLoader.GetAll();
        chests.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        foreach (ChestData chest in chests)
            lines.Add(Invariant($"chest {chest.Id} {chest.Rarity} {chest.SpriteClosed} {chest.SpriteOpen} col={chest.ColumnHeight:R}/{chest.ColumnCore:R} fx={chest.FxFamily} open={chest.OpenRangePx:R}px loot={chest.LootTableId}x{chest.LootRolls} down={chest.DowngradeTo ?? "-"}"));
        ChestPlacementData placement = ChestDataLoader.LoadPlacement();
        lines.Add(Invariant($"chest placement spacing={placement.MinSpacingPx:R} candidates={placement.CandidatesPerChest} attempts={placement.AttemptsPerChest} path={placement.PathBonus:R} clear={placement.Clearance} building={placement.BuildingClearance} pointer={placement.PointerRangePx:R}/{placement.PointerMax}"));
        foreach (ChestPlacementGroup group in placement.Groups)
            lines.Add(Invariant($"chest group {group}"));
        // Q7c-3 : petits lieux et repères.
        SmallPlaceConfig places = SmallPlaceDataLoader.Load();
        lines.Add(Invariant($"places enabled={places.Enabled} spacing={places.MinSpacingPx:R} sign={places.SignRangePx:R}"));
        foreach (SmallPlaceData place in places.Places)
            lines.Add(Invariant($"place {place.Id} [{string.Join(",", place.Sprites)}] {place.PromptKey} hold={place.HoldSeconds:R} {place.Reward} amount={place.Amount:R} range={place.AmountMin}-{place.AmountMax} enemies={place.Enemies} ambush={place.AmbushRadiusPx:R}/{place.AmbushTimeoutSeconds:R} max={place.MaxPerMap} chance={place.Chance:R} duration={place.DurationSeconds:R} radius={place.RadiusPx:R} lore=[{string.Join(",", place.Lore)}] {place.Color} {place.Family}"));
        WaymarkConfig waymarks = WaymarkDataLoader.Load();
        lines.Add($"waymarks enabled={waymarks.Enabled}");
        foreach ((string type, WaymarkReward reward) in waymarks.Rewards)
            lines.Add(Invariant($"waymark {type} {reward.NameKey} {reward.Stat ?? "-"} {reward.Amount:R} {reward.ModifierType} rerolls={reward.Rerolls} banishes={reward.Banishes}"));
        FieldBonusConfig field = FieldBonusDataLoader.Load();
        List<string> variants = new(field.VariantChance.Keys);
        variants.Sort(StringComparer.Ordinal);
        lines.Add(Invariant($"field enabled={field.Enabled} max={field.MaxOnGround} life={field.LifetimeSeconds:R} blink={field.BlinkSeconds:R} pickup={field.PickupPx:R} kill={field.KillChance:R} pool={string.Join(",", field.KillPool)} crisis={field.CrisisEnd}/{field.CrisisFirst} variants={string.Join(",", variants.ConvertAll(v => Invariant($"{v}:{field.VariantChance[v]:R}")))}"));
        foreach (FieldBonusData bonus in field.Bonuses)
        {
            List<string> names = new(bonus.Params.Keys);
            names.Sort(StringComparer.Ordinal);
            lines.Add(Invariant($"field bonus {bonus.Id} {bonus.Sprite} {bonus.NameKey} {bonus.Weight:R} {bonus.Effect} {bonus.Color} {string.Join(",", names.ConvertAll(n => Invariant($"{n}={bonus.Params[n]:R}")))}"));
        }
        // Q7c-4a : biomes, ordre des groupes et des répétitions compris (ils pondèrent les tirages).
        List<BiomeData> biomes = BiomeDataLoader.GetAll();
        lines.Add($"biomes order {string.Join(",", biomes.ConvertAll(biome => biome.Id))}");
        foreach (BiomeData biome in biomes)
        {
            lines.Add(Invariant($"biome {biome.Id} {biome.Name} audio={biome.FootstepAudio ?? "-"} danger={biome.DangerLevel} map={biome.MapWeight:R} poi={biome.PoiCountMin}-{biome.PoiCountMax} blend={biome.BlendTerrains}"));
            lines.Add($"biome {biome.Id} exploration={string.Join(",", biome.ExplorationEnemyPool)} resurgence={string.Join(",", biome.ResurgenceEnemyPool)} wang={string.Join(",", biome.WangTileGroups)}");
            foreach ((string terrain, float weight) in biome.TerrainWeights)
                lines.Add(Invariant($"biome {biome.Id} terrain {terrain} {weight:R}"));
            foreach ((string poi, float weight) in biome.PoiPool)
                lines.Add(Invariant($"biome {biome.Id} poi {poi} {weight:R}"));
            foreach (string terrain in biome.TileSources.Keys)
                lines.Add($"biome {biome.Id} tiles {terrain} {string.Join(",", biome.TileSources[terrain])}");
            if (biome.PathStyle is PathStyle style)
                lines.Add(Invariant($"biome {biome.Id} path {style.Tone} ruts={style.Ruts:R} width={style.WidthPx:R}"));
        }
        // Q7c-4b : réglages du monde (world_gen.json), section par section, ordre des listes compris.
        Vestiges.World.WorldGenConfig world = Vestiges.World.WorldGenConfig.Load();
        lines.Add(Invariant($"world map={world.MapRadius}x{world.MapRadiusY} clearance={world.SpawnClearance} ca={world.CaIterations} biomes={world.BiomeCount} fade={world.EdgeFadeWidth} poi_gap={world.PoiMinDistanceBetween} pois={world.PoisEnabled} lore={world.LoreElementsEnabled} fog={world.FogRevealRadius}/{world.FogInitialClearRadius}"));
        lines.Add($"world available {string.Join(",", world.AvailableBiomes)}");
        lines.Add(Invariant($"world layout {world.BiomeLayout.RegionSpacing:R} {world.BiomeLayout.WarpStrength:R} {world.BiomeLayout.SpawnOffsetFactor:R}"));
        foreach (Vestiges.World.WorldGenerator.ZoneConfig zone in world.Zones)
            lines.Add(Invariant($"world zone {zone.MaxRadius:R} {zone.GrassWeight:R} {zone.ConcreteWeight:R} {zone.WaterWeight:R} {zone.ForestWeight:R}"));
        Vestiges.World.PropRules props = world.PropRules;
        lines.Add(Invariant($"world props {props.MinBlockingHeight:R} {props.MinBlockingPixels} {props.FootprintScale:R} {props.GroundDecalMaxHeight:R} {props.OccluderMinHeight:R} {props.TrampleMaxHeight:R} {string.Join(",", props.TramplePrefixes)}"));
        Vestiges.World.GroundBlendConfig blend = world.GroundBlend;
        lines.Add(Invariant($"world blend {blend.Enabled} {blend.BandPx:R} {blend.EdgeNoise:R} {blend.NoiseScale:R} {blend.Grain:R}"));
        Vestiges.World.PathNetworkConfig paths = world.Paths;
        lines.Add(Invariant($"world paths {paths.Enabled} {paths.NeighbourFactor:R} {paths.ExtraEdgeChance:R} {paths.ExtraEdgeFactor:R} {paths.SpawnLinks} {paths.NoiseAmplitude:R} {paths.NoiseFrequency:R} costs={paths.RoadCost:R},{paths.ExistingPathCost:R},{paths.FieldLaneCost:R},{paths.ForestCost:R},{paths.SidewalkCost:R},{paths.PlazaCost:R} wobble={paths.WobblePx:R} margin={paths.EdgeMargin} max={paths.MaxExpansions} style={paths.DefaultStyle.Tone} {paths.DefaultStyle.Ruts:R} {paths.DefaultStyle.WidthPx:R}"));
        Vestiges.World.JunctionPropPlacer.Config junction = world.JunctionProps;
        lines.Add(Invariant($"world junction {junction.Enabled} {junction.BandCells} {junction.Chance:R}"));
        List<string> pairs = new(junction.Pairs.Keys);
        pairs.Sort(StringComparer.Ordinal);
        foreach (string pair in pairs)
            lines.Add($"world junction {pair} {string.Join(",", junction.Pairs[pair])}");
        Vestiges.World.LandsConfig lands = world.Lands;
        List<string> scales = new(lands.CountScales.Keys);
        scales.Sort(StringComparer.Ordinal);
        lines.Add(Invariant($"world lands {lands.Enabled} {lands.MapRadius}x{lands.MapRadiusY} band={lands.MemorialBand.X:R}-{lands.MemorialBand.Y:R} scales={string.Join(",", scales.ConvertAll(k => Invariant($"{k}:{lands.CountScales[k]:R}")))}"));
        // Q7c-4c : fermes et chantiers de la carrière, éléments dans l'ordre.
        Vestiges.World.FarmConfig farm = Vestiges.World.FarmConfig.Load();
        Vestiges.World.SitePlan quarry = Vestiges.World.SitePlan.Load("res://data/world/quarry_sites.json", "collapsed_quarry",
            "res://assets/props/collapsed_quarry/", "chantiers");
        foreach (Vestiges.World.SitePlan plan in new[] { farm.Site, quarry })
        {
            lines.Add(Invariant($"site {plan.LogName} {plan.BiomeId} {plan.Folder} enabled={plan.Enabled} search={plan.SearchRadiusCells} half={plan.HalfExtentPx} reach={plan.PathReachPx:R} spur={plan.SpurStart} width={plan.SpurWidthFactor:R}"));
            foreach (Vestiges.World.SitePlan.Element element in plan.Elements)
                lines.Add(Invariant($"site {plan.LogName} element {string.Join(",", element.Sprites)} at={element.At} to={element.To} step={element.Step:R} blocking={element.Blocking} chance={element.Chance:R}"));
        }
        lines.Add($"farm edges {string.Join(",", farm.ParcelEdges)}");
        lines.Add($"farm orchard {string.Join(";", farm.OrchardTrees.ConvertAll(tree => $"{tree.Base}+{tree.Canopy}"))}");
        lines.Add(Invariant($"farm scenes {string.Join(",", farm.Scenes)} blocking={string.Join(",", farm.BlockingScenes)} chance={farm.SceneChancePerRegion:R}"));
        // Q7d-1 : crises.json, relevé dans les champs des trois lecteurs (présents des deux côtés du changement).
        foreach (Node reader in new Node[] { new Vestiges.Events.CrisisManager(), new Vestiges.Events.CrisisAftermath(), new Vestiges.Events.EndgameManager() })
        {
            System.Type type = reader.GetType();
            type.GetMethod("LoadConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(reader, null);
            List<string> fields = new();
            foreach (System.Reflection.FieldInfo info in type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
            {
                if (info.FieldType == typeof(float) || info.FieldType == typeof(int) || info.FieldType == typeof(string) || info.FieldType == typeof(Vector2))
                    fields.Add(Invariant($"{info.Name}={info.GetValue(reader)}"));
            }
            fields.Sort(StringComparer.Ordinal);
            lines.Add($"crises {type.Name} {string.Join(" ", fields)}");
            reader.Free();
        }
        // Q7d-2 : erasure.json, champs du gestionnaire et des éclats (réflexion), échos et effets par phase.
        foreach (Node reader in new Node[] { new Vestiges.World.ErasureManager(), new Vestiges.World.ErasureMotes() })
        {
            System.Type type = reader.GetType();
            object enabled = type.GetMethod("LoadConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(reader, null);
            List<string> fields = new();
            foreach (System.Reflection.FieldInfo info in type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
            {
                if (info.FieldType == typeof(float) || info.FieldType == typeof(int) || info.FieldType == typeof(bool))
                    fields.Add(Invariant($"{info.Name}={info.GetValue(reader)}"));
            }
            fields.Sort(StringComparer.Ordinal);
            lines.Add($"erasure {type.Name} enabled={enabled} {string.Join(" ", fields)}");
            reader.Free();
        }
        Vestiges.World.EchoConfig echoes = Vestiges.World.EchoConfig.Load();
        List<string> echoFields = new();
        foreach (System.Reflection.FieldInfo info in typeof(Vestiges.World.EchoConfig).GetFields())
            echoFields.Add(Invariant($"{info.Name}={(info.GetValue(echoes) is List<string> list ? string.Join(",", list) : info.GetValue(echoes))}"));
        echoFields.Sort(StringComparer.Ordinal);
        lines.Add($"erasure echoes {string.Join(" ", echoFields)}");
        foreach (Vestiges.World.ErasureManager.ErasureZonePhase phase in Enum.GetValues<Vestiges.World.ErasureManager.ErasureZonePhase>())
            lines.Add(Invariant($"erasure effect {phase} {Vestiges.World.ErasureEffects.For(phase)}"));
        // Q7d-3 : micro-événements, cadence et réglages par sorte (commentaires « _ » exclus).
        RunEventDataLoader.Load();
        RunEventSchedule schedule = RunEventDataLoader.Schedule;
        lines.Add(Invariant($"events schedule first={schedule.FirstEventMinSec:R}-{schedule.FirstEventMaxSec:R} gap={schedule.GapMinSec:R}-{schedule.GapMaxSec:R} margin={schedule.CrisisMarginSec:R} calm={schedule.AfterCrisisCalmSec:R} recent={schedule.RecentMemory}/{schedule.RecentWeightFactor:R} progress={schedule.ProgressEmitIntervalSec:R} fixed={string.Join(",", schedule.FixedEvents.ConvertAll(f => Invariant($"{f.EventId}@{f.AtSec:R}")))}"));
        foreach (RunEventData runEvent in RunEventDataLoader.Events)
        {
            List<string> values = new();
            foreach ((string key, float number) in runEvent.Numbers)
                if (!key.StartsWith('_'))
                    values.Add(Invariant($"{key}={number:R}"));
            foreach ((string key, string text) in runEvent.Texts)
                if (!key.StartsWith('_'))
                    values.Add($"{key}={text}");
            foreach ((string key, List<string> list) in runEvent.Lists)
                if (!key.StartsWith('_'))
                    values.Add($"{key}=[{string.Join(",", list)}]");
            values.Sort(StringComparer.Ordinal);
            lines.Add(Invariant($"events {runEvent.Id} {runEvent.Kind} {runEvent.TitleKey} {runEvent.ObjectiveKey} min={runEvent.MinSec:R} weight={runEvent.Weight:R} duration={runEvent.DurationSec:R} {string.Join(" ", values)}"));
        }
        // Q7e-1 : spawn_flow.json, champs de SpawnManager par réflexion (présents des deux côtés), listes et placement.
        Vestiges.Spawn.SpawnManager spawn = new();
        System.Reflection.BindingFlags hidden = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        typeof(Vestiges.Spawn.SpawnManager).GetMethod("LoadScalingConfig", hidden)!.Invoke(spawn, null);
        List<string> spawnFields = new();
        foreach (System.Reflection.FieldInfo info in typeof(Vestiges.Spawn.SpawnManager).GetFields(hidden))
        {
            object value = info.GetValue(spawn);
            if (value is float or int)
                spawnFields.Add(Invariant($"{info.Name}={value}"));
            else if (value is List<float> numbers)
                spawnFields.Add(Invariant($"{info.Name}=[{string.Join(",", numbers.ConvertAll(n => n.ToString("R", CultureInfo.InvariantCulture)))}]"));
            else if (value is List<string> names)
                spawnFields.Add($"{info.Name}=[{string.Join(",", names)}]");
            else if (value is Vestiges.Spawn.SpawnPositionPicker picker)
                spawnFields.Add(Invariant($"{info.Name}={picker.MarginMin:R},{picker.MarginMax:R},{picker.ForwardBias:R},{picker.ForwardArcDegrees:R},{picker.LeadSeconds:R},{picker.MinDistance:R}"));
        }
        spawnFields.Sort(StringComparer.Ordinal);
        foreach (string spawnField in spawnFields)
            lines.Add($"spawn {spawnField}");
        spawn.Free();
        // Q7e-2 : foule et variantes (crowd.json, enemies/_variants.json).
        lines.Add(Invariant($"crowd {CrowdDataLoader.SeparationRadius:R} {CrowdDataLoader.SeparationStrength:R} {CrowdDataLoader.SeparationMaxPush:R} {CrowdDataLoader.ContactDistance:R}"));
        foreach (string variantId in new[] { "elite", "champion", "aberration" })
        {
            EnemyVariantData v = EnemyVariantDataLoader.GetVariant(variantId);
            VariantLevelScaling level = v.LevelScaling;
            string scaling = level == null ? "none" : Invariant($"{level.FightSeconds:R},{level.DpsCoefficient:R},{level.DpsExponent:R},{level.DamageBase:R},{level.DamagePerLevel:R}");
            lines.Add(Invariant($"variant {v.Id} [{v.TitleMasculine}|{v.TitleFeminine}] target={v.HpTarget:R} min={v.HpMultMin:R} hp={v.HpMult:R} dmg={v.DamageMult:R} speed={v.SpeedMult:R} scale={v.Scale:R} xp={v.XpMult:R} affixes={v.AffixCount} pool=[{string.Join(",", v.AffixPool)}] essence={v.BonusEssence} chest={v.RewardChest}@{v.RewardChestChance:R} outline={v.OutlineColor.ToHtml()} shake={v.DeathShake} plate={v.Nameplate} shader={v.AberrationShader} level={scaling}"));
        }
        foreach (string affixId in new[] { "enraged", "armored", "regenerant", "explosive", "swift" })
        {
            EnemyAffixData a = EnemyVariantDataLoader.GetAffix(affixId);
            lines.Add(Invariant($"affix {a.Id} [{a.NameMasculine}|{a.NameFeminine}] color={a.Color.ToHtml()} hp={a.HpMult:R} dmg={a.DamageMult:R} speed={a.SpeedMult:R} taken={a.DamageTakenMult:R} regen={a.RegenRatioPerSec:R}/{a.RegenPauseAfterHitSec:R} blast={a.DeathExplosionRadius:R}x{a.DeathExplosionDamageMult:R}"));
        }
        NaturalEliteConfig elites = EnemyVariantDataLoader.NaturalElites;
        lines.Add(Invariant($"elites start={elites.StartSec:R} interval={elites.IntervalMinSec:R}-{elites.IntervalMaxSec:R} alive={elites.MaxAlive} pool=[{string.Join(",", elites.AffixPool)}] late={elites.LateFromMinute:R}-{elites.LateFullMinute:R} {elites.LateIntervalMinSec:R}-{elites.LateIntervalMaxSec:R} alive={elites.LateMaxAlive} hp={elites.LateHpMult:R} dmg={elites.LateDamageMult:R} extra={elites.LateExtraAffixAt:R}"));
        PhaseModifierConfig phases = EnemyVariantDataLoader.PhaseModifiers;
        lines.Add(Invariant($"phases aberration={phases.CrisisAberrationChance:R}+{phases.CrisisAberrationChancePerIntensity:R} affix={phases.AffixChanceCrisis:R},{phases.AffixChanceLateGame:R},{phases.AffixChanceEndgame:R} pool=[{string.Join(",", phases.AffixPool)}]"));
        // Q7f-1 : personnages (characters.json), dans l'ordre du fichier.
        foreach (CharacterData character in CharacterDataLoader.GetAll())
        {
            CharacterStats stats = character.BaseStats;
            lines.Add(Invariant($"character {character.Id} [{character.Name}|{character.Description}] weapon={character.StartingWeaponId} passive={character.PassivePerk} speed={stats.Speed:R} dmg={stats.AttackDamage:R} as={stats.AttackSpeed:R} range={stats.AttackRange:R} hp={stats.MaxHp:R} regen={stats.RegenRate:R} shield={stats.Shield:R} interact={stats.InteractRange:R} color={character.VisualColor.ToHtml()} accent={character.AccentColor.ToHtml()} sprite={character.SpriteFolder} feet={character.SpriteFeetOffset:R} mobility={character.MobilityProfile ?? "null"}"));
        }
        // Q7f-2 : signatures (perks.json), dans l'ordre du fichier.
        foreach (PerkData perk in PerkDataLoader.GetAll())
        {
            string blocks = Invariant($"momentum={(perk.Momentum == null ? "-" : $"{perk.Momentum.RampSeconds:R},{perk.Momentum.SpeedBonus:R},{perk.Momentum.AttackSpeedBonus:R},{perk.Momentum.StopGraceSeconds:R}")} echo={(perk.Echo == null ? "-" : $"{perk.Echo.Seconds:R},{perk.Echo.DamageRatio:R}")} opening={(perk.OpeningStrike == null ? "-" : $"{perk.OpeningStrike.BaseChance:R},{perk.OpeningStrike.GainPerLevel:R},{perk.OpeningStrike.MaxChance:R}")} pressure={(perk.Pressure == null ? "-" : $"{perk.Pressure.FrayedBonus:R},{perk.Pressure.ErasedBonus:R}")} route={perk.RouteEndurance?.ToString() ?? "-"} shock={perk.Shockwave?.ToString() ?? "-"}");
            string effects = perk.Effects == null ? "null" : string.Join(";", perk.Effects.ConvertAll(e => Invariant($"{e.Stat}:{e.Modifier:R}:{e.ModifierType}")));
            lines.Add(Invariant($"perk {perk.Id} [{perk.Name}|{perk.Description}] stat={perk.Stat ?? "null"}:{perk.Modifier:R}:{perk.ModifierType ?? "null"} passive={perk.IsPassive} character={perk.CharacterId ?? "null"} effects={effects} {blocks}"));
        }
        // Q7f-3 : constellations et Souvenirs, montées d'armes.
        foreach (ConstellationData constellation in SouvenirDataLoader.GetAllConstellations())
            lines.Add(Invariant($"constellation {constellation.Order} {constellation.Id} [{constellation.Name}|{constellation.Description}] {constellation.Color.R:R},{constellation.Color.G:R},{constellation.Color.B:R},{constellation.Color.A:R}"));
        List<SouvenirData> souvenirs = SouvenirDataLoader.GetAll();
        souvenirs.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        foreach (SouvenirData souvenir in souvenirs)
            lines.Add($"souvenir {souvenir.Id} {souvenir.ConstellationId} [{souvenir.Name}|{souvenir.Text}]");
        lines.Add($"weapon_max_level {WeaponUpgradeDataLoader.GetWeaponMaxLevel()}");
        foreach (string stat in new[] { "damage", "attack_speed", "range", "arc_angle", "cone_angle_end", "knockback", "projectile_speed",
            "orbital_speed", "chain_range", "homing_strength", "projectile_count", "projectile_pierce", "chain_targets", "inconnue" })
        {
            WeaponUpgradeStatConfig config = WeaponUpgradeDataLoader.GetStatConfig(stat);
            lines.Add(config == null ? $"upgrade {stat} null" : Invariant($"upgrade {config.Key} step={config.Step:R} additive={config.Additive} integer={config.Integer} max={config.Max:R}"));
        }
        // Q7g-1 : tables de butin.
        foreach (string tableId in new[] { "chest_common", "chest_rare", "chest_epic", "chest_lore", "poi_anomaly", "poi_cache", "poi_sanctuary", "poi_searchable", "loot_table_id" })
        {
            LootTableData table = LootTableLoader.Exists(tableId) ? LootTableLoader.Get(tableId) : null;
            lines.Add(table == null ? $"loot {tableId} null"
                : $"loot {table.Id} " + string.Join(";", table.Entries.ConvertAll(e => Invariant($"{e.Type}:{e.Item}:{e.Weight:R}:{e.MinAmount}-{e.MaxAmount}:{e.FallbackEssence}"))));
        }
        // Q7g-2 : lieux (pois.json), dans l'ordre du fichier.
        foreach (PoiData poi in PoiDataLoader.GetAll())
            lines.Add(Invariant($"poi {poi.Id} [{poi.Name}] {poi.Type} size={poi.Size} dist={poi.MinDistanceFromSpawn}-{poi.MaxDistanceFromSpawn} white=[{string.Join(",", poi.BiomeWhitelist)}] black=[{string.Join(",", poi.BiomeBlacklist)}] color={poi.Color.R:R},{poi.Color.G:R},{poi.Color.B:R},{poi.Color.A:R} outline={poi.OutlineColor.R:R},{poi.OutlineColor.G:R},{poi.OutlineColor.B:R},{poi.OutlineColor.A:R} shape={poi.Shape} interaction={poi.InteractionType} search={poi.SearchTime:R} loot={poi.LootTableId}x{poi.LootRolls} guards=[{string.Join(",", poi.EnemyGuards)}]"));
        // Q7g-3 : défense, pistage, budget d'effets (champs privés par réflexion, présents des deux côtés).
        DefenseConfig defense = DefenseConfig.Load();
        lines.Add(Invariant($"defense {defense.HurtInvulnerabilitySeconds:R} {defense.RegenDelayAfterDamageSeconds:R} {defense.ShieldRechargeDelaySeconds:R} {defense.ShieldRechargeSeconds:R} {defense.ArmorHalfValue:R} {defense.ArmorMaxReduction:R} {defense.LifestealMaxHpPerSecond:R} {defense.LifestealTickSeconds:R}"));
        System.Reflection.BindingFlags staticHidden = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
        typeof(Vestiges.Combat.EnemyTracking).GetMethod("LoadConfig", staticHidden)!.Invoke(null, null);
        foreach (string trackingField in new[] { "_engageDistanceSq", "_loseTrackDistanceSq", "_loseTrackSeconds", "_wanderSpeedFactor", "_wanderTurnSeconds" })
            lines.Add(Invariant($"tracking {trackingField}={typeof(Vestiges.Combat.EnemyTracking).GetField(trackingField, staticHidden)!.GetValue(null):R}"));
        typeof(Vestiges.Combat.FxBudget).GetMethod("EnsureLoaded", staticHidden)!.Invoke(null, null);
        foreach (string budgetField in new[] { "Full", "Reduced" })
            lines.Add($"fx_budget {budgetField}=[{string.Join(",", (int[])typeof(Vestiges.Combat.FxBudget).GetField(budgetField, staticHidden)!.GetValue(null)!)}]");
        // Q7g-4a : bilan de run, palette de la carte, palette de rareté, visuels d'armes.
        RunSummaryConfig summary = RunSummaryConfig.Load();
        lines.Add(Invariant($"run_summary {summary.PixelsPerMeter:R} {summary.DistanceStepSeconds:R}"));
        MapPalette map = MapPalette.Load();
        lines.Add($"map road={map.Road.ToHtml()} path={map.Path.ToHtml()}");
        foreach (string biomeId in new[] { "forest_reclaimed", "urban_ruins", "wild_fields", "swamp", "collapsed_quarry", "inconnu" })
            lines.Add($"map {biomeId} " + string.Join(",", Enum.GetValues<Vestiges.World.TerrainType>().Select(terrain => map.Terrain(biomeId, terrain).ToHtml())));
        for (int phase = 0; phase < 5; phase++)
            lines.Add($"map phase {phase} {map.PhaseColor(phase).ToHtml()} {map.ApplyPhase(Colors.White, phase, 1, 2).ToHtml()} {map.ApplyPhase(Colors.White, phase, 0, 0).ToHtml()}");
        foreach (string rarityId in new[] { "common", "uncommon", "rare", "epic", "legendary", "lore", "memorial", "rift", "inconnue", "" })
        {
            RarityColors rarity = RarityPalette.Colors(rarityId);
            lines.Add($"rarity [{rarityId}] {rarity.Main.ToHtml()} {rarity.Light.ToHtml()} {rarity.Dark.ToHtml()} {RarityPalette.DisplayName(rarityId)}");
        }
        WeaponVisualConfig visuals = WeaponVisualConfig.Load();
        lines.Add(Invariant($"weapon_visuals {visuals.SizeResponse:R} {visuals.MaxScale:R} {visuals.ShotCooldownShare:R} {visuals.ChainedShotVolumeDb:R} {visuals.ChainedShotPitchVariance:R} {visuals.WaveInterval:R} {visuals.ConeMaxFronts}"));
        // Q7g-4b : musique, catalogue des stats, mobilité.
        MusicConfig music = MusicConfig.Load();
        foreach (KeyValuePair<MusicIntent, MusicConfig.Cue> cue in music.Cues.OrderBy(entry => entry.Key))
            lines.Add(Invariant($"music {cue.Key} {cue.Value.Key} fade={cue.Value.FadeSeconds:R} loop={cue.Value.Loop} start={cue.Value.StartSeconds:R}"));
        lines.Add(Invariant($"music combat {music.CombatRadiusPx:R} {music.CombatSampleSeconds:R} {music.CombatEnterEnemies} {music.CombatEnterHoldSeconds:R} {music.CombatExitEnemies} {music.CombatExitHoldSeconds:R} [{string.Join(",", music.CombatPeakVariants.OrderBy(id => id, StringComparer.Ordinal))}]"));
        using (System.Text.Json.JsonDocument stats = System.Text.Json.JsonDocument.Parse(FileAccess.GetFileAsString("res://data/ui/stats.json")))
        {
            foreach (System.Text.Json.JsonElement stat in stats.RootElement.GetProperty("stats").EnumerateArray())
            {
                string id = stat.GetProperty("id").GetString();
                lines.Add($"stat {id} {StatCatalog.Name(id)} {StatCatalog.Display(id)} {StatCatalog.Property(id) ?? "-"} {StatCatalog.Format(id, 1.5f)} {StatCatalog.FormatBonus(id, 0.25f, false)}");
            }
        }
        lines.Add($"stat inconnue {StatCatalog.Name("inconnue")} {StatCatalog.Display("inconnue")} {StatCatalog.Format("inconnue", 1.5f)}");
        foreach (string profile in new[] { null, "phase", "glide", "float_jump" })
        {
            MobilityConfig m = MobilityConfig.Load(profile);
            lines.Add(Invariant($"mobility {profile ?? "-"} [{m.Name}|{m.Description}] {m.DurationSeconds:R} {m.SpeedMultiplier:R} {m.CooldownSeconds:R} {m.InputBufferSeconds:R} {m.BriefResponse} {m.AccelerationSeconds:R} {m.BrakingSeconds:R} {m.HurtRecoverySeconds:R} {m.InvulnerabilitySeconds:R} {m.TrialInvulnerabilitySeconds:R} {m.WaterSpeedFactor:R} {m.TurnRateDegrees:R} {m.SlidesAlongWalls} {m.VisualHeightPixels:R} {m.IgnoreWaterWhileActive} {m.StartAudio} {m.EndAudio}"));
        }
        // Q7h-1 : banque de sons, dictionnaires de l'AudioManager par réflexion (présents des deux côtés).
        AudioManager audio = AudioManager.Instance;
        foreach (string bankField in new[] { "_paths", "_soundVolumes", "_sfxMinIntervals", "_soundPriorities", "_soundMaxVoices", "_soundBuses", "_soundVolumeVariance", "_soundDucks", "_soundVariants" })
        {
            System.Collections.IDictionary bank = (System.Collections.IDictionary)typeof(AudioManager).GetField(bankField, hidden)!.GetValue(audio);
            List<string> entries = new();
            foreach (System.Collections.DictionaryEntry entry in bank)
            {
                object value = entry.Value;
                FormatBankValue(ref value);
                entries.Add(Invariant($"{entry.Key}={value}"));
            }
            entries.Sort(StringComparer.Ordinal);
            lines.Add($"sounds {bankField} {entries.Count} {string.Join(";", entries)}");
        }
        // Q7h-2 : décors par biome (data/props/*.json), lus par PropSpawner.LoadPropConfigs (présent des deux côtés).
        System.Collections.IDictionary propConfigs = (System.Collections.IDictionary)typeof(Vestiges.World.PropSpawner)
            .GetMethod("LoadPropConfigs", staticHidden)!.Invoke(null, null)!;
        List<string> propLines = new();
        foreach (System.Collections.DictionaryEntry entry in propConfigs)
        {
            Vestiges.World.BiomePropConfig config = (Vestiges.World.BiomePropConfig)entry.Value;
            propLines.Add(Invariant($"props {entry.Key} {config.BiomeId} density={config.Density:R} count={config.Props.Count}"));
            foreach (Vestiges.World.PropDefinition prop in config.Props)
                propLines.Add(Invariant($"props {config.BiomeId} {prop.Id} [{prop.Name}] w={prop.Weight:R} terrain=[{string.Join(",", prop.Terrain)}] base={prop.SpriteBasePath} canopy={prop.SpriteCanopyPath ?? "-"} offset={prop.CanopyOffsetY:R} blocking={prop.Blocking} footprint={prop.FootprintScale:R} min={prop.MinDistance}"));
        }
        propLines.Sort(StringComparer.Ordinal);
        lines.AddRange(propLines);
        foreach (string line in lines)
            GD.Print($"[CatalogValuesProbe] {line}");
        GD.Print($"[CatalogValuesProbe] RESULT lines={lines.Count}");
        GetTree().Quit(0);
    }

    /// <summary>Variantes d'un son : chemins des flux, dans l'ordre.</summary>
    private static void FormatBankValue(ref object value)
    {
        System.Reflection.FieldInfo streams = value?.GetType().GetField("Streams");
        if (streams != null)
            value = string.Join(",", Array.ConvertAll((AudioStream[])streams.GetValue(value)!, stream => stream.ResourcePath));
        else if (value is float number)
            value = number.ToString("R", CultureInfo.InvariantCulture);
    }

    private static string Bands(List<LandmarkBand> bands) =>
        string.Join(";", bands.ConvertAll(band => Invariant($"{band.Count}@{band.Min:R}-{band.Max:R}")));

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}
