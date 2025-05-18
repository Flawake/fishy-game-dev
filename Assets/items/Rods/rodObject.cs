using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Rod", menuName = "Items/AddRod", order = 1)]
public class rodObject : ItemObject
{
    public new string name;
    public int strength;
    public bool durabilityIsInfinite;
    public int throwIns;
    public int newPriceCoins;
    public int newPriceBucks;

    public void Awake()
    {
        type = ItemType.Rod;
    }

    public static string AsString()
    {
        return "Rod";
    }
}
