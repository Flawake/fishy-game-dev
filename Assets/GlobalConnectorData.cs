using UnityEngine;

[System.Serializable]
public struct AreaImageConnector
{
    public Area Area;
    public Sprite AreaImage;
}

public static class GlobalConnector
{
    public static AreaImageConnector[] areaImageConnector;
}

public class GlobalConnectorData : MonoBehaviour
{
    [SerializeField]
    AreaImageConnector[] areaImageConnectorData;

    void Awake()
    {
        GlobalConnector.areaImageConnector = areaImageConnectorData;
    }
}
