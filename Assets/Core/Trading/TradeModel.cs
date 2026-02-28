namespace TradeSystem
{
    using System;

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
        RequestAccepted,
        TradeStarted,
        TradeCancelled,
        TradeExpired,
    }

    public enum TradeCommandType
    {
        RequestNewTrade,
        AcceptRequest,
        CancelTrade,
    }

    public enum TradeStatus
    {
        None,
        PendingRequest,
        Active,
        Cancelled,
        Completed,
        Expired,
    }

    public struct TradeCommandMessage
    {
        public TradeCommandType Type;

        public Guid TargetPlayerId;

        public Guid TradeId;
    }

    public enum TradeRpcType
    {
        RequestIncoming,
        RequestRemoved,
        TradeStarted,
        TradeCancelled,
        TradeExpired,
    }

    public struct TradeRpcMessage
    {
        public TradeRpcType Type;

        public PendingTradeRequest Pending;

        public TradeSession Session;

        public CancelTradeRequestReason CancelReason;
    }

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
        public TradeStatus Status { get; }

        public Guid TradeId { get; }
        public Guid RequesterId { get; }
        public string RequesterName { get; }
        public Guid ReceiverId { get; }
        public string ReceiverName { get; }

        public TradeViewModel(TradeEventType eventType, TradeStatus status, PendingTradeRequest request)
        {
            EventType = eventType;
            Status = status;

            TradeId = request.tradeId;
            RequesterId = request.requesterId;
            RequesterName = request.requesterName;
            ReceiverId = request.receiverId;
            ReceiverName = request.receiverName;
        }

        public TradeViewModel(TradeEventType eventType, TradeStatus status, TradeSession session)
        {
            EventType = eventType;
            Status = status;

            TradeId = session.tradeId;
            RequesterId = session.requesterId;
            RequesterName = session.requesterName;
            ReceiverId = session.receiverId;
            ReceiverName = session.receiverName;
        }
    }

    public static class TradeEvents
    {
        public static event Action<TradeViewModel> ClientTradeStateChanged;

        public static void RaiseClient(PendingTradeRequest request, TradeEventType type, TradeStatus status)
        {
            ClientTradeStateChanged?.Invoke(new TradeViewModel(type, status, request));
        }

        public static void RaiseClient(TradeSession session, TradeEventType type, TradeStatus status)
        {
            ClientTradeStateChanged?.Invoke(new TradeViewModel(type, status, session));
        }
    }
}

