namespace TradeSystem
{
    using System;
    using Mirror;

    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public readonly Guid Value;

        public PlayerId(Guid value) => Value = value;

        public bool Equals(PlayerId other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();

        public static implicit operator Guid(PlayerId id) => id.Value;
        public static implicit operator PlayerId(Guid value) => new PlayerId(value);
    }

    public readonly struct TradeId : IEquatable<TradeId>
    {
        public readonly Guid Value;

        public TradeId(Guid value) => Value = value;

        public bool Equals(TradeId other) => Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is TradeId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();

        public static implicit operator Guid(TradeId id) => id.Value;
        public static implicit operator TradeId(Guid value) => new TradeId(value);
    }

    public enum CancelTradeRequestReason
    {
        AcceptedButUnavailable,
        RequestTimeout,
        ClosedByOther,
    }

    public enum TradingInfoType
    {
        TradeExpired,
        AlreadyTrading,
        ClosedByOther,
    }

    public enum TradeEventType
    {
        RequestCreated,
        RequestCancelled,
        RequestExpired,
        TradeStarted,
        TradeCancelled,
        TradeItemsUpdated,
    }

    public enum TradeStatus
    {
        None,
        PendingRequest,
        Active,
        Cancelled,
        Completed,
        Expired,
        TradeItemsUpdated,
    }

    #region TradeCMD
    public struct TradeCMDRequestNewTrade : NetworkMessage
    {
        public Guid requestTargetID;
    }

    public struct TradeCMDAcceptTradeRequest : NetworkMessage
    {
        public Guid tradeID;
    }

    public struct TradeCMDCancelTrade : NetworkMessage
    {
        public Guid tradeID;
    }

    public struct TradeCMDItemAdded : NetworkMessage
    {
        public Guid tradeID;
        public TradableItem itemAdded;
    }

    #endregion

    #region TradeRPC
    public struct TradeRPCRequestIncoming : NetworkMessage
    {
        public PendingTradeRequest Pending;
    }

    public struct TradeRPCRequestRemoved : NetworkMessage
    {
        public Guid TradeId;
        public CancelTradeRequestReason CancelReason;
    }

    public struct TradeRPCTradeRequestExpired : NetworkMessage
    {
        public Guid PendingID;
    }

    public struct TradeRPCTradeStarted : NetworkMessage
    {
        public TradeSession Session;
    }

    public struct TradeRPCTradeCancelled : NetworkMessage
    {
        
    }

    public struct TradeRPCTradeitemAdded : NetworkMessage
    {
        public TradableItem addedItem;
    }
    #endregion

    public struct PendingTradeRequest
    {
        public Guid tradeId;

        public DateTime requestTime;
        public Guid requesterId;
        public string requesterName;
        public Guid receiverId;
        public string receiverName;

        public bool Owns(Guid playerId)
        {
            return requesterId == playerId || receiverId == playerId;
        }
    }

    public class TradeViewModel
    {
        public TradeEventType EventType { get; }
        public Guid TradeId { get; }

        public TradeViewModel(TradeEventType eventType, Guid tradeID)
        {
            EventType = eventType;
            TradeId = tradeID;
        }
    }

    public static class TradeEvents
    {
        public static event Action<TradeViewModel> ClientTradeStateChanged;

        public static void RaiseClient(TradeEventType type, Guid tradeID)
        {
            ClientTradeStateChanged?.Invoke(new TradeViewModel(type, tradeID));
        }
    }
}
