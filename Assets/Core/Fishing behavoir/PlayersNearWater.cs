using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayersNearWater : NetworkBehaviour
{
    //Holds a list of all players that are close to the water to know if they can throw in.
    [SerializeField] public List<uint> playersCloseToWater = new List<uint>();

    //Add a player to the list when it comes close
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            NetworkIdentity id = collision.gameObject.GetComponent<NetworkIdentity>();
            if (id != null)
            {
                playersCloseToWater.Add(id.netId);
            }
        }
    }

    //Remove a player from the list when it moves away.
    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            NetworkIdentity id = collision.gameObject.GetComponent<NetworkIdentity>();
            if (id != null)
            {
                playersCloseToWater.Remove(id.netId);
            }
        }
    }

    //Clearing and repopulating the list every frame to make sure it is up to date since a disconnetion of a client would not call onTriggerExit.
    /*
    private void OnTriggerStay(Collider collision)
    {
        playersCloseToWater.Clear();

        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            NetworkIdentity id = collision.gameObject.GetComponent<NetworkIdentity>();
            if (id != null)
            {
                playersCloseToWater.Add(id.netId);
            }
        }
    }
    */
}
