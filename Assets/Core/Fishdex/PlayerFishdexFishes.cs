using Mirror;

public class PlayerFishdexFishes : NetworkBehaviour
{
    public readonly SyncList<StatFish> statFishContainer = new();

    [Server]
    public void SaveFishStats(UserData playerData)
    {
        if(playerData.stats.caughtFishes == null)
        {
            return;
        }

        foreach(UserData.CaughtFish fish in playerData.stats.caughtFishes)
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
}
