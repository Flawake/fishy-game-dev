using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

public class PlayersNearWater : NetworkBehaviour
{
    //Holds a list of all players that are close to the water to know if they can throw in.
    [SerializeField] List<uint> playersNearThisWater = new List<uint>();
    [SerializeField] List<PlayersNearWater> connectedWaters = new List<PlayersNearWater>();

    LayerMask playerLayer;

    void Awake()
    {
        playerLayer = LayerMask.NameToLayer("Player");
    }

    //Add a player to the list when it comes close
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == playerLayer)
        {
            NetworkIdentity id = collision.gameObject.GetComponent<NetworkIdentity>();
            if (id != null)
            {
                playersNearThisWater.Add(id.netId);
            }
        }
    }

    //Remove a player from the list when it moves away.
    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == playerLayer)
        {
            NetworkIdentity id = collision.gameObject.GetComponent<NetworkIdentity>();
            if (id != null)
            {
                playersNearThisWater.Remove(id.netId);
            }
        }
    }

    List<uint> GetPlayersNearWater() {
        return playersNearThisWater;
    }

    public List<uint> GetPlayersNearPuddle() {
        List<PlayersNearWater> puddleCollection = new List<PlayersNearWater>
        {
            this
        };
        MakePuddle(puddleCollection);
        List<uint> players = new List<uint>();
        foreach(PlayersNearWater playerCollection in puddleCollection) {
            players.AddRange(playerCollection.GetPlayersNearWater());
        }
        return players;
    }

    List<PlayersNearWater> MakePuddle(List<PlayersNearWater> collection) {
        foreach(PlayersNearWater water in connectedWaters) {
            if(!collection.Contains(water)) {
                collection.Add(water);
                water.MakePuddle(collection);
            }
        }
        return collection;
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
