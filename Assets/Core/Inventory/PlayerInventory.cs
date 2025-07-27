using System;
using System.Collections.Generic;
using Mirror;
using System.Linq;
using UnityEngine;
using ItemSystem;

public class PlayerInventory : NetworkBehaviour
{
    // Unified container with the new runtime representation.
    private readonly List<ItemInstance> items = new();

    [SerializeField]
    PlayerData playerData;

    // ------------------------------------------------------------------
    // Inventory loading -------------------------------------------------
    // ------------------------------------------------------------------
    [Server]
    public void SaveInventory(UserData userData)
    {
        items.Clear();

        foreach (UserData.InventoryItem inv in userData.inventory_items)
        {
            ItemDefinition def = ItemRegistry.Get(inv.definition_id);
            if (def == null)
            {
                Debug.LogWarning($"Unknown item definition id {inv.definition_id}");
                continue;
            }
            var inst = new ItemInstance { def = def, uuid = inv.ItemUuid };
            if (!string.IsNullOrEmpty(inv.state_blob))
            {
                try
                {
                    byte[] stateBytes = Convert.FromBase64String(inv.state_blob);
                    StatePacker.UnpackInto(stateBytes, inst.state);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to unpack state for item {inv.item_uuid}: {e}");
                    continue;
                }
            }
            items.Add(inst);
            if (userData.SelectedRod == inst.uuid)
            {
                if (inst.def.GetBehaviour<RodBehaviour>() == null)
                {
                    continue;
                }
                playerData.SelectNewRod(inst, true);
            }
            if (userData.SelectedBait == inst.uuid)
            {
                if (inst.def.GetBehaviour<BaitBehaviour>() == null)
                {
                    continue;
                }
                playerData.SelectNewBait(inst, true);
            }
        }

        // Sync the initial inventory to the client
        TargetSyncInitialInventory(items.ToArray());
    }

    [TargetRpc]
    private void TargetSyncInitialInventory(ItemInstance[] inventoryItems)
    {
        items.Clear();
        items.AddRange(inventoryItems);
    }

    [TargetRpc]
    private void TargetTryMergeOrAdd(ItemInstance item)
    {
        TryMergeOrAdd(item);
    }

    [TargetRpc]
    private void TargetRemoveItem(Guid uuid)
    {
        items.RemoveAll(item => item.uuid == uuid);
    }

    // ------------------------------------------------------------------
    // CRUD helpers ------------------------------------------------------
    // ------------------------------------------------------------------

    [Server]
    public ItemInstance AddItem(ItemInstance inst)
    {
        if (inst == null) return null;
        ItemInstance result = TryMergeOrAdd(inst);
        
        // Sync the specific item change to the client
        TargetTryMergeOrAdd(inst);
        
        return result;
    }

    // Server-side method for confirming optimistic purchases (no RPC needed
    [Server]
    public ItemInstance AddItemConfirmPurchase(ItemInstance inst)
    {
        if (inst == null) return null;
        return TryMergeOrAdd(inst);
    }

    [Client]
    public ItemInstance ClientAddItem(ItemInstance inst)
    {
        if (inst == null) return null;
        return TryMergeOrAdd(inst);
    }

    [Server]
    public void RemoveItem(Guid uuid)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].uuid == uuid)
            {
                items.RemoveAt(i);
                break;
            }
        }
        
        // Sync the specific item removal to the client
        TargetRemoveItem(uuid);
    }

    public ItemInstance GetItem(Guid uuid)
    {
        return items.FirstOrDefault(i => i.uuid == uuid);
    }

    public List<ItemInstance> GetItems()
    {
        return items;
    }

    [Command]
    public void CmdGetInventory()
    {
        TargetSyncInitialInventory(items.ToArray());
    }

    public ItemInstance GetRodByUuid(Guid uuid)
    {
        return items.FirstOrDefault(i => i.uuid == uuid && i.HasBehaviour<RodBehaviour>());
    }

    public ItemInstance GetRodByDefinitionId(int id)
    {
        return items.FirstOrDefault(i => i.def.Id == id && i.HasBehaviour<RodBehaviour>());
    }

    public ItemInstance GetBaitByDefinitionId(int id)
    {
        return items.FirstOrDefault(i => i.def.Id == id && i.HasBehaviour<BaitBehaviour>());
    }

    private ItemInstance GetFirstNonFullStack(int definitionId)
    {
        return items.FirstOrDefault(item => 
            item.def.Id == definitionId && 
            item.GetState<StackState>()?.currentAmount < item.def.MaxStack);
    }

    ItemInstance TryMergeOrAdd(ItemInstance danglingItem)
    {
        if (danglingItem.def.GetBehaviour<DurabilityBehaviour>() == null)
        {
            // Look for the first non-full stack with the same definition ID
            ItemInstance itemRef = GetFirstNonFullStack(danglingItem.def.Id);
            if (itemRef == null)
            {
                items.Add(danglingItem);
                return danglingItem;
            }

            if (ServerMergeItem(itemRef, danglingItem))
            {
                return itemRef;
            }
        }
        items.Add(danglingItem);
        return danglingItem;
    }

    private bool ServerMergeItem(ItemInstance itemReference, ItemInstance danglingItem)
    {
        if (itemReference.def.Id != danglingItem.def.Id)
        {
            Debug.LogWarning("Items are not compatibel with each other");
            return false;
        }
        StackState stackStateReference = itemReference.GetState<StackState>();
        StackState stackStateDangling = danglingItem.GetState<StackState>();
        if (stackStateReference == null || stackStateDangling == null)
        {
            return false;
        }
        if (stackStateReference.currentAmount + stackStateDangling.currentAmount <= itemReference.def.MaxStack)
        {
            stackStateReference.currentAmount += stackStateDangling.currentAmount;
            itemReference.SetState(stackStateReference);
        }
        
        // Only call TargetRPC from server
        if (isServer)
        {
            TargetUpdateItem(itemReference);
        }
        
        return true;
    }

    [Server]
    public bool ServerTryUseItem(ItemInstance itemReference)
    {
        if (itemReference.def.InfiniteUse || itemReference.def.IsStatic)
        {
            return true;
        }
        DurabilityState durabilityState = itemReference.GetState<DurabilityState>();
        if (durabilityState == null)
        {
            Debug.LogWarning($"Could not use item with id {itemReference.def.Id} since it's durabilityState was null");
            return false;
        }
        durabilityState.remaining -= 1;
        itemReference.SetState(durabilityState);
        TargetUpdateItem(itemReference);
        return true;
    }

    [Server]
    public bool ServerConsumeFromStack(ItemInstance itemReference)
    {
        if (itemReference.def.InfiniteUse || itemReference.def.IsStatic)
        {
            return true;
        }
        StackState stackState = itemReference.GetState<StackState>();
        if (stackState == null)
        {
            Debug.LogWarning($"Could not use item with id {itemReference.def.Id} since it's durabilityState was null");
            return false;
        }
        stackState.currentAmount -= 1;
        itemReference.SetState(stackState);
        TargetUpdateItem(itemReference);
        return true;
    }

    [TargetRpc]
    private void TargetUpdateItem(ItemInstance danglingItem)
    {
        ItemInstance itemReference = GetItem(danglingItem.uuid);
        CopyState<StackState>(danglingItem, itemReference, (src, dst) => dst.currentAmount = src.currentAmount);
        CopyState<DurabilityState>(danglingItem, itemReference, (src, dst) => dst.remaining = src.remaining);
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

