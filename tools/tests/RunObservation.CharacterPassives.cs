using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.Spawn;

namespace Vestiges.Tests;

public partial class RunObservation
{
    /// <summary>--capture-route-forge --character vagabond : deux vrais chargements de Main, sans mesure de densité.</summary>
    private async Task CaptureRouteAndForge()
    {
        while (_world.GetNodeOrNull("GameLoadingOverlay") != null)
            await Frames(1);
        _camera.MakeCurrent();
        PreparePassiveCapture();
        RouteEndurance reserve = _player.GetNode<RouteEndurance>("RouteEndurance");
        Vector2 start = _player.GlobalPosition;
        // Le vrai FogOfWar révèle les emplacements du scénario ; aucun signal de découverte n'est simulé.
        for (int step = 0; reserve.Charges < 3 && step < 81; step++)
        {
            Vector2 candidate = start + new Vector2((step % 9 - 4) * 640f, (step / 9 - 4) * 640f);
            if (!_player.ClampHarpoonTravel(candidate, Vector2.Right).IsEqualApprox(Vector2.Right))
                continue;
            _player.GlobalPosition = candidate;
            _camera.ResetSmoothing();
            await Frames(3);
        }
        if (reserve.Charges != 3)
            throw new InvalidOperationException($"Terrain inédit : {reserve.Charges}/3 charges, {reserve.CellsUntilCharge} cellules restantes.");
        _player.GlobalPosition = start;
        _camera.ResetSmoothing();
        _camera.ForceUpdateScroll();
        await Frames(15);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        SaveCrop("vagabond-reserve", _player.GlobalPosition + new Vector2(0f, -25f), new Vector2(120f, 80f));
        _player.IsGodMode = false;
        _player.TakeDamage(20f);
        if (reserve.Charges != 2)
            throw new InvalidOperationException("La blessure réelle n'a pas consommé une seule charge.");
        _player.IsGodMode = true;
        await Frames(2);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        SaveCrop("vagabond-blessure", _player.GlobalPosition + new Vector2(0f, -25f), new Vector2(120f, 80f));

        ulong seed = GetNode<GameManager>("/root/GameManager").RunSeed;
        GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Hub);
        GetTree().Root.RemoveChild(_world);
        _world.QueueFree();
        await Frames(2);
        await LoadRun(seed, "forgeuse");
        while (_world.GetNodeOrNull("GameLoadingOverlay") != null)
            await Frames(1);
        _camera.MakeCurrent();
        PreparePassiveCapture();
        await MoveToOpenGround(new Vector2(180f, 100f));
        TemperedImpact forge = _player.GetNode<TemperedImpact>("TemperedImpact");
        SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
        Enemy target = spawner.SpawnEventEnemy("rodeur", _player.GlobalPosition + Vector2.Right * 70f);
        Enemy neighbour = spawner.SpawnEventEnemy("rodeur", target.GlobalPosition + Vector2.Right * 30f);
        FieldInfo hp = typeof(Enemy).GetField("_currentHp", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (Enemy enemy in new[] { target, neighbour })
        {
            enemy.SetTicking(false);
            hp.SetValue(enemy, 10000f);
        }
        CrowdIndex.MarkMoved();
        MethodInfo attack = typeof(Player).GetMethod("OnWeaponAttackTimeout", BindingFlags.Instance | BindingFlags.NonPublic);
        for (int hit = 0; hit < 3; hit++)
        {
            attack.Invoke(_player, new object[] { 0 });
            await Frames(3);
        }
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        SaveCrop("forgeuse-trois-coups", _player.GlobalPosition + new Vector2(40f, -20f), new Vector2(140f, 90f));
        attack.Invoke(_player, new object[] { 0 });
        await Frames(2);
        if (forge.WavesTriggered != 1)
            throw new InvalidOperationException($"Redresser : une seule onde attendue, {forge.WavesTriggered} produites.");
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        SaveCrop("forgeuse-onde", _player.GlobalPosition + new Vector2(60f, -20f), new Vector2(140f, 90f));
        GD.Print("[RunObservation] RESULT route-forge charges=3→2 wave=1 failures=0");
    }

    private void PreparePassiveCapture()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        _world.GetNode("ErasureManager").ProcessMode = ProcessModeEnum.Disabled;
        _player.AIInputOverride = Vector2.Zero;
        foreach (Timer timer in (List<Timer>)typeof(Player).GetField("_weaponTimers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_player))
        {
            timer.Stop();
            timer.Paused = true;
        }
        EnemyPool pool = _world.GetNode<EnemyPool>("EnemyPool");
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Enemy { IsActive: true } enemy) pool.Return(enemy);
    }
}
