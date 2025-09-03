using System;
using System.Collections.Generic;
using UnityEngine;

public class FriendsGUIManager : MonoBehaviour
{
    [SerializeField] private GameObject background;
    [SerializeField] private GameObject FriendPreviewPrefab;
    [SerializeField] private GameObject PendingFriendPreviewPrefab;
    [SerializeField] private GameObject contentHolder;
    
    PlayerData playerData;

    PlayerData GetPlayerData()
    {
        if (playerData == null)
        {
            playerData = GetComponentInParent<PlayerData>();
        }
        return playerData;
    }
    
    public void CloseFriendManager()
    {
        background.SetActive(false);
    }
    
    public void OpenFriendManager()
    {
        LoadFriends();
        background.SetActive(true);
    }

    public void ToggleFriendManager()
    {
        if (background.activeSelf)
        {
            CloseFriendManager();
        }
        else
        {
            OpenFriendManager();
        }
    }

    public void RefreshRequestGUI()
    {
        if (!background.activeSelf)
        {
            return;
        }
        LoadFriendRequests();
    }

    public void RefreshFriendsGUI()
    {
        if (!background.activeSelf)
        {
            return;
        }
        LoadFriends();
    }

    public void SearchFriendName(string friendName)
    {
        Debug.Log($"Searching for friend {friendName}");
    }

    //Also called from button in game
    public void LoadFriends()
    {
        // Remove previous content items first.
        foreach (Transform child in contentHolder.transform)
        {
            Destroy(child.gameObject);
        }
        HashSet<Guid> friends = GetPlayerData().GetFriendList();
        foreach (Guid friend in friends)
        {
            GameObject friendPreview = Instantiate(FriendPreviewPrefab, contentHolder.transform);
            FriendPreviewData previewData = friendPreview.GetComponent<FriendPreviewData>();
            previewData.SetGuid(friend);
            previewData.SetPlayerName("Can't show name yet");
        }
    }

    //Also called from button in game
    public void LoadFriendRequests()
    {
        // Remove previous content items first.
        foreach (Transform child in contentHolder.transform)
        {
            Destroy(child.gameObject);
        }
        Dictionary<Guid, bool> pendingRequests = GetPlayerData().GetPendingFriendRequests();
        foreach ((Guid playerID, bool requestSent) in pendingRequests)
        {
            if (requestSent)
            {

            }
            else
            {

            }
            GameObject pendingFriend = Instantiate(PendingFriendPreviewPrefab, contentHolder.transform);
            FriendPreviewData previewData = pendingFriend.GetComponent<FriendPreviewData>();
            previewData.SetGuid(playerID);
            previewData.SetPlayerName("Can't show name yet");
        }
    }
}
