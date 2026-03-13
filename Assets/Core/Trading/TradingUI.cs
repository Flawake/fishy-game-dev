namespace TradeSystem
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
        GameObject verifyTradeObject;


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
            switch (state.EventType)
            {
                case TradeEventType.RequestCreated:
                    break;
                case TradeEventType.RequestExpired:
                    InformPlayer(TradingInfoType.TradeExpired);
                    break;
                
                case TradeEventType.TradeStarted:
                    {
                        TradeSession activeSession = TradeService.ClientGetRunning();
                        if (activeSession != null && activeSession.tradeId == state.TradeId)
                        {
                            OpenTradingMenu(activeSession);
                        }
                        break;
                    }

                case TradeEventType.TradeCancelled:
                    RunningTradeCanceled(TradingInfoType.ClosedByOther);
                    break;

                case TradeEventType.TradeItemsUpdated:
                    {
                        Debug.Log("Items updated");
                        TradeSession tradeSession = TradeService.ClientGetRunning();
                        if (tradeSession != null && tradeSession.tradeId == state.TradeId)
                        {
                            UpdateTradingMenu(tradeSession);
                            MakeTradableInventory(tradeSession);
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

        void MakeTradableInventory(TradeSession currentTrade)
        {
            foreach (Transform item in tradableItemsInventoryContent.transform)
            {
                Destroy(item.gameObject);
            }
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
                    if (currentTrade != null)
                    {
                        var tradeItem = currentTrade
                            .GetOwnTradeList(GetComponentInParent<PlayerData>().GetUuid())
                            .FirstOrDefault(i => i.ItemInst != null && i.ItemInst.uuid == item.uuid);

                        if (tradeItem != null && tradeItem.Amount == item.GetState<StackState>().currentAmount)
                        {
                            continue;
                        }
                    }
                    GameObject tradableItem = Instantiate(tradableItemPrefab, tradableItemsInventoryContent.transform);
                    tradableItem.GetComponent<TradeSystemItemView>().SetTradableItem(TradableItem.FromItem(item, amount));
                }
            }
            int fishbucks = GetComponentInParent<PlayerData>().GetFishBucks();
            int fishbucksInTrade = currentTrade
                            .GetOwnTradeList(GetComponentInParent<PlayerData>().GetUuid())
                            .FirstOrDefault(i => i.Type == TradableItemType.Bucks)?.Amount ?? 0;
            if (fishbucks - fishbucksInTrade > 0)
            {
                GameObject bucksItem = Instantiate(tradableItemPrefab, tradableItemsInventoryContent.transform);
                bucksItem.GetComponent<TradeSystemItemView>().SetTradableItem(TradableItem.Bucks(GetComponentInParent<PlayerData>().GetFishBucks()));
            }
        }

        public void OpenTradingMenu(TradeSession runningTrade)
        {
            background.SetActive(true);
            ResetTradingMenu();
            MakeTradableInventory(runningTrade);
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
        public void AcceptradeButton()
        {
            GetComponentInParent<Trading>().AcceptTrade(true);
        }
    }
}
