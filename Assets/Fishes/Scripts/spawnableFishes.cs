using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class spawnableFishes : NetworkBehaviour
{
    public List<FishConfiguration> fishes = new List<FishConfiguration>();

    protected override void OnValidate()
    {
        base.OnValidate();
        for (int i = 0; i < fishes.Count; i++)
        {
            ScriptableObject currentFish = fishes[i];
            for (int j = 0; j < fishes.Count; j++)
            {
                if (i == j) {
                    continue;
                }
                if (currentFish == fishes[j])
                {
                    Debug.LogError("There are some double fishes added in the same water");
                }
            }
            if(currentFish == null) {
                Debug.LogError("There is a fish in the water that does not exist (null value)");
            }
        }
    }

    //returns a new fish if succesfull, second argument tells if generating was succesfull.
    [Server]
    public (CurrentFish, bool) GenerateFish() {
        // a fish is being generated and returned
        FishConfiguration generatedFish = SpawnManager.instance.GenerateFish(fishes);
        CurrentFish fishToCatch = new CurrentFish();
        if (generatedFish == null)
        {
            return (fishToCatch, false);
        }
        fishToCatch.id = generatedFish.id;
        fishToCatch.maxLength = generatedFish.maximumLength;
        fishToCatch.minLength = generatedFish.minimumLength;
        (fishToCatch.length, fishToCatch.weight, fishToCatch.xp) = generateLengthWeightAndXp(generatedFish);
        fishToCatch.rarity = generatedFish.rarity;
        
        return (fishToCatch, true);
    }

    [Server]
    (int, int, int) generateLengthWeightAndXp(FishConfiguration generatedFish)
    {
        int length = (int)generatedFish.avarageLength;
        int weight = (int)generatedFish.avarageWeightGrams;
        int xp = 2;
        return (length, weight, xp);
    }
}
