#nullable enable
[System.Serializable]
public struct UserData
{
    public string user;
    public PlayerInventory inventory;
    public PlayerStats stats;
    public bool showInv;
    public int lastItemUID;
    public int selectedRodUid;
    public int selectedBaitUid;

    [System.Serializable]
    public struct PlayerInventory
    {
        //Each item has 2 id's, one wich tells what item it is(id) and one to hold items apart from each other(uid)
        public Rod[]? rods;
        public Bait[]? baits;
        public Fish[]? fishes;
    }

    [System.Serializable]
    public struct Rod
    {
        public int uid;
        public int id;
        public int durability;
    }

    [System.Serializable]
    public struct Bait
    {
        public int uid;
        public int id;
        public int amount;
    }

    [System.Serializable]
    public struct Fish
    {
        public int id;
        public int amount;
    }

    [System.Serializable]
    public struct PlayerStats
    {
        public int xp;
        public CaughtFish[]? caughtFishes;
        public int coins;
        public int bucks;
    }

    [System.Serializable]
    public struct CaughtFish
    {
        public int id;
        public int amount;
        public int maxLength;
    }
}
#nullable disable