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
    
    // Item ID to item type lookup
    private Dictionary<int, ItemType> idToTypeLut = new();

    private void Awake()
    {
        ItemsInGame.fishesInGame = fishesInGame;
        ItemsInGame.rodsInGame = rodsInGame;
        ItemsInGame.baitsInGame = baitsInGame;
        ItemsInGame.storeItemRods = storeItemRods;
        ItemsInGame.storeItemBaits = storeItemBaits;
        ItemsInGame.storeItemMisc = storeItemMisc;
        ItemsInGame.storeItemMisc = storeItemMisc;
        ItemsInGame.idToTypeLut = idToTypeLut;
    }
    
    private void Start()
    {
        foreach (rodObject rod in ItemsInGame.rodsInGame)
        {
            idToTypeLut.Add(rod.id, ItemType.Rod);
        }
        foreach (baitObject bait in ItemsInGame.baitsInGame)
        {
            idToTypeLut.Add(bait.id, ItemType.Bait);
        }
        foreach (FishConfiguration fish in ItemsInGame.fishesInGame)
        {
            idToTypeLut.Add(fish.id, ItemType.Fish);
        }
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
    
    public static Dictionary<int, ItemType> idToTypeLut;

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