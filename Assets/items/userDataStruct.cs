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
    public Friend[] friends;
    public FriendRequest[] friend_requests;
    public ActiveEffect[] active_effects;
    
    public readonly Guid? SelectedRod
    {
        get
        {
            return string.IsNullOrEmpty(selected_rod) ? null : Guid.Parse(selected_rod);
        }
    }
    public readonly Guid? SelectedBait => string.IsNullOrEmpty(selected_bait) ? null : Guid.Parse(selected_bait);
    
    [Serializable]
    public struct ActiveEffect
    {
        public int item_id;          // ItemDefinition ID that created this effect
        public string expiry_time;   // DateTime as ISO 8601 string
        
        public readonly DateTime ExpiryTime => DateTimeOffset.Parse(expiry_time).UtcDateTime;
    }
    
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
        public string item_uuid;      // Unique instance ID (Guid as string)
        public int definition_id;     // ItemDefinition ID
        public string state_blob;     // Base64-encoded state dictionary

        public Guid ItemUuid => string.IsNullOrEmpty(item_uuid) ? Guid.Empty : Guid.Parse(item_uuid);
    }
    
    [Serializable]
    public struct MailEntry
    {
        string mail_id;
        public string title;
        public string message;
        
        public Guid MailID => Guid.Parse(mail_id);
    }

    [Serializable]
    public struct Friend
    {
        public string user_one;
        public string user_two;
        
        public Guid UserOne => Guid.Parse(user_one);
        public Guid UserTwo => Guid.Parse(user_two);
    }

    [Serializable]
    public struct FriendRequest
    {
        public string user_one;
        public string user_two;
        public string request_sender_id;
        
        public Guid UserOne => Guid.Parse(user_one);
        public Guid UserTwo => Guid.Parse(user_two);
        public Guid RequestSenderId => Guid.Parse(request_sender_id);
    }
}
#nullable disable