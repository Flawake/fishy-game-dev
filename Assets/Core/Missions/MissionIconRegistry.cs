using UnityEngine;

public static class StaticMissionIconRegistry
{
    [SerializeField]
    public static Sprite fishdexProgressMissionIcon;
    [SerializeField]
    public static Sprite makeFriendsMissionIcon;
    [SerializeField]
    public static Sprite makeTradesMissionIcon;
}

public class MissionIconRegistry : MonoBehaviour
{
    [SerializeField]
    Sprite fishdexProgressMissionIcon;
    [SerializeField]
    Sprite makeFriendsMissionIcon;
    [SerializeField]
    Sprite makeTradesMissionIcon;

    void Awake()
    {
        StaticMissionIconRegistry.fishdexProgressMissionIcon = fishdexProgressMissionIcon;
        StaticMissionIconRegistry.makeTradesMissionIcon = makeFriendsMissionIcon;
        StaticMissionIconRegistry.makeTradesMissionIcon = makeTradesMissionIcon;
    }
}
