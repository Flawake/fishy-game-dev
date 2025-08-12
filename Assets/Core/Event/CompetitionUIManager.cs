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
            foreach (Competition upcomingCompetition in upcomingCompetitions)
            {
                GameObject newObject = Instantiate(upcomingCompetitionPreviewObject, upcomingCompetitionsContainerTransform);
            }
        }

        public void LoadCurrentCompetition(SortedList<int, PlayerResult> rankedPlayerResults, List<int> prizes)
        {
            if (rankedPlayerResults == null || prizes == null)
            {
                return;
            }
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

            foreach (var kvp in cleanResults)
            {
                PlayerResult result = kvp.Value;
                GameObject newObject = Instantiate(currentCompetitionResultObject, currentCompetitionsContainerTransform);
                PersonalResultsUIManager resultUI = newObject.GetComponent<PersonalResultsUIManager>();
                int prize = (kvp.Key < prizes.Count) ? prizes[kvp.Key] : 0;
                resultUI.SetResults(kvp.Key, result.PlayerName, result.Result, prize, CompetitionManager.GetCurrentCompetition().CompetitionData.RunningCompetition.RewardCurrency);
            }
        }
    }
}
