using System;
using UnityEngine;
using ItemSystem;
using Mirror;

//Item manager should manage the syncronisation of items between the server and client.
public class PlayerDataSyncManager : MonoBehaviour
{
    [SerializeField]
    PlayerData playerData;
    [SerializeField]
    PlayerInventory inventory;
    [SerializeField]
    PlayerFishdexFishes fishdexFishes;

    [Server]
    public void ChangeFishCoinsAmount(int amount)
    {
        DatabaseCommunications.ChangeFishCoinsAmount(amount, playerData.GetUuid());
        playerData.ChangeFishCoinsAmount(amount);
    }

    [Server]
    public void ChangeFishBucksAmount(int amount)
    {
        DatabaseCommunications.ChangeFishBucksAmount(amount, playerData.GetUuid());
        playerData.ChangeFishBucksAmount(amount);
    }

    [Server]
    public void AddXP(int amount)
    {
        DatabaseCommunications.AddXP(amount, playerData.GetUuid());
        playerData.AddXp(amount);
    }

    [Server]
    public void AddItem(ItemInstance item)
    {
        AddItem(item, null, false);
    }

    [Server]
    public void AddItem(ItemInstance item, CurrentFish fish, bool fromCaught)
    {
        if (fish != null && fromCaught)
        {
            fishdexFishes.AddStatFish(fish);
            DatabaseCommunications.AddStatFish(fish, playerData.GetUuid());
        }

        ItemInstance toUpdate = inventory.AddItem(item);
        DatabaseCommunications.AddOrUpdateItem(toUpdate, playerData.GetUuid());
    }

    [Server]
    public void DestroyItem(ItemInstance item)
    {
        inventory.RemoveItem(item.uuid);
        DatabaseCommunications.DestroyItem(item, playerData.GetUuid());
    }

    /// <summary>
    /// Attempts to use an item (reduce durability by 1) and syncs changes to database
    /// </summary>
    /// <param name="itemReference">The item to use</param>
    /// <returns>True if the item was successfully used, false otherwise</returns>
    [Server]
    public bool ServerTryUseItem(ItemInstance itemReference)
    {
        if (itemReference == null)
        {
            Debug.LogWarning("Cannot use null item reference");
            return false;
        }

        bool success = inventory.ServerTryUseItem(itemReference);
        if (success)
        {
            DatabaseCommunications.AddOrUpdateItem(itemReference, playerData.GetUuid());
            
            // Check if the item broke (durability reached 0)
            DurabilityState durabilityState = itemReference.GetState<DurabilityState>();
            if (durabilityState != null && durabilityState.remaining <= 0)
            {
                inventory.RemoveItem(itemReference.uuid);
                DatabaseCommunications.DestroyItem(itemReference, playerData.GetUuid());
            }
        }
        
        return success;
    }

    /// <summary>
    /// Attempts to consume one item from a stack and syncs changes to database
    /// </summary>
    /// <param name="itemReference">The item stack to consume from</param>
    /// <returns>True if an item was successfully consumed, false otherwise</returns>
    [Server]
    public bool ServerConsumeFromStack(ItemInstance itemReference)
    {
        if (itemReference == null)
        {
            Debug.LogWarning("Cannot consume from null item reference");
            return false;
        }

        // Try to consume from the stack in the inventory
        bool success = inventory.ServerConsumeFromStack(itemReference);
        
        if (success)
        {
            // Check if the stack is now empty
            StackState stackState = itemReference.GetState<StackState>();
            if (stackState != null && stackState.currentAmount <= 0)
            {
                // Remove the empty stack from inventory and database
                DestroyItem(itemReference);
                Debug.Log($"Stack of {itemReference.def.DisplayName} is now empty and has been removed");
            }
            else
            {
                // Update the database with the new stack amount
                DatabaseCommunications.AddOrUpdateItem(itemReference, playerData.GetUuid());
            }
        }
        
        return success;
    }

    /// <summary>
    /// Checks if an item can be used (has durability or is infinite/static)
    /// </summary>
    /// <param name="itemReference">The item to check</param>
    /// <returns>True if the item can be used, false otherwise</returns>
    [Server]
    public bool CanUseItem(ItemInstance itemReference)
    {
        if (itemReference == null)
        {
            return false;
        }

        if (itemReference.def.InfiniteUse || itemReference.def.IsStatic)
        {
            return true;
        }

        DurabilityState durabilityState = itemReference.GetState<DurabilityState>();
        return durabilityState != null && durabilityState.remaining > 0;
    }

    /// <summary>
    /// Checks if an item stack can be consumed from (has items remaining or is infinite/static)
    /// </summary>
    /// <param name="itemReference">The item stack to check</param>
    /// <returns>True if the stack can be consumed from, false otherwise</returns>
    [Server]
    public bool CanConsumeFromStack(ItemInstance itemReference)
    {
        if (itemReference == null)
        {
            return false;
        }

        if (itemReference.def.InfiniteUse || itemReference.def.IsStatic)
        {
            return true;
        }

        StackState stackState = itemReference.GetState<StackState>();
        return stackState != null && stackState.currentAmount > 0;
    }

    /// <summary>
    /// Gets the remaining durability of an item
    /// </summary>
    /// <param name="itemReference">The item to check</param>
    /// <returns>The remaining durability, or -1 if the item has no durability</returns>
    [Server]
    public int GetItemDurability(ItemInstance itemReference)
    {
        if (itemReference == null)
        {
            return -1;
        }

        DurabilityState durabilityState = itemReference.GetState<DurabilityState>();
        return durabilityState?.remaining ?? -1;
    }

    /// <summary>
    /// Gets the remaining amount in an item stack
    /// </summary>
    /// <param name="itemReference">The item stack to check</param>
    /// <returns>The remaining amount, or -1 if the item is not stackable</returns>
    [Server]
    public int GetStackAmount(ItemInstance itemReference)
    {
        if (itemReference == null)
        {
            return -1;
        }

        StackState stackState = itemReference.GetState<StackState>();
        return stackState?.currentAmount ?? -1;
    }

    internal void ChangeFishCoinsAmount(int? v)
    {
        throw new NotImplementedException();
    }

    internal void ChangeFishBucksAmount(int? v)
    {
        throw new NotImplementedException();
    }
}
