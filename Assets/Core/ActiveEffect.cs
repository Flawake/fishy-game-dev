using System;
using Mirror;

public struct ActiveEffect
{
    private int _itemId;
    private DateTime _expiry;

    public ActiveEffect(int itemId, DateTime expiry)
    {
        _itemId = itemId;
        _expiry = expiry;
    }
    
    public int ItemId => _itemId;
    public DateTime Expiry => _expiry;
}

public static class ActiveEffectSyncer
{
    public static void WriteEffect(this NetworkWriter writer, ActiveEffect value)
    {
        writer.WriteInt(value.ItemId);
        writer.WriteLong(value.Expiry.Ticks);
    }

    public static ActiveEffect ReadEffect(this NetworkReader reader)
    {
        return new ActiveEffect(
            reader.ReadInt(),
            new DateTime(reader.ReadLong())
        );
    }
}