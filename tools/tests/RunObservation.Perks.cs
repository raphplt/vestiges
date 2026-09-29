using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Spawn;

namespace Vestiges.Tests;

/// <summary>
/// --capture-perks [--perk-scene survival|overflow|priority|carry] : effets des perks en run et leurs retours (plan 05,
/// B2). Une scène par run, le joueur n'ayant que quatre emplacements. survival : réserve de Prévoyance et part
/// récupérable de Reprise sur la barre de PV. overflow : l'arc surpuissant achève des rôdeurs, case d'arme éclairée et
/// chiffre renforcé. priority : une élite plus lointaine que les rôdeurs est visée et entourée de son repère.
/// carry : la Cloche ralentit une grappe, puis une transmission certaine montre le trait vers le receveur.
/// </summary>
public partial class RunObservation
{
    private async Task CapturePerks(string scene)
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        await Frames(90);
        switch (scene)
        {
            case "survival":
                await CaptureSurvivalPerks();
                break;
            case "overflow":
                await CaptureOverflow();
                break;
            case "priority":
                await CapturePriorityTargeting();
                break;
            case "carry":
                await CaptureCarryControl();
                break;
            default:
                GD.PushError($"[Perks] Scène inconnue : {scene}");
                break;
        }
    }

    private async Task CaptureSurvivalPerks()
    {
        Acquire("overheal_reserve", "rally");
        await Frames(10);
        SaveFrame("perks-survival-0-reserve-full");

        _player.IsGodMode = false;
        _player.DisableDefenseForTests();
        _player.TakeDamage(_player.EffectiveMaxHp * 0.55f);
        _player.IsGodMode = true;
        await Frames(10);
        SaveFrame("perks-survival-1-wounded");
        GD.Print($"[Perks] après le coup : PV {_player.CurrentHp:0.#}, réserve {_player.SpecializationRuntime.Reserve.Stock:0.#}, "
            + $"récupérable {_player.SpecializationRuntime.Rally.Recoverable:0.#}");

        await Seconds(2.8f);
        SaveFrame("perks-survival-2-rally-ending");
        await Seconds(1.5f);
        SaveFrame("perks-survival-3-rally-expired");
        GD.Print($"[Perks] RESULT fenêtre ouverte après 4,3 s : {_player.SpecializationRuntime.Rally.IsOpen}");
    }

    private async Task CaptureOverflow()
    {
        Acquire("overflow");
        _player.ApplyPerkModifier("damage", 6f, "multiplicative");
        SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
        Vector2 origin = _player.GlobalPosition;
        int consumed = 0;
        GetNode<EventBus>("/root/EventBus").EnemyDamageResolved += result => consumed += result.CarriedDamage > 0f ? 1 : 0;
        for (int wave = 0; wave < 4; wave++)
        {
            for (int index = 0; index < 5; index++)
                spawner.ForceSpawnEnemy("rodeur", origin + Vector2.FromAngle(index * Mathf.Tau / 5f + wave) * (90f + index * 12f));
            for (int shot = 0; shot < 4; shot++)
            {
                await Frames(8);
                await SkipLevelChoices();
                SavePlayerCloseUp($"{_output}/perks-overflow-{wave}-{shot}.png", new Vector2(220f, 140f));
            }
            SaveFrame($"perks-overflow-hud-{wave}");
        }
        GD.Print($"[Perks] RESULT impacts renforcés : {consumed}");
    }

    private async Task CapturePriorityTargeting()
    {
        Acquire("priority_targeting");
        // Arc inoffensif : aucune élimination, donc aucun choix de niveau ne vient masquer la scène.
        _player.ApplyPerkModifier("damage", 1e-4f, "multiplicative");
        SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
        Enemy elite = spawner.SpawnEventEnemy("rodeur", _player.GlobalPosition + new Vector2(-170f, 40f), "elite");
        for (int index = 0; index < 3; index++)
            spawner.ForceSpawnEnemy("rodeur", _player.GlobalPosition + Vector2.FromAngle(index * 0.9f) * 70f);
        for (int shot = 0; shot < 3; shot++)
        {
            await Frames(20);
            SavePlayerCloseUp($"{_output}/perks-priority-{shot}.png", new Vector2(240f, 150f));
        }
        Node marker = _player.SpecializationRuntime.GetNode("PriorityTargetMarker");
        GD.Print($"[Perks] RESULT repère sur l'élite : {marker.Get("visible").AsBool() && IsInstanceValid(elite)}");
    }

    private async Task CaptureCarryControl()
    {
        Acquire("carry_control");
        _player.RemoveWeapon(0);
        _player.AddWeapon(WeaponDataLoader.Get("teachers_bell"));
        SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
        int transmitted = 0;
        int kills = 0;
        int oneShots = 0;
        System.Collections.Generic.Dictionary<EnemyLife, ulong> lastHit = new();
        System.Collections.Generic.List<ulong> gaps = new();
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        bus.EnemyDamageResolved += result =>
        {
            if (!result.Fatal)
                lastHit[result.Target] = Time.GetTicksMsec();
        };
        bus.EnemyKillResolved += kill =>
        {
            kills++;
            if (lastHit.TryGetValue(kill.Target, out ulong previous))
                gaps.Add(Time.GetTicksMsec() - previous);
            else
                oneShots++;
            transmitted += kill.Slow.Remaining > 0f ? 1 : 0;
        };
        for (int index = 0; index < 14; index++)
            spawner.ForceSpawnEnemy("rodeur", _player.GlobalPosition + Vector2.FromAngle(index * 2.4f) * (40f + index * 9f));
        // Le trait est bref : l'image est prise à la frame qui suit une transmission.
        int seen = 0;
        int shots = 0;
        ulong deadline = Time.GetTicksMsec() + 15000;
        while (Time.GetTicksMsec() < deadline && shots < 6)
        {
            await Frames(1);
            await SkipLevelChoices();
            if (transmitted == seen)
                continue;
            seen = transmitted;
            SavePlayerCloseUp($"{_output}/perks-carry-{shots++}.png", new Vector2(200f, 130f));
        }
        GD.Print($"[Perks] Cloche seule, 15 s : {kills} éliminations, {oneShots} d'un seul coup, {transmitted} encore ralenties ; écarts avant le coup fatal (ms) : {string.Join(", ", gaps)}");

        // Transmission certaine pour juger le trait : terrain vidé, une victime ralentie par la Cloche, une voisine.
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Enemy existing && existing.IsActive)
                _world.GetNode<EnemyPool>("EnemyPool").Return(existing);
        await Frames(2);
        WeaponInstance bell = _player.WeaponSlots[0];
        Enemy victim = spawner.SpawnEventEnemy("rodeur", _player.GlobalPosition + new Vector2(-60f, -60f));
        Enemy receiver = spawner.SpawnEventEnemy("rodeur", _player.GlobalPosition + new Vector2(30f, -110f));
        await Frames(2);
        victim.ApplySlow(0.5f, 2f, _player.BeginAttack(bell, 5f));
        victim.TakeDamage(1e6f, source: _player.BeginAttack(bell, 1f));
        // Lu tout de suite : un coup de Cloche ultérieur sur la voisine remplacerait l'origine par un ralentissement natif.
        bool propagated = receiver.SlowControl.Origin == ControlOrigin.Propagated;
        for (int frame = 0; frame < 3; frame++)
        {
            await Frames(2);
            SavePlayerCloseUp($"{_output}/perks-link-{frame}.png", new Vector2(160f, 100f));
        }
        GD.Print($"[Perks] RESULT voisine ralentie par Propagation : {propagated}");
    }

    private void Acquire(params string[] ids)
    {
        foreach (string id in ids)
            if (!_player.AcquireSpecialization(PerkSpecializationDataLoader.Get(id)))
                GD.PushError($"[Perks] Perk refusé : {id}");
    }

    /// <summary>Les éliminations ont pu ouvrir des choix de niveau : ils sont passés pour laisser voir le monde.</summary>
    private async Task SkipLevelChoices()
    {
        Node screen = _world.GetNode("LevelUpScreen");
        MethodInfo skip = screen.GetType().GetMethod("Skip", BindingFlags.NonPublic | BindingFlags.Instance);
        for (int guard = 0; guard < 40 && screen.Get("visible").AsBool(); guard++)
        {
            skip.Invoke(screen, null);
            await Frames(2);
        }
    }
}
