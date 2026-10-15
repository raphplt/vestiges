using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;
using Vestiges.UI;
using Vestiges.World;

namespace Vestiges.Tests;

public partial class ItemLootRegression
{
    private async Task CheckCatalogWorldItems()
    {
        string[] ids = { "pied_de_biche", "ticket_de_vestiaire", "sonnette", "carnet_dadresses" };
        Check(PassiveSouvenirDataLoader.Get("etiquette_perdue") == null, "Étiquette perdue retirée : les flèches de coffres valent pour tous (DECISIONS §184)");
        foreach (string id in ids)
        {
            bool exists = PassiveSouvenirDataLoader.Get(id) is { IsBadge: false };
            Check(exists, $"objet monde présent dans le catalogue réel : {id}");
            if (!exists) return;
        }
        _player.SetBagCapacity(200);
        foreach (string id in ids)
            Check(_player.AddItemCopy(id), $"acquisition réelle applique {id}");
        Check(WorldItemEffects.FreeChestChance(_player) == .15f
            && WorldItemEffects.ExtraItemSeconds(_player) == 45f && WorldItemEffects.ExtraChoices(_player) == 1,
            "les paramètres du catalogue sont raccordés aux services du monde");
        for (int copy = 1; copy < 8; copy++)
            foreach (string id in ids)
                if (id != "carnet_dadresses") Check(_player.AddItemCopy(id), $"copie {copy + 1} autorisée : {id}");
        Check(WorldItemEffects.FreeChestChance(_player) == .6f
            && WorldItemEffects.ExtraItemSeconds(_player) == 360f, "huit copies : caps d’effet conservés, durée linéaire");
        Check(_player.AddItemCopy("carnet_dadresses") && _player.AddItemCopy("carnet_dadresses")
            && !_player.AddItemCopy("carnet_dadresses") && WorldItemEffects.ExtraChoices(_player) == 3,
            "Carnet légendaire : trois copies, quatre propositions au maximum");
        Balance(0);
        Chest free = Chest();
        Check(free.TryPurchase(_player, _essence, 0, SeedForRoll(true), out _) && free.FreeOpening
            && _essence.CurrentEssence == 0, "Pied-de-biche réel : coffre ouvert et objets réservés sans Essence");
        RemoveCopies("pied_de_biche");
        await CheckRealPointers();
        await CheckRealTicket();
        CheckRealBell();
        await CheckRealAddressBook();
        foreach (string id in ids) RemoveCopies(id);
        Check(WorldItemEffects.FreeChestChance(_player) == 0
            && WorldItemEffects.ExtraItemSeconds(_player) == 0 && WorldItemEffects.ExtraChoices(_player) == 0,
            "oubli réel : tous les effets du monde retirés");
    }

    private void RemoveCopies(string id)
    {
        while (_player.FindObject(id) != null) _player.ForgetItemCopy(id);
    }

    private async Task CheckRealPointers()
    {
        Camera2D camera = new() { Zoom = new Vector2(5, 5) }; AddChild(camera); camera.MakeCurrent();
        Control hud = new() { Size = new Vector2(960, 540) }; AddChild(hud);
        ChestPointers pointers = new(); hud.AddChild(pointers);
        Chest offscreen = Chest(); offscreen.GlobalPosition = new Vector2(650, 0);
        SmallPlace revealed = new() { Position = new Vector2(660, 0) }; AddChild(revealed);
        revealed.Initialize(new SmallPlaceData { Id = "fixture", Color = Colors.White, Family = "pale" }, null);
        revealed.Reveal(20);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        camera.ForceUpdateScroll();
        pointers._Process(0);
        Check((int)typeof(ChestPointers).GetField("_count", Private).GetValue(pointers) > 0
            && (int)typeof(ChestPointers).GetField("_placeCount", Private).GetValue(pointers) == 1,
            "sans objet : coffre hors champ signalé, lieu révélé aussi");
        revealed.Unreveal(); revealed.QueueFree(); hud.QueueFree(); offscreen.QueueFree(); camera.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private ItemPickup GroundItem(Vector2 position)
    {
        ItemPickup pickup = new(); pickup.Initialize(PassiveSouvenirDataLoader.Get("boite_de_pansements"), position);
        AddChild(pickup); pickup.SetProcess(false); return pickup;
    }

    private async Task CheckRealTicket()
    {
        ErasureManager erasure = new() { Name = "ErasureManager" }; AddChild(erasure); erasure.SetProcess(false);
        ItemPickup kept = GroundItem(new Vector2(3000, 0));
        kept._Process(121);
        Check(kept.CanInteract, "Ticket réel : un objet dépasse sa durée naturelle de 120 s");
        RemoveCopies("ticket_de_vestiaire");
        kept._Process(.3);
        Check(!kept.CanInteract, "oubli du Ticket : l’objet trop ancien expire sans repartir à zéro");
        _player.AddItemCopy("ticket_de_vestiaire");
        ItemPickup expired = GroundItem(new Vector2(3100, 0));
        expired._Process(164);
        Check(expired.CanInteract, "Ticket : l’objet reste jusqu’à son âge total de 165 s");
        expired._Process(1.1);
        Check(!expired.CanInteract, "Ticket : l’objet expire malgré le bonus lorsque son délai finit");
        ItemPickup erased = GroundItem(new Vector2(4096, 0));
        typeof(ErasureManager).GetMethod("SetMemory", Private).Invoke(erasure,
            new object[] { new Vector2I(4096 / erasure.CellSize, 0), 0f, true });
        erased._Process(.3);
        Check(!erased.CanInteract, "Ticket : l’Effacement retire immédiatement l’objet, même prolongé");
        kept.QueueFree(); expired.QueueFree(); erased.QueueFree(); erasure.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private void CheckRealBell()
    {
        PackedScene scene = GD.Load<PackedScene>("res://scenes/combat/XpOrb.tscn");
        XpOrb near = scene.Instantiate<XpOrb>(), far = scene.Instantiate<XpOrb>(); AddChild(near); AddChild(far);
        near.SetRelease(_ => { }); far.SetRelease(_ => { });
        near.Launch(new Vector2(1000, 0), 3); far.Launch(new Vector2(2600, 0), 5);
        float gained = 0;
        void Gain(float amount) => gained += amount;
        _bus.XpGained += Gain;
        WorldItemEffects.AttractChestXp(_player, new Vector2(1000, 0));
        bool pulled = (bool)typeof(XpOrb).GetField("_trailBound", Private).GetValue(near);
        bool outside = (bool)typeof(XpOrb).GetField("_trailBound", Private).GetValue(far);
        Check(pulled && !outside && gained == 0, "Sonnette réelle x8 : attraction jusqu’à 1200 px, aucune XP créée");
        Vector2 before = near.GlobalPosition;
        near.PhysicsTick(.1);
        Check(near.GlobalPosition.DistanceSquaredTo(_player.GlobalPosition) < before.DistanceSquaredTo(_player.GlobalPosition),
            "Sonnette : l’orbe appelée se déplace réellement vers le joueur hors du rayon naturel");
        RemoveCopies("sonnette");
        typeof(XpOrb).GetField("_trailBound", Private).SetValue(far, false);
        WorldItemEffects.AttractChestXp(_player, far.GlobalPosition);
        Check(!(bool)typeof(XpOrb).GetField("_trailBound", Private).GetValue(far) && gained == 0,
            "Sonnette oubliée : aucune nouvelle attraction ni XP");
        _bus.XpGained -= Gain;
        near.QueueFree(); far.QueueFree();
    }

    private async Task CheckRealAddressBook()
    {
        ChestLootScreen loot = new(); AddChild(loot);
        ChoiceScreen choices = new(); AddChild(choices);
        WorldInteraction interaction = new(); AddChild(interaction); interaction.Setup(_player); interaction.Configure(loot, _essence, choices);
        ItemLootPool pool = ItemLootPool.For(_player);
        ulong seed = 1;
        for (; seed < 1000; seed++)
        {
            ChestRewardSession probe = new(pool, "chest_common", _player.LuckBonus, 1, 3, new RandomNumberGenerator { Seed = seed });
            if (probe.Propose().Count >= 2) break;
        }
        ((RandomNumberGenerator)typeof(WorldInteraction).GetField("_chestRng", Private).GetValue(interaction)).Seed = seed;
        Balance(20);
        _player.AddItemCopy("sonnette");
        XpOrb bellOrb = GD.Load<PackedScene>("res://scenes/combat/XpOrb.tscn").Instantiate<XpOrb>();
        AddChild(bellOrb); bellOrb.SetRelease(_ => { }); bellOrb.Launch(new Vector2(1010, 0), 3);
        Chest chest = Chest(); chest.GlobalPosition = new Vector2(1000, 0);
        typeof(WorldInteraction).GetMethod("OpenChest", Private).Invoke(interaction, new object[] { chest });
        Check(chest.IsOpened && choices.IsOpen && !loot.Visible && _essence.CurrentEssence == 0,
            "Carnet réel : paiement avant le choix, aucune attribution avant décision");
        IReadOnlyList<PassiveSouvenirData> proposals = chest.Rewards.Propose();
        string chosen = proposals[0].Id;
        int before = _player.FindObject(chosen)?.Copies ?? 0;
        bool sameRank = true;
        foreach (PassiveSouvenirData candidate in proposals) sameRank &= candidate.Rarity == proposals[0].Rarity;
        Check(proposals.Count >= 2 && proposals.Count <= 4 && sameRank, "Carnet : deux à quatre vrais objets distincts du même rang");
        choices.Activate(0); choices.Activate(0);
        Check(loot.Visible && !choices.IsOpen && chest.Rewards.Rewards.Count == 1 && !chest.Rewards.Applied,
            "Carnet : un seul choix conduit à la roulette, les alternatives ne sont pas attribuées");
        Check(!(bool)typeof(XpOrb).GetField("_trailBound", Private).GetValue(bellOrb),
            "Sonnette : la sélection du Carnet ne déclenche pas prématurément l’attraction");
        typeof(ChestLootScreen).GetMethod("Advance", Private).Invoke(loot, null);
        typeof(ChestLootScreen).GetMethod("Advance", Private).Invoke(loot, null);
        choices.Activate(0);
        Check(chest.Rewards.Applied && (_player.FindObject(chosen)?.Copies ?? 0) == before + 1 && _essence.CurrentEssence == 0,
            "Carnet : une copie reçue, ni double débit ni second choix après fermeture");
        Check((bool)typeof(XpOrb).GetField("_trailBound", Private).GetValue(bellOrb),
            "Sonnette : fermeture réelle de la roulette attire les orbes autour du coffre");
        bellOrb.QueueFree();
        loot.QueueFree(); choices.QueueFree(); interaction.QueueFree(); chest.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
}
