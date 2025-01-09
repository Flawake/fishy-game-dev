using UnityEngine;

[CreateAssetMenu(fileName = "Bait", menuName = "Items/AddBait", order = 1)]
public class baitObject : ItemObject
{
    public new string name;
    public bool durabilityIsInfinite;
    public bool availableInShop;
    public int throwIns;
    public int newPriceCoins;
    public int newPriceBucks;
    public ItemBaitType baitType;
    public void Awake()
    {
        type = ItemType.bait;
    }

    public static string AsString()
    {
        return "bait";
    }
}
