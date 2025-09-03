using UnityEngine;

public static class SpawnPointProvider
{
    /// Returns a custom spawn position from the per-scene registry if configured; otherwise null.
    public static Vector3? TryGetCustomSpawnPoint(Area targetArea, WorldTravel.CustomSpawnInstruction instruction)
    {
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(targetArea.ToString());
        var registry = SceneObjectCache.GetSpawnPointRegistry(activeScene);
        if (registry != null && registry.TryGet(instruction, out var spawn, out _))
        {
            if (spawn != null)
            {
                return spawn.position;
            }
        }
        return null;
    }

    /// Returns an arrival target from the per-scene registry if configured; otherwise null.
    public static Vector3? TryGetArrivalTarget(Area targetArea, WorldTravel.CustomSpawnInstruction instruction)
    {
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(targetArea.ToString());
        var registry = SceneObjectCache.GetSpawnPointRegistry(activeScene);
        if (registry != null && registry.TryGet(instruction, out _, out var target))
        {
            if (target != null)
            {
                return target.position;
            }
        }
        return null;
    }
} 