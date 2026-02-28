namespace TradeSystem
{
    using Mirror;
    using System;

    public class Trading : NetworkBehaviour
    {
        #region Shared helpers

        bool TradeAlreadyRequestedByOther(Guid playerToRequestId, out PendingTradeRequest pendingRequest)
        {
            Guid thisPlayerId = GetComponentInParent<PlayerData>().GetUuid();
            return TradeService.TryGetPendingFromTo(playerToRequestId, thisPlayerId, out pendingRequest);
        }

        bool CanMakeTradeRequest(Guid playerToRequestId)
        {
            Guid thisPlayerID = GetComponentInParent<PlayerData>().GetUuid();

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

        #endregion

        #region Client

        [Client]
        public void RequestNewTrade(Guid playerToRequestId, string _playerToRequestName)
        {
            TradeCommandMessage command = new TradeCommandMessage
            {
                Type = TradeCommandType.RequestNewTrade,
                TargetPlayerId = playerToRequestId,
            };
            CmdHandleTradeCommand(command);
        }

        [Client]
        public void CancelCurrentTrade()
        {
            TradeSession current = TradeService.ClientGetRunning();
            if (current == null)
            {
                return;
            }

            TradeCommandMessage command = new TradeCommandMessage
            {
                Type = TradeCommandType.CancelTrade,
                TradeId = current.tradeId,
            };

            CmdHandleTradeCommand(command);

            TradeService.ClientRemoveRunning();
        }

        [Client]
        public void AcceptTradeRequest(PendingTradeRequest request)
        {
            TradeCommandMessage command = new TradeCommandMessage
            {
                Type = TradeCommandType.AcceptRequest,
                TradeId = request.tradeId,
            };

            CmdHandleTradeCommand(command);
        }

        [TargetRpc]
        void TargetHandleTradeRpc(TradeRpcMessage message)
        {
            switch (message.Type)
            {
                case TradeRpcType.RequestIncoming:
                    {
                        TradeService.AddPending(message.Pending);
                        TradeEvents.RaiseClient(message.Pending, TradeEventType.RequestCreated, TradeStatus.PendingRequest);
                        break;
                    }

                case TradeRpcType.RequestRemoved:
                    {
                        TradeService.RemovePending(message.Pending.tradeId);
                        if (message.CancelReason == CancelTradeRequestReason.AcceptedButUnavailable)
                        {
                            TradeEvents.RaiseClient(message.Pending, TradeEventType.TradeCancelled, TradeStatus.Cancelled);
                        }
                        break;
                    }

                case TradeRpcType.TradeStarted:
                    {
                        TradeService.RemovePending(message.Session.tradeId);
                        TradeService.ClientSetRunning(message.Session);
                        TradeEvents.RaiseClient(message.Session, TradeEventType.TradeStarted, TradeStatus.Active);
                        break;
                    }

                case TradeRpcType.TradeCancelled:
                    {
                        TradeSession current = TradeService.ClientGetRunning();
                        TradeService.ClientRemoveRunning();
                        if (current != null)
                        {
                            TradeEvents.RaiseClient(current, TradeEventType.TradeCancelled, TradeStatus.Cancelled);
                        }
                        break;
                    }

                case TradeRpcType.TradeExpired:
                    {
                        TradeService.RemovePending(message.Pending.tradeId);
                        TradeEvents.RaiseClient(message.Pending, TradeEventType.TradeExpired, TradeStatus.Expired);
                        break;
                    }
            }
        }

        #endregion

        #region Server

        [Command]
        void CmdHandleTradeCommand(TradeCommandMessage command, NetworkConnectionToClient sender = null)
        {
            switch (command.Type)
            {
                case TradeCommandType.RequestNewTrade:
                    ServerHandleRequestNewTrade(command.TargetPlayerId);
                    break;
                case TradeCommandType.AcceptRequest:
                    ServerHandleAcceptTradeRequest(command.TradeId, sender);
                    break;
                case TradeCommandType.CancelTrade:
                    ServerHandleCancelTrade(command.TradeId, sender);
                    break;
            }
        }

        [Server]
        bool IDOwnsTradeRequest(PendingTradeRequest req, Guid claimer)
        {
            return req.Owns(claimer);
        }

        [Server]
        void ServerHandleRequestNewTrade(Guid playerToRequestId)
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
                tradeId = Guid.NewGuid(),
                requestTime = DateTime.Now,
                requesterId = requesterID,
                requesterName = requesterName,
                receiverId = playerToRequestId,
                receiverName = playerToRequestName,
            };

            TradeService.AddPending(tradeRequest);

            TradeRpcMessage rpc = new TradeRpcMessage
            {
                Type = TradeRpcType.RequestIncoming,
                Pending = tradeRequest,
            };

            otherConnection.identity.GetComponent<Trading>().TargetHandleTradeRpc(rpc);
        }

        [Server]
        void ServerHandleAcceptTradeRequest(Guid tradeId, NetworkConnectionToClient sender)
        {
            if (!TradeService.TryGetPending(tradeId, out PendingTradeRequest tradeRequest))
            {
                // Either crafted or expired on the server already.
                TradeRpcMessage expiredRpc = new TradeRpcMessage
                {
                    Type = TradeRpcType.TradeExpired,
                    Pending = default,
                };
                TargetHandleTradeRpc(expiredRpc);
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
        void ServerTradeRequestAccepted(PendingTradeRequest tradeRequest, Guid accepter)
        {
            if (TradeService.ServerIsPlayerTrading(accepter))
            {
                return;
            }
            if (TradeService.ServerIsPlayerTrading(tradeRequest.receiverId))
            {
                if (GameNetworkManager.connUUID.TryGetValue(accepter, out NetworkConnectionToClient accepterConnection))
                {
                    TradeRpcMessage removeRpc = new TradeRpcMessage
                    {
                        Type = TradeRpcType.RequestRemoved,
                        Pending = tradeRequest,
                        CancelReason = CancelTradeRequestReason.AcceptedButUnavailable,
                    };

                    accepterConnection.identity.GetComponent<Trading>().TargetHandleTradeRpc(removeRpc);
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

                TradeRpcMessage startedRpc = new TradeRpcMessage
                {
                    Type = TradeRpcType.TradeStarted,
                    Session = runningTrade,
                };

                requesterConnection.identity.GetComponent<Trading>().TargetHandleTradeRpc(startedRpc);
                receiverConnection.identity.GetComponent<Trading>().TargetHandleTradeRpc(startedRpc);
            }
            
            TradeRpcMessage failedRPC = new TradeRpcMessage
            {
                Type = TradeRpcType.TradeExpired,
                Pending = tradeRequest,
            };

            if (requesterConnection == null && receiverConnection != null)
            {
                receiverConnection.identity.GetComponent<Trading>().TargetHandleTradeRpc(failedRPC);
            }

            if (requesterConnection != null && receiverConnection == null)
            {
                requesterConnection.identity.GetComponent<Trading>().TargetHandleTradeRpc(failedRPC);
            }
        }

        [Server]
        void ServerHandleCancelTrade(Guid tradeId, NetworkConnectionToClient sender)
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
                TradeRpcMessage cancelledRpc = new TradeRpcMessage
                {
                    Type = TradeRpcType.TradeCancelled,
                    Session = tradeToCancel,
                };

                receiverConnection.identity.GetComponent<Trading>().TargetHandleTradeRpc(cancelledRpc);
            }
        }

        #endregion
    }
}
