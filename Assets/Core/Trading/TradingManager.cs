using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System;
using ItemSystem;

static class TradabilityRules
{
    private static readonly Type[] TradableBaseTypes =
    {
        typeof(ShellBehaviour),
        typeof(FishBehaviour),
        typeof(BaitBehaviour),
    };

    public static bool IsTradable(ItemInstance item)
    {
        if (item.def.IsStatic || item.def.InfiniteUse)
        {
            return false;
        }
        foreach (Type tradableType in TradableBaseTypes)
        {
            if (item.def.GetBehaviour(tradableType) != null)
            {
                return true;
            }
        }

        return false;
    }
}

enum TradableItemType
{
    Bucks,
    Item
}

class TradableItem
{
    public TradableItemType Type { get; }
    public int BucksAmount { get; }
    public ItemInstance ItemInst { get; }
    private TradableItem(TradableItemType type, int bucks, ItemInstance item)
    {
        Type = type;
        BucksAmount = bucks;
        ItemInst = item;
    }

    public static TradableItem Bucks(int amount)
    {
        return new TradableItem(TradableItemType.Bucks, amount, null);
    }

    public static TradableItem Item(ItemInstance item)
    {
        if (!TradabilityRules.IsTradable(item))
        {
            //This should kick the player requesting to trade the item
            throw new InvalidOperationException($"{item.GetType().Name} is not tradable");
        }
        return new TradableItem(TradableItemType.Item, 0, item);
    }
}

struct PendingTradeRequest
{
    public DateTime requestTime;
    public Guid requesterId;
    public string requesterName;
    public Guid receiverId;
    public string receiverName;

    public override int GetHashCode()
    {
        return HashCode.Combine(
            requesterId,
            receiverId
        );
    }
}

struct RunningTrade
{
    public Guid requesterId;
    public string requesterName;
    public Guid receiverId;
    public string receiverName;
    public List<TradableItem> requesterTradeItems;
    public List<TradableItem> receiverTradeItems; 

    public override int GetHashCode()
    {
        return HashCode.Combine(
            requesterId,
            receiverId
        );
    }
}

public class TradingManager : MonoBehaviour
{
    // trade request hash -> trade request
    // For server all pending trade requests on the server. For clients their own incoming and outgoing requests
    static Dictionary<int, PendingTradeRequest> pendingTradeRequests = new Dictionary<int, PendingTradeRequest>();
    // running trade hash -> running trade
    // For server all running trades on the server. For clients their own current trade
    static Dictionary<int, RunningTrade> runningTrades = new Dictionary<int, RunningTrade>();

    bool TradeAlreadyRequestedByOther(Guid playerToRequestId, out PendingTradeRequest pendingRequest)
    {
        PendingTradeRequest newRequest = 
        new PendingTradeRequest
        {
            // Invert, requester requested the request from its perspective
            requesterId = playerToRequestId,
            receiverId = GetComponentInParent<PlayerData>().GetUuid(),
        };

        if (pendingTradeRequests.TryGetValue(newRequest.GetHashCode(), out pendingRequest)) {
            return true;
        }
        return false;
    }

    bool CanMakeTradeRequest(Guid playerToRequestId)
    {
        // Check if request is new
        PendingTradeRequest newRequest = 
        new PendingTradeRequest
        {
            requesterId = GetComponentInParent<PlayerData>().GetUuid(),
            receiverId = playerToRequestId,
        };

        if (pendingTradeRequests.ContainsKey(newRequest.GetHashCode())) {
            return false;
        }
        return true;
    }

    [Command]
    void CmdRequestNewTrade(Guid playerToRequestId)
    {
        if (!CanMakeTradeRequest(playerToRequestId))
        {
            return;
        }

        if (TradeAlreadyRequestedByOther(playerToRequestId, out PendingTradeRequest existingTradeRequest))
        {
            ServerTradeRequestAccepted(existingTradeRequest);
            return;
        }

        Guid requesterID = GetComponentInParent<PlayerData>().GetUuid();
        string requesterName = GetComponentInParent<PlayerData>().GetUsername();
        if (playerToRequestId == requesterID)
        {
            return;
        }

        GameNetworkManager.connUUID.TryGetValue(playerToRequestId, out NetworkConnectionToClient otherConnection);
        if (otherConnection == null)
        {
            return;
        }
        GameNetworkManager.connNames.TryGetValue(otherConnection, out string playerToRequestName);

        PendingTradeRequest tradeRequest = 
        new PendingTradeRequest
        {
            requestTime = DateTime.Now,
            requesterId = requesterID,
            requesterName = requesterName,
            receiverId = playerToRequestId,
            receiverName = playerToRequestName,
        };

        pendingTradeRequests.Add(tradeRequest.GetHashCode(), tradeRequest);
        TargetTradeRequestIncoming(otherConnection, tradeRequest);
    }

    [TargetRpc]
    void TargetTradeRequestIncoming(NetworkConnection _, PendingTradeRequest tradeRequest)
    {
        pendingTradeRequests.Add(tradeRequest.GetHashCode(), tradeRequest);
    }

    [Command]
    void CmdAcceptTradeRequest(PendingTradeRequest tradeRequest)
    {
        ServerTradeRequestAccepted(tradeRequest);
    }

    [Server]
    void ServerTradeRequestAccepted(PendingTradeRequest tradeRequest)
    {
        pendingTradeRequests.Remove(tradeRequest.GetHashCode());

        RunningTrade runningTrade = new RunningTrade
        {
            requesterId = tradeRequest.requesterId,
            requesterName = tradeRequest.requesterName,
            receiverId = tradeRequest.receiverId,
            receiverName = tradeRequest.receiverName,
            requesterTradeItems = new List<TradableItem>(),
            receiverTradeItems = new List<TradableItem>(),
        };

        runningTrades.Add(tradeRequest.GetHashCode(), runningTrade);
    }
}
