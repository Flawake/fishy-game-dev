using System;
using Mirror;
using UnityEngine;

/// <summary>
/// The friend system is a system containing of multiple files. Here is a brief description of their purpose.
/// 
/// -- PlayerData.cs
/// Keeps the localplayers data on the server and client. Should also keep the variables is sync
/// 
/// -- FriendSystem.cs
/// Handles the requests coming in from the GUI, and forwards it to the PlayerData and the Database after some calculations have been done.
/// 
/// -- FriendGUIManager.cs
/// Handles the GUI searching for friends and loading the friend preview containers.
/// 
/// -- FriendPreviewData.cs
/// Each container has this file. This makes accepting, declining, trading, etc... possible with friends.
/// 
/// </summary>

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
        playerData.RemoveFromFriendList(friendToRemove);
        CmdRemoveFriend(friendToRemove);
    }

    [Command]
    private void CmdRemoveFriend(Guid friendToRemove, NetworkConnectionToClient conn = null)
    {
        if (playerData.GuidInFriendList(friendToRemove))
        {
            playerData.RemoveFromFriendList(friendToRemove);
            DatabaseCommunications.RemoveFriend(playerData.GetUuid(), friendToRemove);
        }
        if (GameNetworkManager.connUUID.TryGetValue(friendToRemove, out NetworkConnectionToClient other))
        {
            other.identity.gameObject.GetComponent<PlayerData>().RemoveFromFriendList(playerData.GetUuid());
        }
    }

    [Command]
    void CmdSendFriendRequest(Guid playerToBefriend)
    {
        if (!CanSendRequest(playerToBefriend))
        {
            return;
        }
        // The other player has already send a friend request in the past, add this player as friend immediatly
        if (playerData.GetPendingFriendRequests().TryGetValue(playerToBefriend, out bool requestSend))
        {
            if (!requestSend)
            {
                DatabaseCommunications.HandleFriendRequest(playerData.GetUuid(), playerToBefriend, true);
                playerData.RemoveFromFriendRequestList(playerToBefriend);
                playerData.AddToFriendList(playerToBefriend);
                if (GameNetworkManager.connUUID.TryGetValue(playerToBefriend, out NetworkConnectionToClient other))
                {
                    PlayerData othersData = other.identity.gameObject.GetComponent<PlayerData>();
                    othersData.RemoveFromFriendRequestList(playerData.GetUuid());
                    othersData.AddToFriendList(playerData.GetUuid());
                }
            }
            // Request was handled
            return;
        }
        DatabaseCommunications.AddFriendRequest(playerData.GetUuid(), playerToBefriend, playerData.GetUuid());
        playerData.AddNewFriendRequest(playerToBefriend, true);

        if (GameNetworkManager.connUUID.TryGetValue(playerToBefriend, out NetworkConnectionToClient receiverConn))
        {
            PlayerData receivingPlayersData = receiverConn.identity.GetComponent<PlayerData>();
            receivingPlayersData.AddNewFriendRequest(playerData.GetUuid(), false);
        }
    }

    [Client]
    public void AnswerFriendRequest(Guid answeredPlayerRequest, bool accepted)
    {
        // Players can't accept a request they have send themself. They can cancel it tough
        if (!playerData.FriendrequestReceivedFromGuid(answeredPlayerRequest) && accepted)
        {
            return;
        }
        playerData.RemoveFromFriendRequestList(answeredPlayerRequest);
        if (accepted)
        {
            playerData.AddToFriendList(answeredPlayerRequest);
        }
        CmdAnswerFriendRequest(answeredPlayerRequest, accepted);
    }

    [Command]
    public void CmdAnswerFriendRequest(Guid answeredPlayerRequest, bool accepted, NetworkConnectionToClient conn = null)
    {
        // Players can't accept a request they have send themself. They can cancel it tough
        if (!playerData.FriendrequestReceivedFromGuid(answeredPlayerRequest) && accepted)
        {
            return;
        }

        DatabaseCommunications.HandleFriendRequest(playerData.GetUuid(), answeredPlayerRequest, accepted);

        playerData.RemoveFromFriendRequestList(answeredPlayerRequest);
        if (accepted)
        {
            playerData.AddToFriendList(answeredPlayerRequest);
        }


        // Also inform the other player about the change if he's online
        GameNetworkManager.connUUID.TryGetValue(answeredPlayerRequest, out NetworkConnectionToClient receiverConn);
        if (receiverConn != null)
        {
            PlayerData otherPlayersData = receiverConn.identity.gameObject.GetComponent<PlayerData>();
            otherPlayersData.RemoveFromFriendRequestList(playerData.GetUuid());
            if (accepted)
            {
                otherPlayersData.AddToFriendList(playerData.GetUuid());
            }
        }
    }
}
