using ItemSystem;
using UnityEngine;

[CreateAssetMenu(fileName = "Item reward", menuName = "Mission rewards/Item reward")]
public class MissionRewardItem : IMissionReward
{
    [SerializeField]
    ItemDefinition rewardItem;
    [SerializeField, Min(1)]
    int rewardAmount = 1;

    public ItemDefinition RewardItem => rewardItem;
    public int RewardAmount => rewardAmount;

    public override void BuildReward(MissionRewardDraft draft)
    {
        draft.AddItem(rewardItem, rewardAmount);
    }

    void OnValidate()
    {
        if (rewardItem != null && rewardAmount > Mathf.Max(1, rewardItem.MaxStack))
        {
            Debug.LogWarning(
                $"'{name}' rewards {rewardAmount}x {rewardItem.DisplayName} but a stack holds "
                + $"{rewardItem.MaxStack}. A mission reward writes a single inventory row, so the amount will be clamped.",
                this);
        }
    }

    public override string GetRewardDescription() => rewardItem == null ? "" : $"{rewardAmount} {rewardItem.DisplayName}";

    public override Sprite GetIcon()
    {
        return rewardItem == null ? null : rewardItem.Icon;
    }
}
