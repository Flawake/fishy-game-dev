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
    void spawnFish() {

        //number generated tells how rare the fish is you are going to catch, 1 = Legendary, 2 - 50 = Epic, 51-230 = Rare, 231 - 550 = Uncommon, 551 - 1000 = Common.
        //When you get for example a 3 star, but that is not available for some reason, you get a fish that has a rarity of one less, Legendary becomes Epic and so forth.
        int spawnNumber = Random.Range(1, 1000);
    }

    [Server]
    public FishConfiguration GenerateFish(List<FishConfiguration> fishes) {
        //we generate 2 lists first the list of the rarity we're going to catch.
        //After that we generate a second list that takes the rarity factor in account.

        //Debug.Log($"There are {fishes.Count} fishes in the water");
        if(fishes.Count == 0) {
            Debug.LogError("The water is empty");
            return null;
        }
        int spawnNumber = Random.Range(1, 1000);

        List<FishConfiguration> fishesRarity = new List<FishConfiguration>();

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

        for (int i = 0; i < fishes.Count; i++)
        {
            if (fishes[i].rarity == FishRarity.LEGENDARY && fishRarity == 5) {
                fishesRarity.Add(fishes[i]);
            }
            else if (fishes[i].rarity == FishRarity.EPIC && fishRarity == 4)
            {
                fishesRarity.Add(fishes[i]);
            }
            else if (fishes[i].rarity == FishRarity.RARE && fishRarity == 3)
            {
                fishesRarity.Add(fishes[i]);
            }
            else if (fishes[i].rarity == FishRarity.UNCOMMON && fishRarity == 2)
            {
                fishesRarity.Add(fishes[i]);
            }
            else if (fishes[i].rarity == FishRarity.COMMON && fishRarity == 1)
            {
                fishesRarity.Add(fishes[i]);
            }
        }

        List<FishConfiguration> fishRarityFactor = new List<FishConfiguration>();
        
        float rarityFactor = Random.Range(1, 1000) / 1000f;


        fishesRarity.Sort(delegate (FishConfiguration configA, FishConfiguration configB) {
            return configA.rarityFactor.CompareTo(configB.rarityFactor);
        });

        foreach (FishConfiguration config in fishesRarity)
        {
            float previousRarity = 0;
            int index = fishesRarity.IndexOf(config);

            if (index > 0)
            {
                if(fishesRarity[index - 1].rarityFactor != config.rarityFactor)
                    previousRarity = fishesRarity[index - 1].rarityFactor;
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
