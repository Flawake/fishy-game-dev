using Mirror;
using UnityEngine;

public class StatFish
{
    public int id;
    public int amount;
    public int maxCaughtLength;
    public int avarageLength;
    //TODO: add caught area's

    public StatFish(UserData.CaughtFish fish)
    {
        id = fish.id;
        amount = fish.amount;
        maxCaughtLength = fish.maxLength;
        FishConfiguration fishConfig = ItemsInGame.getFishByID(fish.id);
        if (fishConfig != null )
        {
            avarageLength = fishConfig.avarageLength;
        }
        else
        {
            avarageLength = 0;
            Debug.LogWarning("Made a statfish with an ID that could not be found in the game");
        }
    }

    public StatFish(StatFish fish)
    {
        id = fish.id;
        amount = fish.amount;
        maxCaughtLength = fish.maxCaughtLength;
        avarageLength = fish.avarageLength;
    }
}

public static class StatFishReaderWriter
{
    public static void WriteStatFish(this NetworkWriter writer, StatFish fish)
    {
        writer.Write(fish);
    }

    public static StatFish ReadStatFishm(this NetworkReader reader)
    {
        return new StatFish(reader.Read<StatFish>());
    }
}
