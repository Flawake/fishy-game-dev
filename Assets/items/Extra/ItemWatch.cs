using UnityEngine;

public enum WatchType
{
    Rare = 0,
    Epic = 1,
}

[CreateAssetMenu(fileName = "ItemWatch", menuName = "Items/Watch", order = 2)]
public class ItemWatch: ExtraObject
{
    public int amount;
    public float duration;
    public WatchType quality;
    
    public string AsString()
    {
        switch (quality)
        {
            case WatchType.Rare:
                return "Rare watch";
            case WatchType.Epic:
                return "Epic watch";
            default:
                Debug.LogError("Unknown watch type");
                return "Unknown watch type";
        };
    }
}
