namespace TradeSystem
{
    using System;
    using System.Collections.Generic;
    using Mirror;

    [Flags]
    public enum TradeSessionState
    {
        None = 0,

        RequesterAccepted = 1 << 0,
        ReceiverAccepted  = 1 << 1,
        RequesterVerified = 1 << 2,
        ReceiverVerified  = 1 << 3,

        AcceptFlags = RequesterAccepted | ReceiverAccepted,
        VerifyFlags  = RequesterVerified  | ReceiverVerified,
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

        public TradeSessionState State;

        public TradeSession() {}

        public TradeSession(PendingTradeRequest request)
        {
            tradeId = request.tradeId;
            requesterId = request.requesterId;
            requesterName = request.requesterName;
            receiverId = request.receiverId;
            receiverName = request.receiverName;
            requesterTradeItems = new List<TradableItem>();
            receiverTradeItems = new List<TradableItem>();
        }
        
        public bool IsOwnedBy(Guid playerId)
        {
            return requesterId == playerId || receiverId == playerId;
        }

        public bool BothPlayersAccepted()
        {
            return (State & TradeSessionState.AcceptFlags) == TradeSessionState.AcceptFlags;
        }

        public bool BothPlayersVerified()
        {
            return (State & TradeSessionState.VerifyFlags) == TradeSessionState.VerifyFlags;
        }
    }

    public static class TradeSessionReaderWriter
    {
        public static void WriteTradeSession(this NetworkWriter writer, TradeSession session)
        {
            writer.WriteGuid(session.tradeId);
            writer.WriteGuid(session.requesterId);
            writer.WriteGuid(session.receiverId);
            writer.WriteString(session.requesterName);
            writer.WriteString(session.receiverName);
            writer.WriteInt(session.requesterTradeItems.Count);
            writer.WriteInt(session.receiverTradeItems.Count);
            foreach (TradableItem item in session.requesterTradeItems)
            {
                writer.WriteTradableItem(item);
            }
            foreach (TradableItem item in session.receiverTradeItems)
            {
                writer.WriteTradableItem(item);
            }
        }
        
        public static TradeSession ReadTradeSession(this NetworkReader reader)
        {
            TradeSession session = new TradeSession
            {
                tradeId = reader.ReadGuid(),
                requesterId = reader.ReadGuid(),
                receiverId = reader.ReadGuid(),
                requesterName = reader.ReadString(),
                receiverName = reader.ReadString()
            };

            int requesterCount = reader.ReadInt();
            int receiverCount = reader.ReadInt();

            session.requesterTradeItems = new List<TradableItem>(requesterCount);
            session.receiverTradeItems = new List<TradableItem>(receiverCount);

            for (int i = 0; i < requesterCount; i++)
            {
                session.requesterTradeItems.Add(reader.ReadTradableItem());
            }

            for (int i = 0; i < receiverCount; i++)
            {
                session.receiverTradeItems.Add(reader.ReadTradableItem());
            }

            return session;
        }
    }
}

