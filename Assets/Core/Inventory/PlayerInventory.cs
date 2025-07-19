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
                }
            }
            items.Add(inst);
        }
    }

    // ------------------------------------------------------------------
    // CRUD helpers ------------------------------------------------------
    // ------------------------------------------------------------------
    [Server]
    public void AddItem(ItemInstance inst)
    {
        if (inst == null) return;
        TryMergeOrAdd(inst);
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
    
    bool TryMergeOrAdd(ItemInstance inst)
    {
        StackState stack = inst.GetState<StackState>();
        if (stack != null && stack.maxStack > 1)
        {
            int toAdd = stack.currentAmount;
            foreach (ItemInstance existing in items.Where(i => i.def.Id == inst.def.Id && i.GetState<StackState>()?.currentAmount < stack.maxStack).ToList())
            {
                StackState exStack = existing.GetState<StackState>();
                int space = exStack.maxStack - exStack.currentAmount;
                int add = Math.Min(space, toAdd);
                exStack.currentAmount += add;
                existing.SetState(exStack);
                toAdd -= add;
                if (toAdd <= 0) return true;
            }
            // If there is leftover, create new stacks as needed
            while (toAdd > 0)
            {
                int thisStack = Math.Min(stack.maxStack, toAdd);
                ItemInstance newStack = new ItemInstance(inst.def, thisStack);
                items.Add(newStack);
                toAdd -= thisStack;
            }
            return true;
        }
        items.Add(inst);
        return false;
    }
}

