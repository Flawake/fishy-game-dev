using Mirror;
using ItemSystem;
using UnityEngine;
using System;

public class StoreManager : NetworkBehaviour
{
    [SerializeField]
    PlayerData playerData;
    [SerializeField]
    PlayerDataSyncManager playerDataManager;

    public enum CurrencyType
    {
        coins,
        bucks
    }

    // Optimistic update tracking
    private struct PendingPurchase
    {
        public int itemId;
        public CurrencyType currencyType;
        public int amount;
        public DateTime timestamp;
        public Guid tempUuid;
    }

    private PendingPurchase? pendingPurchase = null;

    [Client]
    public void BuyItem(ItemDefinition item, CurrencyType currencyType)
    {
        if (TryOptimisticPurchase(item, currencyType))
        {
            CmdBuyItem(item.Id, currencyType);
        }
        else
        {
            Debug.LogWarning($"Optimistic purchase failed for {item.DisplayName}");
        }
    }

    [Client]
    private bool TryOptimisticPurchase(ItemDefinition item, CurrencyType currencyType)
    {
        if (pendingPurchase.HasValue)
        {
            Debug.LogWarning("Cannot make purchase while another purchase is pending verification");
            return false;
        }

        ShopBehaviour shopBehaviour = item.GetBehaviour<ShopBehaviour>();
        if (shopBehaviour == null)
        {
            Debug.LogWarning("Item cannot be bought");
            return false;
        }
        
        if ((currencyType == CurrencyType.bucks && shopBehaviour.PriceBucks <= 0) ||
            (currencyType == CurrencyType.coins && shopBehaviour.PriceCoins <= 0))
        {
            Debug.LogWarning("Player tried to buy an item with a currency that the item does not support");
            return false;
        }

        int price = currencyType == CurrencyType.coins ? shopBehaviour.PriceCoins : shopBehaviour.PriceBucks;
        int currentAmount = currencyType == CurrencyType.coins ? playerData.GetFishCoins() : playerData.GetFishBucks();

        if (currentAmount < price)
        {
            return false;
        }

        if (currencyType == CurrencyType.coins)
        {
            playerData.ClientChangeFishCoinsAmount(-price);
        }
        else
        {
            playerData.ClientChangeFishBucksAmount(-price);
        }

        ItemInstance instance = new ItemInstance(
            item,
            shopBehaviour.Amount
        );
        Guid tempUuid = instance.uuid;
        playerDataManager.ClientAddItem(instance);

        // Track pending purchase for potential rollback
        pendingPurchase = new PendingPurchase
        {
            itemId = item.Id,
            currencyType = currencyType,
            amount = price,
            timestamp = DateTime.UtcNow,
            tempUuid = tempUuid
        };

        return true;
    }

    [Command]
    void CmdBuyItem(int itemID, CurrencyType currencyType)
    {
        // Server-side validation
        ItemDefinition item = ItemRegistry.Get(itemID);
        ShopBehaviour shopBehaviour = item.GetBehaviour<ShopBehaviour>();
        
        if (shopBehaviour == null)
        {
            GameNetworkManager.KickPlayerForCheating(connectionToClient, "This item could be bought");
            return;
        }

        if ((currencyType == CurrencyType.bucks && shopBehaviour.PriceBucks <= 0) ||
            (currencyType == CurrencyType.coins && shopBehaviour.PriceCoins <= 0))
        {
            GameNetworkManager.KickPlayerForCheating(connectionToClient, "Attempted to buy item with unsupported currency");
            return;
        }

        int price = currencyType == CurrencyType.coins ? shopBehaviour.PriceCoins : shopBehaviour.PriceBucks;
        int currentAmount = currencyType == CurrencyType.coins ? playerData.GetFishCoins() : playerData.GetFishBucks();

        if (currentAmount < price)
        {
            GameNetworkManager.KickPlayerForCheating(connectionToClient, "Attempted to buy item with insufficient funds");
            return;
        }

        if (currencyType == CurrencyType.coins)
        {
            playerDataManager.ChangeFishCoinsAmount(-price);
        }
        else
        {
            playerDataManager.ChangeFishBucksAmount(-price);
        }

        ItemInstance instance = new ItemInstance(
            item,
            shopBehaviour.Amount
        );
        playerDataManager.AddItemFromStore(instance);

        TargetPurchaseConfirmed(connectionToClient, instance.uuid);
    }

    [TargetRpc]
    private void TargetPurchaseConfirmed(NetworkConnectionToClient target, Guid realUuid)
    {
        if (pendingPurchase.HasValue)
        {
            PlayerInventory inventory = playerData.GetComponent<PlayerInventory>();
            var optimisticItem = inventory.GetItem(pendingPurchase.Value.tempUuid);
            if (optimisticItem != null)
            {
                optimisticItem.uuid = realUuid;
            }
        }

        pendingPurchase = null;
    }
}
