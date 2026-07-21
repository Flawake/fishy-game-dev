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

    // Material applied to spawned fish spot sprites; its stencil test clips them
    // to the water mask so nothing draws over land.
    private Material spotMaterial;

    // Invisible mesh that writes the water shape into the stencil buffer.
    private GameObject waterMaskObject;

    private void Awake()
    {
        fishSpots = GetComponent<FishSpots>();
        fishSpotPrefab = Resources.Load<GameObject>("FishSpot");
        SetupWaterMask();
    }

    // Builds a stencil mask from the water collider so fish spot sprites render
    // pixel-perfect, clipped to the water's exact outline. The mask mesh writes a
    // stencil value wherever the water is; the spot material only draws where that
    // value is present.
    private void SetupWaterMask()
    {
        // No graphics device on a dedicated (headless) server, so there is
        // nothing to mask and no shaders to load.
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            return;
        }

        Collider2D water = fishSpots.WaterCollider;
        if (water == null)
        {
            return;
        }

        Shader maskShader = Resources.Load<Shader>("Shaders/WaterStencilMask");
        Shader spotShader = Resources.Load<Shader>("Shaders/FishSpotMasked");
        if (maskShader == null || spotShader == null)
        {
            Debug.LogWarning("FishSpot mask shaders not found under Resources/Shaders; spots will not be clipped.");
            return;
        }

        spotMaterial = new Material(spotShader);

        // World-space mesh of the water shape (body position + rotation baked in).
        Mesh waterMesh = water.CreateMesh(true, true);
        if (waterMesh == null)
        {
            return;
        }

        waterMaskObject = new GameObject("FishSpotWaterMask");
        // Match the fish spot layer so the same camera that draws the spots also
        // draws the mask (the stencil buffer is per-camera).
        waterMaskObject.layer = fishSpotPrefab != null ? fishSpotPrefab.layer : gameObject.layer;
        // Vertices are already in world space, so keep the transform at the origin.
        waterMaskObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        waterMaskObject.transform.localScale = Vector3.one;

        MeshFilter filter = waterMaskObject.AddComponent<MeshFilter>();
        filter.sharedMesh = waterMesh;

        MeshRenderer meshRenderer = waterMaskObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(maskShader);
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    private void OnDestroy()
    {
        if (waterMaskObject != null)
        {
            Destroy(waterMaskObject);
        }
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
            GameObject newSpot = Instantiate(fishSpotPrefab, transform);

            // Clip the sprite to the water outline via the stencil mask material,
            // so the parts of the spot that fall on land are not drawn.
            if (spotMaterial != null)
            {
                SpriteRenderer sr = newSpot.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sharedMaterial = spotMaterial;
                }
            }

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
