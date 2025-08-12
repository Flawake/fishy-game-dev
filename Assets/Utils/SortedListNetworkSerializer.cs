using System.Collections.Generic;
using GlobalCompetitionSystem;
using Mirror;

public static class SortedListReaderWriter 
{
    public static void WriteSortedList(this NetworkWriter writer, SortedList<int, PlayerResult> list)
    {
        writer.WriteInt(list.Count);

        foreach (var kvp in list)
        {
            writer.Write(kvp.Key);
            writer.Write(kvp.Value);
        }
    }

    public static SortedList<int, PlayerResult> ReadSortedList(this NetworkReader reader)
    {
        int count = reader.ReadInt();
        var list = new SortedList<int, PlayerResult>(count);

        for (int i = 0; i < count; i++)
        {
            int key = reader.ReadInt();
            PlayerResult value = reader.ReadPlayerResult();
            list.Add(key, value);
        }

        return list;
    }
}