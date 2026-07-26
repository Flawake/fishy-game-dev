using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Money reward", menuName = "Mission rewards/Money reward")]
public class MissionRewardMoney : IMissionReward
{
    public StoreManager.CurrencyType rewardCurrency;
    public int rewardAmount;
    public override void DistributeReward()
    {
        
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