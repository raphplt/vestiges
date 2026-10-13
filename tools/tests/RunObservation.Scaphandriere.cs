using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.Spawn;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>Plan 45 N5 : réserve dans le vrai monde et cinq salves du vrai harpon, avec preuves de déplacement.</summary>
public partial class RunObservation
{
    private async Task CaptureScaphandriereKit()
    {
        AirReserve air = _player.GetNodeOrNull<AirReserve>("AirReserve")
            ?? throw new InvalidOperationException("Le scénario du kit exige la Scaphandrière et sa signature réelle.");
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        _player.AIInputOverride = Vector2.Zero;
        EnemyPool pool = _world.GetNode<EnemyPool>("EnemyPool");
        SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Enemy { IsActive: true } enemy) pool.Return(enemy);
        while (_world.GetNodeOrNull("GameLoadingOverlay") != null)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await MoveToOpenGround(new Vector2(190f, 100f));
        Vector2 origin = FindScaphTrialOrigin(_player.GlobalPosition);
        _player.GlobalPosition = origin;
        _camera.ResetSmoothing();
        ErasureManager erasure = _world.GetNode<ErasureManager>("ErasureManager");
        erasure.ProcessMode = ProcessModeEnum.Disabled;
        using AudioTraceProbe trace = Array.IndexOf(OS.GetCmdlineUserArgs(), "--audio-trace") >= 0
            ? new AudioTraceProbe(_output, _world, _player) : null;
        double elapsed = 0;
        SceneTree tree = GetTree();
        void TraceFrame()
        {
            if (!tree.Paused) elapsed += GetProcessDeltaTime();
            trace?.Sample(elapsed);
        }
        if (trace != null) tree.ProcessFrame += TraceFrame;
        try
        {
            int checks = 0;
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException("Scaph kit : " + message);
                checks++;
                GD.Print("[ScaphKit] PASS " + message);
            }
            void SetMemory(float memory)
            {
                Vector2I center = new(Mathf.FloorToInt(origin.X / erasure.CellSize), Mathf.FloorToInt(origin.Y / erasure.CellSize));
                for (int y = -8; y <= 8; y++)
                    for (int x = -8; x <= 8; x++) erasure.OverrideMemory(center + new Vector2I(x, y), memory);
                erasure.RefreshGroundMemory();
            }
            async Task AirFrame(string name, AirReserveState state, float min, float max)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Check(air.State == state && air.RemainingSeconds >= min && air.RemainingSeconds <= max,
                    $"air {name} état={air.State} réserve={air.RemainingSeconds:F2}");
                SaveCrop("scaph-air-" + name, _player.GlobalPosition + new Vector2(0, -18), new Vector2(100, 90));
                trace?.Sample(elapsed);
            }
            SetMemory(1f);
            await AirFrame("plein", AirReserveState.Full, 5.9f, 6f);
            SetMemory(0.38f);
            await Seconds(3f);
            await AirFrame("consommation", AirReserveState.Consuming, 2.7f, 3.2f);
            Check(air.IsProtecting && _player.ErasurePhase == ErasureManager.ErasureZonePhase.Frayed,
                "consommation protège en vraie phase effilochée");
            await Seconds(3.2f);
            await AirFrame("vide", AirReserveState.Empty, 0f, 0f);
            Check(!air.IsProtecting, "réserve épuisée : malus restaurés");
            SetMemory(1f);
            await Seconds(5f);
            await AirFrame("recharge", AirReserveState.Recharging, 2.8f, 3.2f);
            // Le reste de la recharge avance pendant la galerie ; aucune manipulation de la réserve.
            Dictionary<string, float> distances = new();
            foreach ((string name, string enemyId, string ascension) in new[]
            {
                ("leger", "rodeur", ""), ("lourd", "void_brute", ""), ("boss", EnemyGrammar.BossPartId, ""),
                ("treuil", "rodeur", "salvage_winch"), ("abysse", "rodeur", "abyssal_spear"),
            })
            {
                while (_player.WeaponSlots.Count > 0) _player.RemoveWeapon(0);
                _player.GlobalPosition = origin;
                _camera.ResetSmoothing();
                _player.AddWeapon(WeaponDataLoader.Get("harpoon_rifle"));
                WeaponInstance weapon = _player.WeaponSlots[0];
                if (ascension.Length > 0)
                {
                    while (weapon.CanLevelUp) weapon.ApplyUpgrade(Array.Empty<StatGain>());
                    Check(_player.AscendWeapon(weapon.Id, ascension), $"ascension réelle {ascension}");
                }
                // Une seule salve : l'appel normal ci-dessous garde ciblage, VFX, son, contexte et pool du jeu.
                foreach (Timer timer in (List<Timer>)typeof(Player).GetField("_weaponTimers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_player))
                    timer.Stop();
                Enemy target = spawner.SpawnEventEnemy(enemyId, origin + new Vector2(240f, 0f))
                    ?? throw new InvalidOperationException("Cible du harpon absente : " + enemyId);
                target.SetTicking(false);
                BossHealth boss = null;
                Sprite2D bossProxy = null;
                if (name == "boss")
                {
                    boss = new BossHealth("Ancre de contrôle", 100000f);
                    boss.AddPart(target, 16f, 0f);
                    // boss_part est invisible : le mannequin montre un sprite existant, sans changer le corps testé.
                    EnemyData visible = EnemyDataLoader.Get("void_brute");
                    bossProxy = new Sprite2D
                    {
                        Texture = EnemySpriteLoader.LoadOrGet(visible.Id, visible.Visual.SpriteFolder).GetFrameTexture("SW_idle", 0),
                        Position = new Vector2(0, 10f - visible.Visual.SpriteFeetOffset),
                        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                    };
                    target.AddChild(bossProxy);
                    GD.Print("[ScaphKit] boss : vraie partie BossHealth ; mannequin visuel emprunté à void_brute");
                }
                else
                {
                    typeof(Enemy).GetField("_maxHp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, 100000f);
                    typeof(Enemy).GetField("_currentHp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, 100000f);
                }
                Vector2 targetStart = target.GlobalPosition;
                float hpBefore = boss?.Current ?? target.CurrentHp;
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                typeof(Player).GetMethod("OnWeaponAttackTimeout", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(_player, new object[] { 0 });
                bool caught = false;
                for (int tick = 0; tick < Engine.PhysicsTicksPerSecond * 3; tick++)
                {
                    await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                    if ((boss?.Current ?? target.CurrentHp) >= hpBefore) continue;
                    caught = true;
                    break;
                }
                Check(caught, $"{name} : projectile réel a infligé des dégâts");
                Check(name == "boss" ? !_player.Harpoon.IsActive : _player.Harpoon.IsActive && _player.Harpoon.PullsPlayer == (name == "lourd"),
                    $"{name} : état de traction au premier impact");
                await Seconds(0.05f);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                if (name != "boss")
                    Check(_player.Harpoon.GetNode<Line2D>("SafetyRope").Visible, $"{name} : filin affiché après impact");
                SaveCrop("scaph-harpon-" + name, origin + new Vector2(100f, -15f), new Vector2(180f, 100f));
                trace?.Sample(elapsed);
                // Attendre la fin de la salve évite qu'un projectile précédent rencontre la cible suivante.
                await Seconds(1.6f);
                float playerTravel = _player.GlobalPosition.DistanceTo(origin);
                float targetTravel = target.GlobalPosition.DistanceTo(targetStart);
                float expected = weapon.SpecialEffect.Get(SpecialEffectParam.PullDistance);
                Check(name == "boss" ? playerTravel < 0.1f && targetTravel < 0.1f
                    : name == "lourd" ? playerTravel > 20f && targetTravel < 0.1f
                    : playerTravel < 0.1f && Mathf.Abs(targetTravel - expected) < 2f,
                    $"{name} : joueur={playerTravel:F2}px cible={targetTravel:F2}px plafond={expected:F0}px");
                Check(!_player.Harpoon.IsActive && _player.Harpoon.TrackedLaunches == 0, $"{name} : liaison et registre libérés");
                distances[name] = targetTravel;
                GD.Print(string.Create(CultureInfo.InvariantCulture,
                    $"[ScaphKit] sample={name} player_start={origin} target_start={targetStart} player_end={_player.GlobalPosition} target_end={target.GlobalPosition} damage={hpBefore - (boss?.Current ?? target.CurrentHp):F2}"));
                if (bossProxy != null) { target.RemoveChild(bossProxy); bossProxy.QueueFree(); }
                pool.Return(target);
            }
            Check(distances["treuil"] > distances["leger"] && distances["leger"] > distances["abysse"],
                "trajectoires comparables : treuil > base > abysse");
            Check(air.State == AirReserveState.Full, "recharge complète pendant la galerie");
            GD.Print($"[RunObservation] RESULT scaph-kit checks={checks} air=4 harpoon=5 failures=0");
        }
        finally
        {
            if (trace != null) tree.ProcessFrame -= TraceFrame;
        }
    }

    /// <summary>Les cinq tirs partagent un couloir sec sans obstacle ; le monde n'est pas effacé pour les besoins du banc.</summary>
    private Vector2 FindScaphTrialOrigin(Vector2 around)
    {
        for (int row = -12; row <= 12; row++)
            for (int col = -12; col <= 12; col++)
            {
                Vector2 candidate = around + new Vector2(col * 32, row * 32);
                bool clear = true;
                for (int x = -20; x <= 280; x += 8)
                    for (int y = -20; y <= 20; y += 10)
                    {
                        Vector2 point = candidate + new Vector2(x, y);
                        if (_world.IsWaterAt(point) || ObstacleField.Resolve(ref point, 18f)) clear = false;
                    }
                if (clear) return candidate;
            }
        throw new InvalidOperationException("Aucun couloir sec de 300 px pour le harpon près du cadrage.");
    }
}
