using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[AddComponentMenu("")]
public class GameNetworkManager : NetworkManager
{
    // Server-only cross-reference of connections to player names
    internal static readonly Dictionary<NetworkConnectionToClient, string> connNames = new Dictionary<NetworkConnectionToClient, string>();
    internal static readonly HashSet<string> playerNames = new HashSet<string>();

    [Scene]
    [Tooltip("Add all sub-scenes to this list")]
    public string[] subScenes;

    public static List<AsyncOperation> scenesUnloading = new List<AsyncOperation>();

    public override void Update()
    {
        base.Update();
        if(NetworkServer.active || scenesUnloading.Count == 0)
        {
            return;
        }

        for(int i = scenesUnloading.Count - 1; i >= 0; i--)
        {
            if (scenesUnloading[i].isDone)
            {
                scenesUnloading.RemoveAt(i);
            }
        }

        //Only enable camera and eventsystem when everything else has been loaded
        if(scenesUnloading.Count == 0)
        {
            SetEventSystemActive(networkSceneName, true);
            NetworkClient.connection.identity.transform.GetComponentInChildren<PlayerInfoUIManager>().ShowCanvas();
            NetworkClient.connection.identity.transform.GetComponentInChildren<AudioListener>().enabled = true;
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        // remove player name from the HashSet
        if (conn.authenticationData != null)
            playerNames.Remove((string)conn.authenticationData);

        // remove connection from Dictionary of conn > names
        connNames.Remove(conn);

        base.OnServerDisconnect(conn);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // load all subscenes on the server only
        StartCoroutine(LoadSubScenes());

        NetworkServer.RegisterHandler<CreateCharacterMessage>(OnBeginCreateCharacter);
        NetworkServer.RegisterHandler<MovePlayerMessage>(OnPlayerMoveMessage);
    }

    public override void OnClientConnect()
    {
        //TODO: set all other character values like clothes
        base.OnClientConnect();

        CreateCharacterMessage characterMessage = new CreateCharacterMessage();

        NetworkClient.Send(characterMessage);
    }

    public static void SetEventSystemActive(string sceneName, bool active)
    {
        //Enable the event system in the new scene
        Scene newScene = SceneManager.GetSceneByName(networkSceneName);
        if (!newScene.IsValid())
        {
            newScene = SceneManager.GetSceneByPath(networkSceneName);
        }
        GameObject[] objects = newScene.GetRootGameObjects();
        foreach (GameObject obj in objects)
        {
            if (obj.name == "EventSystem")
            {
                obj.SetActive(true);
            }
        }
    }

    [Client]
    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
    {
        base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);
        networkSceneName = newSceneName;
    }

    [Client]
    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();

        //Don't move player to the WorldMap or unload scenes.
        if (networkSceneName == null || networkSceneName == "WorldMap" || NetworkClient.connection.identity == null)
        {
            return;
        }
        SceneManager.MoveGameObjectToScene(NetworkClient.connection.identity.gameObject, SceneManager.GetSceneByName(networkSceneName));
        //Unload all scenes that are not the new scene
        foreach (string sceneName in subScenes)
        {
            Scene loadedScene = SceneManager.GetSceneByPath(sceneName);
            if (loadedScene.name == networkSceneName)
            {
                continue;
            }
            if (loadedScene.isLoaded)
            {
                AsyncOperation unloadingScene = SceneManager.UnloadSceneAsync(loadedScene);
                scenesUnloading.Add(unloadingScene);
            }
        }
    }

    [Server]
    /// Makes the player character ready and requests data from database
    void OnBeginCreateCharacter(NetworkConnectionToClient conn, CreateCharacterMessage _characterData)
    {
        GameObject player = Instantiate(playerPrefab);
        //Hard coded value
        player.transform.position = new Vector3(0, -10, 0);
        PlayerData dataPlayer = player.GetComponent<PlayerData>();
        if (!connNames.TryGetValue(conn, out string name) || dataPlayer == null)
        {
            //TODO: Is this the right way to disconnect?
            conn.Disconnect();
            return;
        }

        dataPlayer.SetUsername(name);
        dataPlayer.SetRandomColor();

        WWWForm getInventoryForm = new WWWForm();
        getInventoryForm.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        getInventoryForm.AddField("user", name);

        GameObject[] objectsArray = { player };

        WebRequestHandler.SendWebRequest(DatabaseEndpoints.getInventoryEndpoint, getInventoryForm, conn, objectsArray, OnEndCreateCharacter);
    }

    [Server]
    ///Spawns player in when all data from the database has been received
    void OnEndCreateCharacter(WebRequestHandler.ResponseMessageData data)
    {
        if (data.EndRequestReason == WebRequestHandler.RequestEndReason.timeout)
        {
            Debug.Log("Timeout");
            data.Connection.Disconnect();
        }
        if (data.Objects.Length < 1)
        {
            data.Connection.Disconnect();
            return;
        }
        PlayerData dataPlayer = data.Objects[0].GetComponent<PlayerData>();
        if (dataPlayer.ParsePlayerData(data.ResponseData)) {
            NetworkServer.AddPlayerForConnection(data.Connection, data.Objects[0]);
        }
        else
        {
            data.Connection.Disconnect();
        }
    }

    [Server]
    void OnPlayerMoveMessage(NetworkConnectionToClient conn, MovePlayerMessage data)
    {
        SceneManager.MoveGameObjectToScene(conn.identity.gameObject, SceneManager.GetSceneByName(data.requestedScene));
        conn.Send(new SceneMessage()
        {
            sceneName = data.requestedScene,
            sceneOperation = SceneOperation.LoadAdditive
        });
    }

    [Server]
    IEnumerator LoadSubScenes()
    {
        Debug.Log("Loading Scenes");

        foreach (string sceneName in subScenes)
        {
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }
    }
}

public struct CreateCharacterMessage : NetworkMessage
{
    //CAREFUL: the player can fill in the data of this struct. So don't add the player name and inventory here!!!
    //TODO: Add clothes that the player wants to wear, and check on the server if this is possible.
}

public struct MovePlayerMessage : NetworkMessage
{
    public string requestedScene;
}
