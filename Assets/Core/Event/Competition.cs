using System;
using System.Collections.Generic;
using Mirror;

namespace GlobalCompetitionSystem
{
    public struct Competition
    {
        private readonly ICompetitionState _competitionState;
        private readonly DateTime _startDateTime;
        private readonly DateTime _endDateTime;
        private StoreManager.CurrencyType _rewardCurrency;
        // index 0 is the prize for first place, etc...
        private List<int> _prizepool;
        public ICompetitionState CompetitionState => _competitionState;
        public DateTime StartDateTime => _startDateTime;
        public DateTime EndDateTime => _endDateTime;
        public StoreManager.CurrencyType RewardCurrency => _rewardCurrency;
        public List<int> Prizepool => _prizepool;
            
        public Competition(ICompetitionState competitionState, DateTime start, DateTime end, StoreManager.CurrencyType rewardCurrency, List<int> prizepool)
        {
            _competitionState = competitionState;
            _startDateTime = start;
            _endDateTime = end;
            _rewardCurrency = rewardCurrency;
            _prizepool = prizepool;
        }
    }
    
    public struct CurrentCompetitionData
    {
        private Competition RunningCompetition;
        private readonly SyncList<PlayerResult> results;

        public CurrentCompetitionData(Competition runningCompetition)
        {
            RunningCompetition = runningCompetition;
            results = new SyncList<PlayerResult>();
        }
    }
    
    public struct PlayerResult
    {
        private readonly Guid _playerID;
        public string playerName;
        public int result;

        public Guid PlayerID => _playerID;
            
        PlayerResult(Guid playerID, string playerName, int result)
        {
            _playerID = playerID;
            this.playerName = playerName;
            this.result = result;
        }
    }

    public static class CompetitionManager
    {
        [SyncVar] private static CurrentCompetition _currentCompetition;
        private static readonly SyncList<Competition> _upcomingCompetitions = new SyncList<Competition>();

        public static CurrentCompetition GetCurrentCompetition()
        {
            return _currentCompetition;
        }

        public static SyncList<Competition> getUpcomingCompetitions()
        {
            return _upcomingCompetitions;
        }

        [Server]
        public static void StartCompetitions()
        {
            throw new NotImplementedException();
        }

        [Server]
        public static void AddUpcomingCompetition(ICompetitionState competitionState, DateTime startDate,
            DateTime endDate, StoreManager.CurrencyType rewardCurrency, List<int> rewardDistribution)
        {
            _upcomingCompetitions.Add(new Competition(competitionState, startDate, endDate, rewardCurrency,
                rewardDistribution));
        }

        [Server]
        private static void SetCurrentCompetition(Competition metadata)
        {
            CurrentCompetition currentCompetition = CompetitionStateRegistry.GetImplementation(metadata.CompetitionState);
            currentCompetition.SetState(metadata.CompetitionState);
            currentCompetition.CompetitionData = new CurrentCompetitionData(metadata);
            _currentCompetition = currentCompetition;
        }

        [Server]
        private static void EndCurrentCompetition()
        {
            throw new NotImplementedException();
        }

        public static bool AddToRunningCompetition<T>(T data)
        {
            if (_currentCompetition is CurrentCompetition<T> competition)
            {
                return competition.AddToCompetition(data);
            }

            return false;
        }
    }

    // Non-generic interfaces for type erasure
    public abstract class CurrentCompetition
    {
        public CurrentCompetitionData CompetitionData { get; set; }
        public abstract void SetState(ICompetitionState state);
    }

    public abstract class CurrentCompetition<T> : CurrentCompetition
    {
        ICompetitionState State { get; set; }
        public abstract bool AddToCompetition(T data);
    }
}
