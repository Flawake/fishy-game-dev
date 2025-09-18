using System;
using UnityEngine;
using ItemSystem;
using Mirror;
using Grants;

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
	public void ChangeFishCoinsAmount(int amount, bool needsTargetSync)
	{
		DatabaseCommunications.ChangeFishCoinsAmount(amount, playerData.GetUuid());
		playerData.ChangeFishCoinsAmount(amount, needsTargetSync);
	}

	[Server]
	public void ChangeFishBucksAmount(int amount, bool needsTargetSync)
	{
		DatabaseCommunications.ChangeFishBucksAmount(amount, playerData.GetUuid());
		playerData.ChangeFishBucksAmount(amount, needsTargetSync);
	}

	[Server]
	public void AddXP(int amount)
	{
		DatabaseCommunications.AddXP(amount, playerData.GetUuid());
		playerData.AddXp(amount);
	}

	[Server]
	public void ServerAddItem(ItemInstance item, bool needsTargetSync)
	{
		ServerAddItem(item, null, false, needsTargetSync);
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
		return grantService.ServerAddItem(item, needsTargetSync);
		//grantService.ServerAddAndSync(item);
	}

	[Server]
	public void DestroyItem(ItemInstance item)
	{
		grantService.ServerRemove(item);
	}

	/// <summary>
	/// Attempts to use an item (reduce durability by 1) and syncs changes to database
	/// </summary>
	/// <param name="itemReference">The item to use</param>
	/// <returns>True if the item was successfully used, false otherwise</returns>
	[Server]
	public bool ServerTryUseItem(ItemInstance itemReference)
	{
		return grantService.ServerUseItem(itemReference);
	}

	/// <summary>
	/// Attempts to consume one item from a stack and syncs changes to database
	/// </summary>
	/// <param name="itemReference">The item stack to consume from</param>
	/// <returns>True if an item was successfully consumed, false otherwise</returns>
	[Server]
	public bool ServerConsumeFromStack(ItemInstance itemReference)
	{
		return grantService.ServerConsume(itemReference);
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
