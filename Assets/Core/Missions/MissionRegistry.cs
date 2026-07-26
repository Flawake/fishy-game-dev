using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves a mission ID to its Mission asset. Mirrors ItemRegistry: every
/// Mission asset under a Resources folder is loaded once, on first use.
/// </summary>
public static class MissionRegistry
{
    private static readonly Dictionary<int, Mission> byId = new();
    private static Mission[] missions;
    private static bool loaded = false;

    public static Mission Get(int id)
    {
        EnsureLoaded();
        if (!byId.TryGetValue(id, out Mission mission))
        {
            Debug.LogWarning($"Mission with id {id} not found in registry");
        }
        return mission;
    }

    public static bool TryGet(int id, out Mission mission)
    {
        EnsureLoaded();
        return byId.TryGetValue(id, out mission);
    }

    public static Mission[] GetFullMissionList()
    {
        EnsureLoaded();
        return missions;
    }

    private static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;

        missions = Resources.LoadAll<Mission>("");
        foreach (Mission mission in missions)
        {
            if (byId.ContainsKey(mission.MissionID))
            {
                Debug.LogWarning($"Duplicate Mission id {mission.MissionID} between {byId[mission.MissionID].name} and {mission.name}");
                continue;
            }
            byId[mission.MissionID] = mission;
        }
    }
}
