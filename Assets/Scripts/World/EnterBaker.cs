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
            ArrivalAnimationRunner runner = other.gameObject.GetComponentInParent<ArrivalAnimationRunner>();
            if (runner != null && runner.IsRunning)
            {
                return;
            }
            WorldTravel.TravelTo(Area.Baker);
        }
    }
}
