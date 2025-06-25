using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;
using System;
using Unity.Collections;
using Unity.Jobs;
using System.Linq;

class Node
{
    public Vector2 WorldPoint;
    public bool walkable;
    // gscore(N) is the currently known cheapest path from start to n
    public float gscore;
    // fScore(N) is the estimated path from n to end
    public float fscore;
    public bool isPath;
    public bool beenSearched;

    public Node(Vector2 _worldPoint, bool _walkable)
    {
        WorldPoint = _worldPoint;
        walkable = _walkable;
        gscore = float.MaxValue;
        fscore = float.MaxValue;
        isPath = false;
        beenSearched = false;
    }

    public void ResetToDefault()
    {
        gscore = float.MaxValue;
        fscore = float.MaxValue;
        isPath = false;
        beenSearched = false;
    }
}

class NodeMap
{
    public const float NodeSize = 0.2f;
    public Node[,] Nodes;
    public Node StartNode;
    public Node EndNode;
    public Vector2 MapOrigin;

    public Vector2 NodeToWorldPoint(Vector2 point)
    {
        return new Vector2(
            MapOrigin.x + ((point.x + 0.5f) - Nodes.GetLength(0) / 2f) * NodeSize,
            MapOrigin.y + ((point.y + 0.5f) - Nodes.GetLength(1) / 2f) * NodeSize
        );
}

    public Vector2Int WorldPointToMapPos(Vector2 point)
    {
        int x = Mathf.FloorToInt((point.x - MapOrigin.x) / NodeSize + Nodes.GetLength(0) / 2f);
        int y = Mathf.FloorToInt((point.y - MapOrigin.y) / NodeSize + Nodes.GetLength(1) / 2f);
        return new Vector2Int(x, y);
    }

    public void AddNodeToMap(Node n, Vector2Int pointInArray, CompositeCollider2D worldCollider)
    {
        n.WorldPoint = NodeToWorldPoint(new Vector2(pointInArray.x, pointInArray.y));
        Collider2D[] hits = Physics2D.OverlapAreaAll(
            new Vector2(n.WorldPoint.x - (NodeSize / 2), n.WorldPoint.y - (NodeSize / 2)),
            new Vector2(n.WorldPoint.x + (NodeSize / 2), n.WorldPoint.y + (NodeSize / 2))
        );
        bool isWalkable = true;
        foreach (Collider2D hit in hits)
        {
            if (hit == worldCollider)
            {
                isWalkable = false;
                break;
            }
        }
        n.walkable = isWalkable;
        Nodes[pointInArray.x, pointInArray.y] = n;
    }

    public void ResetMap()
    {
        for (int x = 0; x < Nodes.GetLength(0); x++)
        {
            for (int y = 0; y < Nodes.GetLength(1); y++)
            {
                Nodes[x, y].ResetToDefault();
            }
        }
    }

    public void PrebuildMap(CompositeCollider2D mapCollider)
    {
        int nodeCountX = (int)(Mathf.Ceil(mapCollider.bounds.size.x) / NodeSize);
        int nodeCountY = (int)(Mathf.Ceil(mapCollider.bounds.size.y) / NodeSize);
        MapOrigin = new Vector2(mapCollider.bounds.min.x + (mapCollider.bounds.size.x / 2), mapCollider.bounds.min.y + (mapCollider.bounds.size.y / 2));
        Nodes = new Node[nodeCountX, nodeCountY];
        for (int x = 0; x < nodeCountX; x++)
        {
            for (int y = 0; y < nodeCountY; y++)
            {
                Node node = new Node(Vector2.zero, false);
                AddNodeToMap(node, new Vector2Int(x, y), mapCollider);
            }
        }
    }
}

public class PathFinding : MonoBehaviour
{
    private Dictionary<GameObject, (Vector2, Vector2, Action<List<Vector2>>)> pathsToDo = new Dictionary<GameObject, (Vector2, Vector2, Action<List<Vector2>>)>();
    private bool pathfinderRunning = false;
    readonly NodeMap map = new NodeMap();

    [SerializeField] bool showGizmos = false;
    
    private void Awake()
    {
        map.PrebuildMap(SceneObjectCache.GetWorldCollider(gameObject.scene));
        StartCoroutine(updatePathRequests());
    }
    
    float EndDistance(Vector2 a, Vector2 b)
    {
        return Vector2.Distance(a, b) * 2;
    }

    internal static List<Vector2> FilterPath(List<Vector2> path)
    {
        if (path.Count() < 3)
        {
            return path;
        }

        List<Vector2> filtered = new List<Vector2>
        {
            path[0]
        };

        Vector2 GetDirection(Vector2 from, Vector2 to)
        {
            Vector2 diff = to - from;
            return new Vector2(
                diff.x == 0 ? 0 : (diff.x > 0 ? 1 : -1),
                diff.y == 0 ? 0 : (diff.y > 0 ? 1 : -1)
            );
        }

        Vector2 prevDir = GetDirection(path[0], path[1]);

        for (int i = 1; i < path.Count() - 1; i++)
        {
            Vector2 currDir = GetDirection(path[i], path[i + 1]);

            if (currDir != prevDir)
            {
                // Direction changed, so keep the current point
                filtered.Add(path[i]);
                prevDir = currDir;
            }
        }
        // Add the endpoint
        filtered.Add(path[path.Count() - 1]);

        return filtered;
    }

    List<Vector2> ReconstructPath(Dictionary<Node, Node> fromPath, Node curr)
    {
        List<Vector2> path = new List<Vector2>();
        curr.isPath = true;
        path.Add(curr.WorldPoint);
        while (fromPath.ContainsKey(curr))
        {
            curr = fromPath[curr];
            path.Add(curr.WorldPoint);
            curr.isPath = true;
        }
        path.Reverse();
        return FilterPath(path);
    }

     public void queueNewPath(Vector2 StartPoint, Vector2 EndPoint, GameObject caller, Action<List<Vector2>> callback) {
        if(!pathfinderRunning) {
            FindPath(StartPoint, EndPoint, callback);
        }
        else {
            if(pathsToDo.ContainsKey(caller)) {
                pathsToDo[caller] = (StartPoint, EndPoint, callback);
            }
            else {
                pathsToDo.Add(caller, (StartPoint, EndPoint, callback));
            }
        }
    }

    IEnumerator updatePathRequests() {
        while(true) {
            if(pathfinderRunning == false && pathsToDo.Count > 0) {
                var first = pathsToDo.First();
                pathsToDo.Remove(first.Key);
                (Vector2 start, Vector2 end, Action<List<Vector2>> callback) = first.Value;
                FindPath(start, end, callback);
            }
            yield return 0;
        }
    }

    void FindPath(Vector2 StartPoint, Vector2 EndPoint, Action<List<Vector2>> callback)
    {
        if(pathfinderRunning == true) {
            Debug.LogWarning("Logic error, void FindPath was already running");
        }
        pathfinderRunning = true;
        map.ResetMap();

        Vector2Int startNodeCoord = map.WorldPointToMapPos(StartPoint);
        map.StartNode = map.Nodes[startNodeCoord.x, startNodeCoord.y];
        Vector2Int endNodeCoord = map.WorldPointToMapPos(EndPoint);
        map.EndNode = map.Nodes[endNodeCoord.x, endNodeCoord.y];

        if (!map.EndNode.walkable)
        {
            // Find closest neighbour that is walkable
            map.EndNode = FindClosestWalkable(map.EndNode);
        }

        map.StartNode.gscore = 0;
        map.StartNode.fscore = EndDistance(map.StartNode.WorldPoint, map.EndNode.WorldPoint);

        StartCoroutine(CalculatePath(callback));
    }

    Node FindClosestWalkable(Node endNode)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(map.WorldPointToMapPos(endNode.WorldPoint));
        visited.Add(map.WorldPointToMapPos(endNode.WorldPoint));

        Vector2Int[] directions = {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1),
            new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbourCoord = current + dir;
                if (neighbourCoord.x < 0 || neighbourCoord.x > map.Nodes.GetLength(0) -1 ||
                neighbourCoord.y < 0 || neighbourCoord.y > map.Nodes.GetLength(1) - 1 ||
                visited.Contains(neighbourCoord)
                )
                {
                    continue;
                }

                visited.Add(neighbourCoord);

                Node neighbour = map.Nodes[neighbourCoord.x, neighbourCoord.y];

                if (neighbour.walkable)
                {
                    return neighbour;
                }

                queue.Enqueue(neighbourCoord);
            }
        }

        Debug.LogWarning("No walkable node found near the end point.");
        return endNode;
    }

    //1/50th of a second.
    const float maxBlockingTime = 0.02f;
    IEnumerator CalculatePath(Action<List<Vector2>> doneCallback)
    {
        List<Vector2> foundPath = new List<Vector2>();
        Node closestSoFar = map.StartNode;
        PriorityQueue<Node> openSet = new PriorityQueue<Node>();
        openSet.Enqueue(map.StartNode, map.StartNode.fscore);

        // First Node from, second Node current
        Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();

        System.Diagnostics.Stopwatch watchdog = System.Diagnostics.Stopwatch.StartNew();
        while (openSet.Count > 0)
        {
            if(watchdog.Elapsed.TotalSeconds > maxBlockingTime) {
                yield return 0;
                watchdog.Restart();
            }
            Node currentNode = openSet.Dequeue();

            if(currentNode.fscore  < closestSoFar.fscore) {
                closestSoFar = currentNode;
            }

            if (currentNode == map.EndNode)
            {
                foundPath = ReconstructPath(cameFrom, map.EndNode);
                //return true;
                doneCallback(foundPath);
                pathfinderRunning = false;
                yield break;
            }

            List<Node> neighbours = new List<Node>();
            Vector2Int currentNodePos = map.WorldPointToMapPos(currentNode.WorldPoint);

            int maxX = map.Nodes.GetLength(0);
            int maxY = map.Nodes.GetLength(1);

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    if (currentNodePos.x + x > 0 && currentNodePos.x + x < maxX &&
                        currentNodePos.y + y > 0 && currentNodePos.y + y < maxY)
                    {
                        Node neighbour = map.Nodes[currentNodePos.x + x, currentNodePos.y + y];
                        if (neighbour.walkable)
                        {
                            neighbours.Add(neighbour);
                        }
                    }
                }
            }

            for (int i = 0; i < neighbours.Count; i++)
            {
                Node neighbour = neighbours[i];

                Vector2Int neighbourPos = map.WorldPointToMapPos(neighbour.WorldPoint);
                Vector2Int diff = currentNodePos - neighbourPos;

                int dx = Mathf.Abs(diff.x);
                int dy = Mathf.Abs(diff.y);

                float cost = (dx == 1 && dy == 1) ? 1.4f : 0.8f;
                float tentativeGscore = currentNode.gscore + cost;
                if (tentativeGscore < neighbour.gscore)
                {
                    neighbour.beenSearched = true;
                    neighbour.gscore = tentativeGscore;
                    neighbour.fscore = tentativeGscore + EndDistance(neighbour.WorldPoint, map.EndNode.WorldPoint);
                    cameFrom[neighbour] = currentNode;
                    openSet.Enqueue(neighbour, neighbour.fscore);
                }
            }
        }
        Debug.LogWarning("No path found");
        Debug.LogWarning($"Cheapest score: {closestSoFar.fscore}, is start: {map.StartNode.WorldPoint == closestSoFar.WorldPoint}");
        foundPath = ReconstructPath(cameFrom, closestSoFar);
        doneCallback(foundPath);
        pathfinderRunning = false;
        yield return 0;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || map?.Nodes == null)
        {
            return;
        }
        for (int i = 0; i < map.Nodes.GetLength(0); i++)       // rows
        {
            for (int j = 0; j < map.Nodes.GetLength(1); j++)   // columns
            {
                Node node = map.Nodes[i, j];
                // Do something with node
                if (map.StartNode.WorldPoint == node.WorldPoint)
                {
                    Gizmos.color = Color.green;
                }
                else if (map.EndNode.WorldPoint == node.WorldPoint)
                {
                    Gizmos.color = Color.yellow;
                }
                else if (!node.walkable)
                {
                    Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                }
                else if (node.isPath)
                {
                    Gizmos.color = Color.magenta;
                }
                else if(node.beenSearched) {
                    Gizmos.color = new Color(0.1f, 0.2f, 0.3f, 0.3f);
                }
                else
                {
                    Gizmos.color =  new Color(0.0f, 1f, 1f, 0.3f);
                }

                Vector3 center = new Vector3(
                    map.MapOrigin.x + ((i + 0.5f) - map.Nodes.GetLength(0) / 2f) * NodeMap.NodeSize,
                    map.MapOrigin.y + ((j + 0.5f) - map.Nodes.GetLength(1) / 2f) * NodeMap.NodeSize,
                    0
                );
                Gizmos.DrawCube(center, new Vector3(NodeMap.NodeSize, NodeMap.NodeSize, NodeMap.NodeSize));
                Gizmos.color = new Color(1f, 0.2f, 0.4f, 0.3f);
                Gizmos.DrawWireCube(center, new Vector3(NodeMap.NodeSize, NodeMap.NodeSize, NodeMap.NodeSize));
            }
        }
    }
}

[InitializeOnLoad]
public static class PathFilterValidator
{
    static PathFilterValidator()
    {
        var testCases = new[]
        {
            new {
                input = new List<Vector2> { new Vector2(1,1), new Vector2(2,2), new Vector2(3,3) },
                expected = new List<Vector2> { new Vector2(1,1), new Vector2(3,3) }
            },
            new {
                input = new List<Vector2> { new Vector2(1,2), new Vector2(1,3), new Vector2(1,4) },
                expected = new List<Vector2> { new Vector2(1,2), new Vector2(1,4) }
            },
            new {
                input = new List<Vector2> { new Vector2(1,1), new Vector2(2,2), new Vector2(2,3) },
                expected = new List<Vector2> { new Vector2(1,1), new Vector2(2,2), new Vector2(2,3) }
            }
        };

        int passed = 0;
        int failed = 0;

        foreach (var test in testCases)
        {
            var result = PathFinding.FilterPath(test.input);

            // Simple manual comparison:
            if (result.Count() == test.expected.Count())
            {
                bool allMatch = true;
                for (int i = 0; i < result.Count(); i++)
                {
                    if (result[i] != test.expected[i])
                    {
                        allMatch = false;
                        break;
                    }
                }
                if (allMatch)
                {
                    passed++;
                    continue;
                }
            }

            failed++;
        }

        Debug.Log($"FilterPath validation done. Passed: {passed}, Failed: {failed}");
    }
}
