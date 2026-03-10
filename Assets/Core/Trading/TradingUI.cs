namespace TradeSystem
{
    using System;
    using System.Collections.Generic;
    using ItemSystem;
    using UnityEngine;
    
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
        GameObject background;

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
            switch (state.Status)
            {
                case TradeStatus.Active:
                    {
                        TradeSession activeSession = TradeService.ClientGetRunning();
                        if (activeSession != null && activeSession.tradeId == state.TradeId)
                        {
                            OpenTradingMenu(activeSession);
                        }
                        break;
                    }

                case TradeStatus.Cancelled:
                    RunningTradeCanceled(TradingInfoType.ClosedByOther);
                    break;

                case TradeStatus.Expired:
                    InformPlayer(TradingInfoType.TradeExpired);
                    break;

                case TradeStatus.TradeItemsUpdated:
                    {
                        TradeSession ItemUpdatedSession = TradeService.ClientGetRunning();
                        if (ItemUpdatedSession != null && ItemUpdatedSession.tradeId == state.TradeId)
                        {
                            UpdateTradingMenu(ItemUpdatedSession);
                        }
                    break;
                    }
                default:
                    break;
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

        void MakeTradableInventory()
        {
            PlayerInventory inventory = GetComponentInParent<PlayerInventory>();
            List<ItemInstance> inventoryItems = inventory.GetItems();
            foreach(ItemInstance item in inventoryItems)
            {
                if (TradabilityRules.IsTradable(item))
                {
                    int amount = 1;
                    StackState stack = item.GetState<StackState>();
                    if (stack != null)
                    {
                        amount = stack.currentAmount;
                    }
                    GameObject tradableItem = Instantiate(tradableItemPrefab, tradableItemsInventoryContent.transform);
                    tradableItem.GetComponent<TradeSystemItemView>().SetTradableItem(TradableItem.FromItem(item, amount));
                }
            }
            GameObject bucksItem = Instantiate(tradableItemPrefab, tradableItemsInventoryContent.transform);
            bucksItem.GetComponent<TradeSystemItemView>().SetTradableItem(TradableItem.Bucks(GetComponentInParent<PlayerData>().GetFishBucks()));
        }

        public void OpenTradingMenu(TradeSession runningTrade)
        {
            background.SetActive(true);
            ResetTradingMenu();
            MakeTradableInventory();
        }

        public void UpdateTradingMenu(TradeSession runningTrade)
        {
            List<TradableItem> sendItems = runningTrade.receiverTradeItems;
            List<TradableItem> receivingItems = runningTrade.requesterTradeItems;
            if (GetComponentInParent<PlayerData>().GetUuid() == runningTrade.requesterId)
            {
                sendItems = runningTrade.requesterTradeItems;
                receivingItems = runningTrade.receiverTradeItems;
            }
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
                tradableItem.GetComponent<TradeSystemItemView>().SetTradableItem(item);
            }
            foreach (TradableItem item in receivingItems)
            {
                GameObject tradableItem = Instantiate(tradableItemPrefab, othersTradeInputContent.transform);
                tradableItem.GetComponent<TradeSystemItemView>().SetTradableItem(item);
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
        }

        public void CloseTradingMenuButtonu()
        {
            CloseTradingMenu();
            GetComponentInParent<Trading>().CancelCurrentTrade();
        }
    }
}
