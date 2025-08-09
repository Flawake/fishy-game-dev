using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using GlobalCompetitionSystem;
using Mirror.SimpleWeb;
using UnityEngine;
using UnityEngine.SceneManagement;

[AddComponentMenu("")]
public class GameNetworkManager : NetworkManager
{
    internal struct PlayerConnectionInfo
    {
        public Guid userID;
        public double playerConnectionTime;
    }
    // Server-only cross-reference of connections to player names
    internal static readonly Dictionary<NetworkConnectionToClient, string> connNames = new Dictionary<NetworkConnectionToClient, string>();
    internal static readonly DualDict<NetworkConnectionToClient, Guid> connUUID = new DualDict<NetworkConnectionToClient, Guid>();

    //netID and start time (time.time);
    internal static readonly Dictionary<int, PlayerConnectionInfo> connectedPlayersInfo = new Dictionary<int, PlayerConnectionInfo>();

    [Scene]
    [Tooltip("Add all sub-scenes to this list")]
    public string[] subScenes;

    public static List<AsyncOperation> scenesUnloading = new List<AsyncOperation>();

    public override void Awake()
    {
        EnvConfig.LoadEnv();
        if (transport is SimpleWebTransport swt)
        {
#if UNITY_EDITOR
            swt.clientUseWss = false;
            swt.clientWebsocketSettings.ClientPortOption = WebsocketPortOption.DefaultSameAsServer;
#endif
            swt.port = EnvConfig.Port;
            if (swt.clientWebsocketSettings.ClientPortOption == WebsocketPortOption.SpecifyPort)
            {
                swt.clientWebsocketSettings.CustomClientPort = EnvConfig.ClientPort;
            }
        }
        else
        {
            Debug.LogWarning("Environment variables port could not be set");
        }
        
        base.Awake();
        
        StartCoroutine(CompetitionManager.UpdateCompetitions());
    }

    public override void Update()
    {
        base.Update();
        if (NetworkServer.active || scenesUnloading.Count == 0)
        {
            return;
        }

        for (int i = scenesUnloading.Count - 1; i >= 0; i--)
        {
            if (scenesUnloading[i].isDone)
            {
                scenesUnloading.RemoveAt(i);
            }
        }

        //Only enable camera and eventsystem when everything else has been loaded
        if (scenesUnloading.Count == 0)
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
            DatabaseCommunications.AddPlaytime((int)(NetworkTime.time - playerInfo.playerConnectionTime), playerInfo.userID);
        }
        else
        {
            Debug.LogWarning($"playerStartTime was not foud for a player, connid {conn.connectionId}");
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
        conn.authenticationData = new PlayerAuthData
        {
            playerObject = player,
            playerData = null,
        };

        DatabaseCommunications.RetrievePlayerData(uuid, conn, PlayerDataReceived);
    }

    [Server]
    void PlayerDataReceived(WebRequestHandler.ResponseMessageData data)
    {
        if (data.EndRequestReason != WebRequestHandler.RequestEndReason.success)
        {
            data.Connection.Disconnect();
        }
        PlayerAuthData authData = data.Connection.authenticationData as PlayerAuthData;
        authData.playerData = data;
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
        GameObject playerObject = (conn.authenticationData as PlayerAuthData).playerObject;
        PlayerData dataPlayer = playerObject.GetComponent<PlayerData>();
        
        if (GameNetworkManager.connUUID.TryGetValue(conn, out Guid uuid) && uuid != Guid.Empty)
        {
            if(dataPlayer.ParsePlayerData(playerData.ResponseData, uuid))
            {
                NetworkServer.AddPlayerForConnection(conn, playerObject);
                PlayerConnectionInfo playerConnection = new PlayerConnectionInfo
                {
                    userID = dataPlayer.GetUuid(),
                    playerConnectionTime = NetworkTime.time,
                };
                connectedPlayersInfo.Add(conn.connectionId, playerConnection);
            }
            else
            {
                StartCoroutine(DelayedDisconnect(conn, 1f));
            }
        }
        else
        {
            StartCoroutine(DelayedDisconnect(conn, 1f));
        }
    }

    [Server]
    void OnPlayerMoveMessage(NetworkConnectionToClient conn, MovePlayerMessage data)
    {
        if (!AreaUnlockManager.IsAreaUnlocked(data.requestedArea, conn.identity.GetComponent<PlayerData>()))
        {
            KickPlayerForCheating(conn, "Tried to enter an area which was not unlocked yet");
            return;
        }
        SceneManager.MoveGameObjectToScene(conn.identity.gameObject, SceneManager.GetSceneByName(data.requestedArea.ToString()));
        if (data.requestedArea.ToString() != null && data.requestedArea != Area.WorldMap && conn.identity != null)
        {
            conn.identity.gameObject.GetComponentInChildren<PlayerController>().ServerTeleportPlayer(
                spawnPoint.GetRandomSpawnPoint(data.requestedArea.ToString())
                );
        }
        conn.Send(new SceneMessage()
        {
            sceneName = data.requestedArea.ToString(),
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
    private IEnumerator DelayedDisconnect(NetworkConnection conn, float delay)
    {
        conn.Send(new DisconnectMessage {
            reason = ClientDisconnectReason.InvalidPlayerData,
            reasonText = "Could not parse your data.",
        });
        yield return new WaitForSeconds(delay);
        conn.Disconnect();
    }

    [Server]
    public static void KickPlayerForCheating(NetworkConnectionToClient conn, string reason)
    {
        Debug.Log($"Kickreason: {reason}");
        conn.Send(new DisconnectMessage
        {
            reason = ClientDisconnectReason.Cheating,
            reasonText = $"You have been kicked for cheating: {reason}",
        });
        if (NetworkManager.singleton is GameNetworkManager manager)
        {
            manager.StartCoroutine(DelayedKick(conn, 3f));
        }
    }

    [Server]
    private static IEnumerator DelayedKick(NetworkConnectionToClient conn, float delay)
    {
        yield return new WaitForSeconds(delay);
        conn.Disconnect();
    }
}

//Data of the player used when authenticating
public class PlayerAuthData
{
    public GameObject playerObject;
    public WebRequestHandler.ResponseMessageData? playerData;

    public bool IsDataComplete()
    {
        if (playerData != null)
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
    public Area requestedArea;
}
