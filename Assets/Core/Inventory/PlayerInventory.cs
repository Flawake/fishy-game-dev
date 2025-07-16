using System;
using System.Collections.Generic;
using Mirror;
using System.Linq;
using UnityEditor;
using UnityEngine;
using NewItemSystem;

public class PlayerInventory : NetworkBehaviour
{
    //Divide the containers for faster accessing
    public readonly SyncList<InventoryItem> miscContainer = new();
    public readonly SyncList<InventoryItem> rodContainer = new();
    public readonly SyncList<InventoryItem> baitContainer = new();
    public readonly SyncList<InventoryItem> fishContainer = new();
    public readonly SyncList<ItemInstance> instances = new(); // NEW unified container

    [SerializeField]
    PlayerData playerData;

    [Server]
    public void SaveInventory(UserData userData)
    {
        // NEW: clear ItemInstance container
        instances.Clear();
        miscContainer.Clear();
        rodContainer.Clear();
        baitContainer.Clear();
        fishContainer.Clear();

        foreach (UserData.InventoryItem item in userData.inventory_items)
        {
            if (ItemsInGame.idToTypeLut.TryGetValue(item.item_id, out var itemType))
            {
                switch (itemType) 
                {
                    case ItemType.Rod:
                        rodObject inventoryRod = ItemObjectGenerator.RodObjectFromMinimal(item.itemUuid, item.item_id, item.amount);
                        if(inventoryRod == null)
                        {
                            Debug.LogWarning($"Trying to create a rod from id: {item.item_id} failed");
                            continue;
                        }
                        AddItem(inventoryRod);

                        // mirror → ItemInstance
                        TryAddInstance(LegacyItemAdapter.From(inventoryRod));

                        //TODO: find a better place for this.
                        if (inventoryRod.uuid == userData.SelectedRod)
                        {
                            playerData.SelectNewRod(inventoryRod, true);
                        }
                        break;
                    case ItemType.Bait:
                        baitObject inventoryBait = ItemObjectGenerator.BaitObjectFromMinimal(item.itemUuid, item.item_id, item.amount);
                        if(inventoryBait == null)
                        {
                            Debug.LogWarning($"Tried to create a bait from id: {item.item_id} failed");
                            continue;
                        }
                        AddItem(inventoryBait);

                        TryAddInstance(LegacyItemAdapter.From(inventoryBait));
    
                        //TODO: find a better place for this.
                        if (inventoryBait.uuid == userData.SelectedBait)
                        {
                            playerData.SelectNewBait(inventoryBait, true);
                        }
                       break;
                    case ItemType.Fish:
                        FishObject inventoryFish = ItemObjectGenerator.FishObjectFromMinimal(item.itemUuid, item.item_id, item.amount);
                        if (inventoryFish == null)
                        {
                            Debug.LogWarning($"Tried to create a fish from id: {item.item_id} failed");
                            continue;
                        }
    
                        AddItem(inventoryFish);
                        TryAddInstance(LegacyItemAdapter.From(inventoryFish));
                        break;
                    case ItemType.Extra:
                        Debug.LogWarning($"item {itemType} can not yet be added to the inventory");
                        break;
                    default:
                        Debug.LogError($"item {itemType} not recognised");
                        break;
                }
            }
            else
            {
                Debug.LogWarning($"Could not find an item with id {item.item_id}");
            }
        }
    }

    // --------------------------------------------------------------
    // NEW helpers working with ItemInstance container
    // --------------------------------------------------------------
    void TryAddInstance(ItemInstance inst) {
        if (inst == null) return;

        // VERY simple merge: if stackable & same definition id and no uuid (stackable items
        // use Guid.Empty in legacy), merge stack amounts; else push as new instance.
        if (inst.GetState<StackState>()?.maxStack > 1) {
            for (int i = 0; i < instances.Count; i++) {
                if (instances[i].def.Id == inst.def.Id) {
                    var stackA = instances[i].GetState<StackState>();
                    var stackB = inst.GetState<StackState>();
                    stackA.currentAmount += stackB.currentAmount;
                    instances[i].SetState(stackA);
                    return;
                }
            }
        }
        instances.Add(inst);
    }

    [Server]
    public void RemoveItem(ItemObject item)
    {
        if(item is rodObject)
        {
            RemoveItem(rodContainer, item, true);
        }
        else if (item is baitObject)
        {
            RemoveItem(baitContainer, item, false);
        }
        else if(item is FishObject)
        {
            RemoveItem(fishContainer, item, false);
        }
        else
        {
            RemoveItem(miscContainer, item, false);
        }
    }

    private void RemoveItem(SyncList<InventoryItem> container, ItemObject item, bool useUID)
    {
        for (int i = 0; i < container.Count(); i++)
        {
            if(useUID)
            {
                if (container[i].item.uuid == item.uuid)
                {
                    container.RemoveAt(i);
                    break;
                }
            }
            else
            {
                if (container[i].item.id == item.id)
                {
                    container.RemoveAt(i);
                    break;
                }
            }
        }
    }

    [Server]
    public void AddItem(ItemObject item)
    {
        if(item is rodObject)
        {
            AddItem(rodContainer, item);
        }
        else if (item is baitObject)
        {
            AddItem(baitContainer, item);
        }
        else if (item is FishObject)
        {
            AddItem(fishContainer, item);
        }
        else
        {
            AddItem(miscContainer, item);
        }

        // Mirror into ItemInstance list
        TryAddInstance(LegacyItemAdapter.From(item));
    }

    private void AddItem(SyncList<InventoryItem> container, ItemObject item)
    {
        bool addNewItem = true;
        if (item.stackable)
        {
            for (int i = 0; i < container.Count; i++)
            {
                //Stackable item, look for id. Not uid
                if (container[i].item.GetType() == item.GetType() && container[i].item.id == item.id)
                {
                    container[i] = container[i].AddAmount(item);
                    addNewItem = false;
                    break;
                }
            }
        }
        if (addNewItem)
        {
            container.Add(new InventoryItem(item));
        }
    }

    public bool ContainsItem(ItemObject item, out Guid? itemUuid)
    {
        if (item is rodObject)
        {
            itemUuid = item.uuid;
            return ContainsItemInContainer(rodContainer, item, out Guid? _);
        }
        else if (item is baitObject)
        {
            if (ContainsItemInContainer(baitContainer, item, out Guid? _itemUuid))
            {
                itemUuid = _itemUuid;
                return true;
            }
        }
        else if (item is FishObject)
        {
            if (ContainsItemInContainer(fishContainer, item, out Guid? _itemUuid))
            {
                itemUuid = _itemUuid;
                return true;
            }
        }
        else
        {
            Debug.LogWarning($"item should be of type: rod, bait or fish but was: {item}");
        }

        itemUuid = null;
        return false;
    }

    private bool ContainsItemInContainer(SyncList<InventoryItem> container, ItemObject item, out Guid? item_uuid)
    {
        foreach (InventoryItem _item in container)
        {
            if (item.stackable)
            {
                if (item.id == _item.item.id)
                {
                    item_uuid = _item.item.uuid;
                    return true;
                }
            }
            else
            {
                if (item.uuid == _item.item.uuid)
                {
                    item_uuid = _item.item.uuid;
                    return true;
                }
            }
        }
        item_uuid = null;
        return false;
    }

    public rodObject ReplaceRodByUID(Guid uuid)
    {
        ItemObject item = GetItemByUID(uuid, ItemType.Rod);
        if (item == null)
        {
            return null;
        }
        return item as rodObject;
    }

    public rodObject GetRodByID(int id)
    {
        ItemObject item = GetItemByID(id, rodObject.AsString());
        if(item == null)
        {
            return null;
        }
        return item as rodObject;
    }

    public rodObject GetRodByUID(Guid uuid)
    {
        ItemObject item = GetItemByUID(uuid, ItemType.Rod);
        if (item == null)
        {
            return null;
        }
        return item as rodObject;
    }

    public baitObject GetBaitByID(int id)
    {
        ItemObject item = GetItemByID(id, baitObject.AsString());
        if (item == null)
        {
            return null;
        }
        return item as baitObject;
    }

    public ItemObject GetItemByID(int id, string type)
    {
        SyncList<InventoryItem> container;
        if (type == rodObject.AsString())
        {
            container = rodContainer;
        }
        else if (type == baitObject.AsString())
        {
            container = baitContainer;
        }
        else if (type == FishObject.AsString())
        {
            container = fishContainer;
        }
        else
        {
            Debug.LogWarning($"GetItemById is not implemented for {type}");
            return null;
        }

        foreach (InventoryItem item in container)
        {
            if(item.item.id == id)
            {
                return item.item;
            }
        }
        return null;
    }

    public ItemObject GetItemByUID(Guid uuid, ItemType type)
    {
        SyncList<InventoryItem> container;
        if (type == ItemType.Rod)
        {

            container = rodContainer;
        }
        else if (type == ItemType.Bait)
        {
            Debug.LogWarning("A bait should not be found by i'ts UID but by it's ID instead");
            return null;
        }
        else
        {
            Debug.LogWarning($"GetItemById is not implemented for {type}");
            return null;
        }

        foreach (InventoryItem item in container)
        {
            if (item.item.uuid == uuid)
            {
                return item.item;
            }
        }
        return null;
    }
}


[System.Serializable]
public class InventoryItem
{
    public ItemObject item;
    public InventoryItem(ItemObject _item)
    {
        item = _item;
    }
    public InventoryItem AddAmount(ItemObject _item)
    {
        if (item is baitObject bait && _item is baitObject _bait)
        {
            bait.throwIns += _bait.throwIns;
            return new InventoryItem(bait);
        } 
        else if (item is FishObject fish && _item is FishObject _fish)
        {
            fish.amount += _fish.amount;
            return new InventoryItem(fish);
        }
        else if (item is rodObject && _item is rodObject)
        {
            Debug.LogError("Should not add multiple rod's together");
            return null;
        }
        return null;
    }
}

public static class InventoryItemReaderWriter
{
    public static void WriteInventoryItem(this NetworkWriter writer, InventoryItem obj)
    {
        writer.Write(obj.item);
    }

    public static InventoryItem ReadInventoryItem(this NetworkReader reader)
    {
        return new InventoryItem(reader.Read<ItemObject>());
    }
}

