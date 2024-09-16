using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Fish", menuName = "Items/AddFish", order = 2)]
public class FishObject : ItemObject
{
    new public string name;
    public int amount;
    public void Awake()
    {
        type = ItemType.fish;
    }

    public static string AsString()
    {
        return "fish";
    }
}
