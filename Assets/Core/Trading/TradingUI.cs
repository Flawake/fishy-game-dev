namespace TradeSystem
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ItemSystem;
    using Mirror;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class TradingUIManager : MonoBehaviour
    {
        [SerializeField]
        GameObject tradableItemPrefab;
        [SerializeField]
        GameObject tradableItemsInventoryContent;
        [SerializeField]
        GameObject yourTradeInputContent;
        [SerializeField]
        GameObject othersTradeInputContent;
        [SerializeField]
        GameObject amountSelector;
        [SerializeField]
        GameObject verifyTradeObject;
        [SerializeField]
        TMP_Text ownNameField;
        [SerializeField]
        TMP_Text otherNameField;

        [SerializeField]
        Image ownTradeStateImage;
        [SerializeField]
        TMP_Text ownTradeStateText;
        [SerializeField]
        Image otherTradeStateImage;
        [SerializeField]
        TMP_Text otherTradeStateText;
        [SerializeField]
        TMP_Dropdown tradeFilter;

        [SerializeField]
        Sprite checkMark;
        [SerializeField]
        Sprite explenationMark;


        [SerializeField]
        GameObject background;

        private String makingOfferText = "Making offer";
        private String readyText = "Ready";

        enum ItemFiler
        {
            EVERYTHING,
            FISH,
            SHELLS,
            BAITS,
            RODS,
            SPECIAL,

        }

        public void InformPlayer(TradingInfoType infoType)
        {
            // TODO: hook up to popups / notifications.
            Debug.Log($"Trading info: {infoType}");
        }

        void OnEnable()
        {
            TradeEvents.ClientTradeStateChanged += OnTradeStateChanged;
        }

        void OnDisable()
        {
            TradeEvents.ClientTradeStateChanged -= OnTradeStateChanged;
        }

        void OnTradeStateChanged(TradeViewModel state)
        {
            switch (state.EventType)
            {
                case TradeEventType.RequestCreated:
                    {
                        Notification notification = new Notification();
                        if (TradeService.TryGetPending(state.TradeId, out PendingTradeRequest pending))
                        {
                            notification.message = $"{pending.requesterName} sent you a trade request";
                            // Clicking the notification accepts the trade request.
                            notification.callback = () => GetComponentInParent<Trading>().AcceptTradeRequest(pending);
                        }
                        else
                        {
                            notification.message = "Someone sent you a trade request";
                        }
                        MessageUIHandler.AddNotification(notification);
                        break;
                    }
                case TradeEventType.RequestExpired:
                    InformPlayer(TradingInfoType.TradeExpired);
                    MessageUIHandler.AddNotification(new Notification
                    {
                        message = "A trade request timed out"
                    });
                    break;
                
                case TradeEventType.TradeStarted:
                    {
                        TradeSession activeSession = TradeService.ClientGetRunning();
                        if (activeSession != null && activeSession.tradeId == state.TradeId)
                        {
                            OpenTradingMenu(activeSession);
                        }
                        ownTradeStateImage.sprite = explenationMark;
                        otherTradeStateImage.sprite = explenationMark;
                        ownTradeStateText.text = makingOfferText;
                        otherTradeStateText.text = makingOfferText;
                        break;
                    }

                case TradeEventType.TradeCancelled:
                    RunningTradeCanceled(TradingInfoType.ClosedByOther);
                    MessageUIHandler.AddNotification(new Notification
                    {
                        message = "The other player cancelled the trade"
                    });
                    break;

                case TradeEventType.TradeItemsUpdated:
                    {
                        TradeSession tradeSession = TradeService.ClientGetRunning();
                        if (tradeSession != null && tradeSession.tradeId == state.TradeId)
                        {
                            UpdateTradingMenu(tradeSession);
                            MakeTradableInventory(tradeSession, ItemFiler.EVERYTHING);
                        }
                        break;
                    }
                case TradeEventType.OpenVerifyMenu:
                    {
                        TradeSession tradeSession = TradeService.ClientGetRunning();
                        if (tradeSession != null && tradeSession.BothPlayersAccepted())
                        {
                            verifyTradeObject.SetActive(true);
                        }
                        break;
                    }
                case TradeEventType.CloseVerifyMenu:
                    {
                        verifyTradeObject.SetActive(false);
                        break;
                    }
                case TradeEventType.TradeCompleted:
                    {
                        CloseTradingMenu();
                        MessageUIHandler.AddNotification(new Notification
                        {
                            message = "Trade completed successfully"
                        });
                        break;
                    }
                case TradeEventType.ResetReadyState:
                    {
                        ownTradeStateImage.sprite = explenationMark;
                        otherTradeStateImage.sprite = explenationMark;
                        ownTradeStateText.text = makingOfferText;
                        otherTradeStateText.text = makingOfferText;
                        break;
                    }
                case TradeEventType.AcceptSelf:
                    {
                        ownTradeStateImage.sprite = checkMark;
                        ownTradeStateText.text = readyText;
                        break;
                    }
                case TradeEventType.AcceptOther:
                    {
                        otherTradeStateImage.sprite = checkMark;
                        otherTradeStateText.text = readyText;
                        break;
                    }
                default:
                    throw new NotImplementedException("Unhandled TradeEventType");
            }
        }
    
        void ResetTradingMenu()
        {
            foreach (Transform item in tradableItemsInventoryContent.transform)
            {
                Destroy(item.gameObject);
            }
            foreach (Transform item in yourTradeInputContent.transform)
            {
                Destroy(item.gameObject);
            }
            foreach (Transform item in othersTradeInputContent.transform)
            {
                Destroy(item.gameObject);
            }
        }

        void MakeTradableInventory(TradeSession currentTrade, ItemFiler filter)
        {
            var playerData = GetComponentInParent<PlayerData>();
            var inventory = GetComponentInParent<PlayerInventory>();

            var tradableItems = BuildTradableItems(
                inventory.GetItems(),
                currentTrade,
                playerData.GetUuid(),
                playerData.GetFishBucks(),
                filter);

            RenderTradableItems(tradableItems);
        }

        IEnumerable<TradableItem> BuildTradableItems(
            IEnumerable<ItemInstance> inventoryItems,
            TradeSession currentTrade,
            Guid playerUuid,
            int fishbucks,
            ItemFiler filter)
        {
            List<TradableItem> tradeList = currentTrade?.GetOwnTradeList(playerUuid) 
                            ?? null;

            IEnumerable<TradableItem> itemTradables = inventoryItems
                .Where(TradabilityRules.IsTradable)
                .Select(item => CreateTradableItem(item, tradeList))
                .Where(t => t != null);

            TradableItem bucksTradable = CreateBucksTradable(fishbucks, tradeList);

            if(bucksTradable != null)
            {
                itemTradables = itemTradables.Append(bucksTradable);
            }

            return filter switch
            {
                ItemFiler.EVERYTHING => itemTradables,
                ItemFiler.FISH => itemTradables.Where(item => item.HasBehaviour<FishBehaviour>()),
                ItemFiler.SHELLS => itemTradables.Where(item => item.HasBehaviour<ShellBehaviour>()),
                ItemFiler.BAITS => itemTradables.Where(item => item.HasBehaviour<BaitBehaviour>()),
                ItemFiler.RODS => itemTradables.Where(item => item.HasBehaviour<RodBehaviour>()),
                ItemFiler.SPECIAL => itemTradables.Where(item => item.HasBehaviour<SpecialBehaviour>()),
                _ => throw new Exception("Unhandled option"),
            };
        }


        TradableItem CreateTradableItem(ItemInstance item, IEnumerable<TradableItem> tradeList)
        {
            int amount = item.GetState<StackState>()?.currentAmount ?? 0;

            int amountInTrade = tradeList
                .FirstOrDefault(t => t.ItemInst?.uuid == item.uuid)
                ?.Amount ?? 0;

            int available = amount - amountInTrade;

            return available > 0
                ? TradableItem.FromItem(item, available)
                : null;
        }

        TradableItem CreateBucksTradable(int fishbucks, IEnumerable<TradableItem> tradeList)
        {
            int bucksInTrade = tradeList
                .FirstOrDefault(t => t.Type == TradableItemType.Bucks)
                ?.Amount ?? 0;

            int available = fishbucks - bucksInTrade;

            return available > 0
                ? TradableItem.Bucks(available)
                : null;
        }

        void RenderTradableItems(IEnumerable<TradableItem> items)
        {
            foreach (Transform child in tradableItemsInventoryContent.transform)
            {
                Destroy(child.gameObject);
            }

            foreach (TradableItem item in items)
            {
                GameObject gameObject = Instantiate(tradableItemPrefab, tradableItemsInventoryContent.transform);
                gameObject.GetComponent<TradeSystemItemView>().SetTradableItem(item);
            }
        }

        public void OpenTradingMenu(TradeSession runningTrade)
        {
            background.SetActive(true);
            tradeFilter.value = 0;
            ResetTradingMenu();
            MakeTradableInventory(runningTrade, ItemFiler.EVERYTHING);
            string ownName = GetComponentInParent<PlayerData>().GetUuid() == runningTrade.receiverId ? 
                runningTrade.receiverName : 
                runningTrade.requesterName;

            string otherName = GetComponentInParent<PlayerData>().GetUuid() == runningTrade.receiverId ? 
                runningTrade.requesterName : 
                runningTrade.receiverName;

            ownNameField.text = ownName;
            otherNameField.text = otherName;
        }

        public void UpdateTradingMenu(TradeSession runningTrade)
        {
            Guid thisPlayerID = GetComponentInParent<PlayerData>().GetUuid();
            List<TradableItem> sendItems = runningTrade.GetOwnTradeList(thisPlayerID);
            List<TradableItem> receivingItems = runningTrade.GetOtherTradeList(thisPlayerID);

            foreach(Transform child in yourTradeInputContent.transform)
            {
                Destroy(child.gameObject);
            }
            foreach(Transform child in othersTradeInputContent.transform)
            {
                Destroy(child.gameObject);
            }

            foreach (TradableItem item in sendItems)
            {
                GameObject tradableItem = Instantiate(tradableItemPrefab, yourTradeInputContent.transform);
                tradableItem.GetComponent<TradeSystemItemView>().SetTradableItem(item).EnableRemoveButton();
            }
            foreach (TradableItem item in receivingItems)
            {
                GameObject tradableItem = Instantiate(tradableItemPrefab, othersTradeInputContent.transform);
                tradableItem.GetComponent<TradeSystemItemView>().SetTradableItem(item);
            }
        }

        [Client]
        public void FilterUpdated()
        {
            ItemFiler filter = (ItemFiler)tradeFilter.value;
            TradeSession tradeSession = TradeService.ClientGetRunning();
            if (tradeSession != null)
            {
                MakeTradableInventory(tradeSession, filter);
            }
        }

        public void RunningTradeCanceled(TradingInfoType infoType)
        {
            InformPlayer(infoType);
            CloseTradingMenu();
        }

        public void CloseTradingMenu()
        {
            background.SetActive(false);
            amountSelector.SetActive(false);
            verifyTradeObject.SetActive(false);
        }

        // Called from button in game
        public void CloseTradingMenuButtonu()
        {
            CloseTradingMenu();
            GetComponentInParent<Trading>().CancelCurrentTrade();
        }

        // Called from button in game
        public void AcceptTradeButton()
        {
            GetComponentInParent<Trading>().AcceptTrade(true);
        }
    }
}
