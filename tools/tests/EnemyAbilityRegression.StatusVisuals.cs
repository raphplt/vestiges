using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;
using Vestiges.Spawn;

namespace Vestiges.Tests;

/// <summary>
/// Plan 27 V1a : chaque statut allume son canal sur le sprite puis l'éteint à son terme, la teinte suit la priorité,
/// le matériau n'est pas réécrit tant que l'état ne change pas, et une créature rendue au pool repart neutre.
/// </summary>
public partial class EnemyAbilityRegression
{
    private const float ShortStatus = 0.2f;
    private const int ExpireTicks = 20;

    private async Task RunStatusVisualChecks()
    {
        Check(StatusVisualConfig.TryLoad(out StatusVisualConfig config, out string error), $"Statuts visibles : réglages lus ({error ?? "ok"})");
        string valid = FileAccess.GetFileAsString("res://data/fx/status_visuals.json");
        Check(!StatusVisualConfig.TryParse(valid.Replace("\"glass\"", "\"verre\""), out _, out error)
              && error.Contains("« verre » inconnue"), $"Statuts visibles : famille inconnue refusée ({error})");
        Check(!StatusVisualConfig.TryParse(valid.Replace("\"crack_spacing_px\": 7", "\"crack_spacing_px\": 2"), out _, out error)
              && error.Contains("crack_spacing_px"), $"Statuts visibles : fêlures trop serrées refusées ({error})");

        Enemy enemy = await SpawnReady("rodeur", new Vector2(120f, 0f));
        AnimatedSprite2D sprite = enemy.GetNode<AnimatedSprite2D>("Sprite");
        if (!sprite.Visible || sprite.Material is not ShaderMaterial material)
        {
            Check(false, "Statuts visibles : le Rôdeur a un sprite et son matériau");
            Despawn(enemy);
            return;
        }
        float Tint() => TintAlpha(material);
        int Depth(string name) => material.GetShaderParameter(name).AsInt32();
        await Step(1);
        Check(Tint() == 0f && Depth("frost_depth") == 0 && Depth("crack_spacing") == 0 && Depth("heat_depth") == 0
              && sprite.SpeedScale == 1f, $"Statuts visibles : créature saine sans marque (teinte {Tint()}, givre {Depth("frost_depth")}, fêlures {Depth("crack_spacing")}, chaleur {Depth("heat_depth")}, cadence {sprite.SpeedScale})");

        enemy.ApplyIgnite(1f, ShortStatus);
        await Step(1);
        Check(Depth("heat_depth") == config.HeatDepthPx && Tint() == config.BurnTint.A, "Brûlure : bord chaud et teinte allumés");
        await Step(ExpireTicks);
        Check(Depth("heat_depth") == 0 && Tint() == 0f, "Brûlure : canal éteint à son terme");

        enemy.ApplyFragile(0.2f, ShortStatus);
        await Step(1);
        Check(Depth("crack_spacing") == config.CrackSpacingPx, "Fragile : fêlures allumées");
        await Step(ExpireTicks);
        Check(Depth("crack_spacing") == 0, "Fragile : fêlures éteintes à son terme");

        enemy.ApplySlow(0.4f, ShortStatus);
        await Step(1);
        Check(Tint() == config.SlowTint.A && Mathf.IsEqualApprox(sprite.SpeedScale, 0.4f),
            $"Ralenti : teinte froide, animation au facteur de marche ({sprite.SpeedScale:F2})");
        await Step(ExpireTicks);
        Check(Tint() == 0f && sprite.SpeedScale == 1f, "Ralenti : teinte éteinte, animation à cadence normale");

        await Step(1);
        StringName pose = sprite.Animation;
        enemy.Freeze(ShortStatus);
        enemy.ApplyIgnite(1f, ShortStatus);
        await Step(1);
        int frame = sprite.Frame;
        await Step(6);
        Check(Depth("frost_depth") == config.FrostDepthPx && Tint() == config.FrozenTint.A && Depth("heat_depth") > 0,
            "Figé et brûlant : givre, teinte du figé prioritaire, bord chaud cumulé");
        Check(sprite.SpeedScale == 0f && sprite.Animation == pose && sprite.Frame == frame,
            $"Figé : animation arrêtée sur sa pose ({pose} {frame}, maintenant {sprite.Animation} {sprite.Frame})");

        // Écrit au changement d'état seulement : une valeur posée à la main survit aux ticks sans changement.
        Color sentinel = new(0.1f, 0.2f, 0.3f, 0.4f);
        material.SetShaderParameter("status_tint", sentinel);
        await Step(3);
        Check(material.GetShaderParameter("status_tint").AsColor() == sentinel, "Statuts visibles : matériau non réécrit sans changement d'état");
        await Step(ExpireTicks);
        Check(Depth("frost_depth") == 0 && Depth("heat_depth") == 0 && Tint() == 0f && sprite.SpeedScale == 1f,
            "Figé : givre éteint, animation repartie");

        // Marques autour du sprite (V1b) : information de jeu, gardée avec les effets d'attaque coupés.
        EnemyStatusMarks marks = enemy.GetNode<EnemyStatusMarks>("StatusMarks");
        bool attackFx = CombatFxSettings.PlayerAttackFx;
        CombatFxSettings.PlayerAttackFx = false;
        enemy.ApplyDisorient(ShortStatus);
        enemy.ApplyBleed(1f, ShortStatus);
        await Step(1);
        Check(marks.Visible, "Désorienté et saignant : marques affichées, effets d'attaque coupés");
        CombatFxSettings.PlayerAttackFx = attackFx;
        await Step(ExpireTicks);
        Check(!marks.Visible, "Désorienté et saignant : marques éteintes à leur terme");
        enemy.ApplySlow(0.4f, ShortStatus);
        await Step(2);
        Check(!marks.Visible, "Ralenti : aucune marque autour du sprite (teinte et pas suffisent)");
        await Step(ExpireTicks);

        enemy.Freeze(10f);
        enemy.ApplyDisorient(10f);
        await Step(2);
        enemy.TakeDamage(1000000f);
        Check(!marks.Visible, "Mort : marques éteintes");
        int deathFrame = sprite.Frame;
        await Step(12);
        Check(sprite.SpeedScale > 0f && sprite.Frame != deathFrame,
            $"Tuée figée : l'animation de mort avance (cadence {sprite.SpeedScale}, image {deathFrame} → {sprite.Frame})");
        Despawn(enemy);

        await RunStatusVisualPoolCheck();
    }

    /// <summary>Force de la teinte ; un paramètre jamais écrit vaut la valeur du shader (transparente), pas un nil converti.</summary>
    private static float TintAlpha(ShaderMaterial material)
    {
        Variant tint = material.GetShaderParameter("status_tint");
        return tint.VariantType == Variant.Type.Nil ? 0f : tint.AsColor().A;
    }

    private async Task RunStatusVisualPoolCheck()
    {
        EnemyPool pool = new() { Name = "EnemyPool", InitialSize = 0 };
        AddChild(pool);
        Enemy dirty = pool.Get();
        AddChild(dirty);
        dirty.Initialize(EnemyDataLoader.Get("rodeur"), 1000f, 1f);
        dirty.SetPhysicsProcess(false);
        dirty.Position = _player.Position + new Vector2(140f, 0f);
        dirty.Freeze(10f);
        dirty.ApplyFragile(0.2f, 10f);
        dirty.ApplyIgnite(1f, 10f);
        dirty.ApplyDisorient(10f);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        dirty._PhysicsProcess(Dt);
        pool.Return(dirty);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(dirty.GetNode<AnimatedSprite2D>("Sprite").SpeedScale == 1f, "Pool : cadence remise à 1 dès le retour, avant tout tick");

        Enemy reused = pool.Get();
        AddChild(reused);
        reused.Initialize(EnemyDataLoader.Get("rodeur"), 1000f, 1f);
        reused.SetPhysicsProcess(false);
        reused.Position = dirty.Position;
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        reused._PhysicsProcess(Dt);
        AnimatedSprite2D sprite = reused.GetNode<AnimatedSprite2D>("Sprite");
        bool neutral = sprite.Material is ShaderMaterial material
            && TintAlpha(material) == 0f
            && material.GetShaderParameter("frost_depth").AsInt32() == 0
            && material.GetShaderParameter("crack_spacing").AsInt32() == 0
            && material.GetShaderParameter("heat_depth").AsInt32() == 0;
        Check(reused == dirty && neutral && sprite.SpeedScale == 1f,
            $"Pool : créature figée, Fragile et brûlante rendue neutre (même créature {reused == dirty}, matériau neutre {neutral}, cadence {sprite.SpeedScale})");
        Check(!reused.GetNode<EnemyStatusMarks>("StatusMarks").Visible, "Pool : marques éteintes à la réutilisation");
        reused.QueueFree();
        pool.QueueFree();
    }
}
