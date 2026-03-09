using System;
using GlobalCompetitionSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GlobalCompetitionSystem
{
    public class UpcomingCompetitionUIManager : MonoBehaviour
    {
        Competition _competition;
        [SerializeField] TMP_Text CountdownText;
        [SerializeField] TMP_Text DescriptionText;
        [SerializeField] Image CompetitionIcon;
        [SerializeField] TMP_Text CompetitionUuidText; // For debugging

        public void SetUpcomingCompetition(Competition competition)
        {
            _competition = competition;
            DescriptionText.text = competition.CompetitionState.AsString();
            CompetitionIcon.sprite = competition.CompetitionState.Icon();
            
            // Display competition UUID for debugging
            if (CompetitionUuidText != null)
            {
                CompetitionUuidText.text = $"ID: {competition.CompetitionId}";
            }
        }

        private void Update()
        {
            CountdownText.text = (_competition.StartDateTime - DateTime.UtcNow).ToString();
        }
    }
}
