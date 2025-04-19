using UnityEngine;
using Mirror;
using System;

//TODO: can this class be static and should we make it static?
public static class DatabaseCommunications
{

    //TODO: we should do something when the request fails at the webrequesthandlers
    [Server]
    public static void RequestInventory(NetworkConnectionToClient conn, string userName)
    {
        WWWForm getInventoryForm = new WWWForm();
        getInventoryForm.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        getInventoryForm.AddField("user", userName);

        WebRequestHandler.SendWebRequest(DatabaseEndpoints.getInventoryEndpoint, getInventoryForm);
    }



    [Server]
    public static void AddItem(ItemObject item, bool asNewItem, string uuid)
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
        addItemInventory.AddField("uuid", uuid);
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
    public static void AddStatFish(CurrentFish fish, string uuid)
    {
        //Add a fish to the player statistics
        //We are sending the length as max_length, the database server checks if it indeed is the new max length
        string fishStat = "stat_fish{\"id\": " + fish.id + ", \"amount\": " + 1 + ", \"max_length\": " + fish.length + "}";
        WWWForm addFishStat = new WWWForm();
        addFishStat.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        addFishStat.AddField("uuid", uuid);
        addFishStat.AddField("id", fish.id);
        addFishStat.AddField("amount", 1);
        addFishStat.AddField("length", fish.length);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.addFishStatEndpoint, addFishStat);
    }

    [Server]
    public static  void ChangeFishCoinsAmount(int amount, string uuid)
    {
        WWWForm adjustMoneyForm = new WWWForm();
        adjustMoneyForm.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        adjustMoneyForm.AddField("uuid", uuid);
        adjustMoneyForm.AddField("type", "coin");
        adjustMoneyForm.AddField("amount", amount);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.adjustMoneyEndpoint, adjustMoneyForm);
    }

    [Server]
    public static void ChangeFishBucksAmount(int amount, string uuid)
    {
        WWWForm adjustMoneyForm = new WWWForm();
        adjustMoneyForm.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        adjustMoneyForm.AddField("uuid", uuid);
        adjustMoneyForm.AddField("type", "buck");
        adjustMoneyForm.AddField("amount", amount);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.adjustMoneyEndpoint, adjustMoneyForm);
    }

    [Server]
    public static void AddXP(int amount, string uuid) {
        WWWForm addXPForm = new WWWForm();
        addXPForm.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        addXPForm.AddField("uuid", uuid);
        addXPForm.AddField("amount", amount);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.addXPEndpoint, addXPForm);
    }

    [Server]
    public static void AddPlaytime(int amount, string uuid)
    {
        WWWForm addPlatimeForm = new WWWForm();
        addPlatimeForm.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        addPlatimeForm.AddField("uuid", uuid);
        addPlatimeForm.AddField("time", amount);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.addPlaytime, addPlatimeForm);
    }

    [Server]
    public static void SelectOtherItem(ItemObject item, string uuid)
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
        else
        {
            Debug.Log("Only a bait and a rod should be selectable");
            return;
        }
        //Select a different item as using
        WWWForm otherItemSelectForm = new WWWForm();
        otherItemSelectForm.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        otherItemSelectForm.AddField("uuid", uuid);
        otherItemSelectForm.AddField("type", type);
        otherItemSelectForm.AddField("uid", id);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.selectOtherItemEndpoint, otherItemSelectForm);
    }

    [Server]
    public static void ReduceItem(ItemObject item, int amount, string uuid)
    {
        string type;
        int id = int.MinValue;

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
        else if(item is FishObject)
        {
            type = FishObject.AsString();
            id = item.id;
        }
        else
        {
            Debug.LogWarning($"Could not reduce {item}, unsopported item");
            return;
        }

        //Add a item to the inventory
        WWWForm addItemInventory = new WWWForm();
        addItemInventory.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        addItemInventory.AddField("uuid", uuid);
        addItemInventory.AddField("type", type);
        addItemInventory.AddField("uid", id);
        addItemInventory.AddField("amount", amount);

        string endpoint = DatabaseEndpoints.reduceItemEndpoint;

        WebRequestHandler.SendWebRequest(endpoint, addItemInventory);
    }

    [Server]
    public static void DestroyItem(ItemObject item, string uuid)
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
        addItemInventory.AddField("uuid", uuid);
        addItemInventory.AddField("type", type);
        addItemInventory.AddField("u/id", id);

        string endpoint = DatabaseEndpoints.removeItemEndpoint;

        WebRequestHandler.SendWebRequest(endpoint, addItemInventory);
    }

    [Server]
    public static void AddMail(Mail mail)
    {
        WWWForm addNewMail = new WWWForm();
        addNewMail.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        addNewMail.AddField("mailUuid", mail.mailUuid.ToString());
        addNewMail.AddField("senderUuid", mail.senderUuid.ToString());
        addNewMail.AddField("receiverUuid", mail.receiverUuid.ToString());
        addNewMail.AddField("prevMailUuid", mail.prevMailUuid.ToString());
        addNewMail.AddField("sendTime", mail.sendTime.ToString("O"));
        addNewMail.AddField("mailTitle", mail.title);
        addNewMail.AddField("mailMessage", mail.message);
        
        string endpoint = DatabaseEndpoints.addMailEndpoint;
        WebRequestHandler.SendWebRequest(endpoint, addNewMail);
    }

    [Server]
    public static void ReadMail(int mailUID, bool read)
    {
        Debug.LogWarning("Saving the read status of a mail to the db has not yet been implemented");
    }
}
