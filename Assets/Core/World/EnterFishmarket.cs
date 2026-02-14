using Mirror;
using UnityEngine;

public class EnterFishmarket : NetworkBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!NetworkClient.active)
        {
            return;
        }
        if (other.gameObject.GetComponentInParent<NetworkIdentity>().isLocalPlayer)
        {
            ArrivalAnimationRunner runner = other.gameObject.GetComponentInParent<ArrivalAnimationRunner>();
            if (runner != null && runner.IsRunning)
            {
                return;
            }
            WorldTravel.ClientInstantiateTravelTo(Area.FishMarket);
        }
    }
}
