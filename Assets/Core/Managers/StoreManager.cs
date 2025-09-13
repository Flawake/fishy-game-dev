using Mirror;
using ItemSystem;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Grants;

public class StoreManager : NetworkBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerData playerData;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerDataSyncManager playerDataManager;
    [SerializeField] private ItemGrantService itemGrantService;
    
    [Header("Store Configuration")]
    [SerializeField] private float purchaseTimeoutSeconds = 10f;
    [SerializeField] private int maxConcurrentPurchases = 1;
    [SerializeField] private bool enablePurchaseLogging = true;

    public enum CurrencyType
    {
        coins,
        bucks
    }

    private readonly HashSet<Guid> processedOperationIds = new HashSet<Guid>();
    private readonly Dictionary<Guid, Guid> operationToRealUuid = new Dictionary<Guid, Guid>();
    
    // Events for UI and analytics
    public static event Action<ItemDefinition, CurrencyType, int> OnPurchaseAttempted;
    public static event Action<ItemDefinition, CurrencyType, int> OnPurchaseConfirmed;
    public static event Action<ItemDefinition, CurrencyType, string> OnPurchaseFailed;


    private void Awake()
    {
        ValidateDependencies();
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

        if (TryOptimisticPurchase(item, currencyType, out Guid operationId))
        {
            OnPurchaseAttempted?.Invoke(item, currencyType, GetItemPrice(item, currencyType));
            CmdBuyItem(item.Id, currencyType, operationId);
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

        if (processedOperationIds.Count >= maxConcurrentPurchases)
        {
            LogWarning($"Purchase request failed: Too many pending purchases ({processedOperationIds.Count})");
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
        if (playerLevel < GetRequiredBuyLevel(item))
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

        // Centralized optimistic item grant via service
        Guid operationId = itemGrantService.ClientRegisterOptimistic(item, shopBehaviour.Amount);
        if (operationId == Guid.Empty)
        {
            RollbackCurrencyChange(currencyType, price);
            LogWarning($"Optimistic purchase failed: Could not add item {item.DisplayName} to inventory");
            return false;
        }
        tempUuid = operationId;

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
    
    [Command]
    private void CmdBuyItem(int itemID, CurrencyType currencyType, Guid tempUuid)
    {
        ItemDefinition item = ItemRegistry.Get(itemID);
        ShopBehaviour shopBehaviour = item?.GetBehaviour<ShopBehaviour>();
        int addedAmountForRollback = shopBehaviour != null ? shopBehaviour.Amount : 0;
        int priceForRollback = GetItemPrice(item, currencyType);

        // Idempotency: if we've processed this operation already, re-send confirmation
        if (processedOperationIds.Contains(tempUuid))
        {
            if (operationToRealUuid.TryGetValue(tempUuid, out var realUuid))
            {
                int priceEcho = GetItemPrice(item, currencyType);
                TargetPurchaseConfirmed(connectionToClient, realUuid, tempUuid, itemID, currencyType, priceEcho);
            }
            return;
        }

        if (!ValidateServerPurchase(itemID, currencyType))
        {
            // deny item grant centrally
            itemGrantService.ServerDeny(tempUuid, addedAmountForRollback);
            TargetPurchaseFailed(connectionToClient, tempUuid, itemID, currencyType, addedAmountForRollback, priceForRollback, "Validation failed");
            return;
        }

        int price = GetItemPrice(item, currencyType);

        // Deduct currency on server
        if (currencyType == CurrencyType.coins)
        {
            playerDataManager.ChangeFishCoinsAmount(-price, false);
        }
        else
        {
            playerDataManager.ChangeFishBucksAmount(-price, false);
        }

        // Create and add item on server (authoritative)
        ItemInstance instance = new ItemInstance(item, shopBehaviour.Amount);
        instance = playerDataManager.AddItemFromStore(instance);

        // Persist to DB (best-effort)
        DatabaseCommunications.AddOrUpdateItem(instance, playerData.GetUuid());

        // Record idempotency
        processedOperationIds.Add(tempUuid);
        operationToRealUuid[tempUuid] = instance.uuid;

        // Confirm centrally (updates client item mapping)
        itemGrantService.ServerConfirm(tempUuid, instance.uuid);

        // Notify client with operationId for currency/UI
        TargetPurchaseConfirmed(connectionToClient, instance.uuid, tempUuid, itemID, currencyType, price);
        
        LogServerPurchase(item, currencyType, price, connectionToClient);
    }

    [TargetRpc]
    private void TargetPurchaseConfirmed(NetworkConnectionToClient target, Guid realUuid, Guid tempUuid, int itemId, CurrencyType currencyType, int price)
    {
        var item = ItemRegistry.Get(itemId);
        OnPurchaseConfirmed?.Invoke(item, currencyType, price);
        LogInfo($"Purchase confirmed: {item?.DisplayName} for {price} {currencyType}");
    }

    [TargetRpc]
    private void TargetPurchaseFailed(NetworkConnectionToClient target, Guid tempUuid, int itemId, CurrencyType currencyType, int addedAmount, int price, string reason)
    {
        var item = ItemRegistry.Get(itemId);
        // Rollback optimistic currency only (item rollback handled by ItemGrantService)
        RollbackCurrencyChange(currencyType, price);
        OnPurchaseFailed?.Invoke(item, currencyType, reason);
        LogWarning($"Purchase failed: {item?.DisplayName} - {reason}");
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
        if (playerLevel < GetRequiredBuyLevel(item))
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
        if (itemGrantService == null)
        {
            LogError("StoreManager: ItemGrantService dependency is missing!");
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
