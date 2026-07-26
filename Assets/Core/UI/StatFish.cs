using Mirror;

public class StatFish
{
    public int id;
    public int amount;
    public int maxCaughtLength;
    public int[] areasCaught;
    public int[] baitsCaught;

    public StatFish(FishyGame.Api.FishData fish)
    {
        id = fish.fish_id;
        amount = fish.amount;
        maxCaughtLength = fish.max_length;
        // the generated DTO uses List<int>; SyncList needs the arrays
        areasCaught = fish.areas.ToArray();
        baitsCaught = fish.baits.ToArray();
    }

    public StatFish(CurrentFish fish, int _amount)
    {
        id = fish.id;
        amount = _amount;
        maxCaughtLength = fish.length;
        areasCaught = new int[] { (int)fish.areaFishing };
        baitsCaught = new int[] { fish.usedBait.Id };
    }

    public StatFish(int id, int amount, int maxCaughtLength, int[] areasCaught, int[] baitsCaught)
    {
        this.id = id;
        this.amount = amount;
        this.maxCaughtLength = maxCaughtLength;
        this.areasCaught = areasCaught;
        this.baitsCaught = baitsCaught;
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

    public static StatFish ReadStatFish(this NetworkReader reader)
    {
        return new StatFish(reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadArray<int>(), reader.ReadArray<int>());
    }
}
