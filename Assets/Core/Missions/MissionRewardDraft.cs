using System.Linq;
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

    DeltaItem grantedItemDelta;

    public int Coins => coins;
    public int Bucks => bucks;

    public DeltaItem GrantedItemDelta => grantedItemDelta;

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
    /// Decides the single inventory row this reward writes: an existing stack that
    /// can take the whole amount, or a stack of its own. <see cref="Apply"/> carries
    /// that decision out instead of deciding again, so what ends up in memory cannot
    /// disagree with what was stored.
    /// </summary>
    public void AddItem(ItemDefinition definition, int amount)
    {
        if (definition == null)
        {
            Debug.LogError("Mission reward has no item definition assigned.");
            return;
        }

        if (grantedItemDelta != null)
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

        ItemInstance target = ResolveMergeTarget(definition, amount, maxStack);
        if (target == null)
        {
            grantedItemDelta = new DeltaItem(definition, amount);
            return;
        }

        grantedItemDelta = DeltaItem.FromItemInstance(target);
        grantedItemDelta.SetState(new StackState { currentAmount = amount });
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

        if (grantedItemDelta != null && !inventory.ServerApplyDelta(grantedItemDelta))
        {
            Debug.LogError($"Mission reward row {grantedItemDelta.ItemUUID} was stored but could not be applied.");
        }
    }

    /// <summary>
    /// The stack the reward can be folded into whole, if there is one. Anything that
    /// would split across two stacks is refused, because the payload holds one row.
    /// </summary>
    ItemInstance ResolveMergeTarget(ItemDefinition definition, int amount, int maxStack)
    {
        if (definition.GetBehaviour<DurabilityBehaviour>() != null)
        {
            return null;
        }

        return inventory.GetNonFullStacks(definition.Id)
            .FirstOrDefault(stack =>
                stack.GetState<StackState>() is { } state &&
                state.currentAmount + amount <= maxStack);
    }
}
