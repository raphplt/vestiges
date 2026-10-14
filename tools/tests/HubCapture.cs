using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

namespace Vestiges.Tests;

/// <summary>
/// Captures de l'écran d'accueil rendu : état initial, puis une capture par action rejouée.
/// --output DIR, --actions ui_right,ui_accept (actions d'input envoyées une à une, capture après chacune).
/// --capture-size WxH : taille réelle de la fenêtre et des images, également contrôlée dans les logs.
/// --locale fr|en : langue imposée au profil isolé de la capture.
/// --record N : après la dernière action, enregistre N frames d'affilée en vignettes (départ en run, chargement),
/// avec le temps écoulé et la scène courante de chaque frame.
/// --collection-focus ID : arrivée depuis le bilan, « Voir dans la Collection » sur l'arme ID.
/// --quest-focus ID : ouvre la quête précise ; les actions suivantes exercent ses liens réels vers la Collection.
/// --fake-leaderboard : l'écran Classement lit un faux service rempli (aucun appel à Steam).
/// --character ID : personnage sélectionné avant ouverture de la page Characters.
/// --claim-quests a,b : quêtes accomplies dans le profil isolé avant l'accueil (badge « Nouveau » de la Collection).
/// --meta-fixture TEXTE : contenu déposé dans la sauvegarde méta avant l'accueil (avis de sauvegarde, plan 26 Q2b).
/// --open-settings : ouvre les Paramètres de l'accueil.
/// </summary>
public partial class HubCapture : Node
{
    private string _output;

    public override async void _Ready()
    {
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            string[] args = OS.GetCmdlineUserArgs();
            string locale = Argument(args, "--locale", "");
            if (locale.Length > 0)
            {
                if (!Vestiges.Infrastructure.LocaleManager.SupportedLocales.ContainsKey(locale))
                    throw new ArgumentException($"Langue de capture inconnue : {locale}");
                Vestiges.Infrastructure.LocaleManager.Instance.SetLocale(locale);
            }
            _output = Argument(args, "--output", "/tmp/vestiges-hub");
            DirAccess.MakeDirRecursiveAbsolute(_output);
            string focus = Argument(args, "--collection-focus", "");
            if (focus.Length > 0)
                GetNode<Vestiges.Core.GameManager>("/root/GameManager").CollectionFocusWeaponId = focus;

            string fixture = Argument(args, "--meta-fixture", "");
            if (fixture.Length > 0)
            {
                System.IO.File.WriteAllText(ProjectSettings.GlobalizePath(Vestiges.Infrastructure.DevelopmentMode.GetSavePath("meta_save.json")), fixture);
                Vestiges.Infrastructure.MetaSaveManager.ReloadProfile();
            }

            // --fake-leaderboard : classement rempli par un faux service, ta place 43ᵉ (plan 43 C2).
            if (System.Array.IndexOf(args, "--fake-leaderboard") >= 0)
                Vestiges.Infrastructure.Steam.SteamManager.UseLeaderboardsForTests(
                    new Vestiges.Infrastructure.Steam.LeaderboardQueue(new DemoLeaderboardBackend()), DemoLeaderboardBackend.LocalId);

            // --claim-quests a,b : quêtes accomplies avant l'accueil, leurs pièces portent le badge « Nouveau ».
            foreach (string questId in Argument(args, "--claim-quests", "").Split(',', System.StringSplitOptions.RemoveEmptyEntries))
                Vestiges.Infrastructure.MetaSaveManager.ClaimQuest(Vestiges.Infrastructure.QuestDataLoader.Get(questId), out _);

            string characterId = Argument(args, "--character", "");
            if (characterId.Length > 0)
            {
                if (Vestiges.Infrastructure.CharacterDataLoader.Get(characterId) == null)
                    throw new ArgumentException($"Personnage de capture inconnu : {characterId}");
                GetNode<Vestiges.Core.GameManager>("/root/GameManager").SelectedCharacterId = characterId;
            }

            GetTree().CurrentScene = null;
            Node hub = GD.Load<PackedScene>("res://scenes/Hub.tscn").Instantiate();
            GetTree().Root.AddChild(hub);
            GetTree().CurrentScene = hub;
            await Frames(150);
            string resolution = Argument(args, "--capture-size", "");
            if (resolution.Length > 0)
            {
                string[] dimensions = resolution.Split('x');
                // Sous macOS, --resolution ne suffisait pas : imposer la taille après le démarrage du Hub.
                GetWindow().Mode = Window.ModeEnum.Windowed;
                await Frames(15);
                GetWindow().Size = new Vector2I(int.Parse(dimensions[0], CultureInfo.InvariantCulture),
                    int.Parse(dimensions[1], CultureInfo.InvariantCulture));
                await Frames(30);
            }
            string page = Argument(args, "--open-page", "");
            if (page.Length > 0)
            {
                System.Type stateType = typeof(Vestiges.UI.HubScreen).GetNestedType("HubState", BindingFlags.NonPublic);
                hub.GetType().GetMethod("OpenPage", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(hub, new[] { System.Enum.Parse(stateType, page) });
                await Frames(30);
            }
            // --open-settings : ouvre les Paramètres de l'accueil (curseurs, bouton fermer).
            if (System.Array.IndexOf(args, "--open-settings") >= 0)
            {
                ((Vestiges.UI.SettingsScreen)hub.GetType().GetField("_settingsScreen", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(hub)).Open();
                await Frames(30);
            }
            string tab = Argument(args, "--collection-tab", "");
            if (tab.Length > 0)
            {
                typeof(Vestiges.UI.HubCollectionPanel).GetField("_tab", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, tab);
                System.Type stateType = typeof(Vestiges.UI.HubScreen).GetNestedType("HubState", BindingFlags.NonPublic);
                hub.GetType().GetMethod("OpenPage", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(hub, new[] { System.Enum.Parse(stateType, "Collection") });
                await Frames(30);
                Vestiges.UI.HubCollectionPanel collection = (Vestiges.UI.HubCollectionPanel)hub.GetType()
                    .GetField("_collectionPanel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(hub);
                GridContainer grid = (GridContainer)typeof(Vestiges.UI.HubCollectionPanel)
                    .GetField("_grid", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(collection);
                int index = int.Parse(Argument(args, "--collection-index", "0"), CultureInfo.InvariantCulture);
                ((Control)grid.GetChild(Mathf.Clamp(index, 0, grid.GetChildCount() - 1))).GrabFocus();
                await Frames(20);
            }
            string questFocus = Argument(args, "--quest-focus", "");
            if (questFocus.Length > 0)
            {
                typeof(Vestiges.UI.HubScreen).GetMethod("OpenQuest", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(hub, new object[] { questFocus });
                await Frames(30);
            }
            if (characterId.Length > 0 && page == "Characters")
            {
                Vestiges.UI.HubCharactersPanel panel = (Vestiges.UI.HubCharactersPanel)hub.GetType()
                    .GetField("_charactersPanel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(hub);
                if (panel.ExaminedCharacterId != characterId)
                    throw new InvalidOperationException("Le Hub n'examine pas le personnage demandé.");
                GD.Print($"[HubCapture] character={panel.ExaminedCharacterId} unlocked={panel.CanDepart}");
            }
            Capture("hub-00");

            string actions = Argument(args, "--actions", "");
            string[] steps = actions.Length > 0 ? actions.Split(',') : Array.Empty<string>();
            for (int index = 0; index < steps.Length; index++)
            {
                Press(steps[index]);
                await Frames(45);
                Capture($"hub-{index + 1:00}-{steps[index]}");
            }
            int record = int.Parse(Argument(args, "--record", "0"), CultureInfo.InvariantCulture);
            if (record > 0)
                await Record(record);
            await Vestiges.Core.GameExit.QuitAsync(GetTree());
        }
        catch (Exception exception)
        {
            GD.PushError($"[HubCapture] {exception}");
            GetTree().Quit(1);
        }
    }

    /// <summary>Une vignette par frame : ce que le joueur voit pendant la transition et le chargement.</summary>
    private async Task Record(int frames)
    {
        ulong start = Time.GetTicksMsec();
        for (int frame = 0; frame < frames; frame++)
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using Image image = GetViewport().GetTexture().GetImage();
            image.Resize(480, 270, Image.Interpolation.Bilinear);
            image.SavePng($"{_output}/rec-{frame:0000}.png");
            string scene = GetTree().CurrentScene?.Name ?? "-";
            GD.Print($"[HubCapture] frame {frame} t={Time.GetTicksMsec() - start} ms scene={scene}");
        }
    }

    private static void Press(string action)
    {
        InputEventAction down = new() { Action = action, Pressed = true };
        Input.ParseInputEvent(down);
        InputEventAction up = new() { Action = action, Pressed = false };
        Input.ParseInputEvent(up);
    }

    private void Capture(string name)
    {
        using Image image = GetViewport().GetTexture().GetImage();
        string path = $"{_output}/{name}.png";
        image.SavePng(path);
        GD.Print($"[HubCapture] {path} pixels={image.GetSize()} fenêtre={GetWindow().Size}");
    }

    private async Task Frames(int count)
    {
        for (int frame = 0; frame < count; frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static string Argument(string[] args, string name, string fallback)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
    }
}

/// <summary>Faux classement de capture : dix noms en tête, puis la place du joueur et ses voisins.</summary>
internal sealed class DemoLeaderboardBackend : Vestiges.Infrastructure.Steam.ILeaderboardBackend
{
    public const ulong LocalId = 7_000;
    private static readonly string[] Names =
    {
        "Ombre_du_Phare", "marie.l", "Kestrel", "le_facteur_fou", "Nox", "Brume", "paul_r", "Sentinelle", "Atlas", "Ilsa",
    };

    public void Find(string board, System.Action<bool, ulong> done) => done(true, 1);

    public void Upload(ulong handle, int score, System.Action<bool, bool, int> done) => done(true, true, 43);

    public void Download(ulong handle, Vestiges.Infrastructure.Steam.LeaderboardQueue.EntryRange range, int count,
        System.Action<bool, System.Collections.Generic.IReadOnlyList<Vestiges.Infrastructure.Steam.LeaderboardQueue.Entry>> done)
    {
        System.Collections.Generic.List<Vestiges.Infrastructure.Steam.LeaderboardQueue.Entry> entries = new();
        bool around = range == Vestiges.Infrastructure.Steam.LeaderboardQueue.EntryRange.AroundUser;
        for (int i = 0; i < count; i++)
        {
            int rank = around ? 41 + i : i + 1;
            string name = around ? (rank == 43 ? "raph_plt" : $"joueur{rank}") : Names[i % Names.Length];
            entries.Add(new(rank, name, 48_000 - rank * 900, around && rank == 43 ? LocalId : (ulong)rank));
        }
        done(true, entries);
    }

    public void Cancel()
    {
    }
}

