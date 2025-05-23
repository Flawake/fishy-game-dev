using System;
using UnityEngine;

#nullable enable
[Serializable]
public struct UserData
{
    public string name;
    public int xp;
    public int coins;
    public int bucks;
    public ulong total_playtime;
    [SerializeField] string? selected_rod;
    [SerializeField] string? selected_bait;
    public FishData[] fish_data;
    public InventoryItem[] inventory_items;
    public MailEntry[] mailbox;
    
    public Guid? SelectedRod
    {
        get
        {
            return string.IsNullOrEmpty(selected_rod) ? null : Guid.Parse(selected_rod);
        }
    }
    public Guid? SelectedBait => string.IsNullOrEmpty(selected_bait) ? null : Guid.Parse(selected_bait);
    
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
        public string item_uid;  // public so Unity can deserialize
        public int amount;
        public int cell_id;
    
        public Guid itemUuid
        {
            get
            {
                return string.IsNullOrEmpty(item_uid) ? Guid.Empty : Guid.Parse(item_uid);
            }
        }
    }
    
    [Serializable]
    public struct MailEntry
    {
        string mail_id;
        public string title;
        public string message;
        
        public Guid MailID => Guid.Parse(mail_id);
    }
}
#nullable disable