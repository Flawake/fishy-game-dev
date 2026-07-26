using UnityEngine;

[CreateAssetMenu(fileName = "Make Friends Mission", menuName = "Missions/Make new friends")]
public class MakeFriendsMission : Mission<FriendMadeEvent>
{
    // How many friends to make is Mission.RequiredProgress.
    protected override void ProgressMission(FriendMadeEvent missionEvent, MissionWrapper state)
    {
        state.AddProgress();
    }
}
