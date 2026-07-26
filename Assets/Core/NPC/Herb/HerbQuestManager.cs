using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FishyGame.Api;
using ItemSystem;
using UnityEngine;

/// <summary>
/// One fish species Herb asks for and how many of it
/// </summary>
public class HerbQuestEntry
{
    public int fishId;
    public int amount;
}

/// <summary>
/// The daily quest of Herb: a mix of fishes to bring him for a fishcoin reward.
///
/// The quest is owned by the database server, not generated in the game anymore.
/// The game server polls it (see <see cref="HerbSpawnManager"/>) and the clients
/// receive it through the SyncVars on the spawned Herb (see <see cref="HerbQuestSync"/>).
/// </summary>
public class HerbQuest
{
    // Unique id of this quest, decided by the database. Used to detect a new quest and to
    // remember which quest a player already accepted/handed in (a UUID rather than a date,
    // so it never collides even if a server's clock resets).
    public Guid questId = Guid.Empty;
    public Area area;
    // Reward comes straight from the database, it is no longer derived in the game.
    public int rewardCoins;
    // When the database rolls over to the next quest (Herb's next move), in UTC.
    public DateTime nextMoveAtUtc;
    public List<HerbQuestEntry> entries = new List<HerbQuestEntry>();

    public int RewardCoins => rewardCoins;

    // Defensive: a quest without fishes should not happen (the database guarantees a
    // valid quest), but the game never crashes on an empty one either.
    public bool IsValid => entries.Count > 0;

    public Guid QuestId => questId;

    public string DescribeFishes()
    {
        return string.Join(", ", entries.Select(entry =>
        {
            ItemDefinition def = ItemRegistry.Get(entry.fishId);
            string name = def != null ? def.DisplayName : $"fish {entry.fishId}";
            return $"{entry.amount}x {name}";
        }));
    }
}

/// <summary>
/// Holds the daily quest that is currently active on the game server.
///
/// The game no longer generates the quest; the database is the single source of truth.
/// <see cref="HerbSpawnManager"/> polls the database and updates this store, and the
/// server side hand-in code (see PlayerDataHerbQuest) validates against it.
/// This lives on the server only, clients read the quest from the spawned Herb instead.
/// </summary>
public static class HerbQuestService
{
    /// <summary>
    /// The quest that is active right now, or null when it has not been fetched yet.
    /// </summary>
    public static HerbQuest CurrentQuest { get; private set; }

    public static void SetCurrentQuest(HerbQuest quest)
    {
        CurrentQuest = quest;
    }
}

/// <summary>
/// Helpers around Herb's daily quest: building it from a database response and
/// checking a player's inventory against it. All quest generation now happens on
/// the database server, so this class only reads and validates.
/// </summary>
public static class HerbQuestManager
{
    /// <summary>
    /// Turns a database response into a <see cref="HerbQuest"/>. Returns null when the
    /// response could not be interpreted (the caller should keep the previous quest then).
    /// </summary>
    public static HerbQuest FromResponse(CurrentHerbQuestResponse response)
    {
        if (response == null || !Guid.TryParse(response.herb_quest_id, out Guid parsedId))
        {
            return null;
        }

        HerbQuest quest = new HerbQuest
        {
            questId = parsedId,
            area = (Area)response.area_id,
            rewardCoins = response.reward_coins,
            nextMoveAtUtc = ParseUtc(response.next_move_at),
            entries = new List<HerbQuestEntry>(),
        };

        if (response.fishes != null)
        {
            foreach (HerbQuestFishDto fish in response.fishes)
            {
                quest.entries.Add(new HerbQuestEntry { fishId = fish.fish_id, amount = fish.amount });
            }
        }

        return quest;
    }

    private static DateTime ParseUtc(string iso8601)
    {
        if (!string.IsNullOrEmpty(iso8601) &&
            DateTime.TryParse(iso8601, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime parsed))
        {
            return parsed;
        }

        // Unknown/absent next move time: poll again soon rather than never.
        Debug.LogWarning($"[HerbQuestManager] Could not parse next_move_at '{iso8601}', will retry shortly");
        return DateTime.UtcNow;
    }

    /// <summary>
    /// Counts how many of the given fish the inventory holds over all stacks
    /// </summary>
    public static int CountFishInInventory(PlayerInventory inventory, int fishId)
    {
        int total = 0;
        foreach (ItemInstance item in inventory.GetItems())
        {
            if (item.def.Id != fishId || item.def.GetBehaviour<FishBehaviour>() == null)
            {
                continue;
            }
            // Static/infinite items can not be handed in
            if (item.def.IsStatic || item.def.InfiniteUse)
            {
                continue;
            }
            StackState stack = item.GetState<StackState>();
            if (stack != null)
            {
                total += stack.currentAmount;
            }
        }
        return total;
    }

    public static bool HasAllQuestFishes(PlayerInventory inventory, HerbQuest quest)
    {
        return quest.entries.All(entry => CountFishInInventory(inventory, entry.fishId) >= entry.amount);
    }
}
