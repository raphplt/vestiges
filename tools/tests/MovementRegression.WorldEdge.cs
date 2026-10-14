using System;
using System.Threading.Tasks;
using Godot;
using Vestiges.World;

namespace Vestiges.Tests;

public partial class MovementRegression
{
    /// <summary>
    /// Mur du bord du monde sur des bords de tuiles synthétiques : une poussée oblique glisse le long du bord vertical
    /// en quinconce, du bord horizontal et d'un bord à 45° qui ne suit aucun axe de la grille, au lieu de s'y coincer.
    /// </summary>
    private async Task RunWorldEdgeChecks()
    {
        TileMapLayer ground = new() { TileSet = GD.Load<TileSet>("res://assets/tiles/iso_tileset.tres") };
        AddChild(ground);
        Rect2I cells = new(-20, -60, 41, 121);
        await CheckEdgeSlide(ground, cells, "vertical", c => c.X < 40f, new Vector2(1f, -1f), Vector2.Up, 0.9f);
        await CheckEdgeSlide(ground, cells, "horizontal", c => c.Y > -40f, new Vector2(1f, -1f), Vector2.Right, 0.9f);
        // Bord à 45° : en losanges, un escalier de pentes et de contremarches verticales. Une poussée à 30° du bord y
        // glisse, freinée par les contremarches ; une poussée perpendiculaire à une contremarche s'y arrête.
        await CheckEdgeSlide(ground, cells, "en biais", c => c.X + c.Y < 120f, new Vector2(Mathf.Cos(0.26f), -Mathf.Sin(0.26f)),
            new Vector2(1f, -1f), 0.5f);
        ground.QueueFree();
        await Step(1);
    }

    private async Task CheckEdgeSlide(TileMapLayer ground, Rect2I cells, string label, Func<Vector2, bool> isGround,
        Vector2 push, Vector2 along, float minRatio)
    {
        const int frames = 120;
        StaticBody2D wall = WorldEdgeCollision.Build(this, ground, cells,
            (x, y) => isGround(ground.MapToLocal(new Vector2I(x, y))));
        _player.Position = Vector2.Zero;
        await Step(1);
        SetActions(push.Normalized());
        await Step(frames);
        SetActions(Vector2.Zero);
        Vector2 tangent = along.Normalized();
        float slid = _player.Position.Dot(tangent);
        float free = push.Normalized().Dot(tangent) * _player.Speed * frames / Engine.PhysicsTicksPerSecond;
        Check(slid > free * minRatio && isGround(_player.Position),
            $"bord du monde {label} : la poussée oblique glisse, {slid:0} px sur {free:0} sans mur, position={_player.Position}");
        wall.QueueFree();
        await Step(1);
    }
}
