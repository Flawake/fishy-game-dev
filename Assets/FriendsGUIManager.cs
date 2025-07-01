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

    public void SearchFriendName(string friendName)
    {
        Debug.Log($"Searching for friend {friendName}");
    }

    public void LoadFriends()
    {
        // Remove previous content items first.
        foreach (Transform child in contentHolder.transform)
        {
            Destroy(child);
        }
        HashSet<Guid> friends = GetPlayerData().GetFriendList();
        foreach (Guid friend in friends)
        {
            GameObject friendPreview = Instantiate(FriendPreviewPrefab, contentHolder.transform);
            FriendPreviewData previewData = friendPreview.GetComponent<FriendPreviewData>();
            previewData.setGuid(friend);
            previewData.SetPlayerName("Can't show name yet");
        }
    }

    public void LoadFriendRequests()
    {
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
        }
    }
}
