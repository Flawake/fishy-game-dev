using System;
using UnityEngine;
using ItemSystem;
using Mirror;
using Grants;
using System.Collections.Generic;
using System.Linq;

//Item manager should manage the syncronisation of items between the server and client.
public class PlayerDataSyncManager : MonoBehaviour
{
	[SerializeField]
	PlayerData playerData;
	[SerializeField]
	PlayerInventory inventory;
	[SerializeField]
	PlayerFishdexFishes fishdexFishes;
	[SerializeField]
	ItemGrantService grantService;

	[Server]
	public void SellFish(ItemInstance fish, int sellAmount, int earnings)
	{
		if(!inventory.ServerRemoveAmountFromSpecifiedStack(fish, sellAmount, false, out InventoryChange change))
		{
			Debug.LogWarning("Failed to remove amount from stack");
			return;
		}
		if (change == null)
		{
			// Static and infinite use definitions come back with nothing taken, so there is nothing
			// to sell either. Bailing out here keeps the payout tied to an actual removal.
			Debug.LogWarning($"Nothing was taken from {fish.def.Id}, so it cannot be sold");
			return;
		}
		if (fish.GetState<StackState>().currentAmount <= 0)
		{
			inventory.RemoveItem(fish.uuid, true);
		}


		List<DeltaItem> fishesList = change.All;

		DatabaseCommunications.SellFishes(playerData.GetUuid(), fishesList, earnings);
	}

	[Server]
	public void SellAllFish(List<ItemInstance> fishes, int earnings)
	{
		foreach (ItemInstance fish in fishes)
		{
			inventory.RemoveItem(fish.uuid, true);
		}

		List<DeltaItem> fishesList = fishes
    	.Select(fish =>
    	{
    	    var states = new List<(Type, IRuntimeBehaviourState)>();

    	    if (fish.GetState<StackState>() is { } stack)
    	    {
    	        states.Add((
    	            typeof(StackState),
    	            new StackState
    	            {
    	                currentAmount = -stack.currentAmount
    	            }
    	        ));
    	    }

    	    return new DeltaItem(fish.def, fish.uuid, states);
    	})
    	.ToList();

		DatabaseCommunications.SellFishes(playerData.GetUuid(), fishesList, earnings);
	}

	[Server]
	public bool ServerBuyItem(DeltaItem instace, int price, StoreManager.CurrencyType currencyType, out InventoryChange change)
	{
		if(!inventory.ServerMergeOrAdd(instace, false, out change))
		{
			return false;
		}
		if (currencyType == StoreManager.CurrencyType.BUCKS)
		{
			playerData.ChangeFishBucksAmount(-price, false);
		}
		else if (currencyType == StoreManager.CurrencyType.COINS)
		{
			playerData.ChangeFishCoinsAmount(-price, false);
		}
		DatabaseCommunications.BuyItem(playerData.GetUuid(), change.All, price, currencyType);
		return true;
	}

	[Server]
	public bool ServerAddItem(DeltaItem item, bool needsTargetSync, out InventoryChange change)
	{
		return ServerAddItem(item, null, false, needsTargetSync, out change);
	}

	/// <summary>
	/// Adds currency to the player and pushes the new balance to the owning client.
	/// Persistence is the responsibility of whatever domain endpoint caused the
	/// grant; there is no standalone balance endpoint.
	/// </summary>
	[Server]
	public void ServerAddCurrency(StoreManager.CurrencyType currencyType, int amount)
	{
		switch (currencyType)
		{
			case StoreManager.CurrencyType.COINS:
				playerData.ChangeFishCoinsAmount(amount, true);
				break;
			case StoreManager.CurrencyType.BUCKS:
				playerData.ChangeFishBucksAmount(amount, true);
				break;
			default:
				Debug.LogError($"Unhandled currency type {currencyType}.");
				break;
		}
	}

	[Server]
	public bool ServerAddItem(DeltaItem item, CurrentFish fish, bool fromCaught, bool needsTargetSync, out InventoryChange change)
	{
		if (fish != null && fromCaught)
		{
			fishdexFishes.AddStatFish(fish);
			DatabaseCommunications.AddStatFish(fish, playerData.GetUuid());
		}
		if(!inventory.ServerMergeOrAdd(item, needsTargetSync, out change))
		{
			return false;
		}
		DatabaseCommunications.AddOrUpdateItem(change.All, playerData.GetUuid());
		return true;
	}

	[Server]
	public void DestroyItem(ItemInstance item)
	{
		inventory.RemoveItem(item.uuid, true);
		DatabaseCommunications.DestroyItem(item.uuid, playerData.GetUuid());
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
		bool success = inventory.ServerTryUseItem(itemReference, out DeltaItem deltaItem);
		if (success)
		{
			// Might be null when the item has infiniteUse set
			if (deltaItem != null)
			{
				DatabaseCommunications.AddOrUpdateItem(new() {deltaItem}, playerData.GetUuid());
				DurabilityState durabilityState = itemReference.GetState<DurabilityState>();
				if (durabilityState != null && durabilityState.remaining <= 0)
				{
					inventory.RemoveItem(itemReference.uuid, true);
					DatabaseCommunications.DestroyItem(itemReference.uuid, playerData.GetUuid());
				}
			}
		}
		return success;
	}

	/// <summary>
	/// Attempts to consume n items from a stack and syncs changes to database
	/// </summary>
	/// <param name="itemReference">The item stack to consume from</param>
	/// <param name="amount">The amount of items to consume from the stack</param>
	/// <returns>True if the items were successfully consumed or the item was marked for infinite use, false otherwise</returns>
	[Server]
	public bool ServerRemoveAmountFromStack(ItemInstance itemReference, int removeAmount, bool needsTargetSync)
	{
		if (itemReference == null)
		{
			Debug.LogWarning("Cannot consume from null item reference");
			return false;
		}
		StackState stackState = itemReference.GetState<StackState>();
		if (stackState == null)
		{
			Debug.LogWarning("Stackstate was null, could not remove from stack");
			return false;
		}

		if (stackState.currentAmount < removeAmount)
		{
			Debug.LogWarning("Not enough items in the stack to remove from");
			return false;
		}

		bool success = inventory.ServerRemoveAmountFromSpecifiedStack(itemReference, removeAmount, needsTargetSync, out InventoryChange change);

		if (success)
		{
			if (stackState.currentAmount <= 0)
			{
				inventory.RemoveItem(itemReference.uuid, true);
				DatabaseCommunications.DestroyItem(itemReference.uuid, playerData.GetUuid());
				Debug.Log($"Stack of {itemReference.def.DisplayName} is now empty and has been removed");
			}
			else
			{
				DatabaseCommunications.AddOrUpdateItem(change.All, playerData.GetUuid());
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
}
