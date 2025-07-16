using UnityEngine;

public enum BoosterExperienceType
{
    Rare = 0,
    Epic = 1,
}

[CreateAssetMenu(fileName = "ItemBoosterExperience", menuName = "Items/BoosterExperience", order = 2)]
public class ItemBoosterExperience: ItemObject
{
    public int amount;
    public float duration;
    public BoosterExperienceType quality;
    
    public string AsString()
    {
        switch (quality)
        {
            case BoosterExperienceType.Rare:
                return "Rare xp booster";
            case BoosterExperienceType.Epic:
                return "Epic xp booster";
            default:
                Debug.LogError("Unknown experience booster type");
                return "Unknown experience booster type";
        };
    }
}
