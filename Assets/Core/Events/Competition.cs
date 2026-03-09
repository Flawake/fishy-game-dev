using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GlobalCompetitionSystem;
using Mirror;
using NUnit.Framework;
using UnityEngine;

namespace GlobalCompetitionSystem
{
    /// <summary>
    /// Competition type enum matching backend values
    /// </summary>
    public enum CompetitionType
    {
        MostFish = 1,
        LargestFish = 2,
        MostItems = 3
    }

    public class Competition
    {
        private readonly Guid _competitionId;
        private readonly ICompetitionState _competitionState;
        private readonly DateTime _startDateTime;
        private readonly DateTime _endDateTime;
        private StoreManager.CurrencyType _rewardCurrency;
        // index 0 is the prize for first place, etc...
        private List<int> _prizepool;
        public Guid CompetitionId => _competitionId;
        public ICompetitionState CompetitionState => _competitionState;
        public DateTime StartDateTime => _startDateTime;
        public DateTime EndDateTime => _endDateTime;
        public StoreManager.CurrencyType RewardCurrency => _rewardCurrency;
        public List<int> Prizepool => _prizepool;
            
        public Competition(Guid competitionId, ICompetitionState competitionState, DateTime start, DateTime end, StoreManager.CurrencyType rewardCurrency, List<int> prizepool)
        {
            _competitionId = competitionId;
            _competitionState = competitionState;
            _startDateTime = start;
            _endDateTime = end;
            _rewardCurrency = rewardCurrency;
            _prizepool = prizepool;
        }
        
        /// <summary>
        /// Creates a Competition instance from backend CompetitionData
        /// </summary>
        public static Competition FromBackendData(CompetitionData backendData)
        {
            // Parse competition ID
            Guid competitionId = Guid.Parse(backendData.competition_id);
            
            // Parse competition type from string to enum
            CompetitionType competitionType = Enum.Parse<CompetitionType>(backendData.competition_type);
            
            // Map competition type to ICompetitionState
            ICompetitionState competitionState = competitionType switch
            {
                CompetitionType.MostFish => new MostFishCompetitonState 
                { 
                    specificFish = backendData.target_fish_id > 0, 
                    fishIDToCatch = backendData.target_fish_id 
                },
                CompetitionType.LargestFish => new largestFishCompetitonState 
                { 
                    specificFish = backendData.target_fish_id > 0, 
                    fishIDToCatch = backendData.target_fish_id 
                },
                CompetitionType.MostItems => new MostItemsCompetitonState 
                { 
                    ItemId = backendData.target_fish_id  // target_fish_id is used for item ID in this case
                },
                _ => throw new ArgumentException($"Unknown competition type: {competitionType} ({backendData.competition_type})")
            };
            
            // Parse date times (backend sends ISO 8601 UTC strings)
            DateTime startTime = DateTime.Parse(backendData.start_time).ToUniversalTime();
            DateTime endTime = DateTime.Parse(backendData.end_time).ToUniversalTime();
            
            // Parse currency enum from string
            StoreManager.CurrencyType currency = Enum.Parse<StoreManager.CurrencyType>(backendData.reward_currency);
            
            // Convert prize pool array to list
            List<int> prizePool = new List<int>(backendData.prize_pool);
            
            return new Competition(competitionId, competitionState, startTime, endTime, currency, prizePool);
        }
    }
    
    public struct CurrentCompetitionData
    {
        public Competition RunningCompetition { get; }
        // Updated once in a while, might take quite a bit of server load when the rankings contain a lot of players, so this is most of the time not up to date
        private readonly Dictionary<Guid, int> _playerRanking;
        // int -> score, List<PlayerResult> -> all players with that score
        private readonly SortedDictionary<int, List<PlayerResult>> _results;
        private readonly Dictionary<Guid, int> _playerScoreLookup;


        public CurrentCompetitionData(Competition runningCompetition)
        {
            RunningCompetition = runningCompetition;
            _playerRanking = new Dictionary<Guid, int>(100);
            _results = new SortedDictionary<int, List<PlayerResult>>();
            _playerScoreLookup = new Dictionary<Guid, int>(100);
        }

        public void UpdatePlayerRankings()
        {
            _playerRanking.Clear();
            int currentRank = 1;
            foreach (var scoreGroup in _results.Reverse())
            {
                foreach (var result in scoreGroup.Value)
                {
                    _playerRanking[result.PlayerID] = currentRank;
                    currentRank++;
                }
            }
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

        public SortedList<int, PlayerResult> GetTopPerformers(int amount)
        {
            var topPlayers = new SortedList<int, PlayerResult>(amount);

            foreach (var (player, index) in _results
                         .Reverse()
                         .SelectMany(kvp => kvp.Value)
                         .Take(amount)
                         .Select((player, index) => (player, index)))
            {
                topPlayers.Add(index + 1, player);
            }

            return topPlayers;
        }


        public (int, PlayerResult) GetPlayerResult(Guid playerID)
        {
            if (_playerScoreLookup.TryGetValue(playerID, out int score))
            {
                if (_playerRanking.TryGetValue(playerID, out int rank))
                {
                    return (rank, _results[score].First(r => r.PlayerID == playerID));
                }
            }
            return (0, null);
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
        private static readonly HashSet<Guid> _loadedCompetitionIds = new HashSet<Guid>();
        private static DateTime _lastBackendPoll = DateTime.MinValue;
        private static readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(5); // Poll backend every 5 minutes

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
            DateTime lastRankingRefresh = DateTime.MinValue;
            // hours, minute, seconds
            TimeSpan timeBetweenRankingRebuilds = new TimeSpan(0, 1, 0);
            
            // Initial poll on startup (after short delay)
            yield return new WaitForSeconds(2);
            FetchCompetitionsFromBackend();
            
            while (true)
            {
                // Poll backend for new competitions periodically
                if (DateTime.UtcNow - _lastBackendPoll > _pollInterval)
                {
                    FetchCompetitionsFromBackend();
                    _lastBackendPoll = DateTime.UtcNow;
                }
                
                if (_currentCompetition == null)
                {
                    if (_upcomingCompetitions.Count > 0)
                    {
                        Competition nextCompetition = _upcomingCompetitions.First();
                        if (nextCompetition.StartDateTime <= DateTime.UtcNow)
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

                if (_currentCompetition != null && DateTime.UtcNow - lastRankingRefresh > timeBetweenRankingRebuilds)
                {
                    _currentCompetition.CompetitionData.UpdatePlayerRankings();
                    lastRankingRefresh = DateTime.UtcNow;
                }
                yield return new WaitForSeconds(1);
            }
        }

        [Server]
        private static void FetchCompetitionsFromBackend()
        {
            Debug.Log("[CompetitionManager] Fetching competitions from backend...");
            
            // Fetch active competition
            DatabaseCommunications.GetActiveCompetition((response) =>
            {
                if (response != null && response.competitions != null)
                {
                    if (response.competitions.Length == 0)
                    {
                        Debug.Log("[CompetitionManager] No active competitions from backend");
                    }
                    else if (response.competitions.Length == 1)
                    {
                        Debug.Log("[CompetitionManager] Received 1 active competition from backend");
                        ProcessBackendCompetition(response.competitions[0]);
                    }
                    else
                    {
                        Debug.LogWarning($"[CompetitionManager] Received {response.competitions.Length} active competitions from backend, but only one can be active at a time. Processing first competition only.");
                        ProcessBackendCompetition(response.competitions[0]);
                    }
                }
            });
            
            // Fetch upcoming competitions
            DatabaseCommunications.GetUpcomingCompetitions((response) =>
            {
                if (response != null && response.competitions != null)
                {
                    Debug.Log($"[CompetitionManager] Received {response.competitions.Length} upcoming competition(s) from backend");
                    foreach (var competitionData in response.competitions)
                    {
                        ProcessBackendCompetition(competitionData);
                    }
                }
            });
        }

        [Server]
        private static void ProcessBackendCompetition(CompetitionData backendData)
        {
            try
            {
                Guid competitionId = Guid.Parse(backendData.competition_id);
                
                // Skip if we already loaded this competition
                if (_loadedCompetitionIds.Contains(competitionId))
                {
                    return;
                }
                
                // Convert backend data to Unity Competition object
                Competition competition = Competition.FromBackendData(backendData);
                
                // Check if competition is in the past
                if (competition.EndDateTime < DateTime.UtcNow)
                {
                    Debug.Log($"[CompetitionManager] Skipping expired competition {competitionId}");
                    return;
                }
                
                // Add to upcoming competitions
                _upcomingCompetitions.Add(competition);
                _loadedCompetitionIds.Add(competitionId);
                
                CompetitionType logType = Enum.Parse<CompetitionType>(backendData.competition_type);
                Debug.Log($"[CompetitionManager] Added competition {competitionId} (Type: {logType}, Start: {competition.StartDateTime}, End: {competition.EndDateTime})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CompetitionManager] Error processing backend competition: {ex.Message}");
            }
        }

        [Server]
        public static void AddUpcomingCompetition(Guid competitionId, ICompetitionState competitionState, DateTime startDate,
            DateTime endDate, StoreManager.CurrencyType rewardCurrency, List<int> rewardDistribution)
        {
            _upcomingCompetitions.Add(new Competition(competitionId, competitionState, startDate, endDate, rewardCurrency,
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
            if (_currentCompetition == null)
            {
                Debug.LogWarning("[CompetitionManager] Cannot distribute prizes - no current competition");
                return;
            }

            List<int> prizes = _currentCompetition.CompetitionData.RunningCompetition.Prizepool;
            SortedList<int, PlayerResult> winners = _currentCompetition.CompetitionData.GetTopPerformers(prizes.Count);
            
            Debug.Log($"[CompetitionManager] Distributing prizes to {winners.Count} winner(s)");
            
            for (int i = 0; i < winners.Count && i < prizes.Count; i++)
            {
                PlayerResult winner = winners[i + 1]; // winners is 1-indexed (rank 1, 2, 3...)
                int prizeAmount = prizes[i]; // prizes is 0-indexed
                
                // Give prize to online player (offline players will receive prizes from backend)
                if (GameNetworkManager.connUUID.TryGetValue(winner.PlayerID, out NetworkConnectionToClient playerConnection))
                {
                    PlayerData playerData = playerConnection.identity.GetComponent<PlayerData>();
                    if (playerData != null)
                    {
                        switch (_currentCompetition.CompetitionData.RunningCompetition.RewardCurrency)
                        {
                            case StoreManager.CurrencyType.BUCKS:
                                playerData.ChangeFishBucksAmount(prizeAmount, true);
                                Debug.Log($"[CompetitionManager] Awarded {prizeAmount} BUCKS to online player {winner.PlayerName} (Rank {i + 1})");
                                break;
                            case StoreManager.CurrencyType.COINS:
                                playerData.ChangeFishCoinsAmount(prizeAmount, true);
                                Debug.Log($"[CompetitionManager] Awarded {prizeAmount} COINS to online player {winner.PlayerName} (Rank {i + 1})");
                                break;
                            default:
                                throw new NotSupportedException($"Currency type {_currentCompetition.CompetitionData.RunningCompetition.RewardCurrency} has not yet been implemented as a reward");
                        }
                    }
                }
            }
        }

        [Server]
        public static void MailResults()
        {
            if (_currentCompetition == null)
            {
                Debug.LogWarning("[CompetitionManager] Cannot mail results - no current competition");
                return;
            }

            // Get all participants
            List<int> prizes = _currentCompetition.CompetitionData.RunningCompetition.Prizepool;
            SortedList<int, PlayerResult> allParticipants = _currentCompetition.CompetitionData.GetTopPerformers(100); // Get top 100 or all
            
            string competitionName = _currentCompetition.CompetitionData.RunningCompetition.CompetitionState.AsString();
            
            // Mail results to all participants
            foreach (var entry in allParticipants)
            {
                int rank = entry.Key;
                PlayerResult participant = entry.Value;
                int prizeAmount = (rank - 1 < prizes.Count) ? prizes[rank - 1] : 0;
                string currencyName = _currentCompetition.CompetitionData.RunningCompetition.RewardCurrency == StoreManager.CurrencyType.COINS ? "Coins" : "Bucks";
                
                string title = prizeAmount > 0 
                    ? $"Competition Winner! - Rank #{rank}" 
                    : $"Competition Results - Rank #{rank}";
                    
                string message = prizeAmount > 0
                    ? $"Congratulations! You finished rank #{rank} in the '{competitionName}' competition!\n\n" +
                      $"You won: {prizeAmount} {currencyName}\n\n" +
                      $"Thank you for participating!"
                    : $"Thank you for participating in the '{competitionName}' competition!\n\n" +
                      $"You finished rank #{rank}.\n\n" +
                      $"Keep practicing and good luck in future competitions!";
                
                Mail resultMail = new Mail(participant.PlayerID, title, message, "Competition System");
                DatabaseCommunications.AddMail(resultMail);
            }
            
            Debug.Log($"[CompetitionManager] Mailed results to {allParticipants.Count} participant(s)");
        }

        public static bool AddToRunningCompetition<T>(T data, PlayerData playerData)
        {
            if (_currentCompetition is CurrentCompetition<T> competition)
            {
                bool success = competition.AddToCompetition(data, playerData);
                
                // If score was successfully added, submit to backend
                if (success)
                {
                    SubmitScoreToBackend(playerData.GetUuid());
                }
                
                return success;
            }

            return false;
        }

        [Server]
        private static void SubmitScoreToBackend(Guid playerId)
        {
            if (_currentCompetition == null)
            {
                return;
            }

            Guid competitionId = _currentCompetition.CompetitionData.RunningCompetition.CompetitionId;
            (int _, PlayerResult result) = _currentCompetition.CompetitionData.GetPlayerResult(playerId);
            
            if (result != null)
            {
                int score = result.Result;
                
                // Submit to backend (fire and forget - backend handles upserts)
                DatabaseCommunications.SubmitCompetitionScore(competitionId, playerId, score, (response) =>
                {
                    // Success - backend will update leaderboard
                    Debug.Log($"[CompetitionManager] Submitted score {score} for player {playerId} to competition {competitionId}");
                });
            }
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

public static class PlayerResultReaderWriter
{
    public static void WritePlayerResult(this NetworkWriter writer, PlayerResult result)
    {
        writer.WriteGuid(result.PlayerID);
        writer.WriteString(result.PlayerName);
        writer.WriteInt(result.Result);
    }

    public static PlayerResult ReadPlayerResult(this NetworkReader reader)
    {
        return new PlayerResult(
            reader.ReadGuid(),
            reader.ReadString(),
            reader.ReadInt()
            );
    }
}
