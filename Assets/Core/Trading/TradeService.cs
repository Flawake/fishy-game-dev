namespace TradeSystem
{
    using Mirror;
    using System;
    using System.Collections.Generic;

    public static class TradeService
    {
        // trade request id -> trade request
        // For server: all pending trade requests on the server.
        // For clients: their own incoming requests.
        static readonly Dictionary<TradeId, PendingTradeRequest> pendingRequests = new();

        // requester -> (receiver -> trade id) for O(1) directional lookup
        static readonly Dictionary<PlayerId, Dictionary<PlayerId, TradeId>> pendingByPair = new();

        // running trade id -> running trade
        static readonly Dictionary<TradeId, TradeSession> serverRunningSessions = new();
        // player -> running trade id (each player can only be in one active trade)
        static readonly Dictionary<PlayerId, TradeId> runningByPlayer = new();
        static TradeSession clientRunningSession;

        // ---- Pending trades ----

        public static bool TryGetPending(TradeId tradeId, out PendingTradeRequest trade)
            => pendingRequests.TryGetValue(tradeId, out trade);

        public static bool PendingContainsKey(TradeId tradeId)
            => pendingRequests.ContainsKey(tradeId);

        public static void AddPending(PendingTradeRequest trade)
        {
            TradeId id = trade.tradeId;
            pendingRequests[id] = trade;

            PlayerId requester = trade.requesterId;
            PlayerId receiver = trade.receiverId;

            if (!pendingByPair.TryGetValue(requester, out Dictionary<PlayerId, TradeId> inner))
            {
                inner = new Dictionary<PlayerId, TradeId>();
                pendingByPair[requester] = inner;
            }

            inner[receiver] = id;
        }

        public static bool RemovePending(TradeId tradeId)
        {
            if (!pendingRequests.TryGetValue(tradeId, out PendingTradeRequest trade))
            {
                return false;
            }

            pendingRequests.Remove(tradeId);

            PlayerId requester = trade.requesterId;
            PlayerId receiver = trade.receiverId;

            if (pendingByPair.TryGetValue(requester, out Dictionary<PlayerId, TradeId> inner))
            {
                inner.Remove(receiver);
                if (inner.Count == 0)
                {
                    pendingByPair.Remove(requester);
                }
            }

            return true;
        }

        public static bool TryGetPendingFromTo(PlayerId requesterId, PlayerId receiverId, out PendingTradeRequest trade)
        {

            trade = default;

            if (!pendingByPair.TryGetValue(requesterId, out Dictionary<PlayerId, TradeId> inner))
            {
                return false;
            }

            if (!inner.TryGetValue(receiverId, out TradeId tradeId))
            {
                return false;
            }

            return pendingRequests.TryGetValue(tradeId, out trade);
        }

        public static bool HasPendingRequestFromTo(Guid requesterId, Guid receiverId)
        {
            PlayerId req = requesterId;
            PlayerId rec = receiverId;

            return pendingByPair.TryGetValue(req, out Dictionary<PlayerId, TradeId> inner)
                && inner.ContainsKey(rec);
        }

        // ---- Running trades server ----

        [Server]
        public static bool ServerTryGetRunning(TradeId tradeId, out TradeSession trade)
            => serverRunningSessions.TryGetValue(tradeId, out trade);

        [Server]
        public static bool ServerRunningContainsKey(TradeId tradeId)
            => serverRunningSessions.ContainsKey(tradeId);

        [Server]
        public static void ServerAddRunning(TradeSession trade)
        {
            if (trade.tradeId == Guid.Empty)
            {
                trade.tradeId = Guid.NewGuid();
            }

            TradeId id = trade.tradeId;
            serverRunningSessions[id] = trade;

            PlayerId requester = trade.requesterId;
            PlayerId receiver = trade.receiverId;

            runningByPlayer[requester] = id;
            runningByPlayer[receiver] = id;
        }

        [Server]
        public static void ServerRemoveRunning(TradeId tradeId)
        {
            if (serverRunningSessions.TryGetValue(tradeId, out TradeSession session))
            {
                PlayerId requester = session.requesterId;
                PlayerId receiver = session.receiverId;

                runningByPlayer.Remove(requester);
                runningByPlayer.Remove(receiver);
            }

            serverRunningSessions.Remove(tradeId);
        }

        [Server]
        public static bool ServerIsPlayerTrading(PlayerId playerId)
        {
            return runningByPlayer.ContainsKey(playerId);
        }

        // ---- Running trades client ----

        [Client]
        public static TradeSession ClientGetRunning()
            => clientRunningSession;

        [Client]
        public static void ClientSetRunning(TradeSession trade)
            => clientRunningSession = trade;

        [Client]
        public static void ClientRemoveRunning()
            => clientRunningSession = null;
    }
}

