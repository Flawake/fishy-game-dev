using System;
using UnityEngine;

public class TimeRange
{
    public TimeSpan BeginTime;
    public TimeSpan EndTime;
}

public class DateRange
{
    public TimeSpan BeginDate;
    public TimeSpan EndDate;
}

[CreateAssetMenu(fileName = "FishConfig", menuName = "Fishes/config", order = 1), System.Serializable]
public class FishConfiguration : ScriptableObject
{
    public int id;
    public new string name;
    //TextAreaAttribute(int minLines, int maxLines);
    [TextArea(3, 10)]
    public string description;
    public Sprite fishImage;
    public int minimumLength;
    public int maximumLength;
    public int avarageLength;
    [Space(20)]
    public float minimumWeightGrams;
    public float maximumWeightGrams;
    public float avarageWeightGrams;
    [Space(20)]
    public int minMartketPrice;
    public int maxMarketPrice;
    [Space(20)]
    //1 is normal, 0.1 is 10 times as rare.
    public float rarityFactor;  
    public TimeRange timeRange;
    public DateRange dateRange;
    [Space(20)]
    public int breedSuccessRate;
    public int breedPrice;


    public FishBaitType baitType;
    public FishRarity rarity;
    public Locations locations; 
}
