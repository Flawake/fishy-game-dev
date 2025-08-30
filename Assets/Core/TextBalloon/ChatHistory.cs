using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ChatHistory : MonoBehaviour
{
    [SerializeField]
    TMP_Text textUI;
    [SerializeField]
    TMP_InputField chatInput;
    [SerializeField]
    PlayerInfoUIManager UIManager;

    PlayerControls playerControls;

    public void OpenChat(InputAction.CallbackContext context)
    {
        if(chatInput.isFocused)
        {
            return;
        }
        chatInput.ActivateInputField();
        chatInput.Select();
    }

    public void SendChat(InputAction.CallbackContext context)
    {
        DeselectSendChatField();
        UIManager.SendChat();
    }

    void DeselectSendChatField()
    {
        //TODO: Also deselect textField on escape
        //TODO: check if textfield is selected, we're currently justs deselecting whatever was selected.
        GameObject eventSystem = GameObject.Find("EventSystem");
        eventSystem.GetComponent<UnityEngine.EventSystems.EventSystem>().SetSelectedGameObject(null);
    }

    public void AddChatHistory(string text, string playerName, string playerColor)
    {
        textUI.text = textUI.text + $"<color={playerColor}>" + ChatBalloon.SanitizeTMPString(playerName) + ": " + "</color>" + "<color=black>" + ChatBalloon.SanitizeTMPString(text) + "</color>" + "\n\r";
    }

    public void Start()
    {
        playerControls = new PlayerControls();

        playerControls.Player.OpenChat.performed += OpenChat;
        playerControls.Player.OpenChat.Enable();

        playerControls.Player.SendChat.performed += SendChat;
        playerControls.Player.SendChat.Enable();
        chatInput.onSelect.AddListener(_ =>
        {
            NetworkClient.connection.identity.GetComponent<PlayerController>().IncreaseObjectsPreventingMovement();
        });
        chatInput.onDeselect.AddListener(_ =>
        {
            NetworkClient.connection.identity.GetComponent<PlayerController>().DecreaseObjectsPreventingMovement();
        });
    }

    private void OnDisable()
    {
        playerControls.Player.OpenChat.Disable();
        playerControls.Player.SendChat.Disable();
    }
}
