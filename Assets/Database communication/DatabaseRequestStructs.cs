using System;
using System.Diagnostics.CodeAnalysis;
#nullable enable
#pragma warning disable CS8618
// Authenticate requests
[Serializable]
public class LoginRequest
{
    public string username;
    public string password;
}

// player data requests
[Serializable]
public class RetreiveDataRequest
{
    public string user_id;
}

// Inventory requests
[Serializable]
public class AddItemRequest
{
    public string user_id;
    public int item_id;
    public string? item_uid;
    public int amount;
    public int cell_id;
}

[Serializable]
public class DegradeItemRequest
{
    public string user_id;
    public int item_id;
    public string? item_uid;
    public int amount;
}

[Serializable]
public class IncreaseItemRequest
{
    public string user_id;
    public int item_id;
    public string? item_uid;
    public int amount;
}

[Serializable]
public class DestroyItemRequest
{
    public string user_id;
    public int item_id;
    public string? item_uid;
}

// Mail requests
[Serializable]
public class CreateMailRequest
{
    public string mail_id;
    public string sender_id;
    public string[] receiver_ids;
    public string title;
    public string message;
}

[Serializable]
public class DeleteMailRequest
{
    public string user_id;
    public string mail_id;
}

[Serializable]
public class ReadMailRequest
{
    public string user_id;
    public string mail_id;
    public bool read;
}

[Serializable]
public class ArchiveMailRequest
{
    public string user_id;
    public string mail_id;
    public bool archived;
}


// Change stats requests
[Serializable]
public class SelectItemRequest
{
    public string user_id;
    public int item_id;
    public string? item_uid;
    public ItemType item_type;
}
[Serializable]
public class AddXPRequest
{
    public string user_id;
    public int amount;
}

[Serializable]
public class ChangeBucksRequest
{
    public string user_id;
    public int amount;
}

[Serializable]
public class ChangeCoinsRequest
{
    public string user_id;
    public int amount;
}

[Serializable]
public class AddPlayTimeRequest
{
    public string user_id;
    public int amount;
}

[Serializable]
public class AddFishRequest
{
    public string user_id;
    public int length;
    public int fish_id;
    public int bait_id;
    public int area_id;
}

// user requests
[Serializable]
public class CreateUserRequest
{
    public string email;
    public string username;
    public string password;
}
#nullable disable
#pragma warning restore CS8618
