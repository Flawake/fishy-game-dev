using UnityEngine;


[System.Flags]
public enum baitType : int
{
    hook =      0b00000001,
    dough =     0b00000010,
    meat =      0b00000100,
    insects =   0b00001000,
    lure =      0b00010000,
    shark =     0b00100000,
}

[CreateAssetMenu(fileName = "Bait", menuName = "Items/AddBait", order = 1)]
public class baitObject : ItemObject
{
    public new string name;
    public bool durabilityIsInfinite;
    public bool availableInShop;
    public int throwIns;
    public int newPriceCoins;
    public int newPriceBucks;
    public baitType baitType;
    public void Awake()
    {
        type = ItemType.bait;
    }

    public static string AsString()
    {
        return "bait";
    }
}
