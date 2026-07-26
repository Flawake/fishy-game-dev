using ItemSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "Collect Item Mission", menuName = "Missions/Collect items")]
public class CollectItemMission : Mission<ItemCollectedEvent>
{
    public ItemDefinition itemDefinition;

    // How many to collect is Mission.RequiredProgress.
    protected override void ProgressMission(ItemCollectedEvent missionEvent, MissionWrapper state)
    {
        if (itemDefinition == null || missionEvent.ItemID != itemDefinition.Id)
        {
            return;
        }

        state.AddProgress(missionEvent.Amount);
    }
}
