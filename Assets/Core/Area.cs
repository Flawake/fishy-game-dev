using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum Area
{
    WorldMap,
    FusetaBeach,
    SelvaBandeira,
    GreenfieldPonds,
}

public class AreaComponent : MonoBehaviour
{
    public Area area;
}

public interface IUnlockCriteria
{
    bool IsUnlocked(PlayerData playerData);
}

public class LevelUnlockCriteria : IUnlockCriteria
{
    private int _requiredLevel;

    public LevelUnlockCriteria(int level)
    {
        _requiredLevel = level;
    }

    public bool IsUnlocked(PlayerData playerData)
    {
        int playerLevel = LevelMath.XpToLevel(playerData.GetXp()).level;
        return playerLevel >= _requiredLevel;
    }
}

public class FishCaughtAmountUnlockCriteria : IUnlockCriteria
{
    private int requiredFishCount;

    public void FishCaughtUnlockCriteria(int fishCount)
    {
        requiredFishCount = fishCount;
    }


    public bool IsUnlocked(PlayerData playerData)
    {
        throw new NotImplementedException("IsUnlocked for FishCaughtAmountUnlockCriteria has not been implemented");
    }
}

public static class AreaUnlockManager
{
    private static Dictionary<Area, IUnlockCriteria> _unlockCriteria = new Dictionary<Area, IUnlockCriteria>
    {
        { Area.FusetaBeach, new LevelUnlockCriteria(0) },
        { Area.SelvaBandeira, new LevelUnlockCriteria(0) },
        { Area.GreenfieldPonds, new LevelUnlockCriteria(0) }
    };

    public static bool IsAreaUnlocked(Area area, PlayerData playerData)
    {
        if (_unlockCriteria.TryGetValue(area, out var criteria))
        {
            return criteria.IsUnlocked(playerData);
        }
        return false;
    }
}
