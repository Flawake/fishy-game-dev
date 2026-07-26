using System;

#nullable enable

// Every request/response type that the backend actually exposes now comes from
// the generated client in FishyGame.Api (Generated/ApiModels.cs). What is left
// here are structs with no endpoint behind them; they are currently unreferenced
// and can be deleted once you are sure they are not coming back.

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

// Change stats requests
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

// Friend requests
[Serializable]
public class CreateFriendRequest
{
    public string user_one = string.Empty;
    public string user_two = string.Empty;
    public string sender_id = string.Empty;
}

[Serializable]
public class RemoveFriendRequest
{
    public string user_one = string.Empty;
    public string user_two = string.Empty;
}

// Active Effects requests
[Serializable]
public class RemoveActiveEffectRequest
{
    public string user_id = string.Empty;
    public int item_id = 0;          // ItemDefinition ID to identify which effect to remove
}

#nullable disable
