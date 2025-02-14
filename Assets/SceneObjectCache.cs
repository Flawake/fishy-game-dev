using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class SceneObjectCache
{
    private static Dictionary<Scene, CompositeCollider2D> colliderLookup = new();

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
}