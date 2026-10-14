using System;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.Combat;
using Vestiges.Development;
using Vestiges.Progression;
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
                DevelopmentRunTools toolsProbe = new();
                ExpectDenied(() => toolsProbe.Execute("god on"));
                toolsProbe.Free();
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
                DevelopmentRunTools tools = world.GetNode<DevelopmentRunTools>("DevelopmentRunTools");
                Check(tools.Progression.CurrentLevel == 7 && tools.DifficultySeconds >= 600 && player.IsGodMode, "commandes de lancement appliquées");
                Check(Find<Button>(actions, button => button.Name == "OpenWorkshop") != null, "accès visible aux outils");
                CheckButton god = Find<CheckButton>(actions, button => button.Text == "Invincibilité");
                god.ButtonPressed = false;
                float hp = player.CurrentHp;
                player.TakeErasureDamage(5f);
                Check(!player.IsGodMode && player.CurrentHp < hp, "invincibilité désactivable depuis le panneau");
                god.ButtonPressed = true;
                hp = player.CurrentHp;
                player.TakeErasureDamage(5f);
                player.TakeDamage(5f);
                Check(player.IsGodMode && player.CurrentHp == hp, "invincibilité contre les coups et le Néant");
                Press(actions, "Soigner");
                Check(Mathf.IsEqualApprox(player.CurrentHp, player.EffectiveMaxHp), "bouton de soin actif");
                SpinBox levelInput = Find<SpinBox>(actions, node => node.Name == "PlayerLevel");
                levelInput.Value = 40;
                Press(actions, "Fixer le niveau");
                Check(tools.Progression.CurrentLevel == 40 && world.GetNode<FragmentManager>("FragmentManager").PendingChoices.Count == 0, "niveau précis sans choix ajoutés");
                levelInput.Value = 3;
                Press(actions, "Fixer le niveau");
                Check(tools.Progression.CurrentLevel == 3 && tools.Progression.CurrentXp == 0, "retour à un niveau bas");
                OptionButton weaponInput = Find<OptionButton>(actions, node => node.Name == "WeaponCatalog");
                for (int i = 0; i < weaponInput.ItemCount; i++)
                    if (weaponInput.GetItemMetadata(i).AsString() == "makeshift_bow") weaponInput.Select(i);
                Find<SpinBox>(actions, node => node.Name == "WeaponLevel").Value = 10;
                Press(actions, "Équiper / reconstruire");
                Check(player.GetWeaponInstance(0).Id == "makeshift_bow" && player.GetWeaponInstance(0).Level == 10, "arme et niveau choisis dans le panneau");
                CheckButton teleport = Find<CheckButton>(actions, button => button.Text.Contains("Téléportation"));
                teleport.ButtonPressed = true;
                player.GlobalPosition = new Vector2(150f, 100f);
                Vector2 position = player.GlobalPosition;
                actions._UnhandledInput(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Right });
                Check(player.GlobalPosition != position, "téléportation en coordonnées monde");
                actions.TogglePanel();
                Check(GetTree().Paused, "fermer conserve une pause déjà active");
                GetTree().Paused = false;
                actions.TogglePanel();
                Check(GetTree().Paused, "ouvrir suspend une simulation active");
                KeyInput(Key.Escape);
                await Frames(2);
                Check(!GetTree().Paused && !actions.GetChild<Control>(0).Visible, "Échap ferme et reprend la simulation");
                GetTree().Paused = true;
                await CheckCommands(tools, player);
                actions.TogglePanel();
                await Capture("dev-workshop");
                ScrollContainer scroll = Find<ScrollContainer>(actions, node => node.Name == "WorkshopScroll");
                scroll.ScrollVertical = (int)scroll.GetVScrollBar().MaxValue;
                await Frames(2);
                await Capture("dev-workshop-time-console");

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

            if (!DevelopmentMode.IsEnabled && !normalEntry)
                await CheckCommands(world.GetNode<DevelopmentRunTools>("DevelopmentRunTools"), player);

            ScoreManager score = world.GetNode<ScoreManager>("ScoreManager");
            RunProvenance expected = DevelopmentMode.IsEnabled ? RunProvenance.Development : normalEntry ? RunProvenance.Normal : RunProvenance.Test;
            Check(score.BuildRunRecord().Provenance == expected, "provenance de la run enregistrable");
            if (!normalEntry)
            {
                DevelopmentRunTools tools = world.GetNode<DevelopmentRunTools>("DevelopmentRunTools");
                Check(tools.Execute("level 1").Success, "préparation d'un vrai gain de niveaux");
                if (actions != null)
                {
                    actions.TogglePanel();
                    GetTree().Paused = false;
                    actions.TogglePanel();
                }
                Check(tools.Execute("xp 1000").Success && tools.HasPendingChoices, "XP déclenchant les vrais choix de progression");
                Check(!tools.Execute("pause off").Success && GetTree().Paused, "choix de progression maintenus en pause");
                if (actions != null)
                {
                    actions.TogglePanel();
                    Check(GetTree().Paused, "fermer l'atelier conserve la pause des choix");
                }
                manager.ChangeState(GameManager.GameState.Death);
                Check(Engine.TimeScale == 1, "vitesse restaurée au bilan");
                Check(!world.GetNode<DevelopmentRunTools>("DevelopmentRunTools").Execute("god off").Success, "commandes refusées après la run");
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

    private async Task CheckCommands(DevelopmentRunTools tools, Player player)
    {
        void Run(string command) => Check(tools.Execute(command).Success, "commande " + command);
        Run("god on");
        Run("heal");
        Run("level 25");
        Run("weapon makeshift_bow 1 8 rare");
        float damage = player.GetWeaponInstance(0).GetStat("damage");
        Run("weapon makeshift_bow 1 8 rare");
        Check(player.GetWeaponInstance(0).GetStat("damage") == damage, "reconstruction reproductible");
        Run("weapon chipped_blade 2 1 common");
        Check(player.WeaponSlots.Count == 2, "ajout au premier slot libre");
        WeaponInstance second = player.GetWeaponInstance(1);
        Check(!tools.Execute("weapon makeshift_bow 2 3 rare").Success && player.GetWeaponInstance(1) == second, "doublon refusé sans perdre l'arme");
        Run("weapon makeshift_bow 1 1 common");
        Check(player.GetWeaponInstance(0).Level == 1 && player.GetWeaponInstance(1) == second, "niveau d'arme réduit, autre slot conservé");
        Run("remove-weapon 2");
        Run("time 1800");
        Check(tools.DifficultySeconds == 1800, "horloge de difficulté exacte en pause");
        foreach (string invalid in new[] { "", "god maybe", "level 0", "level 1 extra", "xp -1", "speed 0", "speed NaN", "time Infinity", "weapon absent 1 3 rare", "weapon makeshift_bow 1 10000 rare", "weapon makeshift_bow 1 2 absent", "weapon makeshift_bow 4 1 common", "remove-weapon 4", "spawn absent 0 0" })
            Check(!tools.Execute(invalid).Success, "refus : " + invalid);
        Check(player.GetWeaponInstance(0).Level == 1 && tools.DifficultySeconds == 1800 && tools.Progression.CurrentLevel == 25, "arguments invalides sans mutation");
        Run("speed 0.5");
        Check(Engine.TimeScale == 0.5, "ralenti actif");
        Run("speed 2");
        Check(Engine.TimeScale == 2, "accélération active");
        Run("pause off");
        await Frames(3);
        Check(tools.DifficultySeconds > 1800, "simulation reprise à partir de l'horloge réglée");
        Run("pause on");
        Run("move 0 0");
        int before = tools.GetParent().GetNode("EnemyContainer").GetChildCount();
        Run("spawn shadow_crawler 100 0");
        Check(tools.GetParent().GetNode("EnemyContainer").GetChildCount() == before + 1, "spawn par commande partagé");
        float xp = 0f;
        int essence = 0;
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        bus.XpGained += amount => xp += amount;
        bus.LootReceived += (kind, id, amount) => { if (kind == "essence") essence += amount; };
        Run("essence 123"); Run("xp 1000");
        Check(xp >= 1000 && essence >= 123, "quantités de XP et Essence choisies");

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
