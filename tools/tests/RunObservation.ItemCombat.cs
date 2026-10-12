using System;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Spawn;

namespace Vestiges.Tests;

/// <summary>--capture-item-combat : dash réel, Cassette, Tampon et créature détournée par le Portrait.</summary>
public partial class RunObservation
{
    private async Task CaptureItemCombat()
    {
        await PrepareCloseUpScene();
        await MoveToOpenGround(new Vector2(230f, 140f));
        foreach (string id in new[] { "portrait_efface", "cassette_rembobinee", "tampon_annule" })
            for (int copy = 0; copy < (id == "tampon_annule" ? 2 : 3); copy++)
                if (!_player.AddItemCopy(id)) throw new InvalidOperationException("Objet de capture refusé : " + id);
        Vector2 origin = _player.GlobalPosition;
        _player.IsGodMode = false;
        float hp = _player.CurrentHp;
        _player.TakeDamage(20f, origin - new Vector2(20f, 0f));
        await Frames(30);
        SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
        Enemy enemy = spawner.SpawnEventEnemy("rodeur", origin + new Vector2(90f, -35f));
        enemy.SetTicking(false);
        EnemyProjectile Shot(Vector2 offset)
        {
            EnemyProjectile shot = CombatPools.Instance.TakeEnemyProjectile();
            shot.Speed = 0f;
            shot.Launch(origin + offset, Vector2.Left, 1f, "fading_spitter", "spit", FxFamily.Hostile);
            return shot;
        }
        EnemyProjectile first = Shot(new Vector2(25f, 0f)), second = Shot(new Vector2(55f, 0f));
        EnemyProjectile outside = Shot(new Vector2(85f, -65f));
        await Frames(2);
        SaveScreen("item-combat-0-before");
        _player.AIInputOverride = Vector2.Right;
        _player.Mobility.Request();
        await Frames(20);
        _player.AIInputOverride = Vector2.Zero;
        await Frames(2);
        if (!_player.ObjectCombatEffects.DecoyActive || first.Visible || second.Visible || !outside.Visible)
            throw new InvalidOperationException("Dash de capture : leurre ou annulation non appliqués.");
        Sprite2D portrait = _player.GetNode<Sprite2D>("RememberedPortrait");
        GD.Print($"[RunObservation] portrait visible={portrait.IsVisibleInTree()} texture={portrait.Texture?.GetSize()} used={portrait.Texture?.GetImage().GetUsedRect()} scale={portrait.GlobalScale} world={portrait.GlobalPosition} screen={portrait.GetGlobalTransformWithCanvas().Origin} player={_player.GlobalPosition}");
        float healed = _player.CurrentHp - (hp - 20f);
        if (Mathf.Abs(healed - 15f) > .1f)
            throw new InvalidOperationException($"Cassette capture : soin attendu 15, obtenu {healed}");
        _player.IsGodMode = true;
        enemy.SetTicking(true);
        await Frames(3);
        bool diverted = enemy.Velocity.X < 0f;
        SaveScreen("item-combat-1-dash");
        await Frames(35);
        SaveScreen("item-combat-2-decoy");
        await Frames(60);
        SaveScreen("item-combat-3-decoy");
        if (!diverted) throw new InvalidOperationException("Créature non détournée vers le Portrait.");
        GD.Print($"[RunObservation] RESULT item_combat healed={healed:F2} cancelled=2 outside_preserved=True diverted={diverted} output={_output}");
    }
}
