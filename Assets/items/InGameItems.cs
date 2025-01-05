using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGameItems : MonoBehaviour
{
    [SerializeField] private FishConfiguration[] fishesInGame;    //catchable fish
    [SerializeField] private rodObject[] rodsInGame;
    [SerializeField] private baitObject[] baitsInGame;

    [SerializeField] private StoreItemObject[] storeItemRods;
    [SerializeField] private StoreItemObject[] storeItemBaits;
    [SerializeField] private StoreItemObject[] storeItemMisc;

    private void Awake()
    {
        ItemsInGame.fishesInGame = fishesInGame;
        ItemsInGame.rodsInGame = rodsInGame;
        ItemsInGame.baitsInGame = baitsInGame;
        ItemsInGame.storeItemRods = storeItemRods;
        ItemsInGame.storeItemBaits = storeItemBaits;
        ItemsInGame.storeItemMisc = storeItemMisc;
    }

#if UNITY_EDITOR 
    //TODO: Check if all items are added and if there are no duplicates.
#endif
}

static class ItemsInGame
{
    public static FishConfiguration[] fishesInGame;    //catchable fish
    public static rodObject[] rodsInGame;
    public static baitObject[] baitsInGame;

    public static StoreItemObject[] storeItemRods;
    public static StoreItemObject[] storeItemBaits;
    public static StoreItemObject[] storeItemMisc;

    public static FishConfiguration getFishByID(int id)
    {
        foreach (FishConfiguration fish in fishesInGame)
        {
            if (fish.id == id)
            {
                return fish;
            }
        }
        return null;
    }
}