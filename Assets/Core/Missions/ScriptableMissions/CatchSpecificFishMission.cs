using ItemSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "Catch Specific Fish Mission", menuName = "Missions/Catch Specific Fish")]
public class CatchSpecificFishMission : Mission<FishCaughtEvent>
{
    public ItemDefinition fishDefinition;
    protected override void ProgressMission(FishCaughtEvent missionEvent, MissionWrapper state)
    {
        if (fishDefinition == null || missionEvent.FishID != fishDefinition.Id)
        {
            return;
        }

        state.AddProgress();
    }

    public override Sprite GetMissionIcon()
    {
        return fishDefinition.Icon;
    }
}
