using UnityEngine;
using Mirror;

public class ExitFishmarket : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!NetworkClient.active)
        {
            return;
        }
        if (other.gameObject.GetComponentInParent<NetworkIdentity>().isLocalPlayer)
        {
            WorldTravel.ClientInstantiateTravelTo(Area.FusetaBeach, WorldTravel.CustomSpawnInstruction.WalkOutsideFishmarket);
        }
    }
}
