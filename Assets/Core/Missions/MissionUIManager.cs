using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionUIManager : MonoBehaviour
{
    [SerializeField]
    GameObject missionCanvas;
    [SerializeField]
    Slider progressSlider;
    [SerializeField]
    TMP_Text missionTitle;
    [SerializeField]
    TMP_Text missionDescription;
    [SerializeField]
    TMP_Text progressDescription;
    [SerializeField]
    Image missionIcon;
    [SerializeField]
    Image rewardIcon;
    [SerializeField]
    TMP_Text rewardDescription;
    

    public void RenderMission(MissionWrapper mission)
    {
        missionTitle.text = mission.Mission.MissionTitle;
        missionDescription.text = mission.Mission.MissionDescription;
        progressDescription.text = $"{mission.Progress}/{mission.Mission.RequiredProgress}";
        missionIcon.sprite = mission.Mission.GetMissionIcon();

        rewardIcon.sprite = mission.Mission.completionReward.GetIcon();
        rewardDescription.text = mission.Mission.completionReward.GetRewardDescription();

    }

    // Called from button in game
    public void CloseMissionCanvas()
    {
        missionCanvas.SetActive(false);
    }

    private void SetProgressSlider(int progress, int goal)
    {
        progressSlider.value = goal / progress;
    }
}
