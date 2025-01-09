using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager instance;
    private void Awake()
    {
        instance = this;
    }

    [Server]
    public FishConfiguration GenerateFish(List<FishConfiguration> fishes, ItemBaitType bait) {
        //we generate 2 lists first the list of the rarity we're going to catch.
        //After that we generate a second list that takes the rarity factor in account.
        
        if(fishes.Count == 0) {
            Debug.LogError("The water is empty");
            return null;
        }
        int spawnNumber = Random.Range(1, 1000);

        List<FishConfiguration> possibleFishes = new List<FishConfiguration>();

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

        //Place all the fishes of a certain rarity in the possibleFishes list
        for (int i = 0; i < fishes.Count; i++)
        {
            if (fishes[i].rarity == FishRarity.LEGENDARY && fishRarity == 5) {
                possibleFishes.Add(fishes[i]);
            }
            else if (fishes[i].rarity == FishRarity.EPIC && fishRarity == 4)
            {
                possibleFishes.Add(fishes[i]);
            }
            else if (fishes[i].rarity == FishRarity.RARE && fishRarity == 3)
            {
                possibleFishes.Add(fishes[i]);
            }
            else if (fishes[i].rarity == FishRarity.UNCOMMON && fishRarity == 2)
            {
                possibleFishes.Add(fishes[i]);
            }
            else if (fishes[i].rarity == FishRarity.COMMON && fishRarity == 1)
            {
                possibleFishes.Add(fishes[i]);
            }
        }

        //Remove all fishes from the list that do not bite on the currently selected bait
        for (int i = possibleFishes.Count - 1; i >= 0; i--)
        {
            if (!BaitEnumsDefinition.FishBaitContainsItemBait(possibleFishes[i].baitType, bait))
            {
                possibleFishes.RemoveAt(i);
            }
        }

        List<FishConfiguration> fishRarityFactor = new List<FishConfiguration>();
        
        float rarityFactor = Random.Range(1f, 1000f) / 1000f;


        possibleFishes.Sort(delegate (FishConfiguration configA, FishConfiguration configB) {
            return configA.rarityFactor.CompareTo(configB.rarityFactor);
        });

        foreach (FishConfiguration config in possibleFishes)
        {
            float previousRarity = 0;
            int index = possibleFishes.IndexOf(config);

            if (index > 0)
            {
                if(possibleFishes[index - 1].rarityFactor != config.rarityFactor)
                    previousRarity = possibleFishes[index - 1].rarityFactor;
            }

            if (rarityFactor <= config.rarityFactor && rarityFactor >= previousRarity)
            {
                fishRarityFactor.Add(config);
            }
        }

        if (fishRarityFactor.Count == 0)
        {
            return null;
        }

        int chooseFish = Random.Range(0, fishRarityFactor.Count);
        return fishRarityFactor[chooseFish];
    }
}
