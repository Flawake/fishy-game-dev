using System;
using System.Collections.Generic;

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
    public int fish_id;
    public int fish_amount;
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
    public int item_amount;
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

// Herb quest requests
[Serializable]
public class HandInFish
{
    public string fish_uid = string.Empty;
    public int fish_id;
    public int fish_amount;
    public string? new_state_blob = null;
}

[Serializable]
public class CompleteHerbQuestRequest
{
    public string user_id = string.Empty;
    public string herb_quest_id = string.Empty;
    public int reward_coins = 0;
    public List<HandInFish> fishes = new List<HandInFish>();
}

// Records that a player accepted (saw) the Herb quest of a given day, so the game
// can skip Herb's introduction the next time that player talks to him.
[Serializable]
public class AcceptHerbQuestRequest
{
    public string user_id = string.Empty;
    public string herb_quest_id = string.Empty;
}

// One fish species the current Herb quest asks for and how many of it.
[Serializable]
public class HerbQuestFishDto
{
    public int fish_id;
    public int amount;
}

// Response of the herb_quest/current_daily endpoint: the Herb quest that is active
// right now. The database is the single source of truth for this, the game server
// only reads it and never generates a quest itself.
[Serializable]
public class CurrentHerbQuestResponse
{
    // Unique id of this quest (a UUID). Used to detect a new quest.
    public string herb_quest_id = string.Empty;
    // Area enum value (see Area.cs) the quest fishes are caught in / Herb stands in.
    public int area_id;
    // Fishcoins the player receives for handing the quest in.
    public int reward_coins;
    // The fishes (species + amount) the quest asks for.
    public List<HerbQuestFishDto> fishes = new List<HerbQuestFishDto>();
    // When the database will roll over to the next quest (Herb's next move),
    // ISO 8601 / UTC (e.g. "2026-07-10T02:00:00Z"). The game server schedules its
    // next poll for this moment and then retries until a new herb_quest_id is served.
    public string next_move_at = string.Empty;
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

// Competition requests and data structures
[Serializable]
public class CompetitionData
{
    public string competition_id = string.Empty;
    public string competition_type = string.Empty;  // "MostFish", "LargestFish", or "MostItems"
    public int target_fish_id = 0;
    public string start_time = string.Empty;    // DateTime as ISO 8601 string
    public string end_time = string.Empty;      // DateTime as ISO 8601 string
    public string reward_currency = string.Empty; // "COINS" or "BUCKS"
    public int[] prize_pool = new int[0];      // Array of prizes for top N players
    public string created_at = string.Empty;    // DateTime as ISO 8601 string
    public string status = string.Empty;        // "SCHEDULED", "ACTIVE", "COMPLETED"
}

[Serializable]
public class CompetitionResultData
{
    public string result_id = string.Empty;
    public string competition_id = string.Empty;
    public string player_id = string.Empty;
    public int score = 0;
    public string last_updated = string.Empty;  // DateTime as ISO 8601 string
}

[Serializable]
public class GetActiveCompetitionResponse
{
    public CompetitionData competition = null;
}

[Serializable]
public class GetUpcomingCompetitionsResponse
{
    public CompetitionData[] competitions = new CompetitionData[0];
}

[Serializable]
public class SubmitCompetitionScoreRequest
{
    public string competition_id = string.Empty;
    public string player_id = string.Empty;
    public int score = 0;
}

[Serializable]
public class LeaderboardResponse
{
    public string competition_id = string.Empty;
    public CompetitionResultData[] results = new CompetitionResultData[0];
}

#nullable disable
