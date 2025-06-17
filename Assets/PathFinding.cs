using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

class Node
{
    public Vector2 WorldPoint;
    public bool walkable;
    // gscore(N) is the currently known cheapest path from start to n
    public float gscore;
    // fScore(N) is the estimated path from n to end
    public float fscore;
    public float distanceToGoal;
    public bool isPath;

    public Node()
    {
        WorldPoint = Vector2.zero;
        walkable = false;
        gscore = float.MaxValue;
        fscore = float.MaxValue;
        distanceToGoal = float.MaxValue;
        isPath = false;
    }

    public Node(Vector2 _worldPoint, bool _walkable)
    {
        WorldPoint = _worldPoint;
        walkable = _walkable;
        gscore = float.MaxValue;
        fscore = float.MaxValue;
        distanceToGoal = float.MaxValue;
        isPath = false;
    }

    public void ResetToDefault()
    {
        gscore = float.MaxValue;
        fscore = float.MaxValue;
        distanceToGoal = float.MaxValue;
        isPath = false;
    }
}

class NodeMap
{
    public Node[,] Nodes;
    public Node StartNode;
    public Node EndNode;
    public float NodeSize;
    public Vector2 MapOrigin;

    public Vector2 NodeToWorldPoint(Vector2 point)
    {
        return new Vector2(
            MapOrigin.x + ((point.x + 0.5f) - Nodes.GetLength(0) / 2f) * NodeSize,
            MapOrigin.y + ((point.y + 0.5f) - Nodes.GetLength(0) / 2f) * NodeSize
        );
    }

    public Vector2Int WorldPointToNode(Vector2 point)
    {
        int x = Mathf.FloorToInt((point.x - MapOrigin.x) / NodeSize + Nodes.GetLength(0) / 2f);
        int y = Mathf.FloorToInt((point.y - MapOrigin.y) / NodeSize + Nodes.GetLength(1) / 2f);
        return new Vector2Int(x, y);
    }

    public void AddNodeToMap(Node n, Vector2Int pointInArray, GameObject objectSearchingPath)
    {
        Vector2 nodeWorldPoint = NodeToWorldPoint(new Vector2(pointInArray.x, pointInArray.y));
        // Make the collider as least as big as half the player collider plus a little.
        Collider2D[] hits = Physics2D.OverlapAreaAll(
            new Vector2(nodeWorldPoint.x - 0.2f, nodeWorldPoint.y - 0.2f),
            new Vector2(nodeWorldPoint.x + 0.2f, nodeWorldPoint.y + 0.2f)
        );
        bool isWalkable = true;
        foreach (Collider2D hit in hits)
        {
            if (hit == SceneObjectCache.GetWorldCollider(objectSearchingPath.scene))
            {
                isWalkable = false;
                break;
            }
        }
        n.walkable = isWalkable;
        n.WorldPoint = nodeWorldPoint;
        Nodes[pointInArray.x, pointInArray.y] = n;
    }
}

class NodePool
{
    private Stack<Node> pool = new Stack<Node>();

    //Initialize the nodepool with x amount of nodes before the game starts to save cpu cycles when they matter.
    public NodePool(int amount)
    {
        List<Node> nodes = new List<Node>();
        for (int i = 0; i < amount; i++)
        {
            nodes.Add(Get());
        }
        foreach (Node n in nodes)
        {
            Return(n);
        }
    }

    public Node Get()
    {
        if (pool.Count > 0)
        {
            Node node = pool.Pop();
            node.ResetToDefault();
            return node;
        }
        return new Node();
    }

    public void Return(Node node)
    {
        pool.Push(node);
    }
}

public static class PathFinding
{
    readonly static NodeMap map = new NodeMap();
    static NodePool nodepool = new NodePool(5000);
    static float ManhattanDistance(Vector2 a, Vector2 b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    internal static List<Vector2> FilterPath(List<Vector2> path)
    {
        if (path == null || path.Count < 3)
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

        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector2 currDir = GetDirection(path[i], path[i + 1]);

            if (currDir != prevDir)
            {
                // Direction changed, so keep the current point
                filtered.Add(path[i]);
                prevDir = currDir;
            }
            // else direction same: skip this point
        }
        // Add the endpoint
        filtered.Add(path[path.Count - 1]);

        return filtered;
    }

    static List<Vector2> ReconstructPath(Dictionary<Node, Node> fromPath, Node curr)
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

    public static List<Vector2> FindPath(Vector2 StartPoint, Vector2 EndPoint, GameObject objectNeedingPath)
    {
        float distance = Vector2.Distance(StartPoint, EndPoint);
        float nodeSize = distance / 50;
        nodeSize = Mathf.Clamp(nodeSize, 0.03f, 1f);
        int arraySize = (int)(distance / nodeSize) * 4;
        //Make sure the arraysize is always uneven so we have a node at our midpoint
        arraySize = arraySize % 2 == 0 ? arraySize + 1 : arraySize;

        map.Nodes = new Node[arraySize, arraySize];
        map.NodeSize = nodeSize;
        // Set the map origin in the middle of the map
        map.MapOrigin = StartPoint;

        for (int i = 0; i < arraySize; i++)
        {
            for (int j = 0; j < arraySize; j++)
            {
                map.Nodes[i, j] = null;
            }
        }

        Vector2Int startNodeCoord = map.WorldPointToNode(StartPoint);
        Node startNode = nodepool.Get();
        map.AddNodeToMap(startNode, startNodeCoord, objectNeedingPath);
        map.StartNode = startNode;
        Vector2Int endNodeCoord = map.WorldPointToNode(EndPoint);
        Node endNode = nodepool.Get();
        map.AddNodeToMap(endNode, endNodeCoord, objectNeedingPath);
        map.EndNode = endNode;

        map.StartNode.gscore = 0;
        map.StartNode.fscore = ManhattanDistance(map.StartNode.WorldPoint, map.EndNode.WorldPoint);

        List<Vector2> path;
        bool found = CalculatePath(out path, objectNeedingPath);
        if (!found)
        {
            Debug.LogWarning("Could not find path");
        }

        foreach (Node n in map.Nodes)
        {
            if (n == null)
            {
                continue;
            }
            nodepool.Return(n);
        }

        return path;
    }

    static bool CalculatePath(out List<Vector2> foundPath, GameObject objectNeedingPath)
    {
        foundPath = null;
        Node closestSoFar = map.StartNode;
        PriorityQueue<Node> openSet = new PriorityQueue<Node>();
        openSet.Enqueue(map.StartNode, map.StartNode.fscore);

        // First Node from, second Node current
        Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();

        while (openSet.Count > 0)
        {
            Node currentNode = null;
            currentNode = openSet.Dequeue();

            if (currentNode == null)
            {
                Debug.LogWarning("No path found");
                return false;
            }

            currentNode.distanceToGoal = ManhattanDistance(currentNode.WorldPoint, map.EndNode.WorldPoint);
            if (currentNode.distanceToGoal < closestSoFar.distanceToGoal)
            {
                closestSoFar = currentNode;
            }

            if (currentNode == map.EndNode)
            {
                foundPath = ReconstructPath(cameFrom, map.EndNode);
                return true;
            }

            List<Node> neighbours = new List<Node>();
            Vector2Int currentNodePos = map.WorldPointToNode(currentNode.WorldPoint);

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
                        if (neighbour == null)
                        {
                            Node newNode = nodepool.Get();
                            map.AddNodeToMap(newNode, new Vector2Int(currentNodePos.x + x, currentNodePos.y + y), objectNeedingPath);
                            neighbour = newNode;
                        }
                        if (neighbour.walkable)
                        {
                            neighbours.Add(neighbour);
                        }
                    }
                }
            }

            foreach (Node neighbour in neighbours)
            {
                Vector2Int neighbourPos = map.WorldPointToNode(neighbour.WorldPoint);
                Vector2Int diff = currentNodePos - neighbourPos;

                int dx = Mathf.Abs(diff.x);
                int dy = Mathf.Abs(diff.y);

                float cost = (dx == 1 && dy == 1) ? 1.4f : 0.8f;
                float tentativeGscore = currentNode.gscore + cost;
                if (tentativeGscore < neighbour.gscore)
                {
                    neighbour.gscore = tentativeGscore;
                    neighbour.fscore = tentativeGscore + ManhattanDistance(neighbour.WorldPoint, map.EndNode.WorldPoint);
                    cameFrom[neighbour] = currentNode;
                    openSet.Enqueue(neighbour, neighbour.fscore);
                }
            }
        }
        Debug.LogWarning("No path found");
        Debug.LogWarning($"Cheapest score: {closestSoFar.fscore}, is start: {map.StartNode == closestSoFar}");
        foundPath = ReconstructPath(cameFrom, closestSoFar);
        return foundPath.Count != 0;
    }

    private static void OnDrawGizmos()
    {
        if (map?.Nodes == null)
        {
            return;
        }
        for (int i = 0; i < map.Nodes.GetLength(0); i++)       // rows
        {
            for (int j = 0; j < map.Nodes.GetLength(1); j++)   // columns
            {
                Node node = map.Nodes[i, j];
                if (node == null)
                {
                    continue;
                }
                // Do something with node
                if (map.StartNode == node)
                {
                    Gizmos.color = Color.green;
                }
                else if (map.EndNode == node)
                {
                    Gizmos.color = Color.yellow;
                }
                else if (!node.walkable)
                {
                    Gizmos.color = Color.red;
                }
                else if (node.isPath)
                {
                    Gizmos.color = Color.magenta;
                }
                else
                {
                    Gizmos.color = Color.cyan;
                }

                Vector3 center = new Vector3(
                    map.MapOrigin.x + ((i + 0.5f) - map.Nodes.GetLength(0) / 2f) * map.NodeSize,
                    map.MapOrigin.y + ((j + 0.5f) - map.Nodes.GetLength(1) / 2f) * map.NodeSize,
                    0
                );
                Gizmos.DrawCube(center, new Vector3(map.NodeSize, map.NodeSize, map.NodeSize));
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
            if (result.Count == test.expected.Count)
            {
                bool allMatch = true;
                for (int i = 0; i < result.Count; i++)
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
