namespace TradeSystem
{
    using System;
    using System.Collections.Generic;
    public enum TradeSessionState
    {
        RequesterAccepted,
        ReceiverAccepted,
    }

    public class TradeSession
    {
        public Guid tradeId;

        public Guid requesterId;
        public string requesterName;
        public Guid receiverId;
        public string receiverName;
        public List<TradableItem> requesterTradeItems;
        public List<TradableItem> receiverTradeItems;

        public TradeSessionState State { get; private set; }

        public TradeSession()
        {
            requesterTradeItems = new List<TradableItem>();
            receiverTradeItems = new List<TradableItem>();
            State = default;
        }

        public TradeSession(PendingTradeRequest request)
        {
            tradeId = request.tradeId;
            requesterId = request.requesterId;
            requesterName = request.requesterName;
            receiverId = request.receiverId;
            receiverName = request.receiverName;
        }

        public bool OwnedBy(Guid playerId)
        {
            return requesterId == playerId || receiverId == playerId;
        }
    }
}

