using Mirror;
using System;
using UnityEngine;

public enum ItemType
{
    rod,
    bait,
    fish,
}

public abstract class ItemObject : ScriptableObject
{
    //uid is only unique for each item on each player, all players
    //can have a item with a uid of one, but one single player can only have on item with a uid of 1
    //Used to hold items apart, handy for selecting and using items.
    public int uid;
    public ItemType type;
    public int id;
    public Sprite sprite;
    [TextArea(15, 20)]
    public string description;
    public bool stackable;
}

public static class ItemObjectGenerator
{
    public static rodObject RodObjectFromMinimal(int uid, int id, int durability)
    {
        rodObject inventoryRod = (rodObject)ScriptableObject.CreateInstance("rodObject");

        try
        {
            inventoryRod.uid = uid;
            inventoryRod.id = id;
            inventoryRod.stackable = false;
            inventoryRod.name = Array.Find(ItemsInGame.rodsInGame, element => element.id == id).name;
            inventoryRod.strength = Array.Find(ItemsInGame.rodsInGame, element => element.id == id).strength;
            inventoryRod.description = Array.Find(ItemsInGame.rodsInGame, element => element.id == id).description;
            inventoryRod.sprite = Array.Find(ItemsInGame.rodsInGame, element => element.id == id).sprite;
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

    public static baitObject BaitObjectFromMinimal(int uid, int id, int amount)
    {
        baitObject inventoryBait = (baitObject)ScriptableObject.CreateInstance("baitObject");

        try
        {
            inventoryBait.uid = uid;
            inventoryBait.id = id;
            inventoryBait.baitType = baitType.dough;
            inventoryBait.stackable = true;
            inventoryBait.durabilityIsInfinite = (amount == -1) ? true : false;
            inventoryBait.throwIns = amount;
            inventoryBait.name = Array.Find(ItemsInGame.baitsInGame, element => element.id == id).name;
            inventoryBait.description = Array.Find(ItemsInGame.baitsInGame, element => element.id == id).description;
            inventoryBait.sprite = Array.Find(ItemsInGame.baitsInGame, element => element.id == id).sprite;
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
            inventoryFish.uid = 0;
            inventoryFish.id = id;
            inventoryFish.name = Array.Find(ItemsInGame.fishesInGame, element => element.id == id).name;
            inventoryFish.description = Array.Find(ItemsInGame.fishesInGame, element => element.id == id).description;
            inventoryFish.sprite = Array.Find(ItemsInGame.fishesInGame, element => element.id == id).fishImage;
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

    public static void WriteItemObject(this NetworkWriter writer, ItemObject item)
    {
        if (item is rodObject rod)
        {
            writer.WriteByte(ROD);
            writer.WriteInt(rod.uid);
            writer.WriteInt(rod.id);
            writer.WriteInt(rod.throwIns);
        }
        else if (item is baitObject bait)
        {
            writer.WriteByte(BAIT);
            writer.WriteInt(bait.uid);
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
                    int uid = reader.ReadInt();
                    int id = reader.ReadInt();
                    int throwIns = reader.ReadInt();
                    return ItemObjectGenerator.RodObjectFromMinimal(uid, id, throwIns);
                }
            case BAIT:
                {
                    int uid = reader.ReadInt();
                    int id = reader.ReadInt();
                    int throwIns = reader.ReadInt();
                    return ItemObjectGenerator.BaitObjectFromMinimal(uid, id, throwIns);
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
        writer.WriteInt(rod.uid);
        writer.WriteInt(rod.id);
        writer.WriteInt(rod.throwIns);
    }

    public static rodObject ReadDateTime(this NetworkReader reader)
    {
        int uid = reader.ReadInt();
        int id = reader.ReadInt();
        int throwIns = reader.ReadInt();
        return ItemObjectGenerator.RodObjectFromMinimal(uid, id, throwIns);
    }
}

public static class BaitObjectReaderWriter
{
    public static void WriteRodObject(this NetworkWriter writer, baitObject bait)
    {
        writer.WriteInt(bait.uid);
        writer.WriteInt(bait.id);
        writer.WriteInt(bait.throwIns);
    }

    //TODO: why is this called DateTime???
    public static baitObject ReadDateTime(this NetworkReader reader)
    {
        int uid = reader.ReadInt();
        int id = reader.ReadInt();
        int throwIns = reader.ReadInt();
        return ItemObjectGenerator.BaitObjectFromMinimal(uid, id, throwIns);
    }
}