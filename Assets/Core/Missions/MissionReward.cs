using System;
using System.Reflection.Emit;
using ItemSystem;
using UnityEngine;

public abstract class IMissionReward : ScriptableObject
{
    public abstract void DistributeReward();
    public abstract string GetRewardDescription();
    public abstract Sprite GetIcon();
}
