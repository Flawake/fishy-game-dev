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

public abstract class ItemObject : ScriptableObject
{
    public Guid uuid;
    public ItemType type;
    public int id;
    public Sprite sprite;
    [TextArea(15, 20)]
    public string description;
    public bool stackable;
}

public static class ItemObjectGenerator
{
    public static rodObject RodObjectFromMinimal(Guid uuid, int id, int durability)
    {
        rodObject inventoryRod = (rodObject)ScriptableObject.CreateInstance("rodObject");

        try
        {
            rodObject rod = Array.Find(ItemsInGame.rodsInGame, element => element.id == id);
            inventoryRod.uuid = uuid;
            inventoryRod.id = id;
            inventoryRod.stackable = false;
            inventoryRod.name = rod.name;
            inventoryRod.strength = rod.strength;
            inventoryRod.description = rod.description;
            inventoryRod.sprite = rod.sprite;
            inventoryRod.durabilityIsInfinite = (durability == -1) ? true : false;
            inventoryRod.throwIns = durability;
        }
        catch (Exception err)
        {
            Debug.LogWarning($"{inventoryRod} could not be generated from minimal \n {err}");
            return null;
        }

        return inventoryRod;
    }

    public static baitObject BaitObjectFromMinimal(Guid uuid, int id, int amount)
    {
        baitObject inventoryBait = (baitObject)ScriptableObject.CreateInstance("baitObject");

        try
        {
            baitObject bait = Array.Find(ItemsInGame.baitsInGame, element => element.id == id);
            inventoryBait.uuid = uuid;
            inventoryBait.id = id;
            inventoryBait.baitType = bait.baitType;
            inventoryBait.stackable = true;
            inventoryBait.durabilityIsInfinite = (amount == -1) ? true : false;
            inventoryBait.throwIns = amount;
            inventoryBait.name = bait.name;
            inventoryBait.description = bait.description;
            inventoryBait.sprite = bait.sprite;
        }
        catch (Exception err) 
        {
            Debug.LogWarning($"{inventoryBait} could not be generated from minimal \n {err}");
            return null;
        }
        return inventoryBait;
    }

    public static FishObject FishObjectFromMinimal(int id, int amount)
    {
        FishObject inventoryFish = (FishObject)ScriptableObject.CreateInstance("FishObject");

        try
        {
            FishConfiguration fish = Array.Find(ItemsInGame.fishesInGame, element => element.id == id);
            inventoryFish.uuid = Guid.Empty;
            inventoryFish.id = id;
            inventoryFish.name = fish.name;
            inventoryFish.description = fish.description;
            inventoryFish.sprite = fish.fishImage;
            inventoryFish.amount = amount;
            inventoryFish.stackable = true;
        }
        catch (Exception err) 
        {
            Debug.LogWarning($"{inventoryFish} could not be generated from minimal \n {err}");
            return null;
        }
        return inventoryFish;
    }
}

public static class ItemObjectSerializer
{
    const byte ROD = 1;
    const byte BAIT = 2;
    const byte FISH = 3;
    
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


    public static void WriteItemObject(this NetworkWriter writer, ItemObject item)
    {
        if (item is rodObject rod)
        {
            writer.WriteByte(ROD);
            writer.WriteGuid(rod.uuid);
            writer.WriteInt(rod.id);
            writer.WriteInt(rod.throwIns);
        }
        else if (item is baitObject bait)
        {
            writer.WriteByte(BAIT);
            writer.WriteGuid(bait.uuid);
            writer.WriteInt(bait.id);
            writer.WriteInt(bait.throwIns);
        }
        else if (item is FishObject fish)
        {
            writer.WriteByte(FISH);
            writer.WriteInt(fish.id);
            writer.WriteInt(fish.amount);
        }
    }

    public static ItemObject ReadItemObject(this NetworkReader reader)
    {
        byte type = reader.ReadByte();
        switch (type)
        {
            case ROD:
                {
                    Guid uuid = reader.ReadGuid();
                    int id = reader.ReadInt();
                    int throwIns = reader.ReadInt();
                    return ItemObjectGenerator.RodObjectFromMinimal(uuid, id, throwIns);
                }
            case BAIT:
                {
                    Guid uuid = reader.ReadGuid();
                    int id = reader.ReadInt();
                    int throwIns = reader.ReadInt();
                    return ItemObjectGenerator.BaitObjectFromMinimal(uuid, id, throwIns);
                }
            case FISH:
                {
                    int id = reader.ReadInt();
                    int amount = reader.ReadInt();
                    return ItemObjectGenerator.FishObjectFromMinimal(id, amount);
                }
            default:
                throw new Exception($"Invalid item type {type}");
        }
    }
}

public static class RodObjectReaderWriter
{
    public static void WriteRodObject(this NetworkWriter writer, rodObject rod)
    {
        writer.WriteGuid(rod.uuid);
        writer.WriteInt(rod.id);
        writer.WriteInt(rod.throwIns);
    }

    public static rodObject ReadRodObject(this NetworkReader reader)
    {
        Guid uuid = reader.ReadGuid();
        int id = reader.ReadInt();
        int throwIns = reader.ReadInt();
        return ItemObjectGenerator.RodObjectFromMinimal(uuid, id, throwIns);
    }
}

public static class BaitObjectReaderWriter
{
    public static void WriteBaitObject(this NetworkWriter writer, baitObject bait)
    {
        writer.WriteGuid(bait.uuid);
        writer.WriteInt(bait.id);
        writer.WriteInt(bait.throwIns);
    }

    public static baitObject ReadBaitObject(this NetworkReader reader)
    {
        Guid uuid = reader.ReadGuid();
        int id = reader.ReadInt();
        int throwIns = reader.ReadInt();
        return ItemObjectGenerator.BaitObjectFromMinimal(uuid, id, throwIns);
    }
}