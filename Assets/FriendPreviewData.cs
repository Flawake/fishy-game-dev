using System;
using TMPro;
using UnityEngine;

public class FriendPreviewData : MonoBehaviour
{
    private Guid friendGUID;
    [SerializeField] TMP_Text friendName;
    public void setGuid(Guid guid)
    {
        friendGUID = guid;
    }

    public void SetPlayerName(string playerName)
    {
        friendName.text = playerName;
    }

    //Called from button in game
    public void MessagePlayer()
    {
        MailSystem mailSystem = transform.GetComponentInParent<MailSystem>();
        mailSystem.SetupNewMail(friendGUID);
    }

    //Called from button in game
    public void RemoveFriend()
    {
        FriendSystem friendSystem = transform.GetComponentInParent<FriendSystem>();
        friendSystem.RemoveFriend(friendGUID);
    }
}
