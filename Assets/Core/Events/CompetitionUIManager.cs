using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using TMPro;
using UnityEngine;

namespace  GlobalCompetitionSystem
{
    public class CompetitionUIManager : MonoBehaviour
    {
        [SerializeField] private GameObject competitionBackground;
        [SerializeField] private GameObject currentCompetitionView;
        [SerializeField] private GameObject upcomingCometitionsView;
        [SerializeField] private GameObject CompetitionNotStarted;
        [SerializeField] private GameObject currentCompetitionResultObject;
        [SerializeField] private GameObject upcomingCompetitionPreviewObject;
        [SerializeField] private Transform upcomingCompetitionsContainerTransform;
        [SerializeField] private Transform currentCompetitionsContainerTransform;
        [SerializeField] private TMP_Text competitionEndCountdownText;
        [SerializeField] private TMP_Text competitionStartsInText;

        private void Awake()
        {
            if (competitionBackground == null)
            {
                Debug.LogWarning("CompetitionBackground is null");
            }

            if (currentCompetitionResultObject == null)
            {
                Debug.LogWarning("CurrentCompetitionResultObject is null");
            }

            if (upcomingCompetitionPreviewObject == null)
            {
                Debug.LogWarning("upcomingCompetitionPreviewObject is null");
            }

            if (upcomingCompetitionsContainerTransform == null)
            {
                Debug.LogWarning("UpcomingCompetitionsContainerTransform is null");
            }
        }

        private void Update()
        {
            if (!competitionBackground.activeSelf)
            {
                return;
            }

            CurrentCompetition currentCompetition = CompetitionManager.GetCurrentCompetition();
            if (currentCompetition == null)
            {
                return;
            }
            TimeSpan timeTillCompetitionEnd = CompetitionManager.GetCurrentCompetition().CompetitionData.RunningCompetition.EndDateTime - DateTime.UtcNow;
            competitionEndCountdownText.text = timeTillCompetitionEnd.ToString(@"hh\:mm\:ss");
        }

        // Called from button ingame
        public void OpenCompetitionUI()
        {
            competitionBackground.SetActive(true);
            OpenCurrentCompetitionsScreen();
        }

        // Called from button ingame
        public void CloseCompetitionUI()
        {
            competitionBackground.SetActive(false);
        }
        
        // Called from button ingame
        public void OpenUpcomingCompetitionsScreen()
        {
            currentCompetitionView.SetActive(false);
            CompetitionNotStarted.SetActive(false);
            upcomingCometitionsView.SetActive(true);
            GetUpcomingCompetitions();
        }
        private void GetUpcomingCompetitions()
        {
            LoadUpcomingCompetitions(CompetitionManager.GetUpcomingCompetitions());
        }
        
        // Also called from button ingame
        public void OpenCurrentCompetitionsScreen()
        {
            currentCompetitionView.SetActive(false);
            CompetitionNotStarted.SetActive(false);
            upcomingCometitionsView.SetActive(false);
            if (CompetitionManager.GetCurrentCompetition() == null)
            {
                CompetitionNotStarted.SetActive(true);
                if (CompetitionManager.GetUpcomingCompetitions() == null || CompetitionManager.GetUpcomingCompetitions().Count == 0)
                {
                    competitionStartsInText.text = "--:--";
                    return;
                }
                competitionStartsInText.text = (CompetitionManager.GetUpcomingCompetitions().First().StartDateTime - DateTime.UtcNow).ToString();
                return;
            }
            currentCompetitionView.SetActive(true);
            CompetitionNotStarted.SetActive(false);
            
            GetComponentInParent<PlayerData>().CmdGetTopPerformers();
        }

        private void LoadUpcomingCompetitions(SyncSortedSet<Competition> upcomingCompetitions)
        {
            // Clear existing entries
            foreach (Transform child in upcomingCompetitionsContainerTransform)
            {
                Destroy(child.gameObject);
            }
            
            // Create new entries with data
            foreach (Competition upcomingCompetition in upcomingCompetitions)
            {
                GameObject newObject = Instantiate(upcomingCompetitionPreviewObject, upcomingCompetitionsContainerTransform);
                UpcomingCompetitionUIManager upcomingUI = newObject.GetComponent<UpcomingCompetitionUIManager>();
                if (upcomingUI != null)
                {
                    upcomingUI.SetUpcomingCompetition(upcomingCompetition);
                }
            }
        }

        public void LoadCurrentCompetition(SortedList<int, PlayerResult> rankedPlayerResults, List<int> prizes)
        {
            if (rankedPlayerResults == null)
            {
                return;
            }
            
            List<int> prizePool = prizes;
            
            foreach (Transform child in currentCompetitionsContainerTransform)
            {
                Destroy(child.gameObject);
            }
            
            Dictionary<int, PlayerResult> filtered = rankedPlayerResults
                .GroupBy(kvp => kvp.Value.PlayerID) 
                .Select(g => g.First())
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Convert back to SortedList if absolutely needed
            SortedList<int, PlayerResult> cleanResults = new SortedList<int, PlayerResult>(filtered);

            // Only show leaderboard entries up to the prize pool length (or show all if we want to display non-winners too)
            // Current implementation shows all players but only awards prizes to top N
            foreach (var kvp in cleanResults)
            {
                PlayerResult result = kvp.Value;
                GameObject newObject = Instantiate(currentCompetitionResultObject, currentCompetitionsContainerTransform);
                PersonalResultsUIManager resultUI = newObject.GetComponent<PersonalResultsUIManager>();
                
                // Prize amount is 0 if player rank exceeds prize pool length
                int prize = (kvp.Key - 1 < prizePool.Count) ? prizePool[kvp.Key - 1] : 0;
                resultUI.SetResults(kvp.Key, result.PlayerName, result.Result, prize, CompetitionManager.GetCurrentCompetition().CompetitionData.RunningCompetition.RewardCurrency);
            }
        }
    }
}
