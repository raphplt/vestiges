using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.UI;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>Tirages réels et transactions des coffres : aucun test ne remplace l’ouverture par une simple variation de modificateur.</summary>
public partial class ItemLootRegression : Node
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private int _checks, _failures;
    private Player _player;
    private EssenceTracker _essence;
    private EventBus _bus;
    private readonly RandomNumberGenerator _rng = new() { Seed = 42 };

    public override async void _Ready()
    {
        try
        {
            ItemLootConfig.Load();
            CheckConfig();
            CheckFrequencies();
            CheckFilteringAndChoices();
            _player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
            AddChild(_player);
            _player.InitializeCharacter(CharacterDataLoader.Get("vagabond"));
            _player.SetPhysicsProcess(false);
            _bus = GetNode<EventBus>("/root/EventBus");
            _essence = new EssenceTracker();
            AddChild(_essence);
            CheckTransactions();
            await CheckScreensAndOverflow();
            CheckWorldHooks();
            await CheckCatalogWorldItems();
            GD.Print($"[ItemLootRegression] RESULT checks={_checks} failures={_failures}");
            if (_failures == 0) GD.Print("[ItemLootRegression] PASS");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError($"[ItemLootRegression] FAIL {exception}");
            GetTree().Quit(1);
        }
    }
    private void CheckConfig()
    {
        string json = FileAccess.GetFileAsString(ItemLootConfig.Path);
        Check(ItemLootConfig.Apply(json) == null, "configuration complète acceptée");
        foreach (string key in new[] { "sources", "chest_costs", "workshop_costs", "chest_cost_growth_per_open" })
        {
            JsonObject invalid = JsonNode.Parse(json).AsObject();
            invalid.Remove(key);
            Check(ItemLootConfig.Apply(invalid.ToJsonString()) != null, $"configuration refuse champ absent {key}");
        }
        JsonObject bad = JsonNode.Parse(json).AsObject();
        bad["sources"]["chest_common"]["common"] = -1;
        Check(ItemLootConfig.Apply(bad.ToJsonString()) != null, "poids négatif refusé sans publication partielle");
        Check(ItemLootConfig.ChestCost("common", 0) == 20, "configuration précédente préservée après rejet");
        ItemLootConfig.Apply(json);
        Check(ItemLootConfig.ChestCost("common", 1) == 24 && ItemLootConfig.ChestCost("rare", 1) == 54
            && ItemLootConfig.ChestCost("common", 5) == 40, "prix qui monte à chaque coffre payé (DECISIONS §184)");
    }
    private static PassiveSouvenirData Item(string id, string rarity, int max = 0) => new()
    { Id = id, Name = id, Kind = ObjectKind.Item, Rarity = rarity, MaxCopies = max };
    private void CheckFrequencies()
    {
        List<PassiveSouvenirData> catalog = new();
        foreach (string rarity in ItemLootConfig.Rarities) catalog.Add(Item(rarity, rarity));
        ItemLootPool pool = new(catalog, _ => true, _ => 0);
        const int draws = 100000;
        foreach (string source in ItemLootConfig.Sources)
        {
            double[] previous = null;
            foreach (float luck in new[] { 0f, .03f, .3f, 1f })
            {
                _rng.Seed = 123;
                int[] observed = new int[5];
                double[] expected = pool.Probabilities(source, luck);
                for (int i = 0; i < draws; i++) observed[pool.RollRank(source, _rng, luck)]++;
                bool valid = true;
                for (int rank = 0; rank < 5; rank++)
                {
                    double mean = draws * expected[rank];
                    double tolerance = 6d * Math.Sqrt(mean * (1d - expected[rank])) + 5d;
                    valid &= Math.Abs(observed[rank] - mean) <= tolerance;
                }
                GD.Print($"[ItemLootRegression] FREQUENCY source={source} luck={luck:0.##} counts={string.Join(",", observed)}");
                Check(valid, $"100 000 tirages {source}, Chance {luck:0.##} : fréquences à moins de 6 écarts-types + 5 tirages");
                if (previous != null)
                {
                    for (int minimum = 2; minimum <= 4; minimum++)
                    {
                        double before = 0d, after = 0d;
                        for (int rank = minimum; rank < 5; rank++) { before += previous[rank]; after += expected[rank]; }
                        Check(after >= before, $"{source} : Chance augmente P(rang ≥ {minimum})");
                    }
                }
                previous = expected;
            }
        }
        double[] extreme = pool.Probabilities("chest_common", float.MaxValue);
        Check(double.IsFinite(extreme[4]) && extreme[4] > .999d, "Chance extrême : probabilités finies et normalisées");
        double[] workshop = pool.Probabilities("workshop", 1f);
        Check(Math.Abs(workshop[0] - .3d) < .00001d, "Chance ne change pas les offres d’Atelier");
        List<PassiveSouvenirData> ordinary = new() { Item("a", "common"), Item("b", "common"), Item("c", "common") };
        ItemLootPool uniform = new(ordinary, _ => true, _ => 0);
        Dictionary<string, int> counts = new() { ["a"] = 0, ["b"] = 0, ["c"] = 0 };
        for (int i = 0; i < 30000; i++) counts[uniform.Pick("generic", _rng).Id]++;
        Check(Math.Abs(counts["a"] - 10000) < 600 && Math.Abs(counts["b"] - 10000) < 600 && Math.Abs(counts["c"] - 10000) < 600,
            "dans le rang, objets tirés uniformément");
    }
    private void CheckFilteringAndChoices()
    {
        PassiveSouvenirData ordinary = Item("ordinary", "common");
        PassiveSouvenirData locked = Item("locked", "epic");
        PassiveSouvenirData capped = Item("capped", "legendary", 1);
        PassiveSouvenirData badge = Item("badge", "common"); badge.Kind = ObjectKind.Badge;
        ItemLootPool filtered = new(new[] { ordinary, locked, capped, badge }, item => item != locked, id => id == "capped" ? 1 : 41);
        Check(filtered.Pick("chest_epic", _rng)?.Id == "ordinary", "rang haut vide : repli sur objet accessible, copies ordinaires > 6 conservées");
        ItemLootPool empty = new(Array.Empty<PassiveSouvenirData>(), _ => true, _ => 0);
        Check(!empty.CanSupply(1) && empty.Pick("generic", _rng) == null, "catalogue vide : pas de lot fantôme ni Essence");
        PassiveSouvenirData first = Item("first", "legendary", 1), second = Item("second", "legendary", 1);
        ItemLootPool limited = new(new[] { first, second }, _ => true, _ => 0);
        ChestRewardSession choices = new(limited, "chest_epic", .3f, 2, 3, _rng);
        IReadOnlyList<PassiveSouvenirData> proposed = choices.Propose();
        Check(proposed.Count == 2 && proposed[0] != proposed[1] && ReferenceEquals(proposed, choices.Propose()), "Carnet : choix distincts, rang et sélection mémorisés, liste réduite aux candidats présents");
        string chosen = proposed[0].Id;
        Check(choices.Choose(0) && !choices.Choose(0), "un choix ne peut être validé deux fois");
        Check(choices.Propose().Count == 1 && choices.Propose()[0].Id != chosen, "seul l’objet choisi est réservé, l’alternative reste disponible au second tirage");
        choices.Choose(0);
        Check(choices.Complete && choices.Rewards.Count == 2 && choices.MarkApplied() && !choices.MarkApplied(), "attribution unique des deux récompenses");
        Check(!limited.CanSupply(3), "deux objets uniques ne financent pas trois récompenses");
    }
    private Chest Chest(string id = "chest_common")
    {
        Chest chest = new(); AddChild(chest); chest.Initialize(ChestDataLoader.Get(id)); return chest;
    }
    private void Balance(int amount)
    {
        _essence.TrySpend(_essence.CurrentEssence);
        _essence.AddEssence(amount);
    }
    private List<ActivePassiveSouvenir> Bag => (List<ActivePassiveSouvenir>)typeof(Player).GetField("_passiveSlots", Private).GetValue(_player);
    private ActivePassiveSouvenir Fixture(string id, int copies, params (string Key, float Value)[] parameters)
    {
        PassiveSouvenirData data = Item(id, "common");
        foreach ((string key, float value) in parameters) data.Parameters[key] = value;
        ActivePassiveSouvenir owned = new(data); owned.SetItemCopies(copies); Bag.Add(owned); return owned;
    }
    private void CheckTransactions()
    {
        Balance(19);
        Chest unpaid = Chest();
        Check(unpaid.Quote(5) == 40 && unpaid.Quote(0) == 20, "devis qui suit les coffres payés de la run");
        string prompt = unpaid.PricePrompt(_player, _essence, 0);
        Check(prompt.Contains("20 Essence") && prompt.Contains("insuffisants") && ReferenceEquals(prompt, unpaid.PricePrompt(_player, _essence, 0)), "prompt annonce prix/manque, réutilise le texte sans allocation par frame");
        Check(unpaid.PricePrompt(_player, _essence, 1).Contains("24 Essence"), "prompt refait quand le prix monte");
        ulong rngBefore = _rng.State;
        Check(!unpaid.TryPurchase(_player, _essence, 0, _rng, out _) && !unpaid.IsOpened && _essence.CurrentEssence == 19 && _rng.State == rngBefore,
            "pas assez : coffre fermé, aucun débit/tirage de butin ; zéro Pied-de-biche ne consomme pas de hasard");
        ActivePassiveSouvenir crowbar = Fixture("pied_de_biche", 100, ("chance_per_copy", .15f), ("max_chance", .6f));
        Balance(20);
        Check(unpaid.TryPurchase(_player, _essence, 0, _rng, out _) && _essence.CurrentEssence == 0 && !unpaid.FreeOpening,
            "retour après acquisition du Pied-de-biche : première tentative payante conservée, prix original débité");
        Check(!unpaid.TryPurchase(_player, _essence, 0, _rng, out _), "coffre ouvert ne paie jamais deux fois");
        Check(Mathf.IsEqualApprox(WorldItemEffects.FreeChestChance(_player), .6f), "100 copies ordinaires autorisées, gratuité plafonnée à 60 %");
        Balance(0);
        Chest free = Chest();
        RandomNumberGenerator lucky = SeedForRoll(true);
        Check(free.TryPurchase(_player, _essence, 0, lucky, out _) && free.FreeOpening && free.IsOpened && _essence.CurrentEssence == 0,
            "gratuité réussie : ouverture réelle avec zéro Essence");
        Chest dropped = Chest();
        dropped.AlwaysFree = true;
        Check(dropped.PricePrompt(_player, _essence, 3).Contains("gratuit") && dropped.TryPurchase(_player, _essence, 3, _rng, out _)
            && dropped.FreeOpening && dropped.IsOpened && _essence.CurrentEssence == 0,
            "coffre d'élite ou de boss : gratuit, sans Essence (DECISIONS §184)");
        Chest failed = Chest();
        RandomNumberGenerator unlucky = SeedForRoll(false);
        Check(!failed.TryPurchase(_player, _essence, 0, unlucky, out _), "gratuité manquée, portefeuille vide : reste fermé");
        ulong state = unlucky.State;
        Check(!failed.TryPurchase(_player, _essence, 0, unlucky, out _) && state == unlucky.State, "insuffisance répétée ne relance pas la gratuité");
        ItemLootPool empty = new(Array.Empty<PassiveSouvenirData>(), _ => true, _ => 0);
        Chest unavailable = Chest(); Balance(100);
        state = _rng.State;
        Check(!unavailable.TryPurchase(_player, _essence, 0, _rng, empty, out _) && _essence.CurrentEssence == 100 && state == _rng.State,
            "aucun candidat : aucun paiement ni jet de gratuité, coffre fermé");
        Check(!(bool)typeof(Chest).GetField("_freeAttempted", Private).GetValue(unavailable), "indisponibilité ne brûle pas la première tentative");
        Bag.Remove(crowbar);
        Chest reentrant = Chest(); Balance(100);
        bool entered = false, duplicate = false;
        void Reenter(int amount)
        {
            if (entered) return;
            entered = true;
            duplicate = reentrant.TryPurchase(_player, _essence, 0, _rng, out _);
        }
        _bus.EssenceChanged += Reenter;
        bool purchased = reentrant.TryPurchase(_player, _essence, 0, _rng, out _);
        _bus.EssenceChanged -= Reenter;
        Check(purchased && entered && !duplicate && _essence.CurrentEssence == 80, "réentrée depuis EssenceChanged : un seul paiement et une seule ouverture");
    }
    private static RandomNumberGenerator SeedForRoll(bool success)
    {
        RandomNumberGenerator probe = new();
        for (ulong seed = 1; seed < 10000; seed++)
        {
            probe.Seed = seed;
            if ((probe.Randf() < .6f) == success) return new RandomNumberGenerator { Seed = seed };
        }
        throw new InvalidOperationException("Graine de test introuvable");
    }
    private async Task CheckScreensAndOverflow()
    {
        ChestLootScreen screen = new(); AddChild(screen);
        ChoiceScreen choices = new(); AddChild(choices);
        WorldInteraction interaction = new(); AddChild(interaction); interaction.Setup(_player); interaction.Configure(screen, _essence, choices);
        _player.SetBagCapacity(1);
        _player.AddItemCopy("boite_de_pansements");
        int dropped = 0, opened = 0;
        void OnDrop(string id, Vector2 position) { if (PassiveSouvenirDataLoader.Get(id) is { IsBadge: false }) dropped++; }
        void OnOpen(string id, string rarity, Vector2 position) => opened++;
        _bus.ItemDropRequested += OnDrop;
        _bus.ChestOpened += OnOpen;
        Balance(90);
        Chest epic = Chest("chest_epic");
        typeof(WorldInteraction).GetMethod("OpenChest", Private).Invoke(interaction, new object[] { epic });
        Check(epic.IsOpened && opened == 1 && _essence.CurrentEssence == 0 && screen.Visible && dropped == 0,
            "épique payé : deux objets réservés avant roulette, aucune attribution anticipée");
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        ScrollContainer lootArea = (ScrollContainer)typeof(ChestLootScreen).GetField("_scroll", Private).GetValue(screen);
        Check(lootArea.Size.Y >= 144f, "prix et probabilités laissent au moins une carte de butin lisible");
        typeof(ChestLootScreen).GetMethod("Advance", Private).Invoke(screen, null);
        Check(screen.Visible && dropped == 0, "sauter animation révèle sans payer ni attribuer à nouveau");
        typeof(ChestLootScreen).GetMethod("Advance", Private).Invoke(screen, null);
        typeof(ChestLootScreen).GetMethod("Advance", Private).Invoke(screen, null);
        Check(!screen.Visible && dropped == 2 && opened == 1 && _essence.CurrentEssence == 0 && epic.Rewards.Applied,
            "sac plein : deux objets déposés, fermeture répétée sans doublon de paiement/loot");
        _bus.ItemDropRequested -= OnDrop; _bus.ChestOpened -= OnOpen;
        screen.QueueFree(); choices.QueueFree(); interaction.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private void CheckWorldHooks()
    {
        ActivePassiveSouvenir label = Fixture("etiquette_perdue", 8, ("pointers_per_copy", 1f), ("max_pointers", 3f));
        Check(WorldItemEffects.ChestPointers(_player) == 3, "Étiquette : huit copies, trois flèches au maximum");
        Bag.Remove(label);
        Check(WorldItemEffects.ChestPointers(_player) == 0, "sans Étiquette : aucune flèche de coffre");
        ActivePassiveSouvenir ticket = Fixture("ticket_de_vestiaire", 8, ("extra_seconds_per_copy", 45f));
        Check(WorldItemEffects.ExtraItemSeconds(_player) == 360f, "Ticket : durée additive au-delà de six copies");
        Bag.Remove(ticket);
        PackedScene scene = GD.Load<PackedScene>("res://scenes/combat/XpOrb.tscn");
        XpOrb near = scene.Instantiate<XpOrb>(), far = scene.Instantiate<XpOrb>(); AddChild(near); AddChild(far);
        near.SetRelease(_ => { }); far.SetRelease(_ => { });
        near.Launch(new Vector2(1000, 0), 3); far.Launch(new Vector2(1400, 0), 5);
        int attracted = XpOrb.AttractExisting(new Vector2(1000, 0), 150f, _player);
        Check(attracted == 1 && (bool)typeof(XpOrb).GetField("_trailBound", Private).GetValue(near)
            && !(bool)typeof(XpOrb).GetField("_trailBound", Private).GetValue(far), "Sonnette : attraction réelle d’orbes existantes dans le rayon seulement");
        near.QueueFree(); far.QueueFree();
    }
    private void Check(bool condition, string label)
    {
        _checks++;
        if (condition) GD.Print($"[ItemLootRegression] OK {label}");
        else { _failures++; GD.PushError($"[ItemLootRegression] FAIL {label}"); }
    }
}
