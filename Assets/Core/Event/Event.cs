using System;
using ItemSystem;
using Mirror;

namespace GlobalCompetitionSystem
{
    public static class CompetitionManager
    {
        [SyncVar]
        private static ICompetition currentCompetition;
        private static readonly SyncList<ICompetition> upcomingCompetitions = new SyncList<ICompetition>();

        [Server]
        public static void StartCompetitions()
        {
            throw new NotImplementedException();
        }
        
        [Server]
        public static void AddUpcomingCompetition<T>(ICompetition<T> competition, DateTime startDate, DateTime endDate)
        {
            upcomingCompetitions.Add(new Competition<T>(competition, startDate, endDate));
        }

        [Server]
        private static void SetCurrenctCompetition(ICompetition competition)
        {
            currentCompetition = competition;
        }

        [Server]
        private static void EndCurrentCompetition()
        {
            throw new NotImplementedException();
        }
        
        public static bool AddToCompetition<T>(T data)
        {
            if (currentCompetition is ICompetition<T> competition)
            {
                return competition.AddToCompetition(data);
            }
            return false;
        }
        
        // Non-generic interface for type erasure
        private interface ICompetition { }
        
        private struct Competition<T> : ICompetition
        {
            private readonly ICompetition<T> _competitionImplementation;
            private readonly DateTime _startDateTime;
            private readonly DateTime _endDateTime;

            public ICompetition<T> CompetitionImplementation => _competitionImplementation;
            public DateTime StartDateTime => _startDateTime;
            public DateTime EndDateTime => _endDateTime;
            
            public Competition(ICompetition<T> competitionImplementation, DateTime start, DateTime end)
            {
                _competitionImplementation = competitionImplementation;
                _startDateTime = start;
                _endDateTime = end;
            }

        }
    }

    public interface ICompetition<in T>
    {
        bool AddToCompetition(T data);
    }

    public class MostFishCompetition : ICompetition<CurrentFish>
    {
        [Server]
        public bool AddToCompetition(CurrentFish competition)
        {
            return false;
        }
    }
    
    public class BiggestFishCompetition : ICompetition<CurrentFish>
    {
        [Server]
        public bool AddToCompetition(CurrentFish competition)
        {
            return false;
        }
    }
    
    public class MostShellsCompetition : ICompetition<ItemObject>
    {
        [Server]
        public bool AddToCompetition(ItemObject competition)
        {
            return false;
        }
    }
}
