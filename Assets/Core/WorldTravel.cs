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
    public static void TravelTo(string destination)
    {
        //Disable the event system before unloading async and loading a new map
        GameNetworkManager.SetEventSystemActive("WorldMap", false);

        if (SceneManager.GetSceneByName(destination).isLoaded)
        {
            GameNetworkManager.SetEventSystemActive(destination, true);
            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync("WorldMap");
            GameNetworkManager.scenesUnloading.Add(unloadOperation);
            return;
        }

        MovePlayerMessage msg = new MovePlayerMessage()
        {
            requestedScene = destination
        };
        NetworkClient.Send(msg);
    }
}
