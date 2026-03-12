using System;
using System.Collections.Generic;
using Unity.VisualScripting;

#nullable enable
// Authenticate requests
[Serializable]
public class LoginRequest
{
    public string username = string.Empty;
    public string password = string.Empty;
}

// player data requests
[Serializable]
public class RetreiveDataRequest
{
    public string user_id = string.Empty;
}

// Inventory requests
[Serializable]
public class AddItemRequest
{
    public string user_id = string.Empty;
    public int item_id = 0;
    public string item_uid = string.Empty;
    public int amount = 0;
    public int cell_id = 0;
}

[Serializable]
public class BuyItemRequest
{
    public string buyer_id = string.Empty;
    public int item_def_id = 0;
    public string item_uuid = string.Empty;
    public string item_state_blob = string.Empty;
    public int item_price = 0;
    public string bought_using = string.Empty;
}

[Serializable]
public class FishToSell {
    public string fish_uid = string.Empty;
    public int fish_id = 0;
    public string? new_state_blob = null;
}

[Serializable]
public class SellFishesRequest {
    public string seller_id = string.Empty;
    public List<FishToSell> fishes = new List<FishToSell>();
    public int price = 0;
}

[Serializable]
public class AddOrUpdateItemRequest {
    public string user_id = string.Empty;
    public string item_uuid = string.Empty;
    public int definition_id = 0;
    public string state_blob = string.Empty; // base64
}

[Serializable]
public class DegradeItemRequest
{
    public string user_id = string.Empty;
    public string item_uid = string.Empty;
    public int amount = 0;
}

[Serializable]
public class IncreaseItemRequest
{
    public string user_id = string.Empty;
    public string item_uid = string.Empty;
    public int amount;
}

[Serializable]
public class DestroyItemRequest
{
    public string user_id = string.Empty;
    public string item_uid = string.Empty;
}

// Mail requests
[Serializable]
public class CreateMailRequest
{
    public string mail_id = string.Empty;
    public string sender_id = string.Empty;
    public string[] receiver_ids = new string[0];
    public string title = string.Empty;
    public string message = string.Empty;
}

[Serializable]
public class DeleteMailRequest
{
    public string user_id = string.Empty;
    public string mail_id = string.Empty;
}

[Serializable]
public class ReadMailRequest
{
    public string user_id = string.Empty;
    public string mail_id = string.Empty;
    public bool read = false;
}

[Serializable]
public class ArchiveMailRequest
{
    public string user_id = string.Empty;
    public string mail_id = string.Empty;
    public bool archived = false;
}


// Change stats requests
[Serializable]
public class SelectItemRequest
{
    public string user_id = string.Empty;
    public string item_uid = string.Empty;
    public string item_type = string.Empty;
}
[Serializable]
public class AddXPRequest
{
    public string user_id = string.Empty;
    public int amount = 0;
}

[Serializable]
public class ChangeBucksRequest
{
    public string user_id = string.Empty;
    public int amount = 0;
}

[Serializable]
public class ChangeCoinsRequest
{
    public string user_id = string.Empty;
    public int amount = 0;
}

[Serializable]
public class AddPlayTimeRequest
{
    public string user_id = string.Empty;
    public int amount = 0;
}

[Serializable]
public class AddFishRequest
{
    public string user_id = string.Empty;
    public int length = 0;
    public int fish_id = 0;
    public int bait_id = 0;
    public int area_id = 0;
    public int xp_earned = 0;
}

[Serializable]
public class TradeItemRequest {
    public string item_uid = string.Empty;
    public int item_id;
    public string state_blob = string.Empty;
}


[Serializable]
public class TradeRequest
{
    public string user_one_id = string.Empty;
    public string user_two_id = string.Empty;
    public List<TradeItemRequest> user_one_receives = new List<TradeItemRequest>();
    public List<TradeItemRequest> user_two_receives = new List<TradeItemRequest>();
    public int user_one_bucks_received;
    public int user_two_bucks_received;
}

[Serializable]
public class CreateFriendRequest
{
    public string user_one = string.Empty;
    public string user_two = string.Empty;
    public string sender_id = string.Empty;
}

[Serializable]
public class HandleFriendRequest
{
    public string user_one = string.Empty;
    public string user_two = string.Empty;
    public bool request_accepted = false;
}

[Serializable]
public class RemoveFriendRequest
{
    public string user_one = string.Empty;
    public string user_two = string.Empty;
}

// user requests
[Serializable]
public class CreateUserRequest
{
    public string email = string.Empty;
    public string username = string.Empty;
    public string password = string.Empty;
}

// Active Effects requests
[Serializable]
public class AddActiveEffectRequest
{
    public string user_id = string.Empty;
    public int item_id = 0;          // ItemDefinition ID that created this effect
    public string expiry_time= string.Empty;   // DateTime as ISO 8601 string
}

[Serializable]
public class RemoveActiveEffectRequest
{
    public string user_id = string.Empty;
    public int item_id = 0;          // ItemDefinition ID to identify which effect to remove
}

[Serializable]
public class RemoveExpiredEffectsRequest
{
    public string user_id = string.Empty;
    public int item_id = 0;
}

#nullable disable
