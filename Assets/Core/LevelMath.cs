using System;
using UnityEditor;
using UnityEngine;

public static class LevelMath
{

    static int XpToLevelHelper(int Xp)
    {
        double level = Math.Sqrt(Xp / 10) + 1;

        return (int)Math.Floor(level);
    }

    static int LevelToXpHelper(int level)
    {
        int xp = (int)(10 * Math.Pow((level - 1),  2));
        return (int)xp;
    }
    
    public static (int level, int xpBeginLevel, int curXP, int xpEndLevel) XpToLevel(int xp)
    {

        int level;
        int xpBeginLevel;
        int xpEndLevel;

        level = XpToLevelHelper(xp);
        xpBeginLevel = LevelToXpHelper(level);
        xpEndLevel = LevelToXpHelper(level + 1);

        int curXP = xp - xpBeginLevel;

        return (level, xpBeginLevel, curXP, xpEndLevel);
    }

#if UNITY_EDITOR
    //Automated tests in the unity editor
    [InitializeOnLoadMethod]
    static void TestLevelFunctions()
    {
        //0 XP must return level 1
        if(XpToLevelHelper(0) != 1)
        {
            Debug.LogError($"0 XP did not return level 1, it returned level: {XpToLevelHelper(0)}");
        }
        //160 XP must return level 5
        if (XpToLevelHelper(160) != 5)
        {
            Debug.LogError($"160 XP did not return level 5, it returned level: {XpToLevelHelper(100)}");
        }
        //Level 1 must return 0 xp
        if (LevelToXpHelper(1) != 0)
        {
            Debug.LogError($"Level 1 did not return XP 0, it returned xp: {LevelToXpHelper(1)}");
        }        
        //level 5 must return 160 xp
        if (LevelToXpHelper(5) != 160)
        {
            Debug.LogError($"Level 5 did not return XP 160, it returned xp: {LevelToXpHelper(5)}");
        }
    }
#endif
}
