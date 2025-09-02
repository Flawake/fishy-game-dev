using Mirror;
using UnityEngine;

public class ExitBaker : NetworkBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!NetworkClient.active)
        {
            return;
        }
        if (other.gameObject.GetComponentInParent<NetworkIdentity>().isLocalPlayer)
        {
            WorldTravel.TravelTo(Area.Greenfields, WorldTravel.CustomSpawnInstruction.WalkOusideBakery);
        }
    }
}
