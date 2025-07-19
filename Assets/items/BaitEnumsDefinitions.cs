#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EnumConsistencyValidator
{
    static EnumConsistencyValidator()
    {
        ValidateEnums();
    }

    private static void ValidateEnums()
    {
        var itemBaitValues = Enum.GetValues(typeof(ItemBaitType)).Cast<int>().ToArray();
        var fishBaitValues = Enum.GetValues(typeof(FishBaitType)).Cast<int>().ToArray();

        var itemBaitNames = Enum.GetNames(typeof(ItemBaitType));
        var fishBaitNames = Enum.GetNames(typeof(FishBaitType));

        if (!itemBaitValues.SequenceEqual(fishBaitValues) || !itemBaitNames.SequenceEqual(fishBaitNames))
        {
            throw new InvalidOperationException("ItemBaitType and FishBaitType enums are inconsistent!");
        }
    }
}
#endif

public static class BaitFields
{
    public const int hook = 0b00000001;
    public const int dough = 0b00000010;
    public const int meat = 0b00000100;
    public const int insects = 0b00001000;
    public const int lure = 0b00010000;
    public const int shark = 0b00100000;
    public const int fish = 0b01000000;
}

[System.Flags]
public enum FishBaitType : int
{
    hook = BaitFields.hook,
    dough = BaitFields.dough,
    meat = BaitFields.meat,
    insects = BaitFields.insects,
    lure = BaitFields.lure,
    shark = BaitFields.shark,
    fish = BaitFields.fish,
}

public enum ItemBaitType : int
{
    hook = BaitFields.hook,
    dough = BaitFields.dough,
    meat = BaitFields.meat,
    insects = BaitFields.insects,
    lure = BaitFields.lure,
    shark = BaitFields.shark,
    fish = BaitFields.fish,
}

public static class BaitEnumsDefinition
{
    public static bool FishBaitContainsItemBait(FishBaitType fishBait, ItemBaitType itemBait)
    {
        return (fishBait & (FishBaitType)itemBait) != 0;
    }
}
