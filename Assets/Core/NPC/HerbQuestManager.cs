using System;
using System.Collections.Generic;
using System.Linq;
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
/// The daily quest of Herb: a mix of 2-4 fishes that are all catchable in the area Herb is standing in
/// </summary>
public class HerbQuest
{
    public int questDayNumber;
    // The Amsterdam civil date on which this quest started (at 04:00)
    public DateTime questDate;
    public Area area;
    public List<HerbQuestEntry> entries = new List<HerbQuestEntry>();

    // 2 fishes = 2 coins, 3 fishes = 3 coins, 4 fishes = 4 coins
    public int RewardCoins => entries.Sum(entry => entry.amount);

    // A quest without fishes can happen when no fish is eligible in any area (e.g. all out of season)
    public bool IsValid => entries.Count > 0;

    public string QuestDateString => questDate.ToString("yyyy-MM-dd");

    public bool HasSameFishesAs(HerbQuest other)
    {
        if (other == null || entries.Count != other.entries.Count)
        {
            return false;
        }
        // Entries are sorted by fish id, so they can be compared pairwise
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].fishId != other.entries[i].fishId || entries[i].amount != other.entries[i].amount)
            {
                return false;
            }
        }
        return true;
    }

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
/// Generates the daily quest of Herb and decides in which area Herb is standing.
///
/// Everything is generated deterministically from the quest day number, so every game server
/// and every client computes exactly the same quest without any synchronisation.
/// A new quest day starts at 04:00 Amsterdam time.
/// </summary>
public static class HerbQuestManager
{
    // Herb moves and finds a new quest at 04:00 Amsterdam time
    private const int RolloverHour = 4;
    private const int MinQuestFishes = 2;
    private const int MaxQuestFishes = 4;
    private const int MaxRerollAttempts = 32;
    // Salt so Herbs rng stream differs from anything else that might seed on the day number
    private const int SeedSalt = 0x4865_7262; // "Herb"

    // The quest sequence is computed day by day from this epoch, never change it
    // or every already handed out quest changes with it.
    private static readonly DateTime SequenceEpoch = new DateTime(2026, 1, 1);

    private static HerbQuest cachedQuest;
    private static TimeZoneInfo amsterdamTimeZone;

    /// <summary>
    /// The quest that is active right now, same for everybody
    /// </summary>
    public static HerbQuest GetCurrentQuest()
    {
        int day = CurrentQuestDayNumber();
        if (cachedQuest == null || cachedQuest.questDayNumber != day)
        {
            cachedQuest = GenerateQuestSequenceUpTo(day);
        }
        return cachedQuest;
    }

    /// <summary>
    /// The quest day number changes at 04:00 Amsterdam time, this is Herbs timer
    /// </summary>
    public static int CurrentQuestDayNumber()
    {
        DateTime amsterdamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, AmsterdamTimeZone());
        DateTime questDate = amsterdamNow.AddHours(-RolloverHour).Date;
        return Math.Max(0, (int)(questDate - SequenceEpoch).TotalDays);
    }

    private static DateTime QuestDateForDayNumber(int dayNumber)
    {
        return SequenceEpoch.AddDays(dayNumber);
    }

    private static TimeZoneInfo AmsterdamTimeZone()
    {
        if (amsterdamTimeZone != null)
        {
            return amsterdamTimeZone;
        }

        // IANA id works on Linux/macOS (and modern .NET on Windows), the windows id is the fallback
        foreach (string timeZoneId in new[] { "Europe/Amsterdam", "W. Europe Standard Time" })
        {
            try
            {
                amsterdamTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return amsterdamTimeZone;
            }
            catch (Exception)
            {
                // Try the next id
            }
        }

        // Platforms without a timezone database (some WebGL builds): fall back to CET without
        // daylight saving. Worst case the rollover happens at 05:00 instead of 04:00 in summer.
        Debug.LogWarning("[HerbQuestManager] Could not find the Amsterdam timezone, falling back to fixed UTC+1");
        amsterdamTimeZone = TimeZoneInfo.CreateCustomTimeZone("Amsterdam-Fallback", TimeSpan.FromHours(1), "Amsterdam Fallback", "Amsterdam Fallback");
        return amsterdamTimeZone;
    }

    /// <summary>
    /// All fishes Herb may ask for in the given area on the given date:
    /// 1 or 2 stars rarity, catchable in the area, no time-of-day requirement,
    /// and when the fish has date requirements the quest date must fall within one of them.
    /// Sorted by fish id so the result is deterministic.
    /// </summary>
    public static List<ItemDefinition> GetEligibleQuestFishes(Area area, DateTime date)
    {
        if (!FishEnumConfig.TryAreaToLocation(area, out Locations location))
        {
            return new List<ItemDefinition>();
        }

        return ItemRegistry.GetFullItemsList()
            .Where(def =>
            {
                FishBehaviour fish = def.GetBehaviour<FishBehaviour>();
                if (fish == null)
                {
                    return false;
                }
                if (fish.Rarity != FishRarity.COMMON && fish.Rarity != FishRarity.UNCOMMON)
                {
                    return false;
                }
                if ((fish.Locations & location) == 0)
                {
                    return false;
                }
                // A fish that bites on no bait can never be caught
                if (fish.BitesOn == 0)
                {
                    return false;
                }
                // Herb only asks for fishes without a specific time requirement
                if (fish.TimeRanges.Count > 0)
                {
                    return false;
                }
                // Date requirements are fine, but only when the quest date falls within one
                if (fish.DateRanges.Count > 0 && !fish.DateRanges.Any(range => range.DateRangeContainsDate(date.Month, date.Day)))
                {
                    return false;
                }
                return true;
            })
            .OrderBy(def => def.Id)
            .ToList();
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

    // The quest of a day depends on the quest of the day before (Herb never repeats a quest
    // or an area two days in a row), so the sequence is replayed from the epoch.
    // This scans the item registry a few hundred times at most and is only done once per day.
    private static HerbQuest GenerateQuestSequenceUpTo(int targetDayNumber)
    {
        HerbQuest previous = null;
        for (int day = 0; day <= targetDayNumber; day++)
        {
            previous = GenerateQuestForDay(day, previous);
        }
        return previous;
    }

    private static HerbQuest GenerateQuestForDay(int dayNumber, HerbQuest previousQuest)
    {
        System.Random rng = new System.Random(dayNumber ^ SeedSalt);
        DateTime questDate = QuestDateForDayNumber(dayNumber);

        Area area = PickArea(rng, questDate, previousQuest);
        List<ItemDefinition> eligibleFishes = GetEligibleQuestFishes(area, questDate);

        List<HerbQuestEntry> entries = new List<HerbQuestEntry>();
        for (int attempt = 0; attempt < MaxRerollAttempts; attempt++)
        {
            entries = RollEntries(rng, eligibleFishes);
            // Herb can't have the same quest 2 times in a row
            if (previousQuest == null || !SameEntries(entries, previousQuest.entries))
            {
                break;
            }
        }

        return new HerbQuest
        {
            questDayNumber = dayNumber,
            questDate = questDate,
            area = area,
            entries = entries,
        };
    }

    private static Area PickArea(System.Random rng, DateTime questDate, HerbQuest previousQuest)
    {
        // Herb only stands in areas that are unlockable by level and actually have quest fishes
        List<Area> candidates = AreaUnlockManager.GetLevelUnlockableAreas()
            .Where(area => GetEligibleQuestFishes(area, questDate).Count > 0)
            .ToList();

        if (candidates.Count == 0)
        {
            Debug.LogWarning("[HerbQuestManager] No area has any eligible quest fish, Herb has nothing to ask for");
            return Area.FusetaBeach;
        }

        // Herb moves every day, so don't stand in the same area as yesterday when avoidable
        if (previousQuest != null && candidates.Count > 1)
        {
            candidates.Remove(previousQuest.area);
        }

        return candidates[rng.Next(candidates.Count)];
    }

    private static List<HerbQuestEntry> RollEntries(System.Random rng, List<ItemDefinition> eligibleFishes)
    {
        List<HerbQuestEntry> entries = new List<HerbQuestEntry>();
        if (eligibleFishes.Count == 0)
        {
            return entries;
        }

        // A random mix of 2-4 fishes, the same species may be asked multiple times
        int fishCount = rng.Next(MinQuestFishes, MaxQuestFishes + 1);
        Dictionary<int, int> amounts = new Dictionary<int, int>();
        for (int i = 0; i < fishCount; i++)
        {
            int fishId = eligibleFishes[rng.Next(eligibleFishes.Count)].Id;
            amounts.TryGetValue(fishId, out int current);
            amounts[fishId] = current + 1;
        }

        foreach (KeyValuePair<int, int> pair in amounts.OrderBy(pair => pair.Key))
        {
            entries.Add(new HerbQuestEntry { fishId = pair.Key, amount = pair.Value });
        }
        return entries;
    }

    private static bool SameEntries(List<HerbQuestEntry> a, List<HerbQuestEntry> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i].fishId != b[i].fishId || a[i].amount != b[i].amount)
            {
                return false;
            }
        }
        return true;
    }
}
