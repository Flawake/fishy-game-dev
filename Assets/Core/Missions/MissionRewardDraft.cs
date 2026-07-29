using System;
using System.Collections.Generic;
using ItemSystem;
using Mirror;
using UnityEngine;

/// <summary>
/// A mission reward described as data instead of applied as side effects.
///
/// Completion and payout have to land in one database transaction, which means
/// the reward must be known before anything is written and only committed to
/// memory once the transaction is acknowledged. Rewards therefore fill in a
/// draft, the draft is turned into the request payload, and <see cref="Apply"/>
/// runs afterwards.
/// </summary>
public class MissionRewardDraft
{
    readonly PlayerInventory inventory;
    readonly PlayerDataSyncManager syncManager;

    int coins;
    int bucks;

    ItemInstance grantedItem;
    int itemDefinitionId;
    Guid itemUuid;

    public int Coins => coins;
    public int Bucks => bucks;

    public int ItemDefinitionId => itemDefinitionId;

    /// <summary>
    /// <see cref="Guid.Empty"/> when the mission rewards no item. The definition id
    /// cannot carry that signal, because 0 is a real item id.
    /// </summary>
    public Guid ItemUuid => itemUuid;

    public MissionRewardDraft(PlayerInventory inventory, PlayerDataSyncManager syncManager)
    {
        this.inventory = inventory;
        this.syncManager = syncManager;
    }

    public void AddCurrency(StoreManager.CurrencyType currencyType, int amount)
    {
        switch (currencyType)
        {
            case StoreManager.CurrencyType.COINS:
                coins += amount;
                break;
            case StoreManager.CurrencyType.BUCKS:
                bucks += amount;
                break;
            default:
                Debug.LogError($"Unhandled currency type {currencyType} in a mission reward.");
                break;
        }
    }

    /// <summary>
    /// Resolves the single inventory row this reward will write. The row is either
    /// an existing stack grown by <paramref name="amount"/> or a brand new stack,
    /// mirroring the decision <see cref="PlayerInventory.TryMergeOrAdd"/> makes, so
    /// that what is stored matches what the inventory does with it later.
    /// </summary>
    public void AddItem(ItemDefinition definition, int amount)
    {
        if (definition == null)
        {
            Debug.LogError("Mission reward has no item definition assigned.");
            return;
        }

        if (grantedItem != null)
        {
            Debug.LogError($"A mission reward can only grant one item; ignoring {definition.DisplayName}.");
            return;
        }

        if (amount <= 0)
        {
            Debug.LogError($"Mission reward grants {amount}x {definition.DisplayName}; ignoring it.");
            return;
        }

        int maxStack = Mathf.Max(1, definition.MaxStack);
        if (amount > maxStack)
        {
            Debug.LogError(
                $"Mission reward grants {amount}x {definition.DisplayName} but its stack holds {maxStack}. Clamping.");
            amount = maxStack;
        }

        grantedItem = new ItemInstance(definition, amount);
        itemDefinitionId = definition.Id;

        if (TryResolveMergeTarget(definition, amount, maxStack, out ItemInstance target, out int mergedAmount))
        {
            itemUuid = target.uuid;
            itemStateBlob = PackWithStackAmount(target, mergedAmount);
            return;
        }

        itemUuid = grantedItem.uuid;
        itemStateBlob = Convert.ToBase64String(StatePacker.Pack(grantedItem.state));
    }

    /// <summary>
    /// Hands the reward to the player. Deliberately writes nothing to the database:
    /// the transaction that authorised this call already did.
    /// </summary>
    [Server]
    public void Apply()
    {
        if (coins != 0)
        {
            syncManager.ServerAddCurrency(StoreManager.CurrencyType.COINS, coins);
        }

        if (bucks != 0)
        {
            syncManager.ServerAddCurrency(StoreManager.CurrencyType.BUCKS, bucks);
        }

        if (grantedItem != null)
        {
            inventory.ServerMergeOrAdd(grantedItem, true);
        }
    }

    bool TryResolveMergeTarget(ItemDefinition definition, int amount, int maxStack, out ItemInstance target, out int mergedAmount)
    {
        target = null;
        mergedAmount = 0;

        // Items that wear out are never stacked.
        if (definition.GetBehaviour<DurabilityBehaviour>() != null)
        {
            return false;
        }

        ItemInstance candidate = inventory.GetFirstNonFullStack(definition.Id);
        if (candidate == null)
        {
            return false;
        }

        StackState stack = candidate.GetState<StackState>();
        if (stack == null || stack.currentAmount + amount > maxStack)
        {
            return false;
        }

        target = candidate;
        mergedAmount = stack.currentAmount + amount;
        return true;
    }

    /// <summary>
    /// Packs a copy of the item's state with a different stack amount, so the blob
    /// can be built without mutating the live instance.
    /// </summary>
    static string PackWithStackAmount(ItemInstance item, int stackAmount)
    {
        Dictionary<Type, IRuntimeBehaviourState> copy = new Dictionary<Type, IRuntimeBehaviourState>(item.state);
        copy[typeof(StackState)] = new StackState { currentAmount = stackAmount };
        return Convert.ToBase64String(StatePacker.Pack(copy));
    }
}
