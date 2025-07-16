using UnityEngine;

public enum LuckPotionType
{
    Rare = 0,
    Epic = 1,
}

[CreateAssetMenu(fileName = "LuckPotion", menuName = "Items/LuckPotion", order = 2)]
public class ItemLuckPotion : ExtraObject
{
    public int amount;
    public float duration;
    public LuckPotionType luckType;
    
    public string AsString()
    {
        switch (luckType)
        {
            case LuckPotionType.Rare:
                return "Rare luck potion";
            case LuckPotionType.Epic:
                return "Epic luck potion";
            default:
                Debug.LogError("Unknown luck potion type");
                return "Unknown luck potion type";
        };
    }
}
