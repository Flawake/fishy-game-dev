using UnityEngine;

[CreateAssetMenu(fileName = "Fishdex Milestone Mission", menuName = "Missions/Reach Fishdex Milestone")]
public class FishdexMilestoneMission : Mission<FishdexUpdatedEvent>
{
    // The milestone to reach is Mission.RequiredProgress.
    protected override void ProgressMission(FishdexUpdatedEvent missionEvent, MissionWrapper state)
    {
        state.SetProgress(missionEvent.TotalSpeciesDiscovered);
    }

    public override Sprite GetMissionIcon()
    {
        return StaticMissionIconRegistry.fishdexProgressMissionIcon;
    }
}
