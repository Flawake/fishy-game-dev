using UnityEngine;

public static class SpawnPointProvider
{
    /// Returns a custom spawn position if the combination of area and instruction is supported; otherwise null.
    public static Vector3? TryGetCustomSpawnPoint(Area targetArea, WorldTravel.CustomSpawnInstruction instruction)
    {
        // Prefer registry object in the active target scene
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(targetArea.ToString());
        var registry = SceneObjectCache.GetSpawnPointRegistry(activeScene);
        if (registry != null && registry.TryGet(instruction, out var spawn, out _))
        {
            if (spawn != null)
            {
                return spawn.position;
            }
        }

        // Fallback: code defaults
        switch (instruction)
        {
            case WorldTravel.CustomSpawnInstruction.WalkOusideBakery:
                if (targetArea == Area.Greenfields)
                {
                    return new Vector3(10.5f, 3.0f, 0f);
                }
                break;
        }
        return null;
    }

    /// Optional: provide an arrival target to path to from the spawn, enables natural walk-outs.
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

        // Fallback: code defaults
        switch (instruction)
        {
            case WorldTravel.CustomSpawnInstruction.WalkOusideBakery:
                if (targetArea == Area.Greenfields)
                {
                    return new Vector3(10.5f, 0.5f, 0f);
                }
                break;
        }
        return null;
    }
} 