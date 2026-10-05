using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Tests;

/// <summary>Vrais impacts du Transistor : compteurs et allocations, aucune conclusion FPS.</summary>
public partial class ConeRegression : Node2D
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static FieldInfo Field(object obj, string name) => obj.GetType().GetField(name, Private)
        ?? throw new MissingFieldException(obj.GetType().FullName, name);
    private static object Read(object obj, string field) => Field(obj, field).GetValue(obj);
    private static void Set(object obj, string field, object value) => Field(obj, field).SetValue(obj, value);
    private static T Method<T>(object obj, string method) where T : Delegate =>
        (obj.GetType().GetMethod(method, Private) ?? throw new MissingMethodException(obj.GetType().FullName, method)).CreateDelegate<T>(obj);
    private readonly List<object> _results = new();
    private readonly Enemy[] _enemies = new Enemy[100];
    private Player _player;
    private WeaponInstance _weapon;
    private Action<WeaponSpecialEffect> _activate;
    private Action<float> _cone;
    private int _hits;
    private double _damage;
    private int _failures;
    private bool _baseline;
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    private void Check(bool pass, string label)
    {
        if (!pass) _failures++;
        _results.Add(new { check = label, pass });
        GD.Print($"[ConeRegression] {(pass ? "PASS" : "FAIL")} {label}");
    }
    public override async void _Ready()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            _baseline = Array.IndexOf(args, "--baseline") >= 0;
            GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Run);
            _player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
            AddChild(_player);
            _player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
            _player.IsGodMode = true;
            _player.IsAIControlled = true;
            _player.SetPhysicsProcess(false);
            _player.SetProcess(false);
            foreach (Node child in _player.GetChildren()) if (child is Timer timer) timer.Stop();
            AddChild(new CombatPools());
            for (int i = 0; i < _enemies.Length; i++)
            {
                Enemy enemy = GD.Load<PackedScene>("res://scenes/enemies/Enemy.tscn").Instantiate<Enemy>();
                AddChild(enemy);
                enemy.Initialize(EnemyDataLoader.Get("shade"), 100000f, 1f);
                enemy.SetPhysicsProcess(false); enemy.SetProcess(false);
                _enemies[i] = enemy;
            }
            _weapon = new(WeaponDataLoader.Get("last_broadcast"));
            Set(_player, "_equippedWeapon", _weapon);
            Set(_player, "_facingDirection", Vector2.Right);
            _activate = Method<Action<WeaponSpecialEffect>>(_player, "ActivateSustainedCone");
            _cone = Method<Action<float>>(_player, "ProcessSustainedCone");
            GetNode<EventBus>("/root/EventBus").EntityDamaged += Damaged;
            await Frame();
            foreach (int count in new[] { 0, 1, 10, 50, 100, 100, 50, 10, 1, 0 }) await Measure(count);
            await Contracts();
            string output = args[Array.IndexOf(args, "--output") + 1];
            System.IO.File.WriteAllText(output, JsonSerializer.Serialize(new { seed = 221092026, failures = _failures, results = _results }, new JsonSerializerOptions { WriteIndented = true }));
            GD.Print($"[ConeRegression] RESULT failures={_failures}");
            await GameExit.QuitAsync(GetTree(), _failures == 0 ? 0 : 1);
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(2); }
    }
    private void Damaged(Node target, float amount) { if (target is Enemy) { _hits++; _damage += amount; } }
    private void Place(int count)
    {
        for (int i = 0; i < _enemies.Length; i++)
            _enemies[i].Position = i < count ? new Vector2(80 + i % 20, (i % 3 - 1) * 2) : new Vector2(-900, 0);
    }
    private async Task Measure(int count)
    {
        Place(count);
        GD.Seed(221092026);
        Vestiges.Core.RunRandom.Begin(221092026);
        double[] intervals = new double[240];
        long bytes = 0;
        int feedback = 0;
        float attribution = 0;
        HitFeedback[] visuals = new HitFeedback[count];
        for (int i = 0; i < count; i++) visuals[i] = (HitFeedback)Read(_enemies[i], "_hitFeedback");
        for (int cycle = 0; cycle < 3; cycle++)
        {
            _activate(_weapon.Base.SpecialEffect);
            if (cycle == 1) { _hits = 0; _damage = 0; attribution = _player.GetDamageDealt(_weapon.Id); }
            for (int tick = 0; tick < 120; tick++)
            {
                await Frame();
                foreach (HitFeedback visual in visuals) visual.Tick(1f / 60);
                long before = GC.GetAllocatedBytesForCurrentThread();
                long time = Stopwatch.GetTimestamp();
                _cone(1f / 60);
                double elapsed = Stopwatch.GetElapsedTime(time).TotalMilliseconds;
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                if (cycle == 0) continue;
                bytes += allocated;
                intervals[(cycle - 1) * 120 + tick] = elapsed;
                // Hors de la mesure : Trigger remet elapsed à zéro, Tick ci-dessus l'a fait avancer.
                foreach (HitFeedback visual in visuals) if ((float)Read(visual, "_elapsed") == 0) feedback++;
            }
        }
        float attributed = _player.GetDamageDealt(_weapon.Id) - attribution;
        _results.Add(new { scenario = "cone", targets = count, ticks = 240, hits = _hits, damage = _damage, attributed, feedback, allocated_bytes = bytes, intervals_ms = intervals });
        Check(_hits == count * 240, $"{count} cibles : cadence des dégâts conservée");
        Check(Math.Abs(attributed - _damage) <= Math.Max(0.1, _damage * 0.001), $"{count} cibles : attribution Transistor conservée");
        if (!_baseline) Check(feedback <= count * 42, $"{count} cibles : feedback borné à environ 10 Hz");
    }
    private async Task Contracts()
    {
        // Isoler le contrat des impacts de leur présentation ; les objets ont leur propre RNG.
        CombatFxSettings.ParticleLevel = ParticleLevel.Off;
        CombatFxSettings.PlayerAttackFx = false;
        Place(1);
        Enemy enemy = _enemies[0];
        GD.Seed(221092026);
        Vestiges.Core.RunRandom.Begin(221092026);
        _player.AddOrUpgradePassive("allumette_humide", 30);
        _player.ObjectTriggers.Rng.Seed = 11;
        _player.ApplyPerkModifier("lifesteal", 0.1f, "additive");
        Set(_player, "_lifestealPending", 0f);
        Set(_player, "_lifestealTimer", 0f);
        Set(_player, "_currentHp", 50f);
        _activate(_weapon.Base.SpecialEffect);
        _hits = 0; _damage = 0;
        int burns = 0;
        for (int tick = 0; tick < 120; tick++)
        {
            await Frame();
            Set(enemy, "_igniteTimer", 0f);
            _cone(1f / 60);
            if (enemy.IsBurning) burns++;
        }
        Check(_hits == 120 && burns > 0 && burns <= 25, $"Allumette : {burns} déclenchements aux impacts visibles, 120 ticks de dégâts");
        Check(_player.CurrentHp == 50f, "vol de vie : dégâts accumulés sans soin par tick de cône");
        DefenseConfig defense = DefenseConfig.Load();
        float expectedHeal = Math.Min((float)_damage * _player.Lifesteal,
            _player.EffectiveMaxHp * defense.LifestealMaxHpPerSecond * defense.LifestealTickSeconds);
        Action<float> lifesteal = Method<Action<float>>(_player, "StepLifesteal");
        lifesteal(defense.LifestealTickSeconds);
        Check(Math.Abs(_player.CurrentHp - 50f - expectedHeal) < 0.001f, "vol de vie : un soin cadencé et plafonné sur les dégâts du cône");
        float afterHeal = _player.CurrentHp;
        lifesteal(defense.LifestealTickSeconds);
        Check(_player.CurrentHp == afterHeal, "vol de vie : l'excédent n'est pas rendu au prochain intervalle");
        _results.Add(new { scenario = "procs", hits = _hits, damage = _damage, burns, hp = _player.CurrentHp });
        _player.ObjectTriggers.Add(ObjectTriggers.BurnChanceStat, -_player.ObjectTriggers.BurnChance);
        _player.ApplyPerkModifier("lifesteal", -0.1f, "additive");
        // Les procs déterministes restent par impact, même quand le feedback est refusé.
        WeaponSpecialEffect originalSpecial = _weapon.Base.SpecialEffect;
        WeaponOnHitEffect originalOnHit = _weapon.Base.OnHitEffect;
        _activate(originalSpecial);
        _weapon.Base.SpecialEffect = new WeaponSpecialEffect
        {
            Kind = SpecialEffectKind.HealEveryNHits,
            Params = new System.Collections.Generic.Dictionary<string, float> { [SpecialEffectParam.HitsPerHeal] = 5f, [SpecialEffectParam.HealAmount] = 1f },
        };
        _weapon.Base.OnHitEffect = new WeaponOnHitEffect { Kind = OnHitEffectKind.Slow, Value = 0.5f, Duration = 2f };
        Set(_player, "_currentHp", 50f);
        WeaponInstance other = new(WeaponDataLoader.Get("makeshift_bow"));
        Set(_player, "_equippedWeapon", other);
        float sourceBefore = _player.GetDamageDealt(_weapon.Id);
        float otherBefore = _player.GetDamageDealt(other.Id);
        bool refreshed = true;
        for (int tick = 0; tick < 15; tick++)
        {
            Set(enemy, "_slowTimer", 0f);
            _cone(1f / 60);
            refreshed &= (float)Read(enemy, "_slowTimer") == 2f;
        }
        Check(_player.CurrentHp == 53f && refreshed, "soin tous les cinq coups et slow à chaque impact");
        Check(_player.GetDamageDealt(_weapon.Id) > sourceBefore && _player.GetDamageDealt(other.Id) == otherBefore,
            "changer l’arme courante conserve la source du cône");
        _weapon.Base.SpecialEffect = originalSpecial;
        _weapon.Base.OnHitEffect = originalOnHit;
        Set(_player, "_equippedWeapon", _weapon);
        // Émission imbriquée : les arguments du signal extérieur restent intacts (aucun tampon partagé).
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        List<float> observed = new();
        void Nested(Node target, float amount) { if (target == enemy) _enemies[1].TakeDamage(7f); }
        void Observer(Node target, float amount) { if (target == enemy || target == _enemies[1]) observed.Add(amount); }
        bus.EntityDamaged += Nested;
        bus.EntityDamaged += Observer;
        enemy.TakeDamage(3f);
        bus.EntityDamaged -= Nested;
        bus.EntityDamaged -= Observer;
        Check(observed.Count == 2 && observed[0] == 7f && observed[1] == 3f, "signaux synchrones réentrants : arguments conservés");

        _activate(_weapon.Base.SpecialEffect);
        enemy.Position = new Vector2(-900, 0); _hits = 0;
        _cone(1f / 60);
        Check(_hits == 0, "sortie du cône sans impact résiduel");
        enemy.Position = new Vector2(80, 0);
        Set(enemy, "_currentHp", 0.001f);
        _cone(1f / 60); int fatalHits = _hits;
        _cone(1f / 60);
        Check(enemy.IsDying && _hits == fatalHits, "aucun impact après mort");
        enemy.Reset();
        _cone(1f / 60);
        Check(_hits == fatalHits, "aucun dégât sur un ennemi rendu au pool");
        enemy.Initialize(EnemyDataLoader.Get("shade"), 100000f, 1f);
        enemy.SetPhysicsProcess(false); enemy.SetProcess(false);
        enemy.Position = new Vector2(80, 0);
        await Frame();
        _cone(1f / 60);
        Check(_hits == fatalHits + 1 && (float)Read(Read(enemy, "_hitFeedback"), "_elapsed") == 0, "premier impact visible après recyclage");
    }
}
