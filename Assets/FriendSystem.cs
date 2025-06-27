using System;
using Mirror;
using UnityEngine;

public class FriendSystem : MonoBehaviour
{
    static Guid playerToBefriend = Guid.Empty;
    [SerializeField] PlayerData playerData;

    [Client]
    void MakeNewFriendRequest()
    {
        if (playerToBefriend == Guid.Empty)
        {
            Debug.LogWarning("playerToBefriend was empty, this should be impossible");
        }
        SendFriendRequest(playerToBefriend);
        playerToBefriend = Guid.Empty;
    }

    [Server]
    void SendFriendRequest(Guid playerToBefriend, NetworkConnectionToClient conn = null)
    {
        PlayerData playerData = conn.identity.GetComponent<PlayerData>();
        // Check if player already has an unanswered friend request running or if the player is already a friend
        if (playerData.GuidInFriendList(playerToBefriend) || playerData.FriendrequestSendToGuid(playerToBefriend))
        {
            return;
        }
        DatabaseCommunications.AddFriendRequest(playerData.GetUuid(), playerToBefriend);
        playerData.AddNewFriendRequest(playerToBefriend, true);
    }

    [Server]
    void AnswerFriendRequest(Guid userID, bool accepted, NetworkConnectionToClient conn = null)
    {
        PlayerData playerData = conn.identity.GetComponent<PlayerData>();
        if (!playerData.FriendrequestReceivedFromGuid(userID))
        {
            return;
        }
        playerData.RemovePendingFriendRequest(userID);
        if (accepted)
        {
            playerData.AddFriend(userID);
        }
    }

    [TargetRpc]
    void ReceiveFriendRequest(Guid sendingPlayerID)
    {
        playerData.AddNewFriendRequest(playerToBefriend, false);
    }
}
