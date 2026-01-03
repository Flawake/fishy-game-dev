using Mirror;
using System;
using UnityEngine;

[Serializable]
public enum ItemType
{
    Rod,
    Bait,
    Fish,
    Extra,
}

public enum RodThrowDistance
{
    Close,
    Medium,
    Far,
}

public static class ItemObjectSerializer
{
    
    public static void WriteGuid(this NetworkWriter writer, Guid guid)
    {
        byte[] bytes = guid.ToByteArray();
        writer.WriteBytes(bytes, 0, 16);
    }
    
    public static Guid ReadGuid(this NetworkReader reader)
    {
        byte[] bytes = reader.ReadBytes(16);
        return new Guid(bytes);
    }
}
