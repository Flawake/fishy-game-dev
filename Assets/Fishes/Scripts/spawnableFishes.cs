using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Unity.VisualScripting;

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
    (int, float, int) generateLengthWeightAndXp(FishConfiguration generatedFish)
    {
        int length = TriangularDistributionRandomInt(generatedFish.minimumLength, generatedFish.maximumLength, generatedFish.avarageLength);
        float weight = LinearEqualValue(
            generatedFish.minimumLength,
            generatedFish.maximumLength,
            generatedFish.minimumWeightGrams,
            generatedFish.maximumWeightGrams,
            length);
        int xp = 2;
        return (length, weight, xp);
    }

    [Server]
    int TriangularDistributionRandomInt(int min, int max, int average) {

        float range = min - max;
        float averageProportion = (int)(average - min) / range;

        float u = Random.value;

        float result;
        if (u < averageProportion)
        {
            result = min + Mathf.Sqrt(u * range * (average - min));
        }
        else
        {
            result = max - Mathf.Sqrt((1 - u) * range * (max - average));
        }

        // Round to nearest integer
        return Mathf.RoundToInt(result);
    }

    [Server]
    float LinearEqualValue(int xMin, int xMax, float yMin, float yMax, int val) {
        return (val - xMin) * (yMax - yMin) / (xMax - xMin) + yMin;
    }
}
