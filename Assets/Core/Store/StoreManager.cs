using Mirror;
using ItemSystem;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class StoreManager : NetworkBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerData playerData;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerDataSyncManager playerDataManager;
    
    [Header("Store Configuration")]
    [SerializeField] private float purchaseTimeoutSeconds = 10f;
    [SerializeField] private int maxConcurrentPurchases = 1;
    [SerializeField] private bool enablePurchaseLogging = true;

    public enum CurrencyType
    {
        coins,
        bucks
    }

    [Serializable]
    private struct PendingPurchase
    {
        public int itemId;
        public CurrencyType currencyType;
        public int amount;
        public DateTime timestamp;
        public Guid tempUuid;
    }

    private readonly Dictionary<Guid, PendingPurchase> pendingPurchases = new Dictionary<Guid, PendingPurchase>();
    
    // Events for UI and analytics
    public static event Action<ItemDefinition, CurrencyType, int> OnPurchaseAttempted;
    public static event Action<ItemDefinition, CurrencyType, int> OnPurchaseConfirmed;
    public static event Action<ItemDefinition, CurrencyType, string> OnPurchaseFailed;


    private void Awake()
    {
        ValidateDependencies();
    }

    private void Update()
    {
        if (isServer)
        {
            return;
        }
        CleanupExpiredPurchases();
    }

    private void OnDestroy()
    {
        // Clear static events to prevent memory leaks
        OnPurchaseAttempted = null;
        OnPurchaseConfirmed = null;
        OnPurchaseFailed = null;
    }

    #region Public API

    [Client]
    public bool BuyItem(ItemDefinition item, CurrencyType currencyType)
    {
        if (!ValidatePurchaseRequest(item, currencyType))
        {
            return false;
        }

        if (TryOptimisticPurchase(item, currencyType, out Guid tempUuid))
        {
            OnPurchaseAttempted?.Invoke(item, currencyType, GetItemPrice(item, currencyType));
            CmdBuyItem(item.Id, currencyType, tempUuid);
            return true;
        }
        OnPurchaseFailed?.Invoke(item, currencyType, "");
        return false;
    }
    
    public int GetRequiredBuyLevel(ItemDefinition item) 
    {
        if (item?.GetBehaviour<ShopBehaviour>() is ShopBehaviour shopBehaviour)
        {
            return shopBehaviour.UnlockLevel;
        }
        return int.MaxValue;
    }

    public int GetItemPrice(ItemDefinition item, CurrencyType currencyType)
    {
        if (item?.GetBehaviour<ShopBehaviour>() is ShopBehaviour shopBehaviour)
        {
            return currencyType == CurrencyType.coins ? shopBehaviour.PriceCoins : shopBehaviour.PriceBucks;
        }
        return 0;
    }

    public bool CanAffordItem(ItemDefinition item, CurrencyType currencyType)
    {
        int price = GetItemPrice(item, currencyType);
        if (price <= 0) return false;

        int currentAmount = currencyType == CurrencyType.coins 
            ? playerData.GetFishCoins() 
            : playerData.GetFishBucks();

        return currentAmount >= price;
    }



    #endregion


    [Client]
    private bool ValidatePurchaseRequest(ItemDefinition item, CurrencyType currencyType)
    {
        if (item == null)
        {
            LogWarning("Purchase request failed: Item is null");
            return false;
        }

        if (playerData == null)
        {
            LogError("Purchase request failed: PlayerData is null");
            return false;
        }

        if (pendingPurchases.Count >= maxConcurrentPurchases)
        {
            LogWarning($"Purchase request failed: Too many pending purchases ({pendingPurchases.Count})");
            return false;
        }

        return true;
    }

    [Client]
    private bool TryOptimisticPurchase(ItemDefinition item, CurrencyType currencyType, out Guid tempUuid)
    {
        tempUuid = Guid.Empty;
        ShopBehaviour shopBehaviour = item.GetBehaviour<ShopBehaviour>();
        if (shopBehaviour == null)
        {
            LogWarning($"Optimistic purchase failed: Item {item.DisplayName} has no ShopBehaviour");
            return false;
        }

        int playerLevel = LevelMath.XpToLevel(playerData.GetXp()).level;
        if (GetRequiredBuyLevel(item) < playerLevel)
        {
            LogWarning($"Optimistic purchase failed: Playerlevel too low");
            return false;
        }

        int price = GetItemPrice(item, currencyType);
        if (price <= 0)
        {
            LogWarning($"Optimistic purchase failed: Item {item.DisplayName} has invalid price for {currencyType}");
            return false;
        }

        int currentPlayerMoneyAmount = currencyType == CurrencyType.coins 
            ? playerData.GetFishCoins() 
            : playerData.GetFishBucks();

        if (currentPlayerMoneyAmount < price)
        {
            LogInfo($"Optimistic purchase failed: Insufficient funds. Required: {price}, Available: {currentPlayerMoneyAmount}");
            return false;
        }

        // Apply optimistic currency deduction
        if (currencyType == CurrencyType.coins)
        {
            playerData.ClientChangeFishCoinsAmount(-price);
        }
        else
        {
            playerData.ClientChangeFishBucksAmount(-price);
        }

        // Create optimistic item instance
        ItemInstance instance = new ItemInstance(item, shopBehaviour.Amount);

        // Add to inventory (may merge) and obtain resulting reference via sync manager
        ItemInstance storedItem = playerDataManager.ClientAddItem(instance);

        if (storedItem == null)
        {
            // Rollback currency if item addition fails
            RollbackCurrencyChange(currencyType, price);
            LogWarning($"Optimistic purchase failed: Could not add item {item.DisplayName} to inventory");
            return false;
        }

        tempUuid = storedItem.uuid; // use the resulting item's uuid (merged or new)

        var pendingPurchase = new PendingPurchase
        {
            itemId = item.Id,
            currencyType = currencyType,
            amount = price,
            timestamp = DateTime.UtcNow,
            tempUuid = tempUuid
        };

        // TODO: this can cause collisions if buying the same item twice which will get merged in the same uuid while the previous item was still in this dict
        pendingPurchases[tempUuid] = pendingPurchase;

        LogInfo($"Optimistic purchase successful: {item.DisplayName} for {price} {currencyType}");
        return true;
    }

    [Client]
    private void RollbackCurrencyChange(CurrencyType currencyType, int amount)
    {
        if (currencyType == CurrencyType.coins)
        {
            playerData.ClientChangeFishCoinsAmount(amount);
        }
        else
        {
            playerData.ClientChangeFishBucksAmount(amount);
        }
    }
    
    [Client]
    private void CleanupExpiredPurchases()
    {
        var expiredPurchases = pendingPurchases
            .Where(kvp => (DateTime.UtcNow - kvp.Value.timestamp).TotalSeconds > purchaseTimeoutSeconds)
            .ToList();

        foreach (var kvp in expiredPurchases)
        {
            var item = ItemRegistry.Get(kvp.Value.itemId);
            string itemName = item?.DisplayName ?? "Unknown Item";
            CmdReportTimeout(kvp.Value.tempUuid, kvp.Value.itemId);
            pendingPurchases.Remove(kvp.Key);
        }
    }

    [Command]
    private void CmdBuyItem(int itemID, CurrencyType currencyType, Guid tempUuid)
    {
        if (!ValidateServerPurchase(itemID, currencyType))
        {
            return;
        }

        ItemDefinition item = ItemRegistry.Get(itemID);
        ShopBehaviour shopBehaviour = item.GetBehaviour<ShopBehaviour>();
        int price = GetItemPrice(item, currencyType);

        // Deduct currency
        if (currencyType == CurrencyType.coins)
        {
            playerDataManager.ChangeFishCoinsAmount(-price, false);
        }
        else
        {
            playerDataManager.ChangeFishBucksAmount(-price, false);
        }

        // Create and add item
        ItemInstance instance = new ItemInstance(item, shopBehaviour.Amount);
        instance = playerDataManager.AddItemFromStore(instance);

        // Notify client with tempUuid so client can update mapping
        TargetPurchaseConfirmed(connectionToClient, instance.uuid, tempUuid, itemID, currencyType, price);
        
        LogServerPurchase(item, currencyType, price, connectionToClient);
    }

    [Command]
    private void CmdReportTimeout(Guid tempUuid, int itemId)
    {
        throw new NotImplementedException();
        var item = ItemRegistry.Get(itemId);
        string itemName = item?.DisplayName ?? "Unknown Item";
    }

    [Server]
    private bool ValidateServerPurchase(int itemID, CurrencyType currencyType)
    {
        var item = ItemRegistry.Get(itemID);
        if (item == null)
        {
            GameNetworkManager.KickPlayerForCheating(connectionToClient, "Attempted to buy non-existent item");
            return false;
        }

        int playerLevel = LevelMath.XpToLevel(playerData.GetXp()).level;
        if (GetRequiredBuyLevel(item) < playerLevel)
        {
            GameNetworkManager.KickPlayerForCheating(connectionToClient, "Attempted to buy an item with a lower than required level");
            return false;
        }

        var shopBehaviour = item.GetBehaviour<ShopBehaviour>();
        if (shopBehaviour == null)
        {
            GameNetworkManager.KickPlayerForCheating(connectionToClient, "Attempted to buy item without shop behavior");
            return false;
        }

        int price = GetItemPrice(item, currencyType);
        if (price <= 0)
        {
            GameNetworkManager.KickPlayerForCheating(connectionToClient, "Attempted to buy item with invalid price");
            return false;
        }

        int currentAmount = currencyType == CurrencyType.coins 
            ? playerData.GetFishCoins() 
            : playerData.GetFishBucks();

        if (currentAmount < price)
        {
            GameNetworkManager.KickPlayerForCheating(connectionToClient, $"Attempted to buy item with insufficient funds. Required: {price}, Available: {currentAmount}");
            return false;
        }

        return true;
    }

    [TargetRpc]
    private void TargetPurchaseConfirmed(NetworkConnectionToClient target, Guid realUuid, Guid tempUuid, int itemId, CurrencyType currencyType, int price)
    {
        var item = ItemRegistry.Get(itemId);
        
        // Lookup by tempUuid to ensure mapping
        if (pendingPurchases.TryGetValue(tempUuid, out var pendingPurchase))
        {
            ItemInstance optimisticItem = playerInventory.GetItem(tempUuid);
            if (optimisticItem != null)
            {
                optimisticItem.uuid = realUuid;
            }

            pendingPurchases.Remove(tempUuid);
        }

        OnPurchaseConfirmed?.Invoke(item, currencyType, price);
        LogInfo($"Purchase confirmed: {item?.DisplayName} for {price} {currencyType}");
    }

    private void ValidateDependencies()
    {
        if (playerData == null)
        {
            LogError("StoreManager: PlayerData dependency is missing!");
        }

        if(playerInventory == null)
        {
            LogError("StoreManager: PlayerInventory dependency is missing!");
        }

        if (playerDataManager == null)
        {
            LogError("StoreManager: PlayerDataSyncManager dependency is missing!");
        }
    }

    #region Logging

    [Server]
    private void LogServerPurchase(ItemDefinition item, CurrencyType currencyType, int price, NetworkConnectionToClient conn)
    {
        if (enablePurchaseLogging)
        {
            LogInfo($"Server purchase: Player {conn.connectionId} bought {item.DisplayName} for {price} {currencyType}");
        }
    }

    private void LogInfo(string message)
    {
        if (enablePurchaseLogging)
        {
            Debug.Log($"[StoreManager] {message}");
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[StoreManager] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[StoreManager] {message}");
    }

    #endregion
}
