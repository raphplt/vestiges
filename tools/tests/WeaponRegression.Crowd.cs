using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>Créatures sans corps physique (plan 29 B) : contour des décors et touches des projectiles.</summary>
public partial class WeaponRegression
{
    /// <summary>Un décor carré loin de tout, un disque de rayon 14 qui l'aborde, le traverse ou s'y trouve.</summary>
    private async Task CheckObstacleField()
    {
        Vector2 origin = new(90000f, 90000f);
        Node2D square = new() { Position = origin };
        Node2D flat = new() { Position = origin + new Vector2(200f, 0f) };
        AddChild(square);
        AddChild(flat);
        ObstacleField.Add(square, new[] { new Vector2(-20f, -20f), new Vector2(20f, -20f), new Vector2(20f, 20f), new Vector2(-20f, 20f) });
        ObstacleField.Add(flat, new[] { new Vector2(-20f, 0f), new Vector2(0f, 0f), new Vector2(20f, 0f) });
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        Vector2 free = origin + new Vector2(-60f, 0f);
        Check(!ObstacleField.Resolve(ref free, 14f) && free == origin + new Vector2(-60f, 0f), "Obstacles : un disque loin du décor ne bouge pas");
        Vector2 touching = origin + new Vector2(-30f, 5f);
        ObstacleField.Resolve(ref touching, 14f);
        Check(Mathf.IsEqualApprox(touching.X, origin.X - 34f) && Mathf.IsEqualApprox(touching.Y, origin.Y + 5f),
            $"Obstacles : un disque qui mord l'arête en sort sans glisser de côté ({touching - origin})");
        Vector2 inside = origin + new Vector2(15f, 2f);
        ObstacleField.Resolve(ref inside, 14f);
        Check(Mathf.IsEqualApprox(inside.X, origin.X + 34f), $"Obstacles : un centre dans le décor sort par l'arête la plus proche ({inside - origin})");
        // Une créature qui avance en biais contre la face ouest garde sa composante le long du mur.
        Vector2 sliding = origin + new Vector2(-34f, 0f) + new Vector2(6f, 6f);
        ObstacleField.Resolve(ref sliding, 14f);
        Check(Mathf.IsEqualApprox(sliding.X, origin.X - 34f) && Mathf.IsEqualApprox(sliding.Y, origin.Y + 6f),
            $"Obstacles : un pas en biais contre le mur glisse le long ({sliding - origin})");
        Vector2 onFlat = flat.Position + new Vector2(0f, 5f);
        Check(!ObstacleField.Resolve(ref onFlat, 14f), "Obstacles : une emprise sans surface est ignorée");
        square.QueueFree();
        flat.QueueFree();
    }

    /// <summary>Un tir à 60 px par tick ne traverse pas une créature placée entre deux de ses positions.</summary>
    private async Task CheckFastShotHits()
    {
        Vector2 origin = new(80000f, 80000f);
        Enemy target = EnemyScene.Instantiate<Enemy>();
        AddChild(target);
        target.Initialize(EnemyDataLoader.Get("rodeur"), 1000f, 1f);
        target.SetTicking(false);
        target.GlobalPosition = origin + new Vector2(30f, 0f);
        CrowdIndex.MarkMoved();
        float hp = target.HpRatio;
        Projectile shot = GD.Load<PackedScene>("res://scenes/combat/Projectile.tscn").Instantiate<Projectile>();
        AddChild(shot);
        WeaponInstance bow = _player.WeaponSlots[0];
        shot.Launch(origin, Vector2.Right, 10f, 3600f, 1f, 0, false, _player, bow.Base, bow, _player.BeginAttack(bow, 10f));
        for (int frame = 0; frame < 3; frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        Check(target.HpRatio < hp, $"Tir rapide : la créature sur le trajet est touchée (PV {hp:0.000} → {target.HpRatio:0.000})");
        target.QueueFree();
        if (IsInstanceValid(shot))
            shot.QueueFree();
    }
}
