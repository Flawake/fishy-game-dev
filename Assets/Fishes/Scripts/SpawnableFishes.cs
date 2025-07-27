using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using ItemSystem;

public class SpawnableFishes : NetworkBehaviour
{
    public List<ItemDefinition> fishes = new List<ItemDefinition>();

    protected override void OnValidate()
    {
        base.OnValidate();
        foreach (ItemDefinition fish in fishes)
        {
            if (fish.GetBehaviour<FishBehaviour>() == null)
            {
                Debug.LogError("There is a fish in a catch place without a fishBehaviour");
            }
        }
        for (int i = 0; i < fishes.Count; i++)
        {
            ScriptableObject currentFish = fishes[i];
            for (int j = 0; j < fishes.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }
                if (currentFish == fishes[j])
                {
                    Debug.LogError("There are some double fishes added in the same water");
                }
            }
            if (currentFish == null)
            {
                Debug.LogError("There is a fish in the water that does not exist (null value)");
            }
        }
    }

    //returns a new fish if succesfull, second argument tells if generating was succesfull.
    [Server]
    public (CurrentFish, bool) GenerateFish(ItemBaitType bait, float luckMultiplier = 1.0f) {
        // a fish is being generated and returned
        ItemDefinition generatedFish = SpawnManager.instance.GenerateFish(fishes, bait, luckMultiplier);
        CurrentFish fishToCatch = new CurrentFish();
        
        if (generatedFish == null)
        {
            return (fishToCatch, false);
        }
        
        FishBehaviour generatedFishBehaviour = generatedFish.GetBehaviour<FishBehaviour>();
        if (generatedFishBehaviour == null)
        {
            Debug.LogError($"Generated fish {generatedFish.Id} does not have a FishBehaviour");
            return (fishToCatch, false);
        }
        
        fishToCatch.id = generatedFish.Id;
        fishToCatch.maxLength = generatedFishBehaviour.MaximumLength;
        fishToCatch.minLength = generatedFishBehaviour.MinimumLength;
        (fishToCatch.length, fishToCatch.weight, fishToCatch.xp) = GenerateLengthWeightAndXp(generatedFishBehaviour);
        fishToCatch.rarity = generatedFishBehaviour.Rarity;
        
        return (fishToCatch, true);
    }

    [Server]
    (int, float, int) GenerateLengthWeightAndXp(FishBehaviour generatedFishBehaviour)
    {
        int length = TriangularDistributionRandomInt(generatedFishBehaviour.MinimumLength, generatedFishBehaviour.MaximumLength, generatedFishBehaviour.AvarageLength);
        float weight = 0;
        int xp = 2;
        return (length, weight, xp);
    }

    [Server]
    int TriangularDistributionRandomInt(int min, int max, int average) {
#if UNITY_EDITOR
        if (average < min || average > max)
        {
            throw new ArgumentOutOfRangeException(nameof(average), "The average must be between min and max.");
        }
#endif
        float range = max - min;
        float averageProportion = (float)(average - min) / range;

        float u = UnityEngine.Random.value;

        float result;
        if (u < averageProportion)
        {
            result = min + Mathf.Sqrt(u * range * (average - min));
        }
        else
        {
            result = max - Mathf.Sqrt((1 - u) * range * (max - average));
        }

        // Round to nearest integer and clamp it between te min and max value
        return Mathf.Clamp(Mathf.RoundToInt(result), min, max);
    }
}
