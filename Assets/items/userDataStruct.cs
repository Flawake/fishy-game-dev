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
    [SerializeField] string? last_completed_herb_quest_id;
    [SerializeField] string? last_accepted_herb_quest_id;

    // Id of the last completed Herb quest, Guid.Empty when never completed
    public readonly Guid LastCompletedHerbQuestId =>
        string.IsNullOrEmpty(last_completed_herb_quest_id) ? Guid.Empty : Guid.Parse(last_completed_herb_quest_id);

    // Id of the last accepted Herb quest, Guid.Empty when never accepted
    public readonly Guid LastAcceptedHerbQuestId =>
        string.IsNullOrEmpty(last_accepted_herb_quest_id) ? Guid.Empty : Guid.Parse(last_accepted_herb_quest_id);

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
        public string sender_name;
        public string title;
        public string message;
        
        public Guid MailID => Guid.Parse(mail_id);
    }

    [Serializable]
    public struct Friend
    {
        public string friend_name;
        public string friend_id;
        
        public string friendName => friend_name;
        public Guid friendId => Guid.Parse(friend_id);
    }

    [Serializable]
    public struct FriendRequest
    {
        public string other_id;
        public string other_name;
        public string request_sender_id;
        public Guid otherId => Guid.Parse(other_id);
        public string otherName => other_name;
        public Guid RequestSenderId => Guid.Parse(request_sender_id);
    }
}
#nullable disable