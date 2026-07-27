using System;
using System.Text;
using UnityEngine;
using Mirror;
using ItemSystem;
using System.Collections.Generic;

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
    public static void CommitTradeRequest(Guid userOneID, Guid userTwoID, List<TradeItemRequest> userOneItemsReceived, List<TradeItemRequest> userTwoItemsReceived, int userOneBucksReceived, int userTwoBucksReceived)
    {
        TradeRequest requestData = new TradeRequest
        {
            user_one_id = userOneID.ToString(),
            user_two_id = userTwoID.ToString(),
            user_one_receives = userOneItemsReceived,
            user_two_receives = userTwoItemsReceived,
            user_one_bucks_received = userOneBucksReceived,
            user_two_bucks_received = userTwoBucksReceived,
        };
        
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.tradeCommitEndpoint, bodyRaw);
    }

    /// <summary>
    /// Fetches the Herb quest that is currently active. The database owns the quest,
    /// the game server only reads it (see HerbSpawnManager which polls this).
    /// The request has no body of interest, the quest is global (same for everyone).
    /// </summary>
    [Server]
    public static void GetCurrentHerbQuest(WebRequestHandler.WebRequestCallback callback)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes("{}");
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.currentHerbQuestEndpoint, bodyRaw, callback);
    }

    [Server]
    public static void CompleteHerbQuest(Guid userID, Guid herbQuestId, int rewardCoins, List<HandInFish> fishes)
    {
        CompleteHerbQuestRequest requestData = new CompleteHerbQuestRequest
        {
            user_id = userID.ToString(),
            herb_quest_id = herbQuestId.ToString(),
            reward_coins = rewardCoins,
            fishes = fishes,
        };

        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.completeHerbQuestEndpoint, bodyRaw);
    }

    /// <summary>
    /// Records that a player accepted (saw) the Herb quest of the given day. Used so the
    /// game can skip Herb's introduction the next time this player talks to him.
    /// </summary>
    [Server]
    public static void AcceptHerbQuest(Guid userID, Guid herbQuestId)
    {
        AcceptHerbQuestRequest requestData = new AcceptHerbQuestRequest
        {
            user_id = userID.ToString(),
            herb_quest_id = herbQuestId.ToString(),
        };

        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.acceptHerbQuestEndpoint, bodyRaw);
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
            area_id = (int)fish.areaFishing,
            bait_id = fish.usedBait.Id,
            xp_earned = fish.xp,
        };
        
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        WebRequestHandler.SendWebRequest(DatabaseEndpoints.addFishStatEndpoint, bodyRaw);
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
    public static void BuyItem(Guid buyerID, ItemInstance item, int price, StoreManager.CurrencyType currencyType)
    {
        Debug.Log("Buying item");
        BuyItemRequest request = new BuyItemRequest
        {
            buyer_id = buyerID.ToString(),
            item_def_id = item.def.Id,
            item_uuid = item.uuid.ToString(),
            item_state_blob = Convert.ToBase64String(StatePacker.Pack(item.state)),
            item_price = price,
            bought_using = currencyType.ToString(),
        };
        string json = JsonUtility.ToJson(request);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.buyItemEndpoint, bodyRaw);
    }

    [Server]
    public static void SellFishes(Guid sellerID, List<FishToSell> fishes, int earnings)
    {
        Debug.Log("Selling fishes");

        SellFishesRequest request = new SellFishesRequest
        {
            seller_id = sellerID.ToString(),
            fishes = fishes,
            price = earnings,
        };
        string json = JsonUtility.ToJson(request);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.sellFishEndpoint, bodyRaw);
    }

    [Server]
    public static void AddOrUpdateItem(ItemInstance item, Guid userID)
    {
        Debug.Log("Adding item");
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

    [Server]
    public static void GetActiveCompetition(WebRequestHandler.WebRequestCallback callback)
    {
        // GET request - send empty body
        byte[] bodyRaw = Encoding.UTF8.GetBytes("{}");
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.getActiveCompetitionEndpoint, bodyRaw, null, callback);
    }

    [Server]
    public static void GetUpcomingCompetitions(WebRequestHandler.WebRequestCallback callback)
    {
        // GET request - send empty body
        byte[] bodyRaw = Encoding.UTF8.GetBytes("{}");
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.getUpcomingCompetitionsEndpoint, bodyRaw, null, callback);
    }

    [Server]
    public static void SubmitCompetitionScore(Guid competitionId, Guid playerId, int score, WebRequestHandler.WebRequestCallback callback = null)
    {
        SubmitCompetitionScoreRequest requestData = new SubmitCompetitionScoreRequest
        {
            competition_id = competitionId.ToString(),
            player_id = playerId.ToString(),
            score = score,
        };
        
        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.submitCompetitionScoreEndpoint, bodyRaw, null, callback);
    }
}
