using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// The friend system is a system containing of multiple files. Here is a brief description of their purpose.
/// 
/// -- FriendSystem.cs
/// Handles the requests coming in from the GUI, also forwards it to the Database after some calculations have been done.
/// 
/// -- FriendGUIManager.cs
/// Handles the GUI searching for friends and loading the friend preview containers.
/// 
/// -- FriendPreviewData.cs
/// Each container has this file. This makes accepting, declining, trading, etc... possible with friends.
/// 
/// </summary>

public class Friend
{
    public Guid friendGuid;
    public string friendName;

    public Friend(Guid _friendGuid, string _friendName)
    {
        friendGuid = _friendGuid;
        friendName = _friendName;
    }
}

public enum FriendRequestType
{
    SEND,
    RECEIVED,
}

public class FriendRequest
{
    public Guid GuidOther;
    public string NameOther;
    public FriendRequestType requestType;
    public FriendRequest(Guid _guidOther, string _nameOther, FriendRequestType _requestType)
    {
        GuidOther = _guidOther;
        NameOther = _nameOther;
        requestType = _requestType;
    }
}

public class FriendSystem : NetworkBehaviour
{
    [SerializeField]
    PlayerData playerData;
    private SyncDictionary<Guid, Friend> friends = new SyncDictionary<Guid, Friend>();
    private SyncDictionary<Guid, FriendRequest> friendRequests = new SyncDictionary<Guid, FriendRequest>();

    [Server]
    // fully qualified: this file also declares a game-side Friend/FriendRequest
    public void SetInitialFriendList(List<FishyGame.Api.Friend> initialFriends)
    {
        foreach (FishyGame.Api.Friend initialFriend in initialFriends)
        {
            friends.Add(initialFriend.friendId, new Friend(initialFriend.friendId, initialFriend.friendName));
        }
    }
    
    [Server]
    public void SetInitialFriendRequestList(List<FishyGame.Api.FriendRequest> initialFriendRequests)
    {
        foreach (FishyGame.Api.FriendRequest initialFriendRequest in initialFriendRequests)
        {
            FriendRequestType requestType = initialFriendRequest.RequestSenderId == playerData.GetUuid() ? FriendRequestType.SEND : FriendRequestType.RECEIVED;
            friendRequests.Add(initialFriendRequest.otherId, new FriendRequest(initialFriendRequest.otherId, initialFriendRequest.otherName, requestType));
        }
    }

    public SyncDictionary<Guid, Friend> GetFriendList()
    {
        return friends;
    }

    public SyncDictionary<Guid, FriendRequest> GetFriendRequestList()
    {
        return friendRequests;
    }

    public bool CanSendRequest(Guid receiverID)
    {
        if (!isLocalPlayer && !isServer)
        {
            Debug.Log("This function should only be called on an spawned player or the server");
            return false;
        }
        // Check if player already has an unanswered friend request running or if the player is already a friend
        if (friends.ContainsKey(receiverID) || friendRequests.ContainsKey(receiverID) || playerData.GetUuid() == receiverID)
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
    private void CmdRemoveFriend(Guid friendToRemove, NetworkConnectionToClient conn = null)
    {
        if (friends.ContainsKey(friendToRemove))
        {
            friends.Remove(friendToRemove);
            DatabaseCommunications.RemoveFriend(playerData.GetUuid(), friendToRemove);
        }
        if (GameNetworkManager.connUUID.TryGetValue(friendToRemove, out NetworkConnectionToClient other))
        {
            other.identity.gameObject.GetComponent<FriendSystem>().friends.Remove(playerData.GetUuid());
        }
    }

    [Server]
    void FriendRequestHandled(Guid otherPlayer, string nameOther, bool accepted)
    {
        friendRequests.Remove(otherPlayer);
        if (accepted)
        {
            friends.Add(otherPlayer, new Friend(otherPlayer, nameOther));
        }
    }

    [Command]
    void CmdSendFriendRequest(Guid playerToBefriend)
    {
        if (!CanSendRequest(playerToBefriend))
        {
            return;
        }

        NetworkConnectionToClient otherNetConnection;
        GameNetworkManager.connUUID.TryGetValue(playerToBefriend, out otherNetConnection);
        string playerToBefriendName = null;
        if (otherNetConnection != null)
        {
            GameNetworkManager.connNames.TryGetValue(otherNetConnection, out playerToBefriendName);
        }

        if (playerToBefriendName == null)
        {
            playerToBefriendName = "Err: unknown name";
        }
        
        if (friendRequests.TryGetValue(playerToBefriend, out FriendRequest friendRequest))
        {
            if (friendRequest.requestType == FriendRequestType.RECEIVED)
            {
                // The other player has already send a friend request in the past, add this player as friend immediatly
                CmdAnswerFriendRequest(playerToBefriend, true);
            }
            // Request was handled
            return;
        }

        DatabaseCommunications.AddFriendRequest(playerData.GetUuid(), playerToBefriend, playerData.GetUuid());
        friendRequests.Add(playerToBefriend, new FriendRequest(playerToBefriend, playerToBefriendName, FriendRequestType.SEND));

        if (GameNetworkManager.connUUID.TryGetValue(playerToBefriend, out NetworkConnectionToClient receiverConn))
        {
            FriendSystem otherFriendSystem = receiverConn.identity.gameObject.GetComponent<FriendSystem>();
            otherFriendSystem.friendRequests.Add(playerData.GetUuid(), new FriendRequest(playerData.GetUuid(), playerData.GetUsername(), FriendRequestType.RECEIVED));
            otherFriendSystem.TargetNotifyFriendRequestReceived(receiverConn, playerData.GetUsername());
        }
    }

    // Notifies the receiving player that they got a new friend request.
    [TargetRpc]
    void TargetNotifyFriendRequestReceived(NetworkConnectionToClient target, string senderName)
    {
        MessageUIHandler.AddNotification(new Notification
        {
            message = $"{senderName} sent you a friend request",
            callback = () => GetComponentInChildren<FriendsGUIManager>().OpenFriendRequests(),
        });
    }

    // Notifies the original requester that their friend request was accepted.
    [TargetRpc]
    void TargetNotifyFriendRequestAccepted(NetworkConnectionToClient target, string accepterName)
    {
        MessageUIHandler.AddNotification(new Notification
        {
            message = $"{accepterName} accepted your friend request"
        });
    }

    [Client]
    public void AnswerFriendRequest(Guid answeredPlayerRequest, string answeredPlayerName, bool accepted)
    {
        // Players can't accept a request they have send themself. They can cancel it tough
        if (accepted && !friendRequests.TryGetValue(answeredPlayerRequest, out FriendRequest request))
        {
            if (request.requestType == FriendRequestType.SEND)
            {
                return;
            }
        }
        CmdAnswerFriendRequest(answeredPlayerRequest, accepted);
    }

    [Command]
    public void CmdAnswerFriendRequest(Guid answeredPlayerRequestUuid, bool accepted)
    {
        // Players can't accept a request they have send themself. They can cancel it tough
        if (accepted && !friendRequests.TryGetValue(answeredPlayerRequestUuid, out FriendRequest request))
        {
            if (request.requestType == FriendRequestType.SEND)
            {
                return;
            }
        }

        DatabaseCommunications.HandleFriendRequest(playerData.GetUuid(), answeredPlayerRequestUuid, accepted);
        friendRequests.TryGetValue(answeredPlayerRequestUuid, out FriendRequest friendRequest);

        string playerName = friendRequest.NameOther == null ? "Err: unknown name" : friendRequest.NameOther;

        FriendRequestHandled(answeredPlayerRequestUuid, playerName, accepted);

        // Also inform the other player about the change if he's online
        GameNetworkManager.connUUID.TryGetValue(answeredPlayerRequestUuid, out NetworkConnectionToClient receiverConn);
        if (receiverConn != null)
        {
            FriendSystem otherFriendSystem = receiverConn.identity.gameObject.GetComponent<FriendSystem>();
            otherFriendSystem.FriendRequestHandled(playerData.GetUuid(), playerData.GetUsername(), accepted);
            if (accepted)
            {
                otherFriendSystem.TargetNotifyFriendRequestAccepted(receiverConn, playerData.GetUsername());
            }
        }
    }
}

public static class FriendNetworkSerialization
{

    // -------------------------
    // Friend
    // -------------------------

    public static void WriteFriend(this NetworkWriter writer, Friend value)
    {
        writer.WriteGuid(value.friendGuid);
        writer.WriteString(value.friendName);
    }

    public static Friend ReadFriend(this NetworkReader reader)
    {
        Guid guid = reader.ReadGuid();
        string name = reader.ReadString();
        return new Friend(guid, name);
    }

    // -------------------------
    // FriendRequest
    // -------------------------

    public static void WriteFriendRequest(this NetworkWriter writer, FriendRequest value)
    {
        writer.WriteGuid(value.GuidOther);
        writer.WriteString(value.NameOther);
        writer.WriteByte((byte)value.requestType);
    }

    public static FriendRequest ReadFriendRequest(this NetworkReader reader)
    {
        Guid guid = reader.ReadGuid();
        string name = reader.ReadString();
        FriendRequestType type = (FriendRequestType)reader.ReadByte();
        return new FriendRequest(guid, name, type);
    }
}