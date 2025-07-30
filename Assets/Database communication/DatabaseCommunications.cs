using System;
using System.Text;
using UnityEngine;
using Mirror;
using ItemSystem;
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

    [Server]
    public static void AddActiveEffect(Guid userID, int itemId, DateTime expiryTime)
    {
        AddActiveEffectRequest requestData = new AddActiveEffectRequest
        {
            user_id = userID.ToString(),
            item_id = itemId,
            expiry_time = expiryTime.ToString("O"), // ISO 8601 format
        };
        
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.addActiveEffectEndpoint, bodyRaw);
    }

    [Server]
    public static void RemoveExpiredEffect(Guid userID, int itemId)
    {
        RemoveExpiredEffectsRequest requestData = new RemoveExpiredEffectsRequest
        {
            user_id = userID.ToString(),
            item_id = itemId,
        };
        
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.removeExpiredEffectEndpoint, bodyRaw);
    }
}
