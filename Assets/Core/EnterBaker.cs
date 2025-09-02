using Mirror;
using UnityEngine;

public class EnterBaker : NetworkBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {        
        if (!NetworkClient.active)
        {
            return;
        }
        if (other.CompareTag("PlayerSprite") && other.gameObject.GetComponentInParent<NetworkIdentity>().isLocalPlayer)
        {
            WorldTravel.TravelTo(Area.Baker);
        }
    }
}
