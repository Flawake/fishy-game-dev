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

        public void SetUpcomingCompetition(Competition competition)
        {
            _competition = competition;
            DescriptionText.text = competition.CompetitionState.AsString();
            CompetitionIcon.sprite = competition.CompetitionState.Icon();
        }

        private void Update()
        {
            CountdownText.text = (_competition.StartDateTime - DateTime.UtcNow).ToString();
        }
    }
}
