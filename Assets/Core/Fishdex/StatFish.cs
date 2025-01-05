using Mirror;
using UnityEngine;

public class StatFish
{
    public int id;
    public int amount;
    public int maxCaughtLength;
    public int averageLength;
    //TODO: add caught area's

    public StatFish(UserData.CaughtFish fish)
    {
        id = fish.id;
        amount = fish.amount;
        maxCaughtLength = fish.maxLength;
        FishConfiguration fishConfig = ItemsInGame.getFishByID(fish.id);
        if (fishConfig != null )
        {
            averageLength = fishConfig.avarageLength;
        }
        else
        {
            averageLength = 0;
            Debug.LogWarning("Made a statfish with an ID that could not be found in the game");
        }
    }

    public StatFish(int _id, int _amount, int _maxCaughtLength)
    {
        id = _id;
        amount = _amount;
        maxCaughtLength = _maxCaughtLength;
        FishConfiguration fishConfig = ItemsInGame.getFishByID(_id);
        if (fishConfig != null)
        {
            averageLength = fishConfig.avarageLength;
        }
        else
        {
            averageLength = 0;
            Debug.LogWarning("Made a statfish with an ID that could not be found in the game");
        }
    }
}

public static class StatFishReaderWriter
{
    public static void WriteStatFish(this NetworkWriter writer, StatFish fish)
    {
        writer.WriteInt(fish.id);
        writer.WriteInt(fish.amount);
        writer.WriteInt(fish.maxCaughtLength);
    }

    public static StatFish ReadStatFishm(this NetworkReader reader)
    {
        return new StatFish(reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
    }
}
