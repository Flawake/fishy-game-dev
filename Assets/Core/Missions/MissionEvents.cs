/// <summary>
/// Marker interface for anything that can drive mission progress.
/// </summary>
public interface IMissionEvent { }

/// <summary>Raised once for every fish the player reels in.</summary>
public readonly struct FishCaughtEvent : IMissionEvent
{
    public readonly int FishID;

    public FishCaughtEvent(int fishID)
    {
        FishID = fishID;
    }
}

/// <summary>
/// Raised when the fishdex grows. Carries the absolute total rather than a
/// delta, so a mission can recover the right progress even if it was started
/// after the player had already discovered some species.
/// </summary>
public readonly struct FishdexUpdatedEvent : IMissionEvent
{
    public readonly int TotalSpeciesDiscovered;

    public FishdexUpdatedEvent(int totalSpeciesDiscovered)
    {
        TotalSpeciesDiscovered = totalSpeciesDiscovered;
    }
}

/// <summary>Raised once per completed trade. Carries no data (yet).</summary>
public readonly struct TradeMadeEvent : IMissionEvent { }

/// <summary>Raised once per new friend. Carries no data (yet).</summary>
public readonly struct FriendMadeEvent : IMissionEvent { }

/// <summary>Raised when items enter the player's inventory.</summary>
public readonly struct ItemCollectedEvent : IMissionEvent
{
    public readonly int ItemID;
    public readonly int Amount;

    public ItemCollectedEvent(int itemID, int amount)
    {
        ItemID = itemID;
        Amount = amount;
    }
}
