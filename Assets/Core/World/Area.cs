using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public enum Area
{
    // Don't even think about adding an area BEFORE the END of the list, this will fuck up world travel and database things. Really, don't!
    WorldMap = 0,
    Container = 1,
    FusetaBeach = 2,
    SelvaBandeira = 3,
    Greenfields = 4,
    Baker = 5,
    FishMarket = 6,
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
        { Area.FishMarket, 100 },
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
        { Area.FishMarket, new LevelUnlockCriteria(0) },
    };

    public static bool IsAreaUnlocked(Area area, PlayerData playerData)
    {
        if (_unlockCriteria.TryGetValue(area, out var criteria))
        {
            return criteria.IsUnlocked(playerData);
        }
        return false;
    }

    /// <summary>
    /// All areas that are unlockable by level, ordered by area id so the result is deterministic
    /// </summary>
    public static List<Area> GetLevelUnlockableAreas()
    {
        return _unlockCriteria
            .Where(pair => pair.Value is LevelUnlockCriteria)
            .Select(pair => pair.Key)
            .OrderBy(area => (int)area)
            .ToList();
    }
}

public static class SceneToAreaMapper
{
    private static Dictionary<string, Area> _sceneToAreaMap = new Dictionary<string, Area>
    {
        { "WorldMap", Area.WorldMap },
        { "Container", Area.Container },
        { "FusetaBeach", Area.FusetaBeach },
        { "SelvaBandeira", Area.SelvaBandeira },
        { "Greenfields", Area.Greenfields },
        { "Baker", Area.Baker },
        { "FishMarket", Area.FishMarket },
    };

    private static HashSet<string> _excludedScenes = new HashSet<string>
    {
        "Startup", // Startup scene (offline/initial scene, doesn't need Area mapping)
    };

    public static Area GetAreaFromSceneName(string sceneName)
    {
        if (_sceneToAreaMap.TryGetValue(sceneName, out Area area))
        {
            return area;
        }

        Debug.LogWarning($"Could not map scene name '{sceneName}' to an Area, defaulting to WorldMap");
        return Area.WorldMap;
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void ValidateSceneMappings()
    {
        // Delay validation slightly to avoid serialization issues
        EditorApplication.delayCall += () =>
        {
            try
            {
                // Get all enabled scenes from build settings
                HashSet<string> buildSettingsSceneNames = new HashSet<string>();
                var scenes = EditorBuildSettings.scenes;
                
                if (scenes == null || scenes.Length == 0)
                {
                    return; // Build settings not ready yet
                }

                foreach (var scene in scenes)
                {
                    if (scene != null && scene.enabled && !string.IsNullOrEmpty(scene.path))
                    {
                        string sceneName = Path.GetFileNameWithoutExtension(scene.path);
                        if (!string.IsNullOrEmpty(sceneName))
                        {
                            buildSettingsSceneNames.Add(sceneName);
                        }
                    }
                }

                // Check 1: All scenes in build settings should have a mapping (excluding excluded scenes)
                List<string> missingMappings = new List<string>();
                foreach (string sceneName in buildSettingsSceneNames)
                {
                    if (!_excludedScenes.Contains(sceneName) && !_sceneToAreaMap.ContainsKey(sceneName))
                    {
                        missingMappings.Add(sceneName);
                    }
                }

                if (missingMappings.Count > 0)
                {
                    Debug.LogError($"[SceneToAreaMapper] The following scenes in build settings are missing mappings: {string.Join(", ", missingMappings)}");
                }

                // Check 2: All mappings should have corresponding scenes in build settings
                List<string> orphanedMappings = new List<string>();
                foreach (string mappedSceneName in _sceneToAreaMap.Keys)
                {
                    if (!buildSettingsSceneNames.Contains(mappedSceneName))
                    {
                        orphanedMappings.Add(mappedSceneName);
                    }
                }

                if (orphanedMappings.Count > 0)
                {
                    Debug.LogError($"[SceneToAreaMapper] The following scene mappings have no corresponding scene in build settings: {string.Join(", ", orphanedMappings)}");
                }

                // Success message if everything is valid
                if (missingMappings.Count == 0 && orphanedMappings.Count == 0)
                {
                    Debug.Log($"[SceneToAreaMapper] All scene mappings are valid. {_sceneToAreaMap.Count} scenes mapped.");
                }
            }
            catch (System.Exception ex)
            {
                // Silently catch exceptions during validation to avoid breaking editor
                Debug.LogWarning($"[SceneToAreaMapper] Validation failed: {ex.Message}");
            }
        };
    }
#endif
}
