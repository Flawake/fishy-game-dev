using System;
using System.Collections.Generic;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

public class TradingUIManager : MonoBehaviour
{
    List<PendingTradeRequest> pendingTrades = new List<PendingTradeRequest>();
    
    PendingTradeRequest activeTrade;

    public void RequestNewTrade(Guid otherPlayer, string otherName)
    {
        CmdRequestNewTrade(otherPlayer);
        pendingTrades.Add(new PendingTradeRequest
        {
            requestTime = DateTime.Now,
            requesterId = otherPlayer,
            requesterName = otherName,
            receiverId = GetComponent<PlayerData>().GetUuid(),
            receiverName = GetComponent<PlayerData>().GetUsername(),
        });
    }

    [Command]
    void CmdRequestNewTrade(Guid otherPlayerId)
    {
        GameNetworkManager.connUUID.TryGetValue(otherPlayerId, out NetworkConnectionToClient otherConnection);
        if (otherConnection == null)
        {
            return;
        }
        GameNetworkManager.connNames.TryGetValue(otherConnection, out string otherName);

        Guid requesterID = GetComponentInParent<PlayerData>().GetUuid();
        string requesterName = GetComponentInParent<PlayerData>().GetUsername();

            PendingTradeRequest tradeRequest = 
            new PendingTradeRequest
            {
                requestTime = DateTime.Now,
                requesterId = requesterID,
                requesterName = requesterName,
                receiverId = otherPlayerId,
                receiverName = otherName,
            };

            pendingTrades.Add(tradeRequest.Clone());

            TradingUIManager otherTradeManager = otherConnection.identity.GetComponentInChildren<TradingUIManager>();
            otherTradeManager.pendingTrades.Add(tradeRequest);

        TargetIncomingTradeRequest(otherConnection, requesterID, requesterName);
    }

    [TargetRpc]
    void TargetIncomingTradeRequest(NetworkConnection target, Guid requester, string requesterName)
    {
        throw new NotImplementedException();
    }

    void IncomingTradeRequest(Guid otherPlayer, string name)
    {
        pendingTrades.Add(new PendingTradeRequest
        {
            requestTime = DateTime.Now,
            requesterId = otherPlayer,
            requesterName = name,
            receiverId = GetComponent<PlayerData>().GetUuid(),
            receiverName = GetComponent<PlayerData>().GetUsername(),
        });
    }

    [Server]
    void AcceptTradeRequest(PendingTradeRequest tradeRequest)
    {
        pendingTrades.Remove(tradeRequest);
        activeTrade = tradeRequest;
    }

    void ResetTradingMenu()
    {
        
    }

    void OpenTradingMenu()
    {
        ResetTradingMenu();
    }

    void CloseTradingMenu()
    {
        
    }
}
