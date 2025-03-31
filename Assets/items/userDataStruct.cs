using System;

#nullable enable
[Serializable]
public struct UserData
{
    public byte[] uuid;
    public PlayerInventory inventory;
    public PlayerStats stats;
    public bool showInv;
    public int lastItemUID;
    public int selectedRodUid;
    public int selectedBaitId;

    [Serializable]
    public struct PlayerInventory
    {
        //Each item has 2 id's, one wich tells what item it is(id) and one to hold items apart from each other(uid)
        public Rod[]? rods;
        public Bait[]? baits;
        public Fish[]? fishes;
    }

    [Serializable]
    public struct Rod
    {
        public int uid;
        public int id;
        public int durability;
    }

    [Serializable]
    public struct Bait
    {
        public int uid;
        public int id;
        public int amount;
    }

    [Serializable]
    public struct Fish
    {
        public int id;
        public int amount;
    }

    [Serializable]
    public struct PlayerStats
    {
        public int xp;
        public CaughtFish[]? fishes;
        public int coins;
        public int bucks;
        public int biggestFishCm;
        public ulong playtime;
    }

    [Serializable]
    public struct CaughtFish
    {
        public int id;
        public int amount;
        public int maxLength;
    }
}
#nullable disable