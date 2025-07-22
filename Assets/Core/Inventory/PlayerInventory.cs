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
        if (inst.def.CanStack)
        {
            ItemInstance itemRef = items.FirstOrDefault(i => i.def.Id == inst.def.Id);
            if (itemRef == null)
            {
                items.Add(inst);
                return inst;
            }

            if (ServerMergeItem(itemRef, inst))
            {
                return itemRef;
            }

            return inst;
        }
        items.Add(inst);
        return inst;
    }

    [Server]
    private bool ServerMergeItem(ItemInstance original, ItemInstance ghost)
    {
        TargetMergeItem(original, ghost);
        return MergeItem(original, ghost);
    }

    [TargetRpc]
    private void TargetMergeItem(ItemInstance original, ItemInstance ghost)
    {
        // Original was send over the server. It is a new at this point, not a ref anymore
        ItemInstance itemRef = items.FirstOrDefault(i => i.def.Id == original.def.Id);
        if (itemRef == null)
        {
            Debug.LogWarning("Could not merge items");
            return;
        }

        MergeItem(itemRef, ghost);
    }

    private bool MergeItem(ItemInstance itemRef, ItemInstance ghost)
    {
        StackState stackState = itemRef.GetState<StackState>();
        if (stackState == null)
        {
            return false;
        }
        stackState.currentAmount += ghost.GetState<StackState>().currentAmount;
        itemRef.SetState(stackState);
        return true;
    }

    [Server]
    public void ServerReduceItemAmount(Guid itemUUID, int amount)
    {
        TargetReduceItemAmount(itemUUID, amount);
        ReduceItemAmount(itemUUID, amount);
    }

    [TargetRpc]
    void TargetReduceItemAmount(Guid itemUUID, int amount)
    {
        ReduceItemAmount(itemUUID, amount);
    }
    void ReduceItemAmount(Guid itemID, int amount)
    {
        ItemInstance item = GetItem(itemID);
        if (item == null)
        {
            Debug.LogWarning("Could not find a object that needs a amount deduced");
            return;
        }

        item.GetState<StackState>().currentAmount -= amount;
    }
}

