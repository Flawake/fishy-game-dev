using System;
using System.Text;
using UnityEngine;
using Mirror;
using ItemSystem;
using System.Collections.Generic;
using FishyGame.Api;

// Extension helpers for ItemInstance behaviour checks
static class ItemInstanceExtensions {
    public static bool HasBehaviour<T>(this ItemInstance inst) where T : class, IItemBehaviour {
        return inst.def.GetBehaviour<T>() != null;
    }
}

public static class DatabaseCommunications
{
    [Server]
    public static void LoginRequest(string username, string password, NetworkConnectionToClient conn, Action<NetworkConnectionToClient, ApiResult<LoginResponse>> callback)
    {
        AuthenticationApi.Login(
            new LoginRequest
            {
                username = username,
                password = password,
            },
            // The generated client knows nothing about Mirror, so conn is carried
            // through the closure rather than by the transport.
            result => callback?.Invoke(conn, result));
    }

    [Server]
    public static void RegisterRequest(string username, string password, string email, NetworkConnectionToClient conn, Action<NetworkConnectionToClient, ApiResult<bool>> callback)
    {
        UsersApi.Register(
            new CreateUserRequest
            {
                username = username,
                password = password,
                email = email,
            },
            result => callback?.Invoke(conn, result));
    }

    [Server]
    public static void CommitTradeRequest(Guid userOneID, Guid userTwoID, List<TradeItemRequest> userOneItemsReceived, List<TradeItemRequest> userTwoItemsReceived, int userOneBucksReceived, int userTwoBucksReceived, Action<ApiResult<bool>> callback = null)
    {
        TradingApi.CommitTrade(
            new TradeRequest
            {
                user_one_id = userOneID.ToString(),
                user_two_id = userTwoID.ToString(),
                user_one_receives = userOneItemsReceived,
                user_two_receives = userTwoItemsReceived,
                user_one_bucks_received = userOneBucksReceived,
                user_two_bucks_received = userTwoBucksReceived,
            },
            callback);
    }

    [Server]
    public static void GetCurrentHerbQuest(Action<ApiResult<CurrentHerbQuestResponse>> callback)
    {
        // This endpoint takes no request body, so the generated method has no body
        // parameter and the transport sends "{}" on its own.
        HerbQuestApi.CurrentDailyQuest(callback);
    }

    [Server]
    public static void CompleteHerbQuest(Guid userID, Guid herbQuestId, int rewardCoins, List<HandInFish> fishes, Action<ApiResult<bool>> callback = null)
    {
        HerbQuestApi.CompleteDailyQuest(
            new CompleteDailyQuestRequest
            {
                user_id = userID.ToString(),
                herb_quest_id = herbQuestId.ToString(),
                reward_coins = rewardCoins,
                fishes = fishes,
            },
            callback);
    }

    [Server]
    public static void AcceptHerbQuest(Guid userID, Guid herbQuestId, Action<ApiResult<bool>> callback = null)
    {
        HerbQuestApi.AcceptDailyQuest(
            new AcceptDailyQuestRequest
            {
                user_id = userID.ToString(),
                herb_quest_id = herbQuestId.ToString(),
            },
            callback);
    }

    [Server]
    public static void AddFriendRequest(Guid userOne, Guid userTwo, Guid senderID, Action<ApiResult<bool>> callback = null)
    {
        FriendsApi.AddFriendRequest(
            new FriendRequests
            {
                user_one = userOne.ToString(),
                user_two = userTwo.ToString(),
                sender_id = senderID.ToString(),
            },
            callback);
    }

    [Server]
    public static void HandleFriendRequest(Guid userOne, Guid userTwo, bool accepted, Action<ApiResult<bool>> callback = null)
    {
        FriendsApi.HandleFriendRequest(
            new HandleFriendRequest
            {
                user_one = userOne.ToString(),
                user_two = userTwo.ToString(),
                request_accepted = accepted,
            },
            callback);
    }

    [Server]
    public static void RemoveFriend(Guid userOne, Guid userTwo, Action<ApiResult<bool>> callback = null)
    {
        FriendsApi.RemoveFriend(
            new RemoveFriend
            {
                user_one = userOne.ToString(),
                user_two = userTwo.ToString(),
            },
            callback);
    }

    // NOT MIGRATED YET. UserDataApi.RetrieveAllPlayerdata hands back a parsed
    // FishyGame.Api.UserData, but the whole downstream chain expects raw JSON:
    // PlayerAuthData.playerData stores a ResponseMessageData, and
    // PlayerData.ParsePlayerData deserialises into the hand-written UserData
    // struct in Assets/items/userDataStruct.cs (arrays plus helper properties
    // like LastCompletedHerbQuestId, which the generated class does not have).
    // Migrating this means reconciling those two UserData types first.
    [Server]
    public static void RetrievePlayerData(Guid userID, NetworkConnectionToClient conn, WebRequestHandler.WebRequestCallback callback)
    {
        RetrieveDataRequest requestData = new RetrieveDataRequest
        {
            user_id = userID.ToString()
        };

        string json = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.getPlayerDataEndpoint, bodyRaw, conn, callback);
    }

    [Server]
    public static void AddStatFish(CurrentFish fish, Guid userID, Action<ApiResult<bool>> callback = null)
    {
        StatsApi.AddFish(
            new AddFishRequest
            {
                user_id = userID.ToString(),
                length = fish.length,
                fish_id = fish.id,
                area_id = (int)fish.areaFishing,
                bait_id = fish.usedBait.Id,
                xp_earned = fish.xp,
            },
            callback);
    }

    [Server]
    public static void AddPlaytime(int amount, Guid userID, Action<ApiResult<bool>> callback = null)
    {
        StatsApi.AddPlaytime(
            new AddPlayTimeRequest
            {
                user_id = userID.ToString(),
                amount = amount,
            },
            callback);
    }

    [Server]
    public static void BuyItem(Guid buyerID, ItemInstance item, int price, StoreManager.CurrencyType currencyType, Action<ApiResult<bool>> callback = null)
    {
        Debug.Log("Buying item");
        ShopApi.BuyItem(
            new BuyItemRequest
            {
                buyer_id = buyerID.ToString(),
                item_def_id = item.def.Id,
                item_uuid = item.uuid.ToString(),
                item_state_blob = Convert.ToBase64String(StatePacker.Pack(item.state)),
                item_price = price,
                // CurrencyType is COINS/BUCKS, matching the MoneyType enum in the spec.
                bought_using = currencyType.ToString(),
            },
            callback);
    }

    [Server]
    public static void SellFishes(Guid sellerID, List<FishToSell> fishes, int earnings, Action<ApiResult<bool>> callback = null)
    {
        Debug.Log("Selling fishes");
        FishMarketApi.SellFishes(
            new SellFishesRequest
            {
                seller_id = sellerID.ToString(),
                fishes = fishes,
                price = earnings,
            },
            callback);
    }

    [Server]
    public static void AddOrUpdateItem(ItemInstance item, Guid userID, Action<ApiResult<bool>> callback = null)
    {
        InventoryApi.AddOrUpdateItem(
            new AddOrUpdateItemRequest
            {
                user_id = userID.ToString(),
                item_uuid = item.uuid.ToString(),
                definition_id = item.def.Id,
                state_blob = Convert.ToBase64String(StatePacker.Pack(item.state)),
            },
            callback);
    }

    [Server]
    public static void DestroyItem(ItemInstance item, Guid userID, Action<ApiResult<bool>> callback = null)
    {
        InventoryApi.DestroyItem(
            new DestroyItemRequest
            {
                user_id = userID.ToString(),
                item_uid = item.uuid.ToString(),
            },
            callback);
    }

    [Server]
    public static void SelectOtherItem(ItemInstance item, Guid userID, Action<ApiResult<bool>> callback = null)
    {
        // Left as literals on purpose: the generated FishyGame.Api.ItemType constants
        // are shadowed by the global ItemType enum in items/ItemObject.cs.
        string itemType;
        if (item.HasBehaviour<RodBehaviour>())
            itemType = FishyGame.Api.ItemType.Rod.ToString();
        else if (item.HasBehaviour<BaitBehaviour>())
            itemType = FishyGame.Api.ItemType.Bait.ToString();
        else {
            Debug.Log("Only a bait and a rod should be selectable");
            return;
        }

        StatsApi.SelectItem(
            new SelectItemRequest
            {
                user_id = userID.ToString(),
                item_uid = item.uuid.ToString(),
                item_type = itemType,
            },
            callback);
    }

    [Server]
    public static void AddMail(Mail mail, Action<ApiResult<bool>> callback = null)
    {
        MailsApi.CreateMail(
            new CreateMailRequest
            {
                mail_id = mail.mailUuid.ToString(),
                sender_id = mail.senderUuid.ToString(),
                // the generated DTO uses List<string>, the hand-written one used string[]
                receiver_ids = new List<string> { mail.receiverUuid.ToString() },
                title = mail.title,
                message = mail.message,
            },
            callback);
    }

    [Server]
    public static void ReadMail(Guid mailUID, Guid userID, bool read, Action<ApiResult<bool>> callback = null)
    {
        MailsApi.ReadMail(
            new ReadMailRequest
            {
                mail_id = mailUID.ToString(),
                user_id = userID.ToString(),
                read = read,
            },
            callback);
    }

    [Server]
    public static void AddActiveEffect(Guid userID, int itemId, DateTime expiryTime, Action<ApiResult<bool>> callback = null)
    {
        EffectsApi.AddEffect(
            new AddActiveEffectRequest
            {
                user_id = userID.ToString(),
                item_id = itemId,
                expiry_time = expiryTime.ToString("O"), // ISO 8601 format
            },
            callback);
    }

    [Server]
    public static void RemoveExpiredEffect(Guid userID, int itemId, Action<ApiResult<bool>> callback = null)
    {
        EffectsApi.RemoveExpiredEffects(
            new RemoveExpiredEffectRequest
            {
                user_id = userID.ToString(),
                item_id = itemId,
            },
            callback);
    }
}
