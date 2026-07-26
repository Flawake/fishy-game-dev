using System;
using UnityEngine;
using ItemSystem;
using Mirror;
using Grants;
using System.Collections.Generic;
using System.Linq;
using FishyGame.Api;

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
		inventory.ServerRemoveAmountFromStack(fish, sellAmount, false);
		if (fish.GetState<StackState>().currentAmount <= 0)
		{
			inventory.RemoveItem(fish.uuid);
		}


		List<FishToSell> fishesList = new List<FishToSell>
		{
    		new FishToSell
    		{
        		fish_uid = fish.uuid.ToString(),
	        	fish_id = fish.def.Id,
				fish_amount = fish.GetState<StackState>().currentAmount,
    	    	new_state_blob = Convert.ToBase64String(StatePacker.Pack(fish.state))
    		}
		};

		DatabaseCommunications.SellFishes(playerData.GetUuid(), fishesList, earnings);
	}

	[Server]
	public void SellAllFish(List<ItemInstance> fishes, int earnings)
	{
		foreach (ItemInstance fish in fishes)
		{
			inventory.RemoveItem(fish.uuid);
		}

		List<FishToSell> fishesList = fishes
            .Select(fish => new FishToSell
            {
                fish_uid = fish.uuid.ToString(),
                fish_id = fish.def.Id,
				fish_amount = fish.GetState<StackState>().currentAmount,
                new_state_blob = null,
            })
            .ToList();

		DatabaseCommunications.SellFishes(playerData.GetUuid(), fishesList, earnings);
	}

	[Server]
	public ItemInstance ServerBuyItem(ItemInstance instace, int price, StoreManager.CurrencyType currencyType)
	{
		ItemInstance toUpdate = inventory.ServerMergeOrAdd(instace, false);
		if (currencyType == StoreManager.CurrencyType.BUCKS)
		{
			playerData.ChangeFishBucksAmount(price, false);
		}
		else if (currencyType == StoreManager.CurrencyType.COINS)
		{
			playerData.ChangeFishCoinsAmount(price, false);
		}
		DatabaseCommunications.BuyItem(playerData.GetUuid(), toUpdate, price, currencyType);
		return toUpdate;
	}

	[Server]
	public ItemInstance ServerAddItem(ItemInstance item, bool needsTargetSync)
	{
		return ServerAddItem(item, null, false, needsTargetSync);
	}

	// Client-side version for optimistic updates
	[Client]
	public ItemInstance ClientAddItem(ItemInstance item)
	{
		return inventory.TryMergeOrAdd(item);
	}

	[Server]
	public ItemInstance ServerAddItem(ItemInstance item, CurrentFish fish, bool fromCaught, bool needsTargetSync)
	{
		if (fish != null && fromCaught)
		{
			fishdexFishes.AddStatFish(fish);
			DatabaseCommunications.AddStatFish(fish, playerData.GetUuid());
		}
		ItemInstance toUpdate = inventory.ServerMergeOrAdd(item, needsTargetSync);
		DatabaseCommunications.AddOrUpdateItem(toUpdate, playerData.GetUuid());
		return toUpdate;
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

		bool success = inventory.ServerRemoveAmountFromStack(itemReference, removeAmount, needsTargetSync);

		if (success)
		{
			if (stackState.currentAmount <= 0)
			{
				inventory.RemoveItem(itemReference.uuid);
				DatabaseCommunications.DestroyItem(itemReference, playerData.GetUuid());
				Debug.Log($"Stack of {itemReference.def.DisplayName} is now empty and has been removed");
			}
			else
			{
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
}
