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



		//Max = 10

		// Server has 9
		// Client has 8

		// Client adds 1
		// Client adds 1

		// Client has 10

		// Server adds 1
		// Server adds 1

		// Server has 10 and 1
		// ------------------ Client ------------------
		[Client]
		public Guid ClientRegisterOptimistic(ItemDefinition item, int amount)
		{
			if (item == null) return Guid.Empty;
			ItemInstance instance = new ItemInstance(item, amount);
			ItemInstance stored = playerInventory.TryMergeOrAdd(instance);
			if (stored == null) return Guid.Empty;

			Guid operationId = Guid.NewGuid();
			optimisticGrants[operationId] = new OptimisticGrant
			{
				optimisticUuid = stored.uuid,
				addedAmount = amount,
			};
			return operationId;
		}

		// ------------------ TargetRPC ------------------
		[TargetRpc]
		private void TargetConfirm(NetworkConnectionToClient target, Guid operationId, Guid realUuid)
		{
			if (optimisticGrants.TryGetValue(operationId, out var grant))
			{
				if (grant.optimisticUuid != realUuid)
				{
					ItemInstance item = playerInventory.ClientRollbackOptimisticAdd(grant.optimisticUuid, grant.addedAmount);
					if (item != null)
					{
						item.uuid = realUuid;
						StackState stack = item.GetState<StackState>();
						stack.currentAmount = grant.addedAmount;
						item.SetState(stack);
						playerInventory.TryMergeOrAdd(item);
					}
				}
				optimisticGrants.Remove(operationId);
			}
		}

		[TargetRpc]
		private void TargetDeny(NetworkConnectionToClient target, Guid operationId, int addedAmount)
		{
			if (optimisticGrants.TryGetValue(operationId, out var grant))
			{
				playerInventory.ClientRollbackOptimisticAdd(grant.optimisticUuid, addedAmount);
				optimisticGrants.Remove(operationId);
			}
		}

		// ------------------ Server ------------------
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
	}
}
