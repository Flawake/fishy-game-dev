using UnityEngine;

[CreateAssetMenu(fileName = "Fish", menuName = "Items/AddFish", order = 2)]
public class FishObject : ItemObject
{
    new public string name;
    public int amount;
    public void Awake()
    {
        type = ItemType.Fish;
    }

    public static string AsString()
    {
        return "Fish";
    }
}
