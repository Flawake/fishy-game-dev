using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

enum FishSpotType
{
    Uninitialized,
    Bad,
    Normal,
    Good,
    Perfect
}

class FishSpot
{
    public Vector2 centrePoint;
    public Vector2 size;
    public FishSpotType spotType;

    // Helper method to check if a point is inside this FishSpot
    public bool Contains(Vector2 point)
    {
        Vector2 halfSize = size / 2f;
        Vector2 min = centrePoint - halfSize;
        Vector2 max = centrePoint + halfSize;

        return point.x >= min.x && point.x <= max.x &&
               point.y >= min.y && point.y <= max.y;
    }
}

struct Grid
{
    public Vector2 BottomLeft;
    public Vector2 UpperRight;
    public float GridSize;
}


[RequireComponent(typeof(FishRadarSpots))]
public class FishSpots : NetworkBehaviour
{
    [SerializeField] private CompositeCollider2D coll;
    [SerializeField] private Collider2D waterCollider;
    private List<FishSpot> fishSpots = new List<FishSpot>();
    static float gridSize = 3f;
    Grid areaGrid;

    private float lastGeneratedTime = float.MinValue;
    readonly private float secondsBetweenGenerations = 10 * 60;

    FishSpot _spot = null;

    private void Awake()
    {
        areaGrid.BottomLeft = coll.bounds.min;
        areaGrid.UpperRight = coll.bounds.max;
        areaGrid.GridSize = gridSize;


        for (float x = areaGrid.BottomLeft.x; x < areaGrid.UpperRight.x; x += areaGrid.GridSize)
        {
            for (float y = areaGrid.BottomLeft.y; y < areaGrid.UpperRight.y; y += areaGrid.GridSize)
            {
                Vector2 cellBottomLeft = new Vector2(x, y);
                Vector2 cellTopRight = cellBottomLeft + new Vector2(gridSize, gridSize);

                Collider2D[] hits = Physics2D.OverlapAreaAll(cellBottomLeft, cellTopRight);
                foreach (Collider2D hit in hits)
                {
                    if (hit.gameObject == waterCollider.gameObject)
                    {
                        fishSpots.Add(new FishSpot
                        {
                            size = new Vector2(areaGrid.GridSize, areaGrid.GridSize),
                            centrePoint = new Vector2(x + areaGrid.GridSize / 2, y + areaGrid.GridSize / 2),
                            spotType = FishSpotType.Uninitialized,
                        });
                    }
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        for (float x = areaGrid.BottomLeft.x; x < areaGrid.UpperRight.x; x += areaGrid.GridSize)
        {
            for (float y = areaGrid.BottomLeft.y; y < areaGrid.UpperRight.y; y += areaGrid.GridSize)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(
                    new Vector3(x + areaGrid.GridSize / 2, y + areaGrid.GridSize / 2, 0),
                    new Vector3(areaGrid.GridSize, areaGrid.GridSize, 1)
                );
            }
        }

        foreach (FishSpot spot in fishSpots)
        {
            if (spot.spotType == FishSpotType.Uninitialized)
            {
                Gizmos.color = new Color(0f, 0f, 0f, 120f / 255f);
            }
            if (spot.spotType == FishSpotType.Bad)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 120f / 255f);
            }
            if (spot.spotType == FishSpotType.Normal)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 120f / 255f);
            }
            if (spot.spotType == FishSpotType.Good)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 120f / 255f);
            }
            if (spot.spotType == FishSpotType.Perfect)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 120f / 255f);
            }
            Gizmos.DrawCube(spot.centrePoint, spot.size);
        }

        if (_spot != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(_spot.centrePoint, 1f);
        }
    }

    private void Update()
    {
        if (isServer)
        {
            if (lastGeneratedTime + secondsBetweenGenerations < Time.time)
            {
                lastGeneratedTime = Time.time;
                GenerateFishSpots();
            }
        }
    }

    [Server]
    private void GenerateFishSpots()
    {
        List<int> toDetermineFishSpots = Enumerable.Range(0, fishSpots.Count).ToList();

        // 15% perfect spots
        int perfectLeft = (int)((float)fishSpots.Count / 100 * 15);
        for (int i = 0; i < perfectLeft; i++)
        {
            int listSpotIndex = Random.Range(0, toDetermineFishSpots.Count);
            int fishSpotIndex = toDetermineFishSpots[listSpotIndex];
            toDetermineFishSpots.RemoveAt(listSpotIndex);
            fishSpots[fishSpotIndex] = new FishSpot()
            {
                size = fishSpots[fishSpotIndex].size,
                centrePoint = fishSpots[fishSpotIndex].centrePoint,
                spotType = FishSpotType.Perfect,
            };
        }

        // 15% good spots
        int goodLeft = (int)((float)fishSpots.Count / 100 * 15);
        for (int i = 0; i < goodLeft; i++)
        {
            int listSpotIndex = Random.Range(0, toDetermineFishSpots.Count);
            int fishSpotIndex = toDetermineFishSpots[listSpotIndex];
            toDetermineFishSpots.RemoveAt(listSpotIndex);
            fishSpots[fishSpotIndex] = new FishSpot()
            {
                size = fishSpots[fishSpotIndex].size,
                centrePoint = fishSpots[fishSpotIndex].centrePoint,
                spotType = FishSpotType.Good,
            };
        }
        // 30% normal spots
        int normalLeft = (int)((float)fishSpots.Count / 100 * 30);
        for (int i = 0; i < normalLeft; i++)
        {
            int listSpotIndex = Random.Range(0, toDetermineFishSpots.Count);
            int fishSpotIndex = toDetermineFishSpots[listSpotIndex];
            toDetermineFishSpots.RemoveAt(listSpotIndex);
            fishSpots[fishSpotIndex] = new FishSpot()
            {
                size = fishSpots[fishSpotIndex].size,
                centrePoint = fishSpots[fishSpotIndex].centrePoint,
                spotType = FishSpotType.Normal,
            };
        }

        // rest bad spots
        foreach (int leftFishSpot in toDetermineFishSpots)
        {
            fishSpots[leftFishSpot] = new FishSpot()
            {
                size = fishSpots[leftFishSpot].size,
                centrePoint = fishSpots[leftFishSpot].centrePoint,
                spotType = FishSpotType.Bad,
            };
        }
    }

    public bool ShouldGeneratefish(Vector2 throwPosition)
    {
        foreach (FishSpot spot in fishSpots)
        {
            if (spot.Contains(throwPosition))
            {
                _spot = spot;
                switch (spot.spotType)
                {
                    case FishSpotType.Uninitialized:
                        return true;
                    case FishSpotType.Bad:
                        return Random.Range(0, 10) >= 8;
                    case FishSpotType.Normal:
                        return Random.Range(0, 10) >= 5;
                    case FishSpotType.Good:
                        return Random.Range(0, 10) >= 3;
                    case FishSpotType.Perfect:
                        return true;
                }
            }
        }
        return false;
    }

    // Returns the authoritative spots matching the given type. Server-side data.
    internal List<FishSpot> GetSpotsByType(FishSpotType type)
    {
        return fishSpots.Where(spot => spot.spotType == type).ToList();
    }

    // Seconds until the next server-side spot generation.
    internal int GetSecondsUntilNextGeneration()
    {
        return (int)(lastGeneratedTime + secondsBetweenGenerations - Time.time);
    }
}
