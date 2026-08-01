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
    
    [SerializeField] private int maxConcurrentPurchases = 1;
    [SerializeField] private bool enablePurchaseLogging = true;

    public enum CurrencyType
    {
        COINS,
        BUCKS
    }

    private readonly HashSet<GrantId> processedGrantIds = new HashSet<GrantId>();
    
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

        if (TryOptimisticPurchase(item, currencyType, out GrantId grantId))
        {
            OnPurchaseAttempted?.Invoke(item, currencyType, GetItemPrice(item, currencyType));
            CmdBuyItem(item.Id, currencyType, grantId);
            return true;
        }
        OnPurchaseFailed?.Invoke(item, currencyType, "");
        return false;
    }
    
    public static int GetRequiredBuyLevel(ItemDefinition item) 
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
            return currencyType == CurrencyType.COINS ? shopBehaviour.PriceCoins : shopBehaviour.PriceBucks;
        }
        return 0;
    }

    public bool CanAffordItem(ItemDefinition item, CurrencyType currencyType)
    {
        int price = GetItemPrice(item, currencyType);
        if (price <= 0) return false;

        int currentAmount = currencyType == CurrencyType.COINS 
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

        if (processedGrantIds.Count >= maxConcurrentPurchases)
        {
            LogWarning($"Purchase request failed: Too many pending purchases ({processedGrantIds.Count})");
            return false;
        }

        return true;
    }

    [Client]
    private bool TryOptimisticPurchase(ItemDefinition item, CurrencyType currencyType, out GrantId grantId)
    {
        grantId = GrantId.None;
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

        int currentPlayerMoneyAmount = currencyType == CurrencyType.COINS 
            ? playerData.GetFishCoins() 
            : playerData.GetFishBucks();

        if (currentPlayerMoneyAmount < price)
        {
            LogInfo($"Optimistic purchase failed: Insufficient funds. Required: {price}, Available: {currentPlayerMoneyAmount}");
            return false;
        }

        // Apply optimistic currency deduction
        if (currencyType == CurrencyType.COINS)
        {
            playerData.ClientChangeFishCoinsAmount(-price);
        }
        else
        {
            playerData.ClientChangeFishBucksAmount(-price);
        }

        // Centralized optimistic item grant via service
        GrantId grant = itemGrantService.ClientRegisterOptimistic(item, shopBehaviour.Amount);
        if (!grant.IsValid)
        {
            RollbackCurrencyChange(currencyType, price);
            LogWarning($"Optimistic purchase failed: Could not add item {item.DisplayName} to inventory");
            return false;
        }
        grantId = grant;

        LogInfo($"Optimistic purchase successful: {item.DisplayName} for {price} {currencyType}");
        return true;
    }

    [Client]
    private void RollbackCurrencyChange(CurrencyType currencyType, int amount)
    {
        if (currencyType == CurrencyType.COINS)
        {
            playerData.ClientChangeFishCoinsAmount(amount);
        }
        else
        {
            playerData.ClientChangeFishBucksAmount(amount);
        }
    }
    
    [Command]
    private void CmdBuyItem(int itemID, CurrencyType currencyType, GrantId grantId)
    {
        ItemDefinition item = ItemRegistry.Get(itemID);
        ShopBehaviour shopBehaviour = item?.GetBehaviour<ShopBehaviour>();
        int addedAmountForRollback = shopBehaviour != null ? shopBehaviour.Amount : 0;
        int priceForRollback = GetItemPrice(item, currencyType);

        // Idempotency: if we've processed this grant already, re-send confirmation
        if (processedGrantIds.Contains(grantId))
        {
            TargetPurchaseConfirmed(connectionToClient, grantId, itemID, currencyType, GetItemPrice(item, currencyType));
            return;
        }

        if (!ValidateServerPurchase(itemID, currencyType))
        {
            // deny item grant centrally
            itemGrantService.ServerDeny(grantId);
            TargetPurchaseFailed(connectionToClient, grantId, itemID, currencyType, addedAmountForRollback, priceForRollback, "Validation failed");
            return;
        }

        int price = GetItemPrice(item, currencyType);

        // Create and add item on server (authoritative)
        DeltaItem instance = new DeltaItem(item, shopBehaviour.Amount);
        if (!playerDataManager.ServerBuyItem(instance, price, currencyType, out InventoryChange change))
        {
            itemGrantService.ServerDeny(grantId);
            TargetPurchaseFailed(connectionToClient, grantId, itemID, currencyType, addedAmountForRollback, priceForRollback, "Item could not be added");
            return;
        }

        // Record idempotency
        processedGrantIds.Add(grantId);

        // Confirm centrally: this is what tells the client what the stacks it guessed at really hold
        itemGrantService.ServerConfirm(grantId, change);

        // Notify client with the grant id for currency/UI
        TargetPurchaseConfirmed(connectionToClient, grantId, itemID, currencyType, price);

        LogServerPurchase(item, currencyType, price, connectionToClient);
    }

    [TargetRpc]
    private void TargetPurchaseConfirmed(NetworkConnectionToClient target, GrantId grantId, int itemId, CurrencyType currencyType, int price)
    {
        ItemDefinition item = ItemRegistry.Get(itemId);
        OnPurchaseConfirmed?.Invoke(item, currencyType, price);
        Notification notification = new Notification {
            message = $"Bought {item?.DisplayName} for {price} {currencyType}"
        };
        MessageUIHandler.AddNotification(notification);
    }

    [TargetRpc]
    private void TargetPurchaseFailed(NetworkConnectionToClient target, GrantId grantId, int itemId, CurrencyType currencyType, int addedAmount, int price, string reason)
    {
        var item = ItemRegistry.Get(itemId);
        // Rollback optimistic currency only (item rollback handled by ItemGrantService)
        RollbackCurrencyChange(currencyType, price);
        OnPurchaseFailed?.Invoke(item, currencyType, reason);
        LogWarning($"Purchase failed: {item?.DisplayName} - {reason}");
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

        int currentAmount = currencyType == CurrencyType.COINS 
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
