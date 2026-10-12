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
/// objets en exemplaires directs sans palier, oubli qui retire exactement un exemplaire, sac plein, butin « objet ».
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
            await CheckHudSorting();
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
            ("sac vide", Config("\"bag_size\": 0", "\"elite_item_chance\": 0.25"), "bag_size"),
            ("ancienne courbe refusée", Config("\"bag_size\": 10", "\"item_copy_levels\": [10, 15]"), "clé « item_copy_levels » inconnue"),
            ("usage absent", Config("\"bag_size\": 10", "\"elite_item_chance\": 0.25, \"sovereign_items\": 1"),
                "usage"),
            ("coup large d'une seule créature", Config("\"bag_size\": 10", "\"elite_item_chance\": 0.25, "
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
        _player.SetBagCapacity(45);
        _player.AddOrUpgradePassive("glacon", 40);
        Check(item.Copies == 41 && item.Level == 41 && !item.IsMaxLevel, "les objets dépassent six copies et trente anciens niveaux ; seul le sac compte");
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
        Check(InventoryConfig.BagSize == 10 && !chill.HasCopyLimit && chill.Rarity == "uncommon" && chill.Milestones.Count == 0,
            "sac de dix ; objets ordinaires sans limite de copies, rareté fixe et aucun palier");
        Check(PassiveSouvenirDataLoader.Get("sifflet_d_arbitre").Effects[0].Step == 1f, "le Sifflet donne un Péril par copie");
        _player.AddItemCopy("glacon");
        ActivePassiveSouvenir item = _player.FindObject("glacon");
        float step = chill.Effects[0].Step;
        Check(item.Copies == 1 && item.Level == 1 && _player.BagCount == 1 && Mathf.IsEqualApprox(_player.ObjectTriggers.ChillChance, 0.08f),
            "un exemplaire : un pas d’effet et une place");
        _player.AddItemCopy("glacon");
        Check(item.Copies == 2 && item.Level == 2 && Mathf.IsEqualApprox(item.Value(0), step * 2f) && item.Data.Milestones.Count == 0,
            "deux exemplaires : deux pas, aucun gel de doublon");
        for (int i = 0; i < 6; i++)
            _player.AddItemCopy("glacon");
        Check(item.Copies == 8 && _player.BagCount == 1 && _player.BagCopyCount == 8 && _player.CanTakeItem("glacon"),
            "huit exemplaires ordinaires : une seule place, sans seuil artificiel à six");
        _player.AddItemCopy("ticket_de_consigne");
        _player.AddItemCopy("ticket_de_consigne");
        _player.SetBagCapacity(12);
        Check(!_player.AddItemCopy("ticket_de_consigne") && _player.IsItemAtMax("ticket_de_consigne"),
            "limite légendaire conservée à deux tickets malgré une place disponible");
    }

    private void CheckForgetting()
    {
        Setup();
        _player.AddItemCopy("glacon");
        _player.AddItemCopy("glacon");
        _player.ForgetItemCopy("glacon");
        ActivePassiveSouvenir item = _player.FindObject("glacon");
        Check(item.Copies == 1 && item.Level == 1 && item.Data.Milestones.Count == 0 && _player.BagCount == 1,
            "oublier le doublon retire un pas, sans palier");
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
        Check(clean && _player.ObjectTriggers.StrideCharges == 0 && _player.BagCount == 0,
            "six exemplaires pris puis oubliés : effets et charges exactement à zéro ; horloge conservée pour les délais");

        float speed = _player.AttackSpeedMultiplier;
        _player.AddItemCopy("tabouret_de_camping");
        _player.AddItemCopy("tabouret_de_camping");
        float armor = _player.Armor;
        _player.ForgetItemCopy("tabouret_de_camping");
        _player.ForgetItemCopy("tabouret_de_camping");
        Check(Mathf.IsEqualApprox(_player.Armor, armor)
            && Mathf.IsEqualApprox(_player.AttackSpeedMultiplier, speed), "un objet d’état oublié ne laisse ni armure ni cadence parasite");
    }

    private void CheckBag()
    {
        Setup();
        string[] ids = { "glacon", "allumette_humide", "de_a_coudre", "semelle_usee", "chewing_gum",
            "boite_de_pansements", "thermos", "medaille_cabossee", "tabouret_de_camping", "flacon_de_pharmacie" };
        foreach (string id in ids)
        {
            _player.AddItemCopy(id);
            _player.AddItemCopy(id);
        }
        Check(_player.BagCount == InventoryConfig.BagSize && _player.BagCopyCount == 20 && !_player.HasBagRoom
            && !_player.AddItemCopy("thermometre") && _player.AddItemCopy("glacon") && _player.BagCount == 10,
            "dix types, vingt exemplaires : nouveau type refusé, doublon accepté à sac plein");
        _player.ForgetItemCopy("chewing_gum");
        Check(_player.BagCount == 10 && !_player.HasBagRoom && !_player.AddItemCopy("thermometre"),
            "oublier un exemplaire laisse la pile et son emplacement occupés");
        _player.ForgetItemCopy("chewing_gum");
        Check(_player.AddItemCopy("thermometre") && _player.BagCount == 10, "une place oubliée se reprend");
        _player.SetBagCapacity(13);
        Check(_player.HasBagRoom && _player.AddItemCopy("epingle_a_nourrice") && _player.BagCount == 11,
            "une capacité améliorée accepte plus de dix types distincts");
        _player.SetBagCapacity(1);
        Check(_player.BagCapacity == 11, "la capacité ne descend pas sous les types portés, sans compter les doublons");
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
            // À sac plein, les deux objets d'un épique sont de nouveaux types (le Glaçon est Inhabituel).
            _player.SetBagCapacity(1);
            _player.AddItemCopy("glacon");
            WorldInteraction interaction = new();
            AddChild(interaction);
            interaction.Setup(_player);
            EssenceTracker chestEssence = new();
            AddChild(chestEssence);
            chestEssence.AddEssence(90);
            interaction.Configure(null, chestEssence, null);
            Chest chest = new();
            AddChild(chest);
            chest.Initialize(ChestDataLoader.Get("chest_epic"));
            chest.GlobalPosition = new Vector2(600f, 0f);
            typeof(WorldInteraction).GetMethod("OpenChest", Private).Invoke(interaction, new object[] { chest });
            Check(chestEssence.CurrentEssence == 0, "coffre épique : paiement de 90 Essence avant les deux objets");
            chestEssence.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            ItemPickup overflow = ground.GetChildCount() == 2 ? ground.GetChild<ItemPickup>(0) : null;
            Check(chest.IsOpened && _player.BagCount == 1 && overflow != null && otherLoot == 0,
                "coffre épique à sac plein : deux nouveaux types au sol, aucune autre récompense");

            List<ResolvedLoot> fullBag = LootRewards.Resolve(LootResolver.Roll("chest_common", 1), _player);
            Check(fullBag.Count == 1 && fullBag[0].Type == "object", "sac plein au tirage : toujours un objet");
            bool joinsStack = _player.CanTakeItem(fullBag[0].ItemId);
            LootRewards.Apply(fullBag[0], _player, bus, new Vector2(700f, 0f));
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(ground.GetChildCount() == (joinsStack ? 2 : 3) && otherLoot == 0,
                "sac plein : doublon empilé, nouveau type posé, aucune conversion en Essence");

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
        foreach (PassiveSouvenirData item in PassiveSouvenirDataLoader.GetAll())
            if (!item.IsBadge && _player.HasBagRoom && _player.FindObject(item.Id) == null)
                _player.AddItemCopy(item.Id);
        int duplicateBefore = _player.FindObject("glacon").Copies;
        ItemPickup duplicate = drops.Drop(new Vector2(450f, 0f), PassiveSouvenirDataLoader.Get("glacon"));
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        duplicate.Interact(_player);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(_player.BagCount == 10 && _player.FindObject("glacon").Copies == duplicateBefore + 1
            && !choices.IsOpen && !IsInstanceValid(duplicate), "doublon au sol à sac plein : empilé sans échange");
        string first = ItemSort.Order(_player.PassiveSlots, item => item.Data.Rarity, item => item.Data.Name)[0].Id;
        _player.AddOrUpgradePassive(first, 3);
        int firstCopies = _player.FindObject(first).Copies;
        // Un objet que le joueur peut encore cumuler, absent du sac : le tirage au hasard pourrait tomber sur un objet au maximum.
        PassiveSouvenirData absent = PassiveSouvenirDataLoader.GetAll().Find(item => !item.IsBadge && _player.FindObject(item.Id) == null);
        ItemPickup full = drops.Drop(new Vector2(500f, 0f), absent);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        PassiveSouvenirData incoming = PassiveSouvenirDataLoader.Get(full.ItemId);
        int incomingBefore = _player.FindObject(incoming.Id)?.Copies ?? 0;
        int copiesBeforeExchange = _player.BagCopyCount;
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
        Check(opened && leftAlone, "sac plein : l'invite ouvre l'échange en pause ; « Laisser au sol » ne change rien");
        Check(_player.FindObject(first) == null && (_player.FindObject(incoming.Id)?.Copies ?? 0) == incomingBefore + 1
            && _player.BagCount == 10 && !IsInstanceValid(full) && !GetTree().Paused,
            $"échange : pile de {first} oubliée, {incoming.Id} pris, la run reprend");
        Check(_player.BagCopyCount == copiesBeforeExchange - firstCopies + 1,
            "remplacer une pile retire tous ses exemplaires et ajoute la nouvelle copie");
        ActivePassiveSouvenir rollback = ItemSort.Order(_player.PassiveSlots, item => item.Data.Rarity, item => item.Data.Name)[0];
        int restoredCopies = rollback.Copies;
        int restoredBagCopies = _player.BagCopyCount;
        PassiveSouvenirData refused = PassiveSouvenirDataLoader.GetAll().Find(item => !item.IsBadge && _player.FindObject(item.Id) == null);
        exchange.Open(_player, refused, () => false);
        choices.Activate(0);
        choices.Activate(0);
        Check(_player.FindObject(rollback.Id)?.Copies == restoredCopies && _player.BagCopyCount == restoredBagCopies && _player.BagCount == 10,
            "un échange refusé restaure tous les exemplaires de la pile");
        // Seuls les objets légendaires/uniques saturés refusent un échange.
        if (_player.FindObject("ticket_de_consigne") == null)
            _player.ForgetItemStack(_player.PassiveSlots[0].Id);
        _player.AddOrUpgradePassive("ticket_de_consigne", 2);
        int bag = _player.BagCount;
        ItemPickup maxed = drops.Drop(new Vector2(600f, 0f), PassiveSouvenirDataLoader.Get("ticket_de_consigne"));
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        maxed.Interact(_player);
        Check(!choices.IsOpen && _player.BagCount == bag && IsInstanceValid(maxed)
            && !exchange.Open(_player, PassiveSouvenirDataLoader.Get("ticket_de_consigne"), () => true),
            "légendaire déjà au maximum : aucun échange ni oubli");
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
        Vestiges.Combat.WeaponInstance weapon = _player.WeaponSlots[0];
        _player.ObjectTriggers.Rng.Seed = 123;
        int doses = 0;
        for (int i = 0; i < 1000; i++)
        {
            _player.ObjectTriggers.OnWeaponImpact(enemy, 100f, weapon, 1, _player.BeginAttack(weapon, 100f), dealt: 100f);
            doses = enemy.PoisonDoseCount;
        }
        Check(doses == 2, $"Flacon : cent coups n’imposent pas cent poisons ; budget partagé de deux doses/s ({doses})");
        float beforePoison = (float)hp.GetValue(enemy);
        tick.Invoke(enemy, new object[] { 3.1f });
        Check(Mathf.Abs(beforePoison - (float)hp.GetValue(enemy) - 40f) < 0.1f && !enemy.IsPoisoned,
            "Deux doses issues de coups de référence 100 infligent au total 40, puis expirent");

        _player.AddItemCopy("montre_arretee");
        enemy.ApplyVariant(EnemyVariantDataLoader.GetVariant("elite"), System.Array.Empty<EnemyAffixData>());
        for (int i = 0; i < 1000 && !enemy.IsSlowed; i++)
            _player.ObjectTriggers.OnWeaponImpact(enemy, 100f, weapon, 1, _player.BeginAttack(weapon, 100f), dealt: 100f);
        Check(enemy.IsSlowed, "Montre arrêtée : un impact d’arme peut ralentir une élite");
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

    private async Task CheckHudSorting()
    {
        Setup();
        _player.AddItemCopy("glacon");
        _player.AddItemCopy("boite_de_pansements");
        ItemSort.Select(ItemSortMode.RarityAscending);
        HudInventoryBar hud = new();
        AddChild(hud);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Control common = hud.ItemIconOf("boite_de_pansements");
        Control uncommon = hud.ItemIconOf("glacon");
        Check(common != null && uncommon != null && common.GlobalPosition.Y < uncommon.GlobalPosition.Y,
            "HUD : objets communs avant inhabituels malgré l'ordre d'acquisition");
        Check(common is TextureRect commonIcon && commonIcon.Texture.ResourcePath == PassiveSouvenirDataLoader.Get("boite_de_pansements").Icon
            && uncommon is TextureRect uncommonIcon && uncommonIcon.Texture.ResourcePath == PassiveSouvenirDataLoader.Get("glacon").Icon,
            "Après tri, les vols de ramassage visent encore l'icône du bon objet");
        PauseMenu pause = new();
        AddChild(pause);
        typeof(PauseMenu).GetMethod("Pause", Private).Invoke(pause, null);
        GridContainer bag = (GridContainer)typeof(PauseMenu).GetField("_objectsGrid", Private).GetValue(pause);
        bag.GetChild<Button>(0).GrabFocus();
        ItemSortSelector selector = (ItemSortSelector)pause.FindChild("ItemSort", true, false);
        selector.GrabFocus();
        ItemSort.Select(ItemSortMode.RarityDescending);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(hud.ItemIconOf("glacon").GlobalPosition.Y < hud.ItemIconOf("boite_de_pansements").GlobalPosition.Y,
            "La préférence de tri réordonne immédiatement le HUD");
        Check(hud.ItemIconOf("glacon") == uncommon && hud.ItemIconOf("boite_de_pansements") == common,
            "Les contrôles d'icône gardent leur identité quand l'ordre change pendant un ramassage");
        Check(bag.GetChild<Button>(0).Name == "glacon" && selector.Selected == (int)ItemSortMode.RarityDescending,
            "La pause et son sélecteur suivent le même tri que le HUD");
        Check((string)typeof(PauseMenu).GetField("_examinedItemId", Private).GetValue(pause) == "boite_de_pansements"
            && GetViewport().GuiGetFocusOwner() == selector,
            "Trier le sac conserve sa fiche examinée et le focus du sélecteur");
        Check(_player.PassiveSlots[0].Id == "glacon" && _player.PassiveSlots[1].Id == "boite_de_pansements" && _player.BagCount == 2,
            "Le tri de présentation ne change ni les exemplaires ni l'inventaire réel");
        ItemSort.Select(ItemSortMode.Original);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(hud.ItemIconOf("glacon").GlobalPosition.Y < hud.ItemIconOf("boite_de_pansements").GlobalPosition.Y,
            "Ordre initial du HUD : ordre d'acquisition retrouvé");
        typeof(PauseMenu).GetMethod("Resume", Private).Invoke(pause, null);
        pause.QueueFree();
        hud.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        ItemSort.Select(ItemSortMode.RarityAscending);
    }

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
