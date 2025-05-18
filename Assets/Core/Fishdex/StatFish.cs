using Mirror;
using UnityEngine;

public class StatFish
{
    public int id;
    public int amount;
    public int maxCaughtLength;
    //TODO: add caught area's

    public StatFish(UserData.FishData fish)
    {
        id = fish.fish_id;
        amount = fish.amount;
        maxCaughtLength = fish.max_length;
    }

    public StatFish(int _id, int _amount, int _maxCaughtLength)
    {
        id = _id;
        amount = _amount;
        maxCaughtLength = _maxCaughtLength;
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
