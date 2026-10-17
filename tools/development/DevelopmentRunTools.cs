using System;
using System.Globalization;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.Spawn;
using Vestiges.World;

namespace Vestiges.Development;

public readonly record struct DevelopmentCommandResult(bool Success, string Message);

/// <summary>Commandes d'essai communes au panneau, aux arguments de lancement et aux bancs, limitées à une run.</summary>
public partial class DevelopmentRunTools : Node
{
    private Player _player;
    private PlayerProgression _progression;
    private SpawnManager _spawner;
    private FragmentManager _fragments;
    private EventBus _eventBus;
    private GameManager _manager;
    private double _previousTimeScale;
    private bool _changedTimeScale;

    public bool HasPendingChoices => _fragments.PendingChoices.Count > 0;
    public bool IsRunActive => _manager.CurrentState == GameManager.GameState.Run;
    public Player Player => _player;
    public PlayerProgression Progression => _progression;
    public float DifficultySeconds => _spawner.ElapsedSeconds;
    public bool TeleportEnabled { get; private set; }

    public override void _Ready()
    {
        DevelopmentMode.RequireTestAccess();
        ProcessMode = ProcessModeEnum.Always;
        _player = GetParent().GetNode<Player>("Player");
        _progression = _player.GetNode<PlayerProgression>("PlayerProgression");
        _spawner = GetParent().GetNode<SpawnManager>("SpawnManager");
        _fragments = GetParent().GetNode<FragmentManager>("FragmentManager");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _manager = GetNode<GameManager>("/root/GameManager");
        _previousTimeScale = Engine.TimeScale;
        _eventBus.GameStateChanged += OnStateChanged;
        string[] arguments = OS.GetCmdlineUserArgs();
        for (int i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] != "--dev-command")
                continue;
            if (++i >= arguments.Length)
            {
                GD.PushError("[DevelopmentRunTools] --dev-command exige une commande.");
                break;
            }
            DevelopmentCommandResult result = Execute(arguments[i]);
            if (!result.Success)
                GD.PushError($"[DevelopmentRunTools] {result.Message}");
        }
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.GameStateChanged -= OnStateChanged;
        RestoreTimeScale();
    }

    private void OnStateChanged(string oldState, string state)
    {
        if (state != nameof(GameManager.GameState.Run))
            RestoreTimeScale();
    }

    private void RestoreTimeScale()
    {
        if (!_changedTimeScale)
            return;
        Engine.TimeScale = _previousTimeScale;
        _changedTimeScale = false;
    }

    /// <summary>Les refus sont rendus à l'interface ; aucun argument invalide ne doit modifier la run.</summary>
    public DevelopmentCommandResult Execute(string command)
    {
        DevelopmentMode.RequireTestAccess();
        if (_manager.CurrentState != GameManager.GameState.Run || _player.IsDead)
            return new(false, "Ces commandes exigent une run active.");
        try
        {
            string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                return new(false, "Commande vide.");
            string message;
            switch (parts[0])
            {
                case "god":
                    Count(parts, 2); _player.IsGodMode = Toggle(parts[1]);
                    message = $"Invincibilité {(_player.IsGodMode ? "activée" : "désactivée")}."; break;
                case "heal":
                    Count(parts, 1); _player.Heal(_player.EffectiveMaxHp);
                    message = "Santé restaurée."; break;
                case "level":
                    Count(parts, 2); int level = Integer(parts[1], 1, 9999);
                    _progression.SetLevelForDevelopment(level);
                    message = $"Niveau du joueur : {level} (sans cartes, XP remise à zéro)."; break;
                case "xp":
                    Count(parts, 2); float xp = Number(parts[1], 0, 1000000);
                    _eventBus.EmitSignal(EventBus.SignalName.XpGained, xp);
                    message = $"{xp} XP ajoutée(s), bonus du build appliqué."; break;
                case "essence":
                    Count(parts, 2); int essence = Integer(parts[1], 0, 1000000);
                    _eventBus.EmitSignal(EventBus.SignalName.LootReceived, "essence", "debug_essence", essence);
                    message = $"{essence} Essence ajoutée(s)."; break;
                case "weapon": message = SetWeapon(parts); break;
                case "remove-weapon":
                    Count(parts, 2); int slot = Integer(parts[1], 1, Player.MaxWeaponSlots) - 1;
                    if (_player.GetWeaponInstance(slot) == null)
                        throw new ArgumentException("Cet emplacement est vide.");
                    _player.RemoveWeapon(slot); message = $"Arme retirée de l'emplacement {slot + 1}."; break;
                case "time":
                    Count(parts, 2); float seconds = Number(parts[1], 0, 86400);
                    _spawner.SetDifficultyTimeForDevelopment(seconds);
                    message = $"Horloge de difficulté : {seconds} s. Monde et événements conservent leur état."; break;
                case "speed":
                    Count(parts, 2); double speed = Number(parts[1], 0.1f, 8f);
                    Engine.TimeScale = speed; _changedTimeScale = true;
                    message = $"Simulation ×{speed}."; break;
                case "pause":
                    Count(parts, 2); bool paused = Toggle(parts[1]);
                    if (!paused && HasPendingChoices)
                        throw new ArgumentException("Terminer les choix de progression avant de reprendre la simulation.");
                    GetTree().Paused = paused;
                    message = GetTree().Paused ? "Simulation en pause." : "Simulation reprise."; break;
                case "teleport":
                    Count(parts, 2); TeleportEnabled = Toggle(parts[1]);
                    message = TeleportEnabled ? "Téléportation au clic droit activée." : "Téléportation désactivée."; break;
                case "move":
                    Count(parts, 3); _player.GlobalPosition = new Vector2(Number(parts[1], -100000, 100000), Number(parts[2], -100000, 100000));
                    message = "Joueur téléporté."; break;
                case "spawn":
                    Count(parts, 4);
                    bool knownEnemy = false;
                    foreach (string id in EnemyDataLoader.GetAllIds())
                        if (id == parts[1]) knownEnemy = true;
                    if (!knownEnemy)
                        throw new ArgumentException("Créature inconnue.");
                    Vector2 position = new(Number(parts[2], -100000, 100000), Number(parts[3], -100000, 100000));
                    if (_spawner.SpawnEventEnemy(parts[1], position) == null)
                        throw new ArgumentException("Apparition impossible à cet endroit.");
                    message = $"Créature créée : {parts[1]}."; break;
                case "land": message = ChangeLand(parts); break;
                default: return new(false, "Commande inconnue. Voir doc/DEV-MODE.md.");
            }
            GD.Print($"[DevelopmentRunTools] {command} → {message}");
            return new(true, message);
        }
        catch (ArgumentException exception) { return new(false, exception.Message); }
    }

    /// <summary>Plan 51 : « land next » passe à la terre suivante, « land N » à la terre de rang N.</summary>
    private string ChangeLand(string[] parts)
    {
        Count(parts, 2);
        LandTransition transition = GetNodeOrNull<LandTransition>("/root/Main/LandTransition")
            ?? throw new ArgumentException("Aucun passage de terre dans cette scène.");
        if (transition.IsChanging)
            throw new ArgumentException("Un passage est déjà en cours.");
        int index = parts[1] == "next" ? transition.LandIndex + 1 : Integer(parts[1], 0, 999);
        _ = transition.GoToLandAsync(index);
        return $"Passage vers la terre {index} lancé.";
    }

    private string SetWeapon(string[] parts)
    {
        Count(parts, 5);
        WeaponData data = WeaponDataLoader.Get(parts[1]) ?? throw new ArgumentException("Arme inconnue.");
        int slot = Integer(parts[2], 1, Player.MaxWeaponSlots) - 1;
        WeaponInstance weapon = new(data);
        int level = Integer(parts[3], 1, weapon.MaxLevel);
        UpgradeRarity rarity = null;
        foreach (UpgradeRarity candidate in UpgradeRoller.Rarities)
            if (candidate.Id == parts[4]) rarity = candidate;
        if (rarity == null) throw new ArgumentException("Rareté d'amélioration inconnue.");
        int count = _player.WeaponSlots.Count;
        if (slot > count) throw new ArgumentException("Choisir un emplacement occupé ou le premier emplacement libre.");
        for (int i = 0; i < count; i++)
            if (i != slot && _player.GetWeaponInstance(i).Id == data.Id)
                throw new ArgumentException("Cette arme occupe déjà un autre emplacement.");
        // Flux propre et recréé à chaque commande : même seed et recette, même arme, sans déplacer les tirages de la run.
        using RandomNumberGenerator rng = new() { Seed = RunRandom.SeedFor("development-weapons") };
        while (weapon.Level < level)
            weapon.ApplyUpgrade(UpgradeRoller.RollWeaponGains(weapon, rarity, rng), rarity.Id);
        if (slot == count)
        {
            _player.SetWeaponSlotLimit(Mathf.Max(_player.WeaponSlotLimit, slot + 1));
            if (!_player.AddWeapon(weapon)) throw new ArgumentException("Ajout d'arme impossible.");
        }
        else
            _player.ReplaceWeaponForDevelopment(slot, weapon);
        return $"Emplacement {slot + 1} : {data.Name}, niveau {level} ({rarity.Id}).";
    }

    private static void Count(string[] parts, int count)
    {
        if (parts.Length != count) throw new ArgumentException("Nombre d'arguments incorrect. Voir doc/DEV-MODE.md.");
    }
    private static bool Toggle(string value) => value switch
    {
        "on" => true, "off" => false, _ => throw new ArgumentException("Valeur attendue : on ou off.")
    };
    private static int Integer(string value, int min, int max)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) || number < min || number > max)
            throw new ArgumentException($"Entier attendu entre {min} et {max}.");
        return number;
    }
    private static float Number(string value, float min, float max)
    {
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) || !float.IsFinite(number) || number < min || number > max)
            throw new ArgumentException($"Nombre attendu entre {min} et {max} (point décimal). ");
        return number;
    }
}
