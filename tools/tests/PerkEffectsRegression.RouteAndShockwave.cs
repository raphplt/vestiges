using System;
using System.Reflection;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Tests;

public partial class PerkEffectsRegression
{
    private void CheckRouteAndShockwave()
    {
        Setup();
        _player.InitializeCharacter(CharacterDataLoader.Get("vagabond"));
        PerkManager perks = new();
        AddChild(perks);
        perks.ApplyPassivePerks("vagabond");
        RouteEndurance reserve = _player.GetNode<RouteEndurance>("RouteEndurance");
        PerkRouteEndurance config = PerkDataLoader.GetAll().Find(p => p.CharacterId == "vagabond").RouteEndurance;
        _events.EmitSignal(EventBus.SignalName.ZoneDiscovered, 0, 0, config.CellsPerCharge * 3);
        Check(reserve.Charges == 0, "Endurance : aucune charge à la révélation initiale immobile");
        _player.Position += Vector2.Right * 32f;
        _events.EmitSignal(EventBus.SignalName.ZoneDiscovered, 1, 0, config.CellsPerCharge * 4);
        Check(reserve.Charges == 3, "Endurance : terrain inédit, réserve plafonnée à trois charges");
        float before = _player.CurrentHp;
        _player.TakeDamage(0f);
        _player.TakeDamage(20f);
        Check(reserve.Charges == 2 && Near(_player.CurrentHp, before - 10f), "Endurance : blessure divisée par deux, une charge consommée, zéro dégât ignoré");
        _player.Position -= Vector2.Right * 32f;
        _events.EmitSignal(EventBus.SignalName.ZoneDiscovered, 0, 0, 0);
        _player.TakeErasureDamage(3f);
        Check(reserve.Charges == 2 && Near(_player.CurrentHp, before - 13f), "Endurance : retour sans découverte et Néant ne consomment ni ne rechargent la réserve");
        _player.RemoveChild(reserve);
        reserve.QueueFree();
        Check(_player.RouteEndurance == null, "Endurance : retrait du module retire la protection");

        Setup();
        _player.InitializeCharacter(CharacterDataLoader.Get("forgeuse"));
        perks.ApplyPassivePerks("forgeuse");
        TemperedImpact forge = _player.GetNode<TemperedImpact>("TemperedImpact");
        Enemy target = SpawnEnemy();
        Enemy neighbour = SpawnEnemy();
        Enemy distant = SpawnEnemy();
        target.Position = new Vector2(500f, 500f);
        neighbour.Position = target.Position + Vector2.Right * 25f;
        distant.Position = target.Position + Vector2.Right * 200f;
        FieldInfo hp = typeof(Enemy).GetField("_currentHp", BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (Enemy enemy in new[] { target, neighbour, distant }) hp.SetValue(enemy, 1000f);
        CrowdIndex.MarkMoved();
        WeaponInstance weapon = _player.WeaponSlots[0];
        AttackContext first = _player.BeginAttack(weapon, 10f);
        target.TakeDamage(1f, source: first);
        neighbour.TakeDamage(1f, source: first);
        target.TakeDamage(1f, source: first with { IsEcho = true });
        target.TakeDamage(1f, source: first.As(DamageKind.DamageOverTime));
        Check(forge.SuccessfulAttacks == 1 && forge.WavesTriggered == 0, "Redresser : plusieurs cibles comptent une attaque ; répliques et DOT exclus");
        for (int i = 0; i < 3; i++) target.TakeDamage(1f, source: _player.BeginAttack(weapon, 10f));
        Check(forge.WavesTriggered == 1 && forge.SuccessfulAttacks == 0 && Near(neighbour.CurrentHp, 993f)
            && Near(distant.CurrentHp, 1000f), "Redresser : quatrième attaque, onde à 60 % sur les voisins, hors rayon intact et aucune récursion");
        Vector2 knockback = (Vector2)typeof(Enemy).GetField("_knockVelocity", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(neighbour);
        Check(knockback.X > 0f, "Redresser : l'onde repousse vers l'extérieur");
        perks.ApplyPassivePerks("forgeuse");
        Check(_player.GetNode<TemperedImpact>("TemperedImpact") == forge, "Redresser : branchement répété sans double effet");
        foreach (Enemy enemy in new[] { target, neighbour, distant })
        {
            RemoveChild(enemy);
            enemy.QueueFree();
        }
        RemoveChild(perks);
        perks.QueueFree();
        try { new PerkRouteEndurance(0, 3, 0.5f).Validate(); Check(false, "Endurance : réglage nul refusé"); }
        catch (InvalidOperationException) { Check(true, "Endurance : réglage nul refusé"); }
        try { new PerkShockwave(4, float.NaN, 0.6f, 25f).Validate(); Check(false, "Redresser : rayon non fini refusé"); }
        catch (InvalidOperationException) { Check(true, "Redresser : rayon non fini refusé"); }
    }
}
