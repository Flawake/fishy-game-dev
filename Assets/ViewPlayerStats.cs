using UnityEngine;
using TMPro;
using Mirror;
using System;
using UnityEngine.Events;

public class ViewPlayerStats : MonoBehaviour
{
    PlayerStatsUIManager _playerStatsUIManager;
    [SerializeField]
    GameObject _player;
    [SerializeField]
    GameObject playerInfoPreviewCanvas;
    [SerializeField]
    PlayerData playerData;

    [SerializeField]
    TMP_Text playerNameField;
    [SerializeField]
    TMP_Text playerLevelField;
    
    public bool ProcesPlayerCheck(Vector2 clickedPos)
    {
        GameObject clickedPlayer = GetClickedPlayer(clickedPos);
        if (clickedPlayer == null)
        {
            return false;
        }
        //Get the viewPlayerStats of the clicked player
        ViewPlayerStats playerStats = clickedPlayer.GetComponentInChildren<ViewPlayerStats>();
        playerStats.OpenPlayerStatMenu();
        return true;
    }

    GameObject GetClickedPlayer(Vector2 clickedPos)
    { 
        var playerLayer = LayerMask.GetMask("Player");
        RaycastHit2D[] hits =  Physics2D.RaycastAll(clickedPos, Vector2.zero, 100, playerLayer);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.name == "PreciseCollision" )
            {
                return hit.collider.GetComponentInParent<PlayerData>().gameObject;
            }
        }
        return null;
    }
    
    void OpenPlayerStatMenu()
    {
        //Bind canvas camera to local player instead of other player.
        playerInfoPreviewCanvas.GetComponent<Canvas>().worldCamera = NetworkClient.connection.identity.GetComponentInChildren<Camera>();
        playerNameField.text = playerData.GetUsername();
        playerLevelField.text = playerData.GetXp().ToString();
        playerInfoPreviewCanvas.gameObject.SetActive(!playerInfoPreviewCanvas.gameObject.activeSelf);
    }

    public void More()
    {
        _playerStatsUIManager = NetworkClient.connection.identity.GetComponentInChildren<PlayerStatsUIManager>();
        if (playerData != null && _playerStatsUIManager != null)
        {
            _playerStatsUIManager.ToggleStore(playerData);
        }
        else
        {
            (_playerStatsUIManager).TestFunction();
        }
        
    }

    public void StartNewMailButton()
    {
        MailSystem mail = NetworkClient.connection.identity.GetComponent<MailSystem>();
        Guid receiverGuid = playerData.GetUuid();
        if (receiverGuid == Guid.Empty || receiverGuid == null)
        {
            Debug.LogWarning("receiverGuid was not valid for sending a mail");
            return;
        }
        print("UUID" + receiverGuid);
        mail.SetupNewMail(receiverGuid);
    }
}
