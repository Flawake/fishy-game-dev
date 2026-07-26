using System.Linq;
using Mirror;
using Unity.Mathematics;
using UnityEngine;

public class PlayerFishdexFishes : NetworkBehaviour
{
    public readonly SyncList<StatFish> statFishContainer = new();

    [SerializeField] MissionManager missionManager;

    /// <summary>Number of distinct species this player has discovered.</summary>
    public int DiscoveredSpeciesCount => statFishContainer.Count;

    [Server]
    public void SaveFishStats(UserData playerData)
    {
        if(playerData.fish_data.Length == 0)
        {
            return;
        }

        foreach(UserData.FishData fish in playerData.fish_data)
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

            if (!statFish.areasCaught.Contains((int)fish.areaFishing))
            {
                statFish.areasCaught = statFish.areasCaught.Append((int)fish.areaFishing).ToArray();
            }
            if (!statFish.baitsCaught.Contains(fish.usedBait.Id))
            {
                statFish.baitsCaught = statFish.baitsCaught.Append(fish.usedBait.Id).ToArray();
            }
            // We're only updating a referenced value inside of the list, not the list itself. The edited item is not being synced across the network,
            // so we need to inform the player about updating the list themself.
            RpcUpdateSynclistFish(fish.id, newLen, (int)fish.areaFishing, fish.usedBait.Id);
        }
        else
        {
            statFishContainer.Add(new StatFish(fish, 1));
            missionManager.FishdexUpdated(statFishContainer.Count);
        }
    }

    [TargetRpc]
    void RpcUpdateSynclistFish(int id, int newLenth, int areaCaught, int baitUsed)
    {
        if (ContainsFish(id))
        {
            StatFish fish = GetStatFish(id);
            fish.maxCaughtLength = newLenth;
            fish.amount += 1;

            if (!fish.areasCaught.Contains((int)areaCaught))
            {
                fish.areasCaught = fish.areasCaught.Append((int)areaCaught).ToArray();
            }
            if (!fish.baitsCaught.Contains(baitUsed))
            {
                fish.baitsCaught = fish.baitsCaught.Append(baitUsed).ToArray();
            }
        }
    }
}
