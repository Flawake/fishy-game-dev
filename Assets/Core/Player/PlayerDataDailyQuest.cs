using System;
using System.Collections.Generic;
using ItemSystem;
using Mirror;
using UnityEngine;

/// <summary>
/// The daily quest (Herb) part of PlayerData: tracks which quest day the player
/// already completed and handles handing in the quest fishes.
/// </summary>
public partial class PlayerData
{
    // Quest date string ("yyyy-MM-dd") of the last completed daily quest.
    // Kept on the server as the authority and mirrored to the owning client for the dialog.
    private string lastDailyQuestCompleted = string.Empty;

    /// <summary>
    /// Fired on the owning client when the server processed a hand-in attempt.
    /// First argument tells if the hand-in succeeded, second is the amount of rewarded fishcoins.
    /// </summary>
    public event Action<bool, int> DailyQuestHandInProcessed;

    [Server]
    private void ServerLoadLastDailyQuestCompleted(string questDate)
    {
        lastDailyQuestCompleted = questDate ?? string.Empty;
    }

    public bool HasCompletedDailyQuest(HerbQuest quest)
    {
        return quest != null && lastDailyQuestCompleted == quest.QuestDateString;
    }

    [Command]
    public void CmdGetDailyQuestState()
    {
        TargetSetDailyQuestState(lastDailyQuestCompleted);
    }

    [TargetRpc]
    private void TargetSetDailyQuestState(string questDate)
    {
        lastDailyQuestCompleted = questDate ?? string.Empty;
    }

    /// <summary>
    /// Hands in the fishes of the current daily quest of Herb.
    /// The server validates the quest, removes the fishes from the inventory,
    /// rewards the fishcoins and persists everything atomically in the database.
    /// </summary>
    [Command]
    public void CmdHandInDailyQuest()
    {
        HerbQuest quest = HerbQuestManager.GetCurrentQuest();

        if (!quest.IsValid)
        {
            Debug.LogWarning("Player tried to hand in the daily quest but there is no valid quest today");
            TargetDailyQuestHandInProcessed(false, 0);
            return;
        }

        if (HasCompletedDailyQuest(quest))
        {
            Debug.Log("Player tried to hand in the daily quest twice on the same day");
            TargetDailyQuestHandInProcessed(false, 0);
            return;
        }

        if (!HerbQuestManager.HasAllQuestFishes(inventory, quest))
        {
            Debug.Log("Player tried to hand in the daily quest without having all quest fishes");
            TargetDailyQuestHandInProcessed(false, 0);
            return;
        }

        List<HandInFish> handedInFishes = ServerRemoveQuestFishes(quest);

        int reward = quest.RewardCoins;
        ChangeFishCoinsAmount(reward, true);
        lastDailyQuestCompleted = quest.QuestDateString;
        TargetSetDailyQuestState(lastDailyQuestCompleted);

        DatabaseCommunications.CompleteDailyQuest(GetUuid(), quest.QuestDateString, reward, handedInFishes);
        TargetDailyQuestHandInProcessed(true, reward);
    }

    /// <summary>
    /// Removes the quest fishes from the server side inventory (synced to the client)
    /// and builds the hand-in list for the database.
    /// </summary>
    [Server]
    private List<HandInFish> ServerRemoveQuestFishes(HerbQuest quest)
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
                // atomically by the complete_daily endpoint afterwards.
                inventory.ServerRemoveAmountFromStack(item, take, true);
                remaining -= take;

                if (stack.currentAmount <= 0)
                {
                    inventory.RemoveItem(item.uuid);
                    handedInFishes.Add(new HandInFish
                    {
                        fish_uid = item.uuid.ToString(),
                        fish_id = item.def.Id,
                        fish_amount = 0,
                        new_state_blob = null,
                    });
                }
                else
                {
                    handedInFishes.Add(new HandInFish
                    {
                        fish_uid = item.uuid.ToString(),
                        fish_id = item.def.Id,
                        fish_amount = stack.currentAmount,
                        new_state_blob = Convert.ToBase64String(StatePacker.Pack(item.state)),
                    });
                }
            }
        }

        return handedInFishes;
    }

    [TargetRpc]
    private void TargetDailyQuestHandInProcessed(bool success, int rewardCoins)
    {
        DailyQuestHandInProcessed?.Invoke(success, rewardCoins);
    }
}
