using UnityEngine;

public enum ItemMiscType
{
    Shell = 0,
}

[CreateAssetMenu(fileName = "Special", menuName = "Items/Misc", order = 1)]
public class ExtraObject : ItemObject
{
    public int amount;
    public ItemMiscType itemType;

    public string AsString()
    {
        switch (itemType)
        {
            case ItemMiscType.Shell:
                return "Shell";
            default:
                Debug.LogError("Unknown special item type");
                return "Unknown";
        };
    }
}
