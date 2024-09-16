using UnityEngine;
using Mirror;
using System;
using UnityEditorInternal.Profiling.Memory.Experimental;

//TODO: can this class be static and should we make it static?
public class DatabaseCommunications : NetworkBehaviour
{
    [SerializeField]
    PlayerData data;

    //TODO: we should do something when the request fails at the webrequesthandlers
    [Server]
    public void RequestInventory(NetworkConnectionToClient conn, string userName)
    {
        WWWForm getInventoryForm = new WWWForm();
        getInventoryForm.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        getInventoryForm.AddField("user", userName);

        WebRequestHandler.SendWebRequest(DatabaseEndpoints.getInventoryEndpoint, getInventoryForm);
    }



    [Server]
    public void AddItem(ItemObject item, bool asNewItem)
    {
        string type;
        int amount;

        WWWForm addItemInventory = new WWWForm();

        if (item is rodObject rod)
        {
            type = "rod";
            amount = rod.throwIns;
            if(asNewItem)
            {
                addItemInventory.AddField("id", rod.id);
                addItemInventory.AddField("uid", rod.uid);
            }
            else
            {
                addItemInventory.AddField("iid", rod.uid);
            }
        }
        else if (item is baitObject bait)
        {
            type = "bait";
            amount = bait.throwIns;
            if (asNewItem)
            {
                addItemInventory.AddField("id", bait.id);
                addItemInventory.AddField("uid", 0);
            }
            else
            {
                addItemInventory.AddField("iid", bait.id);
            }
        }
        else if (item is FishObject fish)
        {
            type = "fish";
            amount = fish.amount;
            if (asNewItem)
            {
                addItemInventory.AddField("id", fish.id);
                addItemInventory.AddField("uid", 0);
            }
            else
            {
                addItemInventory.AddField("iid", fish.id);
            }
        }
        else
        {
            Debug.LogWarning($"Could not add {item} to the database, unsopported item");
            return;
        }
        //Add a item to the inventory
        addItemInventory.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        addItemInventory.AddField("user", data.GetUsername());
        addItemInventory.AddField("type", type);
        addItemInventory.AddField("amount", amount);

        string endpoint = DatabaseEndpoints.addExistingItemEndpoint;

        if (asNewItem)
        {
            endpoint = DatabaseEndpoints.addNewItemEndpoint;
        }

        WebRequestHandler.SendWebRequest(endpoint, addItemInventory);
    }

    [Server]
    public void AddStatFish(CurrentFish fish)
    {
        //Add a fish to the player statistics
        //We are sending the length as max_length, the database server checks if it indeed is the new max length
        string fishStat = "stat_fish{\"id\": " + fish.id + ", \"amount\": " + 1 + ", \"max_length\": " + fish.length + "}";
        WWWForm addFishStat = new WWWForm();
        addFishStat.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        addFishStat.AddField("user", data.GetUsername());
        addFishStat.AddField("stat", fishStat);

        WebRequestHandler.SendWebRequest(DatabaseEndpoints.addStatEndpoint, addFishStat);
    }

    [Server]
    public void ChangeFishCoinsAmount(int amount)
    {
        Debug.LogWarning("ChangeFishCoinsAmount() has not been implemented yet");
    }

    [Server]
    public void ChangeFishBucksAmount(int amount)
    {
        Debug.LogWarning("ChangeFishBucksAmount() has not been implemented yet");
    }

    [Server]
    public void SelectOtherItem(ItemObject item)
    {
        string type;
        if (item is rodObject)
        {
            type = rodObject.AsString();
        }
        else if (item is baitObject)
        {
            type = baitObject.AsString();
        }
        else
        {
            Debug.Log("Only a bait and a rod should be selectable");
            return;
        }
        //Select a different item as using
        WWWForm otherItemSelectForm = new WWWForm();
        otherItemSelectForm.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        otherItemSelectForm.AddField("user", data.GetUsername());
        otherItemSelectForm.AddField("type", type);
        otherItemSelectForm.AddField("uid", item.uid);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.selectOtherItemEndpoint, otherItemSelectForm);
    }

    [Server]
    public void ReduceItem(ItemObject item, int amount)
    {
        string type;
        int uid = item.uid;

        if (item is rodObject)
        {
            type = rodObject.AsString();
        }
        else if (item is baitObject)
        {
            type = baitObject.AsString();
        }
        else if(item is FishObject)
        {
            type = FishObject.AsString();
        }
        else
        {
            Debug.LogWarning($"Could not reduce {item}, unsopported item");
            return;
        }

        //Add a item to the inventory
        WWWForm addItemInventory = new WWWForm();
        addItemInventory.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        addItemInventory.AddField("user", data.GetUsername());
        addItemInventory.AddField("type", type);
        addItemInventory.AddField("uid", uid);
        addItemInventory.AddField("amount", amount);

        string endpoint = DatabaseEndpoints.reduceItemEndpoint;

        WebRequestHandler.SendWebRequest(endpoint, addItemInventory);
    }

    [Server]
    public void DestroyItem(ItemObject item)
    {
        string type;
        int id;

        if (item is rodObject)
        {
            type = rodObject.AsString();
            id = item.uid;
        }
        else if (item is baitObject)
        {
            type = baitObject.AsString();
            id = item.id;
        }
        else if (item is FishObject)
        {
            type = FishObject.AsString();
            id = item.id;
        }
        else
        {
            Debug.LogWarning($"Could not destroy {item}, unsopported item");
            return;
        }

        WWWForm addItemInventory = new WWWForm();
        addItemInventory.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        addItemInventory.AddField("user", data.GetUsername());
        addItemInventory.AddField("type", type);
        addItemInventory.AddField("u/id", id);

        string endpoint = DatabaseEndpoints.removeItemEndpoint;

        WebRequestHandler.SendWebRequest(endpoint, addItemInventory);
    }
}
