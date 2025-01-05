using Mirror;
using Unity.Mathematics;
using UnityEngine;

public class PlayerFishdexFishes : NetworkBehaviour
{
    public readonly SyncList<StatFish> statFishContainer = new();

    [Server]
    public void SaveFishStats(UserData playerData)
    {
        if(playerData.stats.fishes == null)
        {
            return;
        }

        foreach(UserData.CaughtFish fish in playerData.stats.fishes)
        {
            statFishContainer.Add(new StatFish(fish));
        }
    }

    public bool ContainsFish(int id)
    {
        foreach (StatFish fish in statFishContainer)
        {
            if(fish.id == id)
            {
                return true;
            }
        }
        return false;
    }

    public StatFish GetStatFish(int id)
    {
        foreach (StatFish fish in statFishContainer)
        {
            if (fish.id == id)
            {
                return fish;
            }
        }
        return null;
    }

    [Server]
    public void AddStatFish(CurrentFish fish)
    {
        if(ContainsFish(fish.id))
        {
            StatFish statFish = GetStatFish(fish.id);
            int newLen = math.max(fish.length, statFish.maxCaughtLength);
            statFish.maxCaughtLength = newLen;
            statFish.amount += 1;
            // We're only updating a referenced value inside of the list, not the list itself. The edited item is not being synced across the network,
            // so we need to inform the player about updating the list themself.
            RpcUpdateSynclistFish(fish.id, newLen);
        }
        else
        {
            statFishContainer.Add(new StatFish(fish.id, 1, fish.length));
        }
    }

    [TargetRpc]
    void RpcUpdateSynclistFish(int id, int newLenth)
    {
        if (ContainsFish(id))
        {
            StatFish fish = GetStatFish(id);
            fish.maxCaughtLength = newLenth;
            fish.amount += 1;
        }
    }
}
