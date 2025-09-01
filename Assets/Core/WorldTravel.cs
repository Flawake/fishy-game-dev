using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldTravel : MonoBehaviour
{
    [SerializeField]
    Camera cam;
    [SerializeField]
    AudioListener audioListener;

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
    public static void TravelTo(AreaComponent destination)
    {
        TravelTo(destination.area);
    }
    
    [Client]
    public static void TravelTo(Area destination)
    {
        if (!AreaUnlockManager.IsAreaUnlocked(destination, NetworkClient.connection.identity.GetComponent<PlayerData>()))
        {
            Debug.LogWarning("Area was not yet unlocked");
            return;
        }
        //Disable the event system before unloading async and loading a new map
        GameNetworkManager.SetEventSystemActive("WorldMap", false);

        if (SceneManager.GetSceneByName(destination.ToString()).isLoaded)
        {
            GameNetworkManager.SetEventSystemActive(destination.ToString(), true);
            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync("WorldMap");
            GameNetworkManager.scenesUnloading.Add(unloadOperation);
            return;
        }

        MovePlayerMessage msg = new MovePlayerMessage()
        {
            requestedArea = destination,
        };
        NetworkClient.Send(msg);
    }
}
