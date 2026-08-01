using System;
using System.Collections.Generic;
using FishyGame.Api;
using ItemSystem;
using Mirror;
using UnityEngine;

/// <summary>
/// The Herb quest part of PlayerData: tracks which quest the player already
/// accepted and completed, and handles handing in the quest fishes.
/// </summary>
public partial class PlayerData
{
    // Id of the last completed Herb quest.
    // Kept on the server as the authority and mirrored to the owning client for the dialog.
    private Guid lastCompletedHerbQuestId;

    // Id of the last accepted (seen) Herb quest.
    // Lets the dialog skip Herb's introduction once the player has already met him today.
    private Guid lastAcceptedHerbQuestId;

    /// <summary>
    /// Fired on the owning client when the server processed a hand-in attempt.
    /// First argument tells if the hand-in succeeded, second is the amount of rewarded fishcoins.
    /// </summary>
    public event Action<bool, int> HerbQuestHandInProcessed;

    [Server]
    private void ServerLoadLastCompletedHerbQuestId(Guid herbQuestId)
    {
        lastCompletedHerbQuestId = herbQuestId;
    }

    [Server]
    private void ServerLoadLastAcceptedHerbQuestId(Guid herbQuestId)
    {
        lastAcceptedHerbQuestId = herbQuestId;
    }

    public bool HasCompletedHerbQuest(HerbQuest quest)
    {
        return quest != null && lastCompletedHerbQuestId == quest.QuestId;
    }

    public bool HasAcceptedHerbQuest(HerbQuest quest)
    {
        return quest != null && lastAcceptedHerbQuestId == quest.QuestId;
    }

    [Command]
    public void CmdGetHerbQuestState()
    {
        TargetSetHerbQuestState(lastCompletedHerbQuestId, lastAcceptedHerbQuestId);
    }

    [TargetRpc]
    private void TargetSetHerbQuestState(Guid completedId, Guid acceptedId)
    {
        lastCompletedHerbQuestId = completedId;
        lastAcceptedHerbQuestId = acceptedId;
    }

    /// <summary>
    /// Records that the player accepted (saw) the current Herb quest, so the dialog
    /// can skip Herb's introduction from now on. Persisted so it survives relogs.
    /// </summary>
    [Command]
    public void CmdAcceptHerbQuest()
    {
        HerbQuest quest = HerbQuestService.CurrentQuest;
        if (quest == null || !quest.IsValid)
        {
            return;
        }
        if (lastAcceptedHerbQuestId == quest.QuestId)
        {
            return;
        }

        lastAcceptedHerbQuestId = quest.QuestId;
        DatabaseCommunications.AcceptHerbQuest(GetUuid(), quest.QuestId);
        TargetSetHerbQuestAccepted(lastAcceptedHerbQuestId);
    }

    [TargetRpc]
    private void TargetSetHerbQuestAccepted(Guid acceptedId)
    {
        lastAcceptedHerbQuestId = acceptedId;
    }

    /// <summary>
    /// Hands in the fishes of the current Herb quest.
    /// The server validates the quest, removes the fishes from the inventory,
    /// rewards the fishcoins and persists everything atomically in the database.
    /// </summary>
    [Command]
    public void CmdHandInHerbQuest()
    {
        // The quest is owned by the database and polled by HerbSpawnManager into HerbQuestService.
        HerbQuest quest = HerbQuestService.CurrentQuest;

        if (quest == null || !quest.IsValid)
        {
            Debug.LogWarning("Player tried to hand in the Herb quest but there is no valid quest right now");
            TargetHerbQuestHandInProcessed(false, 0);
            return;
        }

        if (HasCompletedHerbQuest(quest))
        {
            Debug.Log("Player tried to hand in the Herb quest twice");
            TargetHerbQuestHandInProcessed(false, 0);
            return;
        }

        if (!HerbQuestManager.HasAllQuestFishes(inventory, quest))
        {
            Debug.Log("Player tried to hand in the Herb quest without having all quest fishes");
            TargetHerbQuestHandInProcessed(false, 0);
            return;
        }

        List<HandInFish> handedInFishes = ServerRemoveHerbQuestFishes(quest);

        int reward = quest.RewardCoins;
        ChangeFishCoinsAmount(reward, true);
        lastCompletedHerbQuestId = quest.QuestId;
        TargetSetHerbQuestState(lastCompletedHerbQuestId, lastAcceptedHerbQuestId);

        DatabaseCommunications.CompleteHerbQuest(GetUuid(), quest.QuestId, reward, handedInFishes);
        TargetHerbQuestHandInProcessed(true, reward);
    }

    /// <summary>
    /// Removes the quest fishes from the server side inventory (synced to the client)
    /// and builds the hand-in list for the database.
    /// </summary>
    [Server]
    private List<HandInFish> ServerRemoveHerbQuestFishes(HerbQuest quest)
    {
        List<HandInFish> handedInFishes = new List<HandInFish>();

        foreach (HerbQuestEntry entry in quest.entries)
        {
            int remaining = entry.amount;
            // A fish species can be spread over multiple stacks
            foreach (ItemInstance item in new List<ItemInstance>(inventory.GetItems()))
            {
                if (remaining <= 0)
                {
                    break;
                }
                if (item.def.Id != entry.fishId || item.def.GetBehaviour<FishBehaviour>() == null)
                {
                    continue;
                }
                if (item.def.IsStatic || item.def.InfiniteUse)
                {
                    continue;
                }
                StackState stack = item.GetState<StackState>();
                if (stack == null || stack.currentAmount <= 0)
                {
                    continue;
                }

                int take = Math.Min(remaining, stack.currentAmount);
                // Only touch the in-memory inventory here, the database is updated
                // atomically by the complete endpoint afterwards.
                if(!inventory.ServerRemoveAmountFromSpecifiedStack(item, take, true, out InventoryChange _)) {
                   Debug.LogWarning("Could not remove items from stack of players inventory");
                   return null;
                }
                remaining -= take;

                if (stack.currentAmount <= 0)
                {
                    inventory.RemoveItem(item.uuid, true);
                    handedInFishes.Add(new HandInFish
                    {
                        fish_uid = item.uuid.ToString(),
                        fish_id = item.def.Id,
                        amount_handed_in = 0,
                    });
                }
                else
                {
                    handedInFishes.Add(new HandInFish
                    {
                        fish_uid = item.uuid.ToString(),
                        fish_id = item.def.Id,
                        amount_handed_in = -take,
                    });
                }
            }
        }

        return handedInFishes;
    }

    [TargetRpc]
    private void TargetHerbQuestHandInProcessed(bool success, int rewardCoins)
    {
        HerbQuestHandInProcessed?.Invoke(success, rewardCoins);
    }
}
