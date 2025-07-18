using System;
using Mirror;
using NewItemSystem;
using UnityEngine;

public class StoreManager : NetworkBehaviour
{
    [SerializeField]
    PlayerData playerData;
    [SerializeField]
    PlayerInventory inventory;
    [SerializeField]
    PlayerDataSyncManager playerDataManager;

    public enum CurrencyType
    {
        coins,
        bucks
    }


    [Client]
    public void BuyItem(StoreItemObject item, CurrencyType currencyType)
    {
        CmdBuyItem(item.itemObject.type, item.itemObject.id, currencyType);
    }

    [Command]
    void CmdBuyItem(int itemID, CurrencyType currencyType)
    {
        //Don't trust the player on giving the whole item, only use the itemID of the item that the player wants to buy.
        ItemDefinition itemCopy = ItemsInGame.GetEmptyItemDefinitionCopy(itemID);
        ShopBehaviour shopBehaviour = itemCopy.GetBehaviour<ShopBehaviour>();
        if (shopBehaviour == null)
        {
            Debug.LogWarning("Player tried to buy an item which could not be bought");
            return;
        }

        if ((currencyType == CurrencyType.bucks && shopBehaviour.PriceBucks == -1) ||
            (currencyType == CurrencyType.coins && shopBehaviour.PriceCoins == -1))
        {
            Debug.LogWarning("Player tied to buy an item with a currency that the item does not support");
            return;
        }

        if(currencyType == CurrencyType.coins)
        {
            if(playerData.GetFishCoins() < shopBehaviour.PriceCoins)
            {
                return;
            }
            playerDataManager.AddItem(itemToBuy.itemObject);
            playerDataManager.ChangeFishCoinsAmount(-shopBehaviour.PriceCoins);
        }
        else if(currencyType == CurrencyType.bucks)
        {
            if (playerData.GetFishBucks() < shopBehaviour.PriceBucks)
            {
                return;
            }
            playerDataManager.AddItem(itemToBuy.itemObject);
            playerDataManager.ChangeFishBucksAmount(-shopBehaviour.PriceBucks);
        }
    }
}
