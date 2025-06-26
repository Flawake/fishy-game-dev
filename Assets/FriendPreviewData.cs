using System;
using TMPro;
using UnityEngine;

public class FriendPreviewData : MonoBehaviour
{
    private Guid friendGUID;
    [SerializeField] TMP_Text friendName;

    private FriendSystem friendSystem;

    private FriendSystem GetFriendSystem()
    {
        if (friendSystem == null)
        {
            friendSystem = GetComponentInParent<FriendSystem>();
        }
        return friendSystem;
    }
    public void SetGuid(Guid guid)
    {
        friendGUID = guid;
    }

    public void SetPlayerName(string playerName)
    {
        friendName.text = playerName;
    }

    // Called from button in game
    public void MessagePlayer()
    {
        MailSystem mailSystem = transform.GetComponentInParent<MailSystem>();
        mailSystem.SetupNewMail(friendGUID);
    }

    // Called from button in game
    public void RemoveFriend()
    {
        FriendSystem friendSystemRef = GetFriendSystem();
        friendSystemRef.RemoveFriend(friendGUID);

        FriendsGUIManager friendsGUI = GetComponentInParent<FriendsGUIManager>();
        friendsGUI.RefreshFriendsGUI();
    }

    // Called from button in game
    public void AcceptFriendRequest()
    {
        HandleFriendRequest(true);
    }

    // Called from button in game
    public void DeclineFriendRequest()
    {
        HandleFriendRequest(false);
    }

    private void HandleFriendRequest(bool accepted)
    {
        FriendSystem friendSystemRef = GetFriendSystem();
        friendSystemRef.AnswerFriendRequest(friendGUID, accepted);

        FriendsGUIManager friendsGUI = GetComponentInParent<FriendsGUIManager>();
        friendsGUI.RefreshRequestGUI();
    }
}
