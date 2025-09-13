using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using ItemSystem;

namespace Grants
{
	public class ItemGrantService : NetworkBehaviour
	{
		[SerializeField] private PlayerInventory playerInventory;
		[SerializeField] private PlayerData playerData;

		[Serializable]
		private struct OptimisticGrant
		{
			public Guid optimisticUuid;
			public int addedAmount;
		}

		// Client-side map from operationId -> optimistic grant context
		private readonly Dictionary<Guid, OptimisticGrant> optimisticGrants = new Dictionary<Guid, OptimisticGrant>();

		// ------------------ Client wrappers ------------------
		[Client]
		public Guid ClientRegisterOptimistic(ItemDefinition item, int amount)
		{
			if (item == null) return Guid.Empty;
			ItemInstance instance = new ItemInstance(item, amount);
			ItemInstance stored = playerInventory.ClientMergeOrAdd(instance);
			if (stored == null) return Guid.Empty;

			Guid operationId = Guid.NewGuid();
			optimisticGrants[operationId] = new OptimisticGrant
			{
				optimisticUuid = stored.uuid,
				addedAmount = amount,
			};
			return operationId;
		}

		// ------------------ Server wrappers ------------------
		[Server]
		public ItemInstance ServerAddAndSync(ItemInstance inst)
		{
			ItemInstance toUpdate = playerInventory.ServerMergeOrAddAndSync(inst);
			DatabaseCommunications.AddOrUpdateItem(toUpdate, playerData.GetUuid());
			return toUpdate;
		}

		[Server]
		public ItemInstance ServerAddAuthoritative(ItemDefinition item, int amount)
		{
			var inst = new ItemInstance(item, amount);
			ItemInstance toUpdate = playerInventory.ServerMergeOrAddNoSync(inst);
			DatabaseCommunications.AddOrUpdateItem(toUpdate, playerData.GetUuid());
			return toUpdate;
		}

		[Server]
		public void ServerRemove(ItemInstance item)
		{
			playerInventory.RemoveItem(item.uuid);
			DatabaseCommunications.DestroyItem(item, playerData.GetUuid());
		}

		[Server]
		public bool ServerUseItem(ItemInstance itemReference)
		{
			if (itemReference == null)
			{
				Debug.LogWarning("Cannot use null item reference");
				return false;
			}
			bool success = playerInventory.ServerTryUseItem(itemReference);
			if (success)
			{
				DatabaseCommunications.AddOrUpdateItem(itemReference, playerData.GetUuid());
				DurabilityState durabilityState = itemReference.GetState<DurabilityState>();
				if (durabilityState != null && durabilityState.remaining <= 0)
				{
					playerInventory.RemoveItem(itemReference.uuid);
					DatabaseCommunications.DestroyItem(itemReference, playerData.GetUuid());
				}
			}
			return success;
		}

		[Server]
		public bool ServerConsume(ItemInstance itemReference)
		{
			if (itemReference == null)
			{
				Debug.LogWarning("Cannot consume from null item reference");
				return false;
			}
			bool success = playerInventory.ServerConsumeFromStack(itemReference);
			if (success)
			{
				StackState stackState = itemReference.GetState<StackState>();
				if (stackState != null && stackState.currentAmount <= 0)
				{
					playerInventory.RemoveItem(itemReference.uuid);
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

		[Server]
		public void ServerConfirm(Guid operationId, Guid realItemUuid)
		{
			TargetConfirm(connectionToClient, operationId, realItemUuid);
		}

		[Server]
		public void ServerDeny(Guid operationId, int addedAmount)
		{
			TargetDeny(connectionToClient, operationId, addedAmount);
		}

		[TargetRpc]
		private void TargetConfirm(NetworkConnectionToClient target, Guid operationId, Guid realUuid)
		{
			if (optimisticGrants.TryGetValue(operationId, out var grant))
			{
				ItemInstance optimisticItem = playerInventory.GetItem(grant.optimisticUuid);
				if (optimisticItem != null)
				{
					optimisticItem.uuid = realUuid;
				}
				optimisticGrants.Remove(operationId);
			}
		}

		[TargetRpc]
		private void TargetDeny(NetworkConnectionToClient target, Guid operationId, int addedAmount)
		{
			if (optimisticGrants.TryGetValue(operationId, out var grant))
			{
				int amount = addedAmount > 0 ? addedAmount : grant.addedAmount;
				playerInventory.ClientRollbackOptimisticAdd(grant.optimisticUuid, amount);
				optimisticGrants.Remove(operationId);
			}
		}
	}
}
