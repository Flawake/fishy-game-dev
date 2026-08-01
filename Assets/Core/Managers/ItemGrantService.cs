using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using ItemSystem;

namespace Grants
{
	/// <summary>
	/// Applies item grants on the client before the server has agreed to them, and reconciles once it
	/// answers.
	///
	/// The client's guess and the server's decision are never matched up item by item. The client
	/// undoes its own guess, which it can do exactly because it recorded every stack it touched, and
	/// then takes the server's word for what those stacks now hold. Nothing depends on the two sides
	/// having split the grant across stacks the same way.
	/// </summary>
	public class ItemGrantService : NetworkBehaviour
	{
		[SerializeField] private PlayerInventory playerInventory;
		[SerializeField] private PlayerData playerData;

		private class OptimisticGrant
		{
			public InventoryChange change;
			public Notification notificationOnGrant;
		}

		// Client-side map from grant id -> optimistic grant context
		private readonly Dictionary<GrantId, OptimisticGrant> optimisticGrants = new Dictionary<GrantId, OptimisticGrant>();

		// Stacks that grants still in flight created. Nothing may merge into them, so each grant owns
		// what it created and can take it back whole instead of unpicking someone else's units from it.
		private readonly HashSet<Guid> pendingCreatedUuids = new HashSet<Guid>();

		// ------------------ Client ------------------
		[Client]
		public GrantId ClientRegisterOptimistic(ItemDefinition item, int amount)
		{
			if (item == null) return GrantId.None;
			DeltaItem delta = new DeltaItem(item, amount);
			if (!playerInventory.TryMergeOrAdd(delta, pendingCreatedUuids, out InventoryChange change))
			{
				Debug.LogWarning("Could not optimistically add an item");
				return GrantId.None;
			}

			GrantId grantId = GrantId.New();
			optimisticGrants[grantId] = new OptimisticGrant
			{
				change = change,
				notificationOnGrant = new Notification
				{
					message = $"Added {amount} {item.DisplayName} to inventory",
				}
			};

			foreach (DeltaItem created in change.Created)
			{
				pendingCreatedUuids.Add(created.ItemUUID);
			}
			return grantId;
		}

		// ------------------ TargetRPC ------------------
		[TargetRpc]
		private void TargetConfirm(NetworkConnectionToClient _, GrantId grantId, ItemInstance[] authoritativeItems, uint generationBefore, uint generationAfter)
		{
			if (!ClientTryTakeGrant(grantId, out OptimisticGrant grant))
			{
				return;
			}

			// Undo our own guess first. It is keyed by stacks we recorded ourselves, so it is exact
			// regardless of what the server ended up doing.
			playerInventory.Revert(grant.change);

			if (generationBefore != playerInventory.LastKnownGeneration)
			{
				// The server changed this inventory in a way we were never told about, so the stacks
				// below are not the only ones we have wrong. Take the whole thing again.
				Debug.LogWarning($"Inventory was built on generation {playerInventory.LastKnownGeneration} but the server granted from {generationBefore}, resyncing");
				playerInventory.CmdGetInventory();
				return;
			}

			playerInventory.ApplyAuthoritative(authoritativeItems, generationAfter);

			if (grant.notificationOnGrant != null)
			{
				MessageUIHandler.AddNotification(grant.notificationOnGrant);
			}
		}

		[TargetRpc]
		private void TargetDeny(NetworkConnectionToClient _, GrantId grantId)
		{
			if (!ClientTryTakeGrant(grantId, out OptimisticGrant grant))
			{
				return;
			}
			playerInventory.Revert(grant.change);
		}

		private bool ClientTryTakeGrant(GrantId grantId, out OptimisticGrant grant)
		{
			if (!optimisticGrants.TryGetValue(grantId, out grant))
			{
				return false;
			}

			optimisticGrants.Remove(grantId);
			foreach (DeltaItem created in grant.change.Created)
			{
				pendingCreatedUuids.Remove(created.ItemUUID);
			}
			return true;
		}

		// ------------------ Server ------------------
		[Server]
		public void ServerConfirm(GrantId grantId, InventoryChange change)
		{
			if (!grantId.IsValid)
			{
				return;
			}
			if (change == null)
			{
				ServerDeny(grantId);
				return;
			}

			TargetConfirm(
				connectionToClient,
				grantId,
				playerInventory.SnapshotOf(change),
				change.GenerationBefore,
				change.GenerationAfter);
		}

		[Server]
		public void ServerDeny(GrantId grantId)
		{
			if (!grantId.IsValid)
			{
				return;
			}
			TargetDeny(connectionToClient, grantId);
		}
	}
}
