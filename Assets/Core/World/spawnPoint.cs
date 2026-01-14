using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class spawnPoint : MonoBehaviour
{
    public static readonly Dictionary<string, List<Vector2>> spawnPoints = new Dictionary<string, List<Vector2>>();

    [ServerCallback]
    private void Awake()
    {
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
            return points[Random.Range(0, points.Count)];
        }
        Debug.LogWarning($"Could not find random spawn point in scene {sceneName}");
        return Vector2.zero;
    }
}
