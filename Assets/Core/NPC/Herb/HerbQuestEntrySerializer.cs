using Mirror;

/// <summary>
/// Mirror serialization for <see cref="HerbQuestEntry"/>. Lets a single
/// SyncList&lt;HerbQuestEntry&gt; carry the quest fishes to clients (see <see cref="HerbQuestSync"/>)
/// instead of two parallel lists. The weaver picks these extension methods up by name.
/// </summary>
public static class HerbQuestEntrySerializer
{
    public static void WriteHerbQuestEntry(this NetworkWriter writer, HerbQuestEntry entry)
    {
        writer.WriteInt(entry.fishId);
        writer.WriteInt(entry.amount);
    }

    public static HerbQuestEntry ReadHerbQuestEntry(this NetworkReader reader)
    {
        return new HerbQuestEntry
        {
            fishId = reader.ReadInt(),
            amount = reader.ReadInt(),
        };
    }
}
