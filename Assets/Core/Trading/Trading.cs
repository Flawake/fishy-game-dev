namespace TradeSystem
{
    using UnityEngine;
    using Mirror;
    using System;

    public class Trading : NetworkBehaviour
    {
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
            NetworkClient.RegisterHandler<TradeRPCAcceptTrade>(msg => TargetHandleTradeRpc(msg));
            NetworkClient.RegisterHandler<TradeRPCTradeAccepted>(msg => TargetHandleTradeRpc(msg));
            NetworkClient.RegisterHandler<TradeRPCVerifyTrade>(msg => TargetHandleTradeRpc(msg));
            NetworkClient.RegisterHandler<TradeRPCDenyVerifyTrade>(msg => TargetHandleTradeRpc(msg));
            NetworkClient.RegisterHandler<TradeRPCTradeVerified>(msg => TargetHandleTradeRpc(msg));
        }

        [Client]
        public void SelectItemToTrade(TradableItem item)
        {
            TradeAmountSelector amountSelector = GetComponentInChildren<TradeAmountSelector>(true);
            amountSelector.gameObject.SetActive(true);
            amountSelector.GetComponent<TradeAmountSelector>().SetItem(item);
        }

        public void AddItemToTrade(TradableItem newItem, bool addedBySelf)
        {
            ResetAcceptState();
            TradeSession current = TradeService.ClientGetRunning();
            Guid thisPlayerID = GetComponentInParent<PlayerData>().GetUuid();

            var list = addedBySelf ? 
                current.GetOwnTradeList(thisPlayerID) : 
                current.GetOtherTradeList(thisPlayerID);

            int index = list.FindIndex(item => item.Eq(newItem));

            if (newItem.Amount <= 0)
            {
                if (index >= 0)
                {
                    list.RemoveAt(index);
                }
            }
            else
            {
                // Remove item istead of update to make clear to the player that the item amount has been changed. Newly added items change to last position
                if (index >= 0)
                {
                    list.RemoveAt(index);
                }

                list.Add(newItem);
            }
            
            if (addedBySelf)
            {
                TradeCMDItemAdded command = new TradeCMDItemAdded
                {
                    tradeID = current.tradeId,
                    itemAdded = newItem,
                };
                NetworkClient.Send(command);
                TradeEvents.RaiseClient(TradeEventType.TradeItemsUpdated, current.tradeId);
            }
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
        public void AcceptTrade(bool accpetedBySelf)
        {
            TradeSession currentTrade = TradeService.ClientGetRunning();

            if (currentTrade == null)
            {
                return;
            }

            bool isRequester = GetComponentInParent<PlayerData>().GetUuid() == currentTrade.requesterId;

            TradeSessionState selfFlag = isRequester
                ? TradeSessionState.RequesterAccepted
                : TradeSessionState.ReceiverAccepted;

            TradeSessionState otherFlag = isRequester
                ? TradeSessionState.ReceiverAccepted
                : TradeSessionState.RequesterAccepted;

            currentTrade.State |= accpetedBySelf ? selfFlag : otherFlag;

            if (accpetedBySelf)
            {
                TradeCMDAcceptTrade command = new TradeCMDAcceptTrade
                {
                    tradeID = currentTrade.tradeId,
                };

                NetworkClient.Send(command);
            }
        }

        [Client]
        public void VerifyTrade(bool accpetedBySelf)
        {
            TradeSession currentTrade = TradeService.ClientGetRunning();
            if (currentTrade == null)
            {
                return;
            }

            if (accpetedBySelf)
            {
                TradeCMDVerifyTrade command = new TradeCMDVerifyTrade
                {
                    tradeID = currentTrade.tradeId,
                };

                NetworkClient.Send(command);
            }
        }

        [Client]
        public void DenyVerifyTrade()
        {
            TradeSession currentTrade = TradeService.ClientGetRunning();
            if (currentTrade == null)
            {
                return;
            }
            ResetAcceptState();
            TradeCMDDenyVerifyTrade command = new TradeCMDDenyVerifyTrade
            {
                tradeID = currentTrade.tradeId,
            };

            NetworkClient.Send(command);
        }

        [Client]
        public void ResetAcceptState()
        {
            TradeSession currentTrade = TradeService.ClientGetRunning();
            currentTrade.State = 0;
            TradeEvents.RaiseClient(TradeEventType.CloseVerifyMenu, currentTrade.tradeId);
        }

        [Client]
        void TargetHandleTradeRpc(NetworkMessage message)
        {
            switch (message)
            {
                case TradeRPCRequestIncoming req:
                    {
                        TradeService.AddPending(req.Pending);
                        TradeEvents.RaiseClient(TradeEventType.RequestCreated, req.Pending.tradeId);
                        break;
                    }
                case TradeRPCRequestRemoved req:
                    {
                        TradeService.RemovePending(req.TradeId);
                        if (req.CancelReason == CancelTradeRequestReason.AcceptedButUnavailable)
                        {
                            TradeEvents.RaiseClient(TradeEventType.RequestCancelled, req.TradeId);
                        }
                        break;
                    }
                case TradeRPCTradeRequestExpired req:
                    {
                        TradeService.RemovePending(req.PendingID);
                        TradeEvents.RaiseClient(TradeEventType.RequestExpired, req.PendingID);
                        break;
                    }
                case TradeRPCTradeStarted req:
                    {
                        TradeService.RemovePending(req.Session.tradeId);
                        TradeService.ClientSetRunning(req.Session);
                        TradeEvents.RaiseClient(TradeEventType.TradeStarted, req.Session.tradeId);
                        break;
                    }
                case TradeRPCTradeCancelled req:
                    {
                        TradeSession current = TradeService.ClientGetRunning();
                        TradeService.ClientRemoveRunning();
                        if (current != null)
                        {
                            TradeEvents.RaiseClient(TradeEventType.TradeCancelled, current.tradeId);
                        }
                        break;
                    }
                case TradeRPCTradeitemAdded req:
                    {
                        TradeSession current = TradeService.ClientGetRunning();
                        AddItemToTrade(req.addedItem, false);
                        if (current != null)
                        {
                            TradeEvents.RaiseClient(TradeEventType.TradeItemsUpdated, current.tradeId);
                        }
                        break;
                    }
                case TradeRPCAcceptTrade req:
                    {
                        AcceptTrade(false);
                        break;
                    }
                case TradeRPCTradeAccepted req:
                    {
                        TradeEvents.RaiseClient(TradeEventType.OpenVerifyMenu, req.tradeID);
                        break;
                    }
                case TradeRPCVerifyTrade req:
                    {
                        VerifyTrade(false);
                        break;
                    }
                case TradeRPCDenyVerifyTrade req:
                    {
                        ResetAcceptState();
                        break;
                    }
                case TradeRPCTradeVerified req:
                    {
                        TradeService.ClientRemoveRunning();
                        TradeEvents.RaiseClient(TradeEventType.TradeCompleted, req.tradeID);
                        break;
                    }
            }
        }
    }
}
