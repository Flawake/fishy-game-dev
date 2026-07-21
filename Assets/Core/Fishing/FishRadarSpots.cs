using System.Collections.Generic;
using ItemSystem;
using Mirror;
using UnityEngine;

// Client-side fish radar. Lives on the same GameObject as FishSpots and, while
// the player has an active FishRadar effect, periodically asks the server for the
// subset of fish spots the radar is allowed to reveal, then draws them.
//
// This component keeps its own display list and never touches the authoritative
// spot data owned by FishSpots.
[RequireComponent(typeof(FishSpots))]
public class FishRadarSpots : NetworkBehaviour
{
    // How long to wait between radar poll requests, so we don't spam the server
    // in consecutive frames while waiting for a response.
    private const float PollCooldownSeconds = 5f;

    private FishSpots fishSpots;

    // Spots currently revealed by the radar. Client-side display only.
    private List<FishSpot> radarSpots = new List<FishSpot>();

    // Client-side timer controlling when the next poll may be sent.
    private float nextPollTime = 0f;

    private GameObject fishSpotPrefab;
    private List<GameObject> activeSpots = new List<GameObject>();

    private void Awake()
    {
        fishSpots = GetComponent<FishSpots>();
        fishSpotPrefab = Resources.Load<GameObject>("FishSpot");
    }

    private void Update()
    {
        if (!NetworkClient.active)
        {
            return;
        }

        Dictionary<SpecialEffectType, ActiveEffect> effects =
            NetworkClient.connection.identity.GetComponent<PlayerData>().GetActiveSpecialEffects();

        if (PlayerHasActiveRadar(effects, out int _))
        {
            if (nextPollTime < Time.time)
            {
                CmdGetFishingSpots();
                // Give the server time to respond before requesting again.
                nextPollTime = Time.time + PollCooldownSeconds;
            }
        }
        else
        {
            radarSpots.Clear();
            DestroyRadarSpots();
        }
    }

    private bool PlayerHasActiveRadar(Dictionary<SpecialEffectType, ActiveEffect> effects, out int radarStrength)
    {
        radarStrength = -1;
        foreach (KeyValuePair<SpecialEffectType, ActiveEffect> effect in effects)
        {
            if (effect.Key == SpecialEffectType.FishRadar)
            {
                radarStrength = (int)ItemRegistry.Get(effect.Value.ItemId).GetBehaviour<SpecialBehaviour>().EffectValue;
                return true;
            }
        }
        return false;
    }

    [Command(requiresAuthority = false)]
    void CmdGetFishingSpots(NetworkConnectionToClient conn = null)
    {
        if (PlayerHasActiveRadar(conn.identity.GetComponent<PlayerData>().GetActiveSpecialEffects(), out int radarStrength))
        {
            int nextGenerationDelay = fishSpots.GetSecondsUntilNextGeneration();
            switch (radarStrength)
            {
                case 1:
                    RpcGetFishingSpots(conn, fishSpots.GetSpotsByType(FishSpotType.Normal), FishSpotType.Normal, nextGenerationDelay);
                    break;
                case 2:
                    RpcGetFishingSpots(conn, fishSpots.GetSpotsByType(FishSpotType.Good), FishSpotType.Good, nextGenerationDelay);
                    break;
                case 3:
                    RpcGetFishingSpots(conn, fishSpots.GetSpotsByType(FishSpotType.Perfect), FishSpotType.Perfect, nextGenerationDelay);
                    break;
            }
        }
    }

    [TargetRpc]
    void RpcGetFishingSpots(NetworkConnectionToClient target, List<FishSpot> spots, FishSpotType spotType, int timeTillNextGeneration)
    {
        radarSpots = spots;
        nextPollTime = timeTillNextGeneration + Time.time;
        SpawnRadarSpots(spots, spotType);
    }

    private void DestroyRadarSpots()
    {
        foreach (GameObject spot in activeSpots)
        {
            if (spot != null)
            {
            Destroy(spot);
            }
        }

        activeSpots.Clear();
    }

    private void SpawnRadarSpots(List<FishSpot> spots, FishSpotType spotType)
    {
        DestroyRadarSpots();

        foreach(FishSpot spot in spots)
        {
            GameObject newSpot = Instantiate(fishSpotPrefab);
            newSpot.GetComponent<FishSpotRenderer>().Create(spot, spotType);
            activeSpots.Add(newSpot);
        }
    }

    private void OnDrawGizmos()
    {
        if (radarSpots == null)
        {
            return;
        }

        foreach (FishSpot spot in radarSpots)
        {
            switch (spot.spotType)
            {
                case FishSpotType.Normal:
                    Gizmos.color = new Color(1f, 1f, 0f, 120f / 255f);
                    break;
                case FishSpotType.Good:
                    Gizmos.color = new Color(0f, 1f, 0f, 120f / 255f);
                    break;
                case FishSpotType.Perfect:
                    Gizmos.color = new Color(0f, 1f, 1f, 120f / 255f);
                    break;
                default:
                    Gizmos.color = new Color(1f, 1f, 1f, 120f / 255f);
                    break;
            }
            Gizmos.DrawCube(spot.centrePoint, spot.size);
        }
    }
}
