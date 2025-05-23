using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class spawnPoint : MonoBehaviour
{
    public static readonly Dictionary<string, List<Vector2>> spawnPoints = new Dictionary<string, List<Vector2>>();

    [ServerCallback]
    private void Awake()
    {
        Debug.Log("Awake is running, test to see if this is server or client build");
        if (!spawnPoints.ContainsKey(transform.gameObject.scene.name))
        {
            spawnPoints[transform.gameObject.scene.name] = new List<Vector2>();
        }
        spawnPoints[transform.gameObject.scene.name].Add(transform.position);
    }

    public static Vector2 GetRandomSpawnPoint(string sceneName)
    {
        if (spawnPoints.TryGetValue(sceneName, out List<Vector2> points) && points.Count > 0)
        {
            return points[UnityEngine.Random.Range(0, points.Count)];
        }
        Debug.LogWarning($"Could not find random spawn point in scene {sceneName}");
        return Vector2.zero;
    }
}
