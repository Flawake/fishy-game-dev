namespace TradeSystem
{
    using Mirror;
    using System.Collections.Generic;
    using System;

    enum CancelTradeRequestReason
    {
        AcceptedButUnavailable,
        RequestTimeout,
        ClosedByOther,
    }

    public enum TradingInfoType
    {
        TradeExpired,
        AlreadyTrading,
        ClosedByOther,
    }

    public struct PendingTradeRequest
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

    public class RunningTrade
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

    public static class TradeRegistry
    {
        // trade request hash -> trade request
        // For server all pending trade requests on the server. For clients their own incoming and outgoing requests
        static readonly Dictionary<int, PendingTradeRequest> pending = new();
        // running trade hash -> running trade
        static readonly Dictionary<int, RunningTrade> ServerRunningTrades = new();
        static RunningTrade ClientRunningTrade;

        // ---- Pending trades ----

        public static bool TryGetPending(int hash, out PendingTradeRequest trade)
            => pending.TryGetValue(hash, out trade);

        public static bool PendingContainsKey(int hash)
            => pending.ContainsKey(hash);

        public static void AddPending(PendingTradeRequest trade)
            => pending[trade.GetHashCode()] = trade;

        public static bool RemovePending(int hash)
            => pending.Remove(hash);

        // ---- Running trades server ----

        [Server]
        public static bool ServerTryGetRunning(int hash, out RunningTrade trade)
            => ServerRunningTrades.TryGetValue(hash, out trade);

        [Server]
        public static bool ServerRunningContainsKey(int hash)
            => ServerRunningTrades.ContainsKey(hash);

        [Server]
        public static void ServerAddRunning(RunningTrade trade)
            => ServerRunningTrades[trade.GetHashCode()] = trade;

        [Server]
        public static void ServerRemoveRunning(int hash)
            => ServerRunningTrades.Remove(hash);

        // ---- Running trades client ----

        [Client]
        public static RunningTrade ClientGetRunning()
            => ClientRunningTrade;

        [Client]
        public static void ClientSetRunning(RunningTrade trade)
            => ClientRunningTrade = trade;

        [Client]
        public static void ClientRemoveRunning()
            => ClientRunningTrade = null;
    }

    public class Trading : NetworkBehaviour
    {
        TradingUIManager tradingUIManager;
        bool TradeAlreadyRequestedByOther(Guid playerToRequestId, out PendingTradeRequest pendingRequest)
        {
            PendingTradeRequest newRequest = 
            new PendingTradeRequest
            {
                // Invert, requester requested the request from its perspective
                requesterId = playerToRequestId,
                receiverId = GetComponentInParent<PlayerData>().GetUuid(),
            };

            if (TradeRegistry.TryGetPending(newRequest.GetHashCode(), out pendingRequest)) {
                return true;
            }
            return false;
        }

        TradingUIManager GetTradingUIManager()
        {
            if (tradingUIManager == null)
            {
                tradingUIManager = GetComponentInChildren<TradingUIManager>();
            }
            return tradingUIManager;
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

        [Server]
        bool PlayerAlreadyTrading(Guid requesterID, Guid receiverID)
        {
            RunningTrade newTrade = 
            new RunningTrade
            {
                // Invert, requester requested the request from its perspective
                requesterId = requesterID,
                receiverId = receiverID,
            };

            if (TradeRegistry.ServerRunningContainsKey(newTrade.GetHashCode())) {
                return true;
            }

            newTrade.requesterId = receiverID;
            newTrade.receiverId = requesterID;

            if (TradeRegistry.ServerRunningContainsKey(newTrade.GetHashCode())) {
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

            if (TradeRegistry.PendingContainsKey(newRequest.GetHashCode())) {
                return false;
            }

            if (playerToRequestId == thisPlayerID)
            {
                return false;
            }

            return true;
        }

        public void RequestNewTrade(Guid playerToRequestId, string playerToRequestName)
        {
            CmdRequestNewTrade(playerToRequestId);
            PendingTradeRequest tradeRequest = 
            new PendingTradeRequest
            {
                requestTime = DateTime.Now,
                requesterId = GetComponentInParent<PlayerData>().GetUuid(),
                requesterName = GetComponentInParent<PlayerData>().GetUsername(),
                receiverId = playerToRequestId,
                receiverName = playerToRequestName,
            };
            TradeRegistry.AddPending(tradeRequest);
        }

        [Client]
        public void CancelCurrentTrade()
        {
            CmdCancelTrade(TradeRegistry.ClientGetRunning());
            TradeRegistry.ClientRemoveRunning();
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

            TradeRegistry.AddPending(tradeRequest);
            otherConnection.identity.GetComponent<Trading>().TargetTradeRequestIncoming(tradeRequest);
        }

        [TargetRpc]
        void TargetTradeRequestIncoming(PendingTradeRequest tradeRequest)
        {
            TradeRegistry.AddPending(tradeRequest);
        }

        [Command]
        void CmdAcceptTradeRequest(PendingTradeRequest tradeRequest)
        {
            Guid accepterID = GetComponentInParent<PlayerData>().GetUuid();
            if (!IDOwnsTradeRequest(tradeRequest, accepterID) || tradeRequest.requesterId == GetComponentInParent<PlayerData>().GetUuid())
            {
                return;
            }
            if (!TradeRegistry.PendingContainsKey(tradeRequest.GetHashCode()))
            {
                // PendingTradeRequest might be crafted by player. But most likely expired
                GetTradingUIManager().InformPlayer(TradingInfoType.TradeExpired);
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
                    acceptorConnection.identity.GetComponent<Trading>().TargetRemoveTradeRequest(tradeRequest, CancelTradeRequestReason.AcceptedButUnavailable);
                }
                return;
            }

            TradeRegistry.RemovePending(tradeRequest.GetHashCode());

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
                TradeRegistry.ServerAddRunning(runningTrade);
                requesterConnection.identity.GetComponent<Trading>().TargetTradeRequestAccepted(runningTrade);
                receiverConnection.identity.GetComponent<Trading>().TargetTradeRequestAccepted(runningTrade);
            }
        }

        [TargetRpc]
        void TargetRemoveTradeRequest(PendingTradeRequest tradeRequest, CancelTradeRequestReason reason)
        {
            TradeRegistry.RemovePending(tradeRequest.GetHashCode());
            if (reason == CancelTradeRequestReason.AcceptedButUnavailable)
            {
                GetTradingUIManager().InformPlayer(TradingInfoType.AlreadyTrading);
            }
        }

        [TargetRpc]
        void TargetTradeRequestAccepted(RunningTrade runningTrade)
        {
            if(TradeRegistry.ClientGetRunning() != null) {
                CmdCancelTrade(runningTrade);
            }
            else
            {
                TradeRegistry.ClientSetRunning(runningTrade);
                GetTradingUIManager().OpenTradingMenu(runningTrade);
            }
        }

        [TargetRpc]
        void TargetCancelRunningTrade()
        {
            TradeRegistry.ClientRemoveRunning();
            GetTradingUIManager().RunningTradeCanceled(TradingInfoType.ClosedByOther);
        }

        [Command]
        void CmdCancelTrade(RunningTrade tradeToCancel, NetworkConnectionToClient sender = null)
        {
            if (tradeToCancel == null)
            {
                return;
            }
            Guid CmdCallerId = sender.identity.GetComponent<PlayerData>().GetUuid();
            if (!TradeRegistry.ServerRunningContainsKey(tradeToCancel.GetHashCode()) || !IDOwnsTrade(tradeToCancel, CmdCallerId))
            {
                return;
            }

            TradeRegistry.ServerRemoveRunning(tradeToCancel.GetHashCode());
            
            Guid receiver = tradeToCancel.requesterId == sender.identity.GetComponent<PlayerData>().GetUuid() ?
                tradeToCancel.receiverId :
                tradeToCancel.requesterId;

            if(GameNetworkManager.connUUID.TryGetValue(receiver, out NetworkConnectionToClient receiverConnection))
            {
                receiverConnection.identity.GetComponent<Trading>().TargetCancelRunningTrade();
            }
        }
    }
}
