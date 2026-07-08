using System;
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
    Greenfields = 0b00000100,
}

public class FishEnumConfig
{
    public static Locations AreaToLocation(Area area)
    {
        if (!TryAreaToLocation(area, out Locations loc))
        {
            throw new NotSupportedException($"Could not make location from {area}");
        }

        return loc;
    }

    // Non throwing variant of AreaToLocation, returns false for areas without fishing water
    public static bool TryAreaToLocation(Area area, out Locations location)
    {
        switch (area)
        {
            case Area.FusetaBeach:
                location = Locations.Beach;
                return true;
            case Area.SelvaBandeira:
                location = Locations.Tropical;
                return true;
            case Area.Greenfields:
                location = Locations.Greenfields;
                return true;
            default:
                location = default;
                return false;
        }
    }
    public static byte RarityToInt(FishRarity rarity)
    {
        return rarity switch
        {
            FishRarity.COMMON => 1,
            FishRarity.UNCOMMON => 2,
            FishRarity.RARE => 3,
            FishRarity.EPIC => 4,
            FishRarity.LEGENDARY => 5,
            _ => throw new System.NotImplementedException(),  // Should be unreachable
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
            _ => throw new System.NotImplementedException(),  // Should be unreachable
        };
    }

    public static Color RarityToColor(FishRarity rarity) {
        return rarity switch
        {
            FishRarity.COMMON => new Color(0.616f, 0.616f, 0.616f),      // #9D9D9D
            FishRarity.UNCOMMON => new Color(0.294f, 0.686f, 0.294f),    // #4BAF4B
            FishRarity.RARE => new Color(0.227f, 0.506f, 1.0f),          // #3A81FF
            FishRarity.EPIC => new Color(0.482f, 0.090f, 0.737f),       // #7B17BC
            FishRarity.LEGENDARY => new Color(1.0f, 0.647f, 0.227f),    // #FFA53A
            _ => throw new System.NotImplementedException(),  // Should be unreachable
        };
    }
}
