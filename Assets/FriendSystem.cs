using System;
using Mirror;
using UnityEngine;

public class FriendSystem : MonoBehaviour
{
    [SerializeField] PlayerData playerData;
    [Server]
    void sendFriendRequest(NetworkConnectionToClient conn)
    {
        
    }
}
