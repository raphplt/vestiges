using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Chargement récupérable (plan 26 Q4), sur la vraie Main : un scénario par lancement (--scenario).
/// normal : la run démarre. random-seed : la seed tirée au lancement est publiée. fault-catalogues, fault-generation, fault-decors : une panne injectée affiche l'écran
/// d'erreur, l'arbre reste en pause derrière lui et le bouton ramène au Hub. leave-generation, leave-decors : la
/// scène quittée en route ne laisse ni pause, ni décors orphelins, ni exception. quit : fermeture pendant le chargement.
/// </summary>
public partial class LoadingRecoveryRegression : Node
{
    private const string HubScene = "res://scenes/Hub.tscn";
    private const ulong TimeoutMsec = 120000;

    private int _checks;
    private string _scenario;
    private string _output;

    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            await Frames(1);
            string[] args = OS.GetCmdlineUserArgs();
            int index = Array.IndexOf(args, "--scenario");
            _scenario = index >= 0 ? args[index + 1] : "normal";
            int outputIndex = Array.IndexOf(args, "--output");
            _output = outputIndex >= 0 ? args[outputIndex + 1] : null;
            GameManager manager = GetNode<GameManager>("/root/GameManager");
            manager.SelectedCharacterId = "traqueur";
            manager.RunSeed = 221092026;

            switch (_scenario)
            {
                case "normal": await NormalLoad(manager); break;
                case "random-seed": await RandomSeedLoad(manager); break;
                case "fault-catalogues": await FaultedLoad("catalogues", "Catalogues"); break;
                case "fault-generation": await FaultedLoad("génération", "Création du monde"); break;
                case "fault-decors": await FaultedLoad("décors", "Décors"); break;
                case "leave-generation": await LeaveDuringGeneration(); break;
                case "leave-decors": await LeaveDuringProps(); break;
                case "quit": await QuitDuringLoad(); return;
                default: throw new InvalidOperationException($"scénario inconnu : {_scenario}");
            }
            GD.Print($"[LoadingRecoveryRegression] RESULT failures=0 checks={_checks} scenario={_scenario}");
            await GameExit.QuitAsync(GetTree(), 0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[LoadingRecoveryRegression] FAIL {_scenario} : {exception}");
            GetTree().Quit(1);
        }
    }

    private async Task NormalLoad(GameManager manager)
    {
        WorldSetup world = StartMain();
        await Until(() => world.IsWorldReady && !GetTree().Paused && manager.CurrentState == GameManager.GameState.Run,
            "Main n'a pas terminé son chargement");
        Check(Find<Control>(world, "LoadingFailure") == null, "chargement normal : run démarrée, aucun écran d'erreur");
    }

    /// <summary>Q8c : une run sans seed imposée publie la seed tirée au lancement, et le record la garde.</summary>
    private async Task RandomSeedLoad(GameManager manager)
    {
        manager.RunSeed = 0;
        manager.EffectiveSeed = 0;
        WorldSetup world = StartMain();
        await Until(() => world.IsWorldReady && !GetTree().Paused && manager.CurrentState == GameManager.GameState.Run,
            "Main n'a pas terminé son chargement");
        Check(manager.RunSeed == 0 && manager.EffectiveSeed != 0 && manager.EffectiveSeed == world.Seed,
            $"seed aléatoire publiée ({manager.EffectiveSeed}), demande du Hub laissée à 0");
        ulong recorded = world.GetNode<Vestiges.Score.ScoreManager>("ScoreManager").BuildRunRecord().Seed;
        Check(recorded == world.Seed, $"le record garde la seed effective ({recorded})");
    }

    private async Task FaultedLoad(string step, string shownStep)
    {
        LoadGuard.FaultStep = step;
        WorldSetup world = StartMain();
        Control failure = null;
        await Until(() => (failure = Find<Control>(world, "LoadingFailure")) != null, "écran d'erreur absent");
        await Frames(2);
        Check(GetTree().Paused, $"panne « {step} » : la run à moitié construite reste en pause derrière l'écran");
        Button back = Find<Button>(failure, "ReturnToCamp");
        Check(back != null && GetViewport().GuiGetFocusOwner() == back, "bouton de retour présent et focalisé (manette, clavier)");
        string detail = FindDetail(failure);
        Check(detail.Contains(shownStep, StringComparison.Ordinal) && detail.Contains("panne injectée", StringComparison.Ordinal),
            $"diagnostic affiché : étape et message ({detail})");
        await Capture($"echec-{step}");
        Node2D staging = ReadField<Node2D>(world, "_propStaging");
        back.EmitSignal(Button.SignalName.Pressed);
        await Until(() => GetTree().CurrentScene?.SceneFilePath == HubScene, "le bouton ne ramène pas au Hub");
        await Frames(5);
        Check(!GetTree().Paused, "Hub atteint, arbre dépausé");
        Check(staging == null || !GodotObject.IsInstanceValid(staging), "décors en attente libérés avec la scène");
    }

    private async Task LeaveDuringGeneration()
    {
        WorldSetup world = StartMain();
        await Frames(2);
        Task generation = ReadField<Task>(world, "_generation");
        Check(generation != null && !generation.IsCompleted, "génération encore en cours au moment du départ");
        GetTree().ChangeSceneToFile(HubScene);
        await Until(() => GetTree().CurrentScene?.SceneFilePath == HubScene, "Hub non atteint");
        await Until(() => generation.IsCompleted, "génération jamais terminée");
        await Frames(30);
        Check(!GetTree().Paused, "scène quittée pendant la génération : Hub dépausé, aucune suite sur la scène détruite");
    }

    private async Task LeaveDuringProps()
    {
        WorldSetup world = StartMain();
        Node2D staging = null;
        await Until(() => (staging = ReadField<Node2D>(world, "_propStaging")) != null, "pose des décors jamais atteinte");
        int props = staging.GetChildCount();
        GetTree().ChangeSceneToFile(HubScene);
        await Until(() => GetTree().CurrentScene?.SceneFilePath == HubScene, "Hub non atteint");
        await Frames(30);
        Check(!GetTree().Paused, "scène quittée pendant la pose des décors : Hub dépausé");
        Check(!GodotObject.IsInstanceValid(staging), $"décors en attente libérés ({props} nœuds hors de l'arbre)");
    }

    private async Task QuitDuringLoad()
    {
        WorldSetup world = StartMain();
        await Until(() => ReadField<Node2D>(world, "_propStaging") != null, "pose des décors jamais atteinte");
        GD.Print("[LoadingRecoveryRegression] RESULT failures=0 checks=0 scenario=quit");
        // Le chemin du jeu quand on ferme la fenêtre pendant le chargement.
        await GameExit.QuitAsync(GetTree(), 0);
    }

    private WorldSetup StartMain()
    {
        WorldSetup world = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<WorldSetup>();
        GetTree().Root.AddChild(world);
        GetTree().CurrentScene = world;
        return world;
    }

    private async Task Until(Func<bool> condition, string failure)
    {
        ulong deadline = Time.GetTicksMsec() + TimeoutMsec;
        while (!condition())
        {
            if (Time.GetTicksMsec() > deadline)
                throw new InvalidOperationException(failure);
            // Sans rendu, les images s'enchaînent plus vite que le thread de génération : lui laisser du CPU.
            OS.DelayMsec(1);
            await Frames(1);
        }
    }

    private static T ReadField<T>(object target, string name) where T : class =>
        GodotObject.IsInstanceValid(target as GodotObject)
            ? target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target) as T
            : null;

    private static string FindDetail(Node failure)
    {
        foreach (Node child in failure.GetChildren())
        {
            if (child is Label label && label.Text.Contains("panne", StringComparison.Ordinal))
                return label.Text;
        }
        return "";
    }

    private static T Find<T>(Node root, string name) where T : Node
    {
        if (!GodotObject.IsInstanceValid(root))
            return null;
        foreach (Node child in root.GetChildren())
        {
            if (child is T match && child.Name == name)
                return match;
            T nested = Find<T>(child, name);
            if (nested != null)
                return nested;
        }
        return null;
    }

    /// <summary>Image de l'écran d'erreur, avec --output et un rendu réel (pas en headless).</summary>
    private async Task Capture(string name)
    {
        if (_output == null || DisplayServer.GetName() == "headless")
            return;
        await Frames(20);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using Image image = GetViewport().GetTexture().GetImage();
        Check(image.SavePng(_output.PathJoin(name + ".png")) == Error.Ok, $"capture {name} enregistrée");
    }

    private async Task Frames(int count)
    {
        for (int frame = 0; frame < count; frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        _checks++;
        GD.Print($"[LoadingRecoveryRegression] OK {message}");
    }
}
