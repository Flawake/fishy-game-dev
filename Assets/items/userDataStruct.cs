using System;

#nullable enable
[Serializable]
public struct UserData
{
    public string name;
    public int xp;
    public int coins;
    public int bucks;
    public ulong total_playtime;
    public Guid? selected_rod;
    public int? selected_bait;
    public FishData[] fish_data;
    public InventoryItem[] inventory_items;
    public MailEntry[] mailbox;
    
    [Serializable]
    public struct FishData
    {
        public int fish_id;
        public int amount;
        public int max_length;
        public DateTime first_caught;
        public int[] areas;
        public int[] baits;
    }
    
    [Serializable]
    public struct InventoryItem
    {
        public int item_id;
        public Guid? item_uuid;
        public ItemType itemType;
        public int amount;
        public int cell_id;
    }
    
    [Serializable]
    public struct MailEntry
    {
        public Guid mail_id;
        public string title;
        public string message;
    }
}
#nullable disable