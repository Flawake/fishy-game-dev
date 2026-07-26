using System;
using System.Reflection.Emit;
using ItemSystem;
using UnityEngine;

public interface IMissionReward
{
    public void DistributeReward();
    public string GetRewardDescription();
    public Sprite GetIcon();
}

[Serializable]
public class MissionRewardMoney : IMissionReward
{
    public StoreManager.CurrencyType rewardCurrency;
    public int rewardAmount;
    public void DistributeReward()
    {
        
    }

    public string GetRewardDescription() => $"{rewardAmount} {rewardCurrency}";

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

    public string GetRewardDescription()
    {
        string amount = "";
        if (rewardItem.GetState<StackState>() != null)
        {
            amount = rewardItem.GetState<StackState>().currentAmount.ToString();
        }
        return $"{amount} {rewardItem.def.name}";
    }

    public Sprite GetIcon()
    {
        return rewardItem.def.Icon;
    }
}
