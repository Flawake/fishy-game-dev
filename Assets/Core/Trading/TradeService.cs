namespace TradeSystem
{
    using ItemSystem;
    using Mirror;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public static class TradeService
    {
        // trade request id -> trade request
        // For server: all pending trade requests on the server.
        // For clients: their own incoming requests.
        static readonly Dictionary<TradeId, PendingTradeRequest> pendingRequests = new();

        // requester -> (receiver -> trade id) for O(1) directional lookup
        static readonly Dictionary<PlayerId, Dictionary<PlayerId, TradeId>> pendingByPair = new();

        // running trade id -> running trade
        static readonly Dictionary<TradeId, TradeSession> serverRunningSessions = new();
        // player -> running trade id (each player can only be in one active trade)
        static readonly Dictionary<PlayerId, TradeId> runningByPlayer = new();
        static TradeSession clientRunningSession;

        // ---- Pending trades ----

        public static bool TryGetPending(TradeId tradeId, out PendingTradeRequest trade)
            => pendingRequests.TryGetValue(tradeId, out trade);

        public static bool PendingContainsKey(TradeId tradeId)
            => pendingRequests.ContainsKey(tradeId);

        public static void AddPending(PendingTradeRequest trade)
        {
            TradeId id = trade.tradeId;
            pendingRequests[id] = trade;

            PlayerId requester = trade.requesterId;
            PlayerId receiver = trade.receiverId;

            if (!pendingByPair.TryGetValue(requester, out Dictionary<PlayerId, TradeId> inner))
            {
                inner = new Dictionary<PlayerId, TradeId>();
                pendingByPair[requester] = inner;
            }

            inner[receiver] = id;
        }

        public static bool RemovePending(TradeId tradeId)
        {
            if (!pendingRequests.TryGetValue(tradeId, out PendingTradeRequest trade))
            {
                return false;
            }

            pendingRequests.Remove(tradeId);

            PlayerId requester = trade.requesterId;
            PlayerId receiver = trade.receiverId;

            if (pendingByPair.TryGetValue(requester, out Dictionary<PlayerId, TradeId> inner))
            {
                inner.Remove(receiver);
                if (inner.Count == 0)
                {
                    pendingByPair.Remove(requester);
                }
            }

            return true;
        }

        public static bool TryGetPendingFromTo(PlayerId requesterId, PlayerId receiverId, out PendingTradeRequest trade)
        {

            trade = default;

            if (!pendingByPair.TryGetValue(requesterId, out Dictionary<PlayerId, TradeId> inner))
            {
                return false;
            }

            if (!inner.TryGetValue(receiverId, out TradeId tradeId))
            {
                return false;
            }

            return pendingRequests.TryGetValue(tradeId, out trade);
        }

        public static bool HasPendingRequestFromTo(Guid requesterId, Guid receiverId)
        {
            PlayerId req = requesterId;
            PlayerId rec = receiverId;

            return pendingByPair.TryGetValue(req, out Dictionary<PlayerId, TradeId> inner)
                && inner.ContainsKey(rec);
        }

        // ---- Running trades server ----

        [Server]
        public static bool ServerTryGetRunning(TradeId tradeId, out TradeSession trade)
            => serverRunningSessions.TryGetValue(tradeId, out trade);

        [Server]
        public static bool ServerRunningContainsKey(TradeId tradeId)
            => serverRunningSessions.ContainsKey(tradeId);

        [Server]
        public static void ServerAddRunning(TradeSession trade)
        {
            if (trade.tradeId == Guid.Empty)
            {
                trade.tradeId = Guid.NewGuid();
            }

            TradeId id = trade.tradeId;
            serverRunningSessions[id] = trade;

            PlayerId requester = trade.requesterId;
            PlayerId receiver = trade.receiverId;

            runningByPlayer[requester] = id;
            runningByPlayer[receiver] = id;
        }

        [Server]
        public static void ServerRemoveRunning(TradeId tradeId)
        {
            if (serverRunningSessions.TryGetValue(tradeId, out TradeSession session))
            {
                PlayerId requester = session.requesterId;
                PlayerId receiver = session.receiverId;

                runningByPlayer.Remove(requester);
                runningByPlayer.Remove(receiver);
            }

            serverRunningSessions.Remove(tradeId);
        }

        [Server]
        public static bool ServerIsPlayerTrading(PlayerId playerId)
        {
            return runningByPlayer.ContainsKey(playerId);
        }

        // ---- Running trades client ----

        [Client]
        public static TradeSession ClientGetRunning()
            => clientRunningSession;

        [Client]
        public static void ClientSetRunning(TradeSession trade)
            => clientRunningSession = trade;

        [Client]
        public static void ClientRemoveRunning()
            => clientRunningSession = null;
    }

    public static class TradeServer
    {
        [Server]
        public static void ServerBindCommands()
        {
            NetworkServer.RegisterHandler<TradeCMDRequestNewTrade>((sender, cmd) => CmdHandleTradeCommand(sender, cmd));
            NetworkServer.RegisterHandler<TradeCMDAcceptTradeRequest>((sender, cmd) => CmdHandleTradeCommand(sender, cmd));
            NetworkServer.RegisterHandler<TradeCMDCancelTrade>((sender, cmd) => CmdHandleTradeCommand(sender, cmd));
            NetworkServer.RegisterHandler<TradeCMDItemAdded>((sender, cmd) => CmdHandleTradeCommand(sender, cmd));
        }

        [Server]
        static bool IDOwnsTradeRequest(PendingTradeRequest req, Guid claimer)
        {
            return req.Owns(claimer);
        }

        [Server]
        static bool TradeAlreadyRequestedByOther(Guid playerToRequestId, Guid requesterID, out PendingTradeRequest pendingRequest)
        {
            Guid thisPlayerId = requesterID;
            return TradeService.TryGetPendingFromTo(playerToRequestId, thisPlayerId, out pendingRequest);
        }

        [Server]
        static bool CanMakeTradeRequest(Guid playerToRequestId, Guid requesterID)
        {
            Guid thisPlayerID = requesterID;

            if (playerToRequestId == thisPlayerID)
            {
                return false;
            }
            
            if (TradeService.HasPendingRequestFromTo(thisPlayerID, playerToRequestId)) {
                return false;
            }

            if (TradeService.ServerIsPlayerTrading(thisPlayerID))
            {
                return false;
            }

            return true;
        }


        [Server]
        static void ServerTradeRequestAccepted(PendingTradeRequest tradeRequest, Guid accepter)
        {
            if (TradeService.ServerIsPlayerTrading(accepter))
            {
                return;
            }
            if (TradeService.ServerIsPlayerTrading(tradeRequest.receiverId))
            {
                if (GameNetworkManager.connUUID.TryGetValue(accepter, out NetworkConnectionToClient accepterConnection))
                {
                    TradeRPCRequestRemoved removeRpc = new TradeRPCRequestRemoved
                    {
                        TradeId = tradeRequest.tradeId,
                        CancelReason = CancelTradeRequestReason.AcceptedButUnavailable,
                    };
                    accepterConnection.Send(removeRpc);
                }
                return;
            }

            TradeService.RemovePending(tradeRequest.tradeId);

            TradeSession runningTrade = new TradeSession(tradeRequest);

            GameNetworkManager.connUUID.TryGetValue(tradeRequest.requesterId, out NetworkConnectionToClient requesterConnection);
            GameNetworkManager.connUUID.TryGetValue(tradeRequest.receiverId, out NetworkConnectionToClient receiverConnection);

            if (requesterConnection != null && receiverConnection != null)
            {
                TradeService.ServerAddRunning(runningTrade);

                TradeRPCTradeStarted startedRpc = new TradeRPCTradeStarted
                {
                    Session = runningTrade,
                };

                requesterConnection.Send(startedRpc);
                receiverConnection.Send(startedRpc);
            }
            
            TradeRPCTradeRequestExpired failedRPC = new TradeRPCTradeRequestExpired
            {
                PendingID = tradeRequest.tradeId,
            };

            if (requesterConnection == null && receiverConnection != null)
            {
                receiverConnection.Send(failedRPC);
            }

            if (requesterConnection != null && receiverConnection == null)
            {
                requesterConnection.Send(failedRPC);
            }
        }

        [Server]
        static void ServerHandleRequestNewTrade(NetworkConnection requester, Guid playerToRequestId)
        {
            PlayerData requesterPlayerData = requester.identity.GetComponentInParent<PlayerData>();
            Guid requesterID = requesterPlayerData.GetUuid();
            string requesterName = requesterPlayerData.GetUsername();

            if (!CanMakeTradeRequest(playerToRequestId, requesterID))
            {
                return;
            }

            if (TradeAlreadyRequestedByOther(playerToRequestId, requesterID, out PendingTradeRequest existingTradeRequest))
            {
                ServerTradeRequestAccepted(existingTradeRequest, requesterPlayerData.GetUuid());
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
                tradeId = Guid.NewGuid(),
                requestTime = DateTime.Now,
                requesterId = requesterID,
                requesterName = requesterName,
                receiverId = playerToRequestId,
                receiverName = playerToRequestName,
            };

            TradeService.AddPending(tradeRequest);

            TradeRPCRequestIncoming rpc = new TradeRPCRequestIncoming
            {
                Pending = tradeRequest,
            };

            otherConnection.Send(rpc);
        }

        [Server]
        static void ServerHandleAcceptTradeRequest(NetworkConnectionToClient sender, Guid tradeId)
        {
            if (!TradeService.TryGetPending(tradeId, out PendingTradeRequest tradeRequest))
            {
                // Either crafted or expired on the server already.
                TradeRPCTradeRequestExpired expiredRpc = new TradeRPCTradeRequestExpired
                {
                    PendingID = tradeId,
                };
                sender.Send(expiredRpc);
                return;
            }

            Guid accepterID = sender.identity.GetComponent<PlayerData>().GetUuid();
            if (!IDOwnsTradeRequest(tradeRequest, accepterID) || tradeRequest.requesterId == accepterID)
            {
                return;
            }

            ServerTradeRequestAccepted(tradeRequest, accepterID);
        }

        [Server]
        static void ServerHandleCancelTrade(NetworkConnectionToClient sender, Guid tradeId)
        {
            if (!TradeService.ServerTryGetRunning(tradeId, out TradeSession tradeToCancel))
            {
                return;
            }

            Guid callerId = sender.identity.GetComponent<PlayerData>().GetUuid();
            if (!tradeToCancel.OwnedBy(callerId))
            {
                return;
            }

            TradeService.ServerRemoveRunning(tradeId);

            Guid receiver = tradeToCancel.requesterId == callerId
                ? tradeToCancel.receiverId
                : tradeToCancel.requesterId;

            if (GameNetworkManager.connUUID.TryGetValue(receiver, out NetworkConnectionToClient receiverConnection))
            {
                TradeRPCTradeCancelled cancelledRpc = new TradeRPCTradeCancelled
                {
                    
                };
                
                receiverConnection.Send(cancelledRpc);
            }
        }

        [Server]
        static void ServerHandleItemAdded(NetworkConnectionToClient sender, Guid tradeID, TradableItem tradableItem)
        {
            PlayerData playerData = sender.identity.GetComponent<PlayerData>();
            Guid itemAdderID = playerData.GetUuid();
            if (!TradeService.ServerTryGetRunning(tradeID, out TradeSession trade))
            {
                Debug.LogWarning("TradeSession not found");
                return;
            }
            if (!trade.OwnedBy(itemAdderID))
            {
                return;
            }
            if (!tradableItem.isValid())
            {
                // Should kick caller automatically
                throw new InvalidOperationException();
            }
            if (tradableItem.Type == TradableItemType.Bucks)
            {
                if (playerData.GetFishBucks() < tradableItem.Amount) {
                    // Should kick caller automatically
                    throw new InvalidOperationException();
                }
            }
            else if (tradableItem.Type == TradableItemType.Item)
            {
                if (sender.identity.GetComponent<PlayerInventory>().GetItem(tradableItem.ItemInst.uuid).GetState<StackState>().currentAmount < tradableItem.Amount) {
                    // Should kick caller automatically
                    throw new InvalidOperationException();
                }
            }

            TradeRPCTradeitemAdded updateMessage = new TradeRPCTradeitemAdded
            {
                addedItem = tradableItem,
            };

            if (itemAdderID == trade.receiverId)
            {
                trade.receiverTradeItems.Add(tradableItem);
                GameNetworkManager.connUUID.TryGetValue(trade.requesterId, out NetworkConnectionToClient otherConnection);
                if (otherConnection == null)
                {
                    return;
                }
                otherConnection.Send(updateMessage);
            }
            else
            {
                trade.requesterTradeItems.Add(tradableItem);
                GameNetworkManager.connUUID.TryGetValue(trade.receiverId, out NetworkConnectionToClient otherConnection);
                if (otherConnection == null)
                {
                    return;
                }
                otherConnection.Send(updateMessage);
            }
        }

        static void CmdHandleTradeCommand(NetworkConnectionToClient requester, NetworkMessage tradeCommand)
        {
            switch (tradeCommand)
            {
                case TradeCMDRequestNewTrade cmd:
                    ServerHandleRequestNewTrade(requester, cmd.requestTargetID);
                    break;
                case TradeCMDAcceptTradeRequest cmd:
                    ServerHandleAcceptTradeRequest(requester, cmd.tradeID);
                    break;
                case TradeCMDCancelTrade cmd:
                    ServerHandleCancelTrade(requester, cmd.tradeID);
                    break;
                case TradeCMDItemAdded cmd:
                    ServerHandleItemAdded(requester, cmd.tradeID, cmd.itemAdded);
                    break;
            }
        }
    }
}

