using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.UI;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Inventaire du joueur (plan 35), dans un profil neuf et isolé. Lot I1 : réglages contrôlés, Écussons à part des objets,
/// rendement au-delà du seuil (aperçu de carte et valeur réelle identiques), déblocage d'un Écusson par sa quête. Lot I2 :
/// objets en exemplaires (niveau d'effet, palier au doublon), oubli qui retire effet et palier, sac plein, butin « objet ».
/// Lot I3 : objets lâchés par les Souverains et les élites, ramassage au sol, écran d'échange quand le sac est plein.
/// Lot I4 : surplus d'une arme au bout de sa progression. Lot I6 : Ticket de consigne, Plan de métro, Flacon de pharmacie,
/// Trèfle plastifié, Montre arrêtée. Lot I7 : fils d'usage des Écussons.
/// </summary>
public partial class InventoryRegression : Node
{
    private const string ConfigPath = "res://data/progression/inventory.json";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private int _checks;
    private int _failures;
    private Player _player;

    public override async void _Ready()
    {
        try
        {
            Check(!DevelopmentMode.IsEnabled, "profil normal");
            CheckConfig();
            CheckBadgeShelf();
            CheckBadgeUnlock();
            CheckItemCopies();
            CheckForgetting();
            CheckBag();
            await CheckChestItem();
            await CheckWorldItems();
            CheckWeaponSurplus();
            CheckTicket();
            CheckMetroMap();
            CheckPoisonAndWatch();
            CheckClover();
            CheckBadgeUsage();
            GD.Print($"[InventoryRegression] RESULT failures={_failures} checks={_checks}");
            if (_failures == 0)
                GD.Print("[InventoryRegression] PASS");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError($"[InventoryRegression] FAIL {exception}");
            GetTree().Quit(1);
        }
    }

    private void CheckConfig()
    {
        Check(InventoryConfig.TryLoad(out string error), $"réglages du dépôt acceptés {error}");
        Check(InventoryConfig.BadgeSlots == 4, "quatre Écussons");
        (string, string, string)[] cases =
        {
            ("emplacements nuls", "{\"badge_slots\": 0, \"badge_soft_cap_level\": 30, \"badge_soft_cap_factor\": 0.5}", "badge_slots"),
            ("facteur nul", "{\"badge_slots\": 4, \"badge_soft_cap_level\": 30, \"badge_soft_cap_factor\": 0}", "badge_soft_cap_factor"),
            ("facteur au-delà de 1", "{\"badge_slots\": 4, \"badge_soft_cap_level\": 30, \"badge_soft_cap_factor\": 1.5}", "badge_soft_cap_factor"),
            ("clé inconnue", "{\"badge_slots\": 4, \"badge_soft_cap_level\": 30, \"badge_soft_cap_factor\": 0.5, \"tomes\": 4}", "clé « tomes » inconnue"),
            ("seuil absent", "{\"badge_slots\": 4, \"badge_soft_cap_factor\": 0.5}", "badge_soft_cap_level absent"),
            ("sac vide", Config("\"bag_size\": 0", "\"item_copy_levels\": [10, 15]"), "bag_size"),
            ("exemplaires qui ne montent pas", Config("\"bag_size\": 10", "\"item_copy_levels\": [10, 10]"),
                "item_copy_levels[1] : 10 pas plus haut"),
            ("exemplaires absents", Config("\"bag_size\": 10", "\"item_copy_levels\": []"), "item_copy_levels"),
            ("usage absent", Config("\"bag_size\": 10", "\"item_copy_levels\": [10, 15], \"elite_item_chance\": 0.25, \"sovereign_items\": 1"),
                "usage"),
            ("coup large d'une seule créature", Config("\"bag_size\": 10", "\"item_copy_levels\": [10, 15], \"elite_item_chance\": 0.25, "
                + "\"sovereign_items\": 1, \"usage\": {\"wide_hit_targets\": 1, \"far_kill_distance\": 250, \"contact_radius\": 200}"),
                "wide_hit_targets"),
        };
        foreach ((string label, string json, string expected) in cases)
        {
            string message = InventoryConfig.Apply(json);
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"réglages refusés ({label}) : {message}");
        }
        Check(InventoryConfig.BadgeSlots == 4 && Mathf.IsEqualApprox(InventoryConfig.BadgeStepFactor(31), 0.5f)
            && Mathf.IsEqualApprox(InventoryConfig.BadgeStepFactor(30), 1f), "un fichier refusé ne remplace pas les réglages chargés");
        InventoryConfig.Apply(Godot.FileAccess.GetFileAsString(ConfigPath));
    }

    private void CheckBadgeShelf()
    {
        Setup();
        _player.AddOrUpgradePassive("instinct");
        _player.AddOrUpgradePassive("glacon");
        Check(_player.BadgeSlots.Count == 1 && _player.PassiveSlots.Count == 1 && _player.FindObject("instinct").Data.IsBadge
            && !_player.FindObject("glacon").Data.IsBadge, "un Écusson et un objet vont chacun sur leur étagère");

        // L'aperçu d'une carte et la valeur obtenue suivent le même rendement, de part et d'autre du seuil.
        ActivePassiveSouvenir badge = _player.FindObject("instinct");
        _player.AddOrUpgradePassive("instinct", 28);
        float preview = badge.ValueAfter(0, 1f);
        _player.AddOrUpgradePassive("instinct");
        bool fullStep = Mathf.IsEqualApprox(badge.Value(0), preview) && badge.Level == 30;
        preview = badge.ValueAfter(0, 3f);
        float before = badge.Value(0);
        _player.AddOrUpgradePassive("instinct", 1, 3f);
        Check(fullStep && Mathf.IsEqualApprox(badge.Value(0), preview)
            && Mathf.IsEqualApprox(badge.Value(0) - before, badge.Data.Effects[0].Step * 3f * 0.5f),
            $"niveau 31 : un demi-pas, même valeur annoncée et obtenue ({before:0.###} → {badge.Value(0):0.###})");

        ActivePassiveSouvenir item = _player.FindObject("glacon");
        _player.AddOrUpgradePassive("glacon", 40);
        Check(item.Level == 30 && item.IsMaxLevel, "un objet reste plafonné au niveau 30");
    }

    private void CheckBadgeUnlock()
    {
        QuestDefinition quest = QuestDataLoader.FindUnlocking(UnlockKind.Badge, "oeil_critique");
        Check(quest?.Id == "read_closely" && QuestDataLoader.FindUnlocking(UnlockKind.Object, "oeil_critique") == null,
            "l'Écusson de l'horloger est débloqué par « Lire de près », comme Écusson");
        bool lockedBefore = !MetaSaveManager.IsObjectUnlocked("oeil_critique") && MetaSaveManager.IsObjectUnlocked("memoire_vive");
        MetaSaveManager.ClaimQuest(quest, out _);
        Check(lockedBefore && MetaSaveManager.IsObjectUnlocked("oeil_critique"), "verrouillé en profil neuf, ouvert une fois la quête accomplie");
    }

    private static string Config(string bag, string copies) =>
        $"{{\"badge_slots\": 4, \"badge_soft_cap_level\": 30, \"badge_soft_cap_factor\": 0.5, {bag}, {copies}}}";

    private void CheckItemCopies()
    {
        Setup();
        PassiveSouvenirData chill = PassiveSouvenirDataLoader.Get("glacon");
        Check(InventoryConfig.BagSize == 10 && chill.CopyLevels.Length == 6 && chill.CopyLevels[0] == 10 && chill.CopyLevels[1] == 15,
            "sac de dix ; un objet vaut son niveau 10, son doublon le niveau 15");
        Check(PassiveSouvenirDataLoader.Get("sifflet_d_arbitre").CopyLevels[2] == 3, "le Sifflet garde ses propres exemplaires : un Péril chacun");
        _player.AddItemCopy("glacon");
        ActivePassiveSouvenir item = _player.FindObject("glacon");
        float step = chill.Effects[0].Step;
        Check(item.Copies == 1 && item.Level == 10 && _player.BagCount == 1 && Mathf.IsEqualApprox(_player.ObjectTriggers.ChillChance, step * 10f),
            $"un exemplaire : niveau 10, une place ({_player.ObjectTriggers.ChillChance:0.###} de chance)");
        _player.AddItemCopy("glacon");
        Check(item.Copies == 2 && item.Level == 15 && _player.BagCount == 2 && FreezeSeconds() > 0f,
            "deux exemplaires : niveau 15 et le palier (le double ralentissement fige)");
        for (int i = 0; i < 6; i++)
            _player.AddItemCopy("glacon");
        Check(item.Copies == 6 && item.Level == 25 && _player.BagCount == 6 && !_player.CanTakeItem("glacon"),
            "six exemplaires au plus, niveau 25 ; le septième est refusé");
    }

    private void CheckForgetting()
    {
        Setup();
        _player.AddItemCopy("glacon");
        _player.AddItemCopy("glacon");
        _player.ForgetItemCopy("glacon");
        ActivePassiveSouvenir item = _player.FindObject("glacon");
        Check(item.Copies == 1 && item.Level == 10 && FreezeSeconds() == 0f && _player.BagCount == 1,
            "oublier le doublon : retour au niveau 10, palier retiré");
        _player.ForgetItemCopy("glacon");
        Check(_player.FindObject("glacon") == null && Mathf.IsZeroApprox(_player.ObjectTriggers.ChillChance) && _player.BagCount == 0,
            "oublier le dernier exemplaire : l'objet quitte le sac, son effet aussi");
        Check(!_player.ForgetItemCopy("glacon") && !_player.ForgetItemCopy("instinct"), "rien à oublier : ni objet absent, ni Écusson");

        // Un objet monté puis entièrement oublié ne laisse aucun reste d'arrondi : son effet ne se déclenche plus.
        foreach (string id in new[] { "chewing_gum", "petard_mouille", "semelle_usee" })
        {
            for (int i = 0; i < 6; i++)
                _player.AddItemCopy(id);
            for (int i = 0; i < 6; i++)
                _player.ForgetItemCopy(id);
        }
        bool clean = true;
        foreach (string field in new[] { "_dashTrail", "_killExplosion", "_strideDamage" })
            clean &= (float)typeof(ObjectTriggers).GetField(field, Private).GetValue(_player.ObjectTriggers) == 0f;
        Check(clean && !_player.ObjectTriggers.IsProcessing() && _player.BagCount == 0,
            "six exemplaires pris puis oubliés : effets exactement à zéro, plus d'horloge par image");

        float speed = _player.AttackSpeedMultiplier;
        _player.AddItemCopy("tabouret_de_camping");
        _player.AddItemCopy("tabouret_de_camping");
        float stillArmor = (float)typeof(ObjectStances).GetField("_stillArmor", Private).GetValue(_player.ObjectStances);
        _player.ForgetItemCopy("tabouret_de_camping");
        _player.ForgetItemCopy("tabouret_de_camping");
        Check(stillArmor > 0f && (float)typeof(ObjectStances).GetField("_stillArmor", Private).GetValue(_player.ObjectStances) == 0f
            && Mathf.IsEqualApprox(_player.AttackSpeedMultiplier, speed), "un objet d'état oublié rend son palier et sa cadence");
    }

    private void CheckBag()
    {
        Setup();
        string[] ids = { "glacon", "allumette_humide", "de_a_coudre", "semelle_usee", "chewing_gum" };
        foreach (string id in ids)
        {
            _player.AddItemCopy(id);
            _player.AddItemCopy(id);
        }
        Check(_player.BagCount == 10 && !_player.HasBagRoom && !_player.AddItemCopy("thermometre") && !_player.AddItemCopy("glacon"),
            "sac plein : ni objet neuf, ni doublon");
        _player.ForgetItemCopy("chewing_gum");
        Check(_player.AddItemCopy("thermometre") && _player.BagCount == 10, "une place oubliée se reprend");
        _player.SetBagCapacity(13);
        Check(_player.HasBagRoom && _player.AddItemCopy("glacon"), "les Poches agrandissent le sac");
    }

    private async Task CheckChestItem()
    {
        Setup();
        foreach (ChestData chest in ChestDataLoader.GetAll())
        {
            bool valid = true;
            for (int draw = 0; draw < 30; draw++)
            {
                List<ResolvedLoot> rewards = LootRewards.Resolve(LootResolver.Roll(chest.LootTableId, chest.LootRolls), _player);
                valid &= rewards.Count == chest.LootRolls;
                foreach (ResolvedLoot reward in rewards)
                {
                    PassiveSouvenirData item = PassiveSouvenirDataLoader.Get(reward.ItemId);
                    valid &= reward.Type == "object" && reward.Amount == 1 && item != null
                        && !item.IsBadge && MetaSaveManager.IsObjectUnlocked(item.Id);
                }
            }
            Check(valid, $"{chest.Id} : uniquement {chest.LootRolls} objet(s) débloqué(s), sur 30 ouvertures");
        }

        ChoiceScreen choices = new();
        AddChild(choices);
        ItemExchange exchange = new();
        exchange.Setup(choices);
        AddChild(exchange);
        Node2D ground = new();
        AddChild(ground);
        ItemDropDirector drops = new();
        drops.Setup(exchange, ground);
        AddChild(drops);
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        int otherLoot = 0;
        void OnLoot(string type, string id, int amount) { if (type != "object") otherLoot++; }
        bus.LootReceived += OnLoot;
        try
        {
            // Un épique donne deux objets même s'il ne reste qu'une place ; le second doit rester échangeable.
            _player.SetBagCapacity(1);
            WorldInteraction interaction = new();
            AddChild(interaction);
            interaction.Setup(_player);
            Chest chest = new();
            AddChild(chest);
            chest.Initialize(ChestDataLoader.Get("chest_epic"));
            chest.GlobalPosition = new Vector2(600f, 0f);
            typeof(WorldInteraction).GetMethod("OpenChest", Private).Invoke(interaction, new object[] { chest });
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            ItemPickup overflow = ground.GetChildCount() == 1 ? ground.GetChild<ItemPickup>(0) : null;
            Check(chest.IsOpened && _player.BagCount == 1 && overflow != null && otherLoot == 0,
                "coffre épique : un objet dans la dernière place, un au sol, aucune autre récompense");

            List<ResolvedLoot> fullBag = LootRewards.Resolve(LootResolver.Roll("chest_common", 1), _player);
            Check(fullBag.Count == 1 && fullBag[0].Type == "object", "sac plein au tirage : toujours un objet");
            LootRewards.Apply(fullBag[0], _player, bus, new Vector2(700f, 0f));
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(ground.GetChildCount() == 2 && otherLoot == 0, "sac plein : objet posé, aucune conversion en Essence");

            overflow?.Interact(_player);
            Check(choices.IsOpen && GetTree().Paused, "objet du coffre : échange ouvert en pause");
            choices._Input(new InputEventAction { Action = "ui_cancel", Pressed = true });
            Check(_player.BagCount == 1 && IsInstanceValid(overflow), "refuser l'échange conserve l'objet au sol");
            interaction.QueueFree();
            chest.QueueFree();
        }
        finally
        {
            bus.LootReceived -= OnLoot;
            drops.QueueFree();
            ground.QueueFree();
            exchange.QueueFree();
            choices.QueueFree();
        }
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task CheckWorldItems()
    {
        Setup();
        Check(Mathf.IsEqualApprox(InventoryConfig.EliteItemChance, 0.25f) && InventoryConfig.SovereignItems == 1,
            "une élite lâche un objet une fois sur quatre, un Souverain toujours");
        ChoiceScreen choices = new();
        AddChild(choices);
        ItemExchange exchange = new();
        exchange.Setup(choices);
        AddChild(exchange);
        Node2D ground = new();
        AddChild(ground);
        ItemDropDirector drops = new();
        drops.Setup(exchange, ground);
        AddChild(drops);
        MethodInfo onKill = typeof(ItemDropDirector).GetMethod("OnEnemyKill", Private);
        onKill.Invoke(drops, new object[] { new Vestiges.Combat.EnemyKillResult(default, "rodeur", new Vector2(300f, 0f), default, default,
            default, Sovereign: true) });
        onKill.Invoke(drops, new object[] { new Vestiges.Combat.EnemyKillResult(default, "rodeur", new Vector2(300f, 0f), default, default,
            default) });
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        ItemPickup pickup = ground.GetChildCount() == 1 ? ground.GetChild<ItemPickup>(0) : null;
        PassiveSouvenirData dropped = PassiveSouvenirDataLoader.Get(pickup?.ItemId ?? "");
        Check(pickup != null && dropped != null && !dropped.IsBadge && MetaSaveManager.IsObjectUnlocked(dropped.Id),
            $"un Souverain lâche un objet débloqué, une créature ordinaire rien ({ground.GetChildCount()} au sol)");

        pickup.Interact(_player);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(_player.FindObject(dropped.Id)?.Copies == 1 && !IsInstanceValid(pickup), "ramassé : l'objet entre dans le sac et quitte le sol");

        // Sac plein : l'invite ouvre l'échange ; oublier la première fiche fait la place et prend le nouvel objet.
        foreach (string id in new[] { "glacon", "allumette_humide", "de_a_coudre", "semelle_usee", "chewing_gum" })
            while (_player.HasBagRoom && _player.AddItemCopy(id)) { }
        string first = _player.PassiveSlots[0].Id;
        int firstCopies = _player.PassiveSlots[0].Copies;
        // Un objet que le joueur peut encore cumuler, absent du sac : le tirage au hasard pourrait tomber sur un objet au maximum.
        ItemPickup full = drops.Drop(new Vector2(500f, 0f), PassiveSouvenirDataLoader.Get("thermometre"));
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        PassiveSouvenirData incoming = PassiveSouvenirDataLoader.Get(full.ItemId);
        int incomingBefore = _player.FindObject(incoming.Id)?.Copies ?? 0;
        full.Interact(_player);
        bool opened = choices.IsOpen && GetTree().Paused;
        choices.Activate(_player.PassiveSlots.Count);
        choices.Activate(_player.PassiveSlots.Count);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        bool leftAlone = IsInstanceValid(full) && _player.BagCount == 10 && !choices.IsOpen;
        full.Interact(_player);
        choices.Activate(0);
        choices.Activate(0);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        int expectedFirst = first == incoming.Id ? firstCopies : firstCopies - 1;
        Check(opened && leftAlone, "sac plein : l'invite ouvre l'échange en pause ; « Laisser au sol » ne change rien");
        Check((_player.FindObject(first)?.Copies ?? 0) == expectedFirst && (_player.FindObject(incoming.Id)?.Copies ?? 0) == incomingBefore + (first == incoming.Id ? 0 : 1)
            && _player.BagCount == 10 && !IsInstanceValid(full) && !GetTree().Paused,
            $"échange : un exemplaire de {first} oublié, {incoming.Id} pris, la run reprend");
        // Objet déjà au maximum (six Glaçons) : rien ne s'ouvre, aucun exemplaire n'est perdu.
        while (_player.FindObject("glacon")?.Copies < 6)
        {
            _player.ForgetItemCopy(_player.PassiveSlots[^1].Id);
            _player.AddItemCopy("glacon");
        }
        int bag = _player.BagCount;
        ItemPickup maxed = drops.Drop(new Vector2(600f, 0f), PassiveSouvenirDataLoader.Get("glacon"));
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        maxed.Interact(_player);
        Check(!choices.IsOpen && _player.BagCount == bag && IsInstanceValid(maxed) && !exchange.Open(_player, PassiveSouvenirDataLoader.Get("glacon"), () => true),
            "objet déjà au maximum : l'échange ne s'ouvre pas, rien n'est oublié");
        choices.QueueFree();
    }

    private void CheckWeaponSurplus()
    {
        Setup();
        FragmentManager fragments = new();
        AddChild(fragments);
        typeof(FragmentManager).GetField("_player", Private).SetValue(fragments, _player);
        Vestiges.Combat.WeaponInstance weapon = _player.WeaponSlots[0];
        RandomNumberGenerator rng = new() { Seed = 7 };
        while (weapon.CanLevelUp)
            _player.UpgradeWeapon(weapon.Id, UpgradeRoller.RollWeaponGains(weapon, UpgradeRoller.Get("common"), rng));
        bool notYet = !weapon.IsSurplusReady && !Offers(fragments).Exists(option => option.Type == FragmentOption.SurplusType)
            && !_player.AddWeaponSurplus(weapon.Id, 0.04f);
        _player.AscendWeapon(weapon.Id, weapon.Base.Ascensions[0].Id);
        List<FragmentOption> offers = Offers(fragments);
        float damage = weapon.GetStat("damage");
        FragmentOption surplus = offers.Find(option => option.Type == FragmentOption.SurplusType && option.Id == weapon.Id);
        bool applied = surplus != null && surplus.ApplyTo(_player) && surplus.ApplyTo(_player);
        Check(notYet && applied && weapon.SurplusCards == 2 && Mathf.IsEqualApprox(weapon.GetStat("damage"), damage * 1.08f)
            && !offers.Exists(option => option.Type == "weapon_upgrade" && option.Id == weapon.Id),
            $"arme au niveau maximal et ascensionnée : le surplus remplace son amélioration, +4 % de dégâts par carte ({damage:0.#} → {weapon.GetStat("damage"):0.#})");
        fragments.QueueFree();
    }

    private void CheckTicket()
    {
        Setup();
        Check(_player.AddItemCopy("ticket_de_consigne") && _player.AddItemCopy("ticket_de_consigne")
            && !_player.CanTakeItem("ticket_de_consigne"), "Ticket de consigne : deux exemplaires au plus");
        float maxHp = _player.EffectiveMaxHp;
        BreakShield();
        Strike(maxHp * 10f);
        bool revived = !_player.IsDead && Mathf.IsEqualApprox(_player.CurrentHp, maxHp * 0.5f)
            && _player.FindObject("ticket_de_consigne")?.Copies == 1 && _player.BagCount == 1;
        bool shielded = !_player.TakeDamage(maxHp * 10f).Applied;
        Check(revived && shielded, $"un coup mortel consume un ticket : relevé à la moitié des PV, invulnérable un instant ({_player.CurrentHp:0.#}/{maxHp:0.#})");
        Strike(maxHp * 10f);
        Check(!_player.IsDead && _player.FindObject("ticket_de_consigne") == null && _player.BagCount == 0,
            "le second ticket relève encore, puis le sac est vide");
        _player.AddItemCopy("ticket_de_consigne");
        _player.SurviveFatalHitsForTests = true;
        _player.TakeErasureDamage(maxHp * 10f);
        _player.SurviveFatalHitsForTests = false;
        Check(_player.FindObject("ticket_de_consigne")?.Copies == 1, "le Néant ne prend pas le ticket : il n'en relève pas");
    }

    private void CheckMetroMap()
    {
        Setup();
        PlayerMobility mobility = _player.Mobility;
        const float Dt = 1f / 60f;
        const float Speed = 200f;
        float dashLength = Speed * mobility.Config.SpeedMultiplier * mobility.Config.DurationSeconds;
        Check(_player.AddItemCopy("plan_de_metro") && Mathf.IsEqualApprox(mobility.BlinkDistance, 1f), "Plan de métro : le dash devient un saut");
        mobility.Request();
        Vector2 jump = mobility.Step(Dt, Vector2.Right, Speed, 1f, true) * Dt;
        // L'invulnérabilité du saut est celle du dash (nulle par défaut), comptée depuis le saut.
        bool dashInvulnerable = mobility.Config.InvulnerabilitySeconds > 0f;
        bool instant = mobility.BlinkedThisStep && mobility.IsDashStep && Mathf.IsEqualApprox(jump.X, dashLength)
            && mobility.IsInvulnerable == dashInvulnerable && !mobility.IsDashing && mobility.DashesStarted == 1;
        mobility.Step(Dt, Vector2.Zero, Speed, 1f, true);
        bool landed = !mobility.IsDashing;
        for (float t = 0f; t < 1f; t += Dt)
            mobility.Step(Dt, Vector2.Zero, Speed, 1f, true);
        Check(instant && landed && !mobility.IsInvulnerable,
            $"toute la longueur du dash en une image ({jump.X:0.#} px sur {dashLength:0.#}), puis le dash est fini");
        // Chewing-gum : le saut se fait en une image, les taches couvrent quand même tout le trajet.
        _player.AddItemCopy("chewing_gum");
        _player.ObjectTriggers.OnBlink(Vector2.Zero, new Vector2(dashLength, 0f));
        int patches = ((System.Collections.ICollection)typeof(ObjectTriggers).GetField("_trailPatches", Private).GetValue(_player.ObjectTriggers)).Count;
        float spacing = PassiveSouvenirDataLoader.Get("chewing_gum").Parameters["spacing"];
        Check(patches == Mathf.FloorToInt(dashLength / spacing) + 1, $"Chewing-gum et saut : {patches} taches du départ à l'arrivée");
        _player.AddItemCopy("plan_de_metro");
        bool longer = Mathf.IsEqualApprox(mobility.BlinkDistance, 1.5f);
        _player.ForgetItemCopy("plan_de_metro");
        _player.ForgetItemCopy("plan_de_metro");
        Check(longer && mobility.BlinkDistance == 0f, "un second exemplaire allonge le saut ; oublié, le dash redevient un dash");
    }

    private void CheckPoisonAndWatch()
    {
        Setup();
        AddChild(new Vestiges.Combat.CombatPools());
        Vestiges.Combat.Enemy enemy = GD.Load<PackedScene>("res://scenes/enemies/Enemy.tscn").Instantiate<Vestiges.Combat.Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get("shade"), 1000f, 1f);
        enemy.SetTicking(false);
        enemy.SetProcess(false);
        enemy.GlobalPosition = _player.GlobalPosition + new Vector2(40f, 0f);
        MethodInfo tick = typeof(Vestiges.Combat.Enemy).GetMethod("ProcessPoison", Private);
        FieldInfo hp = typeof(Vestiges.Combat.Enemy).GetField("_currentHp", Private);
        float start = (float)hp.GetValue(enemy);
        enemy.ApplyPoison(30f, 3f);
        enemy.ApplyPoison(30f, 3f);
        for (int frame = 0; frame < 60; frame++)
            tick.Invoke(enemy, new object[] { 1f / 60f });
        float afterOne = start - (float)hp.GetValue(enemy);
        for (int frame = 0; frame < 150; frame++)
            tick.Invoke(enemy, new object[] { 1f / 60f });
        float total = start - (float)hp.GetValue(enemy);
        Check(Mathf.Abs(afterOne - 20f) < 0.5f && Mathf.Abs(total - 60f) < 0.5f && !enemy.IsPoisoned,
            $"poison : deux coups s'accumulent, la réserve se vide sur la durée ({afterOne:0.#} en 1 s, {total:0.#} au total)");

        _player.AddItemCopy("flacon_de_pharmacie");
        _player.ObjectTriggers.OnWeaponImpact(enemy, 100f, null, 1, default, dealt: 100f);
        float reserve = (float)typeof(Vestiges.Combat.Enemy).GetField("_poisonReserve", Private).GetValue(enemy);
        Check(Mathf.IsEqualApprox(reserve, 20f) && enemy.IsPoisoned, $"Flacon de pharmacie : un coup de 100 empoisonne pour 20 ({reserve:0.#})");

        _player.AddItemCopy("montre_arretee");
        typeof(ObjectTriggers).GetMethod("FreezeTime", Private).Invoke(_player.ObjectTriggers, new object[] { enemy.GlobalPosition });
        Check(enemy.IsSlowed, "Montre arrêtée : à la mort d'un Souverain, les créatures proches ralentissent");
        enemy.QueueFree();
    }

    private void CheckClover()
    {
        Setup();
        FragmentManager fragments = new();
        AddChild(fragments);
        typeof(FragmentManager).GetField("_player", Private).SetValue(fragments, _player);
        MethodInfo offer = typeof(FragmentManager).GetMethod("OfferFragments", Private);
        offer.Invoke(fragments, new object[] { 2 });
        int plain = fragments.PendingChoices.Count;
        _player.AddItemCopy("trefle_plastifie");
        offer.Invoke(fragments, new object[] { 3 });
        Check(plain == 3 && fragments.PendingChoices.Count == 4 && !_player.CanTakeItem("trefle_plastifie"),
            $"Trèfle plastifié : une carte de plus au level-up ({plain} → {fragments.PendingChoices.Count}), un seul exemplaire");
        fragments.QueueFree();
    }

    private void CheckBadgeUsage()
    {
        string contractText = Godot.FileAccess.GetFileAsString(ObjectDataValidator.ContractPath);
        ObjectDataValidator.TryParseContract(contractText, out ObjectDataValidator.Contract contract, out _);
        bool allKnown = contract.UsageTriggers.Count == 16;
        foreach (string trigger in contract.UsageTriggers)
            allKnown &= BadgeUsage.IsKnownTrigger(trigger);
        bool everyBadge = true;
        foreach (PassiveSouvenirData data in PassiveSouvenirDataLoader.GetAll())
            everyBadge &= data.IsBadge == (data.UsageThreshold > 0f) && (!data.IsBadge || BadgeUsage.IsKnownTrigger(data.UsageTrigger));
        Check(allKnown && everyBadge, "chaque Écusson a son fil d'usage, et chaque déclencheur du contrat son lecteur");

        string catalog = Godot.FileAccess.GetFileAsString("res://data/progression/passive_souvenirs.json");
        string onItem = catalog.Replace("\"id\": \"glacon\",", "\"id\": \"glacon\", \"usage\": {\"trigger\": \"crits\", \"threshold\": 5},");
        string unknown = catalog.Replace("\"trigger\": \"crits\"", "\"trigger\": \"jongler\"");
        string onItemError = ObjectDataValidator.Validate(onItem, contract, _ => true);
        string unknownError = ObjectDataValidator.Validate(unknown, contract, _ => true);
        Check(onItemError != null && onItemError.Contains("réservé aux Écussons", StringComparison.Ordinal)
            && unknownError != null && unknownError.Contains("absent du contrat", StringComparison.Ordinal),
            $"fil refusé sur un objet ({onItemError}) et pour un déclencheur inconnu ({unknownError})");

        Setup();
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        int shines = 0;
        float lastProgress = -1f;
        void OnThread(string id, float progress, bool leveled)
        {
            lastProgress = progress;
            shines += leveled ? 1 : 0;
        }
        bus.BadgeThreadChanged += OnThread;
        _player.AddOrUpgradePassive("photo_de_classe");
        int levels = Mathf.RoundToInt(PassiveSouvenirDataLoader.Get("photo_de_classe").UsageThreshold);
        for (int level = 2; level < 2 + levels; level++)
            bus.EmitSignal(EventBus.SignalName.LevelUp, level);
        Check(_player.GetPassiveLevel("photo_de_classe") == 2 && _player.BadgeUsage.UsageLevels == 1 && shines == 1
            && Mathf.IsZeroApprox(lastProgress), $"Écusson du bon élève : {levels} niveaux de la run remplissent son fil, un niveau, un éclat");
        float scale = InventoryConfig.UsageLevelScale;
        bus.EmitSignal(EventBus.SignalName.LevelUp, 99);
        Check(Mathf.IsEqualApprox(_player.BadgeUsage.ProgressOf("photo_de_classe"), 1f / (levels * (1f + scale))),
            "au niveau 2, le fil s'allonge d'un dixième");
        _player.AddOrUpgradePassive("photo_de_classe", 9);
        Check(Mathf.IsEqualApprox(_player.BadgeUsage.ProgressOf("photo_de_classe"), 1f / (levels * (1f + scale * 10f))),
            $"une carte qui monte l'Écusson allonge son fil : au niveau 11, {1f + scale * 10f:0.#} fois le seuil");

        _player.AddOrUpgradePassive("resonance");
        _player.AddOrUpgradePassive("oeil_critique");
        ulong owner = _player.GetInstanceId();
        Vestiges.Combat.AttackContext attack = new(owner, null, 77, Vestiges.Combat.DamageKind.DirectWeapon);
        // Un cône frappe la même créature à chaque image sous un même lancer : un seul coup compte.
        for (int frame = 0; frame < 10; frame++)
            bus.PublishEnemyDamage(new Vestiges.Combat.DamageResult(new Vestiges.Combat.EnemyLife(901, 1), attack, 10f, 5f, 0f, 5f, false, true)
                { Critical = true });
        float coneWide = _player.BadgeUsage.ProgressOf("resonance");
        for (ulong target = 902; target < 904; target++)
            bus.PublishEnemyDamage(new Vestiges.Combat.DamageResult(new Vestiges.Combat.EnemyLife(target, 1), attack, 10f, 5f, 0f, 5f, false, true));
        float wide = _player.BadgeUsage.ProgressOf("resonance") * PassiveSouvenirDataLoader.Get("resonance").UsageThreshold;
        float crits = _player.BadgeUsage.ProgressOf("oeil_critique") * PassiveSouvenirDataLoader.Get("oeil_critique").UsageThreshold;
        Check(coneWide == 0f && Mathf.IsEqualApprox(wide, 1f) && Mathf.IsEqualApprox(crits, 1f),
            $"dix tics sur une créature comptent un coup ; trois créatures du même lancer, un coup large ({wide:0.##}) ; un critique ({crits:0.##})");

        _player.AddOrUpgradePassive("jeton_de_fete");
        int before = _player.GetPassiveLevel("jeton_de_fete");
        int chests = Mathf.RoundToInt(PassiveSouvenirDataLoader.Get("jeton_de_fete").UsageThreshold);
        for (int chest = 0; chest < chests; chest++)
            bus.EmitSignal(EventBus.SignalName.ChestOpened, "test", "common", Vector2.Zero);
        Check(_player.GetPassiveLevel("jeton_de_fete") == before + 1, $"Écusson du forain : {chests} coffres, un niveau");
        bus.BadgeThreadChanged -= OnThread;
    }

    private PlayerDefense Defense => (PlayerDefense)typeof(Player).GetField("_defense", Private).GetValue(_player);

    /// <summary>Un coup accepté : l'invulnérabilité du coup précédent est levée d'abord.</summary>
    private void Strike(float damage)
    {
        typeof(PlayerDefense).GetField("_invulnerableTimer", Private).SetValue(Defense, 0f);
        _player.TakeDamage(damage);
    }

    private void BreakShield()
    {
        while (_player.Shield > 0f)
            Strike(1f);
    }

    private static List<FragmentOption> Offers(FragmentManager fragments) =>
        (List<FragmentOption>)typeof(FragmentManager).GetMethod("BuildFragmentPool", Private).Invoke(fragments, null);

    private float FreezeSeconds() =>
        (float)typeof(ObjectTriggers).GetField("_freezeSeconds", Private).GetValue(_player.ObjectTriggers);

    private void Setup()
    {
        if (_player != null)
        {
            RemoveChild(_player);
            _player.QueueFree();
        }
        _player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(_player);
        _player.InitializeCharacter(CharacterDataLoader.Get("vagabond"));
        _player.IsAIControlled = true;
        _player.SetPhysicsProcess(false);
    }

    private void Check(bool condition, string label)
    {
        _checks++;
        if (condition)
            GD.Print($"[InventoryRegression] OK {label}");
        else
        {
            _failures++;
            GD.PushError($"[InventoryRegression] FAIL {label}");
        }
    }
}
