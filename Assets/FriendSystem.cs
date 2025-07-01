using System;
using Mirror;
using UnityEngine;

public class FriendSystem : NetworkBehaviour
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

    [Client]
    public void RemoveFriend(Guid friendToRemove)
    {
        CommandRemoveFriend(friendToRemove);
    }

    [Command]
    public void CommandRemoveFriend(Guid friendToRemove, NetworkConnectionToClient conn = null)
    {
        DatabaseCommunications.RemoveFriend(playerData.GetUuid(), friendToRemove);
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
    void AnswerFriendRequest(Guid answeredPlayerRequest, bool accepted, NetworkConnectionToClient conn = null)
    {
        PlayerData playerData = conn.identity.GetComponent<PlayerData>();
        DatabaseCommunications.HandleFriendRequest(playerData.GetUuid(), answeredPlayerRequest, accepted);
        if (!playerData.FriendrequestReceivedFromGuid(answeredPlayerRequest))
        {
            return;
        }
        playerData.RemovePendingFriendRequest(answeredPlayerRequest);
        if (accepted)
        {
            playerData.AddFriend(answeredPlayerRequest);
        }
    }

    [TargetRpc]
    void ReceiveFriendRequest(Guid sendingPlayerID)
    {
        playerData.AddNewFriendRequest(playerToBefriend, false);
    }
}
