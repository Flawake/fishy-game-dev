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
            if (!ItemsInGame.idToTypeLut.TryGetValue(inv.item_id, out ItemType t))
            {
                Debug.LogWarning($"Unknown item id {inv.item_id}");
                continue;
            }

            ItemObject legacyObj = t switch
            {
                ItemType.Rod  => ItemObjectGenerator.RodObjectFromMinimal(inv.itemUuid, inv.item_id, inv.amount),
                ItemType.Bait => ItemObjectGenerator.BaitObjectFromMinimal(inv.itemUuid, inv.item_id, inv.amount),
                ItemType.Fish => ItemObjectGenerator.FishObjectFromMinimal(inv.itemUuid, inv.item_id, inv.amount),
                _ => null,
            };
            if (legacyObj == null) continue;
            ItemInstance inst = LegacyItemAdapter.From(legacyObj);
            if (inst != null) items.Add(inst);
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
            var existing = items.FirstOrDefault(i => i.def.Id == inst.def.Id);
            if (existing != null)
            {
                StackState exStack = existing.GetState<StackState>();
                exStack.currentAmount += stack.currentAmount;
                existing.SetState(exStack);
                return true;
            }
        }
        items.Add(inst);
        return false;
    }
}

