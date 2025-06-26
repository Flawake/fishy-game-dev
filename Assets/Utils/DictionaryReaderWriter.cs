using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class DictionaryReaderWriter
{
    public static void WriteGuidBoolDict(this NetworkWriter writer, Dictionary<Guid, bool> dict)
    {
        writer.WriteInt(dict.Count);
        foreach (var kvp in dict)
        {
            writer.WriteGuid(kvp.Key);
            writer.WriteBool(kvp.Value);
        }
    }

    public static Dictionary<Guid, bool> ReadGuidBoolDict(this NetworkReader reader)
    {
        int count = reader.ReadInt();
        var dict = new Dictionary<Guid, bool>(count);
        for (int i = 0; i < count; i++)
        {
            Guid key = reader.ReadGuid();
            bool value = reader.ReadBool();
            dict[key] = value;
        }
        return dict;
    }
}
