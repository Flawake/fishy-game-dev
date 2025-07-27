using System.Collections;
using UnityEngine;
using Mirror;

public enum ClientDisconnectReason
{
    Unknown,
    Timeout,
    InvalidLoginCredentials,
    InvalidPlayerData,
    Cheating,
};

public struct DisconnectMessage : NetworkMessage
{
    public ClientDisconnectReason reason;
    public string reasonText;
}

public class ClientDisconnectManager : MonoBehaviour
{
    private void OnEnable()
    {
        NetworkClient.RegisterHandler<DisconnectMessage>(OnDisconnectMessage);
    }

    private void OnDisable()
    {
        NetworkClient.UnregisterHandler<DisconnectMessage>();
    }

    private void OnDisconnectMessage(DisconnectMessage message)
    {
        Debug.Log($"Disconnected from server: {message.reason}");
        StartCoroutine(TryShowDisconnectReason(message));
    }

    private IEnumerator TryShowDisconnectReason(DisconnectMessage message)
    {
        //Keep trying, the scene containing the object has not yet loaded.
        while (true) {
            GameObject scenemanager = GameObject.Find("StartupSceneManager");
            if (scenemanager == null)
            {
                yield return new WaitForSeconds(0.05f);
                continue;
            }
            StartupSceneManager startupSceneManager = scenemanager.GetComponent<StartupSceneManager>();
            foreach (Component c in scenemanager.GetComponents<Component>()) {
                Debug.Log(c);
            }
            if (startupSceneManager == null)
            {
                yield return new WaitForSeconds(0.05f);
                continue;
            }
            startupSceneManager.DisconnectedReason(message);
            break;
        }
    }
}
