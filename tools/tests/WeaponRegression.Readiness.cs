using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

public partial class WeaponRegression
{
    /// <summary>
    /// Arme prête (DECISIONS §176) : une recharge finie sans cible à portée garde son coup. La Rallonge frappe dès
    /// qu'une créature entre à portée, au lieu d'attendre une recharge entière, et sa recharge repart de ce coup.
    /// </summary>
    private async Task CheckReadyWeapon()
    {
        Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(player);
        player.InitializeCharacter(CharacterDataLoader.Get("vagabond"));
        player.ProcessMode = ProcessModeEnum.Disabled;
        player.GlobalPosition = new Vector2(-90000f, 90000f);
        Enemy enemy = null;
        try
        {
            while (player.WeaponSlots.Count > 0)
                player.RemoveWeapon(0);
            player.AddWeapon(WeaponDataLoader.Get("whip"));
            List<Timer> timers = (List<Timer>)typeof(Player).GetField("_weaponTimers", Private).GetValue(player);
            FieldInfo ready = typeof(Player).GetField("_readyWeaponSlots", Private);
            MethodInfo attack = typeof(Player).GetMethod("OnWeaponAttackTimeout", Private);
            MethodInfo tick = typeof(Player).GetMethod("TickReadyWeapons", Private);
            CrowdIndex.MarkMoved();
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            attack.Invoke(player, new object[] { 0 });
            bool held = (int)ready.GetValue(player) == 1 && timers[0].IsStopped();
            tick.Invoke(player, null);
            bool waits = (int)ready.GetValue(player) == 1 && timers[0].IsStopped();

            float reach = player.GetWeaponStatForDisplay(player.WeaponSlots[0], "range");
            enemy = EnemyScene.Instantiate<Enemy>();
            AddChild(enemy);
            enemy.Initialize(EnemyDataLoader.Get("rodeur"), 100000f, 1f);
            enemy.SetTicking(false);
            enemy.GlobalPosition = player.GlobalPosition + new Vector2(reach * 0.5f, 0f);
            CrowdIndex.MarkMoved();
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            tick.Invoke(player, null);
            Check(held && waits && enemy.HpRatio < 1f && (int)ready.GetValue(player) == 0 && !timers[0].IsStopped()
                  && Mathf.IsEqualApprox((float)timers[0].TimeLeft, (float)timers[0].WaitTime),
                $"Rallonge prête : recharge finie à vide, elle attend ; une créature à portée est frappée aussitôt et la recharge repart (PV {enemy.HpRatio:0.###})");

            ready.SetValue(player, 0b1010);
            typeof(Player).GetMethod("RemoveReadySlot", Private).Invoke(player, new object[] { 1 });
            Check((int)ready.GetValue(player) == 0b100, "Arme retirée : les armes prêtes suivantes descendent d'un emplacement");
        }
        finally
        {
            enemy?.QueueFree();
            player.QueueFree();
        }
    }
}
