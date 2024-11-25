using Mirror;
using System.Linq;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    //Divide the containers for faster accessing
    public readonly SyncList<InventoryItem> miscContainer = new();
    public readonly SyncList<InventoryItem> rodContainer = new();
    public readonly SyncList<InventoryItem> baitContainer = new();
    public readonly SyncList<InventoryItem> fishContainer = new();

    [SerializeField]
    FishingManager fishingManager;

    [Server]
    public void SaveInventory(UserData playerData)
    {
        miscContainer.Clear();
        rodContainer.Clear();
        baitContainer.Clear();
        fishContainer.Clear();

        foreach (UserData.Rod item in playerData.inventory.rods ?? Enumerable.Empty<UserData.Rod>())
        {
            rodObject inventoryRod = ItemObjectGenerator.RodObjectFromMinimal(item.uid, item.id, item.durability);
            if(inventoryRod == null)
            {
                continue;
            }
            AddItem(inventoryRod);

            //TODO: find a better place for this.
            if (inventoryRod.uid == playerData.selectedRodUid)
            {
                fishingManager.SelectNewRod(inventoryRod, true);
            }
        }
        foreach (UserData.Bait item in playerData.inventory.baits ?? Enumerable.Empty<UserData.Bait>())
        {
            baitObject inventoryBait = ItemObjectGenerator.BaitObjectFromMinimal(item.uid, item.id, item.amount);
            if (inventoryBait == null)
            {
                continue;
            }
            AddItem(inventoryBait);
            //TODO: find a better place for this.
            if (inventoryBait.id == playerData.selectedBaitId)
            {
                Debug.Log($"Inventory bait: {inventoryBait.id}");
                Debug.Log($"Selected bait: {playerData.selectedBaitId}");
                fishingManager.SelectNewBait(inventoryBait, true);
            }
        }
        foreach (UserData.Fish item in playerData.inventory.fishes ?? Enumerable.Empty<UserData.Fish>())
        {
            FishObject inventoryFish = ItemObjectGenerator.FishObjectFromMinimal(item.id, item.amount);
            if (inventoryFish == null)
            {
                continue;
            }

            AddItem(inventoryFish);
        }
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
                if (container[i].item.uid == item.uid)
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

    public bool ContainsItem(ItemObject item)
    {
        if (item is rodObject)
        {
            return ContainsItemInContainer(rodContainer, item);
        }
        else if (item is baitObject)
        {
            return ContainsItemInContainer(baitContainer, item);
        }
        else if (item is FishObject)
        {
            return ContainsItemInContainer(fishContainer, item);
        }
        else
        {
            Debug.LogWarning($"item should be of type: rod, bait or fish but was: {item}");
        }
        return false;
    }

    private bool ContainsItemInContainer(SyncList<InventoryItem> container, ItemObject item)
    {
        foreach (InventoryItem _item in container)
        {
            if (item.stackable)
            {
                //uid might be 0 for items that can only be in the inventory once... Stackable items
                if (item.id == _item.item.id)
                {
                    return true;
                }
            }
            else
            {
                if (item.uid == _item.item.uid)
                {
                    return true;
                }
            }
        }
        return false;
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

    public rodObject GetRodByUID(int uid)
    {
        ItemObject item = GetItemByUID(uid, ItemType.rod);
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

    public ItemObject GetItemByUID(int uid, ItemType type)
    {
        SyncList<InventoryItem> container;
        if (type == ItemType.rod)
        {

            container = rodContainer;
        }
        else if (type == ItemType.bait)
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
            if (item.item.uid == uid)
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

