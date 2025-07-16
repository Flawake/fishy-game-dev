using System;
using System.Text;
using UnityEngine;
using Mirror;
using NewItemSystem;
using System.Linq;

// Extension helpers for ItemInstance behaviour checks
static class ItemInstanceExtensions {
    public static bool HasBehaviour<T>(this ItemInstance inst) where T : class, IItemBehaviour {
        return inst.def.GetBehaviour<T>() != null;
    }
}

public static class DatabaseCommunications
{
    [Server]
    public static void LoginRequest(string username, string password, NetworkConnectionToClient conn, WebRequestHandler.WebRequestCallback callback)
    {
        LoginRequest requestData = new LoginRequest
        {
            username = username,
            password = password,
        };
        
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.loginEndpoint, bodyRaw, conn, callback);
    }

    [Server]
    public static void AddFriendRequest(Guid userOne, Guid userTwo, Guid senderID)
    {
        CreateFriendRequest requestData = new CreateFriendRequest
        {
            user_one = userOne.ToString(),
            user_two = userTwo.ToString(),
            sender_id = senderID.ToString(),
        };
        
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.createFriendRequestEndpoint, bodyRaw);
    }
    
    [Server]
    public static void HandleFriendRequest(Guid userOne, Guid userTwo, bool accepted)
    {
        HandleFriendRequest requestData = new HandleFriendRequest
        {
            user_one = userOne.ToString(),
            user_two = userTwo.ToString(),
            request_accepted = accepted,
        };
        
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.handleFriendRequestEndpoint, bodyRaw);
    }

    [Server]
    public static void RemoveFriend(Guid userOne, Guid userTwo)
    {
        RemoveFriendRequest requestData = new RemoveFriendRequest
        {
            user_one = userOne.ToString(),
            user_two = userTwo.ToString(),
        };

        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.removeFriendEndpoint, bodyRaw);
    }

    [Server]
    public static void RegisterRequest(string username, string password, string email, NetworkConnectionToClient conn, WebRequestHandler.WebRequestCallback callback)
    {
        CreateUserRequest requestData = new CreateUserRequest
        {
            username = username,
            password = password,
            email = email,
        };
        
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.registerEndpoint, bodyRaw, conn, callback);
    }

    [Server]
    public static void RetrievePlayerData(Guid userID, NetworkConnectionToClient conn, WebRequestHandler.WebRequestCallback callback)
    {
        RetreiveDataRequest requestData = new RetreiveDataRequest
        {
            user_id = userID.ToString()
        };
        
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.getPlayerDataEndpoint, bodyRaw, conn, callback);

    }

    [Server]
    public static void AddStatFish(CurrentFish fish, Guid userID)
    {
        AddFishRequest requestData = new AddFishRequest
        {
            user_id = userID.ToString(),
            length = fish.length,
            fish_id = fish.id,
            area_id = -1,
            bait_id = -1,
        };
        
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        string endpoint = DatabaseEndpoints.addFishStatEndpoint;

        WebRequestHandler.SendWebRequest(DatabaseEndpoints.addFishStatEndpoint, bodyRaw);
    }

    [Server]
    public static  void ChangeFishCoinsAmount(int amount, Guid userID)
    {
        ChangeCoinsRequest requestData = new ChangeCoinsRequest
        {
            user_id = userID.ToString(),
            amount = amount,
        };
        
        
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.changeCoinsEndpoint, bodyRaw);
    }

    [Server]
    public static void ChangeFishBucksAmount(int amount, Guid userID)
    {
        
        ChangeBucksRequest requestData = new ChangeBucksRequest
        {
            user_id = userID.ToString(),
            amount = amount,
        };
        
        
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.changeBucksEndpoint, bodyRaw);
    }

    [Server]
    public static void AddXP(int amount, Guid userID)
    {
        AddXPRequest requestData = new AddXPRequest
        {
            user_id = userID.ToString(),
            amount = amount,
        };
        
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.addXPEndpoint, bodyRaw);
    }

    [Server]
    public static void AddPlaytime(int amount, Guid userID)
    {
        AddPlayTimeRequest requestData = new AddPlayTimeRequest
        {
            user_id = userID.ToString(),
            amount = amount,
        };
        
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.addPlaytime, bodyRaw);
    }

    [Server]
    public static void AddOrUpdateItem(ItemInstance item, Guid userID)
    {
        AddOrUpdateItemRequest request = new AddOrUpdateItemRequest
        {
            user_id = userID.ToString(),
            item_uuid = item.uuid.ToString(),
            definition_id = item.def.Id,
            state_blob = Convert.ToBase64String(StatePacker.Pack(item.state)),
        };
        string json = JsonUtility.ToJson(request);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.addNewItemEndpoint, bodyRaw);
    }

    [Server]
    public static void DestroyItem(ItemInstance item, Guid userID)
    {
        DestroyItemRequest requestData = new DestroyItemRequest
        {
            user_id = userID.ToString(),
            item_uid = item.uuid.ToString(),
        };
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.removeItemEndpoint, bodyRaw);
    }

    [Server]
    public static void SelectOtherItem(ItemInstance item, Guid userID)
    {
        string itemType;
        if (item.HasBehaviour<RodBehaviour>())
            itemType = "Rod";
        else if (item.HasBehaviour<BaitBehaviour>())
            itemType = "Bait";
        else {
            Debug.Log("Only a bait and a rod should be selectable");
            return;
        }

        SelectItemRequest requestData = new SelectItemRequest
        {
            user_id = userID.ToString(),
            item_uid = item.uuid.ToString(),
            item_type = itemType,
        };
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.selectItemEndpoint, bodyRaw);
    }
    
    [Server]
    public static void AddNewItem(ItemObject item, Guid userID)
    {
        int amount;
        int item_id;
        Guid item_uid;

        if (item is rodObject rod)
        {
            amount = rod.throwIns;
            item_id = rod.id;
            item_uid = rod.uuid;
        }
        else if (item is baitObject bait)
        {
            amount = bait.throwIns;
            item_id = bait.id;
            item_uid = bait.uuid;
        }
        else if (item is FishObject fish)
        {
            amount = fish.amount;
            item_id = fish.id;
            item_uid = fish.uuid;
        }
        else if(item is ExtraObject special) {
            amount = special.amount;
            item_id = special.id;
            item_uid = Guid.Empty;
        }
        else
        {
            Debug.LogWarning($"Could not add {item} to the database, unsopported item");
            return;
        }

        AddItemRequest requestData = new AddItemRequest
        {
            user_id = userID.ToString(),
            amount = amount,
            item_id = item_id,
            item_uid = item_uid.ToString(),
            cell_id = 0,
        };
        
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.addNewItemEndpoint, bodyRaw);
    }
    
    [Server]
    public static void IncreaseItem(ItemObject item, Guid userID)
    {
        int amount;
        Guid item_uid;

        if (item is rodObject rod)
        {
            amount = rod.throwIns;
            item_uid = rod.uuid;
        }
        else if (item is baitObject bait)
        {
            amount = bait.throwIns;
            item_uid = bait.uuid;
        }
        else if (item is FishObject fish)
        {
            amount = fish.amount;
            item_uid = fish.uuid;
        }
        else
        {
            Debug.LogWarning($"Could not add {item} to the database, unsopported item");
            return;
        }

        IncreaseItemRequest requestData = new IncreaseItemRequest
        {
            user_id = userID.ToString(),
            amount = amount,
            item_uid = item_uid.ToString(),
        };
        
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.addExistingItemEndpoint, bodyRaw);
    }

    [Server]
    public static void ReduceItem(ItemObject item, int amount, Guid userID)
    {
        DegradeItemRequest requestData = new DegradeItemRequest
        {
            user_id = userID.ToString(),
            amount = amount,
            item_uid = item.uuid.ToString(),
        };
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.reduceItemEndpoint, bodyRaw);
    }

    [Server]
    public static void AddMail(Mail mail)
    {
        CreateMailRequest requestData = new CreateMailRequest
        {
            mail_id = mail.mailUuid.ToString(),
            sender_id = mail.senderUuid.ToString(),
            receiver_ids = new string[] { mail.receiverUuid.ToString() },
            title = mail.title,
            message = mail.message,
        };
        
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.addMailEndpoint, bodyRaw);
    }

    [Server]
    public static void ReadMail(Guid mailUID, Guid userID, bool read)
    {
        ReadMailRequest requestData = new ReadMailRequest
        {
            mail_id = mailUID.ToString(),
            user_id = userID.ToString(),
            read = read,
        };
        
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.readMailEndpoint, bodyRaw);
    }

    // ------------------------------------------------------------------
    // Legacy overloads (ItemObject) kept for gradual migration
    // ------------------------------------------------------------------
    [System.Obsolete("Use ItemInstance overloads instead")] 
    [Server]
    public static void SelectOtherItem(ItemObject item, Guid userID)
    {
        ItemType type;
        if (item is rodObject)
            type = ItemType.Rod;
        else if (item is baitObject)
            type = ItemType.Bait;
        else {
            Debug.Log("Only a bait and a rod should be selectable");
            return;
        }

        SelectItemRequest requestData = new SelectItemRequest
        {
            user_id = userID.ToString(),
            item_uid = item.uuid.ToString(),
            item_type = type.ToString(),
        };
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.selectItemEndpoint, bodyRaw);
    }

    [System.Obsolete("Use ItemInstance overloads instead")]
    [Server]
    public static void DestroyItem(ItemObject item, Guid userID)
    {
        DestroyItemRequest requestData = new DestroyItemRequest
        {
            user_id = userID.ToString(),
            item_uid = item.uuid.ToString(),
        };
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.removeItemEndpoint, bodyRaw);
    }
}
