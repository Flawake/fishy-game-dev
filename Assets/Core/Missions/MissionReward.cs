using System;
using ItemSystem;
using UnityEngine;

public interface IMissionReward
{
    public void DistributeReward();
    public Sprite GetIcon();
}

[Serializable]
public class MissionRewardMoney : IMissionReward
{
    public StoreManager.CurrencyType rewardCurrency;
    public void DistributeReward()
    {
        
    }

    public Sprite GetIcon()
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

[Serializable]
public class MissionRewardItem : IMissionReward
{
    public ItemInstance rewardItem;
    public void DistributeReward()
    {
        
    }

    public Sprite GetIcon()
    {
        return null;
    }
}
