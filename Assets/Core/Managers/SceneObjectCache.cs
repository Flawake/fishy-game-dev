using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class SceneObjectCache
{
    private static Dictionary<Scene, CompositeCollider2D> colliderLookup = new();

    private static Dictionary<Scene, PathFinding> pathfinderLookup = new();

    private static Dictionary<Scene, SpawnPointRegistry> spawnRegistryLookup = new();

    public static CompositeCollider2D GetWorldCollider(Scene scene)
    {
        if (colliderLookup.TryGetValue(scene, out var collider))
        {
            return collider;
        }

        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            if(obj.name == "Root") {
                CompositeCollider2D coll = obj.GetComponent<CompositeCollider2D>();
                colliderLookup[scene] = coll;
                return coll;
            }
        }

        return null;
    }

    public static PathFinding GetPathFinding(Scene scene)
    {
        if (pathfinderLookup.TryGetValue(scene, out var pathfinder))
        {
            return pathfinder;
        }
        
        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            if(obj.name == "Root") {
                PathFinding pathFinder = obj.GetComponent<PathFinding>();
                pathfinderLookup[scene] = pathFinder;
                return pathFinder;
            }
        }

        return null;
    }

    public static SpawnPointRegistry GetSpawnPointRegistry(Scene scene)
    {
        if (spawnRegistryLookup.TryGetValue(scene, out var reg))
        {
            return reg;
        }
        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            if(obj.name == "Root") {
                SpawnPointRegistry r = obj.GetComponent<SpawnPointRegistry>();
                spawnRegistryLookup[scene] = r;
                return r;
            }
        }
        return null;
    }
}