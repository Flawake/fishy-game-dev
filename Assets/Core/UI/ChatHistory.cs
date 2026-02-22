using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ChatHistory : MonoBehaviour
{
    [SerializeField]
    TMP_Text textUI;
    [SerializeField]
    ScrollRect scrollRect;
    [SerializeField]
    TMP_InputField chatInput;
    [SerializeField]
    PlayerInfoUIManager UIManager;

    PlayerControls playerControls;

    public void OpenChat(InputAction.CallbackContext context)
    {
        if(chatInput.isFocused || MailGuiManager.IsSendingMailOpen())
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

    public void CloseChat(InputAction.CallbackContext context)
    {
        // Only deselect if chat input field is currently selected
        if (chatInput.isFocused)
        {
            DeselectSendChatField();
        }
    }

    void DeselectSendChatField()
    {
        // Check if the chat input field is actually selected before deselecting
        if (chatInput.isFocused)
        {
            GameObject eventSystem = GameObject.Find("EventSystem");
            if (eventSystem != null)
            {
                var es = eventSystem.GetComponent<UnityEngine.EventSystems.EventSystem>();
                if (es != null && es.currentSelectedGameObject == chatInput.gameObject)
                {
                    es.SetSelectedGameObject(null);
                }
            }
        }
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

        playerControls.Player.CloseChat.performed += CloseChat;
        playerControls.Player.CloseChat.Enable();
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
        playerControls.Player.CloseChat.Disable();
    }
}
