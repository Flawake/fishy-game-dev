namespace TradeSystem
{
    using ItemSystem;
    using Mirror;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using FishyGame.Api;

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
            NetworkServer.RegisterHandler<TradeCMDAcceptTrade>((sender, cmd) => CmdHandleTradeCommand(sender, cmd));
            NetworkServer.RegisterHandler<TradeCMDVerifyTrade>((sender, cmd) => CmdHandleTradeCommand(sender, cmd));
            NetworkServer.RegisterHandler<TradeCMDDenyVerifyTrade>((sender, cmd) => CmdHandleTradeCommand(sender, cmd));
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
            if (!tradeToCancel.IsOwnedBy(callerId))
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
        static void ServerHandleItemAdded(NetworkConnectionToClient adderConn, Guid tradeID, TradableItem tradableItem)
        {
            PlayerData adderData = adderConn.identity.GetComponent<PlayerData>();
            Guid itemAdderID = adderData.GetUuid();
            if (!TradeService.ServerTryGetRunning(tradeID, out TradeSession trade))
            {
                Debug.LogWarning("TradeSession not found");
                return;
            }
            if (!trade.IsOwnedBy(itemAdderID))
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
                if (adderData.GetFishBucks() < tradableItem.Amount) {
                    // Should kick caller automatically
                    throw new InvalidOperationException();
                }
            }
            else if (tradableItem.Type == TradableItemType.Item)
            {
                if (adderConn.identity.GetComponent<PlayerInventory>().GetItem(tradableItem.ItemInst.uuid).GetState<StackState>().currentAmount < tradableItem.Amount) {
                    // Should kick caller automatically
                    throw new InvalidOperationException();
                }
            }

            ResetAcceptState(tradeID);

            TradeRPCTradeitemAdded updateMessage = new TradeRPCTradeitemAdded
            {
                addedItem = tradableItem,
            };

            var list = trade.GetOwnTradeList(adderData.GetUuid());

            int index = list.FindIndex(item => item.Eq(tradableItem));

            if (tradableItem.Amount <= 0)
            {
                if (index >= 0)
                {
                    list.RemoveAt(index);
                }
            }
            else
            {
                if (index >= 0)
                {
                    list[index] = tradableItem;
                }
                else
                {
                    list.Add(tradableItem);
                }
            }

            if (itemAdderID == trade.receiverId)
            {
                GameNetworkManager.connUUID.TryGetValue(trade.requesterId, out NetworkConnectionToClient otherConnection);
                if (otherConnection == null)
                {
                    return;
                }
                otherConnection.Send(updateMessage);
            }
            else
            {
                GameNetworkManager.connUUID.TryGetValue(trade.receiverId, out NetworkConnectionToClient otherConnection);
                if (otherConnection == null)
                {
                    return;
                }
                otherConnection.Send(updateMessage);
            }
        }

        [Server]
        public static void ServerAcceptTrade(NetworkConnectionToClient accepterConn, Guid tradeID)
        {
            PlayerData accepterData = accepterConn.identity.GetComponent<PlayerData>();
            Guid AccepterID = accepterData.GetUuid();
            if (!TradeService.ServerTryGetRunning(tradeID, out TradeSession trade))
            {
                Debug.LogWarning("TradeSession not found");
                return;
            }
            if (!trade.IsOwnedBy(AccepterID))
            {
                return;
            }

            var flag = (AccepterID == trade.requesterId)
                ? TradeSessionState.RequesterAccepted
                : TradeSessionState.ReceiverAccepted;

            trade.State |= flag;

            TradeRPCAcceptTrade acceptMessage = new TradeRPCAcceptTrade
            {
                tradeID = tradeID,
            };

            if (AccepterID == trade.receiverId)
            {
                GameNetworkManager.connUUID.TryGetValue(trade.requesterId, out NetworkConnectionToClient otherConnection);
                if (otherConnection == null)
                {
                    return;
                }
                otherConnection.Send(acceptMessage);
            }
            else
            {
                GameNetworkManager.connUUID.TryGetValue(trade.receiverId, out NetworkConnectionToClient otherConnection);
                if (otherConnection == null)
                {
                    return;
                }
                otherConnection.Send(acceptMessage);
            }

            if (trade.BothPlayersAccepted())
            {
                TradeRPCTradeAccepted openVerifyTradeMessage = new TradeRPCTradeAccepted
                {
                    tradeID = tradeID,
                };
                GameNetworkManager.connUUID.TryGetValue(trade.requesterId, out NetworkConnectionToClient connOne);
                connOne?.Send(openVerifyTradeMessage);
                GameNetworkManager.connUUID.TryGetValue(trade.receiverId, out NetworkConnectionToClient connTwo);
                connTwo?.Send(openVerifyTradeMessage);
            }
        }
        
        [Server]
        public static void ServerVerifyTrade(NetworkConnectionToClient accepterConn, Guid tradeID)
        {
            PlayerData accepterData = accepterConn.identity.GetComponent<PlayerData>();
            Guid AccepterID = accepterData.GetUuid();
            if (!TradeService.ServerTryGetRunning(tradeID, out TradeSession trade))
            {
                Debug.LogWarning("TradeSession not found");
                return;
            }
            if (!trade.IsOwnedBy(AccepterID))
            {
                return;
            }

            if (!trade.BothPlayersAccepted())
            {
                return;
            }

            var flag = (AccepterID == trade.requesterId)
                ? TradeSessionState.RequesterVerified
                : TradeSessionState.ReceiverVerified;

            trade.State |= flag;


            if (trade.BothPlayersVerified())
            {
                TradeRPCTradeVerified openVerifyTradeMessage = new TradeRPCTradeVerified
                {
                    tradeID = tradeID,
                };
                GameNetworkManager.connUUID.TryGetValue(trade.requesterId, out NetworkConnectionToClient requesterConn);
                GameNetworkManager.connUUID.TryGetValue(trade.receiverId, out NetworkConnectionToClient receiverConn);

                requesterConn?.Send(openVerifyTradeMessage);
                receiverConn?.Send(openVerifyTradeMessage);

                CommitTrade(trade);

                TradeService.ServerRemoveRunning(trade.tradeId);
            } 
        }

        [Server]
        static void CommitTrade(TradeSession session)
        {
            GameNetworkManager.connUUID.TryGetValue(session.requesterId, out NetworkConnectionToClient requesterConn);
            GameNetworkManager.connUUID.TryGetValue(session.receiverId, out NetworkConnectionToClient receiverConn);
            if (requesterConn == null || receiverConn == null)
            {
                return;
            }
            PlayerInventory requesterInv = requesterConn.identity.GetComponent<PlayerInventory>();
            PlayerInventory receiverInv = receiverConn.identity.GetComponent<PlayerInventory>();
            if (requesterInv == null || receiverInv == null)
            {
                return;
            }

            int requesterMoneySend = 0;
            int receiverMoneySend = 0;

            HashSet<ItemInstance> requesterUpdatedItems = new HashSet<ItemInstance>();
            HashSet<ItemInstance> receiverUpdatedItems = new HashSet<ItemInstance>();

            // Add items
            foreach (TradableItem item in session.requesterTradeItems)
            {
                Debug.Log(item.Amount);
                if (item.Type == TradableItemType.Item)
                {
                    // Don't use the item referenced by the other player
                    ItemInstance newItem = new ItemInstance(item.ItemInst.def, item.Amount);
                    ItemInstance updated = receiverInv.ServerMergeOrAdd(newItem, true);
                    if (!receiverUpdatedItems.Contains(updated))
                    {
                        receiverUpdatedItems.Add(updated);
                    }
                }
                else if (item.Type == TradableItemType.Bucks)
                {
                    requesterMoneySend = item.Amount;
                    requesterConn.identity.GetComponent<PlayerData>().ChangeFishBucksAmount(-requesterMoneySend, true);
                    receiverConn.identity.GetComponent<PlayerData>().ChangeFishBucksAmount(requesterMoneySend, true);
                }
            }
            foreach (TradableItem item in session.receiverTradeItems)
            {
                Debug.Log(item.Amount);
                if (item.Type == TradableItemType.Item)
                {
                    // Don't use the item referenced by the other player
                    ItemInstance newItem = new ItemInstance(item.ItemInst.def, item.Amount);
                    ItemInstance updated =  requesterInv.ServerMergeOrAdd(newItem, true);
                    if (!requesterUpdatedItems.Contains(updated))
                    {
                        requesterUpdatedItems.Add(updated);
                    }
                }
                else if (item.Type == TradableItemType.Bucks)
                {
                    receiverMoneySend = item.Amount;
                    requesterConn.identity.GetComponent<PlayerData>().ChangeFishBucksAmount(receiverMoneySend, true);
                    receiverConn.identity.GetComponent<PlayerData>().ChangeFishBucksAmount(-receiverMoneySend, true);
                }
            }

            // Remove items
            foreach (TradableItem item in session.requesterTradeItems)
            {
                if (item.Type == TradableItemType.Item)
                {
                    ItemInstance refItem = requesterInv.GetFirstNonFullStack(item.ItemInst.def.Id);
                    requesterInv.ServerRemoveAmountFromStack(refItem, item.Amount, true);
                    if (!requesterUpdatedItems.Contains(refItem))
                    {
                        requesterUpdatedItems.Add(refItem);
                    }
                    StackState stack = refItem.GetState<StackState>();
                    if (stack != null && stack.currentAmount <= 0)
                    {
                        requesterInv.RemoveItem(refItem.uuid);
                    }
                }
            }
            foreach (TradableItem item in session.receiverTradeItems)
            {
                if (item.Type == TradableItemType.Item)
                {
                    ItemInstance refItem = receiverInv.GetFirstNonFullStack(item.ItemInst.def.Id);
                    receiverInv.ServerRemoveAmountFromStack(refItem, item.Amount, true);
                    if (!receiverUpdatedItems.Contains(refItem))
                    {
                        receiverUpdatedItems.Add(refItem);
                    }
                    StackState stack = refItem.GetState<StackState>();
                    if (stack != null && stack.currentAmount <= 0)
                    {
                        receiverInv.RemoveItem(refItem.uuid);
                    }
                }
            }

            List<TradeItemRequest> requesterItemsUpdated = new List<TradeItemRequest>();
            List<TradeItemRequest> receiverItemsUpdated = new List<TradeItemRequest>();
            foreach (ItemInstance item in requesterUpdatedItems)
            {
                requesterItemsUpdated.Add(new TradeItemRequest
                {
                    item_uid = item.uuid.ToString(),
                    item_id = item.def.Id,
                    item_amount = item.GetState<StackState>().currentAmount,
                    state_blob = Convert.ToBase64String(StatePacker.Pack(item.state)),
                });
            }
            foreach (ItemInstance item in receiverUpdatedItems)
            {
                receiverItemsUpdated.Add(new TradeItemRequest
                {
                    item_uid = item.uuid.ToString(),
                    item_id = item.def.Id,
                    item_amount = item.GetState<StackState>().currentAmount,
                    state_blob = Convert.ToBase64String(StatePacker.Pack(item.state)),
                });
            }
            DatabaseCommunications.CommitTradeRequest(session.requesterId, session.receiverId, requesterItemsUpdated, receiverItemsUpdated, receiverMoneySend, requesterMoneySend);
        }

        [Server]
        public static void ServerDenyVerifyTrade(NetworkConnectionToClient denierConn, Guid tradeID)
        {
            PlayerData denierData = denierConn.identity.GetComponent<PlayerData>();
            Guid denierID = denierData.GetUuid();
            if (!TradeService.ServerTryGetRunning(tradeID, out TradeSession trade))
            {
                Debug.LogWarning("TradeSession not found");
                return;
            }
            if (!trade.IsOwnedBy(denierID))
            {
                return;
            }
            ResetAcceptState(tradeID);


            TradeRPCDenyVerifyTrade denyTradeMessage = new TradeRPCDenyVerifyTrade
            {
                tradeID = tradeID,
            };

            if (denierID == trade.receiverId)
            {
                GameNetworkManager.connUUID.TryGetValue(trade.requesterId, out NetworkConnectionToClient conn);
                conn?.Send(denyTradeMessage);
                return;
            } 
            else
            {
                GameNetworkManager.connUUID.TryGetValue(trade.receiverId, out NetworkConnectionToClient conn);
                conn?.Send(denyTradeMessage);
                return;
            }
        }

        [Server]
        public static void ResetAcceptState(Guid tradeID)
        {
            if (!TradeService.ServerTryGetRunning(tradeID, out TradeSession trade))
            {
                Debug.LogWarning("TradeSession not found");
                return;
            }
            trade.State = 0;
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
                case TradeCMDAcceptTrade cmd:
                    ServerAcceptTrade(requester, cmd.tradeID);
                    break;
                case TradeCMDVerifyTrade cmd:
                    ServerVerifyTrade(requester, cmd.tradeID);
                    break;
                case TradeCMDDenyVerifyTrade cmd:
                    ServerDenyVerifyTrade(requester, cmd.tradeID);
                    break;
            }
        }
    }
}

