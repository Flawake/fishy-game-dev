using ItemSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "Item reward", menuName = "Mission rewards/Item reward")]
public class MissionRewardItem : IMissionReward
{
    public ItemInstance rewardItem;
    public override void DistributeReward()
    {
        
    }

    public override string GetRewardDescription()
    {
        string amount = "";
        if (rewardItem.GetState<StackState>() != null)
        {
            amount = rewardItem.GetState<StackState>().currentAmount.ToString();
        }
        return $"{amount} {rewardItem.def.name}";
    }

    public override Sprite GetIcon()
    {
        return rewardItem.def.Icon;
    }
}