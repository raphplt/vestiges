using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Combat.Abilities;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>Résultats des nouveaux objets : dégâts, soins, cibles, projectiles et retrait exact.</summary>
public partial class ItemCombatRegression : Node2D
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private Player _player;
    private EventBus _bus;
    private int _checks, _failures;
    private ulong _launch;
    private readonly List<Enemy> _enemies = new();
    public override async void _Ready()
    {
        try
        {
            GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Run);
            _bus = GetNode<EventBus>("/root/EventBus");
            CheckWeaponModifiers();
            CheckOrbitalQuantity();
            await CheckProjectilePayloads();
            CheckCrossWeapon();
            CheckLink();
            CheckRecovery();
            await CheckCancellation();
            CheckDecoy();
            GD.Print($"[ItemCombatRegression] RESULT checks={_checks} failures={_failures}");
            if (_failures == 0) GD.Print("[ItemCombatRegression] PASS");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError($"[ItemCombatRegression] FAIL {exception}");
            GetTree().Quit(1);
        }
    }
    private void Setup()
    {
        foreach (Enemy enemy in _enemies) { RemoveChild(enemy); enemy.QueueFree(); }
        _enemies.Clear();
        if (_player != null) { RemoveChild(_player); _player.QueueFree(); }
        _player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(_player); _player.InitializeCharacter(CharacterDataLoader.Get("vagabond"));
        _player.IsAIControlled = true; _player.SetPhysicsProcess(false); _player.SetBagCapacity(100);
        foreach (Timer timer in Timers()) timer.Stop();
    }
    private IEnumerable<Timer> Timers()
    {
        foreach (Node node in _player.GetChildren()) if (node is Timer timer) yield return timer;
    }
    private void Add(string id, int copies = 1)
    {
        for (int i = 0; i < copies; i++) Check(_player.AddItemCopy(id), $"Prise {id} {i + 1}/{copies}");
        _player.ObjectCombatEffects?.SetProcess(false);
    }
    private void Remove(string id)
    {
        int copies = _player.FindObject(id)?.Copies ?? 0;
        for (int i = 0; i < copies; i++) _player.ForgetItemCopy(id);
    }
    private Enemy Spawn(Vector2 position, string id = "rodeur")
    {
        Enemy enemy = GD.Load<PackedScene>("res://scenes/enemies/Enemy.tscn").Instantiate<Enemy>();
        AddChild(enemy); enemy.Initialize(EnemyDataLoader.Get(id), 100f, 1f); enemy.SetTicking(false);
        enemy.Position = position;
        typeof(Enemy).GetField("_player", Private).SetValue(enemy, _player);
        _enemies.Add(enemy); return enemy;
    }
    private static float Hp(Enemy enemy) => (float)typeof(Enemy).GetField("_currentHp", Private).GetValue(enemy);
    private static bool Near(float a, float b) => Mathf.Abs(a - b) < .003f;
    private void Check(bool condition, string message)
    {
        _checks++; if (!condition) _failures++;
        GD.Print($"[ItemCombatRegression] {(condition ? "OK" : "FAIL")} {message}");
    }
    private DamageResult Hit(Enemy enemy, WeaponInstance weapon, float damage, DamageKind kind = DamageKind.DirectWeapon) =>
        enemy.TakeDamage(damage, showImpact: false, source: new(_player.GetInstanceId(), weapon, ++_launch, kind, damage));
    private void Dash(Vector2 from = default, Vector2 to = default)
    {
        PlayerDashEvent dash = new(_player.GetInstanceId(), from, to, true);
        _bus.PublishDashStarted(dash with { To = from }); _bus.PublishDashMoved(dash); _bus.PublishDashFinished(dash);
    }
    private WeaponInstance Weapon(string id) => new(WeaponDataLoader.Get(id));
    private void CheckWeaponModifiers()
    {
        Setup();
        (string Item, string Stat, float Step)[] cases = {
            ("tampon_elastique", "knockback", .15f), ("flechette_mousse", "projectile_speed", .15f),
            ("roulement_lisse", "orbital_speed", .12f), ("cordon_spirale", "chain_range", .15f),
            ("fer_a_cheval_miniature", "homing_strength", .15f), ("domino_ebreche", "chain_targets", 1f) };
        foreach ((string item, string stat, float step) in cases)
        {
            WeaponData compatible = WeaponDataLoader.GetAll().Find(data => data.Stats.TryGetValue(stat, out float value) && value > 0f);
            WeaponInstance weapon = new(compatible);
            float initial = _player.GetWeaponStatForDisplay(weapon, stat);
            Add(item, 7);
            float expected = stat == "chain_targets" ? initial + 7 : initial * (1f + 7 * step);
            Check(Near(_player.GetWeaponStatForDisplay(weapon, stat), expected), $"{stat} : sept copies additives dans l'effet, valeur effective");
            typeof(Player).GetField("_equippedWeapon", Private).SetValue(_player, weapon);
            float applied = (float)typeof(Player).GetMethod("GetWeaponStat", Private).Invoke(_player, new object[] { stat });
            Check(Near(applied, expected), $"{stat} : consommateur combat = valeur affichée");
            Remove(item);
            Check(Near(_player.GetWeaponStatForDisplay(weapon, stat), initial), $"{stat} : retrait exact");
        }
        WeaponInstance straight = Weapon("sling");
        Add("fer_a_cheval_miniature", 2);
        Check(_player.GetWeaponStatForDisplay(straight, "homing_strength") == 0f, "Fer à cheval : aucun guidage créé sur projectile droit");
        Remove("fer_a_cheval_miniature");
        // Un impact réel utilise aussi le recul augmenté, pas seulement son libellé.
        WeaponData knockWeapon = WeaponDataLoader.GetAll().Find(data => data.Stats.TryGetValue("knockback", out float value) && value > 0f);
        Enemy pushed = Spawn(new Vector2(90, 0));
        Add("tampon_elastique");
        _player.OnProjectileHit(pushed, 1f, false, new(knockWeapon));
        Vector2 velocity = (Vector2)typeof(Enemy).GetField("_knockVelocity", Private).GetValue(pushed);
        Check(Near(velocity.X, Mathf.Min(80f, knockWeapon.Stats["knockback"] * 1.15f) * 12f), "Recul effectif augmenté de 15 % reçu par la créature");
    }
    private void CheckOrbitalQuantity()
    {
        Setup();
        WeaponData orbital = WeaponDataLoader.GetAll().Find(data => data.AttackPattern == AttackPatternKind.Orbital);
        _player.AddWeapon(new WeaponInstance(orbital));
        List<Node2D> notes = (List<Node2D>)typeof(Player).GetField("_orbitalProjectiles", Private).GetValue(_player);
        int initial = notes.Count;
        Add("diapositive_en_double", 7);
        Check(notes.Count == initial + 7, "Diapositive : sept satellites réels de plus immédiatement");
        Remove("diapositive_en_double");
        Check(notes.Count == initial, "Retrait Diapositive : satellites retirés sans toucher à la base");
        WeaponInstance weapon = null;
        for (int i = 0; i < 4; i++) if (_player.GetWeaponInstance(i)?.Id == orbital.Id) weapon = _player.GetWeaponInstance(i);
        _player.UpgradeWeapon(orbital.Id, new[] { new StatGain("projectile_count", 1f) });
        Check(notes.Count == initial + 1 && Near(_player.GetWeaponStatForDisplay(weapon, "projectile_count"), initial + 1),
            "Progression orbitale unifiée : gain existant de quantité conservé et visible");
        Add("roulement_lisse");
        float before = (float)typeof(Player).GetField("_orbitalAngle", Private).GetValue(_player);
        typeof(Player).GetMethod("ProcessOrbitalWeapons", Private).Invoke(_player, new object[] { .1f });
        float after = (float)typeof(Player).GetField("_orbitalAngle", Private).GetValue(_player);
        Check(Near(after - before, Mathf.DegToRad(weapon.GetStat("orbital_speed") * 1.12f) * .1f), "Roulement : déplacement angulaire réellement accéléré");
    }
    private async Task CheckProjectilePayloads()
    {
        Setup();
        CombatPools pools = new(); AddChild(pools);
        Spawn(new Vector2(80, 0));
        Add("flechette_mousse"); Add("peigne_casse", 2); Add("verre_fele", 34);
        Add("fer_a_cheval_miniature"); Add("cordon_spirale"); Add("domino_ebreche", 2);
        void Fire(WeaponData data)
        {
            WeaponInstance weapon = new(data);
            typeof(Player).GetField("_equippedWeapon", Private).SetValue(_player, weapon);
            typeof(Player).GetMethod("PerformRangedAttack", Private).Invoke(_player, new object[] { data.AttackPattern });
        }
        WeaponData homing = WeaponDataLoader.GetAll().Find(data => data.AttackPattern == AttackPatternKind.Homing);
        Fire(homing);
        Projectile shot = null;
        foreach (Node node in pools.GetChildren()) if (node is Projectile projectile && projectile.SourceWeapon.Id == homing.Id) { shot = projectile; break; }
        Check(shot != null && Near(shot.Speed, homing.Stats["projectile_speed"] * 1.15f), "Fléchette : projectile réellement lancé 15 % plus vite");
        Check(shot != null && (int)typeof(Projectile).GetField("_pierceRemaining", Private).GetValue(shot) == (int)(homing.Stats.GetValueOrDefault("projectile_pierce") + 2),
            "Peigne : deux perforations transmises au projectile réel");
        Check(shot != null && (bool)typeof(Projectile).GetField("_isCrit", Private).GetValue(shot), "Verre : chance critique supérieure à 100 %, vrai projectile critique garanti");
        Check(shot != null && Near((float)typeof(Projectile).GetField("_homingStrength", Private).GetValue(shot), homing.Stats["homing_strength"] * 1.15f),
            "Fer à cheval : guidage réellement renforcé au lancement");
        WeaponData bounce = WeaponDataLoader.GetAll().Find(data => data.AttackPattern == AttackPatternKind.Bounce);
        Fire(bounce);
        shot = null;
        foreach (Node node in pools.GetChildren()) if (node is Projectile projectile && projectile.SourceWeapon.Id == bounce.Id) { shot = projectile; break; }
        Check(shot != null && Near((float)typeof(Projectile).GetField("_bounceRange", Private).GetValue(shot), bounce.Stats["chain_range"] * 1.15f),
            "Cordon : portée de rebond réelle renforcée");
        Check(shot != null && (int)typeof(Projectile).GetField("_bouncesRemaining", Private).GetValue(shot) == (int)bounce.Stats["chain_targets"] + 2,
            "Domino : deux rebonds transmis au projectile réel");
        RemoveChild(pools); pools.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private void CheckCrossWeapon()
    {
        Setup(); Add("registre_biffe");
        Enemy enemy = Spawn(new Vector2(100, 0));
        List<WeaponData> weapons = WeaponDataLoader.GetAll();
        WeaponInstance a = new(weapons[0]), b = new(weapons[1]);
        float before = Hp(enemy);
        Hit(enemy, a, 10); Hit(enemy, b, 30);
        Check(Near(before - Hp(enemy), 50), "Registre : 25 % de (10 + 30) ajoutés par vraie seconde arme");
        before = Hp(enemy); Hit(enemy, a, 10); Hit(enemy, b, 30);
        Check(Near(before - Hp(enemy), 40), "Registre : recharge partagée, pas de boucle passive");
        _player.ObjectCombatEffects.AdvanceClock(2.01f);
        before = Hp(enemy); Hit(enemy, a, 10, DamageKind.Passive);
        Check(Near(before - Hp(enemy), 10), "Registre : dégâts d'objets exclus");
        Remove("registre_biffe"); Add("registre_biffe", 7);
        _player.ObjectCombatEffects.AdvanceClock(5);
        before = Hp(enemy); Hit(enemy, a, 10); Hit(enemy, b, 30);
        Check(Near(before - Hp(enemy), 110), "Registre : sept copies, même effet ×7 sans palier");
        Remove("registre_biffe"); before = Hp(enemy); Hit(enemy, a, 10); Hit(enemy, b, 30);
        Check(Near(before - Hp(enemy), 40), "Registre : effet retiré");
    }
    private void CheckLink()
    {
        Setup(); Add("pelote_rouge", 8);
        Enemy a = Spawn(new Vector2(100, 0)), b = Spawn(new Vector2(150, 0));
        WeaponInstance weapon = new(WeaponDataLoader.GetAll()[0]);
        Hit(a, weapon, 10); Hit(b, weapon, 10); Dash();
        float before = Hp(b); Hit(a, weapon, 20);
        Check(Near(before - Hp(b), 12), "Pelote : huit copies autorisées, transfert plafonné à 60 %");
        before = Hp(a); Hit(b, weapon, 10, DamageKind.Passive);
        Check(Near(before, Hp(a)), "Pelote : aucun transfert récursif depuis dégât passif");
        _player.ObjectCombatEffects.AdvanceClock(3.1f); before = Hp(b); Hit(a, weapon, 20);
        Check(Near(before, Hp(b)), "Pelote : expiration après trois secondes");
        Remove("pelote_rouge"); Add("pelote_rouge"); Dash();
        Check(_player.ObjectCombatEffects.LinkRemaining == 0f, "Pelote : échange ne réinitialise pas la recharge");
        Remove("pelote_rouge");
        _player.ObjectCombatEffects.AdvanceClock(3.1f);
        Hit(a, weapon, 1); Hit(b, weapon, 1); Dash();
        Check(_player.ObjectCombatEffects.LinkRemaining == 0f, "Pelote : huit copies puis retrait total ne laissent aucun résidu actif");
        Add("pelote_rouge"); Hit(a, weapon, 1); Hit(b, weapon, 1); Dash();
        typeof(Enemy).GetField("_currentHp", Private).SetValue(a, 2f);
        before = Hp(b); Hit(a, weapon, 200f);
        Check(Near(before - Hp(b), .3f), "Pelote : surkill de 200 sur 2 PV transfère seulement 15 % des 2 PV perdus");
    }
    private void CheckRecovery()
    {
        Setup(); Add("cassette_rembobinee");
        float initial = _player.CurrentHp;
        typeof(Player).GetMethod("LoseHp", Private).Invoke(_player, new object[] { 20f, PlayerDamageKind.Combat, Vector2.Zero });
        Dash();
        Check(Near(_player.CurrentHp, initial - 15), "Cassette : restitue 25 % des 20 PV réellement perdus");
        Remove("cassette_rembobinee"); Add("cassette_rembobinee");
        typeof(Player).GetMethod("LoseHp", Private).Invoke(_player, new object[] { 8f, PlayerDamageKind.Combat, Vector2.Zero });
        float before = _player.CurrentHp; Dash();
        Check(Near(before, _player.CurrentHp), "Cassette : rééquiper ne contourne pas douze secondes");
        _player.ObjectCombatEffects.AdvanceClock(12.1f); Dash();
        Check(Near(before, _player.CurrentHp), "Cassette : pertes anciennes expirées");
        typeof(Player).GetMethod("LoseHp", Private).Invoke(_player, new object[] { 4f, PlayerDamageKind.Erasure, Vector2.Zero });
        before = _player.CurrentHp; Dash();
        Check(Near(before, _player.CurrentHp), "Cassette : Néant exclu");
        typeof(Player).GetMethod("LoseHp", Private).Invoke(_player, new object[] { 1.5f, PlayerDamageKind.Combat, Vector2.Zero });
        before = _player.CurrentHp; Dash();
        Check(Near(_player.CurrentHp, before + .375f), "Cassette : restitue aussi les pertes fractionnaires");
    }
    private async Task CheckCancellation()
    {
        Setup(); Add("tampon_annule", 2);
        EnemyProjectile Projectile(Vector2 position)
        {
            EnemyProjectile shot = new(); shot.AddChild(new Sprite2D { Name = "Visual" }); AddChild(shot);
            shot.Launch(position, Vector2.Up, 10f, "test", "spit", FxFamily.Hostile);
            return shot;
        }
        EnemyProjectile a = Projectile(new Vector2(20, 0)), b = Projectile(new Vector2(40, 0)), c = Projectile(new Vector2(60, 0));
        Dash(Vector2.Zero, new Vector2(80, 0));
        Check(!a.Visible && !b.Visible && c.Visible, "Tampon : saut instantané supprime les deux premiers tirs sur le trajet");
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        c.QueueFree();
        Remove("tampon_annule");
        EnemyProjectile d = Projectile(new Vector2(20, 0)); Dash(Vector2.Zero, new Vector2(80, 0));
        Check(d.Visible, "Tampon retiré : aucun projectile annulé"); d.QueueFree();
    }
    private void CheckDecoy()
    {
        Setup(); Add("portrait_efface");
        _player.Position = new Vector2(200, 0); Dash(Vector2.Zero, _player.Position);
        Enemy enemy = Spawn(new Vector2(100, 0));
        bool distracted = enemy.ProcessRememberedTarget(.1f);
        Check(distracted && enemy.Velocity.X < 0f, "Portrait : créature part vers souvenir, à l'opposé du joueur");
        typeof(Enemy).GetField("_tier", Private).SetValue(enemy, EnemyTier.Elite);
        Check(!enemy.ProcessRememberedTarget(.1f), "Portrait : élite ignore le leurre");
        typeof(Enemy).GetField("_tier", Private).SetValue(enemy, EnemyTier.Normal);
        List<IEnemyAbility> abilities = (List<IEnemyAbility>)typeof(Enemy).GetField("_abilities", Private).GetValue(enemy);
        PounceAbility pounce = new(enemy, "TestWindup");
        FieldInfo phase = typeof(PounceAbility).GetField("_phase", Private);
        phase.SetValue(pounce, Enum.Parse(phase.FieldType, "Windup")); abilities.Add(pounce);
        Check(pounce.IsActive && !enemy.ProcessRememberedTarget(.1f), "Portrait : bond déjà annoncé conservé");
        phase.SetValue(pounce, Enum.Parse(phase.FieldType, "Ready")); abilities.Clear();
        _player.ObjectCombatEffects.AdvanceClock(2.1f);
        Check(!enemy.ProcessRememberedTarget(.1f), "Portrait : cible expirée, logique normale reprend");
        Remove("portrait_efface"); Add("portrait_efface"); Dash();
        Check(!_player.ObjectCombatEffects.DecoyActive, "Portrait : oubli/rééquipement ne recharge pas");
        _player.ObjectCombatEffects.AdvanceClock(12.1f); Dash();
        ErasureManager erasure = new(); AddChild(erasure); erasure.SetProcess(false);
        Dictionary<Vector2I, ErasureManager.ErasureZonePhase> phases = (Dictionary<Vector2I, ErasureManager.ErasureZonePhase>)typeof(ErasureManager).GetField("_zonePhases", Private).GetValue(erasure);
        phases[Vector2I.Zero] = ErasureManager.ErasureZonePhase.Void;
        typeof(ObjectCombatEffects).GetField("_erasure", Private).SetValue(_player.ObjectCombatEffects, erasure);
        _player.ObjectCombatEffects.AdvanceClock(.01f);
        Check(!_player.ObjectCombatEffects.DecoyActive && !enemy.ProcessRememberedTarget(.1f), "Portrait : le Néant détruit le leurre et libère les cibles");
        erasure.QueueFree();
    }
}
