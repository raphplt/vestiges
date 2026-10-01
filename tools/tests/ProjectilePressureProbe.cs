using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;

namespace Vestiges.Tests;

/// <summary>Sonde réservée au banc : observe les pools sans instrumenter les runs normales.</summary>
internal sealed class ProjectilePressureProbe : IDisposable
{
    private static readonly FieldInfo Age = RequiredField("_age");
    private static readonly FieldInfo Source = RequiredField("_sourceEnemyId");
    private static readonly FieldInfo Despawning = RequiredField("_isDespawning");
    private readonly CombatPools _pools;
    private readonly GameManager _manager;
    private readonly float? _lifetime;
    private readonly List<(EnemyProjectile Projectile, float OriginalLifetime)> _projectiles = new();
    private readonly List<string> _rows = new()
    {
        "t,phase,active,visible,visible_age_over_2,spitter,sentinel,weaver,howler,other,visible_shooters"
    };
    private double _nextSample;

    internal ProjectilePressureProbe(float? lifetime)
    {
        if (lifetime.HasValue && (!float.IsFinite(lifetime.Value) || lifetime.Value <= 0))
            throw new ArgumentOutOfRangeException(nameof(lifetime));
        _lifetime = lifetime;
        _pools = CombatPools.Instance ?? throw new InvalidOperationException("CombatPools absent.");
        _manager = _pools.GetNode<GameManager>("/root/GameManager");
        foreach (Node child in _pools.GetChildren())
            Register(child);
        _pools.ChildEnteredTree += Register;
        GD.Print($"[ProjectilePressure] lifetime={lifetime?.ToString(CultureInfo.InvariantCulture) ?? "scene"}");
    }

    private static FieldInfo RequiredField(string name) => typeof(EnemyProjectile)
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(EnemyProjectile).FullName, name);

    private void Register(Node child)
    {
        if (child is not EnemyProjectile projectile)
            return;
        _projectiles.Add((projectile, projectile.MaxLifetime));
        if (_lifetime.HasValue)
            projectile.MaxLifetime = _lifetime.Value;
    }

    internal void Sample(double time, Rect2 view)
    {
        if (time < _nextSample)
            return;
        _nextSample += 0.1;
        int active = 0, visible = 0, old = 0, spitter = 0, sentinel = 0, weaver = 0, howler = 0, other = 0;
        foreach ((EnemyProjectile projectile, _) in _projectiles)
        {
            if (!projectile.Visible || projectile.ProcessMode == Node.ProcessModeEnum.Disabled || (bool)Despawning.GetValue(projectile))
                continue;
            active++;
            if (!view.HasPoint(projectile.GlobalPosition))
                continue;
            visible++;
            if ((float)Age.GetValue(projectile) > 2f)
                old++;
            switch ((string)Source.GetValue(projectile))
            {
                case "fading_spitter": spitter++; break;
                case "wailing_sentinel": sentinel++; break;
                case "tisseuse": weaver++; break;
                case "hurleur": howler++; break;
                default: other++; break;
            }
        }
        int shooters = 0;
        foreach (Node node in _pools.GetTree().GetNodesInGroup("enemies"))
        {
            if (node is Enemy { IsActive: true, IsDying: false } enemy && view.HasPoint(enemy.GlobalPosition)
                && enemy.EnemyId is "fading_spitter" or "wailing_sentinel" or "tisseuse" or "hurleur")
                shooters++;
        }
        string phase = _manager.CurrentRunPhase.ToString();
        _rows.Add(string.Create(CultureInfo.InvariantCulture,
            $"{time:F3},{phase},{active},{visible},{old},{spitter},{sentinel},{weaver},{howler},{other},{shooters}"));
    }

    internal void Save(string path)
    {
        using FileAccess csv = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        csv.StoreString(string.Join('\n', _rows) + "\n");
        GD.Print($"[ProjectilePressure] samples={_rows.Count - 1} pooled={_projectiles.Count} csv={path}");
    }

    public void Dispose()
    {
        _pools.ChildEnteredTree -= Register;
        foreach ((EnemyProjectile projectile, float originalLifetime) in _projectiles)
            projectile.MaxLifetime = originalLifetime;
    }
}
