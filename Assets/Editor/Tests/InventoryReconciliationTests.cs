using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ItemSystem;
using Mirror;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Covers the arithmetic the item grant flow rests on, without any networking.
///
/// A client adds an item optimistically, the server adds the same item to its own copy of the
/// inventory, and the client then reverts its guess and takes the server's word for the stacks that
/// changed. The point of these tests is that the last step converges even when the two sides split
/// the grant across stacks completely differently, because that is the assumption the old
/// index-matched reconciliation quietly depended on.
/// </summary>
public class InventoryReconciliationTests
{
    const int DefinitionId = 42;
    const int MaxStack = 100;

    readonly List<GameObject> spawnedObjects = new List<GameObject>();
    readonly List<ScriptableObject> createdAssets = new List<ScriptableObject>();

    ItemDefinition definition;

    [SetUp]
    public void SetUp()
    {
        definition = CreateDefinition(DefinitionId, MaxStack);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject spawned in spawnedObjects)
        {
            UnityEngine.Object.DestroyImmediate(spawned);
        }
        spawnedObjects.Clear();

        foreach (ScriptableObject asset in createdAssets)
        {
            UnityEngine.Object.DestroyImmediate(asset);
        }
        createdAssets.Clear();
    }

    [Test]
    public void Reverting_an_add_restores_the_inventory_exactly()
    {
        PlayerInventory inventory = CreateInventory();
        Seed(inventory, BaseStacks(60, 95, 100));
        Dictionary<Guid, int> before = Snapshot(inventory);

        Assert.IsTrue(inventory.TryMergeOrAdd(new DeltaItem(definition, 250), out InventoryChange change));
        Assert.AreNotEqual(before, Snapshot(inventory), "the add should have changed something");

        inventory.Revert(change);

        Assert.AreEqual(before, Snapshot(inventory));
    }

    [Test]
    public void Reverting_leaves_nothing_behind_when_the_inventory_started_empty()
    {
        PlayerInventory inventory = CreateInventory();

        Assert.IsTrue(inventory.TryMergeOrAdd(new DeltaItem(definition, 30), out InventoryChange change));
        inventory.Revert(change);

        Assert.IsEmpty(inventory.GetItems());
    }

    [Test]
    public void Reconciling_converges_even_when_the_two_sides_split_the_grant_differently()
    {
        List<ItemInstance> baseStacks = BaseStacks(50, 90);
        PlayerInventory client = CreateInventory();
        PlayerInventory server = CreateInventory();
        Seed(client, baseStacks);
        Seed(server, baseStacks);

        // Force the client into a different split than the server: it refuses to merge into the stack
        // the server will fill first, so it spills into a stack of its own instead.
        HashSet<Guid> ignored = new HashSet<Guid> { baseStacks[0].uuid };
        Assert.IsTrue(client.TryMergeOrAdd(new DeltaItem(definition, 120), ignored, out InventoryChange clientChange));
        Assert.IsTrue(server.TryMergeOrAdd(new DeltaItem(definition, 120), out InventoryChange serverChange));

        Assert.AreNotEqual(
            clientChange.All.Count + clientChange.Created.Count,
            serverChange.All.Count + serverChange.Created.Count,
            "the test is pointless unless the two sides really did split the grant differently");

        Reconcile(client, clientChange, server, serverChange);

        Assert.AreEqual(Snapshot(server), Snapshot(client));
    }

    [Test]
    public void Reconciling_converges_when_the_client_never_saw_an_earlier_server_change()
    {
        List<ItemInstance> baseStacks = BaseStacks(50);
        PlayerInventory client = CreateInventory();
        PlayerInventory server = CreateInventory();
        Seed(client, baseStacks);
        Seed(server, baseStacks);

        // The server grew a stack the client still thinks holds 50. This is the case that used to line
        // up by index and then silently disagree on amounts.
        server.GetItem(baseStacks[0].uuid).GetState<StackState>().currentAmount = 80;

        Assert.IsTrue(client.TryMergeOrAdd(new DeltaItem(definition, 40), out InventoryChange clientChange));
        Assert.IsTrue(server.TryMergeOrAdd(new DeltaItem(definition, 40), out InventoryChange serverChange));

        Reconcile(client, clientChange, server, serverChange);

        // Every stack the grant touched now matches the server exactly, including the one the client
        // was wrong about before the grant.
        foreach (DeltaItem touched in serverChange.All)
        {
            Assert.AreEqual(
                StackAmount(server.GetItem(touched.ItemUUID)),
                StackAmount(client.GetItem(touched.ItemUUID)),
                $"stack {touched.ItemUUID} did not converge");
        }
    }

    [Test]
    public void Randomised_grants_keep_the_two_sides_in_step()
    {
        System.Random random = new System.Random(20260801);

        List<ItemInstance> baseStacks = BaseStacks(10, 55, 100);
        PlayerInventory client = CreateInventory();
        PlayerInventory server = CreateInventory();
        Seed(client, baseStacks);
        Seed(server, baseStacks);

        int expectedTotal = Snapshot(server).Values.Sum();

        for (int round = 0; round < 200; round++)
        {
            int amount = random.Next(1, 260);

            // Half the rounds the client is told to avoid a stack the server will happily use, so the
            // two sides disagree about the split as often as not.
            HashSet<Guid> ignored = new HashSet<Guid>();
            List<ItemInstance> clientStacks = client.GetItems();
            if (random.Next(2) == 0 && clientStacks.Count > 0)
            {
                ignored.Add(clientStacks[random.Next(clientStacks.Count)].uuid);
            }

            Assert.IsTrue(client.TryMergeOrAdd(new DeltaItem(definition, amount), ignored, out InventoryChange clientChange), $"round {round}");
            Assert.IsTrue(server.TryMergeOrAdd(new DeltaItem(definition, amount), out InventoryChange serverChange), $"round {round}");

            Reconcile(client, clientChange, server, serverChange);

            expectedTotal += amount;
            Assert.AreEqual(expectedTotal, Snapshot(server).Values.Sum(), $"server lost units in round {round}");
            Assert.AreEqual(expectedTotal, Snapshot(client).Values.Sum(), $"client lost units in round {round}");

            foreach (DeltaItem touched in serverChange.All)
            {
                Assert.AreEqual(
                    StackAmount(server.GetItem(touched.ItemUUID)),
                    StackAmount(client.GetItem(touched.ItemUUID)),
                    $"stack {touched.ItemUUID} diverged in round {round}");
            }

            Assert.IsFalse(
                server.GetItems().Any(item => StackAmount(item) > MaxStack),
                $"a server stack went over its maximum in round {round}");
        }
    }

    [Test]
    public void Applying_the_same_authoritative_state_twice_changes_nothing()
    {
        PlayerInventory client = CreateInventory();
        PlayerInventory server = CreateInventory();
        List<ItemInstance> baseStacks = BaseStacks(75);
        Seed(client, baseStacks);
        Seed(server, baseStacks);

        Assert.IsTrue(server.TryMergeOrAdd(new DeltaItem(definition, 60), out InventoryChange serverChange));
        ItemInstance[] authoritative = server.SnapshotOf(serverChange);

        client.ApplyAuthoritative(CloneAll(authoritative), 1);
        Dictionary<Guid, int> once = Snapshot(client);
        client.ApplyAuthoritative(CloneAll(authoritative), 1);

        Assert.AreEqual(once, Snapshot(client));
        Assert.AreEqual(Snapshot(server), Snapshot(client));
    }

    [Test]
    public void An_add_of_nothing_is_refused()
    {
        PlayerInventory inventory = CreateInventory();

        Assert.IsFalse(inventory.TryMergeOrAdd(new DeltaItem(definition, 0), out _));
        Assert.IsEmpty(inventory.GetItems());
    }

    // ------------------------------------------------------------------
    // Helpers -----------------------------------------------------------
    // ------------------------------------------------------------------

    /// <summary>
    /// What the client does when a grant is confirmed: drop its own guess, then take the server's word
    /// for the stacks the server actually touched.
    /// </summary>
    static void Reconcile(PlayerInventory client, InventoryChange clientChange, PlayerInventory server, InventoryChange serverChange)
    {
        client.Revert(clientChange);
        client.ApplyAuthoritative(CloneAll(server.SnapshotOf(serverChange)), 0);
    }

    PlayerInventory CreateInventory()
    {
        GameObject host = new GameObject("PlayerInventory");
        spawnedObjects.Add(host);
        host.AddComponent<NetworkIdentity>();
        return host.AddComponent<PlayerInventory>();
    }

    // Stacks with the given amounts, to be handed to more than one inventory as a shared starting point.
    List<ItemInstance> BaseStacks(params int[] amounts)
    {
        return amounts
            .Select(amount => new ItemInstance(definition, amount))
            .ToList();
    }

    static void Seed(PlayerInventory inventory, List<ItemInstance> stacks)
    {
        inventory.ApplyAuthoritative(CloneAll(stacks), 0);
    }

    // Inventories must never share instances, or a change to one would show up in the other and the
    // tests would pass for the wrong reason.
    static List<ItemInstance> CloneAll(IEnumerable<ItemInstance> stacks)
    {
        return stacks.Select(Clone).ToList();
    }

    static ItemInstance Clone(ItemInstance item)
    {
        ItemInstance clone = new ItemInstance { uuid = item.uuid, def = item.def };
        foreach (KeyValuePair<Type, IRuntimeBehaviourState> state in item.state)
        {
            clone.state[state.Key] = state.Value switch
            {
                StackState stack => new StackState { currentAmount = stack.currentAmount },
                DurabilityState durability => new DurabilityState { remaining = durability.remaining },
                _ => state.Value
            };
        }
        return clone;
    }

    static Dictionary<Guid, int> Snapshot(PlayerInventory inventory)
    {
        return inventory.GetItems().ToDictionary(item => item.uuid, StackAmount);
    }

    static int StackAmount(ItemInstance item)
    {
        return item?.GetState<StackState>()?.currentAmount ?? 0;
    }

    ItemDefinition CreateDefinition(int id, int maxStack)
    {
        ItemDefinition created = ScriptableObject.CreateInstance<ItemDefinition>();
        createdAssets.Add(created);
        SetPrivateField(created, "id", id);
        SetPrivateField(created, "maxStack", maxStack);
        SetPrivateField(created, "displayName", $"Test item {id}");
        return created;
    }

    static void SetPrivateField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"{target.GetType().Name} has no field called {name}");
        field.SetValue(target, value);
    }
}
