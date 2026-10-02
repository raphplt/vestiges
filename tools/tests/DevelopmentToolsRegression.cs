using System;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Infrastructure.Steam;
using Vestiges.Score;
using Vestiges.Spawn;
using Vestiges.UI;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>Exerce Main, les entrées F1/F4 et les actions réelles, sur des profils isolés.</summary>
public partial class DevelopmentToolsRegression : Node
{
    private int _checks;
    private string _output;

    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            await Frames(1);
            string[] args = OS.GetCmdlineUserArgs();
            bool normalEntry = Array.IndexOf(args, "--normal-entry") >= 0;
            int outputIndex = Array.IndexOf(args, "--output");
            _output = outputIndex >= 0 ? args[outputIndex + 1] : null;
            Check(DevelopmentMode.IsTestSession == !normalEntry, "provenance résolue avant les autoloads");
            if (!normalEntry)
            {
                Check(!DevelopmentMode.CanSubmitResults && !SteamManager.IsActive, "aucun envoi Steam depuis un banc");
                Check(DevelopmentMode.GetSavePath("fixture.json").StartsWith(DevelopmentMode.IsEnabled ? "user://dev/" : "user://tests/", StringComparison.Ordinal), "profil de banc séparé");
            }
            else
            {
                Check(!DevelopmentMode.IsEnabled && DevelopmentMode.CanSubmitResults, "lancement normal éligible");
                Check(DevelopmentMode.GetSavePath("fixture.json") == "user://fixture.json", "chemin normal conservé");
                Player probe = new();
                ExpectDenied(() => probe.IsGodMode = true);
                ExpectDenied(() => probe.IsAIControlled = true);
                ExpectDenied(() => probe.SurviveFatalHitsForTests = true);
                ExpectDenied(() => probe.DisableDefenseForTests());
                probe.Free();
                PlayerMobility mobility = new(MobilityConfig.Load());
                ExpectDenied(() => mobility.UseInvulnerabilityTrial = true);
            }

            GameManager manager = GetNode<GameManager>("/root/GameManager");
            manager.SelectedCharacterId = "traqueur";
            manager.RunSeed = 221092026;
            WorldSetup world = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<WorldSetup>();
            GetTree().Root.AddChild(world);
            GetTree().CurrentScene = world;
            ulong started = Time.GetTicksMsec();
            while (!world.IsWorldReady || GetTree().Paused || manager.CurrentState != GameManager.GameState.Run)
            {
                if (Time.GetTicksMsec() - started > 90000)
                    throw new InvalidOperationException("Main n'a pas terminé son initialisation.");
                await Frames(1);
            }
            await Frames(3);
            while (world.GetNodeOrNull<GameLoadingOverlay>("GameLoadingOverlay") != null)
            {
                if (Time.GetTicksMsec() - started > 100000)
                    throw new InvalidOperationException("L'overlay de chargement n'a pas disparu.");
                await Frames(1);
            }
            Check(true, "run visible après disparition du chargement");
            Player player = world.GetNode<Player>("Player");
            DebugOverlay overlay = world.GetNodeOrNull<DebugOverlay>("DebugOverlay");
            DebugActionPanel actions = world.GetNodeOrNull<DebugActionPanel>("DebugActionPanel");
            Check((overlay != null && actions != null) == DevelopmentMode.IsEnabled, "panneaux créés uniquement pour le profil dev");
            Check(!DevelopmentMode.SetEnabled(!DevelopmentMode.IsEnabled), "bascule de profil refusée pendant la run");
            // Figer le monde permet de comparer exactement les effets des boutons, sans combat concurrent.
            GetTree().Paused = true;
            KeyInput(Key.F1);
            await Frames(2);
            KeyInput(Key.F4);
            await Frames(2);
            if (DevelopmentMode.IsEnabled)
            {
                Check(overlay.GetChild<Control>(0).Visible && actions.GetChild<Control>(0).Visible, "F1 et F4 ouvrent les vrais panneaux");
                await Capture("dev-f1-f4");
                CheckButton trial = Find<CheckButton>(overlay, button => button.Text.Contains("invulnérabilité"));
                trial.ButtonPressed = true;
                Check(player.Mobility.UseInvulnerabilityTrial, "essai de dash appliqué");
                OptionButton response = Find<OptionButton>(overlay, _ => true);
                response.EmitSignal(OptionButton.SignalName.ItemSelected, 0L);
                Check(player.Mobility.Response == MovementResponse.Direct, "essai de locomotion appliqué");
                Press(actions, "God Mode: OFF");
                float hp = player.CurrentHp;
                player.TakeErasureDamage(5f);
                Check(player.IsGodMode && player.CurrentHp == hp, "bouton invincibilité actif");
                Press(actions, "God Mode: OFF");
                player.TakeErasureDamage(5f);
                Check(!player.IsGodMode && player.CurrentHp < hp, "invincibilité désactivable");
                Press(actions, "Full Heal");
                Check(Mathf.IsEqualApprox(player.CurrentHp, player.EffectiveMaxHp), "bouton de soin actif");
                int level = player.EquippedWeapon.Level;
                Press(actions, "Upgrade Equipped Weapon");
                Check(player.EquippedWeapon.Level == level + 1, "bouton d'amélioration actif");
                SpawnManager spawn = world.GetNode<SpawnManager>("SpawnManager");
                int before = world.GetNode("EnemyContainer").GetChildCount();
                Press(actions, "Spawn Test Enemy (Mouse)");
                Check(world.GetNode("EnemyContainer").GetChildCount() == before + 1, "bouton de spawn actif");
                Press(actions, "Teleport (Right Click): OFF");
                player.GlobalPosition = new Vector2(150f, 100f);
                Vector2 position = player.GlobalPosition;
                actions._UnhandledInput(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Right });
                Check(player.GlobalPosition != position, "téléportation active");
                EventBus bus = GetNode<EventBus>("/root/EventBus");
                float xp = 0f;
                int essence = 0;
                bus.XpGained += amount => xp += amount;
                bus.LootReceived += (kind, id, amount) => { if (kind == "essence") essence += amount; };
                Press(actions, "+ 100 Essence");
                Press(actions, "+ 1000 XP");
                Check(xp >= 1000f && essence >= 100, $"boutons XP et Essence actifs (XP {xp}, Essence {essence}, récompenses de quêtes comprises)");
            }
            else
            {
                Check(overlay == null && actions == null, "F1/F4 sans effet en profil normal");
                await Capture(normalEntry ? "normal-f1-f4" : "test-f1-f4");
                // Une instanciation accidentelle doit également rester sans contrôle utilisable.
                world.AddChild(new DebugActionPanel());
                world.AddChild(new DebugOverlay());
                await Frames(2);
                Check(world.FindChild("DebugActionPanel", true, false) == null && world.FindChild("DebugOverlay", true, false) == null, "panneaux refusés hors profil dev");
            }

            ScoreManager score = world.GetNode<ScoreManager>("ScoreManager");
            RunProvenance expected = DevelopmentMode.IsEnabled ? RunProvenance.Development : normalEntry ? RunProvenance.Normal : RunProvenance.Test;
            Check(score.BuildRunRecord().Provenance == expected, "provenance de la run enregistrable");
            if (!normalEntry)
            {
                manager.ChangeState(GameManager.GameState.Death);
                score.SaveEndOfRun();
                Check(RunHistoryManager.GetHistory()[0].Provenance == expected, "provenance retrouvée dans l'historique");
                Check(!SteamManager.CanSubmitResults, "score de fin d'essai non soumissible");
            }
            if (normalEntry)
            {
                manager.ChangeState(GameManager.GameState.Hub);
                Check(DevelopmentMode.SetEnabled(true) && DevelopmentMode.SetEnabled(false), "bascules depuis le Hub disponibles");
                Check(!DevelopmentMode.CanSubmitResults && !SteamManager.IsActive, "Steam reste fermé après retour au profil normal");
            }
            GD.Print($"[DevelopmentToolsRegression] RESULT failures=0 checks={_checks} provenance={expected}");
            await GameExit.QuitAsync(GetTree(), 0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[DevelopmentToolsRegression] {exception}");
            GetTree().Quit(1);
        }
    }

    private void ExpectDenied(Action action)
    {
        try { action(); }
        catch (InvalidOperationException) { Check(true, "hook refusé sans profil dev/banc"); return; }
        throw new InvalidOperationException("Un hook de modification est accessible en profil normal.");
    }

    private static T Find<T>(Node root, Func<T, bool> predicate) where T : Node
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is T match && predicate(match))
                return match;
            T nested = Find<T>(child, predicate);
            if (nested != null)
                return nested;
        }
        return null;
    }

    private static void Press(Node root, string text)
    {
        Button button = Find<Button>(root, candidate => candidate.Text == text);
        if (button == null)
            throw new InvalidOperationException($"Bouton absent : {text}");
        button.EmitSignal(Button.SignalName.Pressed);
    }

    private static void KeyInput(Key key)
    {
        Input.ParseInputEvent(new InputEventKey { Keycode = key, Pressed = true });
        Input.ParseInputEvent(new InputEventKey { Keycode = key, Pressed = false });
    }

    private async Task Frames(int count)
    {
        for (int frame = 0; frame < count; frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task Capture(string name)
    {
        if (_output == null)
            return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using Image image = GetViewport().GetTexture().GetImage();
        Check(image.SavePng(_output.PathJoin(name + ".png")) == Error.Ok, "capture sauvegardée");
    }

    private void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        _checks++;
        GD.Print($"[DevelopmentToolsRegression] OK {message}");
    }
}
