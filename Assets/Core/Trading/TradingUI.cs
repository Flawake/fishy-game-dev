namespace TradeSystem
{
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
        GameObject background;

        public void InformPlayer(TradingInfoType infoType)
        {
            
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
            if (TradabilityRules.IsTradable(item))
            {
                GameObject tradableItem = Instantiate(tradableItemPrefab, tradableItemsInventoryContent.transform);
                tradableItem.GetComponent<TradeSystemItemView>().SetTradableItem(TradableItem.FromItem(item));
            }
        }

        public void OpenTradingMenu(RunningTrade runningTrade)
        {
            background.SetActive(true);
            ResetTradingMenu();
            MakeTradableInventory();
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
