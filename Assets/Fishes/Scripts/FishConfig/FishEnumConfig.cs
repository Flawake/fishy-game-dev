using UnityEngine;

public enum FishRarity : int
{
    COMMON =        0b00001,
    UNCOMMON =      0b00010,
    RARE =          0b00100,
    EPIC =          0b01000,
    LEGENDARY =     0b10000,
}

[System.Flags]
public enum Locations : int
{
    Beach =     0b00000001,
    Tropical =  0b00000010,
}

public class FishEnumConfig
{
    public static int RatityToInt(FishRarity rarity)
    {
        return rarity switch
        {
            FishRarity.COMMON => 1,
            FishRarity.UNCOMMON => 2,
            FishRarity.RARE => 3,
            FishRarity.EPIC => 4,
            FishRarity.LEGENDARY => 5,
            _ => -1,
        };
    }

    public static string RarityToString(FishRarity rarity)
    {
        return rarity switch
        {
            FishRarity.COMMON => "Common",
            FishRarity.UNCOMMON => "Uncommon",
            FishRarity.RARE => "Rare",
            FishRarity.EPIC => "Epic",
            FishRarity.LEGENDARY => "Legendary",
            _ => "Null",
        };
    }

    public static Color RarityToColor(FishRarity rarity) {
        return rarity switch {
            FishRarity.COMMON => Color.white,
            FishRarity.UNCOMMON => Color.green,
            FishRarity.RARE => Color.blue,
            FishRarity.EPIC => new Color(130, 0, 255, 255),
            FishRarity.LEGENDARY => Color.yellow,
        };
    }
}
