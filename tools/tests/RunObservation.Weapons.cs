using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.Spawn;

namespace Vestiges.Tests;

/// <summary>
/// --capture-weapons [--weapons id1,id2] [--lethal] [--objects id:niveau,…] [--ascensions arme:voie,…] [--integer-gains N] [--dash] [--targets N] : galerie des
/// attaques du joueur. Chaque arme est équipée seule. Avec id1+id2, plusieurs armes sont équipées ensemble pour vérifier
/// leurs interactions. Avec --objects, les objets donnés sont portés au niveau voulu avant la galerie (projectiles en plus, paliers) ;
/// avec --ascensions, une arme de la galerie est montée au niveau 50 et prend la voie donnée ; avec --integer-gains,
/// chaque stat entière de l'arme (projectile, perforation, saut, orbe) gagne N fois +0,5 ; avec --dash, le joueur
/// dashe vers la droite au moment de l'attaque ; --targets fixe le nombre de cibles (5 par défaut, 1 pour voir une rafale).
/// Chaque configuration est déclenchée sur un cercle d'ennemis immobiles, et capturée en gros plan à plusieurs instants de l'attaque.
/// </summary>
public partial class RunObservation
{
    private static readonly int[] WeaponCaptureFrames = { 2, 5, 9, 14 };
    // Avec --lethal : les cibles meurent au premier coup (plan 02 J2), captures étalées sur la dissolution et le saut du butin.
    // Au ralenti (×0,25) : la dissolution dure 0,6 s de jeu, trop peu pour le rendu logiciel du conteneur.
    private static readonly int[] LethalCaptureFrames = { 2, 4, 7, 11, 16, 24 };

    private async Task CaptureWeapons(string weaponList, bool lethal)
    {
        string[] args = OS.GetCmdlineUserArgs();
        using AudioTraceProbe audioTrace = System.Array.IndexOf(args, "--audio-trace") >= 0
            ? new AudioTraceProbe(_output, _world, _player) : null;
        foreach (string bus in Argument(args, "--mute-buses", "").Split(',', System.StringSplitOptions.RemoveEmptyEntries))
        {
            int index = AudioServer.GetBusIndex(bus);
            if (index >= 0)
                AudioServer.SetBusMute(index, true);
        }
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        EnemyPool pool = _world.GetNode<EnemyPool>("EnemyPool");
        SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
        // La première arme était capturée pendant le fondu de l'écran de chargement (1,6 s, plus long que l'attente).
        for (int frame = 0; frame < 600 && _world.FindChild("GameLoadingOverlay", true, false) != null; frame++)
            await Frames(1);
        await Frames(30);
        _player.AIInputOverride = Vector2.Zero;
        foreach (string entry in Argument(OS.GetCmdlineUserArgs(), "--objects", "").Split(',', System.StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = entry.Split(':');
            int level = parts.Length > 1 ? int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) : 1;
            _player.AddOrUpgradePassive(parts[0]);
            if (level > 1)
                _player.AddOrUpgradePassive(parts[0], level - 1);
        }

        bool dash = System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--dash") >= 0;
        MethodInfo attack = typeof(Player).GetMethod("OnWeaponAttackTimeout", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo hp = typeof(Enemy).GetField("_currentHp", BindingFlags.NonPublic | BindingFlags.Instance);
        // XP symbolique : les orbes jaillissent sans déclencher l'écran de montée de niveau au milieu de la galerie.
        FieldInfo xp = typeof(Enemy).GetField("_xpReward", BindingFlags.NonPublic | BindingFlags.Instance);
        List<string> ids = new();
        if (string.IsNullOrEmpty(weaponList))
            foreach (WeaponData data in WeaponDataLoader.GetAll())
                ids.Add(data.Id);
        else
            ids.AddRange(weaponList.Split(','));

        foreach (string id in ids)
        {
            string[] equippedIds = id.Split('+');
            WeaponData weapon = WeaponDataLoader.Get(equippedIds[0]);
            if (weapon == null)
            {
                GD.PushError($"[RunObservation] Arme inconnue : {id}");
                continue;
            }

            while (_player.WeaponSlots.Count > 0)
                _player.RemoveWeapon(0);
            foreach (Node node in GetTree().GetNodesInGroup("enemies"))
                if (node is Enemy existing && existing.IsActive)
                    pool.Return(existing);
            // Laisse les effets de l'arme précédente s'éteindre.
            await Frames(40);

            foreach (string equippedId in equippedIds)
                _player.AddWeapon(WeaponDataLoader.Get(equippedId));
            foreach (string entry in Argument(OS.GetCmdlineUserArgs(), "--ascensions", "").Split(',', System.StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = entry.Split(':');
                foreach (WeaponInstance held in _player.WeaponSlots)
                {
                    if (held.Id != parts[0])
                        continue;
                    while (held.CanLevelUp)
                        held.ApplyUpgrade(System.Array.Empty<StatGain>());
                    _player.AscendWeapon(held.Id, parts[1]);
                }
            }
            // --integer-gains N : N améliorations communes de +0,5 sur chaque stat entière de l'arme (plan 23 R4).
            int integerGains = int.Parse(Argument(OS.GetCmdlineUserArgs(), "--integer-gains", "0"), System.Globalization.CultureInfo.InvariantCulture);
            int targets = int.Parse(Argument(OS.GetCmdlineUserArgs(), "--targets", "5"), System.Globalization.CultureInfo.InvariantCulture);
            foreach (WeaponInstance held in _player.WeaponSlots)
                foreach (string stat in held.Base.Growth.Keys)
                    if (WeaponUpgradeDataLoader.GetStatConfig(stat) is { Integer: true })
                        for (int gain = 0; gain < integerGains && held.CanLevelUp; gain++)
                            _player.UpgradeWeapon(held.Id, new[] { new StatGain(stat, UpgradeRoller.Get("common").IntegerGain) });
            Vector2 origin = _player.GlobalPosition;
            for (int index = 0; index < targets; index++)
            {
                // Une cible seule se place à droite, en terrain dégagé, pour qu'une rafale ou un cône au sol se voie.
                Vector2 offset = targets == 1 ? new Vector2(70f, 0f)
                    : Vector2.FromAngle(-0.9f + index * 0.45f) * (55f + index % 2 * 25f);
                spawner.ForceSpawnEnemy("rodeur", origin + offset);
            }
            await Frames(2);
            bool elite = true;
            foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            {
                if (node is not Enemy enemy || !enemy.IsActive)
                    continue;
                if (lethal && elite)
                {
                    // Une élite parmi les cibles : sa mort porte la signature (onde, éclair).
                    spawner.MakeVariant(enemy, "elite");
                    elite = false;
                }
                hp.SetValue(enemy, lethal ? 1f : 100000f);
                if (lethal)
                    xp.SetValue(enemy, 0.01f);
            }
            if (lethal)
                Engine.TimeScale = 0.25;
            try
            {
                GD.Print(System.FormattableString.Invariant($"[WeaponAudio] {id} audio_s={AudioManager.NowMsec / 1000.0:F2}"));
                audioTrace?.Sample(0);
                for (int slot = 0; slot < _player.WeaponSlots.Count; slot++)
                    attack.Invoke(_player, new object[] { slot });
                // --dash : un dash vers la droite au moment de l'attaque (traînée du Chewing-gum, plan 23 R6).
                if (dash)
                {
                    _player.AIInputOverride = Vector2.Right;
                    _player.Mobility.Request();
                }
                int elapsed = 0;
                // --weapon-frames a,b,… : images capturées après l'attaque (fin de course d'un projectile, plan 27 V2c).
                string customFrames = Argument(OS.GetCmdlineUserArgs(), "--weapon-frames", null);
                int[] frames = customFrames != null
                    ? System.Array.ConvertAll(customFrames.Split(','), frame => int.Parse(frame, System.Globalization.CultureInfo.InvariantCulture))
                    : lethal ? LethalCaptureFrames : equippedIds.Length > 1
                    ? new[] { 2, 9, 30, 60, 120, 180 } : WeaponCaptureFrames;
                for (int shot = 0; shot < frames.Length; shot++)
                {
                    await Frames(frames[shot] - elapsed);
                    elapsed = frames[shot];
                    audioTrace?.Sample(0);
                    SavePlayerCloseUp($"{_output}/weapon-{id}-{shot}.png", new Vector2(150f, 95f));
                    if (dash && shot == 0)
                        _player.AIInputOverride = Vector2.Zero;
                }
            }
            finally
            {
                Engine.TimeScale = 1.0;
                _player.AIInputOverride = Vector2.Zero;
            }
        }
        GD.Print($"[RunObservation] RESULT galerie armes={ids.Count} lethal={lethal} dossier={_output}");
    }

    private void SavePlayerCloseUp(string path, Vector2 half) => SaveCloseUp(path, _player.GlobalPosition, half);
}
