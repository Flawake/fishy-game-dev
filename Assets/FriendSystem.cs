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
        // Call the command on the main player object, not on the receiver's object
        NetworkClient.connection.identity.GetComponent<FriendSystem>().CmdSendFriendRequest(playerToBefriend);
    }

    [Client]
    public void RemoveFriend(Guid friendToRemove)
    {
        CmdRemoveFriend(friendToRemove);
    }

    [Command]
    public void CmdRemoveFriend(Guid friendToRemove, NetworkConnectionToClient conn = null)
    {
        PlayerData playerData = conn.identity.GetComponent<PlayerData>();
        if (playerData.GuidInFriendList(friendToRemove))
        {
            playerData.RemoveFriend(friendToRemove);
            DatabaseCommunications.RemoveFriend(playerData.GetUuid(), friendToRemove);
        }
    }

    [Command]
    void CmdSendFriendRequest(Guid playerToBefriend)
    {
        Debug.Log("Sending friend request");
        if (!CanSendRequest(playerToBefriend))
        {
            return;
        }
        DatabaseCommunications.AddFriendRequest(playerData.GetUuid(), playerToBefriend, playerData.GetUuid());
        playerData.AddNewFriendRequest(playerToBefriend, true);

        if(GameNetworkManager.connUUID.TryGetValue(playerToBefriend, out NetworkConnectionToClient receiverConn))
        {
            PlayerData receivingPlayersData = receiverConn.identity.GetComponent<PlayerData>();
            receivingPlayersData.AddNewFriendRequest(playerData.GetUuid(), false);
        }
    }

    [Command]
    public void CmdAnswerFriendRequest(Guid answeredPlayerRequest, bool accepted, NetworkConnectionToClient conn = null)
    {
        if (!playerData.FriendrequestReceivedFromGuid(answeredPlayerRequest))
        {
            return;
        }

        Guid callerID = conn.identity.GetComponent<PlayerData>().GetUuid();
        GameNetworkManager.connUUID.TryGetValue(answeredPlayerRequest, out NetworkConnectionToClient receiverConn);
            
        if (receiverConn != null)
        {
            playerData.RemovePendingFriendRequest(callerID);
        }
        DatabaseCommunications.HandleFriendRequest(playerData.GetUuid(), answeredPlayerRequest, accepted);
        playerData.RemovePendingFriendRequest(answeredPlayerRequest);
        if (accepted)
        {
            playerData.AddToFriendList(answeredPlayerRequest);
            if (receiverConn != null)
            {
                playerData.AddToFriendList(callerID);
            }
        }
    }
}
