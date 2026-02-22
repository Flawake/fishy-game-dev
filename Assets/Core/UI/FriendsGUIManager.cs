using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class FriendsGUIManager : MonoBehaviour
{
    [SerializeField] private GameObject background;
    [SerializeField] private GameObject FriendPreviewPrefab;
    [SerializeField] private GameObject PendingFriendPreviewPrefab;
    [SerializeField] private GameObject contentHolder;
    private FriendSystem friendSystem;

    private FriendSystem GetFriendSystem()
    {
        if (friendSystem == null)
        {
            friendSystem = GetComponentInParent<FriendSystem>();
        }
        return friendSystem;
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
        SyncDictionary<Guid, Friend> friends = GetFriendSystem().GetFriendList();
        foreach ((Guid friendGuid, Friend friend) in friends)
        {
            GameObject friendPreview = Instantiate(FriendPreviewPrefab, contentHolder.transform);
            FriendPreviewData previewData = friendPreview.GetComponent<FriendPreviewData>();
            previewData.SetGuid(friendGuid);
            previewData.SetPlayerName(friend.friendName);
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
        SyncDictionary<Guid, FriendRequest> pendingRequests = GetFriendSystem().GetFriendRequestList();
        foreach ((Guid playerID, FriendRequest request) in pendingRequests)
        {
            if (request.requestType == FriendRequestType.SEND)
            {
                continue;
            }
            GameObject pendingFriend = Instantiate(PendingFriendPreviewPrefab, contentHolder.transform);
            FriendPreviewData previewData = pendingFriend.GetComponent<FriendPreviewData>();
            previewData.SetGuid(playerID);
            previewData.SetPlayerName(request.NameOther);
        }
    }
}
