using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StartupSceneManager : MonoBehaviour
{
    [SerializeField]
    GameObject accountCanvas;
    [SerializeField]
    GameObject reconnectCanvas;
    [SerializeField]
    Authenticate authenticator;
    [SerializeField]
    GameObject errorMessageObject;
    [SerializeField]
    TMP_Text errorMessageField;
    public void ShowReconnectWindow()
    {
        reconnectCanvas.SetActive(true);
        accountCanvas.SetActive(false);
    }

    public void ShowErrortWindow(string errorMessage)
    {
        ShowReconnectWindow();
        errorMessageObject.SetActive(true);
        errorMessageField.text = errorMessage;
    }

    public void ShowAccountWindow()
    {
        reconnectCanvas.SetActive(false);
        accountCanvas.SetActive(true);
        authenticator.OnLoginInsteadClick();
    }

    public void DisconnectedReason(DisconnectMessage message)
    {
        switch (message.reason)
        {
            case ClientDisconnectReason.Unknown:
                ShowErrortWindow(message.reasonText);
                break;
            case ClientDisconnectReason.Timeout:
                ShowReconnectWindow();
                break;
            case ClientDisconnectReason.InvalidLoginCredentials:
                ShowAccountWindow();
                break;
            case ClientDisconnectReason.InvalidPlayerData:
                ShowErrortWindow(message.reasonText);
                break;
        }
    }
}
