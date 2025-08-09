using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace GlobalCompetitionSystem
{
    public class Competition
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
        public Competition RunningCompetition { get; }
        private readonly SortedDictionary<int, List<PlayerResult>> _results;
        private readonly Dictionary<Guid, int> _playerScoreLookup;


        public CurrentCompetitionData(Competition runningCompetition)
        {
            RunningCompetition = runningCompetition;
            _results = new SortedDictionary<int, List<PlayerResult>>();
            _playerScoreLookup = new Dictionary<Guid, int>();
        }

        public void AddOrUpdateResult(Guid playerId, string playerName, int newResult)
        {
            if (_playerScoreLookup.TryGetValue(playerId, out int oldResult))
            {
                List<PlayerResult> playersAtOldScore = _results[oldResult];
                PlayerResult playerObject = playersAtOldScore.First(p => p.PlayerID == playerId);

                // Update the position in Results first
                if (oldResult != newResult)
                {
                    playersAtOldScore.Remove(playerObject);
                    if (playersAtOldScore.Count == 0)
                    {
                        _results.Remove(oldResult);
                    }

                    if (!_results.ContainsKey(newResult))
                    {
                        _results[newResult] = new List<PlayerResult>();
                    }
                    _results[newResult].Add(playerObject);
                }
                
                // Then update the class itself
                playerObject.PlayerName = playerName;
                playerObject.Result = newResult;
                _playerScoreLookup[playerId] = newResult;
            }
            else
            {
                if (!_results.ContainsKey(newResult))
                {
                    _results[newResult] = new List<PlayerResult>();
                }
            
                PlayerResult newPlayerResult = new PlayerResult(playerId, playerName, newResult);
                _results[newResult].Add(newPlayerResult);
                _playerScoreLookup[playerId] = newResult;
            }
        }

        public List<PlayerResult> GetTopPerformers(int amount)
        {
            List<PlayerResult> topPlayers = new List<PlayerResult>(amount);

            foreach (var kvp in _results.Reverse())
            {
                foreach (var player in kvp.Value)
                {
                    topPlayers.Add(player);
                    if (topPlayers.Count >= amount)
                    {
                        return topPlayers;
                    }
                }
            }
            return topPlayers;
        }

        public PlayerResult GetPlayerResult(Guid playerID)
        {
            if (_playerScoreLookup.TryGetValue(playerID, out int score))
            {
                return _results[score].First(r => r.PlayerID == playerID);
            }
            return null;
        }
    }
    
    public class PlayerResult
    {
        private readonly Guid _playerID;
        public string PlayerName;
        public int Result;

        public Guid PlayerID => _playerID;
            
        public PlayerResult(Guid playerID, string playerName, int result)
        {
            _playerID = playerID;
            PlayerName = playerName;
            Result = result;
        }
    }
    
    class CompetitionStartDateComparer : IComparer<Competition>
    {
        public int Compare(Competition x, Competition y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            return x.StartDateTime.CompareTo(y.StartDateTime);
        }
    }

    public static class CompetitionManager
    {
        [SyncVar] private static CurrentCompetition _currentCompetition;
        private static readonly SyncSortedSet<Competition> _upcomingCompetitions = new SyncSortedSet<Competition>(new CompetitionStartDateComparer());

        public static CurrentCompetition GetCurrentCompetition()
        {
            return _currentCompetition;
        }

        public static SyncSortedSet<Competition> GetUpcomingCompetitions()
        {
            return _upcomingCompetitions;
        }

        [Server]
        public static IEnumerator UpdateCompetitions()
        {
            while (true)
            {
                if (_currentCompetition == null)
                {
                    if (_upcomingCompetitions.Count > 0)
                    {
                        Competition nextCompetition = _upcomingCompetitions.First();
                        if (nextCompetition.StartDateTime >= DateTime.Now)
                        {
                            SetCurrentCompetition(nextCompetition);
                            _upcomingCompetitions.Remove(nextCompetition);
                        }
                    }
                }
                if (_currentCompetition != null && _currentCompetition.CompetitionData.RunningCompetition.EndDateTime < DateTime.UtcNow)
                {
                    EndCurrentCompetition();
                    _currentCompetition = null;
                }
                yield return new WaitForSeconds(1);
            }
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
            CurrentCompetition currentCompetition =
                CompetitionStateRegistry.GetImplementation(metadata.CompetitionState);
            currentCompetition.SetState(metadata.CompetitionState);
            currentCompetition.CompetitionData = new CurrentCompetitionData(metadata);
            _currentCompetition = currentCompetition;
        }

        [Server]
        private static void EndCurrentCompetition()
        {
            DistributePrizes();
            MailResults();
        }

        [Server]
        private static void DistributePrizes()
        {
            List<int> prizes = _currentCompetition.CompetitionData.RunningCompetition.Prizepool;
            List<PlayerResult> winners = _currentCompetition.CompetitionData.GetTopPerformers(prizes.Count);
            for (int i = 0; i < winners.Count; i++)
            {
                PlayerResult winner = winners[i];
                bool prizeGiven = false;
                
                if (GameNetworkManager.connUUID.TryGetValue(winner.PlayerID, out NetworkConnectionToClient playerConnection))
                {
                    PlayerDataSyncManager syncManager = playerConnection.identity.GetComponent<PlayerDataSyncManager>();
                    if (syncManager != null)
                    {
                        switch (_currentCompetition.CompetitionData.RunningCompetition.RewardCurrency)
                        {
                            case StoreManager.CurrencyType.bucks:
                                syncManager.ChangeFishBucksAmount(prizes[i], true);
                                break;
                            case StoreManager.CurrencyType.coins:
                                syncManager.ChangeFishCoinsAmount(prizes[i], true);
                                break;
                            default:
                                throw new NotSupportedException($"Currency type {_currentCompetition.CompetitionData.RunningCompetition.RewardCurrency} has not yet been implemented as a reward");
                        }
                        prizeGiven = true;
                    }
                }
                
                if(!prizeGiven)
                {
                    switch (_currentCompetition.CompetitionData.RunningCompetition.RewardCurrency)
                    {
                        case StoreManager.CurrencyType.bucks:
                            DatabaseCommunications.ChangeFishBucksAmount(prizes[i], winner.PlayerID);
                            break;
                        case StoreManager.CurrencyType.coins:
                            DatabaseCommunications.ChangeFishCoinsAmount(prizes[i], winner.PlayerID);
                            break;
                        default:
                            throw new NotSupportedException($"Currency type {_currentCompetition.CompetitionData.RunningCompetition.RewardCurrency} has not yet been implemented as a reward");
                    }
                }
            }
        }

        [Server]
        public static void MailResults()
        {
         throw new NotImplementedException();   
        }

        public static bool AddToRunningCompetition<T>(T data, PlayerData playerData)
        {
            if (_currentCompetition is CurrentCompetition<T> competition)
            {
                return competition.AddToCompetition(data, playerData);
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
        public abstract bool AddToCompetition(T data, PlayerData playerData);
    }
}
