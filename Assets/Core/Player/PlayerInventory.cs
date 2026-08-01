using System;
using System.Collections.Generic;
using Mirror;
using System.Linq;
using UnityEngine;
using ItemSystem;
using FishyGame.Api;

public class PlayerInventory : NetworkBehaviour
{
    // Unified container with the new runtime representation.
    private readonly List<ItemInstance> items = new();

    [SerializeField]
    PlayerData playerData;

    // Bumped on the server for every change to the inventory. Every message that carries inventory
    // state to the client carries the generation that state belongs to, so a client can tell that a
    // change never reached it instead of quietly drifting apart from the server.
    private uint generation;

    // Client side counterpart: the generation the local inventory reflects.
    private uint lastKnownGeneration;

    public uint Generation => generation;
    public uint LastKnownGeneration => lastKnownGeneration;

    // ------------------------------------------------------------------
    // Inventory loading -------------------------------------------------
    // ------------------------------------------------------------------
    [Server]
    public void SaveInventory(UserData userData)
    {
        items.Clear();

        foreach (InventoryItem item in userData.inventory_items)
        {
            ItemDefinition def = ItemRegistry.Get(item.definition_id);
            if (def == null)
            {
                Debug.LogWarning($"Unknown item definition id {item.definition_id}");
                continue;
            }
            var itemInst = new ItemInstance { def = def, uuid = item.ItemUuid };
            if (item.durability != null)
            {
                itemInst.SetState(new DurabilityState {remaining = item.durability.durability});
            }
            if (item.stack != null)
            {
                itemInst.SetState(new StackState {currentAmount = item.stack.stack});
            }
            items.Add(itemInst);
        }

        // Select rod
        var selectedRod = items.FirstOrDefault(item =>
            item.uuid == userData.SelectedRod &&
            item.def.GetBehaviour<RodBehaviour>() != null);

        if (selectedRod != null)
        {
            playerData.SelectNewRod(selectedRod, true);
        }

        // Select bait
        var selectedBait = items.FirstOrDefault(item =>
            item.uuid == userData.SelectedBait &&
            item.def.GetBehaviour<BaitBehaviour>() != null);

        if (selectedBait != null)
        {
            playerData.SelectNewBait(selectedBait, true);
        }

        // Sync the initial inventory to the client
        TargetSyncInitialInventory(items.ToArray(), ServerBumpGeneration());
    }


    [TargetRpc]
    private void TargetSyncInitialInventory(ItemInstance[] inventoryItems, uint serverGeneration)
    {
        items.Clear();
        items.AddRange(inventoryItems);
        lastKnownGeneration = serverGeneration;
    }

    [TargetRpc]
    private void TargetApplyAuthoritative(ItemInstance[] authoritativeItems, uint serverGeneration)
    {
        ApplyAuthoritative(authoritativeItems, serverGeneration);
    }

    [TargetRpc]
    private void TargetRemoveItem(Guid uuid, uint serverGeneration)
    {
        items.RemoveAll(item => item.uuid == uuid);
        TrackGeneration(serverGeneration);
    }

    // ------------------------------------------------------------------
    // CRUD helpers ------------------------------------------------------
    // ------------------------------------------------------------------

	[Server]
	public bool ServerMergeOrAdd(DeltaItem inst, bool needsTargetSync)
	{
		return ServerMergeOrAdd(inst, needsTargetSync, out _);
	}

	[Server]
	public bool ServerMergeOrAdd(DeltaItem inst, bool needsTargetSync, out InventoryChange change)
	{
		uint generationBefore = generation;
		if (!TryMergeOrAdd(inst, out change))
		{
			return false;
		}

		change.GenerationBefore = generationBefore;
		change.GenerationAfter = ServerBumpGeneration();

		if (needsTargetSync)
		{
			TargetApplyAuthoritative(SnapshotOf(change), change.GenerationAfter);
		}
		return true;
	}

	[Server]
	public bool ServerApplyDelta(DeltaItem delta)
	{
		if (!ApplyDelta(delta))
		{
			return false;
		}

		ItemInstance applied = GetItem(delta.ItemUUID);
		uint syncedGeneration = ServerBumpGeneration();
		if (applied == null)
		{
			TargetRemoveItem(delta.ItemUUID, syncedGeneration);
		}
		else
		{
			TargetApplyAuthoritative(new[] { applied }, syncedGeneration);
		}
		return true;
	}

	[Server]
	public void RemoveItem(Guid uuid, bool needsTargetSync)
	{
		items.RemoveAll(item => item.uuid == uuid);

		// Sync the specific item removal to the client
        if (needsTargetSync)
        {
            TargetRemoveItem(uuid, ServerBumpGeneration());
        }
	}

    [Server]
    private uint ServerBumpGeneration()
    {
        generation++;
        return generation;
    }

    // ------------------------------------------------------------------
    // Reconciliation ----------------------------------------------------
    // ------------------------------------------------------------------

    /// <summary>
    /// Replays inventory state the server has already settled on, keyed purely by uuid. Nothing is
    /// merged here: a stack we know about is overwritten with the server's version of it and a stack
    /// we do not know about is added. That makes this safe to apply twice and self healing for the
    /// stacks it covers, which is why the grant flow reconciles through it instead of trying to
    /// reproduce the server's merge locally.
    /// </summary>
    public void ApplyAuthoritative(IReadOnlyList<ItemInstance> authoritativeItems, uint serverGeneration)
    {
        if (authoritativeItems != null)
        {
            foreach (ItemInstance authoritative in authoritativeItems)
            {
                if (authoritative == null)
                {
                    continue;
                }

                ItemInstance local = GetItem(authoritative.uuid);
                if (local == null)
                {
                    items.Add(authoritative);
                    continue;
                }

                // Updated in place rather than swapped out, because the UI holds references to it.
                local.def = authoritative.def;
                local.state.Clear();
                foreach (KeyValuePair<Type, IRuntimeBehaviourState> state in authoritative.state)
                {
                    local.state[state.Key] = state.Value;
                }
            }
        }

        TrackGeneration(serverGeneration);
    }

    /// <summary>
    /// Applies a delta to the stack it names, creating that stack when it is not there. Decides
    /// nothing of its own: where <see cref="TryMergeOrAdd(DeltaItem, out InventoryChange)"/> works out
    /// which stacks an amount should land in, this one is told, which is what applying a row the
    /// database already holds needs.
    /// </summary>
    public bool ApplyDelta(DeltaItem delta)
    {
        if (delta == null)
        {
            return false;
        }

        StackState gained = delta.GetState<StackState>();
        if (gained == null)
        {
            Debug.LogWarning($"Cannot apply a delta for item {delta.ItemDefinition?.Id} without a stack state");
            return false;
        }

        ItemInstance existing = GetItem(delta.ItemUUID);
        if (existing == null)
        {
            if (gained.currentAmount <= 0)
            {
                Debug.LogWarning($"Cannot apply {gained.currentAmount} to unknown item {delta.ItemUUID}");
                return false;
            }
            items.Add(delta.IntoItemInstance(false));
            return true;
        }

        StackState stack = existing.GetState<StackState>();
        if (stack == null)
        {
            Debug.LogWarning($"Item {delta.ItemUUID} has no stack state to apply a delta to");
            return false;
        }

        stack.currentAmount += gained.currentAmount;
        existing.SetState(stack);
        if (stack.currentAmount <= 0)
        {
            items.Remove(existing);
        }
        return true;
    }

    // Records the generation the local inventory now reflects.
    public void TrackGeneration(uint serverGeneration)
    {
        if (serverGeneration < lastKnownGeneration)
        {
            Debug.LogWarning($"Ignoring inventory generation {serverGeneration}, already at {lastKnownGeneration}");
            return;
        }
        lastKnownGeneration = serverGeneration;
    }

    // The current state of every stack a change touched, to be shipped to a client as truth.
    public ItemInstance[] SnapshotOf(InventoryChange change)
    {
        if (change == null)
        {
            return Array.Empty<ItemInstance>();
        }

        return change.All
            .Select(delta => GetItem(delta.ItemUUID))
            .Where(item => item != null)
            .ToArray();
    }

    /// <summary>
    /// Undoes a change this inventory made: the exact inverse of the add that produced it. Stacks the
    /// change created go away whole, stacks it merged into give back only what it put in.
    ///
    /// This is how an optimistic add is taken back. It is keyed by what was recorded at the time, so
    /// it does not care what the server decided, and it holds regardless of the order changes are
    /// reverted in as long as nothing else merged into the stacks the change created.
    /// </summary>
    public void Revert(InventoryChange change)
    {
        if (change == null)
        {
            return;
        }

        foreach (DeltaItem created in change.Created)
        {
            RemoveLocal(created.ItemUUID);
        }

        foreach (DeltaItem merged in change.Merged)
        {
            if (merged.GetState<StackState>() is { } gained)
            {
                SubtractFromStack(merged.ItemUUID, gained.currentAmount);
            }
        }
    }

    // Removes an item without telling anyone: either the server just told us to, or we are taking
    // back something that only ever existed locally.
    public void RemoveLocal(Guid uuid)
    {
        items.RemoveAll(item => item.uuid == uuid);
    }

    // Takes an amount back out of a stack, dropping the stack when nothing is left. Used to roll an
    // optimistic add back out of the local inventory.
    public void SubtractFromStack(Guid uuid, int amount)
    {
        ItemInstance local = GetItem(uuid);
        if (local == null)
        {
            Debug.LogWarning($"Cannot take {amount} back out of unknown item {uuid}");
            return;
        }

        StackState stack = local.GetState<StackState>();
        if (stack == null)
        {
            items.Remove(local);
            return;
        }

        stack.currentAmount -= amount;
        local.SetState(stack);
        if (stack.currentAmount <= 0)
        {
            items.Remove(local);
        }
    }

    public ItemInstance GetItem(Guid uuid)
    {
        return items.FirstOrDefault(i => i.uuid == uuid);
    }

    public List<ItemInstance> GetItems()
    {
        return items;
    }

    public List<ItemInstance> GetFishes()
    {
        return items.Where(i => i.def.GetBehaviour<FishBehaviour>() != null).ToList();
    }

    [Command]
    public void CmdGetInventory()
    {
        TargetSyncInitialInventory(items.ToArray(), generation);
    }

    public ItemInstance GetRodByUuid(Guid uuid)
    {
        return items.FirstOrDefault(i => i.uuid == uuid && i.def.HasBehaviour<RodBehaviour>());
    }

    public ItemInstance GetRodByDefinitionId(int id)
    {
        return items.FirstOrDefault(i => i.def.Id == id && i.def.HasBehaviour<RodBehaviour>());
    }

    public ItemInstance GetBaitByDefinitionId(int id)
    {
        return items.FirstOrDefault(i => i.def.Id == id && i.def.HasBehaviour<BaitBehaviour>());
    }

    public ItemInstance GetFishByDefinitionId(int id)
    {
        return items.FirstOrDefault(i => i.def.Id == id && i.def.HasBehaviour<FishBehaviour>());
    }

    public List<ItemInstance> GetItemStacks(int definitionId)
    {
        return items.Where(item =>
            item.def.Id == definitionId &&
            item.GetState<StackState>() != null).ToList();
    }

    public List<ItemInstance> GetNonFullStacks(int definitionId)
    {
        return GetNonFullStacks(definitionId, null);
    }

    /// <param name="ignoredUuids">
    /// Stacks that must not be merged into, or null for none. The optimistic path passes the stacks
    /// that grants still in flight created, so every grant owns what it created and can take it back
    /// untouched when the server turns it down.
    /// </param>
    public List<ItemInstance> GetNonFullStacks(int definitionId, ICollection<Guid> ignoredUuids)
    {
        return items.Where(item =>
            item.def.Id == definitionId &&
            (ignoredUuids == null || !ignoredUuids.Contains(item.uuid)) &&
            item.GetState<StackState>() is { } stack &&
            stack.currentAmount < MaxStackOf(item.def)).ToList();
    }

    public bool TryMergeOrAdd(DeltaItem deltaItem, out InventoryChange change)
    {
        return TryMergeOrAdd(deltaItem, null, out change);
    }

    /// <param name="ignoredUuids">See <see cref="GetNonFullStacks(int, ICollection{Guid})"/>.</param>
    public bool TryMergeOrAdd(DeltaItem deltaItem, ICollection<Guid> ignoredUuids, out InventoryChange change)
    {
        change = null;
        if (deltaItem == null)
        {
            return false;
        }

        StackState added = deltaItem.GetState<StackState>();
        if (added == null)
        {
            Debug.LogWarning($"Cannot add item {deltaItem.ItemDefinition?.Id} without a stack state");
            return false;
        }
        if (added.currentAmount <= 0)
        {
            Debug.LogWarning($"Cannot add {added.currentAmount} of item {deltaItem.ItemDefinition?.Id}");
            return false;
        }

        // Items that wear out are never stacked, so they never have anything to merge into.
        List<ItemInstance> mergeTargets = deltaItem.ItemDefinition.GetBehaviour<DurabilityBehaviour>() == null
            ? GetNonFullStacks(deltaItem.ItemDefinition.Id, ignoredUuids)
            : new List<ItemInstance>();

        change = new InventoryChange();
        return Distribute(deltaItem, added.currentAmount, mergeTargets, change);
    }

    /// <summary>
    /// Fills the stacks the delta can merge into, in order, and spills into new stacks once those are
    /// full. Every stack it touches is recorded in <paramref name="change"/> with the amount it
    /// gained, which is what makes the whole thing reversible afterwards.
    /// </summary>
    private bool Distribute(DeltaItem deltaItem, int amount, List<ItemInstance> mergeTargets, InventoryChange change)
    {
        if (mergeTargets.Any(item => item.def.Id != deltaItem.ItemDefinition.Id))
        {
            Debug.LogWarning("Items are not compatible with each other");
            return false;
        }

        int maxStack = MaxStackOf(deltaItem.ItemDefinition);
        int remaining = amount;
        int nextTarget = 0;
        // The first stack this has to create inherits the delta's uuid; anything past that is a stack
        // the delta never described, so it needs one of its own.
        bool reuseDeltaUuid = true;

        while (remaining > 0)
        {
            if (nextTarget < mergeTargets.Count)
            {
                ItemInstance target = mergeTargets[nextTarget++];
                StackState stack = target.GetState<StackState>();
                int fillAmount = Mathf.Min(maxStack - stack.currentAmount, remaining);
                if (fillAmount <= 0)
                {
                    continue;
                }

                stack.currentAmount += fillAmount;
                target.SetState(stack);
                remaining -= fillAmount;
                change.AddMerged(RecordDelta(target, fillAmount));
                continue;
            }

            int stackAmount = Mathf.Min(maxStack, remaining);
            ItemInstance created = deltaItem.IntoItemInstance(!reuseDeltaUuid);
            reuseDeltaUuid = false;
            created.SetState(new StackState { currentAmount = stackAmount });
            items.Add(created);
            remaining -= stackAmount;
            change.AddCreated(RecordDelta(created, stackAmount));
        }

        return true;
    }

    // Records what a stack gained, with a stack state of its own so that later changes to that stack
    // cannot rewrite the record.
    private static DeltaItem RecordDelta(ItemInstance stack, int gainedAmount)
    {
        DeltaItem delta = DeltaItem.FromItemInstance(stack);
        delta.SetState(new StackState { currentAmount = gainedAmount });
        return delta;
    }

    private static int MaxStackOf(ItemDefinition definition)
    {
        return Mathf.Max(1, definition.MaxStack);
    }

    [Server]
    public bool ServerTryUseItem(ItemInstance itemReference, out DeltaItem deltaItem)
    {
        if (itemReference.def.InfiniteUse || itemReference.def.IsStatic)
        {
            deltaItem = null;
            return true;
        }
        DurabilityState durabilityState = itemReference.GetState<DurabilityState>();
        if (durabilityState == null)
        {
            Debug.LogWarning($"Could not use item with id {itemReference.def.Id} since it's durabilityState was null");
            deltaItem = null;
            return false;
        }
        durabilityState.remaining -= 1;
        itemReference.SetState(durabilityState);
        TargetUpdateItem(itemReference, ServerBumpGeneration());

        // The delta describes the change rather than the new total, and gets a state object of its
        // own: mutating the live one to build it would leave the item itself holding the delta.
        deltaItem = DeltaItem.FromItemInstance(itemReference);
        deltaItem.SetState(new DurabilityState { remaining = -1 });
        return true;
    }

    [Server]
	public bool ServerRemoveAmountFromStack(int itemID, int amount, bool needsTargetSync, out InventoryChange updatedItems)
	{
        updatedItems = new();
        if (amount <= 0)
        {
            Debug.LogWarning($"Cannot remove {amount} of item {itemID}");
            return false;
        }

        List<ItemInstance> stacks = GetItemStacks(itemID);
        int available = stacks.Sum(stack => stack.GetState<StackState>().currentAmount);
        if (available < amount)
        {
            // Checked before anything is taken: the stacks are drained in place below, so noticing
            // the shortfall halfway through would leave the inventory partly emptied with no record
            // of what to hand back.
            Debug.LogWarning($"Cannot remove {amount} of item {itemID}, only {available} held");
            return false;
        }

        int remaining = amount;
        foreach (ItemInstance stack in stacks)
        {
            if (remaining <= 0)
            {
                break;
            }

            int stackAmount = stack.GetState<StackState>().currentAmount;
            if (remaining >= stackAmount)
            {
                // The whole stack goes, so the delta gives back everything it held.
                DeltaItem removed = DeltaItem.FromItemInstance(stack);
                removed.SetState(new StackState { currentAmount = -stackAmount });
                updatedItems.AddMerged(removed);
                RemoveItem(stack.uuid, needsTargetSync);
            }
            else
            {
                ServerRemoveAmountFromSpecifiedStack(stack, remaining, needsTargetSync, out InventoryChange updatedItem);
                // Null when the stack is infinite use or static: nothing was taken, nothing to record.
                if (updatedItem != null)
                {
                    updatedItems.MergeChanges(updatedItem);
                }
            }
            remaining -= stackAmount;
        }
        return true;
	}

    [Server]
	public bool ServerRemoveAmountFromSpecifiedStack(ItemInstance itemReference, int amount, bool needsTargetSync, out InventoryChange change)
	{
        if (!RemoveAmountFromStack(itemReference, amount, out change))
        {
            return false;
        }
        ServerBumpGeneration();
        if (needsTargetSync)
        {
            TargetUpdateItem(itemReference, generation);
        }
        return true;
	}

    public bool RemoveAmountFromStack(ItemInstance itemReference, int amount, out InventoryChange change)
    {
        if (itemReference.def.InfiniteUse || itemReference.def.IsStatic)
        {
            change = null;
            return true;
        }
        StackState stackState = itemReference.GetState<StackState>();
        if (stackState == null)
        {
            Debug.LogWarning($"Could not remove from id {itemReference.def.Id} since it's stackState was null");
            change = null;
            return false;
        }
        stackState.currentAmount -= amount;
        itemReference.SetState(stackState);

        // As above: a state object of its own, so that recording the change cannot corrupt the stack
        // the change was made to.
        DeltaItem deltaItem = DeltaItem.FromItemInstance(itemReference);
        deltaItem.SetState(new StackState { currentAmount = -amount });
        change = new();
        change.AddMerged(deltaItem);
        return true;
    }

    [TargetRpc]
    private void TargetUpdateItem(ItemInstance danglingItem, uint serverGeneration)
    {
        ItemInstance itemReference = GetItem(danglingItem.uuid);
        CopyState<StackState>(danglingItem, itemReference, (src, dst) => dst.currentAmount = src.currentAmount);
        CopyState<DurabilityState>(danglingItem, itemReference, (src, dst) => dst.remaining = src.remaining);
        TrackGeneration(serverGeneration);
    }

    private void CopyState<T>(ItemInstance from, ItemInstance to, Action<T, T> copyAction)
    where T : class, IRuntimeBehaviourState
    {
        T source = from.GetState<T>();
        T target = to.GetState<T>();

        if (source != null && target != null)
        {
            copyAction(source, target);
        }
        from.SetState(source);
    }

}
