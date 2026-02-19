using System;
using Utils;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;
using ItemSystem;
using System.Linq;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager instance;
    
    private void Awake()
    {
        instance = this;
    }

    [Server]
    public ItemDefinition GenerateFish(List<ItemDefinition> fishes, ItemBaitType bait, float luckMultiplier = 1.0f) {
        //we generate 2 lists first the list of the rarity we're going to catch.
        //After that we generate a second list that takes the rarity factor in account.
        
        if(fishes.Count == 0) {
            Debug.LogError("The water is empty");
            return null;
        }
        
        // Adjust spawn number based on luck multiplier (inverted for correct rarity logic)
        int baseSpawnNumber = Random.Range(1, 1000);
        int spawnNumber = Mathf.RoundToInt(baseSpawnNumber / luckMultiplier);
        spawnNumber = Mathf.Clamp(spawnNumber, 1, 1000); // Keep within valid range

        List<ItemDefinition> possibleFishes = new List<ItemDefinition>();

        byte fishRarity = 0;

        if (spawnNumber == 1) {
            fishRarity = 5;
        }
        else if (spawnNumber > 1 && spawnNumber <= 50) {
            fishRarity = 4;
        }
        else if (spawnNumber > 50 && spawnNumber <= 230)
        {
            fishRarity = 3;
        }
        else if (spawnNumber > 230 && spawnNumber <= 550)
        {
            fishRarity = 2;
        }
        else {
            fishRarity = 1;
        }

        // Only consider ItemDefinitions with FishBehaviour
        for (int i = 0; i < fishes.Count; i++)
        {
            var fishBehaviour = fishes[i].GetBehaviour<FishBehaviour>();
            if (fishBehaviour == null) continue;
            FishRarity rarity = fishBehaviour.Rarity;
            if (rarity == FishRarity.LEGENDARY && fishRarity == 5)
            {
                possibleFishes.Add(fishes[i]);
            }
            else if (rarity == FishRarity.EPIC && fishRarity == 4)
            {
                possibleFishes.Add(fishes[i]);
            }
            else if (rarity == FishRarity.RARE && fishRarity == 3)
            {
                possibleFishes.Add(fishes[i]);
            }
            else if (rarity == FishRarity.UNCOMMON && fishRarity == 2)
            {
                possibleFishes.Add(fishes[i]);
            }
            else if (rarity == FishRarity.COMMON && fishRarity == 1)
            {
                possibleFishes.Add(fishes[i]);
            }
        }

        // Remove all fishes from the list that do not bite on the currently selected bait
        for (int i = possibleFishes.Count - 1; i >= 0; i--)
        {
            var fishBehaviour = possibleFishes[i].GetBehaviour<FishBehaviour>();
            if (!BaitEnumsDefinition.FishBaitContainsItemBait(fishBehaviour.BitesOn, bait))
            {
                possibleFishes.RemoveAt(i);
            }
        }
        
        // Remove all fishes that can't be caught at this time
        for (int i = possibleFishes.Count - 1; i >= 0; i--)
        {
            var possibleFish = possibleFishes[i].GetBehaviour<FishBehaviour>();

            if (possibleFish.TimeRanges.Count > 0)
            {
                bool isInsideAnyRange = possibleFish.TimeRanges
                    .Any(range => range.TimeRangeContainsTime(DateTime.Now.Hour, DateTime.Now.Minute));

                if (!isInsideAnyRange)
                {
                    possibleFishes.RemoveAt(i);
                    continue;
                }
            }

            if (possibleFish.DateRanges.Count > 0)
            {
                bool isInsideAnyRange = possibleFish.DateRanges
                    .Any(range => range.DateRangeContainsDate(DateTime.Now.Month, DateTime.Now.Day));

                if (!isInsideAnyRange)
                {
                    Debug.Log("Fish out of date range");
                    possibleFishes.RemoveAt(i);
                }
            }
        }

        List<ItemDefinition> fishRarityFactor = new List<ItemDefinition>();
        float rarityFactor = Random.Range(1f, 1000f) / 1000f;

        // Sort by rarityFactor from FishBehaviour
        possibleFishes.Sort((defA, defB) =>
        {
            var fishA = defA.GetBehaviour<FishBehaviour>();
            var fishB = defB.GetBehaviour<FishBehaviour>();
            return fishA.RarityFactor.CompareTo(fishB.RarityFactor);
        });

        for (int i = 0; i < possibleFishes.Count; i++)
        {
            var config = possibleFishes[i];
            var currentFish = config.GetBehaviour<FishBehaviour>();
            float previousRarity = 0;
            if (i > 0)
            {
                var previousFish = possibleFishes[i - 1].GetBehaviour<FishBehaviour>();
                if (previousFish.RarityFactor != currentFish.RarityFactor)
                    previousRarity = previousFish.RarityFactor;
            }

            if (rarityFactor <= currentFish.RarityFactor && rarityFactor >= previousRarity)
            {
                fishRarityFactor.Add(config);
            }
        }

        if (fishRarityFactor.Count == 0)
        {
            return null;
        }

        int chosenFish = Random.Range(0, fishRarityFactor.Count);
        return fishRarityFactor[chosenFish];
    }
}
