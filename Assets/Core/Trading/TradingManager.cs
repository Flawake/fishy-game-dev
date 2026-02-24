using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System;
using ItemSystem;
using Mirror.BouncyCastle.Tls;

enum CancelTradeRequestReason
{
    AcceptedButUnavailable,
    RequestTimeout,
}

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

    [Server]
    bool IDOwnsTrade(RunningTrade trade, Guid claimer)
    {
        if(trade.receiverId == claimer || trade.requesterId == claimer)
        {
            return true;
        }
        return false;
    }

    [Server]
    bool IDOwnsTradeRequest(PendingTradeRequest req, Guid claimer)
    {
        if(req.receiverId == claimer || req.requesterId == claimer)
        {
            return true;
        }
        return false;
    }

    bool PlayerAlreadyTrading(Guid requesterID, Guid receiverID)
    {
        RunningTrade newTrade = 
        new RunningTrade
        {
            // Invert, requester requested the request from its perspective
            requesterId = requesterID,
            receiverId = receiverID,
        };

        if (runningTrades.ContainsKey(newTrade.GetHashCode())) {
            return true;
        }

        newTrade.requesterId = receiverID;
        newTrade.receiverId = requesterID;

        if (runningTrades.ContainsKey(newTrade.GetHashCode())) {
            return true;
        }

        return false;
    }

    bool CanMakeTradeRequest(Guid playerToRequestId)
    {
        Guid thisPlayerID = GetComponentInParent<PlayerData>().GetUuid();
        // Check if request is new
        PendingTradeRequest newRequest = 
        new PendingTradeRequest
        {
            requesterId = thisPlayerID,
            receiverId = playerToRequestId,
        };

        if (pendingTradeRequests.ContainsKey(newRequest.GetHashCode())) {
            return false;
        }

        if (playerToRequestId == thisPlayerID)
        {
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
            ServerTradeRequestAccepted(existingTradeRequest, GetComponentInParent<PlayerData>().GetUuid());
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
        Guid accepterID = GetComponentInParent<PlayerData>().GetUuid();
        if (!IDOwnsTradeRequest(tradeRequest, accepterID) || tradeRequest.requesterId == GetComponentInParent<PlayerData>().GetUuid())
        {
            return;
        }
        if (!pendingTradeRequests.ContainsKey(tradeRequest.GetHashCode()))
        {
            // PendingTradeRequest might be crafted by player. But most likely expired
            tradingUIManager.informPlayer(TradingInfoType.TradeExpired, "Player was already trading");
            throw new NotImplementedException("Todo: tell player request already expired");
        }
        ServerTradeRequestAccepted(tradeRequest, GetComponentInParent<PlayerData>().GetUuid());
    }

    [Server]
    void ServerTradeRequestAccepted(PendingTradeRequest tradeRequest, Guid Acceptor)
    {
        if (PlayerAlreadyTrading(tradeRequest.requesterId, tradeRequest.receiverId))
        {
            if(GameNetworkManager.connUUID.TryGetValue(Acceptor, out NetworkConnectionToClient acceptorConnection))
            {
                TargetRemoveTradeRequest(acceptorConnection, tradeRequest, CancelTradeRequestReason.AcceptedButUnavailable);
            }
            return;
        }

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

        GameNetworkManager.connUUID.TryGetValue(tradeRequest.requesterId, out NetworkConnectionToClient requesterConnection);
        GameNetworkManager.connUUID.TryGetValue(tradeRequest.receiverId, out NetworkConnectionToClient receiverConnection);

        if (requesterConnection != null && receiverConnection != null)
        {
            runningTrades.Add(runningTrade.GetHashCode(), runningTrade);
            TargetTradeRequestAccepted(requesterConnection, runningTrade);
            TargetTradeRequestAccepted(receiverConnection, runningTrade);
        }
    }

    [TargetRpc]
    void TargetRemoveTradeRequest(NetworkConnection _, PendingTradeRequest tradeRequest, CancelTradeRequestReason reason)
    {
        pendingTradeRequests.Remove(tradeRequest.GetHashCode());
        if (reason == CancelTradeRequestReason.AcceptedButUnavailable)
        {
            tradingUIManager.informPlayer(TradingInfoType.CancelTradeRequest, "Player was already trading");
        }
    }

    [TargetRpc]
    void TargetTradeRequestAccepted(NetworkConnection _, RunningTrade runningTrade)
    {
        if (runningTrades.Count > 0)
        {
            CmdCancelTrade();
        }
        else
        {
            runningTrades.Add(runningTrade.GetHashCode(), runningTrade);
            tradingUIManager.openTradeMenu(runningTrade);
        }
    }

    [TargetRpc]
    void TargetCancelRunningTrade(NetworkConnection _, RunningTrade runningTrade)
    {
        runningTrades.Remove(runningTrade.GetHashCode());
        tradingUIManager.closeTradeMenu(TradingStopReason.ClosedByOther, "The other side cancelled the trade. All items have been returned to their original owner");
    }
    
    [Command]
    void CmdCancelTrade(RunningTrade tradeToCancel, NetworkConnectionToClient sender = null)
    {
        Guid CmdCallerId = sender.identity.GetComponent<PlayerData>().GetUuid();
        if (!runningTrades.ContainsKey(tradeToCancel.GetHashCode()) || !IDOwnsTrade(tradeToCancel, CmdCallerId))
        {
            return;
        }
        Guid receiver = tradeToCancel.requesterId;
        if (tradeToCancel.requesterId == sender.identity.GetComponent<PlayerData>().GetUuid())
        {
            receiver = tradeToCancel.receiverId;
        }

        if(GameNetworkManager.connUUID.TryGetValue(receiver, out NetworkConnectionToClient receiverConnection))
        {
            TargetCancelRunningTrade(receiverConnection, tradeToCancel);
        }
    }
}
