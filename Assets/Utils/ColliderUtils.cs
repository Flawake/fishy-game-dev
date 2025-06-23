using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

public static class ColliderUtils
{
    public static List<List<Vector2>> GetColliderPoints(CompositeCollider2D collider)
    {
        List<List<Vector2>> paths = new List<List<Vector2>>();
        int pathCount = collider.pathCount;
        for (int i = 0; i < pathCount; i++)
        {
            List<Vector2> points = new List<Vector2>();
            collider.GetPath(i, points);
            paths.Add(points);
        }
        return paths;
    }

    public static (NativeList<Vector2>, NativeList<int>) GetColliderPointsAsNativeLists(CompositeCollider2D collider, Allocator allocator)
    {
        List<List<Vector2>> listPaths = GetColliderPoints(collider);
        var polygonPaths = new NativeList<Vector2>(collider.pointCount, Allocator.TempJob);
        var polygonPoints = new NativeList<int>(collider.pathCount, Allocator.TempJob)
        {
            0
        };

        foreach (var path in listPaths)
        {
            foreach (var point in path)
            {
                polygonPaths.Add(point);
            }
            polygonPoints.Add(polygonPaths.Length);
        }

        return (polygonPaths, polygonPoints);
    }

    /// <summary>
    /// Returns the signed area (twice) of triangle (a, b, p):
    /// >0 if p is left of a→b, <0 if right, =0 if collinear.
    /// </summary>
    private static float SignedArea(Vector2 a, Vector2 b, Vector2 p)
    {
        float abx = b.x - a.x;
        float aby = b.y - a.y;
        float apx = p.x - a.x;
        float apy = p.y - a.y;
        return (abx * apy) - (apx * aby);
    }

    /// <summary>
    /// True if the horizontal ray from point at y crosses edge a→b going upward.
    /// </summary>
    private static bool IsUpwardCrossing(Vector2 a, Vector2 b, Vector2 point)
    {
        if (a.y <= point.y && b.y > point.y)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// True if the horizontal ray from point at y crosses edge a→b going downward.
    /// </summary>
    private static bool IsDownwardCrossing(Vector2 a, Vector2 b, Vector2 point)
    {
        if (a.y > point.y && b.y <= point.y)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Computes the winding number of a closed polygon around a point.
    /// Winding number ≠ 0 means the point is inside.
    /// </summary>
    public static int CalculateWindingNumber(NativeSlice<Vector2> polygon, Vector2 point)
    {
        int windingNumber = 0;
        int count = polygon.Length;

        for (int i = 0; i < count; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % count];

            if (IsUpwardCrossing(a, b, point))
            {
                // upward crossing: if point is left of edge, count it
                if (SignedArea(a, b, point) > 0)
                {
                    windingNumber++;
                }
            }
            else if (IsDownwardCrossing(a, b, point))
            {
                // downward crossing: if point is right of edge, count it
                if (SignedArea(a, b, point) < 0)
                {
                    windingNumber--;
                }
            }
        }

      return windingNumber;
    }

    static bool IsPointInsidePolygon(NativeSlice<Vector2> polygon, Vector2 point)
    {
        return CalculateWindingNumber(polygon, point) != 0;
    }

    static bool PointOverlapArea(Vector2 pointA, Vector2 pointB, Vector2 point)
    {
        if (point.x > pointA.x && point.x < pointB.x &&
            point.y < pointA.y && point.y > pointB.y)
        {
            return true;
        }
        return false;
    }

    public static bool PointInsidePaths(NativeList<Vector2> polygonsPoints, NativeList<int> polygonPaths, Vector2 point)
    {
        int total = 0;
        for (int i = 0; i < polygonPaths.Length - 1; i++)
        {
            NativeArray<Vector2> polygonPointsArr = polygonsPoints.AsArray();
            int index = i == 0 ? polygonPaths[i] + 1 : 0;
            if (IsPointInsidePolygon(polygonPointsArr.Slice(index, polygonPaths[i + 1]), point))
            {
                total++;
            }

            polygonPointsArr.Dispose();
        }
        return total % 2 == 1;
    }

    /// <summary>
    /// Check if the area is inside the polygon or partially overlaps
    /// </summary>
    /// <returns></returns>
    public static bool AreaOverlapPolygon(Vector2 pointA, Vector2 pointB, NativeList<Vector2> polygonsPoints, NativeList<int> polygonPaths)
    {
        Vector2[] points = 
        {
            new Vector2(pointA.x, pointB.x),
            new Vector2(pointA.x, pointB.y),
            new Vector2(pointA.y, pointB.x),
            new Vector2(pointA.y, pointB.y),
        };

        foreach (Vector2 point in polygonsPoints)
        {
            if (PointOverlapArea(pointA, pointB, point))
            {
                return true;
            }
        }

        foreach (Vector2 point in points)
        {
            if (PointInsidePaths(polygonsPoints, polygonPaths, point))
            {
                return true;
            }
        }

        return false;
    }
}
