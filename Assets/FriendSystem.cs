using System;
using Mirror;
using UnityEngine;

public class FriendSystem : NetworkBehaviour
{
    [SerializeField] PlayerData playerData;

    public bool CanSendRequest(Guid receiverID)
    {
        if (!isLocalPlayer && !isServer)
        {
            Debug.Log("This function should only be called on the localplayer or the server");
            return false;
        }
        // Check if player already has an unanswered friend request running or if the player is already a friend
        if (playerData.GuidInFriendList(receiverID) || playerData.FriendrequestSendToGuid(receiverID) || playerData.GetUuid() == receiverID)
        {
            return false;
        }
        return true;
    }

    [Client]
    public void MakeNewFriendRequest(Guid playerToBefriend)
    {
        if (playerToBefriend == Guid.Empty)
        {
            Debug.LogWarning("playerToBefriend was empty, this should be impossible");
        }
        CMDSendFriendRequest(playerToBefriend);
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

    [Command]
    void CMDSendFriendRequest(Guid playerToBefriend, NetworkConnectionToClient conn = null)
    {
        Debug.Log("Sending friend request");
        PlayerData playerData = conn.identity.GetComponent<PlayerData>();
        FriendSystem playerFriendSystem = conn.identity.GetComponent<FriendSystem>();
        if (!playerFriendSystem.CanSendRequest(playerToBefriend))
        {
            return;
        }
        DatabaseCommunications.AddFriendRequest(playerData.GetUuid(), playerToBefriend);
        playerData.AddNewFriendRequest(playerToBefriend, true);

        if(GameNetworkManager.connUUID.TryGetValue(playerToBefriend, out NetworkConnectionToClient receiverConn))
        {
            TargetReceiveFriendRequest(receiverConn, playerData.GetUuid());
        }
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
    void TargetReceiveFriendRequest(NetworkConnectionToClient conn, Guid sendingPlayerID)
    {
        playerData.AddNewFriendRequest(sendingPlayerID, false);
    }
}
