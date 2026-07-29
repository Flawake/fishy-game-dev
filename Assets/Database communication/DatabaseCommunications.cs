using System;
using UnityEngine;
using Mirror;
using ItemSystem;
using System.Collections.Generic;
using FishyGame.Api;
using System.Linq;


#nullable enable

public static class DatabaseCommunicationsHelper
{
    public static Durability? DeltaItemToDurability(DeltaItem item)
    {
        DurabilityState? durabilityState = item.GetState<DurabilityState>();
        if (durabilityState != null)
        {
            return new Durability {
                durability = durabilityState.remaining,
            };
        }
        return null;
    }

    public static Stack? DeltaItemToStack(DeltaItem item)
    {
        StackState? stackState = item.GetState<StackState>();
        if (stackState != null)
        {
            return new Stack {
                stack = stackState.currentAmount,
            };
        }
        return null;
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
    public static void RegisterRequest(string username, string password, string email, NetworkConnectionToClient conn, Action<NetworkConnectionToClient, ApiResult<LoginResponse>> callback)
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
    public static void CommitTradeRequest(Guid userOneID, Guid userTwoID, List<InventoryItem> userOneItemsReceived, List<InventoryItem> userTwoItemsReceived, int userOneBucksReceived, int userTwoBucksReceived, Action<ApiResult<bool>>? callback = null)
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
    public static void CompleteHerbQuest(Guid userID, Guid herbQuestId, int rewardCoins, List<HandInFish> fishes, Action<ApiResult<bool>>? callback = null)
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
    public static void AcceptHerbQuest(Guid userID, Guid herbQuestId, Action<ApiResult<bool>>? callback = null)
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
    public static void StartMission(Guid userID, int missionID, Action<ApiResult<bool>>? callback = null)
    {
        MissionsApi.StartMission(
            new StartMissionRequest
            {
                user_id = userID.ToString(),
                mission_id = missionID,
            },
            callback);
    }

    [Server]
    public static void ProgressMission(Guid userID, int missionID, int newProgress, Action<ApiResult<bool>>? callback = null)
    {
        MissionsApi.ProgressMission(
            new ProgressMissionRequest
            {
                user_id = userID.ToString(),
                mission_id = missionID,
                new_progress = newProgress,
            },
            callback);
    }

    /// <summary>
    /// Records the completion and pays the reward in one transaction, so the player
    /// can never be marked complete without being paid, or paid twice.
    /// </summary>
    [Server]
    public static void CompleteMission(Guid userID, int missionID, MissionRewardDraft reward, Action<ApiResult<bool>>? callback = null)
    {
        MissionsApi.CompleteMission(
            new CompleteMissionRequest
            {
                user_id = userID.ToString(),
                mission_id = missionID,
                reward_coins = reward.Coins,
                reward_bucks = reward.Bucks,
                reward_item = new InventoryItem
                {
                    definition_id = reward.GrantedItemDelta.ItemDefinition.Id,
                    item_uuid = reward.GrantedItemDelta.ItemUUID.ToString(),
                    durability = DatabaseCommunicationsHelper.DeltaItemToDurability(reward.GrantedItemDelta),
                    stack = DatabaseCommunicationsHelper.DeltaItemToStack(reward.GrantedItemDelta),

                },
            },
            callback);
    }

    [Server]
    public static void AddFriendRequest(Guid userOne, Guid userTwo, Guid senderID, Action<ApiResult<bool>>? callback = null)
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
    public static void HandleFriendRequest(Guid userOne, Guid userTwo, bool accepted, Action<ApiResult<bool>>? callback = null)
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
    public static void RemoveFriend(Guid userOne, Guid userTwo, Action<ApiResult<bool>>? callback = null)
    {
        FriendsApi.RemoveFriend(
            new RemoveFriend
            {
                user_one = userOne.ToString(),
                user_two = userTwo.ToString(),
            },
            callback);
    }

    [Server]
    public static void RetrievePlayerData(Guid userID, NetworkConnectionToClient conn, Action<NetworkConnectionToClient, ApiResult<UserData>> callback)
    {
        UserDataApi.RetrieveAllPlayerdata(
            new RetrieveDataRequest
            {
                user_id = userID.ToString()
            },
            result => callback?.Invoke(conn, result));
    }

    [Server]
    public static void AddStatFish(CurrentFish fish, Guid userID, Action<ApiResult<bool>>? callback = null)
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
    public static void AddPlaytime(int amount, Guid userID, Action<ApiResult<bool>>? callback = null)
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
    public static void BuyItem(Guid buyerID, DeltaItem deltaItem, int price, StoreManager.CurrencyType currencyType, Action<ApiResult<bool>>? callback = null)
    {   
        ShopApi.BuyItem(
            new BuyItemRequest
            {
                buyer_id = buyerID.ToString(),
                item = new InventoryItem {
                    definition_id = deltaItem.ItemDefinition.Id,
                    item_uuid = deltaItem.ItemUUID.ToString(),
                    durability = DatabaseCommunicationsHelper.DeltaItemToDurability(deltaItem),
                    stack = DatabaseCommunicationsHelper.DeltaItemToStack(deltaItem),
                },
                item_price = price,
                bought_using = currencyType.ToString(),
            },
            callback);
    }

    [Server]
    public static void SellFishes(Guid sellerID, List<InventoryItem> fishes, int earnings, Action<ApiResult<bool>>? callback = null)
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
    public static void AddOrUpdateItem(DeltaItem deltaItem, Guid userID, Action<ApiResult<bool>>? callback = null)
    {
        InventoryApi.AddOrUpdateItem(
            new AddOrUpdateItemRequest
            {
                user_id = userID.ToString(),
                item_uuid = deltaItem.ItemUUID.ToString(),
                definition_id = deltaItem.ItemDefinition.Id,
                durability = DatabaseCommunicationsHelper.DeltaItemToDurability(deltaItem),
                stack = DatabaseCommunicationsHelper.DeltaItemToStack(deltaItem),
            },
            callback);
    }

    [Server]
    public static void DestroyItem(DeltaItem deltaItem, Guid userID, Action<ApiResult<bool>>? callback = null)
    {
        InventoryApi.DestroyItem(
            new DestroyItemRequest
            {
                user_id = userID.ToString(),
                item_uid = deltaItem.ItemUUID.ToString(),
            },
            callback);
    }

    [Server]
    public static void SelectOtherItem(DeltaItem deltaItem, Guid userID, Action<ApiResult<bool>>? callback = null)
    {
        var itemType = new[]
        {
            (typeof(RodBehaviour), ItemType.Rod),
            (typeof(BaitBehaviour), ItemType.Bait)
        }
        .FirstOrDefault(x => deltaItem.ItemDefinition.GetBehaviour(x.Item1) != null);

        if (itemType == default)
        {
            Debug.Log("Only a bait and a rod should be selectable");
            return;
        }

        string itemTypeString = itemType.Item2.ToString();

        StatsApi.SelectItem(
            new SelectItemRequest
            {
                user_id = userID.ToString(),
                item_uid = deltaItem.ItemUUID.ToString(),
                item_type = itemTypeString,
            },
            callback);
    }

    [Server]
    public static void AddMail(Mail mail, Action<ApiResult<bool>>? callback = null)
    {
        MailsApi.CreateMail(
            new CreateMailRequest
            {
                mail_id = mail.mailUuid.ToString(),
                sender_id = mail.senderUuid.ToString(),
                receiver_ids = new List<string> { mail.receiverUuid.ToString() },
                title = mail.title,
                message = mail.message,
            },
            callback);
    }

    [Server]
    public static void ReadMail(Guid mailUID, Guid userID, bool read, Action<ApiResult<bool>>? callback = null)
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
    public static void AddActiveEffect(Guid userID, int itemId, DateTime expiryTime, Action<ApiResult<bool>>? callback = null)
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
    public static void RemoveExpiredEffect(Guid userID, int itemId, Action<ApiResult<bool>>? callback = null)
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
