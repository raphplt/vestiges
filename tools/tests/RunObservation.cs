using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Combat.Abilities;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Spawn;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Observation de la vraie scène de run, rendue.
/// --capture-abilities [--enemies a,b] [--shots n] [--still] : captures des attaques d'ennemis choisis (Présage et Charognard par défaut) ;
///     --still garde le joueur immobile, pour les attaques qui visent sa position (surgissement).
/// --capture-map : répartition des biomes autour du spawn (plusieurs seeds) et vues dézoomées.
/// --capture-cartography : parcours fixe, radar/carte entière, phases et bord du monde (RunObservation.Cartography.cs).
/// --capture-character --character ID : personnage en marche, dash, dégât et mort dans Main.
/// --capture-props : zone la plus chargée en décors de chaque biome, collisions affichées (sauf --hide-collisions).
/// --capture-junctions : frontières entre biomes les plus proches du départ, avec et sans décors.
/// --capture-paths : chemins de terre par biome, raccord à une rue, vue dézoomée du départ (RunObservation.Paths.cs).
/// --capture-farms : fermes des Champs Sauvages les plus proches du départ, normal et dézoomé (RunObservation.Farms.cs).
/// --capture-quarries : chantiers de la Carrière Effondrée les plus proches du départ, normal et dézoomé (idem).
/// --capture-omen : présage d'une Résurgence, avant, pendant l'avertissement et pendant la crise (RunObservation.Omen.cs).
/// --check-connectivity : coffres, Mémoriaux et Failles accessibles à pied depuis le départ (RunObservation.Connectivity.cs).
/// --capture-trample : herbes qui plient au passage du joueur (RunObservation.Trample.cs).
/// --show-collisions : formes de collision affichées dans n'importe quel mode de capture.
/// --capture-landmarks : églises et pylônes des Ruines Urbaines, normal et dézoomé (RunObservation.UrbanLandmarks.cs).
/// --capture-levelup-fx : effet de montée de niveau au ralenti, puis entrée de l'écran de choix (RunObservation.LevelUpFx.cs).
/// --capture-crowd : foule de 60 créatures, recul de caméra puis compteur de morts en rafale (RunObservation.Crowd.cs).
/// --capture-micro : coffre qui frémit à l'approche, poussière de pas (RunObservation.Micro.cs).
/// --capture-death [--seconds 8] : mort réelle après quelques secondes, bilan de fin de run (RunObservation.Death.cs).
/// --capture-echoes : échos de l'oubli forcés en zone Fragile, apparition, dissolution, murmure (RunObservation.Echoes.cs).
/// --capture-erasure : une capture par phase de l'oubli, puis un dégradé de toutes les phases.
/// --measure-props [--measure-seconds 8] : coût de rendu des décors par biome (RunObservation.PropCost.cs).
/// --capture-weapons [--weapons a,b] [--lethal] [--objects id:niveau,…] : galerie des attaques du joueur, cibles qui meurent au premier coup avec --lethal, objets portés avec --objects (RunObservation.Weapons.cs).
/// --capture-held [--weapons a,b] : arme en main dans les huit directions et pendant un coup (RunObservation.HeldWeapon.cs).
/// --capture-chests : chaque coffre cadré, avec et sans décors (RunObservation.Chests.cs).
/// --capture-loot : écran de butin du coffre le plus proche, bonus de stat compris (RunObservation.Chests.cs).
/// --capture-places : un petit lieu de chaque type, joueur à côté (signe), puis juste après usage (RunObservation.Places.cs).
/// --capture-levelup : l'écran de level-up, une capture par rareté (RunObservation.LevelUp.cs).
/// --capture-cascade : file de niveaux, cinq niveaux enchaînés puis un niveau ouvert aussitôt (RunObservation.Cascade.cs).
/// --capture-perks [--perk-scene survival|overflow|priority|carry] : effets des perks en run (RunObservation.Perks.cs).
/// --capture-oublis : les neuf Oublis de carte pris d'un coup, effets mesurés (RunObservation.Oublis.cs).
/// --check-orb-sleep : orbe d'XP endormie loin du joueur, réveillée et ramassée à son retour (RunObservation.OrbSleep.cs).
/// --capture-endgame : Indicible forcé, combat, mort et passage en endgame (RunObservation.Endgame.cs).
/// --capture-weapon-pickup : arme au sol ramassée (vol vers le HUD) puis échangée (RunObservation.WeaponPickup.cs).
/// --close-window : quitte par la demande de fermeture de la fenêtre au lieu de GameExit.
/// --capture-rift : offre d'une Faille, Péril et Oubli dans la pause, Oubli levé au Mémorial (RunObservation.Landmarks.cs).
/// --capture-memorial : parcours complet d'un Mémorial, du réveil aux services (RunObservation.Landmarks.cs).
/// --capture-workshop : Atelier, première visite (Trempe), niveau d'arme et Retrempe (RunObservation.Landmarks.cs).
/// --loot-draws N : tirages de butin de chaque coffre, sans les appliquer (RunObservation.Chests.cs).
/// --capture-bestiary : gros plans des créatures du pilote de sprites procéduraux, autour du joueur immobile.
/// --density : mesure de densité en spawn naturel (ennemis visibles, temps sans ennemi, débits, niveaux,
/// coffres entrés dans le cadre, et parmi eux ceux qu'aucun décor ne masquait ; lieux croisés et visités par minute,
/// micro-événements, Essence gagnée et dépensée).
/// --mortal : mesure de survie, bot non invincible ; un coup fatal le remet à fond et se compte (RunObservation.Balance.cs).
/// --prefer id,id,… : pendant la mesure, le bot prend d'abord une carte de ces armes ou objets (plan 21 G6d : build XP/Chance).
/// --scaling cle=valeur,… : surcharge des réglages d'apparition (spawn_flow.json) pendant la mesure de densité.
/// --measure-erasure : cellules suivies/actives et événements de stabilisation pendant la run.
/// --measure-projectiles [--projectile-lifetime secondes] : pression des tirs à 10 Hz ; durée surchargée dans le banc seul.
/// --howler-cooldown multiplicateur : surcharge de la cadence du Hurleur pour comparaison sur le même build.
/// --choice-delay S : pendant la mesure, le bot attend S secondes avant de choisir sur un écran qui fige la run.
/// --music-config chemin : pendant la mesure, réglages de musique lus dans ce fichier (variantes d'écoute).
/// --crisis-at S : pendant la mesure, première Résurgence à S secondes de jeu.
/// --mute-buses A,B : pendant la mesure, bus audio coupés (Music, SFX, Ambiance), pour écouter le reste seul.
/// --audio-trace : pendant la mesure, trace d'écoute (signaux, musique, sons) pour tools/record_run_audio.sh (AudioTraceProbe).
/// --nomad : pendant la mesure, le bot garde un cap (tiré de la seed) au lieu d'errer autour du départ,
/// et en change quand il bute sur le bord du monde.
/// --visit : pendant la mesure, le bot ratisse : il se détourne vers les lieux vus, ouvre, ravive et dépense
/// (RunObservation.Places.cs). Sans lui, les lieux « visités » ne sont que ceux que le trajet traverse.
/// --peril N : pendant la mesure, la run commence avec N points de Péril (plan 17 lot 3A).
/// --capture-every N : pendant la mesure, capture plein écran toutes les N secondes (HUD, événements).
/// --event ID : force le micro-événement ID à 3 s de run (bancs de capture).
/// --hold-map : pendant la mesure, maintient la touche de la carte entière (captures de la carte, plan 24 A6).
/// --show-bonuses : pendant la mesure, pose les cinq bonus lâchés autour du joueur à 4 s (captures, plan 24 C4).
/// </summary>
public partial class RunObservation : Node
{
    private const ulong Seed = 221092026;

    private WorldSetup _world;
    private Player _player;
    private Camera2D _camera;
    private string _output;
    private double _captureEvery;
    private string _forcedEvent;

    public override async void _Ready()
    {
        try
        {
            // La racine termine ses _Ready avant de recevoir la scène de run.
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            string[] args = OS.GetCmdlineUserArgs();
            GD.Print($"[RunObservation] display={DisplayServer.GetName()} screen={DisplayServer.WindowGetCurrentScreen()} position={DisplayServer.WindowGetPosition()} size={DisplayServer.WindowGetSize()}");
            _output = Argument(args, "--output", "/tmp/vestiges-observation");
            // Taille du texte des paramètres (0 normal, 1 grand, 2 très grand), pour vérifier les débordements.
            Vestiges.UI.TextSettings.Step = int.Parse(Argument(args, "--text-step", "0"), CultureInfo.InvariantCulture);
            DirAccess.MakeDirRecursiveAbsolute(_output);
            ulong seed = ulong.Parse(Argument(args, "--seed", Seed.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
            bool captureMap = Array.IndexOf(args, "--capture-map") >= 0;
            bool captureProps = Array.IndexOf(args, "--capture-props") >= 0;
            if ((captureProps && Array.IndexOf(args, "--hide-collisions") < 0) || Array.IndexOf(args, "--show-collisions") >= 0)
                GetTree().DebugCollisionsHint = true;
            // Acquisition des perks sans effets branchés (plan 05, B1) : pour voir les cartes, jamais en run normale.
            Vestiges.Progression.PerkSpecializationEffects.PreviewInactive = Array.IndexOf(args, "--preview-perks") >= 0;
            if (captureMap)
                MeasureBiomeLayout(seed, int.Parse(Argument(args, "--map-seeds", "40"), CultureInfo.InvariantCulture), 4);
            await LoadRun(seed, Argument(args, "--character", "traqueur"));

            if (captureMap)
                await CaptureWorldOverview();
            else if (Array.IndexOf(args, "--capture-cartography") >= 0)
                await CaptureCartography();
            else if (captureProps)
                await CapturePropHotspots();
            else if (Array.IndexOf(args, "--capture-junctions") >= 0)
                await CaptureJunctions();
            else if (Array.IndexOf(args, "--check-connectivity") >= 0)
                await CheckConnectivity();
            else if (Array.IndexOf(args, "--capture-trample") >= 0)
                await CaptureTrample();
            else if (Array.IndexOf(args, "--capture-omen") >= 0)
                await CaptureOmen();
            else if (Array.IndexOf(args, "--capture-quarries") >= 0)
                await CaptureQuarrySites();
            else if (Array.IndexOf(args, "--capture-farms") >= 0)
                await CaptureFarms();
            else if (Array.IndexOf(args, "--capture-landmarks") >= 0)
                await CaptureLandmarks();
            else if (Array.IndexOf(args, "--capture-levelup-fx") >= 0)
                await CaptureLevelUpFx();
            else if (Array.IndexOf(args, "--capture-crowd") >= 0)
                await CaptureCrowd();
            else if (Array.IndexOf(args, "--capture-micro") >= 0)
                await CaptureMicro();
            else if (Array.IndexOf(args, "--capture-death") >= 0)
                await CaptureDeath(double.Parse(Argument(args, "--seconds", "8"), CultureInfo.InvariantCulture));
            else if (Array.IndexOf(args, "--capture-echoes") >= 0)
                await CaptureEchoes();
            else if (Array.IndexOf(args, "--capture-paths") >= 0)
                await CapturePaths();
            else if (Array.IndexOf(args, "--capture-erasure") >= 0)
                await CaptureErasure();
            else if (Array.IndexOf(args, "--capture-abilities") >= 0)
                await CaptureAbilities();
            else if (Array.IndexOf(args, "--capture-hud-art") >= 0)
                await CaptureHudArt();
            else if (Array.IndexOf(args, "--capture-pickup-art") >= 0)
                await CapturePickupArt();
            else if (Array.IndexOf(args, "--measure-props") >= 0)
                await MeasurePropCost(double.Parse(Argument(args, "--measure-seconds", "8"), CultureInfo.InvariantCulture));
            else if (Array.IndexOf(args, "--capture-weapons") >= 0)
                await CaptureWeapons(Argument(args, "--weapons", null), Array.IndexOf(args, "--lethal") >= 0);
            else if (Array.IndexOf(args, "--capture-places") >= 0)
                await CapturePlaces();
            else if (Array.IndexOf(args, "--capture-chests") >= 0)
                await CaptureChests();
            else if (Array.IndexOf(args, "--capture-loot") >= 0)
                await CaptureLootScreen();
            else if (Array.IndexOf(args, "--capture-cascade") >= 0)
                await CaptureCascade();
            else if (Array.IndexOf(args, "--capture-perks") >= 0)
                await CapturePerks(Argument(args, "--perk-scene", "survival"));
            else if (Array.IndexOf(args, "--capture-integrated-art") >= 0)
                await CaptureIntegratedArt();
            else if (Array.IndexOf(args, "--capture-levelup") >= 0)
                await CaptureLevelUp();
            else if (Array.IndexOf(args, "--capture-pause") >= 0)
                await CapturePause();
            else if (Array.IndexOf(args, "--capture-choices") >= 0)
                await CaptureChoiceReopen();
            else if (Array.IndexOf(args, "--capture-memorial") >= 0)
                await CaptureMemorial();
            else if (Array.IndexOf(args, "--capture-workshop") >= 0)
                await CaptureWorkshop();
            else if (Array.IndexOf(args, "--capture-rift") >= 0)
                await CaptureRift();
            else if (Array.IndexOf(args, "--capture-oublis") >= 0)
                await CaptureOublis();
            else if (Array.IndexOf(args, "--check-orb-sleep") >= 0)
                await CheckOrbSleep();
            else if (Array.IndexOf(args, "--capture-weapon-pickup") >= 0)
                await CaptureWeaponPickup();
            else if (Array.IndexOf(args, "--capture-endgame") >= 0)
                await CaptureEndgame(double.Parse(Argument(args, "--seconds", "40"), CultureInfo.InvariantCulture));
            else if (Array.IndexOf(args, "--loot-draws") >= 0)
                MeasureLootDraws(int.Parse(Argument(args, "--loot-draws", "1000"), CultureInfo.InvariantCulture));
            else if (Array.IndexOf(args, "--capture-bestiary") >= 0)
                await CaptureBestiary();
            else if (Array.IndexOf(args, "--capture-held") >= 0)
                await CaptureHeldWeapons(Argument(args, "--weapons", null));
            else if (Array.IndexOf(args, "--capture-character") >= 0)
                await CaptureCharacter(Argument(args, "--character", "traqueur"));
            else
            {
                _forcedEvent = Argument(args, "--event", null);
                _captureEvery = double.Parse(Argument(args, "--capture-every", "0"), CultureInfo.InvariantCulture);
                await MeasureDensity(double.Parse(Argument(args, "--seconds", "180"), CultureInfo.InvariantCulture), seed);
            }

            // --close-window : sortie par la demande de fermeture de la fenêtre, comme le joueur (sortie propre de GameManager).
            if (Array.IndexOf(args, "--close-window") >= 0)
                GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
            else
                await GameExit.QuitAsync(GetTree());
        }
        catch (Exception exception)
        {
            GD.PushError($"[RunObservation] {exception}");
            GetTree().Quit(1);
        }
    }

    private async Task LoadRun(ulong seed, string characterId)
    {
        GD.Seed(seed);
        string howlerCooldown = Argument(OS.GetCmdlineUserArgs(), "--howler-cooldown", null);
        if (howlerCooldown != null)
        {
            float multiplier = float.Parse(howlerCooldown, CultureInfo.InvariantCulture);
            if (!float.IsFinite(multiplier) || multiplier <= 0f)
                throw new ArgumentOutOfRangeException(nameof(howlerCooldown));
            EnemyDataLoader.Get("hurleur").Abilities["aimed_shot"].Numbers["cooldown_multiplier"] = multiplier;
        }
        GameManager manager = GetNode<GameManager>("/root/GameManager");
        manager.RunSeed = seed;
        manager.SelectedCharacterId = characterId;
        GetTree().CurrentScene = null;
        _world = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<WorldSetup>();
        GetTree().Root.AddChild(_world);
        GetTree().CurrentScene = _world;
        ulong timeout = Time.GetTicksMsec() + 120000;
        bool captureLoading = Array.IndexOf(OS.GetCmdlineUserArgs(), "--capture-loading") >= 0;
        ulong nextLoadingFrame = 0;
        int loadingFrame = 0;
        while (!_world.IsWorldReady || GetTree().Paused || manager.CurrentState != GameManager.GameState.Run)
        {
            if (Time.GetTicksMsec() > timeout)
                throw new InvalidOperationException("Initialisation Main supérieure à 120 s.");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (captureLoading && Time.GetTicksMsec() >= nextLoadingFrame)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                SaveFrame($"loading-{loadingFrame++:00}");
                nextLoadingFrame = Time.GetTicksMsec() + 250;
            }
        }
        if (_world.FindChild("ShaderWarmup", true, false) != null)
            throw new InvalidOperationException("Viewport de préchauffage encore présent après le chargement.");
        _player = _world.GetNode<Player>("Player");
        _camera = _player.GetNode<Camera2D>("Camera");
        _player.IsGodMode = true;
        _player.IsAIControlled = true;
    }

    private async Task CaptureAbilities()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Enemy existing && existing.IsActive)
                _world.GetNode<EnemyPool>("EnemyPool").Return(existing);
        // Attendre le fondu réel : 90 frames peuvent ne durer qu'une demi-seconde sur une machine rapide.
        while (_world.GetNodeOrNull("GameLoadingOverlay") != null)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

        SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
        Vector2 origin = _player.GlobalPosition;
        string[] casters = Argument(OS.GetCmdlineUserArgs(), "--enemies", "presage,charognard").Split(',');
        for (int index = 0; index < casters.Length; index++)
            spawner.ForceSpawnEnemy(casters[index], origin + Vector2.FromAngle(Mathf.Pi * (0.9f + 0.45f * index)) * (130f + 40f * (index % 2)));
        await Frames(2);

        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not Enemy enemy || !enemy.IsActive)
                continue;
            // PV renforcés pour observer l'annonce complète malgré l'arme automatique.
            typeof(Enemy).GetField("_currentHp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(enemy, 100000f);
            ResetAbilityCooldowns(enemy);
        }

        bool still = Array.IndexOf(OS.GetCmdlineUserArgs(), "--still") >= 0;
        _player.AIInputOverride = still ? Vector2.Zero : new Vector2(0.5f, 0f);
        int shots = int.Parse(Argument(OS.GetCmdlineUserArgs(), "--shots", "12"), CultureInfo.InvariantCulture);
        int shotFrames = Math.Max(1, int.Parse(Argument(OS.GetCmdlineUserArgs(), "--shot-frames", "15"), CultureInfo.InvariantCulture));
        for (int shot = 0; shot < shots; shot++)
        {
            await Frames(shotFrames);
            using Image image = GetViewport().GetTexture().GetImage();
            string path = $"{_output}/abilities-{shot:00}.png";
            image.SavePng(path);
        }
        GD.Print($"[RunObservation] RESULT abilities captures={shots} output={_output}");
    }

    private async Task CaptureBestiary()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Enemy existing && existing.IsActive)
                _world.GetNode<EnemyPool>("EnemyPool").Return(existing);
        // L'écran de chargement s'efface après l'initialisation du monde.
        await Frames(90);

        SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
        Vector2 origin = _player.GlobalPosition;
        // --enemies a,b,c : créatures à montrer autour du joueur (le pilote par défaut).
        string[] ids = Argument(OS.GetCmdlineUserArgs(), "--enemies", "presage,rodeur,charognard").Split(',');
        Vector2[] spots = { new(-110f, -30f), new(100f, -40f), new(60f, 80f), new(-80f, 70f), new(0f, -90f), new(130f, 40f) };
        for (int i = 0; i < ids.Length && i < spots.Length; i++)
            spawner.ForceSpawnEnemy(ids[i], origin + spots[i]);
        await Frames(2);
        // --affix id : chaque créature montrée porte cet affixe (anneau au sol).
        string affixId = Argument(OS.GetCmdlineUserArgs(), "--affix", "");
        EnemyAffixData affix = affixId.Length > 0 ? EnemyVariantDataLoader.GetAffix(affixId) : null;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not Enemy enemy || !enemy.IsActive)
                continue;
            if (affix != null)
                enemy.ApplyAffix(affix);
            typeof(Enemy).GetField("_currentHp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(enemy, 100000f);
        }

        _player.AIInputOverride = Vector2.Zero;
        Vector2 half = new(200f, 120f);
        // --kill : les créatures montrées meurent après la quatrième vue (mort, explosion d'un affixe instable).
        bool kill = Array.IndexOf(OS.GetCmdlineUserArgs(), "--kill") >= 0;
        for (int shot = 0; shot < 16; shot++)
        {
            if (kill && shot == 4)
            {
                foreach (Node node in GetTree().GetNodesInGroup("enemies"))
                    if (node is Enemy { IsActive: true, IsDying: false } victim)
                        victim.TakeDamage(1000000f, false);
            }
            await Frames(shot >= 4 && kill ? 4 : 12);
            using Image image = GetViewport().GetTexture().GetImage();
            // Gros plan sur le joueur, en pixels physiques de la capture (écrans à haute densité compris).
            float pixelRatio = image.GetWidth() / GetViewport().GetVisibleRect().Size.X;
            Vector2 center = GetViewport().GetCanvasTransform() * _player.GlobalPosition;
            Vector2 zoom = _camera.Zoom;
            Vector2 size = half * 2f * zoom * pixelRatio;
            Vector2 corner = (center - half * zoom) * pixelRatio;
            Rect2I region = new Rect2I((Vector2I)corner, (Vector2I)size).Intersection(new Rect2I(0, 0, image.GetWidth(), image.GetHeight()));
            using Image crop = image.GetRegion(region);
            crop.SavePng($"{_output}/bestiary-{shot:00}.png");
        }
        GD.Print($"[RunObservation] Captures du bestiaire écrites dans {_output}");
    }

    private async Task CaptureCharacter(string characterId)
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        _player.AIInputOverride = Vector2.Zero;
        // Attendre la disparition réelle du chargement : un nombre de frames dépend du débit de rendu.
        while (_world.GetNodeOrNull("GameLoadingOverlay") != null)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
        // Repères d'échelle : un petit et un grand ennemi à distance du joueur.
        spawner.ForceSpawnEnemy("shade", _player.GlobalPosition + new Vector2(-150f, 60f));
        spawner.ForceSpawnEnemy("rodeur", _player.GlobalPosition + new Vector2(160f, -50f));
        Vector2[] directions = { Vector2.Zero, Vector2.Down, new(1, 1), Vector2.Right, new(1, -1), Vector2.Up, Vector2.Left, new(-1, 1), new(-1, -1) };
        for (int index = 0; index < directions.Length; index++)
        {
            _player.AIInputOverride = directions[index].Normalized();
            await Seconds(0.35);
            SaveFrame($"{characterId}-{index:00}");
            SaveCrop($"{characterId}-{index:00}-detail", _player.GlobalPosition, new Vector2(42, 44));
        }
        _player.AIInputOverride = Vector2.Down;
        _player.Mobility.Request();
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        SaveFrame($"{characterId}-dash");
        GD.Print($"[RunObservation] character dash animation={_player.GetNode<AnimatedSprite2D>("Sprite").Animation}");
        await Seconds(0.8);
        _player.IsGodMode = false;
        _player.TakeErasureDamage(1f);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        SaveFrame($"{characterId}-hurt");
        GD.Print($"[RunObservation] character hurt animation={_player.GetNode<AnimatedSprite2D>("Sprite").Animation}");
        await Seconds(0.8);
        _player.TakeErasureDamage(100000f);
        await Seconds(0.15);
        SaveFrame($"{characterId}-death");
        GD.Print($"[RunObservation] character death animation={_player.GetNode<AnimatedSprite2D>("Sprite").Animation}");
        GD.Print($"[RunObservation] RESULT character={characterId} output={_output}");
    }

    private async Task MeasureDensity(double seconds, ulong seed)
    {
        string[] args = OS.GetCmdlineUserArgs();
        _preferredCards = Argument(args, "--prefer", "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        StartBalanceProbes(args);
        string lifetime = Argument(args, "--projectile-lifetime", null);
        using ProjectilePressureProbe projectiles = Array.IndexOf(args, "--measure-projectiles") >= 0
            ? new ProjectilePressureProbe(lifetime == null ? null : float.Parse(lifetime, CultureInfo.InvariantCulture))
            : null;
        using AudioTraceProbe audioTrace = Array.IndexOf(args, "--audio-trace") >= 0 ? new AudioTraceProbe(_output, _world, _player) : null;
        // --music-config chemin : variante de music.json à écouter, sans toucher aux données du jeu.
        string musicConfig = Argument(args, "--music-config", null);
        if (musicConfig != null)
            AudioManager.Instance.GetNode<MusicDirector>("Music").Configure(MusicConfig.Load(musicConfig));
        // --crisis-at S : première Résurgence à S secondes de jeu (annonce 20 s avant), pour écouter un cycle court.
        string crisisAt = Argument(args, "--crisis-at", null);
        if (crisisAt != null)
            typeof(Vestiges.Events.CrisisManager).GetMethod("ScheduleNextCrisis", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(_world.GetNode("CrisisManager"), new object[] { float.Parse(crisisAt, CultureInfo.InvariantCulture) });
        // --mute-buses Music,SFX,Ambiance : passe d'écoute d'une seule famille de sons (plan 15 A0).
        foreach (string bus in Argument(args, "--mute-buses", "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            AudioServer.SetBusMute(AudioServer.GetBusIndex(bus), true);
        RunTracker tracker = _world.GetNode<RunTracker>("RunTracker");
        int peril = int.Parse(Argument(OS.GetCmdlineUserArgs(), "--peril", "0"), CultureInfo.InvariantCulture);
        if (peril > 0)
            _world.GetNode<Vestiges.Progression.PerilManager>("PerilManager").AddPeril(peril);
        // --scaling cle=valeur,… : surcharge des réglages d'apparition pour un avant/après sur le même build, sans
        // toucher aux fichiers de data/ (plan 20, R1-F et R1-A).
        string scaling = Argument(OS.GetCmdlineUserArgs(), "--scaling", "");
        if (scaling.Length > 0)
        {
            Dictionary<string, float> overrides = new();
            foreach (string pair in scaling.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = pair.Split('=');
                overrides[parts[0]] = float.Parse(parts[1], CultureInfo.InvariantCulture);
            }
            _world.GetNode<SpawnManager>("SpawnManager").ApplyScalingOverrides(overrides);
            GD.Print($"[RunObservation] Réglages surchargés : {scaling}");
        }
        List<string> rows = new() { "t,visible,near600,alive,spawned,killed,level,hit_damage,memory,erasure_global,xp_gained,xp_orbs,spawned_hp,damage_dealt,essence_gained" };
        // Mémoire de la zone sous le joueur et Effacement global, pour mesurer le tempo de l'oubli (retour du 28 septembre).
        ErasureManager erasure = _world.GetNode<ErasureManager>("ErasureManager");
        // Indice de pression : dégâts que les ennemis infligent à un joueur qui n'esquive jamais (invincible ici).
        double hitDamage = 0;
        EventBus eventBus = GetNode<EventBus>("/root/EventBus");
        using ErasureRunProbe erasureProbe = Array.IndexOf(args, "--measure-erasure") >= 0
            ? new ErasureRunProbe(_output, erasure, _player, eventBus) : null;
        // Souffle du tout début de run (retour du 26 septembre) : premier coup reçu et dégâts cumulés à 10 et 30 s.
        double firstHit = -1, damage10 = 0, damage30 = 0;
        // Plan 24 L3 : dégâts reçus avant et pendant la première Résurgence (4:00 à 5:10), armes portées à 4:00.
        double damageBefore240 = 0, damageResurgence = 0;
        int weaponsAt240 = -1, levelAt240 = -1;
        // Temps de jeu, pauses exclues : la mesure vaut la même chose en temps réel et en headless
        // accéléré (--fixed-fps), où une seconde de jeu dure bien moins qu'une seconde d'horloge.
        double gameTime = 0;
        int pausedFrames = 0;
        // Pas d'image cumulés, pas l'horloge réelle : en headless accéléré, 3 000 images passent en moins de 2 s.
        double pausedTime = 0.0;
        double choiceDelay = double.Parse(Argument(args, "--choice-delay", "0"), CultureInfo.InvariantCulture);
        // Part de chaque créature dans la pression subie, et XP réellement ramassée (retour du 28 septembre, plan 20).
        Dictionary<string, double> damageBySource = new();
        // Le bot est invincible et compte chaque coup ; « gated » ne garde que les coups qui passeraient
        // l'invulnérabilité après un coup (plan 03 lot 8B), pour comparer tir et mêlée à armes égales.
        Dictionary<string, double> gatedBySource = new();
        float hurtInvulnerability = DefenseConfig.Load().HurtInvulnerabilitySeconds;
        double lastGatedHit = double.NegativeInfinity;
        // Exposition : secondes × créatures à moins de 600 px, par espèce ; et morts par espèce.
        Dictionary<string, double> exposureById = new();
        Dictionary<string, double> killsById = new();
        EventBus.EnemyKilledEventHandler onKill = (enemyId, _) => killsById[enemyId] = killsById.GetValueOrDefault(enemyId) + 1;
        eventBus.EnemyKilled += onKill;
        // Temps pour tuer (plan 20, R1-T) : PV des créatures apparues et dégâts infligés, cumulés ; le modèle en
        // déduit, par palier, le PV moyen d'une créature divisé par les dégâts infligés par seconde.
        double spawnedHp = 0, damageDealt = 0;
        Dictionary<string, double> spawnedById = new();
        EventBus.EnemySpawnedEventHandler onSpawned = (enemyId, hpScale, _) =>
        {
            spawnedHp += (EnemyDataLoader.Get(enemyId)?.Stats.Hp ?? 0f) * hpScale;
            spawnedById[enemyId] = spawnedById.GetValueOrDefault(enemyId) + 1;
        };
        EventBus.EntityDamagedEventHandler onDamaged = (_, amount) => damageDealt += amount;
        eventBus.EnemySpawned += onSpawned;
        eventBus.EntityDamaged += onDamaged;
        double xpGained = 0;
        EventBus.XpGainedEventHandler onXp = amount => xpGained += amount;
        eventBus.XpGained += onXp;
        EventBus.PlayerHitByEventHandler onHit = (source, damage) =>
        {
            hitDamage += damage;
            damageBySource[source] = damageBySource.GetValueOrDefault(source) + damage;
            if (gameTime - lastGatedHit >= hurtInvulnerability)
            {
                lastGatedHit = gameTime;
                gatedBySource[source] = gatedBySource.GetValueOrDefault(source) + damage;
            }
            double at = gameTime;
            if (firstHit < 0)
                firstHit = at;
            if (at <= 10)
                damage10 += damage;
            if (at <= 30)
                damage30 += damage;
            if (at < 240)
                damageBefore240 += damage;
            else if (at < 310)
                damageResurgence += damage;
        };
        eventBus.PlayerHitBy += onHit;
        List<int> visibleSamples = new();
        Dictionary<int, double> levelTimes = new();
        int lastLevel = 1;
        double firstVisible = -1;
        int maxOrbs = 0;
        RandomNumberGenerator rng = new() { Seed = seed };
        Vector2 waypoint = _player.GlobalPosition;
        double nextSample = 1.0;
        double nextCapture = _captureEvery;
        // Coffres entrés dans le cadre (plan 17 lot 0A) : échantillonnés quatre fois par seconde,
        // un coffre traverse l'écran en plus de deux secondes à la vitesse du joueur.
        HashSet<ulong> seenChests = new();
        HashSet<ulong> clearChests = new();
        // Signalé : le coffre, le haut de sa colonne ou sa flèche de bord d'écran est visible.
        HashSet<ulong> signaledChests = new();
        float pointerRange = ChestDataLoader.LoadPlacement().PointerRangePx;
        double firstChestSeen = -1;
        double nextChestSample = 0.0;
        List<(Rect2 Rect, float SortY, bool Canopy)> propRects = CollectPropRects();
        bool nomad = Array.IndexOf(OS.GetCmdlineUserArgs(), "--nomad") >= 0;
        float heading = rng.RandfRange(0f, Mathf.Tau);
        Vestiges.Events.RunEventDirector director = _world.GetNode<Vestiges.Events.RunEventDirector>("RunEventDirector");
        PlayerProgressionAccessor progression = new(_player);
        ProcessMode = ProcessModeEnum.Always;
        Vector2 lastProgressPosition = _player.GlobalPosition;
        double lastProgressTime = 0;
        // Lieux croisés et visités, événements, Essence (plan 22, lot C0).
        PlaceTracker places = new(_world.GetNode<Vestiges.Progression.EssenceTracker>("EssenceTracker").CurrentEssence);
        _placeTracker = places;
        _visitPlaces = Array.IndexOf(OS.GetCmdlineUserArgs(), "--visit") >= 0;
        if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--hold-map") >= 0)
            Input.ActionPress("show_map");
        bool showBonuses = Array.IndexOf(OS.GetCmdlineUserArgs(), "--show-bonuses") >= 0;
        EventBus.EssenceChangedEventHandler onEssence = places.OnEssence;
        EventBus.RunEventStartedEventHandler onEvent = (_, _, _, _) => places.Events++;
        eventBus.EssenceChanged += onEssence;
        eventBus.RunEventStarted += onEvent;

        while (true)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            audioTrace?.Sample(gameTime);
            if (GetTree().Paused)
            {
                // L'écran de niveau fige la run : choisir la première offre ; ce temps ne compte pas.
                // --choice-delay : le bot lit l'écran comme un joueur, pour entendre son entrée en entier.
                pausedTime = pausedFrames == 0 ? 0.0 : pausedTime + GetProcessDeltaTime();
                if (pausedTime >= choiceDelay)
                    AutoPickLevelUp();
                if (++pausedFrames > 3000)
                    throw new InvalidOperationException($"Run en pause depuis 3 000 frames : écran bloquant non géré par le banc ({VisibleScreens()}).");
                continue;
            }
            pausedFrames = 0;
            gameTime += GetProcessDeltaTime();
            _balanceTime = gameTime;
            double t = gameTime;
            if (t >= seconds)
                break;
            projectiles?.Sample(t, VisibleWorldRect());
            erasureProbe?.Sample(t);

            // Déplacement nomade déterministe : nouveau point de passage à 500–900 px dès l'arrivée,
            // ou si le joueur est bloqué (obstacle, Néant) depuis deux secondes.
            if (t - lastProgressTime > 2.0)
            {
                waypoint = _player.GlobalPosition;
                lastProgressTime = t;
                if (nomad)
                    heading += rng.RandfRange(Mathf.Pi * 0.5f, Mathf.Pi);
            }
            if (_player.GlobalPosition.DistanceTo(lastProgressPosition) > 60f)
            {
                lastProgressPosition = _player.GlobalPosition;
                lastProgressTime = t;
            }
            if (_player.GlobalPosition.DistanceTo(waypoint) < 40f && !director.IsEventActive)
            {
                float angle = nomad ? heading + rng.RandfRange(-0.6f, 0.6f) : rng.RandfRange(0f, Mathf.Tau);
                waypoint = _player.GlobalPosition + Vector2.FromAngle(angle) * rng.RandfRange(500f, 900f);
            }
            // Comme un joueur, le bot suit la cible d'un micro-événement en cours (vestige, veille, Souverain).
            if (director.TryGetActiveTarget(out Vector2 eventTarget))
                waypoint = eventTarget;
            bool holding = _visitPlaces && !director.IsEventActive && places.Steer(t, _player, ref waypoint);
            // Tenir une interaction n'est pas un blocage : le cap et le point de passage restent ceux d'avant.
            if (holding)
                lastProgressTime = t;
            places.CheckReach(_player.GlobalPosition);
            _player.AIInputOverride = !holding && _player.GlobalPosition.DistanceTo(waypoint) > 12f
                ? (waypoint - _player.GlobalPosition).Normalized()
                : Vector2.Zero;

            int level = progression.Level;
            if (weaponsAt240 < 0 && t >= 240)
            {
                weaponsAt240 = _player.WeaponSlots.Count;
                levelAt240 = level;
            }
            if (level > lastLevel)
            {
                for (int l = lastLevel + 1; l <= level; l++)
                    levelTimes[l] = t;
                lastLevel = level;
            }

            if (showBonuses && t >= 4.0)
            {
                showBonuses = false;
                FieldBonusDirector bonuses = _world.GetNode<FieldBonusDirector>("FieldBonusDirector");
                FieldBonusConfig bonusConfig = FieldBonusDataLoader.Load();
                for (int i = 0; i < 4; i++)
                    bonuses.Spawn(bonusConfig.Bonuses[i], _player.GlobalPosition + new Vector2(-180f + i * 90f, -110f));
            }

            if (_forcedEvent != null && t >= 3.0)
            {
                _world.GetNode<Vestiges.Events.RunEventDirector>("RunEventDirector").ForceStart(_forcedEvent);
                _forcedEvent = null;
            }

            if (_captureEvery > 0 && t >= nextCapture)
            {
                nextCapture += _captureEvery;
                using Image frame = GetViewport().GetTexture().GetImage();
                frame.SavePng(string.Create(CultureInfo.InvariantCulture, $"{_output}/screen-{t:000}s.png"));
            }

            if (t >= nextChestSample)
            {
                nextChestSample += 0.25;
                Rect2 chestView = VisibleWorldRect();
                Godot.Collections.Array<Node> chestNodes = GetTree().GetNodesInGroup("chests");
                places.Sample(t, chestView, chestNodes, _world.GetNodeOrNull<SmallPlaceDirector>("SmallPlaceDirector"));
                foreach (Node node in chestNodes)
                {
                    if (node is not Chest chest)
                        continue;
                    bool columnVisible = chestView.HasPoint(chest.GlobalPosition + new Vector2(0f, -chest.ColumnHeight * 0.6f));
                    if (columnVisible || chest.GlobalPosition.DistanceTo(_camera.GetScreenCenterPosition()) <= pointerRange)
                        signaledChests.Add(chest.GetInstanceId());
                    if (!chestView.HasPoint(chest.GlobalPosition))
                        continue;
                    signaledChests.Add(chest.GetInstanceId());
                    if (seenChests.Add(chest.GetInstanceId()) && firstChestSeen < 0)
                        firstChestSeen = t;
                    if (!IsChestMasked(chest.GlobalPosition, propRects))
                        clearChests.Add(chest.GetInstanceId());
                }
            }

            if (t < nextSample)
                continue;
            nextSample += 1.0;

            Rect2 view = VisibleWorldRect();
            int visible = 0, near = 0, alive = 0;
            foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            {
                if (node is not Enemy enemy || !enemy.IsActive || enemy.IsDying)
                    continue;
                alive++;
                if (view.HasPoint(enemy.GlobalPosition))
                    visible++;
                if (enemy.GlobalPosition.DistanceTo(_player.GlobalPosition) <= 600f)
                {
                    near++;
                    exposureById[enemy.EnemyId] = exposureById.GetValueOrDefault(enemy.EnemyId) + 1;
                }
            }
            if (visible > 0 && firstVisible < 0)
                firstVisible = t;
            int orbs = CombatPools.Instance?.XpOrbsOnGround ?? 0;
            maxOrbs = Math.Max(maxOrbs, orbs);
            visibleSamples.Add(visible);
            rows.Add(string.Create(CultureInfo.InvariantCulture,
                $"{t:F0},{visible},{near},{alive},{tracker.TotalSpawned},{tracker.TotalKilled},{level},{hitDamage:F0},{erasure.GetMemoryAt(_player.GlobalPosition):F2},{erasure.GlobalErasurePercent:F2},{xpGained:F0},{orbs},{spawnedHp:F0},{damageDealt:F0},{places.EssenceGainedSoFar}"));
        }

        eventBus.PlayerHitBy -= onHit;
        eventBus.XpGained -= onXp;
        eventBus.EnemyKilled -= onKill;
        eventBus.EnemySpawned -= onSpawned;
        eventBus.EntityDamaged -= onDamaged;
        eventBus.EssenceChanged -= onEssence;
        eventBus.RunEventStarted -= onEvent;
        projectiles?.Save($"{_output}/projectiles-{seed}.csv");
        using (FileAccess csv = FileAccess.Open($"{_output}/density-{seed}.csv", FileAccess.ModeFlags.Write))
            csv.StoreString(string.Join("\n", rows) + "\n");

        StringBuilder summary = new();
        summary.Append(CultureInfo.InvariantCulture, $"seed={seed} seconds={seconds:F0} first_visible_s={firstVisible:F0}");
        summary.Append(CultureInfo.InvariantCulture, $" view={VisibleWorldRect().Size.X:F0}x{VisibleWorldRect().Size.Y:F0}");
        summary.Append(CultureInfo.InvariantCulture, $" first_hit_s={firstHit:F1} damage_10s={damage10:F0} damage_30s={damage30:F0}");
        summary.Append(CultureInfo.InvariantCulture,
            $" hit_per_min_0_240={damageBefore240 / 4.0:F0} hit_per_min_240_310={damageResurgence / (70.0 / 60.0):F0} weapons_240s={weaponsAt240} level_240s={levelAt240}");
        int openRifts = 0;
        foreach (Rift rift in Rift.All)
            if (rift.IsOpen)
                openRifts++;
        summary.Append(CultureInfo.InvariantCulture, $" rifts={Rift.All.Count} rifts_open={openRifts}");
        summary.Append(CultureInfo.InvariantCulture, $" peril={peril} kills={tracker.TotalKilled} spawned={tracker.TotalSpawned}");
        summary.Append(CultureInfo.InvariantCulture, $" xp_gained={xpGained:F0} xp_dropped={CombatPools.Instance?.XpDropped ?? 0:F0}");
        AppendBreakdown(summary, "hit_by", damageBySource);
        AppendBreakdown(summary, "hit_gated", gatedBySource);
        AppendBreakdown(summary, "near_by", exposureById);
        AppendBreakdown(summary, "kills_by", killsById);
        AppendBreakdown(summary, "spawned_by", spawnedById);
        summary.Append(CultureInfo.InvariantCulture, $" xp_orbs_end={CombatPools.Instance?.XpOrbsOnGround ?? 0} xp_orbs_max={maxOrbs}");
        summary.Append(CultureInfo.InvariantCulture,
            $" chests_total={GetTree().GetNodesInGroup("chests").Count} chests_seen={seenChests.Count} chests_clear={clearChests.Count} chests_signaled={signaledChests.Count} first_chest_s={firstChestSeen:F0}");
        foreach ((int from, int to) in new[] { (0, 60), (60, 120), (120, 180), (180, 300) })
        {
            if (from >= visibleSamples.Count)
                break;
            int end = Math.Min(to, visibleSamples.Count);
            double sum = 0;
            int empty = 0;
            for (int i = from; i < end; i++)
            {
                sum += visibleSamples[i];
                if (visibleSamples[i] == 0)
                    empty++;
            }
            summary.Append(CultureInfo.InvariantCulture,
                $" | {from}-{end}s visible_mean={sum / (end - from):F1} empty={100.0 * empty / (end - from):F0}%");
        }
        places.Append(summary, seconds);
        AppendBalance(summary);
        foreach (KeyValuePair<int, double> entry in levelTimes)
            summary.Append(CultureInfo.InvariantCulture, $" L{entry.Key}={entry.Value:F0}s");
        GD.Print($"[RunObservation] RESULT {summary}");
    }

    /// <summary>Silhouettes des décors (fixes) : un décor masque un coffre s'il est dessiné devant lui.</summary>
    private List<(Rect2 Rect, float SortY, bool Canopy)> CollectPropRects()
    {
        List<(Rect2, float, bool)> rects = new();
        Stack<Node> pending = new();
        pending.Push(_world.GetNode("PropContainer"));
        while (pending.Count > 0)
        {
            foreach (Node child in pending.Pop().GetChildren())
            {
                if (child is EnvironmentProp prop)
                    rects.Add((prop.VisibleWorldRect(), prop.GlobalPosition.Y, prop.HasCanopy));
                else
                    pending.Push(child);
            }
        }
        return rects;
    }

    /// <summary>Le haut du coffre est couvert par un décor trié devant lui, ou par une canopée.</summary>
    /// <summary>Ajoute « nom=clé:valeur,… » au résumé, trié par valeur décroissante.</summary>
    private static void AppendBreakdown(StringBuilder summary, string name, Dictionary<string, double> values)
    {
        List<KeyValuePair<string, double>> entries = new(values);
        entries.Sort((x, y) => y.Value.CompareTo(x.Value));
        summary.Append(CultureInfo.InvariantCulture, $" {name}=");
        foreach (KeyValuePair<string, double> entry in entries)
            summary.Append(CultureInfo.InvariantCulture, $"{entry.Key}:{entry.Value:F0},");
    }

    private static bool IsChestMasked(Vector2 chest, List<(Rect2 Rect, float SortY, bool Canopy)> props)
    {
        Vector2 top = chest + new Vector2(0f, -12f);
        foreach ((Rect2 rect, float sortY, bool canopy) in props)
        {
            if (rect.HasPoint(top) && (sortY > chest.Y || canopy))
                return true;
        }
        return false;
    }

    /// <summary>Écrans de la run qui tournent malgré la pause et sont visibles, pour nommer celui qui bloque le banc.</summary>
    private string VisibleScreens()
    {
        List<string> names = new();
        foreach (Node child in _world.GetChildren())
        {
            bool visible = child switch
            {
                CanvasItem item => item.Visible,
                CanvasLayer layer => layer.Visible,
                _ => false,
            };
            if (visible && child.ProcessMode == ProcessModeEnum.Always)
                names.Add(child.Name);
        }
        Node screen = _world.GetNode("LevelUpScreen");
        if (screen.GetType().GetField("_fragmentManager", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(screen)
            is Vestiges.Progression.FragmentManager fragments)
            names.Add($"choix actif={fragments.IsChoiceActive}, offres={fragments.PendingChoices.Count}");
        return names.Count > 0 ? string.Join(", ", names) : "aucun écran visible";
    }

    private void AutoPickLevelUp()
    {
        // Écran de choix (Mémorial, Faille) : le bot sort quand c'est permis, sinon prend la première carte.
        if (_world.GetNodeOrNull<Vestiges.UI.ChoiceScreen>("ChoiceScreen") is { IsOpen: true } choices)
        {
            // Passer l'entrée avant de consommer l'intention d'achat du bot.
            if (choices.IsEntering)
            {
                choices.Activate(choices.FirstEnabled());
                return;
            }
            // --visit : le bot achète le premier service qu'il peut payer, comme un joueur qui dépense son Essence.
            int pick = _visitPlaces && _placeTracker != null && _placeTracker.TakePurchase() ? choices.FirstEnabled() : int.MaxValue;
            choices.Activate(choices.CanCancel ? pick : 0);
            return;
        }
        Node screen = _world.GetNode("LevelUpScreen");
        object manager = screen.GetType().GetField("_fragmentManager", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(screen);
        if (manager is not Vestiges.Progression.FragmentManager fragments || !fragments.IsChoiceActive || fragments.PendingChoices.Count == 0)
            return;
        RecordOffer(fragments);
        Vestiges.Progression.FragmentOption choice = fragments.PendingChoices[0];
        foreach (Vestiges.Progression.FragmentOption option in fragments.PendingChoices)
            if (Array.IndexOf(_preferredCards, option.Id) >= 0)
            {
                choice = option;
                break;
            }
        screen.GetType().GetMethod("OnCardChosen", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(screen, new object[] { choice });
    }

    private string[] _preferredCards = Array.Empty<string>();

    private Rect2 VisibleWorldRect()
    {
        Vector2 size = GetViewport().GetVisibleRect().Size / _camera.Zoom;
        return new Rect2(_camera.GetScreenCenterPosition() - size / 2f, size);
    }

    private static void ResetAbilityCooldowns(Enemy enemy)
    {
        var cache = (Dictionary<string, IEnemyAbility>)typeof(Enemy)
            .GetField("_abilityCache", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enemy);
        foreach (IEnemyAbility ability in cache.Values)
        {
            ability.GetType().GetField("_cooldownTimer", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ability, 0f);
            // Enfouissement : la phase de surface se compte sur le minuteur de phase.
            if (ability is BurrowAbility)
                ability.GetType().GetField("_timer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ability, 0f);
        }
    }

    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static string Argument(string[] args, string name, string fallback)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
    }

    /// <summary>Lecture du niveau courant sans dépendre du chemin de nœud de la progression.</summary>
    private readonly struct PlayerProgressionAccessor
    {
        private readonly Vestiges.Progression.PlayerProgression _progression;

        public PlayerProgressionAccessor(Player player)
        {
            _progression = player.GetNode<Vestiges.Progression.PlayerProgression>("PlayerProgression");
        }

        public int Level => _progression.CurrentLevel;
    }
}
