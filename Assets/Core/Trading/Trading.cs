namespace TradeSystem
{
    using UnityEngine;
    using ItemSystem;
    using Mirror;
    using System;

    public class Trading : NetworkBehaviour
    {
        #region Client

        private void Start()
        {
            if (!isLocalPlayer)
            {
                return;
            }
            Debug.Log("registering");
            NetworkClient.RegisterHandler<TradeRPCRequestIncoming>(msg => TargetHandleTradeRpc(msg));
            NetworkClient.RegisterHandler<TradeRPCRequestRemoved>(msg => TargetHandleTradeRpc(msg));
            NetworkClient.RegisterHandler<TradeRPCTradeRequestExpired>(msg => TargetHandleTradeRpc(msg));
            NetworkClient.RegisterHandler<TradeRPCTradeStarted>(msg => TargetHandleTradeRpc(msg));
            NetworkClient.RegisterHandler<TradeRPCTradeCancelled>(msg => TargetHandleTradeRpc(msg));
            NetworkClient.RegisterHandler<TradeRPCTradeitemAdded>(msg => TargetHandleTradeRpc(msg));
        }

        [Client]
        public void SelectItemToTrade(TradableItem item)
        {
            TradeAmountSelector amountSelector = GetComponentInChildren<TradeAmountSelector>(true);
            amountSelector.gameObject.SetActive(true);
            amountSelector.GetComponent<TradeAmountSelector>().SetItem(item);
        }

        public void AddItemToTrade(TradableItem item)
        {
            TradeSession current = TradeService.ClientGetRunning();
            if (GetComponentInParent<PlayerData>().GetUuid() == current.receiverId)
            {
                current.requesterTradeItems.Add(item);
            }
            else
            {
                current.receiverTradeItems.Add(item);
            }
            TradeEvents.RaiseClient(TradeStatus.TradeItemsUpdated);
            TradeCMDItemAdded command = new TradeCMDItemAdded
            {
                tradeID = current.tradeId,
                itemAdded = item,
            };
            NetworkClient.Send(command);
        }

        [Client]
        public void RequestNewTrade(Guid playerToRequestId, string _playerToRequestName)
        {
            TradeCMDRequestNewTrade command = new TradeCMDRequestNewTrade
            {
                requestTargetID = playerToRequestId,
            };
            NetworkClient.Send(command);
        }

        [Client]
        public void CancelCurrentTrade()
        {
            TradeSession current = TradeService.ClientGetRunning();
            if (current == null)
            {
                return;
            }

            TradeCMDCancelTrade command = new TradeCMDCancelTrade
            {
                tradeID = current.tradeId,
            };

            NetworkClient.Send(command);

            TradeService.ClientRemoveRunning();
        }

        [Client]
        public void AcceptTradeRequest(PendingTradeRequest request)
        {
            TradeCMDAcceptTradeRequest command = new TradeCMDAcceptTradeRequest
            {
                tradeID = request.tradeId,
            };

            NetworkClient.Send(command);
        }

        [Client]
        void TargetHandleTradeRpc(NetworkMessage message)
        {
            switch (message)
            {
                case TradeRPCRequestIncoming req:
                    {
                        TradeService.AddPending(req.Pending);
                        TradeEvents.RaiseClient(req.Pending, TradeEventType.RequestCreated, TradeStatus.PendingRequest);
                        break;
                    }
                case TradeRPCRequestRemoved req:
                    {
                        TradeService.RemovePending(req.TradeId);
                        if (req.CancelReason == CancelTradeRequestReason.AcceptedButUnavailable)
                        {
                            TradeEvents.RaiseClient(TradeStatus.Cancelled);
                        }
                        break;
                    }
                case TradeRPCTradeRequestExpired req:
                    {
                        TradeService.RemovePending(req.PendingID);
                        TradeEvents.RaiseClient(TradeStatus.Expired);
                        break;
                    }
                case TradeRPCTradeStarted req:
                    {
                        TradeService.RemovePending(req.Session.tradeId);
                        TradeService.ClientSetRunning(req.Session);
                        TradeEvents.RaiseClient(req.Session, TradeEventType.TradeStarted, TradeStatus.Active);
                        break;
                    }
                case TradeRPCTradeCancelled req:
                    {
                        TradeSession current = TradeService.ClientGetRunning();
                        TradeService.ClientRemoveRunning();
                        if (current != null)
                        {
                            TradeEvents.RaiseClient(current, TradeEventType.TradeCancelled, TradeStatus.Cancelled);
                        }
                        break;
                    }
                case TradeRPCTradeitemAdded req:
                    {
                        TradeSession current = TradeService.ClientGetRunning();
                        TradeSession currentSession = TradeService.ClientGetRunning();
                        if (currentSession.requesterId == GetComponentInParent<PlayerData>().GetUuid())
                        {
                            current.receiverTradeItems.Add(req.addedItem);
                        }
                        else
                        {
                            current.requesterTradeItems.Add(req.addedItem);
                        }
                        if (current != null)
                        {
                            TradeEvents.RaiseClient(TradeStatus.TradeItemsUpdated);
                        }
                        break;
                    }
            }
        }

        #endregion
    }
}
