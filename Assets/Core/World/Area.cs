using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum Area
{
    WorldMap,
    FusetaBeach,
    SelvaBandeira,
    Greenfields,
    Baker,
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

public static class AreaCameraZoomManager
{
    private static Dictionary<Area, int> _zoomPercentage = new Dictionary<Area, int>
    {
        { Area.FusetaBeach, 100 },
        { Area.SelvaBandeira, 100 },
        { Area.Greenfields, 100 },
        { Area.Baker, 160 },
    };

    public static int GetCameraZoomPercentage(Area area)
    {
        if (_zoomPercentage.TryGetValue(area, out int percentage)) {
            return percentage;
        }
        return 100;
    }
}

public static class AreaUnlockManager
{
    private static Dictionary<Area, IUnlockCriteria> _unlockCriteria = new Dictionary<Area, IUnlockCriteria>
    {
        { Area.FusetaBeach, new LevelUnlockCriteria(0) },
        { Area.SelvaBandeira, new LevelUnlockCriteria(0) },
        { Area.Greenfields, new LevelUnlockCriteria(0) },
        { Area.Baker, new LevelUnlockCriteria(0) },
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
