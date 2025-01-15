using Mirror;
using System.Collections;
using System.Collections.Generic;
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


    public void BuyItem(StoreItemObject item, CurrencyType currencyType)
    {
        CmdBuyItem(item.itemObject.type, item.itemObject.id, currencyType);
    }

    [Command]
    void CmdBuyItem(ItemType type, int itemID, CurrencyType currencyType)
    {
        StoreItemObject[] itemsToSearch;
        StoreItemObject itemToBuy = null;
        //Don't trust the player on giving the whole item, only give the details that identify the item and then search the item that corresponds to it on the server.
        if(type == ItemType.rod)
        {
            itemsToSearch = ItemsInGame.storeItemRods;
        }
        else if(type == ItemType.bait)
        {
            itemsToSearch = ItemsInGame.storeItemBaits;
        }
        else
        {
            Debug.LogWarning($"CmdBuyItem does not yet support buying items of type {type}");
            return;
        }

        foreach (StoreItemObject item in itemsToSearch)
        {
            if(item.itemObject.id == itemID) { 
                itemToBuy = item;
                break;
            }
        }
        if(itemToBuy == null)
        {
            return;
        }

        if(currencyType == CurrencyType.coins)
        {
            if(itemToBuy.itemPriceFishCoins <= 0)
            {
                return;
            }
            if(playerData.GetFishCoins() < itemToBuy.itemPriceFishCoins)
            {
                return;
            }
            playerDataManager.AddItem(itemToBuy.itemObject);
            playerDataManager.ChangeFishCoinsAmount(-itemToBuy.itemPriceFishCoins);
        }
        else if(currencyType == CurrencyType.bucks)
        {
            if (itemToBuy.itemPriceFishBucks <= 0)
            {
                return;
            }
            if (playerData.GetFishBucks() < itemToBuy.itemPriceFishBucks)
            {
                return;
            }
            playerDataManager.AddItem(itemToBuy.itemObject);
            playerDataManager.ChangeFishBucksAmount(-itemToBuy.itemPriceFishBucks);
        }
    }
}
