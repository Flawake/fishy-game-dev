using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Money reward", menuName = "Mission rewards/Money reward")]
public class MissionRewardMoney : IMissionReward
{
    [SerializeField]
    StoreManager.CurrencyType rewardCurrency;
    [SerializeField, Min(1)]
    int rewardAmount = 1;

    public StoreManager.CurrencyType RewardCurrency => rewardCurrency;
    public int RewardAmount => rewardAmount;

    public override void BuildReward(MissionRewardDraft draft)
    {
        draft.AddCurrency(rewardCurrency, rewardAmount);
    }

    public override string GetRewardDescription() => $"{rewardAmount} {rewardCurrency}";

    public override Sprite GetIcon()
    {
        switch(rewardCurrency)
        {
            case StoreManager.CurrencyType.COINS:
                return null;
            case StoreManager.CurrencyType.BUCKS:
                return null;
        }

        throw new Exception("How did we get here?");
    }
}
