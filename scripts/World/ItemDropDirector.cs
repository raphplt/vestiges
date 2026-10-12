using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Objets lâchés dans le monde (plan 35 I3) : une élite en laisse un parfois, un Souverain toujours. L'objet tombe au
/// sol (<see cref="ItemPickup"/>), tiré parmi les objets débloqués. Réglages : section <c>drops</c> d'inventory.json.
/// </summary>
public partial class ItemDropDirector : Node
{
    private EventBus _eventBus;
    private ItemExchange _exchange;
    private Node _container;
    private GroupCache _groupCache;
    // Flux à part : les élites abattues ne décalent pas le contenu des coffres.
    private readonly RandomNumberGenerator _rng = RunRandom.Create("item_drops");

    public void Setup(ItemExchange exchange, Node container)
    {
        _exchange = exchange;
        _container = container;
    }

    public override void _Ready()
    {
        _groupCache = GetNodeOrNull<GroupCache>("/root/GroupCache");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EnemyKillResolved += OnEnemyKill;
        _eventBus.ItemDropRequested += OnItemDropRequested;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.EnemyKillResolved -= OnEnemyKill;
            _eventBus.ItemDropRequested -= OnItemDropRequested;
        }
    }

    private void OnItemDropRequested(string itemId, Vector2 position) =>
        Drop(position, PassiveSouvenirDataLoader.Get(itemId));

    private void OnEnemyKill(EnemyKillResult kill)
    {
        int count = kill.Sovereign ? InventoryConfig.SovereignItems
            : kill.Elite && _rng.Randf() < InventoryConfig.EliteItemChance ? 1 : 0;
        for (int i = 0; i < count; i++)
            Drop(kill.Position + new Vector2(18f * i, 0f), LootRewards.PickUnlockedItem(_groupCache?.GetPlayer() as Player,
                _rng, source: kill.Sovereign ? "sovereign" : "elite"));
    }

    /// <summary>Pose un objet débloqué que le joueur peut encore cumuler, au hasard ; rien si aucun ne l'est.</summary>
    public ItemPickup Drop(Vector2 position) =>
        Drop(position, LootRewards.PickUnlockedItem(_groupCache?.GetPlayer() as Player, _rng));

    /// <summary>Pose cet objet au sol.</summary>
    public ItemPickup Drop(Vector2 position, PassiveSouvenirData item)
    {
        if (item == null || _container == null)
            return null;
        ItemPickup pickup = new() { Name = "ItemPickup" };
        pickup.Initialize(item, position, _exchange);
        _container.CallDeferred(Node.MethodName.AddChild, pickup);
        return pickup;
    }
}
