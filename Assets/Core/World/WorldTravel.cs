using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldTravel : MonoBehaviour
{
    [SerializeField]
    Camera cam;
    [SerializeField]
    AudioListener audioListener;

    public enum CustomSpawnInstruction
    {
        None,
        WalkOusideBakery,
    }

    private void Awake()
    {
        if(NetworkServer.active || !NetworkClient.active)
        {
            return;
        }
        cam.enabled = true;
        audioListener.enabled = true;
        GameNetworkManager.SetEventSystemActive("WorldMap", true);
    }

    [Client]
    public static void ClientInstantiateTravelTo(AreaComponent destination)
    {
        ClientInstantiateTravelTo(destination.area, CustomSpawnInstruction.None);
    }
    
    [Client]
    public static void ClientInstantiateTravelTo(Area destination)
    {
        ClientInstantiateTravelTo(destination, CustomSpawnInstruction.None);
    }
    
    [Client]
    public static void ClientInstantiateTravelTo(Area destination, CustomSpawnInstruction requestInstruction)
    {
        if (!AreaUnlockManager.IsAreaUnlocked(destination, NetworkClient.connection.identity.GetComponent<PlayerData>()))
        {
            Debug.LogWarning("Area was not yet unlocked");
            return;
        }
        
        // Disable the event system before unloading async and loading a new map
        GameNetworkManager.SetEventSystemActive("WorldMap", false);

        // Unload worldmap if player decides to stay in current area
        if (SceneManager.GetSceneByName(destination.ToString()).isLoaded)
        {
            GameNetworkManager.SetEventSystemActive(destination.ToString(), true);
            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync("WorldMap");
            GameNetworkManager.scenesUnloading.Add(unloadOperation);
            return;
        }
        
        // Lock local movement during travel to avoid being outside of any world for a frame
        var playerController = NetworkClient.connection.identity.GetComponent<PlayerController>();
        playerController?.BeginTravelLock();

        MovePlayerMessage msg = new MovePlayerMessage()
        {
            requestedArea = destination,
            requestedSpawnInstruction = requestInstruction,
        };
        NetworkClient.Send(msg);
    }
}
