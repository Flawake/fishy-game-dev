using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles all world travel logic: scene loading, player movement, spawn positioning, and arrival animations.
/// Separated from GameNetworkManager to keep networking and game logic separate.
/// </summary>
public class WorldTravelManager : MonoBehaviour
{
    private static WorldTravelManager instance;
    public static WorldTravelManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<WorldTravelManager>();
            }
            return instance;
        }
    }

    // Client-side: Store pending arrival instructions until scene is loaded
    private static Dictionary<string, ArrivalInstructionMessage> pendingArrivals = new Dictionary<string, ArrivalInstructionMessage>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    [Server]
    public void HandlePlayerMoveRequest(NetworkConnectionToClient conn, Area targetArea, WorldTravel.CustomSpawnInstruction approvedInstruction)
    {
        StartCoroutine(ServerHandlePlayerMoveToArea(conn, targetArea, approvedInstruction));
    }

    [Server]
    IEnumerator ServerHandlePlayerMoveToArea(NetworkConnectionToClient conn, Area targetArea, WorldTravel.CustomSpawnInstruction approvedInstruction)
    {
        string sceneName = targetArea.ToString();
        Scene targetScene = SceneManager.GetSceneByName(sceneName);

        // Step 1: Load new scene (if not already loaded)
        if (!targetScene.isLoaded)
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            yield return loadOperation;
            targetScene = SceneManager.GetSceneByName(sceneName);
        }

        // Wait until scene is fully loaded and valid
        while (!targetScene.isLoaded || !targetScene.IsValid())
        {
            yield return null;
            targetScene = SceneManager.GetSceneByName(sceneName);
        }

        // Step 2: Move player to new scene
        SceneManager.MoveGameObjectToScene(conn.identity.gameObject, targetScene);

        // Step 3: Calculate spawn position
        Vector3? spawnPosition = CalculateSpawnPosition(targetArea, approvedInstruction, sceneName);

        // Step 4: Send scene load message to client
        conn.Send(new SceneMessage()
        {
            sceneName = sceneName,
            sceneOperation = SceneOperation.LoadAdditive
        });

        // Step 5: Send arrival instruction with spawn position
        conn.Send(new ArrivalInstructionMessage
        {
            area = targetArea,
            instruction = approvedInstruction,
            spawnPosition = spawnPosition.HasValue ? spawnPosition.Value : Vector3.zero,
            hasSpawnPosition = spawnPosition.HasValue
        });

        // Step 6: Teleport player on server (for sync with other players)
        if (spawnPosition.HasValue && conn.identity != null)
        {
            // The local player's sync will be handled by the worldTravelManager
            conn.identity.gameObject.GetComponentInChildren<PlayerController>().ServerTeleportPlayer(spawnPosition.Value, false);
        }
    }

    [Server]
    private Vector3? CalculateSpawnPosition(Area targetArea, WorldTravel.CustomSpawnInstruction approvedInstruction, string sceneName)
    {
        if (targetArea == Area.WorldMap)
        {
            return null;
        }

        // Try custom spawn point first
        Vector3? customSpawn = SpawnPointProvider.TryGetCustomSpawnPoint(targetArea, approvedInstruction);
        if (customSpawn.HasValue)
        {
            return customSpawn.Value;
        }

        // Fall back to random spawn point
        return spawnPoint.GetRandomSpawnPoint(sceneName);
    }

    [Client]
    public void OnArrivalInstructionReceived(ArrivalInstructionMessage msg)
    {
        // Store arrival instructions to be processed when scene is loaded
        // This prevents race conditions between SceneMessage and ArrivalInstructionMessage
        string sceneName = msg.area.ToString();
        pendingArrivals[sceneName] = msg;

        // If scene is already loaded, process immediately
        Scene targetScene = SceneManager.GetSceneByName(sceneName);
        if (targetScene.isLoaded && targetScene.IsValid() && NetworkClient.connection?.identity != null)
        {
            // Scene is already loaded, process arrival instructions now
            StartCoroutine(ApplyArrivalInstructions(NetworkClient.connection.identity.gameObject, msg));
            pendingArrivals.Remove(sceneName);
        }
    }

    [Client]
    public IEnumerator HandleClientSceneChange(string newSceneName)
    {
        // Step 1: Load new scene - wait until fully loaded and valid
        Scene targetScene = SceneManager.GetSceneByName(newSceneName);
        while (!targetScene.isLoaded || !targetScene.IsValid())
        {
            yield return null;
            targetScene = SceneManager.GetSceneByName(newSceneName);
        }

        // Additional wait to ensure scene root objects are initialized
        // This ensures world bounds and other objects are available
        yield return null;

        // Step 2: Check for pending arrival instructions and apply them
        if (pendingArrivals.TryGetValue(newSceneName, out ArrivalInstructionMessage arrivalMsg))
        {
            pendingArrivals.Remove(newSceneName);
            yield return StartCoroutine(ApplyArrivalInstructions(NetworkClient.connection.identity.gameObject, arrivalMsg));
        }

        // Step 3: Unload old scene
        yield return StartCoroutine(UnloadOldScenes(newSceneName));
    }

    [Client]
    private IEnumerator UnloadOldScenes(string newSceneName)
    {
        // Wait until player is actually in the new scene
        if (NetworkClient.connection?.identity != null)
        {
            Scene targetScene = SceneManager.GetSceneByName(newSceneName);
            while (NetworkClient.connection.identity.gameObject.scene != targetScene)
            {
                yield return null;
            }
        }

        GameNetworkManager manager = NetworkManager.singleton as GameNetworkManager;
        if (manager == null || manager.subScenes == null) yield break;

        // Unload all scenes that aren't the new one
        foreach (string sceneName in manager.subScenes)
        {
            Scene loadedScene = SceneManager.GetSceneByPath(sceneName);
            if (!loadedScene.IsValid() || loadedScene.name == newSceneName)
            {
                continue;
            }
            if (loadedScene.isLoaded)
            {
                AsyncOperation unloadingScene = SceneManager.UnloadSceneAsync(loadedScene);
                GameNetworkManager.scenesUnloading.Add(unloadingScene);
            }
        }
    }

    [Client]
    IEnumerator ApplyArrivalInstructions(GameObject player, ArrivalInstructionMessage msg)
    {
        // Wait until scene is fully loaded
        Scene targetScene = SceneManager.GetSceneByName(msg.area.ToString());
        while (!targetScene.isLoaded)
        {
            yield return null;
        }

        // Teleport player (scene is loaded, world bounds are available)
        if (msg.hasSpawnPosition && player != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // Set position directly on client (server already set it for sync with other players)
                SceneManager.MoveGameObjectToScene(NetworkClient.connection.identity.gameObject, targetScene);
                playerController.ClientSetPosition(msg.spawnPosition);
                
                // Force camera clamp update to use the new scene's world bounds
                if (playerController.isLocalPlayer)
                {
                    playerController.ForceClampCamera();
                }
            }
        }

        // Start arrival animation
        ArrivalAnimationRunner runner = player.GetComponent<ArrivalAnimationRunner>();
        if (runner != null)
        {
            runner.StartArrivalAnimation(msg.instruction, msg.area);
        }

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.ChangeCameraZoom(AreaCameraZoomManager.GetCameraZoomPercentage(msg.area));

            if (msg.instruction == WorldTravel.CustomSpawnInstruction.None)
            {
                pc.EndTravelLock();
            }
        }
    }
}

