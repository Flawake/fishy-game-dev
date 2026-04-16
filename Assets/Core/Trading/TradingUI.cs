namespace TradeSystem
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ItemSystem;
    using TMPro;
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
        TMP_Text own_name_field;
        [SerializeField]
        TMP_Text other_name_field;


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
            var playerData = GetComponentInParent<PlayerData>();
            var inventory = GetComponentInParent<PlayerInventory>();

            var tradableItems = BuildTradableItems(
                inventory.GetItems(),
                currentTrade,
                playerData.GetUuid(),
                playerData.GetFishBucks());

            RenderTradableItems(tradableItems);
        }

        IEnumerable<TradableItem> BuildTradableItems(
            IEnumerable<ItemInstance> inventoryItems,
            TradeSession currentTrade,
            Guid playerUuid,
            int fishbucks)
        {
            List<TradableItem> tradeList = currentTrade?.GetOwnTradeList(playerUuid) 
                            ?? null;

            IEnumerable<TradableItem> itemTradables = inventoryItems
                .Where(TradabilityRules.IsTradable)
                .Select(item => CreateTradableItem(item, tradeList))
                .Where(t => t != null);

            TradableItem bucksTradable = CreateBucksTradable(fishbucks, tradeList);

            return bucksTradable != null
                ? itemTradables.Append(bucksTradable)
                : itemTradables;
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
            ResetTradingMenu();
            MakeTradableInventory(runningTrade);
            string ownName = GetComponentInParent<PlayerData>().GetUuid() == runningTrade.receiverId ? 
                runningTrade.receiverName : 
                runningTrade.requesterName;

            string otherName = GetComponentInParent<PlayerData>().GetUuid() == runningTrade.receiverId ? 
                runningTrade.requesterName : 
                runningTrade.receiverName;

            own_name_field.text = ownName;
            other_name_field.text = otherName;
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
