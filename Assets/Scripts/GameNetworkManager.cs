using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[AddComponentMenu("")]
public class GameNetworkManager : NetworkManager
{
    internal struct PlayerConnectionInfo
    {
        public string uuid;
        public double playerConnectionTime;
    }
    // Server-only cross-reference of connections to player names
    internal static readonly Dictionary<NetworkConnectionToClient, string> connNames = new Dictionary<NetworkConnectionToClient, string>();
    internal static readonly DualDict<NetworkConnectionToClient, Guid> connUUID = new DualDict<NetworkConnectionToClient, Guid>();
    internal static readonly HashSet<string> playerNames = new HashSet<string>();

    //netID and start time (time.time);
    internal static readonly Dictionary<int, PlayerConnectionInfo> connectedPlayersInfo = new Dictionary<int, PlayerConnectionInfo>();

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

    [Server]
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        AddPlaytimeToDatabase(conn);
        // remove player name from the HashSet
        if (conn.authenticationData != null)
            playerNames.Remove((string)conn.authenticationData);

        // remove connection from Dictionary of conn > names
        connNames.Remove(conn);
        connUUID.Remove(conn);
        connectedPlayersInfo.Remove(conn.connectionId);

        conn.identity.GetComponent<PlayerData>();

        base.OnServerDisconnect(conn);
    }

    [Server]
    void AddPlaytimeToDatabase(NetworkConnectionToClient conn)
    {
        if (connectedPlayersInfo.TryGetValue(conn.connectionId, out PlayerConnectionInfo playerInfo))
        {
            DatabaseCommunications.AddPlaytime((int)(NetworkTime.time - playerInfo.playerConnectionTime), playerInfo.uuid);
        }
        else
        {
            Debug.LogWarning($"playerStartTime was not foud for a player {conn.connectionId}");
        }
    }

    [Server]
    public override void OnStartServer()
    {
        base.OnStartServer();

        // load all subscenes on the server only
        StartCoroutine(LoadSubScenes());

        NetworkServer.RegisterHandler<CreateCharacterMessage>(OnBeginCreateCharacter);
        NetworkServer.RegisterHandler<MovePlayerMessage>(OnPlayerMoveMessage);
    }

    [Client]
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
        conn.authenticationData = new PlayerAuthData
        {
            playerData = null,
            playerMails = null,
        };
        GameObject player = Instantiate(playerPrefab);
        //Hard coded value
        player.transform.position = new Vector3(0, 0, 0);
        PlayerData dataPlayer = player.GetComponent<PlayerData>();
        if (!connNames.TryGetValue(conn, out string name) || dataPlayer == null)
        {
            //TODO: Is this the right way to disconnect?
            conn.Disconnect();
            return;
        }
        if (!connUUID.TryGetValue(conn, out Guid uuid) || dataPlayer == null)
        {
            //TODO: Is this the right way to disconnect?
            conn.Disconnect();
            return;
        }

        dataPlayer.SetUsername(name);
        dataPlayer.SetRandomColor();

        WWWForm getInventoryForm = new WWWForm();
        getInventoryForm.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        getInventoryForm.AddField("uuid", uuid.ToString());
        
        WWWForm getMailsForm = new WWWForm();
        getMailsForm.AddField("auth_token", DatabaseEndpoints.databaseAccessToken);
        getMailsForm.AddField("uuid", uuid.ToString());

        GameObject[] objectsArray = { player };

        WebRequestHandler.SendWebRequest(DatabaseEndpoints.getInventoryEndpoint, getInventoryForm, conn, objectsArray, PlayerDataReceived);
        WebRequestHandler.SendWebRequest(DatabaseEndpoints.retreiveMailsEndpoint, getInventoryForm, conn, objectsArray, PlayerMailsReveived);
    }

    [Server]
    void PlayerDataReceived(WebRequestHandler.ResponseMessageData data)
    {
        PlayerAuthData authData = data.Connection.authenticationData as PlayerAuthData;
        authData.playerData = data;
        if (authData.IsDataComplete())
        {
            OnEndCreateCharacter(data.Connection);
        }
    }

    [Server]
    void PlayerMailsReveived(WebRequestHandler.ResponseMessageData data)
    {
        PlayerAuthData authData = data.Connection.authenticationData as PlayerAuthData;
        authData.playerMails = data;
        if (authData.IsDataComplete())
        {
            OnEndCreateCharacter(data.Connection);
        }
    }

    [Server]
    ///Spawns player in when all data from the database has been received
    void OnEndCreateCharacter(NetworkConnectionToClient conn)
    {
        WebRequestHandler.ResponseMessageData playerData = (conn.authenticationData as PlayerAuthData).playerData.Value;
        WebRequestHandler.ResponseMessageData mailData = (conn.authenticationData as PlayerAuthData).playerMails.Value;
        PlayerData dataPlayer = playerData.Objects[0].GetComponent<PlayerData>();
        MailSystem playerMails = mailData.Objects[0].GetComponent<MailSystem>();
        if (
            dataPlayer.ParsePlayerData(playerData.ResponseData) &&
            playerMails.ParseMails(mailData.ResponseData)) 
        {
            NetworkServer.AddPlayerForConnection(conn, playerData.Objects[0]);
            PlayerConnectionInfo playerConnection = new PlayerConnectionInfo
            {
                uuid = dataPlayer.GetUuidAsString(),
                playerConnectionTime = NetworkTime.time,
            };
            connectedPlayersInfo.Add(conn.connectionId, playerConnection);
        }
        else
        {
            conn.Send(new DisconnectMessage {
                reason = ClientDisconnectReason.InvalidPlayerData,
                reasonText = "Inventory data was invalid, please reconnect to the game.",
            });
            StartCoroutine(DelayedDisconnect(conn, 1f));
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

    [Server]
    private IEnumerator DelayedDisconnect(NetworkConnection connection, float delay)
    {
        yield return new WaitForSeconds(delay);
        connection.Disconnect();
    }
}

//Data of the player used when authenticating
public class PlayerAuthData
{
    public WebRequestHandler.ResponseMessageData? playerData;
    public WebRequestHandler.ResponseMessageData? playerMails;

    public bool IsDataComplete()
    {
        if (playerData != null && playerMails != null)
        {
            return true;
        }

        return false;
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
