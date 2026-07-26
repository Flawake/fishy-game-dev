using UnityEngine;

[CreateAssetMenu(fileName = "Make Trades Mission", menuName = "Missions/Make new trades")]
public class MakeTradesMission : Mission<TradeMadeEvent>
{
    // How many trades to make is Mission.RequiredProgress.
    protected override void ProgressMission(TradeMadeEvent missionEvent, MissionWrapper state)
    {
        state.AddProgress();
    }
}
