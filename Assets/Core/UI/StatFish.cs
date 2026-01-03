using Mirror;
using UnityEngine;
using UnityEngine.Rendering;

public class StatFish
{
    public int id;
    public int amount;
    public int maxCaughtLength;
    public int[] areasCaught;
    public int[] baitsCaught;

    public StatFish(UserData.FishData fish)
    {
        id = fish.fish_id;
        amount = fish.amount;
        maxCaughtLength = fish.max_length;
        areasCaught = fish.areas;
        baitsCaught = fish.baits;
    }

    public StatFish(int _id, int _amount, int _maxCaughtLength, int[] _areas, int[] _baits)
    {
        id = _id;
        amount = _amount;
        maxCaughtLength = _maxCaughtLength;
        areasCaught = _areas;
        baitsCaught = _baits;
    }
}

public static class StatFishReaderWriter
{
    public static void WriteStatFish(this NetworkWriter writer, StatFish fish)
    {
        writer.WriteInt(fish.id);
        writer.WriteInt(fish.amount);
        writer.WriteInt(fish.maxCaughtLength);
        writer.WriteArray<int>(fish.areasCaught);
        writer.WriteArray<int>(fish.baitsCaught);
    }

    public static StatFish ReadStatFishm(this NetworkReader reader)
    {
        return new StatFish(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadArray<int>(), reader.ReadArray<int>());
    }
}
