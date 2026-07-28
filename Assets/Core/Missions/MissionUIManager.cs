using System;
using System.Collections.Generic;
using System.Linq;
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
    
    private enum MissionsRendering
    {
        ACTIVE,
        COMPLETED,
    }

    MissionsRendering missionsRendering = MissionsRendering.ACTIVE;
    
    int renderingMissionIndex = 0;

    public void RenderMission(Mission mission, int missionProgress)
    {
        missionTitle.text = mission.MissionTitle;
        missionDescription.text = mission.MissionDescription;
        progressDescription.text = $"{missionProgress}/{mission.RequiredProgress}";
        missionIcon.sprite = mission.GetMissionIcon();

        rewardIcon.sprite = mission.completionReward.GetIcon();
        rewardDescription.text = mission.completionReward.GetRewardDescription();

        SetProgressSlider(missionProgress, mission.RequiredProgress);
    }

    // Called from button in game
    public void OpenMissionCanvas()
    {
        LoadActiveMissions(0);
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

    //Called from button in game
    public void LoadActiveMissions(int index)
    {
        List<MissionState> activeMissions = GetComponentInParent<MissionManager>().ClientGetActiveMissions();
        if (activeMissions.Count() < index + 1)
        {
            Debug.Log("No missions were active");
            return;
        }
        Mission firstActiveMission = MissionRegistry.Get(activeMissions[index].missionID);
        RenderMission(firstActiveMission, activeMissions[index].progress);
    }

    //Called from button in game
    public void LoadCompletedMissions(int index)
    {
        List<int> completedMissionIDs = GetComponentInParent<MissionManager>().GetCompletedMissionsIDs();
    }

    //Called from button in game
    public void NextMissionButton()
    {
        if (missionsRendering == MissionsRendering.ACTIVE)
        {
            LoadActiveMissions(renderingMissionIndex + 1 % GetComponentInParent<MissionManager>().ClientGetActiveMissions().Count());
        }
        else if (missionsRendering == MissionsRendering.COMPLETED)
        {
            LoadCompletedMissions(renderingMissionIndex + 1 % GetComponentInParent<MissionManager>().GetCompletedMissionsIDs().Count());
        }
    }

    //Called from button in game
    public void PreviousMissionButton()
    {

            int max = GetComponentInParent<MissionManager>().ClientGetActiveMissions().Count();
        if (missionsRendering == MissionsRendering.ACTIVE)
        {
            LoadActiveMissions((renderingMissionIndex - 1 % max + max) % max);
        }
        else if (missionsRendering == MissionsRendering.COMPLETED)
        {
            LoadCompletedMissions((renderingMissionIndex - 1 % max + max) % max);
        }
    }
}
