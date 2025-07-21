using System;
using System.Collections.Generic;
using Mirror;
using System.Linq;
using UnityEditor;
using UnityEngine;
using NewItemSystem;

public class PlayerInventory : NetworkBehaviour
{
    // Unified container with the new runtime representation.
    public readonly SyncList<ItemInstance> items = new();

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
    }

    // ------------------------------------------------------------------
    // CRUD helpers ------------------------------------------------------
    // ------------------------------------------------------------------

    /// <summary>
    /// Interface to add item to player's inventory
    /// </summary>
    /// <param name="inst"></param>
    /// <returns>
    /// bool, stacked on existing item or not
    /// </returns>
    [Server]
    public ItemInstance AddItem(ItemInstance inst)
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
    }

    public ItemInstance GetItem(Guid uuid)
    {
        return items.FirstOrDefault(i => i.uuid == uuid);
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

    /// <summary>
    /// Adds an item to the players inventory
    /// </summary>
    /// <param name="inst"></param>
    /// <returns>
    /// The item that needs to be written int the db
    /// </returns>
    ItemInstance TryMergeOrAdd(ItemInstance inst)
    {
        // Exception for bait behaviour, work on durability instead of the stack
        if (inst.def.GetBehaviour<BaitBehaviour>() != null)
        {
            ItemInstance ret = ServerUpdateBaitDurability(inst);
            if (ret == inst)
            {
                items.Add(inst);
            }
            return ret;
        }
        StackState stack = inst.GetState<StackState>();
        if (stack != null && inst.def.MaxStack > 1)
        {
            // Just overstack for now if there is at least one object available to insert. To keep game and database in sync
            ItemInstance itemRef = items.First(i => i.def.Id == inst.def.Id && i.GetState<StackState>()?.currentAmount < i.def.MaxStack);
            if (itemRef != null)
            {
                itemRef.GetState<StackState>().currentAmount += inst.GetState<StackState>().currentAmount;
                RpcUpdateItemStack(itemRef);
                return itemRef;
            }
        }
        items.Add(inst);
        return inst;
    }

    [TargetRpc]
    private void RpcUpdateItemStack(ItemInstance replacement)
    {
        ItemInstance itemRef = items.First(i => i.def.Id == replacement.def.Id);
        itemRef.GetState<StackState>().currentAmount = replacement.GetState<StackState>().currentAmount;
    }

    [Server]
    public ItemInstance ServerUpdateBaitDurability(ItemInstance ghost)
    {
        TargetUpdateBaitDurability(ghost);
        return UpdateBaitDurability(ghost);
    }

    // Changes in items in a synclist are not updated, we need to trigger an update manually
    [TargetRpc]
    private void TargetUpdateBaitDurability(ItemInstance ghost)
    {

        UpdateBaitDurability(ghost);
    }

    private ItemInstance UpdateBaitDurability(ItemInstance ghost)
    {
        ItemInstance item = items.First(i => i.def.Id == ghost.def.Id);
        if (item == null)
        {
            Debug.Log("Not found");
            return ghost;
        }

        DurabilityState state = item.GetState<DurabilityState>();
        DurabilityState updatedState = ghost.GetState<DurabilityState>();

        if (state != null)
        {
            state.remaining += updatedState.remaining;
        }
        Debug.Log($"Updating state done: {GetItem(item.uuid).GetState<DurabilityState>().remaining}");
        return item;
    }
}

