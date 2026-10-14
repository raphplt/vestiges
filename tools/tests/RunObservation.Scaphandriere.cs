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

/// <summary>Plan 45 N5 : Pression dans le vrai monde et cinq salves du vrai harpon, avec preuves de déplacement.</summary>
public partial class RunObservation
{
    private async Task CaptureScaphandriereKit()
    {
        ErasurePressure pressure = _player.GetNodeOrNull<ErasurePressure>("ErasurePressure")
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
            async Task PressureFrame(string name, int intensity, float multiplier)
            {
                await Seconds(0.1f);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Check(pressure.Intensity == intensity && Mathf.IsEqualApprox(pressure.DamageMultiplier, multiplier),
                    $"pression {name} intensité={pressure.Intensity} multiplicateur={pressure.DamageMultiplier:F2}");
                SaveCrop("scaph-pression-" + name, _player.GlobalPosition + new Vector2(0, -18), new Vector2(100, 90));
                trace?.Sample(elapsed);
            }
            SetMemory(1f);
            await PressureFrame("ancree", 0, 1f);
            SetMemory(0.38f);
            await PressureFrame("effilochee", 1, 1.25f);
            SetMemory(0.12f);
            await PressureFrame("effacee", 2, 1.5f);
            SetMemory(0f);
            await PressureFrame("neant", 0, 1f);
            SetMemory(1f);
            await PressureFrame("retour", 0, 1f);
            Dictionary<string, float> distances = new();
            foreach ((string name, string enemyId, string ascension) in new[]
            {
                ("leger", "rodeur", ""), ("lourd", "void_brute", ""), ("boss", EnemyGrammar.BossPartId, ""),
                ("treuil", "rodeur", "salvage_winch"), ("abysse", "rodeur", "abyssal_spear"),
            })
            {
                while (_player.WeaponSlots.Count > 0) _player.RemoveWeapon(0);
                // Les changements de mémoire et le streaming peuvent modifier le terrain depuis le premier cadrage.
                origin = FindScaphTrialOrigin(origin);
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
            Check(pressure.Intensity == 0, "aucune Pression sur sol ancré pendant la galerie");
            GD.Print($"[RunObservation] RESULT scaph-kit checks={checks} pressure=5 harpoon=5 failures=0");
        }
        finally
        {
            if (trace != null) tree.ProcessFrame -= TraceFrame;
        }
    }

    /// <summary>Revalide le terrain réel et l'emprise des corps avant chaque salve, sans déplacer les obstacles.</summary>
    private Vector2 FindScaphTrialOrigin(Vector2 around)
    {
        for (int radius = 0; radius <= 12; radius++)
            for (int row = -radius; row <= radius; row++)
                for (int col = -radius; col <= radius; col++)
                {
                    if (Mathf.Max(Mathf.Abs(row), Mathf.Abs(col)) != radius) continue;
                    Vector2 candidate = around + new Vector2(col * 32, row * 32);
                    Vector2 from = candidate + new Vector2(280f, 0f);
                    Vector2 displacement = new(-300f, 0f);
                    // Une cellule sans sol n'est pas de l'eau ; le même garde que le vrai filin doit l'écarter.
                    if (_player.ClampHarpoonTravel(from, displacement).DistanceSquaredTo(displacement) > 0.01f) continue;
                    bool clear = true;
                    for (int x = -20; clear && x <= 280; x += 2)
                        for (int y = -24; clear && y <= 24; y += 8)
                        {
                            Vector2 point = candidate + new Vector2(x, y);
                            if (_world.IsWaterAt(point) || ObstacleField.Resolve(ref point, 24f)) clear = false;
                        }
                    if (clear) return candidate;
                }
        throw new InvalidOperationException("Aucun couloir réel, sec et libre de 300 px pour le harpon près du cadrage.");
    }
}
